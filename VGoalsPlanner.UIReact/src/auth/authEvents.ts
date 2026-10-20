// httpClient сообщает о 403 (нет роли Owner у этого приложения), а AuthGate показывает экран "нет доступа".
const forbiddenListeners = new Set<() => void>()

export function onForbidden(listener: () => void): () => void {
  forbiddenListeners.add(listener)
  return () => {
    forbiddenListeners.delete(listener)
  }
}

export function notifyForbidden(): void {
  forbiddenListeners.forEach((listener) => listener())
}
