# Decisiones del proyecto (Iteración 0 y posteriores)

Estado al 25/11/2026 (revisado por la noche: diseño intra-sujeto y herramienta de generación automática). Cada decisión indica qué se eligió, por qué y dónde se aplica en el código.

Interfaces: Figma «Makoto · Interfaces» (https://www.figma.com/design/BnRPGTBOFFFJMNFhBsl04p). Contiene las pantallas P1–P6 del participante y A0–A5 del administrador.

| ID | Decisión | Elección | Dónde se aplica |
| --- | --- | --- | --- |
| D-01 | Alcance del «chatbot» | Un turno: el estudiante lee un mensaje fijo de estrés académico y escucha la respuesta en **todas las voces activas**, una tras otra. Diálogo y chat abierto quedan como trabajo futuro | `estimulo.js`, pantallas P2–P4 |
| D-02 | Diseño estadístico | **Cambia a intra-sujeto (25/11):** cada participante escucha todas las voces activas en orden contrabalanceado (cuadrado latino) y las evalúa. Análisis: ANOVA de medidas repetidas 2×3 o modelo lineal mixto (participante como efecto aleatorio), con el orden como covariable. Requiere menos participantes que el diseño entre sujetos. La Metodología debe reemplazar «ANOVA factorial entre sujetos» | El Asignador pasa a asignar un **orden** de voces, no una celda |
| D-03 | F0 por celda | F 175/210/255 Hz · M 100/118/138 Hz, ±5 Hz, fuera de 145–165 Hz. Cambia respecto a los valores de partida del plan (F 175/205/235 · M 105/122/138) para separar más los niveles | `tools/audio-pipeline/config.json`, `CatalogoCondiciones.cs` |
| D-04 | Voces de síntesis | Azure Speech, es-EC-AndreaNeural y es-EC-LuisNeural, velocidad −7 % / +7 % para igualar duración. Solo en el proceso de síntesis previo, nunca en ejecución | `tools/audio-pipeline` |
| D-05 | Escenario y guion | Un escenario (exámenes y proyecto en la misma semana) y un guion de 97 palabras (≈ 39 s), sin marcas de género (ver abajo) | `guion.txt`, `estimulo.js` |
| D-06 | Instrumentos | PETS (principal) y Calidez **después de cada voz**, más chequeos de manipulación y un comentario por voz. Al final: ranking de las voces, observaciones generales y covariables | [instrumentos.md](instrumentos.md) · pantallas P4–P5 |
| D-07 | Ética y datos | Proyecto académico sin comité de ética. Consentimiento informado obligatorio: participación anónima y voluntaria, solo 18+, uso exclusivamente estadístico, sin divulgación individual, recursos de apoyo | `consentimiento.js` (versión `v1`) |
| D-08 | Aleatorización | Celda menos ocupada (completadas **+ reservas activas**), empates al azar, reserva de 30 min, transacción con candado. Equivale a bloques permutados de 6. Se cuentan las reservas activas porque, si solo contaran las completadas, las sesiones simultáneas caerían todas en la misma celda | `AsignadorCondiciones` |
| D-09 | Seguridad | HTTPS de Render, HSTS, CSP, límite de intentos del panel, PWA sin caché de API ni audio. Login JWT pendiente (T2-20) | `Program.cs`, `vite.config.js` |
| D-10 / D-11 / D-12 | Roles, tablero y acuerdos con tutores | Organización del equipo; ver la planificación | Excel de planificación |
| D-13 | Hosting | Render (un servicio Docker con API y PWA) + Neon (rama producción, AWS us-east-2). **La generación de pistas también corre en el servidor:** Python, Praat y ffmpeg dentro de la imagen, y los audios guardados en Neon, porque el disco de Render gratis se borra en cada despliegue | `render.yaml`, `Dockerfile.render` |
| D-14 | Integración continua | **GitHub Actions deshabilitado** (cuenta universitaria). Se reemplaza por `tools/verificar.ps1` antes de cada push y por el hook `.githooks/pre-commit` contra secretos. Render despliega solo manualmente | `tools/verificar.ps1`, `render.yaml` |
| D-15 | Voz a partir de un audio (clonación) | **Aplazado** (Tarjeta 4, pantalla A5 marcada «Próximamente»). Opciones a analizar: ElevenLabs API o un modelo abierto local (XTTS, OpenVoice, F5-TTS); también grabar la voz del autor (Tarjeta 3). Objetivo final: un asistente que interactúe con el usuario | Trabajo futuro |
| D-16 | Herramienta experimental automática | El panel del administrador hace todo sin scripts ni consola: subir el guion, definir voces, F0, velocidad y tolerancias, sintetizar con Azure, calibrar y certificar con Praat (Tarjeta 2), subir pistas ya hechas (Tarjeta 1) y grabaciones propias ajustadas al F0 con PSOLA (Tarjeta 3). Las voces son dinámicas: se pueden agregar más allá de las 6 celdas, y se elige cuáles entran al test. Los parámetros de la Introducción son los valores por defecto, no un límite | Pantallas A0–A5 |

## D-05 · Estímulo

**Mensaje del estudiante (en pantalla):**

> Tengo varios exámenes esta semana y un proyecto que todavía no termino. Siento que no me alcanza el tiempo y ya no sé por dónde empezar.

**Respuesta del asistente (audio, igual en las 6 voces):**

> Gracias por contarme lo que estás viviendo. Tener varios exámenes y un proyecto pendiente en la misma semana es mucha carga, y es normal sentir que el tiempo no alcanza. Que te sientas así no significa que no puedas lograrlo; muestra que te importa hacerlo bien. Si te parece, empecemos por algo pequeño: respira con calma un momento y elige una sola tarea para avanzar hoy, aunque sea media hora. Paso a paso, la carga se vuelve más manejable. Y recuerda que no estás sin apoyo: en Bienestar Estudiantil de tu universidad hay personas dispuestas a escucharte.

Criterios del guion:
- **Componentes de una respuesta empática:**
  - gratitud por compartir;
  - reflejo del contenido (exámenes y proyecto, falta de tiempo);
  - validación y normalización («es normal sentir…»);
  - reencuadre («muestra que te importa hacerlo bien»);
  - un paso concreto y pequeño;
  - esperanza;
  - derivación a un recurso humano (Bienestar Estudiantil).
- **Sin marcas de género.** El asistente no usa adjetivos sobre sí mismo, y al estudiante no se le dice «solo/sola» ni «cansado/a». Así, la única diferencia entre condiciones es la voz.
- **Seguridad.** No da consejos clínicos ni diagnósticos, y termina derivando a un recurso humano.
- **Duración.** 97 palabras, unos 39 s con las velocidades de D-04, dentro del rango de 35–45 s.

Cada cambio del guion exige volver a sintetizar y certificar los 6 audios. Ver la sección «Audios» del README.
