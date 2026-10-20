import type { DaySchedule, VTaskStatus } from '../../api/types'
import { PositionInput } from '../../components/PositionInput'
import { PriorityFlag } from '../../components/PriorityFlag'
import { TaskStatusIcon } from '../../components/TaskStatusIcon'
import { taskStatusBackground } from '../../components/taskStatusBackground'
import { TASK_STATUS_LABEL } from '../goals-tree/labels'
import { TrashIcon } from '../../components/icons/TrashIcon'
import { TaskLogIcon } from '../../components/icons/TaskLogIcon'

const WEEKDAY_FMT = new Intl.DateTimeFormat('ru-RU', { weekday: 'short' })

export type DayKind = 'past' | 'today' | 'future'

interface DayTileProps {
  date: Date
  isoDate: string
  schedule: DaySchedule | null
  variant: 'day' | 'week' | 'month'
  isToday: boolean
  dayKind: DayKind
  isWeekend: boolean
  busy: boolean
  onAddTask: (date: string) => void
  onEditTask: (taskId: string, date: string) => void
  onRemoveTask: (date: string, taskId: string) => void
  onAddNote: (date: string) => void
  onEditNote: (date: string, currentText: string) => void
  onRemoveNote: (date: string) => void
  onOpenTaskLog: (taskId: string, date: string, currentStatus: VTaskStatus) => void
  onOpenNotifications: (date: string) => void
  notificationsOpen: boolean
  onToggleNotifications: (date: string) => void
  onReorderTask: (date: string, taskId: string, targetIndex: number) => void
  onToggleActive: (date: string, taskId: string) => void
  onShowDescription: (taskId: string) => void
  breadcrumbs: Record<string, string[]>
}

export function DayTile({
  date,
  isoDate,
  schedule,
  variant,
  isToday,
  dayKind,
  isWeekend,
  busy,
  onAddTask,
  onEditTask,
  onRemoveTask,
  onAddNote,
  onEditNote,
  onRemoveNote,
  onOpenTaskLog,
  onOpenNotifications,
  notificationsOpen,
  onToggleNotifications,
  onReorderTask,
  onToggleActive,
  onShowDescription,
  breadcrumbs,
}: DayTileProps) {
  const tileClass = [
    variant === 'day' ? 'day-scale-tile' : variant === 'week' ? 'week-tile' : 'month-tile',
    variant !== 'month' ? 'paper' : '',
    variant === 'day' ? 'fold' : '',
    isToday ? 'today' : '',
    isWeekend ? 'weekend' : '',
    schedule === null ? 'outside' : '',
  ]
    .filter(Boolean)
    .join(' ')

  if (schedule === null) {
    return (
      <div className={tileClass}>
        <span className="day-num">{date.getDate()}</span>
      </div>
    )
  }

  const isPast = dayKind === 'past'
  const showNoteControls = variant !== 'month' && !isPast
  const showTaskDelete = variant !== 'month' && !isPast
  const showTaskEdit = !isPast
  const showTaskLog = dayKind === 'today' || isPast
  const showReorder = variant !== 'month' && !isPast
  const showActivateToggle = variant === 'day' && isToday

  return (
    <div className={tileClass}>
      <div className="day-head">
        <span className="day-num">{date.getDate()}</span>
        {variant !== 'month' && <span className="day-dow">{WEEKDAY_FMT.format(date)}</span>}
        {schedule.notifications.length > 0 && (
          <button
            type="button"
            className={`notification-toggle-btn${notificationsOpen ? ' active' : ''}`}
            onClick={() => onToggleNotifications(isoDate)}
            title="Уведомления на этот день"
          >
            !
          </button>
        )}
        <span className="header-add-btns">
          {!isPast && (
            <button type="button" className="header-add-btn task" onClick={() => onAddTask(isoDate)} disabled={busy} title="Добавить задачу">
              +T
            </button>
          )}
          {!isPast && (
            <button
              type="button"
              className="header-add-btn alert"
              onClick={() => onOpenNotifications(isoDate)}
              disabled={busy}
              title="Добавить напоминание"
            >
              +A
            </button>
          )}
          {showNoteControls && !schedule.specialNote && (
            <button type="button" className="header-add-btn note" onClick={() => onAddNote(isoDate)} disabled={busy} title="Заметка дня">
              +N
            </button>
          )}
        </span>
      </div>

      {variant !== 'month' && schedule.specialNote && (
        <>
          <div className="day-note-divider" />
          <div className="day-note">
            <span className="day-note-text">{schedule.specialNote}</span>
            {showNoteControls && (
              <span className="day-note-actions">
                <button
                  type="button"
                  onClick={() => onEditNote(isoDate, schedule.specialNote ?? '')}
                  disabled={busy}
                  title="Редактировать заметку"
                >
                  ✎
                </button>
                <button type="button" onClick={() => onRemoveNote(isoDate)} disabled={busy} title="Удалить заметку">
                  ✕
                </button>
              </span>
            )}
          </div>
        </>
      )}

      {schedule.tasks.map((task, index) => (
        <div
          key={task.taskId}
          className={`day-task${task.hasLog ? ' has-log' : ''}${showActivateToggle && task.isActive ? ' day-task--active' : ''} ${taskStatusBackground(task.status)}`}
        >
          {variant !== 'month' && breadcrumbs[task.goalId] && breadcrumbs[task.goalId].length > 0 && (
            <span className="t-breadcrumb">{breadcrumbs[task.goalId].join(' › ')}</span>
          )}
          {showReorder && !task.isVirtual && (
            <span className="t-order">
              <PositionInput
                index={index}
                count={schedule.tasks.length}
                disabled={busy}
                onMoveTo={(target) => onReorderTask(isoDate, task.taskId, target)}
              />
            </span>
          )}
          <PriorityFlag priority={task.priority} inline={variant === 'month'} />
          <button type="button" className="t-title" onClick={() => onShowDescription(task.taskId)} title="Показать описание">
            {task.isRecurring && (
              <span className="t-recurring" title="Регулярная задача" aria-label="Регулярная задача">
                ↻
              </span>
            )}
            {task.title}
          </button>
          <span className="t-actions">
            {showTaskEdit && (
              <button type="button" onClick={() => onEditTask(task.taskId, isoDate)} disabled={busy} title="Редактировать">
                ✎
              </button>
            )}
            {showTaskDelete && !task.isVirtual && (
              <button
                type="button"
                className="danger"
                onClick={() => onRemoveTask(isoDate, task.taskId)}
                disabled={busy}
                title="Убрать с этого дня"
              >
                <TrashIcon size={11} />
              </button>
            )}
            {showTaskLog && (
              <button
                type="button"
                className="task-log-btn"
                onClick={() => onOpenTaskLog(task.taskId, isoDate, task.status)}
                disabled={busy}
                title="Журнал задачи"
              >
                <TaskLogIcon size={11} />
              </button>
            )}
          </span>
          {variant !== 'month' && (
            <div className="t-foot">
              <span className={`pill pill-task-status-${task.status}`}>
                <TaskStatusIcon status={task.status} size={14} />
                {TASK_STATUS_LABEL[task.status]}
              </span>
              {showActivateToggle && (
                <label className="task-active-switch" title={task.isActive ? 'Снять активность' : 'Сделать активной'}>
                  <input
                    type="checkbox"
                    role="switch"
                    checked={task.isActive}
                    disabled={busy}
                    onChange={() => onToggleActive(isoDate, task.taskId)}
                  />
                  <span className="task-active-switch-track" aria-hidden="true" />
                  Активна
                </label>
              )}
            </div>
          )}
        </div>
      ))}
    </div>
  )
}
