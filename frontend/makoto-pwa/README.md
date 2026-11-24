# Makoto · React PWA

```bash
npm install
npm run dev      # http://localhost:5173 (usa la API de .env.development)
npm run build    # genera dist/ para Render (Static Site)
```

- `src/services/api.js`: único lugar que llama a la API.
- `src/modules/experimento`, `test`, `dashboard`: un módulo por pantalla.
- La PWA guarda en caché solo la interfaz; nunca `/api` ni los audios.
- En Render (Static Site): Root Directory `frontend/makoto-pwa`, Build `npm ci && npm run build`, Publish `dist`, y una regla Rewrite `/*` → `/index.html`.
