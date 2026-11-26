# Mapa del proyecto Makoto

Qué hace cada carpeta y cada archivo, a qué tarea pertenece y en qué estado está.
**Mantener actualizado:** cada tarea que agregue, mueva o borre un archivo actualiza este mapa en el mismo commit.

Estado al 26/11/2026, después de T2-07 (último commit: `feat(T2-07)`).

Leyenda de estado:
- ✅ terminado
- 🟡 parcial o con pendientes
- ⏳ contrato o esqueleto, la lógica llega en otra tarea
- 🧊 sin uso por ahora

## Flujo de un vistazo

```text
Participante (PWA) ─HTTPS─▶ API ASP.NET Core ─SSL─▶ PostgreSQL (Neon en Render · Docker en local)
                              │
                              └── datos/audios/certificados (MP3 verificados por SHA-256 al arrancar)

tools/audio-pipeline (offline, Python): guion.txt ─Azure TTS─▶ crudos/*.wav ─Praat─▶ certificados/*.mp3
```

## Raíz

| Archivo | Qué hace | Tarea |
| --- | --- | --- |
| `Makoto.slnx` | Solución de Visual Studio 2026: `Makoto.Api` y `Makoto.Tests` | T2-01 ✅ |
| `global.json` | Fija el SDK de .NET 10 | T2-01 ✅ |
| `docker-compose.yml` | Entorno local: `postgres:17-alpine` (:5432, volumen `makoto_pgdata`) y `backend` (:8080, monta `datos/audios` en solo lectura). `NEON_DATABASE_URL` en el `.env` lo apunta a Neon | T2-01, T2-05, T2-06 ✅ |
| `Dockerfile.render` | Imagen única para Render: compila la PWA (Node 22), compila la API (.NET 10) y copia los MP3 certificados. Production, sin secretos | T2-18 ✅ |
| `render.yaml` | Blueprint de Render: servicio Docker gratuito en Ohio, `/health`, despliegue manual. Secretos con `sync: false` o `generateValue` | T2-18 ✅ |
| `.env.example` | Plantilla de variables locales **sin valores reales** (la copia la CI y `verificar.ps1`) | T2-01, T2-18 ✅ |
| `.env` | Sus secretos locales. **Nunca se sube** (ignorado) | — |
| `.gitignore` | Ignora `bin/obj`, `node_modules`, `.env*` (salvo plantillas), WAV/MP3 (salvo los certificados) y `.venv` | T2-18 ✅ |
| `.dockerignore` | Impide que `.env`, `.git`, `tools/`, `docs/` y audios crudos entren a una imagen | T2-18 ✅ |
| `.gitattributes` | El hook de Git conserva finales de línea LF | D-14 ✅ |
| `.githooks/pre-commit` | Rechaza commits con `.env`, certificados o URI y contraseñas de base de datos. Activo con `git config core.hooksPath .githooks` | D-14 ✅ |
| `.github/workflows/ci.yml` | CI de GitHub: back-end, PWA, pipeline de audio, gitleaks, imagen de Render y Docker Compose con concurrencia. **Actions está deshabilitado** (cuenta universitaria); lo reemplaza `tools/verificar.ps1` | T2-01…T2-18 🧊 |
| `README.md` | Guía principal: cómo ejecutar, contrato REST, despliegue, seguridad, verificación y problemas frecuentes | Todas ✅ |
| `CLAUDE.md` | Contexto y reglas de trabajo para Claude Code en este repositorio | — |

## `backend/Makoto.Api` · API ASP.NET Core 10

