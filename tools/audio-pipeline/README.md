# Pipeline de estímulos: síntesis (T2-03) y certificación (T2-04)

Genera las 6 pistas del diseño 2 × 3 con **Azure Speech** y ajusta el tono de cada una hasta que su F0 medio, medido con **Praat** (Parselmouth), quede cerca del objetivo. Se ejecuta una sola vez en el equipo de desarrollo: la API nunca llama a Azure.

```text
tools/audio-pipeline/
├── sintetizar.py      # T2-03: comandos voces y generar
├── certificar.py      # T2-04: iguala volumen, MP3, mide con Praat y firma con SHA-256
├── config.json        # voces, F0 objetivo por celda, tolerancias (deben coincidir con CatalogoCondiciones.cs)
├── guion.txt          # texto que dicen las 6 voces (idéntico para todas)
├── requirements.txt
└── tests/             # pruebas sin Azure (sintetizador simulado + Praat real)
```

## Cómo funciona

Para cada celda:

1. Sintetiza el guion con SSML: `<prosody pitch="+12Hz">`, un ajuste relativo al tono natural de la voz.
2. Mide el F0 medio de los tramos con voz con Praat (`Get mean`), con rango 70–300 Hz para la voz masculina y 100–500 Hz para la femenina.
3. Corrige el ajuste con el error medido y repite hasta quedar a ±2 Hz (`precisionHz`), con un máximo de 5 intentos.
4. Se queda con el mejor intento. La pista se acepta si está a ±5 Hz (`toleranciaHz`) y fuera de la banda ambigua de 145–165 Hz.

Las voces son `es-EC-AndreaNeural` (femenina) y `es-EC-LuisNeural` (masculina), de español de Ecuador. La velocidad es la misma para las dos.

## Preparación (una vez)

1. Active **Azure for Students** (azure.microsoft.com/free/students). No pide tarjeta.
2. En el portal de Azure, cree un recurso **Speech** con el plan gratuito **F0** (incluye 0,5 millones de caracteres neuronales al mes). Copie la **clave** y la **región** (por ejemplo `eastus`).
3. Instale Python 3.11 o superior y, en esta carpeta:

```powershell
python -m venv .venv
.venv\Scripts\activate
pip install -r requirements.txt
```

4. Defina las credenciales en la terminal. No las guarde en ningún archivo del repositorio:

```powershell
$env:AZURE_SPEECH_KEY = "<su clave>"
$env:AZURE_SPEECH_REGION = "<su región>"
```

## Uso

```powershell
python sintetizar.py voces                    # comprueba que las voces es-EC existen en su región
python sintetizar.py generar                  # genera las 6 pistas
python sintetizar.py generar --celda F_GRAVE  # solo una celda
pytest -q tests                               # pruebas, sin Azure
```

Salida en `datos/audios/crudos/`:

- `f_grave.wav` … `m_aguda.wav`: pistas crudas en WAV 24 kHz, 16 bits, mono. No se suben a Git.
- `sintesis.json`: voz, ajuste de tono, F0 medido, desviación, duración e intentos de cada celda. **Sí se sube**: documenta cómo se generó cada estímulo.

El comando termina con código 1 si alguna celda queda fuera de tolerancia o en la banda ambigua.

## Certificación (T2-04)

```powershell
python certificar.py
```

Para cada pista cruda:

1. Iguala la intensidad media a 70 dB (Praat: *Scale intensity*), para que ninguna voz suene más fuerte.
2. La convierte a MP3 de 128 kbps, mono, con el ffmpeg que trae `imageio-ffmpeg` (no hay que instalarlo aparte).
3. Mide **sobre el MP3 final**, que es lo que escucha el participante: F0 medio, desviación, mínimo y máximo, duración e intensidad.
4. Calcula su huella SHA-256.

Una pista se aprueba si su F0 está a ±5 Hz del objetivo y fuera de la banda 145–165 Hz, y si no se satura. Además, las 6 deben tener intensidades a ±1 dB entre sí y duraciones con menos de 1 s de diferencia (parámetros en `config.json → certificacion`).

Salida en `datos/audios/certificados/` (todo se sube a Git):

- `f_grave.mp3` … `m_aguda.mp3`: los estímulos definitivos.
- `certificacion.json`: valores medidos, huellas, criterios y versiones de Praat y Parselmouth.

Al arrancar, la API lee `certificacion.json`, verifica la huella de cada MP3 y guarda en `condiciones_vocales` el F0 medido, la duración, la huella y la voz. Si un MP3 falta o cambió después de certificarse, su condición queda como *pendiente* y el Dashboard lo muestra.

## Importante

- **`guion.txt` es un borrador de prueba.** Cuando tenga el guion validado (T1-04), reemplácelo y repita `generar` y `certificar`.
- Si cambia un F0 objetivo, cámbielo también en `backend/Makoto.Api/Data/CatalogoCondiciones.cs`.
- Coste: cada ejecución completa usa unos 14 000 caracteres (6 celdas × 2–3 intentos × unos 550 caracteres), muy por debajo del límite gratuito.
