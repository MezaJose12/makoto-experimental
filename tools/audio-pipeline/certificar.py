"""
Certificación de los 6 estímulos (T2-04).

Toma las pistas crudas de T2-03 (datos/audios/crudos/*.wav), iguala su intensidad, las convierte
a MP3 y mide con Praat el MP3 final: F0 (media, desviación, mínimo, máximo), duración e intensidad.
Escribe los MP3 y certificacion.json en datos/audios/certificados/. La API verifica la huella
SHA-256 de cada MP3 al arrancar y guarda los valores medidos en la base de datos.

Uso:
    python certificar.py
"""
from __future__ import annotations

import datetime as dt
import hashlib
import json
import subprocess
import sys
import tempfile
from dataclasses import dataclass
from pathlib import Path

import imageio_ffmpeg
import parselmouth
from parselmouth.praat import call

import sintetizar as s

CRUDOS = s.SALIDA
CERTIFICADOS = s.REPO / "datos" / "audios" / "certificados"


@dataclass(frozen=True)
class Criterios:
    intensidad_objetivo_db: float
    tolerancia_intensidad_db: float
    max_diferencia_duracion_s: float
    mp3_kbps: int
    pico_maximo: float = 0.99


def cargar_criterios(ruta: Path = s.RAIZ / "config.json") -> Criterios:
    c = json.loads(ruta.read_text(encoding="utf-8"))["certificacion"]
    return Criterios(float(c["intensidadObjetivoDb"]), float(c["toleranciaIntensidadDb"]),
                     float(c["maxDiferenciaDuracionS"]), int(c["mp3Kbps"]))


# ── Procesamiento ──────────────────────────────────────────────────────────

def igualar_intensidad(origen: Path, destino: Path, objetivo_db: float) -> float:
    """Escala la pista a la intensidad media objetivo (Praat: Scale intensity). Devuelve el pico."""
    sonido = parselmouth.Sound(str(origen))
    call(sonido, "Scale intensity", objetivo_db)
    sonido.save(str(destino), "WAV")
    return float(abs(sonido.values).max())


def codificar_mp3(origen: Path, destino: Path, kbps: int) -> None:
    destino.parent.mkdir(parents=True, exist_ok=True)
    resultado = subprocess.run(
        [imageio_ffmpeg.get_ffmpeg_exe(), "-y", "-loglevel", "error", "-i", str(origen),
         "-codec:a", "libmp3lame", "-b:a", f"{kbps}k", "-ac", "1", str(destino)],
        capture_output=True, text=True)
    if resultado.returncode != 0:
        raise RuntimeError(f"ffmpeg no pudo convertir {origen.name}: {resultado.stderr.strip()}")


def medir(ruta: Path, piso_hz: float, techo_hz: float) -> dict:
    sonido = parselmouth.Sound(str(ruta))
    pitch = sonido.to_pitch(pitch_floor=piso_hz, pitch_ceiling=techo_hz)
    valores = {
        "f0MedioHz": call(pitch, "Get mean", 0, 0, "Hertz"),
        "f0DesviacionHz": call(pitch, "Get standard deviation", 0, 0, "Hertz"),
        "f0MinimoHz": call(pitch, "Get minimum", 0, 0, "Hertz", "Parabolic"),
        "f0MaximoHz": call(pitch, "Get maximum", 0, 0, "Hertz", "Parabolic"),
        "duracionS": sonido.duration,
        "intensidadDb": call(sonido, "Get intensity (dB)"),
    }
    if valores["f0MedioHz"] != valores["f0MedioHz"]:  # NaN
        raise RuntimeError(f"No se detectó voz en {ruta.name}.")
    return {k: round(float(v), 2) for k, v in valores.items()}


def sha256(ruta: Path) -> str:
    return hashlib.sha256(ruta.read_bytes()).hexdigest()


# ── Certificación ──────────────────────────────────────────────────────────

