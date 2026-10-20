import { apiGet } from './httpClient'
import type { Statistics } from './types'

// GET /api/statistics — количество целей и задач: всего и по статусам.
export function getStatistics(): Promise<Statistics> {
  return apiGet<Statistics>('/statistics')
}
