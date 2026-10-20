import { apiDelete, apiGet, apiPatch, apiPost, apiPut } from './httpClient'
import type { CreateGoalRequest, GoalDetail, GoalListItem, PagedResult, UpdateGoalRequest } from './types'

// GET /api/vgoals/roots — верхний уровень дерева.
export function getRootGoals(): Promise<GoalListItem[]> {
  return apiGet<GoalListItem[]>('/vgoals/roots')
}

// GET /api/vgoals/{id} — цель + один уровень её дочерних целей/задач.
export function getGoalById(id: string): Promise<GoalDetail> {
  return apiGet<GoalDetail>(`/vgoals/${id}`)
}

// POST /api/vgoals — создать цель (корневую или дочернюю, см. parentGoalId).
export function createGoal(input: CreateGoalRequest): Promise<{ id: string }> {
  return apiPost<{ id: string }>('/vgoals', input)
}

// PUT /api/vgoals/{id} — обновить title/description/priority.
export function updateGoal(id: string, input: UpdateGoalRequest): Promise<void> {
  return apiPut(`/vgoals/${id}`, input)
}

// DELETE /api/vgoals/{id} — каскадное мягкое удаление цели со всем поддеревом.
export function deleteGoal(id: string): Promise<void> {
  return apiDelete(`/vgoals/${id}`)
}

// GET /api/vgoals?search= — поиск целей по названию/описанию (для пикера "перенести").
export function searchGoals(query: string, page = 1, pageSize = 20): Promise<PagedResult<GoalListItem>> {
  const params = new URLSearchParams({ search: query, page: String(page), pageSize: String(pageSize) })
  return apiGet<PagedResult<GoalListItem>>(`/vgoals?${params.toString()}`)
}

// PATCH /api/vgoals/{id}/move — сменить родителя цели (null — сделать целью верхнего уровня).
export function moveGoal(id: string, newParentGoalId: string | null): Promise<void> {
  return apiPatch(`/vgoals/${id}/move`, { newParentGoalId })
}

// PATCH /api/vgoals/reorder — переупорядочить цели внутри одного родителя (null — корневые).
export function reorderGoals(parentGoalId: string | null, orderedGoalIds: string[]): Promise<void> {
  return apiPatch(`/vgoals/reorder`, { parentGoalId, orderedGoalIds })
}

// GET /api/vgoals/{id}/breadcrumb — заголовки от корня дерева до цели включительно.
export function getGoalBreadcrumb(id: string): Promise<string[]> {
  return apiGet<string[]>(`/vgoals/${id}/breadcrumb`)
}
