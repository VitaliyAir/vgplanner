import { apiGet } from './httpClient'
import type { AppVersion } from './types'

// GET /api/version — версия развёрнутого API (эндпоинт открыт анонимно).
export function getAppVersion(): Promise<AppVersion> {
  return apiGet<AppVersion>('/version')
}
