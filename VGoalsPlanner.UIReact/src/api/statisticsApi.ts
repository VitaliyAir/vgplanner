import { apiGet } from './httpClient'
import type { Statistics, StatisticsHistoryRow } from './types'

// GET /api/statistics — количество целей и задач: всего и по статусам.
export function getStatistics(): Promise<Statistics> {
  return apiGet<Statistics>('/statistics')
}

// GET /api/statistics/history?endDate=&days= — снимки статистики по дням за окно в `days` дней, заканчивая endDate.
export function getStatisticsHistory(endDate: string, days = 30): Promise<StatisticsHistoryRow[]> {
  return apiGet<StatisticsHistoryRow[]>(`/statistics/history?endDate=${endDate}&days=${days}`)
}
