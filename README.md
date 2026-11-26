# Makoto · plataforma experimental

Aplicación para evaluar cómo el género y la frecuencia fundamental (F0) de la voz de un chatbot de apoyo emocional influyen en la empatía que perciben estudiantes universitarios (diseño factorial 2 × 3). Universidad de las Fuerzas Armadas ESPE · Ingeniería en TIC.

> Estado: **T2-07 · Experimento intra-sujeto con reproductor bloqueado** (sigue T2-19 · pruebas en móviles y T2-08 · Test). Ya están el modelo de datos (T2-02), los 6 audios sintetizados y certificados (T2-03 y T2-04), el Asignador (T2-05), Neon (T2-17) y el despliegue en Render (T2-18). Cada participante escucha todas las voces en orden contrabalanceado (D-02). Faltan el Test (T2-08), la Telemetría (T2-10) y el panel con generación automática (D-16).

## Estructura

```text
makoto-experimental/
├── Makoto.slnx                 # Solución (.NET 10, Visual Studio 2026)
├── global.json                 # Fija el SDK de .NET 10
├── docker-compose.yml          # Entorno local: postgres (:5432) + backend (:8080)
├── Dockerfile.render           # Imagen única para Render: PWA compilada dentro de la API (T2-18)
├── render.yaml                 # Blueprint de Render, sin secretos (T2-18)
├── .env.example                # Variables locales (copiar como .env)
├── backend/
│   ├── Makoto.Api/             # API ASP.NET Core 10
│   │   ├── Program.cs          # Arranque: puerto, base de datos, CORS, salud
│   │   ├── Models/             # Entidades: las 8 tablas del modelo E-R
│   │   ├── Data/               # MakotoDbContext, configuraciones, catálogo 2×3, cadena de conexión
│   │   ├── Migrations/         # Migraciones de EF Core (se generan con dotnet ef)
│   │   ├── Contracts/          # DTO: lo único que ve la PWA
│   │   ├── Controllers/        # SesionesController (público) y AdminController (clave temporal)
│   │   ├── Services/           # AsignadorCondiciones: elige la celda 2×3 de cada participante (T2-05)
│   │   ├── Security/           # Clave temporal del panel (hasta el JWT de T2-20)
│   │   ├── Health/             # Comprobaciones de PostgreSQL y audios
│   │   └── Dockerfile          # Imagen multi-etapa; se construye desde la raíz
│   └── Makoto.Tests/           # Pruebas xUnit
├── frontend/makoto-pwa/        # React PWA (Vite): experimento, test y dashboard
├── datos/audios/certificados/  # Los 6 audios certificados (T2-03 y T2-04)
├── tools/audio-pipeline/       # Síntesis de los 6 audios con Azure Speech + medición de F0 con Praat (T2-03)
├── tools/revision/             # Script de concurrencia y limpieza para las revisiones técnicas (T2-06)
├── tools/verificar.ps1         # Verificación local completa (reemplaza a GitHub Actions, D-14)
├── .githooks/pre-commit        # Bloquea commits con .env o contraseñas (D-14)
├── CLAUDE.md                   # Contexto y reglas para Claude Code
├── docs/                       # Mapa del proyecto, decisiones, instrumentos, pruebas y guiones de revisión
└── .github/workflows/ci.yml    # CI de GitHub (deshabilitada por ahora: ver «Verificación sin GitHub Actions»)
```

> Detalle de **cada** archivo, su tarea y su estado: [docs/MAPA_DEL_PROYECTO.md](docs/MAPA_DEL_PROYECTO.md).

## Requisitos

- Docker Desktop
- .NET SDK 10 (incluido en Visual Studio 2026) y la herramienta `dotnet-ef`
- Node.js 22 (para la PWA)
- Git

## Ejecutar con Docker Compose

```bash
cp .env.example .env          # en Windows: copy .env.example .env  (cambie la contraseña)
docker compose up --build
```

| URL | Qué responde |
| --- | --- |
| http://localhost:8080/health | `Healthy` si la API está viva. No consulta la base de datos. |
| http://localhost:8080/health/ready | Estado de PostgreSQL y de la carpeta de audios, en JSON. |
| http://localhost:8080/openapi/v1.json | Contrato OpenAPI (solo en desarrollo). |

Para borrar también los datos locales: `docker compose down -v`.

## Base de datos y migraciones

