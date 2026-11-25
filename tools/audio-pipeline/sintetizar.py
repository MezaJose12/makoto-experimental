"""
Pipeline offline de síntesis de los 6 estímulos 2×3 (T2-03).

Para cada celda: sintetiza el guion con Azure Speech (SSML), mide el F0 medio con Praat,
corrige el ajuste de tono y repite hasta quedar dentro de la tolerancia.
Se ejecuta una vez en el equipo de desarrollo; la API nunca llama a Azure.

Uso:
    python sintetizar.py voces              # lista las voces es-EC disponibles
    python sintetizar.py generar            # genera las 6 pistas en datos/audios/crudos/
    python sintetizar.py generar --celda F_GRAVE

Variables de entorno: AZURE_SPEECH_KEY y AZURE_SPEECH_REGION (nunca en el código ni en Git).
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import os
import sys
import time
from dataclasses import dataclass, field
from pathlib import Path
from typing import Callable
from xml.sax.saxutils import escape

import parselmouth
from parselmouth.praat import call

RAIZ = Path(__file__).resolve().parent
REPO = RAIZ.parents[1]
SALIDA = REPO / "datos" / "audios" / "crudos"


# ── Configuración ──────────────────────────────────────────────────────────

@dataclass(frozen=True)
class Celda:
    codigo: str
    genero: str
    f0_objetivo_hz: float


@dataclass(frozen=True)
class Config:
    idioma: str
    formato_salida: str
    velocidad: dict[str, str]                    # por género: iguala la duración entre voces
    tolerancia_hz: float
    precision_hz: float
    max_intentos: int
    banda_ambigua_hz: tuple[float, float]
    voces: dict[str, str]
    rango_praat_hz: dict[str, tuple[float, float]]
    celdas: list[Celda]

    def toca_banda_ambigua(self, f0: float) -> bool:
        bajo, alto = self.banda_ambigua_hz
        return f0 + self.tolerancia_hz >= bajo and f0 - self.tolerancia_hz <= alto


def cargar_config(ruta: Path = RAIZ / "config.json") -> Config:
    datos = json.loads(ruta.read_text(encoding="utf-8"))
    config = Config(
        idioma=datos["idioma"],
        formato_salida=datos["formatoSalida"],
        velocidad=datos["velocidad"] if isinstance(datos["velocidad"], dict)
            else {g: datos["velocidad"] for g in datos["voces"]},
        tolerancia_hz=float(datos["toleranciaHz"]),
        precision_hz=float(datos["precisionHz"]),
        max_intentos=int(datos["maxIntentos"]),
        banda_ambigua_hz=tuple(datos["bandaAmbiguaHz"]),
        voces=datos["voces"],
        rango_praat_hz={g: tuple(r) for g, r in datos["rangoPraatHz"].items()},
        celdas=[Celda(c["codigo"], c["genero"], float(c["f0ObjetivoHz"])) for c in datos["celdas"]],
    )
    for celda in config.celdas:
        if celda.genero not in config.voces:
            raise ValueError(f"{celda.codigo}: no hay voz configurada para '{celda.genero}'.")
        if celda.genero not in config.velocidad:
            raise ValueError(f"{celda.codigo}: no hay velocidad configurada para '{celda.genero}'.")
        if config.toca_banda_ambigua(celda.f0_objetivo_hz):
            raise ValueError(f"{celda.codigo}: {celda.f0_objetivo_hz} Hz toca la banda ambigua {config.banda_ambigua_hz} Hz.")
    return config


# ── SSML y Azure ───────────────────────────────────────────────────────────

def construir_ssml(texto: str, voz: str, idioma: str, desplazamiento_hz: int, velocidad: str) -> str:
    """El tono se ajusta en Hz relativos a la voz base: '+12Hz' o '-25Hz'."""
    tono = f"{desplazamiento_hz:+d}Hz"
    return (
        f'<speak version="1.0" xmlns="http://www.w3.org/2001/10/synthesis" xml:lang="{idioma}">'
        f'<voice name="{voz}">'
        f'<prosody pitch="{tono}" rate="{velocidad}">{escape(texto.strip())}</prosody>'
        f"</voice></speak>"
    )


class ClienteAzure:
    def __init__(self, clave: str, region: str):
        import requests  # solo hace falta para llamar a Azure
        self._requests = requests
        self.clave, self.region = clave, region

    @classmethod
    def desde_entorno(cls) -> "ClienteAzure":
        clave, region = os.environ.get("AZURE_SPEECH_KEY"), os.environ.get("AZURE_SPEECH_REGION")
        if not clave or not region:
            sys.exit("Defina AZURE_SPEECH_KEY y AZURE_SPEECH_REGION (vea el README).")
        return cls(clave, region)

    def _post(self, ssml: str, formato: str) -> bytes:
        url = f"https://{self.region}.tts.speech.microsoft.com/cognitiveservices/v1"
        cabeceras = {
            "Ocp-Apim-Subscription-Key": self.clave,
            "Content-Type": "application/ssml+xml",
            "X-Microsoft-OutputFormat": formato,
            "User-Agent": "makoto-audio-pipeline",
        }
        for intento in range(4):
            r = self._requests.post(url, data=ssml.encode("utf-8"), headers=cabeceras, timeout=60)
            if r.status_code == 429:                      # límite del plan gratuito: esperar y reintentar
                time.sleep(int(r.headers.get("Retry-After", "5")) + intento * 2)
                continue
            if r.status_code != 200:
                raise RuntimeError(f"Azure respondió {r.status_code}: {r.text[:300]}")
            return r.content
        raise RuntimeError("Azure sigue limitando las solicitudes (429). Intente en un minuto.")

    def sintetizar(self, ssml: str, formato: str, destino: Path) -> Path:
        destino.parent.mkdir(parents=True, exist_ok=True)
        destino.write_bytes(self._post(ssml, formato))
        return destino

    def listar_voces(self, prefijo: str) -> list[dict]:
        url = f"https://{self.region}.tts.speech.microsoft.com/cognitiveservices/voices/list"
        r = self._requests.get(url, headers={"Ocp-Apim-Subscription-Key": self.clave}, timeout=30)
        r.raise_for_status()
        return [v for v in r.json() if v.get("Locale", "").startswith(prefijo)]


# ── Medición con Praat ─────────────────────────────────────────────────────

@dataclass(frozen=True)
class Medicion:
    f0_media_hz: float
    f0_sd_hz: float
    duracion_s: float


def medir_f0(ruta: Path, piso_hz: float, techo_hz: float) -> Medicion:
    """F0 medio y desviación de los tramos con voz (equivale a 'Get mean' en Praat)."""
    sonido = parselmouth.Sound(str(ruta))
    pitch = sonido.to_pitch(pitch_floor=piso_hz, pitch_ceiling=techo_hz)
    media = call(pitch, "Get mean", 0, 0, "Hertz")
    sd = call(pitch, "Get standard deviation", 0, 0, "Hertz")
    if media != media:  # NaN: no se detectó voz
        raise RuntimeError(f"No se detectó voz en {ruta.name} (rango {piso_hz}–{techo_hz} Hz).")
    return Medicion(round(media, 1), round(sd, 1), round(sonido.duration, 2))


# ── Calibración iterativa ──────────────────────────────────────────────────

@dataclass
class Resultado:
    codigo: str
    voz: str
    f0_objetivo_hz: float
    desplazamiento_hz: int
    medicion: Medicion
    dentro_de_tolerancia: bool
    historial: list[dict] = field(default_factory=list)


def calibrar_celda(
    celda: Celda,
    voz: str,
    sintetizar: Callable[[int], Path],
    medir: Callable[[Path], Medicion],
    tolerancia_hz: float,
    max_intentos: int,
    desplazamiento_inicial: int = 0,
    precision_hz: float = 2.0,
) -> Resultado:
    """
    Corrige el ajuste de tono con el error medido hasta quedar a ±precision_hz del objetivo.
    Se queda con el mejor intento; la pista se acepta si ese intento está a ±tolerancia_hz.
    """
    desplazamiento = desplazamiento_inicial
    historial: list[dict] = []
    mejor: tuple[int, Medicion, float] | None = None
    for _ in range(max_intentos):
        medicion = medir(sintetizar(desplazamiento))
        error = celda.f0_objetivo_hz - medicion.f0_media_hz
        historial.append({"desplazamientoHz": desplazamiento, "f0MedidoHz": medicion.f0_media_hz})
        if mejor is None or abs(error) < abs(mejor[2]):
            mejor = (desplazamiento, medicion, error)
        if abs(error) <= precision_hz:
            break
        desplazamiento = int(round(desplazamiento + error))

    desp, medicion, error = mejor
    if desp != historial[-1]["desplazamientoHz"]:
        # El archivo en disco es del último intento: se vuelve a generar el mejor
        medicion = medir(sintetizar(desp))
        error = celda.f0_objetivo_hz - medicion.f0_media_hz
        historial.append({"desplazamientoHz": desp, "f0MedidoHz": medicion.f0_media_hz})
    return Resultado(celda.codigo, voz, celda.f0_objetivo_hz, desp, medicion,
                     abs(error) <= tolerancia_hz, historial)


# ── Comandos ───────────────────────────────────────────────────────────────

def comando_voces(config: Config) -> int:
    cliente = ClienteAzure.desde_entorno()
    voces = cliente.listar_voces(config.idioma)
    for v in voces:
        print(f"{v['ShortName']:<28} {v.get('Gender', ''):<8} {v.get('VoiceType', '')}")
    faltan = [v for v in config.voces.values() if v not in {x['ShortName'] for x in voces}]
    if faltan:
        print(f"\nNo disponibles en {cliente.region}: {', '.join(faltan)}. Cambie las voces en config.json.")
        return 1
    print("\nLas dos voces de config.json están disponibles.")
    return 0


def comando_generar(config: Config, solo: str | None) -> int:
    texto = (RAIZ / "guion.txt").read_text(encoding="utf-8").strip()
    if not texto:
        sys.exit("guion.txt está vacío.")
    cliente = ClienteAzure.desde_entorno()
    celdas = [c for c in config.celdas if solo in (None, c.codigo)]
    if not celdas:
        sys.exit(f"No existe la celda {solo}.")

    resultados: list[Resultado] = []
    tono_natural: dict[str, float] = {}         # F0 natural de cada voz, aprendido en su primera celda
    for celda in celdas:
        voz = config.voces[celda.genero]
        piso, techo = config.rango_praat_hz[celda.genero]
        destino = SALIDA / f"{celda.codigo.lower()}.wav"
        print(f"→ {celda.codigo}: objetivo {celda.f0_objetivo_hz:.0f} Hz con {voz}")

        def sintetizar(desp: int, voz=voz, destino=destino, velocidad=config.velocidad[celda.genero]) -> Path:
            ssml = construir_ssml(texto, voz, config.idioma, desp, velocidad)
            return cliente.sintetizar(ssml, config.formato_salida, destino)

        natural = tono_natural.get(celda.genero)
        inicial = 0 if natural is None else int(round(celda.f0_objetivo_hz - natural))
        r = calibrar_celda(celda, voz, sintetizar, lambda p: medir_f0(p, piso, techo),
                           config.tolerancia_hz, config.max_intentos, desplazamiento_inicial=inicial,
                           precision_hz=config.precision_hz)
        tono_natural[celda.genero] = r.medicion.f0_media_hz - r.desplazamiento_hz
        for h in r.historial:
            print(f"   ajuste {h['desplazamientoHz']:+5d} Hz → F0 medido {h['f0MedidoHz']:.1f} Hz")
        ambiguo = config.toca_banda_ambigua(r.medicion.f0_media_hz)
        estado = "OK" if r.dentro_de_tolerancia and not ambiguo else "REVISAR"
        print(f"   {estado}: {r.medicion.f0_media_hz:.1f} Hz (DE {r.medicion.f0_sd_hz:.1f}) · {r.medicion.duracion_s:.2f} s\n")
        resultados.append(r)

    informe = {
        "generadoUtc": dt.datetime.now(dt.timezone.utc).isoformat(timespec="seconds"),
        "proveedor": f"Azure Speech ({cliente.region})",
        "idioma": config.idioma,
        "formato": config.formato_salida,
        "velocidad": config.velocidad,
        "guion": texto,
        "celdas": [
            {
                "codigo": r.codigo, "voz": r.voz, "f0ObjetivoHz": r.f0_objetivo_hz,
                "ajusteTonoHz": r.desplazamiento_hz, "f0MedidoHz": r.medicion.f0_media_hz,
                "f0DesviacionHz": r.medicion.f0_sd_hz, "duracionS": r.medicion.duracion_s,
                "dentroDeTolerancia": r.dentro_de_tolerancia,
                "archivo": f"{r.codigo.lower()}.wav", "intentos": r.historial,
            }
            for r in resultados
        ],
    }
    SALIDA.mkdir(parents=True, exist_ok=True)
    nombre = "sintesis.json" if solo is None else f"sintesis_{solo.lower()}.json"
    (SALIDA / nombre).write_text(json.dumps(informe, ensure_ascii=False, indent=2), encoding="utf-8")
    fallidas = [r.codigo for r in resultados
                if not r.dentro_de_tolerancia or config.toca_banda_ambigua(r.medicion.f0_media_hz)]
    print(f"Informe: {SALIDA / nombre}")
    if fallidas:
        print(f"Revisar: {', '.join(fallidas)} (fuera de ±{config.tolerancia_hz:.0f} Hz o en la banda ambigua).")
        return 1
    print("Las pistas quedaron dentro de la tolerancia.")
    return 0


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Síntesis de los estímulos 2×3 con Azure Speech")
    sub = parser.add_subparsers(dest="comando", required=True)
    sub.add_parser("voces", help="lista las voces disponibles para el idioma configurado")
    generar = sub.add_parser("generar", help="sintetiza y calibra las pistas")
    generar.add_argument("--celda", help="genera solo una celda, p. ej. F_GRAVE")
    args = parser.parse_args(argv)
    config = cargar_config()
    return comando_voces(config) if args.comando == "voces" else comando_generar(config, args.celda)


if __name__ == "__main__":
    sys.exit(main())
