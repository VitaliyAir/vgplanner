import { readFileSync } from 'node:fs'
import { dirname, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

const here = dirname(fileURLToPath(import.meta.url))

// Версия UI — из version.txt рядом с этим конфигом (тот же файл попадает в Docker-образ).
// Читаем файл, а не build-arg: иначе обычная сборка без аргументов молча давала бы 0.0.0.
function readVersion(): string {
  const file = resolve(here, 'version.txt')
  const value = readFileSync(file, 'utf8').trim()

  if (!/^\d+\.\d+\.\d+$/.test(value)) {
    throw new Error(`${file}: expected MAJOR.MINOR.PATCH, got "${value}"`)
  }

  return value
}

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  define: {
    // Вшивается в бандл; см. src/globals.d.ts и подпись версии в App.tsx.
    __APP_VERSION__: JSON.stringify(readVersion()),
    // Коммит знает только сборка (в контексте нет .git) — приходит из Docker build-arg.
    __APP_COMMIT__: JSON.stringify(process.env.VITE_APP_COMMIT ?? ''),
  },
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