Las tablas se crean al arrancar la API con `MigrateAsync()` a partir de las migraciones guardadas en `Migrations/`. Cuando cambie el modelo:

```bash
dotnet tool install --global dotnet-ef        # una sola vez
cd backend/Makoto.Api
dotnet ef migrations add <NombreDelCambio>    # genera la migración; revísela y súbala a Git
```

Tablas (snake_case): `condiciones_vocales`, `sesiones_experimentales`, `datos_participante`, `eventos_telemetria`, `evaluaciones`, `instrumento_items`, `respuestas_item`, `administradores`. Las 6 condiciones 2×3 se siembran en la migración (`Data/CatalogoCondiciones.cs`).

## Contrato REST

| Método y ruta | Quién lo usa | Estado |
| --- | --- | --- |
| `POST /api/sesiones` | Experimento | Implementado (T2-05, T2-07) · asigna un orden contrabalanceado de todas las voces certificadas |
| `GET /api/sesiones/{id}` | Experimento y Test | Implementado · pistas y cuántas escuchó (para retomar al recargar) |
| `GET /api/sesiones/{id}/pistas/{indice}/audio` | Reproductor | Implementado (T2-07) · MP3 de la voz en esa posición; solo la siguiente a las ya escuchadas |
| `POST /api/sesiones/{id}/pistas/{indice}/fin` | Reproductor | Implementado (T2-07) · la pista terminó: desbloquea la siguiente y renueva la reserva |
| `POST /api/sesiones/{id}/participante` | Test | Contrato · T2-08 |
| `POST /api/sesiones/{id}/eventos` | Reproductor | Contrato · T2-10 |
| `POST /api/sesiones/{id}/evaluacion` | Test | Contrato · T2-08 |
| `GET /api/admin/condiciones` | Dashboard | Implementado · cabecera `X-Admin-Key` |
| `GET /api/admin/cuotas` | Dashboard | Implementado · cabecera `X-Admin-Key` |

Los endpoints pendientes responden `501` con un ProblemDetails que indica la tarea.

Respuestas de `POST /api/sesiones`:
- `201` con `sesionId`, `estado`, `pistas` (`indice`, `etiqueta` «Voz k», `audioUrl`) y `pistasEscuchadas`;
- `400` sin consentimiento;
- `403` si el código del estudio falta o es incorrecto;
- `503` si no hay audios certificados.

Respuestas de `GET …/pistas/{indice}/audio`:
- `200 audio/mpeg`;
- `404` si la sesión o la pista no existen;
- `409` si la sesión terminó o expiró, o si la pista está bloqueada porque falta terminar la anterior;
- `503` si el audio no está certificado.

## Experimento intra-sujeto (T2-07 · D-02)

Cada participante escucha **todas** las voces certificadas, en el orden de una fila de un **cuadrado latino de Williams** (`Services/CuadradoLatino.cs`):
- cada voz aparece por igual en cada posición;
- cada par «voz A seguida de voz B» aparece el mismo número de veces, lo que controla el efecto de arrastre;
- con 6 voces hay 6 órdenes; con un número impar de voces, el doble.

El Asignador de T2-05 elige la fila menos usada (empates al azar), con el mismo candado y la misma expiración. `condicion_vocal_id` guarda la **primera** voz del orden; por eso las cuotas del Dashboard siguen balanceadas (cuentan la voz escuchada primero).

El flujo del participante sigue el Figma (P1–P3):
1. consentimiento;
2. prueba de audio y situación;
3. escucha de cada voz;
4. evaluación de cada voz (T2-08).

El **reproductor bloqueado** (`ReproductorBloqueado.jsx`):
- no muestra controles nativos;
- si algo intenta adelantar, vuelve al punto máximo escuchado (retroceder sí se permite);
- si algo cambia la velocidad, la devuelve a 1×;
- quita «adelantar» y «siguiente» de la pantalla de bloqueo del celular;
- activa «Evaluar esta voz» solo al llegar al final.

El servidor también lo impide: no entrega una pista hasta que la anterior se marcó como terminada. Si el estudiante recarga la página, continúa en la misma voz (la sesión se guarda solo en esa pestaña).

Pruebas en celulares y navegadores (T2-19): matriz de casos y planes B en [docs/pruebas/T2-19-moviles.md](docs/pruebas/T2-19-moviles.md).

Migración de este cambio (columnas `orden_condiciones integer[]`, `fila_orden`, `pistas_escuchadas`; ya generada como `20261126193140_OrdenIntraSujeto`):