| Archivo | Qué hace | Tarea |
| --- | --- | --- |
| `Program.cs` | Arranque: puerto (`PORT`), conexión (`DATABASE_URL` o `ConnectionStrings:Default`) con reintentos, Asignador, CORS, límite de intentos del panel, salud, OpenAPI (solo en desarrollo), HSTS y cabeceras de seguridad, PWA estática con su caché, migraciones y sincronización de la certificación al arrancar | T2-01, T2-02, T2-05, T2-18 ✅ |
| `Makoto.Api.csproj` | Paquetes con versiones fijas: EF Core Design 10.0.15, Npgsql EF 10.0.4, OpenApi 10.0.15 | T2-02 ✅ |
| `Dockerfile` | Imagen de la API sola, para Docker Compose local | T2-01 ✅ |
| `appsettings.json` | Configuración sin secretos: `Estudio:ReservaMinutos` (30) y `Estudio:CodigoAcceso` (vacío) | T2-05 ✅ |
| `appsettings.Development.json` | Registro detallado en desarrollo | T2-01 ✅ |
| `Properties/launchSettings.json` | Perfil de `dotnet run` y Visual Studio | T2-01 ✅ |

### `Contracts/` · lo único que ve la PWA (DTO)

| Archivo | Qué hace | Tarea |
| --- | --- | --- |
| `SesionContracts.cs` | Solicitudes y respuestas del estudiante: `IniciarSesionRequest`, `PistaResponse` («Voz k» y URL, sin revelar la condición), `IniciarSesionResponse`, `EstadoSesionResponse` y los contratos pendientes de participante, eventos y evaluación | T2-02, T2-07 ✅ · evaluación ⏳ T2-08 |
| `AdminContracts.cs` | `CondicionResponse` y `CuotaResponse` del Dashboard | T2-02 ✅ |

### `Controllers/`

| Archivo | Qué hace | Tarea |
| --- | --- | --- |
| `SesionesController.cs` | Endpoints públicos:<br>· `POST /api/sesiones` (consentimiento → orden de voces);<br>· `GET /{id}` (para retomar);<br>· `GET /{id}/pistas/{k}/audio` (MP3 de la posición k, bloqueado hasta terminar la anterior);<br>· `POST /{id}/pistas/{k}/fin` (desbloquea la siguiente y renueva la reserva).<br>`participante`, `eventos` y `evaluacion` responden 501 | T2-05, T2-07 ✅ · resto ⏳ T2-08, T2-10 |
| `AdminController.cs` | `GET /api/admin/condiciones` y `GET /api/admin/cuotas` (por voz escuchada primero), con la cabecera `X-Admin-Key` y el límite de 30 solicitudes por minuto | T2-02, T2-18 ✅ · login 🟡 T2-20 |

### `Data/` · acceso a datos (EF Core)

| Archivo | Qué hace | Tarea |
| --- | --- | --- |
| `MakotoDbContext.cs` | 8 tablas, enums como texto y columnas en snake_case | T2-02 ✅ |
| `MakotoDbContextFactory.cs` | Solo para `dotnet ef migrations add`; no se conecta a ninguna base | T2-02 ✅ |
| `ConnectionStringResolver.cs` | Convierte la URI de Neon (`postgresql://…`) a cadena Npgsql con SSL | T2-02 ✅ |
| `CatalogoCondiciones.cs` | Las 6 celdas 2×3 sembradas (código, género, nivel, F0 objetivo, archivo MP3) | T2-02, T2-03 ✅ |
| `CertificacionAudios.cs` | Lee `certificados/certificacion.json` y verifica que cada MP3 exista y su SHA-256 coincida | T2-04 ✅ |
| `SincronizadorCertificacion.cs` | Al arrancar, copia a `condiciones_vocales` el F0 medido, la duración, la huella y la voz de las celdas válidas. Escribe en el log «Audios certificados: X de 6» | T2-04 ✅ |
| `Configurations/*.cs` | Una configuración por tabla: nombres, claves, índices, restricciones (`naturalidad` 1–7, `escala_min < escala_max`) y siembra. `SesionExperimentalConfiguration` incluye el arreglo `orden_condiciones` | T2-02, T2-07 ✅ |

### `Migrations/` · historial del esquema (generado con `dotnet ef`, no se edita a mano)

