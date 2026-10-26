import { apiDelete, apiGet, apiPatch, apiPost, apiPut } from './httpClient'
import type { ActiveTask, CreateTaskRequest, PagedResult, Priority, TaskDetail, TaskListItem, UpdateTaskRequest, VTaskStatus } from './types'

// GET /api/vtasks/{id} — полная задача (используем только для префилла формы).
export function getTaskById(id: string): Promise<TaskDetail> {
  return apiGet<TaskDetail>(`/vtasks/${id}`)
}

// POST /api/vtasks — создать задачу внутри цели (input.goalId).
export function createTask(input: CreateTaskRequest): Promise<{ id: string }> {
  return apiPost<{ id: string }>('/vtasks', input)
}

// PUT /api/vtasks/{id} — обновить задачу (все поля, кроме goalId/status).
export function updateTask(id: string, input: UpdateTaskRequest): Promise<void> {
  return apiPut(`/vtasks/${id}`, input)
}

// DELETE /api/vtasks/{id} — мягкое удаление задачи.
export function deleteTask(id: string): Promise<void> {
  return apiDelete(`/vtasks/${id}`)
}

// PATCH /api/vtasks/{id}/status — сменить статус задачи (отдельная команда, не часть Update).
export function changeTaskStatus(id: string, newStatus: VTaskStatus): Promise<void> {
  return apiPatch(`/vtasks/${id}/status`, { newStatus })
}

// PATCH /api/vtasks/{id}/move — перенести задачу к другой цели.
export function moveTask(id: string, newGoalId: string): Promise<void> {
  return apiPatch(`/vtasks/${id}/move`, { newGoalId })
}

// GET /api/vtasks/search — поиск по названию/описанию/тегам (для пикера в Планировании).
export function searchTasks(query: string, page = 1, pageSize = 20): Promise<PagedResult<TaskListItem>> {
  const params = new URLSearchParams({ query, page: String(page), pageSize: String(pageSize) })
  return apiGet<PagedResult<TaskListItem>>(`/vtasks/search?${params.toString()}`)
}

// PATCH /api/vtasks/reorder — переупорядочить задачи внутри одной цели.
export function reorderTasksInGoal(goalId: string, orderedTaskIds: string[]): Promise<void> {
  return apiPatch(`/vtasks/reorder`, { goalId, orderedTaskIds })
}

// GET /api/vtasks/active?date= — задачи, отмеченные активными сегодня (date — локальная дата клиента, yyyy-MM-dd).
export function getActiveTasks(date: string): Promise<ActiveTask[]> {
  return apiGet<ActiveTask[]>(`/vtasks/active?date=${date}`)
}

export interface TaskFilters {
  title?: string
  tag?: string
  status?: VTaskStatus
  priority?: Priority
  regular?: boolean
}

// GET /api/vtasks/filter — полный список задач с независимыми фильтрами, без пагинации.
export function filterTasks(filters: TaskFilters): Promise<TaskListItem[]> {
  const params = new URLSearchParams()
  if (filters.title) {
    params.set('title', filters.title)
  }
  if (filters.tag) {
    params.set('tag', filters.tag)
  }
  if (filters.status !== undefined) {
    params.set('status', String(filters.status))
  }
  if (filters.priority !== undefined) {
    params.set('priority', String(filters.priority))
  }
  if (filters.regular) {
    params.set('regular', 'true')
  }
  const query = params.toString()
  return apiGet<TaskListItem[]>(`/vtasks/filter${query ? `?${query}` : ''}`)
}