```bash
cd backend/Makoto.Api
dotnet ef migrations add OrdenIntraSujeto
```

## Asignador de condiciones (T2-05)

Cada participante recibe una de las 6 celdas (género × frecuencia) sin poder elegirla:

1. Se toma un candado de PostgreSQL (`pg_advisory_xact_lock`) para que dos estudiantes que entran al mismo tiempo no ocupen la misma plaza.
2. Las sesiones abandonadas (sin terminar tras `Estudio:ReservaMinutos`, 30 min por defecto) pasan a `Expirada` y liberan su plaza.
3. Se cuenta la ocupación de cada celda certificada: sesiones completadas más las que están en curso.
4. Se elige la celda menos ocupada; si hay empate, al azar.
5. Se crea la sesión con un código de participante `MK-XXXXXXXX` generado en el servidor (sin datos personales).

Solo se asignan celdas cuyo audio pasó la certificación de T2-04. La CI comprueba que 60 sesiones simultáneas quedan 10 por celda.

## Probar la API desde Windows

PowerShell no interpreta las comillas `\"` como `cmd`, por eso el `curl.exe` copiado de `cmd` le envía un JSON roto (error `'\' is an invalid start of a property name`). En PowerShell use `Invoke-RestMethod`:

```powershell
$r = Invoke-RestMethod -Method Post -Uri http://localhost:8080/api/sesiones -ContentType "application/json" -Body '{"consentimientoAceptado":true,"consentimientoVersion":"v1"}'
$r                                                   # sesionId, estado, audioUrl
Start-Process ("http://localhost:8080" + $r.audioUrl) # escucha el audio asignado
Invoke-RestMethod -Uri http://localhost:8080/api/admin/cuotas -Headers @{ "X-Admin-Key" = "<ADMIN_CLAVE_TEMPORAL del .env>" }
```

En `cmd`:

```bat
curl.exe -X POST -H "Content-Type: application/json" -d "{\"consentimientoAceptado\":true,\"consentimientoVersion\":\"v1\"}" http://localhost:8080/api/sesiones
```

Si `ESTUDIO_CODIGO_ACCESO` tiene un valor en el `.env`, agregue `"codigoAcceso":"<ese valor>"` al JSON; si no, la API responde `403`.

## Probar contra Neon y prueba de concurrencia (T2-06 · REV-TEC 03)

Guion completo de la revisión: [docs/revisiones/REV-TEC-03.md](docs/revisiones/REV-TEC-03.md).

1. En el `.env`, defina `NEON_DATABASE_URL` con la URI de la rama **desarrollo** de Neon. Vacío = PostgreSQL local.
2. `docker compose up --build`: la API aplica la migración en Neon y sincroniza la certificación de los audios.
3. Envíe 30 sesiones simultáneas y compruebe el balanceo y el audio:

```powershell
powershell -ExecutionPolicy Bypass -File tools\revision\prueba-asignador.ps1 -ClaveAdmin <ADMIN_CLAVE_TEMPORAL> -Sesiones 30
```

4. Borre las sesiones de prueba con `tools/revision/limpiar-pruebas.sql` (SQL Editor de Neon o psql) y vacíe de nuevo `NEON_DATABASE_URL`.

El script funciona en Windows PowerShell 5.1 y en PowerShell 7. Envía las solicitudes en paralelo con `HttpClient` y sube el límite de 2 conexiones por servidor que trae .NET Framework; sin ese cambio, las solicitudes saldrían de dos en dos y no se probaría la concurrencia. Las sesiones de prueba usan `consentimientoVersion = "prueba-rev-tec-03"` (y las de la CI, `"ci"`) para poder separarlas del estudio real.

## Despliegue en Render (T2-18)

Un solo servicio web de Render (Docker) sirve la API y la PWA desde el mismo dominio HTTPS:

```text
Estudiante ──HTTPS──▶ Render (TLS) ──▶ contenedor: ASP.NET Core
                                         ├── /            PWA (wwwroot, compilada en Dockerfile.render)
                                         ├── /api/...     API REST
                                         └── datos/audios/certificados (6 MP3 dentro de la imagen)
                                                │ SSL (sslmode=require)
                                                ▼
                                         Neon · rama «producción»
```

Al ser el mismo origen, no hace falta CORS ni escribir la URL de la API en la PWA (`VITE_API_URL` queda vacío).

### Primer despliegue

