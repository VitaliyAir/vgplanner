import { useEffect, useState } from 'react'
import { ApiError } from '../../api/httpClient'
import { getMonthSchedule, scheduleTask, unscheduleTask } from '../../api/vdaysApi'
import { Modal } from '../../components/Modal'
import { addMonths, formatMonthLabel, monthGridDates, toIsoDate } from '../planning/planningDates'

const MONTH_DOW_LABELS = ['Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб', 'Вс']

interface AssignDayModalProps {
  taskId: string
  // Вызывается после каждого успешного назначения/снятия — родитель может обновить свои данные.
  onChanged?: () => void
  onCancel: () => void
}

export function AssignDayModal({ taskId, onChanged, onCancel }: AssignDayModalProps) {
  const [viewMonth, setViewMonth] = useState(() => new Date())
  const [assignedDates, setAssignedDates] = useState<Set<string>>(new Set())
  const [pendingIso, setPendingIso] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
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

  // Клик по дню: не назначена — назначаем, назначена — снимаем; окно остаётся открытым.
  const toggleDay = async (iso: string) => {
    // Прошедшие дни менять нельзя (ISO-даты yyyy-MM-dd сравниваются как строки).
    if (pendingIso || iso < todayIso) {
      return
    }
    const wasAssigned = assignedDates.has(iso)
    setPendingIso(iso)
    setError(null)
    try {
      if (wasAssigned) {
        await unscheduleTask(iso, taskId)
      } else {
        await scheduleTask(iso, taskId)
      }
      setAssignedDates((prev) => {
        const next = new Set(prev)
        if (wasAssigned) {
          next.delete(iso)
        } else {
          next.add(iso)
        }
        return next
      })
      onChanged?.()
    } catch (err) {
      setError(
        err instanceof ApiError ? err.message : wasAssigned ? 'Не удалось снять задачу с дня' : 'Не удалось назначить задачу на день',
      )
    } finally {
      setPendingIso(null)
    }
  }

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
            const isPast = iso < todayIso

            return (
              <button
                key={iso}
                type="button"
                disabled={pendingIso !== null || isPast}
                className={`day-cell${muted ? ' day-cell--muted' : ''}${isToday ? ' day-cell--today' : ''}${isAssigned ? ' day-cell--assigned' : ''}${isPast ? ' day-cell--past' : ''}`}
                // Дни соседних месяцев: известны назначенные дни только видимого месяца, поэтому клик переключает месяц, а не назначает.
                onClick={() => (muted ? setViewMonth(date) : void toggleDay(iso))}
              >
                {date.getDate()}
              </button>
            )
          })}
        </div>

        <p className="assign-day-hint">Нажмите на день, чтобы назначить задачу; ещё раз — чтобы убрать. Прошедшие дни изменить нельзя.</p>

        {error && <p className="tree-error tree-error--inline">{error}</p>}
      </div>
    </Modal>
  )
}
