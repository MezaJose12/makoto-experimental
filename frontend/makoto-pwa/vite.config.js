import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { VitePWA } from 'vite-plugin-pwa'

export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: 'autoUpdate',
      manifest: {
        name: 'Makoto · Estudio de voz y empatía',
        short_name: 'Makoto',
        description: 'Plataforma experimental · Universidad de las Fuerzas Armadas ESPE',
        lang: 'es',
        theme_color: '#0F6E56',
        background_color: '#ffffff',
        display: 'standalone',
        icons: [{ src: 'favicon.svg', sizes: 'any', type: 'image/svg+xml', purpose: 'any' }],
      },
      workbox: {
        // Solo la interfaz se guarda en caché. Nunca la API ni los audios:
        // el caché de audio permitiría saltarse el reproductor bloqueado.
        globPatterns: ['**/*.{js,css,html,svg}'],
        navigateFallbackDenylist: [/^\/api/],
      },
    }),
  ],
})