1. **Neon:** en la rama **producción**, Connect → *Pooled connection* → copie la URI.
2. **Render** (los cambios de T2-18 deben estar ya en `main` en GitHub, porque Render lee `render.yaml` desde allí):
   1. Entre a dashboard.render.com. Si no lo hizo antes, conecte GitHub: Account Settings → Git → GitHub → Connect y dé acceso al repositorio `makoto-experimental`.
   2. Pulse **New +** (arriba a la derecha) → **Blueprint**.
   3. Elija el repositorio `MezaJose12/makoto-experimental` → **Connect**.
   4. Escriba el nombre del Blueprint (`makoto`), deje la rama `main` y la ruta `render.yaml`.
   5. Render muestra el servicio `makoto-experimental` (Docker, Free, Ohio) y pide las variables con `sync: false`:
      - `DATABASE_URL`: la URI de producción del paso 1.
      - `Estudio__CodigoAcceso`: un código para los participantes, por ejemplo de 8 o más caracteres sin espacios.
      - `Admin__ClaveTemporal` no se pide: Render la genera. Después la verá en el servicio → **Environment** → ojo junto al valor.
   6. **Deploy Blueprint**. El primer despliegue tarda unos 5–10 min (compila la PWA y .NET). Siga el progreso en el servicio → **Logs**.
   7. Si el nombre `makoto-experimental` ya está ocupado, Render agrega un sufijo a la URL. La URL final aparece arriba en el servicio.
   8. Para cambiar una variable después: servicio → Environment → Edit → Save, rebuild and deploy. No la edite en `render.yaml`.
3. Espere a que termine el despliegue. El log debe mostrar la migración aplicada y «Audios certificados: 6 de 6», sin avisos de `Estudio:CodigoAcceso` ni de `Admin:ClaveTemporal`.
4. Compruebe:
   - `https://<servicio>.onrender.com/health` → `Healthy`
   - `https://<servicio>.onrender.com/health/ready` → `postgresql` y `audios` en `Healthy`
   - `https://<servicio>.onrender.com/openapi/v1.json` → `404` (oculto en producción)
   - `https://<servicio>.onrender.com/?codigo=<código>` → consentimiento → sesión creada
5. Cuando funcione, borre en Render el Static Site y el Web Service anteriores (los de T2-02), para no dejar copias desactualizadas expuestas.

El enlace para los participantes (o su QR) es `https://<servicio>.onrender.com/?codigo=<Estudio__CodigoAcceso>`. Sin el código, la API responde `403` y nadie ajeno al laboratorio puede llenar las cuotas.

Render **no despliega solo** (`autoDeployTrigger: off`), porque sin GitHub Actions nadie comprueba el código antes. Para desplegar: `tools\verificar.ps1` en verde → `git push` → Render → **Manual Deploy → Deploy latest commit**. Así un push con errores nunca llega a la URL que usan los participantes.

Probar la imagen de Render en su equipo, contra el PostgreSQL local:

```powershell
docker build -f Dockerfile.render -t makoto-render .
docker run --rm -p 8081:8080 --network makoto_default -e Admin__ClaveTemporal=clave-local-larga-de-prueba-123 -e ConnectionStrings__Default="Host=postgres;Port=5432;Database=makoto;Username=makoto;Password=<la del .env>" makoto-render
# http://localhost:8081  (sin HTTPS en local; HSTS solo se activa detrás de Render)
```

## Seguridad de claves y accesos

| Qué | Dónde vive | Nunca en |
| --- | --- | --- |
| URI de Neon (usuario y contraseña) | `.env` local (rama desarrollo) y Render → Environment (rama producción) | `.env.example`, `appsettings*.json`, `render.yaml`, código, capturas |
| Clave del Dashboard | `.env` local y Render (generada aleatoria) | PWA, código, Git |
| Código del estudio | Render y el enlace o QR que se entrega en el laboratorio | Código de la PWA |
| Contraseña del PostgreSQL local | `.env` | Git |