def certificar(config: s.Config, criterios: Criterios, crudos: Path = CRUDOS,
               certificados: Path = CERTIFICADOS) -> dict:
    sintesis_ruta = crudos / "sintesis.json"
    if not sintesis_ruta.exists():
        raise FileNotFoundError(f"No existe {sintesis_ruta}. Ejecute antes: python sintetizar.py generar")
    sintesis = json.loads(sintesis_ruta.read_text(encoding="utf-8"))
    voz_por_celda = {c["codigo"]: c["voz"] for c in sintesis["celdas"]}

    celdas = []
    with tempfile.TemporaryDirectory() as tmp:
        for celda in config.celdas:
            crudo = crudos / f"{celda.codigo.lower()}.wav"
            if not crudo.exists():
                raise FileNotFoundError(f"Falta la pista cruda {crudo.name}.")
            igualada = Path(tmp) / crudo.name
            pico = igualar_intensidad(crudo, igualada, criterios.intensidad_objetivo_db)
            mp3 = certificados / f"{celda.codigo.lower()}.mp3"
            codificar_mp3(igualada, mp3, criterios.mp3_kbps)

            piso, techo = config.rango_praat_hz[celda.genero]
            m = medir(mp3, piso, techo)
            observaciones = []
            if abs(m["f0MedioHz"] - celda.f0_objetivo_hz) > config.tolerancia_hz:
                observaciones.append(f"F0 {m['f0MedioHz']} Hz fuera de ±{config.tolerancia_hz:g} Hz del objetivo")
            if config.toca_banda_ambigua(m["f0MedioHz"]):
                observaciones.append("F0 dentro de la banda ambigua")
            if pico > criterios.pico_maximo:
                observaciones.append(f"saturación al igualar la intensidad (pico {pico:.2f}): baje intensidadObjetivoDb")
            celdas.append({
                "codigo": celda.codigo,
                "archivo": mp3.name,
                "voz": voz_por_celda.get(celda.codigo, config.voces[celda.genero]),
                "genero": celda.genero,
                "f0ObjetivoHz": celda.f0_objetivo_hz,
                **m,
                "picoNormalizado": round(pico, 3),
                "sha256": sha256(mp3),
                "bytes": mp3.stat().st_size,
                "observaciones": observaciones,
            })

    # Criterios entre pistas: mismo volumen y duraciones comparables
    globales = []
    intensidades = [c["intensidadDb"] for c in celdas]
    duraciones = [c["duracionS"] for c in celdas]
    if max(intensidades) - min(intensidades) > criterios.tolerancia_intensidad_db:
        globales.append(f"intensidades difieren {max(intensidades) - min(intensidades):.2f} dB "
                        f"(máximo {criterios.tolerancia_intensidad_db:g} dB)")
    if max(duraciones) - min(duraciones) > criterios.max_diferencia_duracion_s:
        globales.append(f"duraciones difieren {max(duraciones) - min(duraciones):.2f} s "
                        f"(máximo {criterios.max_diferencia_duracion_s:g} s)")
    for c in celdas:
        c["aprobada"] = not c["observaciones"] and not globales

    return {
        "version": 1,
        "certificadoUtc": dt.datetime.now(dt.timezone.utc).isoformat(timespec="seconds"),
        "herramientas": {"praat": parselmouth.PRAAT_VERSION, "parselmouth": parselmouth.__version__,
                         "imageioFfmpeg": imageio_ffmpeg.__version__},
        "guionSha256": hashlib.sha256(sintesis.get("guion", "").encode("utf-8")).hexdigest(),
        "criterios": {
            "toleranciaF0Hz": config.tolerancia_hz,
            "bandaAmbiguaHz": list(config.banda_ambigua_hz),
            "intensidadObjetivoDb": criterios.intensidad_objetivo_db,
            "toleranciaIntensidadDb": criterios.tolerancia_intensidad_db,
            "maxDiferenciaDuracionS": criterios.max_diferencia_duracion_s,
            "mp3Kbps": criterios.mp3_kbps,
        },
        "observacionesGlobales": globales,
        "aprobada": all(c["aprobada"] for c in celdas),
        "celdas": celdas,
    }


def main() -> int:
    config, criterios = s.cargar_config(), cargar_criterios()
    informe = certificar(config, criterios)
    CERTIFICADOS.mkdir(parents=True, exist_ok=True)
    (CERTIFICADOS / "certificacion.json").write_text(
        json.dumps(informe, ensure_ascii=False, indent=2), encoding="utf-8")

    print(f"{'Celda':<9} {'Objetivo':>8} {'F0 medio':>9} {'DE':>6} {'Duración':>9} {'Intens.':>8}  Estado")
    for c in informe["celdas"]:
        estado = "OK" if c["aprobada"] else "REVISAR"
        print(f"{c['codigo']:<9} {c['f0ObjetivoHz']:>6.0f} Hz {c['f0MedioHz']:>6.1f} Hz {c['f0DesviacionHz']:>6.1f} "
              f"{c['duracionS']:>7.2f} s {c['intensidadDb']:>5.1f} dB  {estado}")
        for o in c["observaciones"]:
            print(f"          · {o}")
    for o in informe["observacionesGlobales"]:
        print(f"Todas las pistas · {o}")
    print(f"\nCertificado: {CERTIFICADOS / 'certificacion.json'}")
    if not informe["aprobada"]:
        print("La certificación NO se aprobó: la API no marcará estas pistas como certificadas.")
        return 1
    print("Las 6 pistas quedaron certificadas.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
