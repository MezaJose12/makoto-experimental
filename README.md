# Makoto · plataforma experimental

Aplicación para evaluar cómo el género y la frecuencia fundamental (F0) de la voz de un chatbot de apoyo emocional influyen en la empatía que perciben estudiantes universitarios (diseño factorial 2 × 3). Universidad de las Fuerzas Armadas ESPE · Ingeniería en TIC.

> Estado: **T2-02 · Modelo de datos, contrato REST y estructura de la PWA**. Las tablas se crean con migraciones de EF Core; los endpoints del estudiante están definidos en el contrato y su lógica llega en T2-05 (Asignador), T2-08 (Test) y T2-10 (Telemetría).

## Estructura

```text
makoto-experimental/
├── Makoto.slnx                 # Solución (.NET 10, Visual Studio 2026)
├── global.json                 # Fija el SDK de .NET 10
├── docker-compose.yml          # Entorno local: postgres (:5432) + backend (:8080)
├── .env.example                # Variables locales (copiar como .env)
├── backend/
│   ├── Makoto.Api/             # API ASP.NET Core 10
│   │   ├── Program.cs          # Arranque: puerto, base de datos, CORS, salud
│   │   ├── Models/             # Entidades: las 8 tablas del modelo E-R
│   │   ├── Data/               # MakotoDbContext, configuraciones, catálogo 2×3, cadena de conexión
│   │   ├── Migrations/         # Migraciones de EF Core (se generan con dotnet ef)
│   │   ├── Contracts/          # DTO: lo único que ve la PWA
│   │   ├── Controllers/        # SesionesController (público) y AdminController (clave temporal)
│   │   ├── Security/           # Clave temporal del panel (hasta el JWT de T2-20)
│   │   ├── Health/             # Comprobaciones de PostgreSQL y audios
│   │   └── Dockerfile          # Imagen multi-etapa; se construye desde la raíz
│   └── Makoto.Tests/           # Pruebas xUnit
├── frontend/makoto-pwa/        # React PWA (Vite): experimento, test y dashboard
├── datos/audios/certificados/  # Los 6 audios certificados (T2-03 y T2-04)
├── docs/                       # Arquitectura y planificación
└── .github/workflows/ci.yml    # Compilación, pruebas y prueba de Docker Compose
```

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
| `POST /api/sesiones` | Experimento | Contrato · lógica en T2-05 |
| `GET /api/sesiones/{id}` | Experimento y Test | Implementado |
| `GET /api/sesiones/{id}/audio` | Reproductor | Contrato · T2-05 |
| `POST /api/sesiones/{id}/participante` | Test | Contrato · T2-08 |
| `POST /api/sesiones/{id}/eventos` | Reproductor | Contrato · T2-10 |
| `POST /api/sesiones/{id}/evaluacion` | Test | Contrato · T2-08 |
| `GET /api/admin/condiciones` | Dashboard | Implementado · cabecera `X-Admin-Key` |
| `GET /api/admin/cuotas` | Dashboard | Implementado · cabecera `X-Admin-Key` |

Los endpoints pendientes responden `501` con un ProblemDetails que indica la tarea. El contrato completo está en http://localhost:8080/openapi/v1.json (solo en desarrollo).

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
| `ConnectionStrings__Default` | Cadena de Npgsql, si no hay `DATABASE_URL` | `Host=postgres;Port=5432;…` |
| `Audios__Ruta` | Carpeta de audios | `/app/datos/audios` |
| `Cors__Origenes__0` | Origen permitido de la PWA | `http://localhost:5173` |
| `PORT` | Puerto, si la plataforma lo define (Render) | `8080` |
| `Admin__ClaveTemporal` | Clave del Dashboard hasta el JWT (T2-20) | (secreto) |
| `Database__AplicarMigraciones` | Aplica migraciones al arrancar (por defecto `true`) | `true` |

## Pruebas

```bash
dotnet test Makoto.slnx
```

Las pruebas no necesitan PostgreSQL: comprueban la conversión de la cadena de conexión, el catálogo 2×3 (ningún F0 en la banda ambigua), los nombres de columna, `/health` sin base de datos, OpenAPI oculto en producción, CORS, la clave del panel y los códigos 400 y 501.

## Verificación de REV-TEC 01

- [ ] `docker compose up --build` levanta `postgres` y `backend` sin errores.
- [ ] `GET /health` responde `200 Healthy`.
- [ ] `GET /health/ready` responde `200` con `postgresql` y `audios` en `Healthy`.
- [ ] `dotnet test` pasa todas las pruebas.
- [ ] La CI de GitHub (pestaña Actions) termina en verde.
- [ ] Ningún secreto en el repositorio (`.env` y contraseñas fuera de Git).

## Verificación de REV-TEC 02

- [ ] La migración crea las 8 tablas y siembra las 6 condiciones (en local y en Neon).
- [ ] `GET /api/admin/condiciones` con `X-Admin-Key` devuelve las 6 celdas; sin la clave, `401`.
- [ ] `/openapi/v1.json` muestra todos los endpoints con sus esquemas.
- [ ] La PWA (`npm run dev`) muestra las cuotas en el Dashboard y el aviso 501 en el Experimento.
- [ ] La CI de GitHub termina en verde (back-end, PWA y Docker Compose).