Medidas en el código:
- **Git:** `.gitignore` ignora `.env` y cualquier variante `.env.*`, salvo la plantilla `.env.example`, que solo lleva valores de ejemplo.
- **Imagen:** `.dockerignore` impide que cualquier `.env`, `.git`, los audios crudos o `tools/` entren en la imagen. `Dockerfile.render` no recibe ningún secreto: llegan como variables de entorno en tiempo de ejecución.
- **Front-end:** todo lo que empieza por `VITE_` es público (queda en el JavaScript). La PWA no lleva claves; el código del estudio llega en el enlace.
- **API en producción:**
  - OpenAPI oculto y errores sin detalles internos.
  - HSTS y cabeceras de seguridad (CSP, `nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy`).
  - Panel limitado a `Seguridad:AdminSolicitudesPorMinuto` solicitudes por minuto y por IP (30 por defecto).
  - Avisos en el log si el código del estudio o la clave del panel faltan.
- **Base de datos:** conexión con SSL obligatorio. `/health` no la consulta, y `/health/ready` solo devuelve estados, nunca mensajes ni cadenas.

**Si una clave llega a GitHub** (aunque luego se borre, sigue en el historial):
1. Cámbiela de inmediato. En Neon: Branches → la rama → Roles → `neondb_owner` → *Reset password*. Hágalo en cada rama que comparta esa contraseña, porque una rama creada a partir de otra hereda sus contraseñas.
2. Actualice la URI nueva en su `.env` y en Render.
3. Quite el valor del archivo y haga commit. El valor antiguo ya no sirve, así que no hace falta reescribir el historial, aunque puede hacerlo con `git filter-repo`.
4. Revise GitHub → Security → Secret scanning por si GitHub detectó el valor.

Buscar secretos en todo el historial antes de cada revisión:

```powershell
git log -p --all | Select-String -Pattern "npg_|neon\.tech|ClaveTemporal=|PASSWORD=" | Select-Object -First 20
```

Solo deben aparecer valores de ejemplo (`cambie_esta_clave…`) o vacíos.

## Audios: síntesis y certificación (T2-03 y T2-04)

Se ejecuta desde `tools/audio-pipeline` (necesita Python 3.12 y una clave de Azure Speech):

```bat
cd tools\audio-pipeline
python -m venv .venv
.venv\Scripts\activate
pip install -r requirements.txt
python -m pytest -q tests

set AZURE_SPEECH_KEY=su_clave
set AZURE_SPEECH_REGION=su_region
python sintetizar.py voces       # comprueba que es-EC-AndreaNeural y es-EC-LuisNeural existen
python sintetizar.py generar     # datos/audios/crudos/*.wav + sintesis.json
python certificar.py             # datos/audios/certificados/*.mp3 + certificacion.json
```

- En `cmd`, `set VARIABLE=valor` va sin espacios alrededor del `=` y sin comillas: `set AZURE_SPEECH_REGION="brazilsouth"` guarda las comillas y la URL falla con `%22brazilsouth%22`. `sintetizar.py` ya limpia comillas y espacios, pero conviene escribirlo bien. En PowerShell: `$env:AZURE_SPEECH_KEY="su_clave"`.
- Ejecute `activate` y `pytest` en líneas separadas y siempre desde `tools/audio-pipeline` (desde otra carpeta pytest no encuentra `tests`).
- El F0 se calibra por iteraciones: se sintetiza con `<prosody pitch>`, se mide con Praat y se corrige hasta quedar a ±5 Hz del objetivo.
- La certificación iguala la intensidad a 70 dB, exige que las 6 pistas duren lo mismo (±1 s) y guarda el SHA-256 de cada MP3. La API recalcula las huellas al arrancar y solo asigna las celdas que coinciden.
- Luis habla más lento que Andrea (37,25 s frente a 32,21 s con el mismo guion), por eso `config.json` usa una velocidad por voz: Femenino `-7%` y Masculino `+7%`.
- El guion definitivo (D-05, 25/11) y sus criterios están en [docs/decisiones.md](docs/decisiones.md). Cada vez que cambie `guion.txt`, vuelva a ejecutar `generar` y `certificar`, suba los 6 MP3 nuevos y `certificacion.json`, y vuelva a desplegar en Render. La API solo asigna las celdas cuya huella coincide.

Resultado aprobado (25/11/2026, con el guion borrador de 87 palabras; se repite con el guion definitivo):

| Celda | Objetivo | F0 medido | Duración |
| --- | --- | --- | --- |
| F_GRAVE | 175 Hz | 173,13 Hz | 34,63 s |
| F_NEUTRA | 210 Hz | 207,89 Hz | 34,63 s |
| F_AGUDA | 255 Hz | 253,46 Hz | 34,65 s |
| M_GRAVE | 100 Hz | 101,53 Hz | 34,80 s |
| M_NEUTRA | 118 Hz | 116,71 Hz | 34,82 s |
| M_AGUDA | 138 Hz | 137,52 Hz | 34,82 s |

