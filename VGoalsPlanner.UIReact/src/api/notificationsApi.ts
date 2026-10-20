import { apiDelete, apiGet, apiPatch, apiPost, apiPut } from './httpClient'
import type { VNotificationItem } from './types'

// GET /api/vnotifications?date= — напоминания на конкретный день.
export function getDayNotifications(date: string): Promise<VNotificationItem[]> {
  return apiGet<VNotificationItem[]>(`/vnotifications?date=${date}`)
}

// GET /api/vnotifications/unread — все непрочитанные напоминания пользователя (для алертов).
export function getUnreadNotifications(): Promise<VNotificationItem[]> {
  return apiGet<VNotificationItem[]>('/vnotifications/unread')
}

interface NotificationInput {
  title: string
  description: string | null
  deadline: string | null
}

// POST /api/vnotifications — создать напоминание на день.
export function createNotification(date: string, input: NotificationInput): Promise<{ id: string }> {
  return apiPost<{ id: string }>('/vnotifications', { date, ...input })
}

// PUT /api/vnotifications/{id} — обновить напоминание.
export function updateNotification(id: string, input: NotificationInput): Promise<void> {
  return apiPut(`/vnotifications/${id}`, input)
}

// DELETE /api/vnotifications/{id} — удалить напоминание (жёсткое удаление).
export function deleteNotification(id: string): Promise<void> {
  return apiDelete(`/vnotifications/${id}`)
}

// PATCH /api/vnotifications/{id}/completed — отметить выполненным (необратимо).
export function markNotificationCompleted(id: string): Promise<void> {
  return apiPatch(`/vnotifications/${id}/completed`)
}

// PATCH /api/vnotifications/{id}/read — отметить прочитанным (закрыть алерт).
export function markNotificationRead(id: string): Promise<void> {
  return apiPatch(`/vnotifications/${id}/read`)
}
