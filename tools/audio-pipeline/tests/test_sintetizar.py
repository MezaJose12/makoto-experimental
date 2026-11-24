"""Pruebas del pipeline sin llamar a Azure: un sintetizador simulado y la medición real de Praat."""
import json
import sys
from pathlib import Path
from xml.dom import minidom

import numpy as np
import parselmouth
import pytest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import sintetizar as s  # noqa: E402


def tono_armonico(ruta: Path, f0: float, segundos: float = 2.0, fs: int = 24000) -> Path:
    """Sonido periódico con 8 armónicos (más parecido a una voz que un seno puro)."""
    t = np.arange(int(segundos * fs)) / fs
    senal = sum(np.sin(2 * np.pi * f0 * k * t) / k for k in range(1, 9))
    senal = 0.3 * senal / np.max(np.abs(senal))
    parselmouth.Sound(senal, sampling_frequency=fs).save(str(ruta), "WAV")
    return ruta


def test_ssml_escapa_el_texto_y_aplica_el_ajuste():
    ssml = s.construir_ssml("Hola & <bienvenido>", "es-EC-AndreaNeural", "es-EC", -25, "0%")
    doc = minidom.parseString(ssml)  # XML válido
    prosody = doc.getElementsByTagName("prosody")[0]
    assert prosody.getAttribute("pitch") == "-25Hz"
    assert prosody.firstChild.data == "Hola & <bienvenido>"
    assert s.construir_ssml("x", "v", "es-EC", 12, "0%").count('pitch="+12Hz"') == 1


def test_la_configuracion_del_repositorio_es_valida():
    config = s.cargar_config()
    assert len(config.celdas) == 6
    assert {c.genero for c in config.celdas} == set(config.voces)


def test_rechaza_un_objetivo_en_la_banda_ambigua(tmp_path):
    datos = json.loads((s.RAIZ / "config.json").read_text(encoding="utf-8"))
    datos["celdas"][0]["f0ObjetivoHz"] = 165
    ruta = tmp_path / "config.json"
    ruta.write_text(json.dumps(datos), encoding="utf-8")
    with pytest.raises(ValueError, match="banda ambigua"):
        s.cargar_config(ruta)


@pytest.mark.parametrize("f0", [100.0, 138.0, 175.0, 255.0])
def test_praat_mide_el_f0_con_precision(tmp_path, f0):
    piso, techo = (70, 300) if f0 < 150 else (100, 500)
    m = s.medir_f0(tono_armonico(tmp_path / "t.wav", f0), piso, techo)
    assert abs(m.f0_media_hz - f0) < 1.0
    assert m.duracion_s == pytest.approx(2.0, abs=0.01)


@pytest.mark.parametrize("objetivo,natural", [(175, 205), (255, 205), (100, 112), (138, 112)])
def test_la_calibracion_converge_aunque_la_voz_responda_de_forma_imperfecta(tmp_path, objetivo, natural):
    # La "voz" simulada solo sube el 80 % de lo pedido, como ocurre con voces reales
    def sintetizar(desplazamiento: int) -> Path:
        return tono_armonico(tmp_path / "pista.wav", natural + 0.8 * desplazamiento)

    piso, techo = (70, 300) if objetivo < 150 else (100, 500)
    celda = s.Celda("X", "Femenino", objetivo)
    r = s.calibrar_celda(celda, "voz", sintetizar, lambda p: s.medir_f0(p, piso, techo),
                         tolerancia_hz=5, max_intentos=5, precision_hz=2)
    assert r.dentro_de_tolerancia
    assert abs(r.medicion.f0_media_hz - objetivo) <= 2
    assert len(r.historial) <= 5


def test_informa_cuando_no_alcanza_la_tolerancia(tmp_path):
    # Una voz que no cambia de tono: debe terminar marcada para revisar, sin bucle infinito
    def sintetizar(_desplazamiento: int) -> Path:
        return tono_armonico(tmp_path / "pista.wav", 205)

    r = s.calibrar_celda(s.Celda("X", "Femenino", 255), "voz", sintetizar,
                         lambda p: s.medir_f0(p, 100, 500), tolerancia_hz=5, max_intentos=3)
    assert not r.dentro_de_tolerancia
    assert len(r.historial) <= 4          # 3 intentos + regenerar el mejor


def test_conserva_el_mejor_intento_aunque_el_ultimo_empeore(tmp_path):
    # Voz que responde bien al primer ajuste y luego se pasa: debe quedarse con el mejor
    respuestas = iter([205.0, 252.0, 300.0, 300.0])
    tonos = {}

    def sintetizar(desplazamiento: int) -> Path:
        f0 = tonos.setdefault(desplazamiento, next(respuestas))
        return tono_armonico(tmp_path / "pista.wav", f0)

    r = s.calibrar_celda(s.Celda("X", "Femenino", 255), "voz", sintetizar,
                         lambda p: s.medir_f0(p, 100, 500), tolerancia_hz=5, max_intentos=3, precision_hz=2)
    assert r.dentro_de_tolerancia
    assert abs(r.medicion.f0_media_hz - 252) < 1   # el archivo final es el del mejor intento