Ningún F0 cae en la banda ambigua de 145–165 Hz. El contrato completo está en http://localhost:8080/openapi/v1.json (solo en desarrollo).

## PWA

```bash
cd frontend/makoto-pwa
npm install
npm run dev        # http://localhost:5173, conectada a la API local
```

## Ejecutar sin Docker (Visual Studio o `dotnet run`)

La API necesita una base de datos. Use una de estas dos opciones, guardadas como secreto de usuario (nunca en `appsettings.json`):

```bash
cd backend/Makoto.Api
# Opción A: el PostgreSQL de Docker Compose
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=makoto;Username=makoto;Password=<la del .env>"
# Opción B: Neon (rama de desarrollo)
dotnet user-secrets set "ConnectionStrings:Default" "<cadena de Neon en formato Npgsql>"
dotnet run
```

También puede definir la variable de entorno `DATABASE_URL` con la URI que entrega Neon (`postgresql://…`); tiene prioridad sobre `ConnectionStrings:Default`.

## Configuración

| Variable | Uso | Ejemplo |
| --- | --- | --- |
| `DATABASE_URL` | URI de PostgreSQL (Neon). Se convierte a Npgsql con SSL. | `postgresql://usuario:clave@host/neondb?sslmode=require` |
| `NEON_DATABASE_URL` (solo `.env`) | Docker Compose la pasa como `DATABASE_URL`, para probar contra la rama desarrollo de Neon | (secreto) |
| `ConnectionStrings__Default` | Cadena de Npgsql, si no hay `DATABASE_URL` | `Host=postgres;Port=5432;…` |
| `Audios__Ruta` | Carpeta de audios | `/app/datos/audios` |
| `Cors__Origenes__0` | Origen permitido de la PWA | `http://localhost:5173` |
| `PORT` | Puerto, si la plataforma lo define (Render) | `8080` |
| `Admin__ClaveTemporal` | Clave del Dashboard hasta el JWT (T2-20) | (secreto) |
| `Estudio__CodigoAcceso` | Código que se entrega a los participantes; vacío = sin código | (secreto) |
| `Estudio__ReservaMinutos` | Minutos antes de liberar la plaza de una sesión abandonada | `30` |
| `Database__AplicarMigraciones` | Aplica migraciones al arrancar (por defecto `true`) | `true` |
| `Seguridad__AdminSolicitudesPorMinuto` | Límite de solicitudes al panel por IP y minuto (`429` al superarlo) | `30` |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | Lo fija `Dockerfile.render`: la API reconoce el HTTPS y la IP real detrás del proxy de Render | `true` |

## Verificación sin GitHub Actions (D-14)

GitHub Actions está deshabilitado en la cuenta universitaria, así que la CI no corre en GitHub. Hasta resolverlo, la misma verificación se ejecuta en local:

```powershell
git config core.hooksPath .githooks                                     # una vez por clon: control de secretos en cada commit
powershell -ExecutionPolicy Bypass -File tools\verificar.ps1            # antes de cada push y de cada despliegue
powershell -ExecutionPolicy Bypass -File tools\verificar.ps1 -SinDocker # versión rápida (sin Docker)
```

`verificar.ps1` revisa:
1. que no haya secretos en los archivos versionados;
2. `dotnet test`;
3. lint y compilación de la PWA;
4. `pytest` del pipeline de audio;
5. Docker Compose desde cero, en un proyecto aparte que no borra sus datos locales: salud, 6 condiciones, 60 sesiones simultáneas → 10 por celda y audio MP3;
6. la compilación de `Dockerfile.render`.

Termina con un resumen y «Todo en verde» o «NO suba ni despliegue». Antes del paso 5, detenga su entorno (`docker compose stop`), porque usa el puerto 8080.

El hook `pre-commit` rechaza el commit si incluye `.env`, certificados (`.pem`, `.pfx`, `.key`) o una URI o contraseña de base de datos. Los ejemplos `usuario:clave@` sí se permiten.

Guarde la salida de `verificar.ps1` (captura o `... | Tee-Object verificacion.txt`) como evidencia en cada revisión técnica, en lugar de la pestaña Actions.

## Pruebas

```bash
dotnet test Makoto.slnx
```

