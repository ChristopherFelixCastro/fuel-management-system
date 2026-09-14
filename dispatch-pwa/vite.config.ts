import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { VitePWA } from 'vite-plugin-pwa'

export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: 'autoUpdate',
      includeAssets: ['fuel-pump.svg', 'GasolinaLogo.png'],
      manifest: {
        name: 'PetroDespacho Terminal',
        short_name: 'PetroDespacho',
        description: 'Sistema de validación y despacho de combustible por QR',
        theme_color: '#0f172a',
        background_color: '#f8fafc',
        display: 'standalone',
        start_url: '/',
        icons: [
          { src: 'GasolinaLogo.png', sizes: '512x512', type: 'image/png', purpose: 'any' },
          { src: 'GasolinaLogo.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' },
          { src: 'fuel-pump.svg', sizes: 'any', type: 'image/svg+xml', purpose: 'any' }
        ]
      }
    })
  ]
})
