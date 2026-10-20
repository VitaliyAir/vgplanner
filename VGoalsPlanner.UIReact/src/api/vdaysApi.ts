import { apiDelete, apiGet, apiPatch, apiPost, apiPut } from './httpClient'
import type { DaySchedule } from './types'

// GET /api/vdays/{date} — день и его задачи.
export function getDaySchedule(date: string): Promise<DaySchedule> {
  return apiGet<DaySchedule>(`/vdays/${date}`)
}

// GET /api/vdays/week?startDate= — 7 дней подряд начиная с startDate (понедельник).
export function getWeekSchedule(startDate: string): Promise<DaySchedule[]> {
  return apiGet<DaySchedule[]>(`/vdays/week?startDate=${startDate}`)
}

// GET /api/vdays/month?year=&month= — все дни календарного месяца.
export function getMonthSchedule(year: number, month: number): Promise<DaySchedule[]> {
  return apiGet<DaySchedule[]>(`/vdays/month?year=${year}&month=${month}`)
}

// POST /api/vdays/{date}/tasks — запланировать существующую задачу на день.
export function scheduleTask(date: string, taskId: string): Promise<void> {
  return apiPost<void>(`/vdays/${date}/tasks`, { taskId })
}

// DELETE /api/vdays/{date}/tasks/{taskId} — снять задачу с дня (не удаляет саму задачу).
export function unscheduleTask(date: string, taskId: string): Promise<void> {
  return apiDelete(`/vdays/${date}/tasks/${taskId}`)
}

// PUT /api/vdays/{date}/note — задать/очистить заметку дня (specialNote: null — удалить).
export function updateDayNote(date: string, specialNote: string | null): Promise<void> {
  return apiPut(`/vdays/${date}/note`, { specialNote })
}

// PATCH /api/vdays/{date}/tasks/reorder — переупорядочить задачи внутри дня.
export function reorderDayTasks(date: string, orderedTaskIds: string[]): Promise<void> {
  return apiPatch(`/vdays/${date}/tasks/reorder`, { orderedTaskIds })
}

// PATCH /api/vdays/{date}/tasks/{taskId}/active — переключить флаг "активна" у задачи в этот день.
export function toggleTaskActive(date: string, taskId: string): Promise<void> {
  return apiPatch(`/vdays/${date}/tasks/${taskId}/active`)
}