Las pruebas no necesitan PostgreSQL: comprueban la conversión de la cadena de conexión, el catálogo 2×3 (ningún F0 en la banda ambigua), los nombres de columna, `/health` sin base de datos, OpenAPI oculto en producción, CORS, la clave del panel, los códigos 400, 403 y 501, la verificación de huellas de los audios y las reglas del Asignador (celda menos ocupada, empates al azar, 600 asignaciones → 100 por celda, formato del código de participante). La concurrencia real con PostgreSQL se prueba en la CI.

Para revisar dependencias vulnerables: `dotnet list package --vulnerable --include-transitive`.

## Verificación de REV-TEC 01

- [ ] `docker compose up --build` levanta `postgres` y `backend` sin errores.
- [ ] `GET /health` responde `200 Healthy`.
- [ ] `GET /health/ready` responde `200` con `postgresql` y `audios` en `Healthy`.
- [ ] `dotnet test` pasa todas las pruebas.
- [ ] La CI termina en verde (mientras Actions esté deshabilitado: `tools\verificar.ps1` en verde).
- [ ] Ningún secreto en el repositorio (`.env` y contraseñas fuera de Git).

## Verificación de REV-TEC 02

- [ ] La migración crea las 8 tablas y siembra las 6 condiciones (en local y en Neon).
- [ ] `GET /api/admin/condiciones` con `X-Admin-Key` devuelve las 6 celdas; sin la clave, `401`.
- [ ] `/openapi/v1.json` muestra todos los endpoints con sus esquemas.
- [ ] La PWA (`npm run dev`) muestra las cuotas en el Dashboard y el aviso 501 en el Experimento.
- [ ] La CI termina en verde: back-end, PWA y Docker Compose (o `tools\verificar.ps1`).

## Verificación de REV-TEC 03

- [ ] Los 6 audios están en `datos/audios/certificados/` y `/health/ready` informa «Audios certificados: 6 de 6» en el log.
- [ ] `POST /api/sesiones` responde `201` con `sesionId` y `audioUrl`, y el audio se reproduce.
- [ ] Sin consentimiento responde `400`; con un código de estudio incorrecto, `403`.
- [ ] El Dashboard muestra la sesión «en curso» en su celda.
- [ ] 60 sesiones simultáneas → 10 por celda (`tools\verificar.ps1`, paso 5).

## Problemas frecuentes