| Archivo | Qué hace | Tarea |
| --- | --- | --- |
| `20261124193125_ModeloInicial(.Designer).cs` | Crea las 8 tablas y siembra las 6 condiciones | T2-02 ✅ |
| `20261126193140_OrdenIntraSujeto(.Designer).cs` | Agrega `orden_condiciones integer[]`, `fila_orden`, `pistas_escuchadas` y un índice | T2-07 ✅ |
| `MakotoDbContextModelSnapshot.cs` | Foto del modelo actual que usa EF para calcular la próxima migración | — |

### `Models/` · entidades (tablas)

| Archivo | Tabla | Notas | Tarea |
| --- | --- | --- | --- |
| `CondicionVocal.cs` | `condiciones_vocales` | Una voz: género, nivel, F0 objetivo y medido, archivo, SHA-256 | T2-02 ✅ · D-16 la volverá dinámica |
| `SesionExperimental.cs` | `sesiones_experimentales` | Participación anónima: código `MK-…`, consentimiento, estado, reserva, **orden de voces**, fila del cuadrado latino, pistas escuchadas. `CondicionVocalId` = primera voz | T2-02, T2-07 ✅ |
| `DatosParticipante.cs` | `datos_participante` | Covariables (edad, género, experiencia con asistentes) | ⏳ T2-08 |
| `Evaluacion.cs` | `evaluaciones` | Chequeos de manipulación y naturalidad. **Hoy es una por sesión; T2-08 la pasa a una por voz** | ⏳ T2-08 |
| `InstrumentoItem.cs` | `instrumento_items` | Ítems de PETS y Calidez | ⏳ T2-08 |
| `RespuestaItem.cs` | `respuestas_item` | Respuesta a cada ítem | ⏳ T2-08 |
| `EventoTelemetria.cs` | `eventos_telemetria` | Inicio, fin y repetición del audio, con tiempos | ⏳ T2-10 |
| `Administrador.cs` | `administradores` | Usuario del panel | ⏳ T2-20 |
| `Enums.cs` | — | `GeneroVocal`, `NivelFrecuencia`, `EstadoSesion`, `TipoEvento` | T2-02 ✅ |

### Resto de `Makoto.Api`

| Archivo | Qué hace | Tarea |
| --- | --- | --- |
| `Services/AsignadorCondiciones.cs` | Dentro de una transacción con `pg_advisory_xact_lock`: expira las reservas vencidas, toma las voces certificadas, elige la fila del cuadrado latino menos usada (empate al azar) y crea la sesión con el código `MK-XXXXXXXX`. También valida el código del estudio | T2-05, T2-07 ✅ |
| `Services/CuadradoLatino.cs` | Órdenes de Williams: cada voz por igual en cada posición y cada par A→B por igual | T2-07 ✅ |
| `Security/ClaveAdminTemporalAttribute.cs` | Exige `X-Admin-Key` igual a `Admin:ClaveTemporal`, con comparación de tiempo constante | T2-02 ✅ · se reemplaza en T2-20 |
| `Health/HealthChecks.cs` | `/health` sin base de datos (para que Neon se duerma) y `/health/ready` con PostgreSQL y audios | T2-01 ✅ |

## `backend/Makoto.Tests` · xUnit (no necesitan PostgreSQL)

| Archivo | Qué prueba | Tarea |
| --- | --- | --- |
| `ApiTests.cs` | `/health`, OpenAPI oculto, CORS, clave del panel (401), 501, validación (400), consentimiento (400), código del estudio (403) y cabeceras de seguridad | T2-02, T2-05, T2-18 ✅ |
| `AsignadorCondicionesTests.cs` | Elección de la opción menos ocupada, empates, 600 asignaciones → 100 cada una, formato del código | T2-05 ✅ |
| `CuadradoLatinoTests.cs` | Permutaciones, equilibrio por posición y por pares, 6 filas con primeras voces distintas | T2-07 ✅ |
| `CatalogoCondicionesTests.cs` | 6 celdas, ningún F0 en la banda ambigua de 145–165 Hz, nombres de columna | T2-02 ✅ |
| `CertificacionAudiosTests.cs` | Archivo faltante, huella alterada, intento de salir de la carpeta (`../`) | T2-04 ✅ |
| `ConnectionStringResolverTests.cs` | Conversión de la URI de Neon | T2-02 ✅ |
| `Makoto.Tests.csproj` | xUnit y `Microsoft.AspNetCore.Mvc.Testing` | T2-01 ✅ |

