import { useEffect, useState } from 'react'
import { getStatistics, getStatisticsHistory } from '../../api/statisticsApi'
import { ApiError } from '../../api/httpClient'
import type { GoalStatistics, Statistics, StatisticsHistoryRow, StatusStatistics, VTaskStatus } from '../../api/types'
import { TaskStatusIcon } from '../../components/TaskStatusIcon'
import { TASK_STATUS_LABEL, TASK_STATUS_OPTIONS } from '../goals-tree/labels'
import { formatDayLabel, toIsoDate } from '../planning/planningDates'
import './statistics.css'

const WINDOW_DAYS = 30

function parseIsoDate(iso: string): Date {
  const [y, m, d] = iso.split('-').map(Number)
  return new Date(y, m - 1, d)
}

const countOf = (tasks: StatusStatistics, status: VTaskStatus) =>
  tasks.byStatus.find((c) => c.status === status)?.count ?? 0

function StatsRow({
  label,
  goals,
  tasks,
  className,
}: {
  label: string
  goals: GoalStatistics
  tasks: StatusStatistics
  className?: string
}) {
  return (
    <tr className={className}>
      <td>{label}</td>
      <td>{goals.total}</td>
      <td>{tasks.total}</td>
      {TASK_STATUS_OPTIONS.map((s) => (
        <td key={s}>{countOf(tasks, s)}</td>
      ))}
    </tr>
  )
}

// Вкладка "Статистика": таблица по дням (снимки на конец дня) + строка "Сейчас" с текущими числами.
export function StatisticsView({ isActive }: { isActive: boolean }) {
  const [current, setCurrent] = useState<Statistics | null>(null)
  const [rows, setRows] = useState<StatisticsHistoryRow[]>([])
  const [nextEndDate, setNextEndDate] = useState<Date>(() => new Date())
  const [loading, setLoading] = useState(false)
  const [loadedOnce, setLoadedOnce] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // append=false — загрузка с начала (при открытии вкладки), true — предыдущие WINDOW_DAYS дней.
  const load = async (endDate: Date, append: boolean) => {
    setLoading(true)
    setError(null)
    try {
      const [page, now] = await Promise.all([
        getStatisticsHistory(toIsoDate(endDate), WINDOW_DAYS),
        append ? Promise.resolve(null) : getStatistics(),
      ])
      if (now) setCurrent(now)
      setRows((prev) => (append ? [...prev, ...page] : page))

      const prevEnd = new Date(endDate)
      prevEnd.setDate(prevEnd.getDate() - WINDOW_DAYS)
      setNextEndDate(prevEnd)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить статистику')
    } finally {
      setLoading(false)
      setLoadedOnce(true)
    }
  }

  useEffect(() => {
    if (isActive) void load(new Date(), false)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isActive])

  return (
    <div className="statistics-view">
      <h1>Статистика</h1>

      {error && <p className="tree-error">{error}</p>}

      <div className="stats-table-wrap">
        <table className="stats-table">
          <thead>
            <tr>
              <th>Дата</th>
              <th>Цели</th>
              <th>Задачи</th>
              {TASK_STATUS_OPTIONS.map((s) => (
                <th key={s}>
                  <TaskStatusIcon status={s} size={14} /> {TASK_STATUS_LABEL[s]}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {current && <StatsRow className="stats-table-now" label="Сейчас" goals={current.goals} tasks={current.tasks} />}
            {rows.map((r) => (
              <StatsRow key={r.date} label={formatDayLabel(parseIsoDate(r.date))} goals={r.goals} tasks={r.tasks} />
            ))}
          </tbody>
        </table>
      </div>

      {rows.length === 0 && loadedOnce && !loading && <p className="tree-empty">Снимков пока нет — первый появится в конце дня</p>}

      <button type="button" className="history-load-more" onClick={() => void load(nextEndDate, true)} disabled={loading}>
        {loading ? 'Загрузка…' : 'Показать ещё'}
      </button>
    </div>
  )
}