| Síntoma | Causa y solución |
| --- | --- |
| `dotnet ef migrations add` dice que no puede crear el DbContext | Falta la cadena en tiempo de diseño. La resuelve `Data/MakotoDbContextFactory.cs`; ejecute el comando desde `backend/Makoto.Api`. |
| Docker: `relation "administradores" already exists` | El volumen tiene tablas de una migración anterior o hay dos migraciones iniciales. `docker compose down -v` (si persiste, `docker volume rm makoto_makoto_pgdata`) y deje una sola migración en `Migrations/`. |
| Log: `relation "__EFMigrationsHistory" does not exist` | Normal en el primer arranque: EF Core la crea en ese momento. |
| La PWA dice «página no encontrada» | La PWA corre en http://localhost:5173 con `npm run dev`; el puerto 8080 es solo la API. |
| Advertencia NU1903 (vulnerabilidad transitiva) | Use las versiones fijas del `.csproj` (Design 10.0.15, Npgsql EF 10.0.4), no `10.0.*`. |
| `git push`: `'origin' does not appear to be a git repository` | `git remote add origin https://github.com/MezaJose12/makoto-experimental.git` y luego `git push -u origin main`. |
| `curl.exe` en PowerShell: `'\' is an invalid start of a property name` | Use `Invoke-RestMethod` (ver «Probar la API desde Windows»). |
| Azure: URL con `%22` o `ConnectionError` | Comillas o espacios en `set AZURE_SPEECH_REGION`. Escríbalo sin comillas ni espacios. |
| `pytest`: `file or directory not found: tests` | Ejecútelo desde `tools/audio-pipeline`. |
| Neon o Render: `relation "…" already exists` (SqlState 42P07) al migrar | La rama tiene tablas creadas antes con `EnsureCreatedAsync()` (despliegue de T2-02) y sin historial de migraciones. Pasa en **desarrollo** y también en **producción** en el primer despliegue de T2-18. En el SQL Editor de Neon, con la rama correcta seleccionada: 1) compruebe que no hay datos reales del estudio (`SELECT COUNT(*) FROM sesiones_experimentales;`); 2) `DROP SCHEMA public CASCADE; CREATE SCHEMA public;`; 3) vuelva a desplegar (Render → Manual Deploy → Deploy latest commit) o a arrancar la API. Con datos reales, no borre: exporte primero (`pg_dump`). |
| La primera solicitud a Neon tarda o falla una vez | Arranque en frío de Neon: la base se apaga por inactividad. `EnableRetryOnFailure` reintenta. Antes de una demostración, abra `/health/ready`. |
| PowerShell: «la ejecución de scripts está deshabilitada» | Ejecute el script con `powershell -ExecutionPolicy Bypass -File …`. Solo afecta a esa ejecución. |
| Las cuotas del Dashboard incluyen sesiones de prueba | Ejecute `tools/revision/limpiar-pruebas.sql`. |
| Render: el log avisa «Estudio:CodigoAcceso está vacío» | Defina `Estudio__CodigoAcceso` en Render → Environment y vuelva a desplegar. |
| La PWA en Render dice «El enlace no tiene un código de estudio válido» | Abra la URL con `?codigo=<código>`, el mismo valor de `Estudio__CodigoAcceso`. |
| El panel responde `429 Too Many Requests` | Se superó el límite de intentos por minuto. Espere un minuto. |
| La primera visita a Render tarda unos 50 s | El plan gratuito duerme el servicio tras 15 min sin uso. Ábralo unos minutos antes de cada sesión o demostración, junto con `/health/ready` para despertar Neon. |
| La pestaña Actions de GitHub no ejecuta nada | Actions está deshabilitado en la cuenta universitaria (D-14). Use `tools\verificar.ps1`. |
| `verificar.ps1`: «El puerto 8080 está en uso» | Detenga su entorno con `docker compose stop` y repita. |
| `git commit`: «pre-commit: … puede contener secretos» | El hook detectó un `.env` o una contraseña. Quítelo del commit (`git restore --staged <archivo>`) y guarde el valor solo en `.env` o en Render. |
| La CI falla en «Crear .env de prueba» | `.env.example` debe estar en Git (no en `.gitignore`) y sin valores reales. |
| `dotnet ef`: «No project was found» | El comando se ejecutó fuera de `backend/Makoto.Api`. Desde la raíz del repositorio: `dotnet ef migrations add <Nombre> --project backend/Makoto.Api`. |
| `GET …/pistas/2/audio` responde `409 Pista bloqueada` | Es lo esperado: la voz 2 se desbloquea cuando la PWA avisa que la voz 1 terminó (`POST …/pistas/1/fin`). |
| Tras actualizar a T2-07, la API falla con `column s.orden_condiciones does not exist` | Falta la migración: `dotnet ef migrations add OrdenIntraSujeto` en `backend/Makoto.Api` y volver a arrancar. |
| Certificación rechazada por duración | Ajuste `velocidad` por voz en `config.json` y repita `generar` y `certificar`. |

Para la revisión con el Tutor 2, la misma verificación contra Neon: ver [docs/revisiones/REV-TEC-03.md](docs/revisiones/REV-TEC-03.md), sección 5.

## Verificación de T2-18 (despliegue en Render)

- [ ] `https://<servicio>.onrender.com/health` → `Healthy` y `/health/ready` con `postgresql` y `audios` en `Healthy`, contra la rama producción de Neon.
- [ ] `/openapi/v1.json` → `404` y el panel sin clave → `401`.
- [ ] La PWA con `?codigo=` crea la sesión; sin el código, aviso de enlace incorrecto (`403`).
- [ ] El audio se reproduce desde la URL de Render.
- [ ] En el log de Render no aparecen avisos de seguridad.
- [ ] La búsqueda de secretos en el historial solo encuentra valores de ejemplo.

## Verificación de T2-07 (REV-TEC 04)

- [ ] Migración `OrdenIntraSujeto` aplicada (local y Neon): la API arranca sin errores.
- [ ] `tools\verificar.ps1` en verde, con 60 sesiones → 10 por voz en el paso 5.
- [ ] El flujo consentimiento → prueba de audio → situación → 6 voces funciona en local y en Render.
- [ ] No se puede adelantar ni cambiar la velocidad, y «Evaluar esta voz» se activa solo al terminar.
- [ ] Al recargar la página se retoma la misma voz.
- [ ] `GET …/pistas/3/audio` sin haber terminado la 2 → `409 Pista bloqueada`.
