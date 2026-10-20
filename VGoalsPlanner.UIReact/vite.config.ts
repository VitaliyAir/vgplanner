import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // Явно IPv4 — на этой машине "localhost" резолвится в ::1, и Node слушает
    // только IPv6-loopback, из-за чего curl/браузер на 127.0.0.1 не достучатся.
    host: '127.0.0.1',
    proxy: {
      '/api': {
        target: 'http://localhost:5016',
        changeOrigin: true,
      },
    },
  },
})
