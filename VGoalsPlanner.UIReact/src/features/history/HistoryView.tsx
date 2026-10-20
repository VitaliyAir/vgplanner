import { useEffect, useState } from 'react'
import { getLogTimeline } from '../../api/taskLogsApi'
import { ApiError } from '../../api/httpClient'
import type { TimelineDay } from '../../api/types'
import { CollapsibleContent } from '../../components/CollapsibleContent'
import { MarkdownView } from '../../components/MarkdownView'
import { TASK_STATUS_LABEL } from '../goals-tree/labels'
import { formatDayLabel, toIsoDate } from '../planning/planningDates'
import './history.css'

const WINDOW_DAYS = 7

function parseIsoDate(iso: string): Date {
  const [y, m, d] = iso.split('-').map(Number)
  return new Date(y, m - 1, d)
}

const TIME_FMT = new Intl.DateTimeFormat('ru-RU', { hour: '2-digit', minute: '2-digit' })

export function HistoryView() {
  const [days, setDays] = useState<TimelineDay[]>([])
  const [nextEndDate, setNextEndDate] = useState<Date>(() => new Date())
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [loadedOnce, setLoadedOnce] = useState(false)

  const loadMore = async () => {
    setLoading(true)
    setError(null)
    try {
      const page = await getLogTimeline(toIsoDate(nextEndDate), WINDOW_DAYS)
      setDays((prev) => [...prev, ...page])

      const windowStart = new Date(nextEndDate)
      windowStart.setDate(windowStart.getDate() - (WINDOW_DAYS - 1))
      const newNextEndDate = new Date(windowStart)
      newNextEndDate.setDate(newNextEndDate.getDate() - 1)
      setNextEndDate(newNextEndDate)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить историю')
    } finally {
      setLoading(false)
      setLoadedOnce(true)
    }
  }

  useEffect(() => {
    loadMore()
  }, [])

  return (
    <div className="history-view">
      <h1>История</h1>

      {error && <p className="tree-error">{error}</p>}

      {days.length === 0 && loadedOnce && !loading && <p className="tree-empty">Записей пока нет</p>}

      {days.map((day) => (
        <section key={day.date} className="history-day">
          <h2 className="history-day-header">{formatDayLabel(parseIsoDate(day.date))}</h2>

          {day.specialNote && (
            <div className="history-day-note">
              <CollapsibleContent>{day.specialNote}</CollapsibleContent>
            </div>
          )}

          {day.logs.length > 0 && (
            <ul className="history-log-list">
              {day.logs.map((log) => (
                <li key={log.id} className="history-log-entry">
                  <div className="history-log-top">
                    <span className="history-log-task">{log.taskTitle}</span>
                    <span className="history-log-time">{TIME_FMT.format(new Date(log.createdAt))}</span>
                  </div>
                  {log.note && (
                    <div className="history-log-note">
                      <CollapsibleContent>
                        <MarkdownView source={log.note} />
                      </CollapsibleContent>
                    </div>
                  )}
                  {(log.timeSpentMinutes != null || log.newStatus != null) && (
                    <div className="history-log-meta">
                      {log.timeSpentMinutes != null && <span>{log.timeSpentMinutes} мин</span>}
                      {log.newStatus != null && (
                        <span>
                          Статус: {log.oldStatus != null ? TASK_STATUS_LABEL[log.oldStatus] : '—'} → {TASK_STATUS_LABEL[log.newStatus]}
                        </span>
                      )}
                    </div>
                  )}
                </li>
              ))}
            </ul>
          )}
        </section>
      ))}

      <button type="button" className="history-load-more" onClick={loadMore} disabled={loading}>
        {loading ? 'Загрузка…' : 'Показать ещё'}
      </button>
    </div>
  )
}