## `frontend/makoto-pwa` · React 19 + Vite 8 (PWA)

| Archivo | Qué hace | Tarea |
| --- | --- | --- |
| `package.json`, `package-lock.json` | Dependencias (React, react-router-dom, vite-plugin-pwa, oxlint). Node 22 | T2-02 ✅ |
| `vite.config.js` | PWA: guarda solo la interfaz, **nunca** `/api` ni audios | T2-02 ✅ |
| `.env.development` | `VITE_API_URL=http://localhost:8080` para `npm run dev` | T2-02 ✅ |
| `.env.production` | `VITE_API_URL=` vacío (mismo origen en Render) | T2-18 ✅ |
| `.oxlintrc.json` | Reglas del lint | T2-02 ✅ |
| `index.html`, `public/favicon.svg` | Página base e ícono | T2-02 ✅ |
| `README.md` | README de la plantilla de Vite (sin contenido propio) | 🧊 |
| `src/main.jsx` | Monta React con el enrutador | T2-02 ✅ |
| `src/App.jsx` | Barra «Makoto · Estudio de voz y empatía» y rutas: `/` Experimento, `/test`, `/admin` | T2-02, T2-07 ✅ |
| `src/index.css` | Colores (los mismos tokens del Figma), tarjetas, burbujas, barra de progreso y reproductor. Modo oscuro | T2-02, T2-07 ✅ |
| `src/services/api.js` | **Único** lugar que llama a la API: `sesiones.iniciar`, `obtener`, `finPista`, `urlApi`, `admin.*` y `ApiError` (ProblemDetails) | T2-02, T2-07 ✅ |
| `src/modules/experimento/ExperimentoView.jsx` | Flujo del participante (Figma P1–P3):<br>1. consentimiento;<br>2. prueba de audio (tono Web Audio) y situación;<br>3. escucha de cada voz;<br>4. evaluación (aviso hasta T2-08);<br>5. fin.<br>Retoma la sesión de la pestaña. Lee `?codigo=` | T2-07 ✅ · P4–P6 ⏳ T2-08 |
| `src/modules/experimento/ReproductorBloqueado.jsx` | Sin controles nativos; impide adelantar y cambiar la velocidad; quita los controles de la pantalla de bloqueo; avisa al terminar | T2-07 ✅ |
| `src/modules/experimento/consentimiento.js` | Texto del consentimiento `v2` (varias voces, anónimo, uso estadístico, sin divulgación, mayores de 18) | D-07 ✅ |
| `src/modules/experimento/estimulo.js` | Mensaje del estudiante (`ESCENARIO`) que se muestra en pantalla | D-05 ✅ |
| `src/modules/test/TestView.jsx` | Marcador de posición del Test | ⏳ T2-08 |
| `src/modules/dashboard/DashboardView.jsx` | Panel actual: clave temporal y tablas de cuotas y condiciones. Se rehace con las 4 tarjetas del Figma | 🟡 T2-12, T2-20, D-16 |

## `datos/audios`

| Ruta | Qué contiene | Tarea |
| --- | --- | --- |
| `certificados/*.mp3` | Las 6 voces certificadas. **Se versionan** porque viajan dentro de la imagen de Render | T2-04 ✅ · hay que regenerarlas con el guion definitivo |
| `certificados/certificacion.json` | F0, duración, intensidad y SHA-256 de cada MP3 (la API lo verifica al arrancar) | T2-04 ✅ |
| `crudos/*.wav` | Salida de Azure sin normalizar. **No se versionan** | T2-03 |
| `crudos/sintesis.json` | Registro de la síntesis: voz, SSML aplicado, intentos y F0 medido | T2-03 ✅ |

