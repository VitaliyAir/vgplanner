import { notifyForbidden } from '../auth/authEvents'
import { userManager } from '../auth/oidcConfig'
import type { ApiErrorBody } from './types'

export class ApiError extends Error {
  code: string
  status: number

  constructor(body: ApiErrorBody, status: number) {
    super(body.description || body.code)
    this.name = 'ApiError'
    this.code = body.code
    this.status = status
  }
}

const BASE_URL = '/api'

async function sendWithToken(input: string, init: RequestInit, forceRenew: boolean): Promise<Response> {
  const user = forceRenew ? await userManager.signinSilent() : await userManager.getUser()
  const headers = new Headers(init.headers)
  if (user?.access_token) {
    headers.set('Authorization', `Bearer ${user.access_token}`)
  }
  return fetch(input, { ...init, headers })
}

// Добавляет Bearer-токен; при 401 один раз обновляет токен и повторяет запрос, иначе отправляет на вход.
async function authorizedFetch(input: string, init: RequestInit = {}): Promise<Response> {
  const response = await sendWithToken(input, init, false)
  if (response.status !== 401) {
    return response
  }

  try {
    const retried = await sendWithToken(input, init, true)
    if (retried.status !== 401) {
      return retried
    }
  } catch {
    // Обновить токен не удалось — падаем на повторный вход ниже.
  }

  await userManager.signinRedirect()
  return new Promise<Response>(() => {})
}

async function handleResponse<T>(path: string, response: Response): Promise<T> {
  if (response.status === 403) {
    notifyForbidden()
    throw new ApiError({ code: 'Auth.Forbidden', description: 'Нет доступа к приложению' }, 403)
  }

  if (!response.ok) {
    const body = (await response.json().catch(() => null)) as ApiErrorBody | null

    if (body?.code) {
      throw new ApiError(body, response.status)
    }

    throw new Error(`Запрос ${path} завершился с HTTP ${response.status}`)
  }

  // PUT/DELETE эндпоинты отдают 200/204 с пустым телом (Ok()/NoContent() без
  // аргумента) — response.json() на пустой строке бросает SyntaxError, поэтому
  // проверяем текст явно вместо того, чтобы полагаться только на статус 204.
  const text = await response.text()
  if (text.length === 0) {
    return undefined as T
  }

  return JSON.parse(text) as T
}

export async function apiGet<T>(path: string): Promise<T> {
  const response = await authorizedFetch(`${BASE_URL}${path}`, {
    headers: { Accept: 'application/json' },
  })

  return handleResponse<T>(path, response)
}

export async function apiPost<T>(path: string, body: unknown): Promise<T> {
  const response = await authorizedFetch(`${BASE_URL}${path}`, {
    method: 'POST',
    headers: { Accept: 'application/json', 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })

  return handleResponse<T>(path, response)
}

export async function apiPut(path: string, body: unknown): Promise<void> {
  const response = await authorizedFetch(`${BASE_URL}${path}`, {
    method: 'PUT',
    headers: { Accept: 'application/json', 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })

  await handleResponse<void>(path, response)
}

export async function apiDelete(path: string): Promise<void> {
  const response = await authorizedFetch(`${BASE_URL}${path}`, {
    method: 'DELETE',
    headers: { Accept: 'application/json' },
  })

  await handleResponse<void>(path, response)
}

export async function apiPatch(path: string, body?: unknown): Promise<void> {
  const response = await authorizedFetch(`${BASE_URL}${path}`, {
    method: 'PATCH',
    headers: body === undefined ? { Accept: 'application/json' } : { Accept: 'application/json', 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  })

  await handleResponse<void>(path, response)
}

// Загрузка файла: тело запроса — сам файл (без multipart), имя файла передаётся в query самим вызывающим.
export async function apiUpload<T>(path: string, file: File): Promise<T> {
  const response = await authorizedFetch(`${BASE_URL}${path}`, {
    method: 'POST',
    headers: { Accept: 'application/json', 'Content-Type': file.type || 'application/octet-stream' },
    body: file,
  })

  return handleResponse<T>(path, response)
}

// Скачивание через fetch: API требует Bearer-токен, обычная ссылка <a href> его не отправит.
export async function apiDownload(path: string): Promise<Blob> {
  const response = await authorizedFetch(`${BASE_URL}${path}`, { method: 'GET' })

  if (!response.ok) {
    await handleResponse<void>(path, response)
  }

  return response.blob()
}
