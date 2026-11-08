import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'
import process from 'node:process'

const apiTarget = process.env.VITE_API_PROXY_TARGET || 'http://localhost:5080'

export default defineConfig({
  plugins: [react()],
  server: {
    host: '0.0.0.0',
    proxy: {
      '/api': {
        target: apiTarget,
        changeOrigin: true,
        secure: false,
      },
    },
  },
})

