import { apiGet, apiPost, apiPut } from './httpClient'
import type { TaskLog, TimelineDay, VTaskStatus } from './types'

// GET /api/vtasklogs?taskId= — вся история логов задачи (все дни).
export function getTaskHistory(taskId: string): Promise<TaskLog[]> {
  return apiGet<TaskLog[]>(`/vtasklogs?taskId=${taskId}`)
}

// GET /api/vtasklogs/timeline?endDate=&days= — лента логов за окно в `days`
// календарных дней, заканчивая endDate (для вкладки "История").
export function getLogTimeline(endDate: string, days = 7): Promise<TimelineDay[]> {
  return apiGet<TimelineDay[]>(`/vtasklogs/timeline?endDate=${endDate}&days=${days}`)
}

interface AddTaskLogInput {
  taskId: string
  date: string
  note: string
  timeSpentMinutes: number | null
  oldStatus: VTaskStatus | null
  newStatus: VTaskStatus | null
}

// POST /api/vtasklogs — новая запись лога (создаёт VDay, если его ещё нет).
export function addTaskLog(input: AddTaskLogInput): Promise<{ id: string }> {
  return apiPost<{ id: string }>('/vtasklogs', {
    taskId: input.taskId,
    date: input.date,
    note: input.note,
    attachments: [],
    timeSpentMinutes: input.timeSpentMinutes,
    oldStatus: input.oldStatus,
    newStatus: input.newStatus,
  })
}

// PUT /api/vtasklogs/{id} — обновить текст существующей записи лога.
export function updateTaskLog(id: string, note: string, attachments: string[]): Promise<void> {
  return apiPut(`/vtasklogs/${id}`, { note, attachments })
}
