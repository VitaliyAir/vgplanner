import { useEffect, useState } from 'react'
import { getMonthSchedule } from '../../api/vdaysApi'
import { Modal } from '../../components/Modal'
import { addMonths, formatMonthLabel, monthGridDates, toIsoDate } from '../planning/planningDates'

const MONTH_DOW_LABELS = ['Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб', 'Вс']

interface AssignDayModalProps {
  taskId: string
  onPick: (isoDate: string) => void
  onCancel: () => void
}

export function AssignDayModal({ taskId, onPick, onCancel }: AssignDayModalProps) {
  const [viewMonth, setViewMonth] = useState(() => new Date())
  const [assignedDates, setAssignedDates] = useState<Set<string>>(new Set())
  const today = new Date()
  const todayIso = toIsoDate(today)

  useEffect(() => {
    let cancelled = false

    getMonthSchedule(viewMonth.getFullYear(), viewMonth.getMonth() + 1)
      .then((days) => {
        if (cancelled) {
          return
        }
        const dates = new Set(days.filter((d) => d.tasks.some((t) => t.taskId === taskId)).map((d) => d.date))
        setAssignedDates(dates)
      })
      .catch(() => {
        // Подсветка занятых дней необязательна для работы модалки — молча пропускаем неудачу.
      })

    return () => {
      cancelled = true
    }
  }, [viewMonth, taskId])

  const dates = monthGridDates(viewMonth.getFullYear(), viewMonth.getMonth() + 1)

  return (
    <Modal title="Назначить на день" onClose={onCancel}>
      <div className="assign-day-calendar">
        <div className="assign-day-nav">
          <button type="button" onClick={() => setViewMonth((m) => addMonths(m, -1))} aria-label="Предыдущий месяц">
            ‹
          </button>
          <span className="assign-day-nav-label">{formatMonthLabel(viewMonth)}</span>
          <button type="button" onClick={() => setViewMonth((m) => addMonths(m, 1))} aria-label="Следующий месяц">
            ›
          </button>
        </div>

        <div className="assign-day-dow-row">
          {MONTH_DOW_LABELS.map((label) => (
            <span key={label}>{label}</span>
          ))}
        </div>

        <div className="assign-day-grid">
          {dates.map((date) => {
            const iso = toIsoDate(date)
            const muted = date.getMonth() !== viewMonth.getMonth()
            const isToday = iso === todayIso
            const isAssigned = assignedDates.has(iso)

            return (
              <button
                key={iso}
                type="button"
                className={`day-cell${muted ? ' day-cell--muted' : ''}${isToday ? ' day-cell--today' : ''}${isAssigned ? ' day-cell--assigned' : ''}`}
                onClick={() => onPick(iso)}
              >
                {date.getDate()}
              </button>
            )
          })}
        </div>
      </div>
    </Modal>
  )
}
