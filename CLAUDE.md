# Makoto · contexto para Claude Code

Plataforma experimental de tesis (ESPE, Ingeniería en TIC). Mide la empatía percibida (PETS y Calidez) de las voces de un asistente de apoyo emocional. Cada participante escucha **todas** las voces, en orden contrabalanceado (D-02).

- **Mapa de archivos y su estado:** `docs/MAPA_DEL_PROYECTO.md`. Léalo antes de cambiar algo y actualícelo en el mismo commit.
- **Decisiones:** `docs/decisiones.md`.
- **Instrumentos:** `docs/instrumentos.md`.
- **Interfaces:** Figma https://www.figma.com/design/BnRPGTBOFFFJMNFhBsl04p

## Reglas de trabajo

1. **Edite sobre el código existente.** Nunca reemplace el proyecto completo ni entregue un zip. Cada bloque nuevo lleva un comentario `// T2-XX:` (o `#`) que explica por qué.
2. **Documente en `README.md`** cada comando, variable, error y solución nuevos («Problemas frecuentes»).
3. **Nunca suba secretos.** `.env` y la URI de Neon van solo en `.env` local o en Render → Environment. `.env.example` lleva solo valores de ejemplo. El hook `.githooks/pre-commit` lo controla.
4. **GitHub Actions está deshabilitado** (cuenta universitaria). Antes de cada push ejecute `powershell -ExecutionPolicy Bypass -File tools\verificar.ps1`. Render se despliega a mano (Manual Deploy).
5. **Migraciones:** cambie el modelo y luego ejecute `dotnet ef migrations add <Nombre> --project backend/Makoto.Api`. No edite migraciones a mano.
6. **No toque el PRISMA** ni las fechas de los hitos con los tutores.
7. **Commits** en estilo Conventional Commits: `feat(T2-XX): …`, `docs(T2-XX): …`, `fix(T2-XX): …`.

## Pila

- **Back-end:** ASP.NET Core 10 + EF Core 10 + Npgsql (PostgreSQL 17 local, Neon en la nube).
- **Front-end:** React 19 + Vite 8 PWA (Node 22).
- **Audios:** Azure Speech (es-EC-AndreaNeural y es-EC-LuisNeural), medidos con Praat (parselmouth).
- **Despliegue:** Render (`Dockerfile.render`, `render.yaml`).