## `tools/`

| Archivo | Qué hace | Tarea |
| --- | --- | --- |
| `audio-pipeline/sintetizar.py` | `voces` y `generar`: SSML con `<prosody pitch rate>` para Azure, mide el F0 con Praat y recalibra hasta ±5 Hz | T2-03 ✅ · D-16 lo llevará al panel |
| `audio-pipeline/certificar.py` | Normaliza a 70 dB, convierte a MP3 de 128 kbps, mide sobre el MP3 final, comprueba duraciones (±1 s) y escribe las huellas | T2-04 ✅ |
| `audio-pipeline/config.json` | Voces es-EC, velocidad por voz (−7 % / +7 %), tolerancias y las 6 celdas con su F0 objetivo | T2-03, T2-04 ✅ |
| `audio-pipeline/guion.txt` | Guion definitivo de 97 palabras, sin marcas de género | D-05 ✅ |
| `audio-pipeline/requirements.txt` | requests, praat-parselmouth, imageio-ffmpeg, pytest (versiones fijas) | T2-03 ✅ |
| `audio-pipeline/tests/` | 18 pruebas con Azure simulado y Praat real | T2-03, T2-04 ✅ |
| `audio-pipeline/README.md` | Cómo usar el pipeline | T2-03 ✅ |
| `revision/prueba-asignador.ps1` | N sesiones simultáneas, balance de cuotas y descarga del audio de la pista 1 | T2-06, T2-07 ✅ |
| `revision/limpiar-pruebas.sql` | Borra las sesiones de prueba (`prueba-rev-tec-03`, `ci`) | T2-06 ✅ |
| `verificar.ps1` | Reemplaza a la CI:<br>1. secretos;<br>2. `dotnet test`;<br>3. lint y compilación de la PWA;<br>4. `pytest`;<br>5. Docker Compose desde cero con 60 sesiones simultáneas;<br>6. imagen de Render | D-14 ✅ |

## `docs/`

| Archivo | Qué contiene |
| --- | --- |
| `README.md` | Índice de la documentación |
| `MAPA_DEL_PROYECTO.md` | Este archivo |
| `decisiones.md` | Decisiones D-01 a D-16 (intra-sujeto, instrumentos, ética, hosting, herramienta automática…), con el escenario y el guion |
| `instrumentos.md` | PETS (10 ítems, de 0 a 100), Calidez (4 ítems, de 1 a 7), chequeos de manipulación y covariables: especificación de T2-08 |
| `revisiones/REV-TEC-03.md` | Guion y evidencia de la revisión del Asignador (T2-06) |
| `pruebas/T2-19-moviles.md` | Matriz de compatibilidad del reproductor en celulares y navegadores |
| `Planificacion_Scrum_Makoto_Tutores-v6.xlsx` | Planificación Scrum (en su copia local) |

Diseño de interfaces: Figma «Makoto · Interfaces», https://www.figma.com/design/BnRPGTBOFFFJMNFhBsl04p (P1–P6 del participante, A0–A5 del administrador).

## Qué cambiará en las próximas tareas

| Tarea | Archivos que se tocarán |
| --- | --- |
| T2-08 Test | `Evaluacion` pasa a una por voz (migración nueva), siembra de `instrumento_items`; `SesionesController` (participante, evaluación, comparación); `TestView` → componentes P4 y P5 en `experimento/` |
| T2-10 Telemetría | `SesionesController.Eventos` y `ReproductorBloqueado` (enviar inicio, fin y pausas) |
| T2-12 y T2-20 Panel | `DashboardView` → A0 y A1 con las tarjetas; `Administrador` y login JWT; exportación CSV |
| D-16 Generación automática | `tools/audio-pipeline` al servidor (Python en `Dockerfile.render`), audios en Neon, `CondicionVocal` dinámica, pantallas A2–A4 |
