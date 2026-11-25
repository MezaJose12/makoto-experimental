"""Pruebas de la certificación con pistas crudas simuladas (sin Azure)."""
import json
import sys
from pathlib import Path

import numpy as np
import parselmouth
import pytest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import certificar as c  # noqa: E402
import sintetizar as s  # noqa: E402


def pista(ruta: Path, f0: float, segundos: float, amplitud: float, fs: int = 24000) -> None:
    t = np.arange(int(segundos * fs)) / fs
    senal = sum(np.sin(2 * np.pi * f0 * k * t) / k for k in range(1, 9))
    parselmouth.Sound(amplitud * senal / np.max(np.abs(senal)), sampling_frequency=fs).save(str(ruta), "WAV")


def preparar_crudos(carpeta: Path, config: s.Config, duraciones: dict | None = None) -> Path:
    carpeta.mkdir(parents=True)
    amplitudes = [0.05, 0.2, 0.5, 0.1, 0.3, 0.8]   # volúmenes muy distintos a propósito
    for celda, amp in zip(config.celdas, amplitudes):
        segundos = (duraciones or {}).get(celda.codigo, 3.0)
        pista(carpeta / f"{celda.codigo.lower()}.wav", celda.f0_objetivo_hz, segundos, amp)
    sintesis = {"guion": "texto de prueba",
                "celdas": [{"codigo": x.codigo, "voz": config.voces[x.genero]} for x in config.celdas]}
    (carpeta / "sintesis.json").write_text(json.dumps(sintesis), encoding="utf-8")
    return carpeta


@pytest.fixture
def config():
    return s.cargar_config()


def test_certifica_iguala_volumen_y_genera_mp3_verificables(tmp_path, config):
    crudos = preparar_crudos(tmp_path / "crudos", config)
    informe = c.certificar(config, c.cargar_criterios(), crudos, tmp_path / "certificados")

    assert informe["aprobada"], informe
    intensidades = [x["intensidadDb"] for x in informe["celdas"]]
    assert max(intensidades) - min(intensidades) <= 1.0
    for celda in informe["celdas"]:
        mp3 = tmp_path / "certificados" / celda["archivo"]
        assert mp3.suffix == ".mp3" and mp3.exists()
        assert celda["sha256"] == c.sha256(mp3)
        assert abs(celda["f0MedioHz"] - celda["f0ObjetivoHz"]) <= 2
        assert celda["voz"].startswith("es-EC-")


def test_rechaza_duraciones_no_comparables(tmp_path, config):
    crudos = preparar_crudos(tmp_path / "crudos", config, duraciones={"M_AGUDA": 5.0})
    informe = c.certificar(config, c.cargar_criterios(), crudos, tmp_path / "certificados")

    assert not informe["aprobada"]
    assert any("duraciones" in o for o in informe["observacionesGlobales"])
    assert not any(x["aprobada"] for x in informe["celdas"])


def test_rechaza_una_pista_con_f0_fuera_de_tolerancia(tmp_path, config):
    crudos = preparar_crudos(tmp_path / "crudos", config)
    pista(crudos / "f_grave.wav", 186.0, 3.0, 0.2)   # 11 Hz por encima de 175
    informe = c.certificar(config, c.cargar_criterios(), crudos, tmp_path / "certificados")

    f_grave = next(x for x in informe["celdas"] if x["codigo"] == "F_GRAVE")
    assert not f_grave["aprobada"] and any("F0" in o for o in f_grave["observaciones"])
    assert not informe["aprobada"]


def test_avisa_si_faltan_las_pistas_crudas(tmp_path, config):
    with pytest.raises(FileNotFoundError, match="sintetizar.py generar"):
        c.certificar(config, c.cargar_criterios(), tmp_path / "vacio", tmp_path / "certificados")
