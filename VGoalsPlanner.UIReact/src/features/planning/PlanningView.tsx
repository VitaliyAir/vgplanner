import { useEffect, useRef, useState } from 'react'
import { getGoalBreadcrumb } from '../../api/goalsApi'
import { getDaySchedule, getMonthSchedule, getWeekSchedule, reorderDayTasks, scheduleTask, toggleTaskActive, unscheduleTask, updateDayNote } from '../../api/vdaysApi'
import { changeTaskStatus, getActiveTasks, getTaskById, updateTask } from '../../api/tasksApi'
import { ApiError } from '../../api/httpClient'
import type { ActiveTask, DaySchedule, TaskDetail, VTaskStatus } from '../../api/types'
import { Modal } from '../../components/Modal'
import { AssignDayModal } from '../goals-tree/AssignDayModal'
import { reorderToVisibleIndex } from '../../components/reorder'
import { TaskAttachments } from '../../components/TaskAttachments'
import { TaskDescriptionView } from '../../components/TaskDescriptionView'
import { TaskLinksView } from '../../components/TaskLinksView'
import { TaskLogIcon } from '../../components/icons/TaskLogIcon'
import { TaskForm, type TaskFormValues } from '../goals-tree/TaskForm'
import { DayTile, type DayKind } from './DayTile'
import { TaskPickerModal } from './TaskPickerModal'
import { TaskLogModal } from './TaskLogModal'
import { NotificationsModal } from './NotificationsModal'
import { DayNotificationsPanel } from './DayNotificationsPanel'
import { addDays, addMonths, formatDayLabel, formatMonthLabel, formatWeekLabel, mondayOf, monthGridDates, toIsoDate } from './planningDates'
import './planning.css'

type Scale = 'day' | 'week' | 'month'

const MONTH_DOW_LABELS = ['Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб', 'Вс']

function isSameDate(a: Date, b: Date): boolean {
  return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate()
}

function dayKindOf(date: Date, today: Date): DayKind {
  if (isSameDate(date, today)) {
    return 'today'
  }
  return date < today ? 'past' : 'future'
}

function isWeekendDate(date: Date): boolean {
  const day = date.getDay()
  return day === 0 || day === 6
}

function parseIsoDate(iso: string): Date {
  const [y, m, d] = iso.split('-').map(Number)
  return new Date(y, m - 1, d)
}

export function PlanningView() {
  const [scale, setScale] = useState<Scale>('day')
  const [anchorDate, setAnchorDate] = useState(() => new Date())
  const [days, setDays] = useState<DaySchedule[] | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [picker, setPicker] = useState<{ date: string } | null>(null)
  const [editing, setEditing] = useState<{ taskId: string; date: string; detail: TaskDetail } | null>(null)
  const [noteEditing, setNoteEditing] = useState<{ date: string; initialText: string } | null>(null)
  const [taskLogTarget, setTaskLogTarget] = useState<{ taskId: string; date: string; currentStatus: VTaskStatus } | null>(null)
  const [notificationsTarget, setNotificationsTarget] = useState<string | null>(null)
  const [openNotifDay, setOpenNotifDay] = useState<string | null>(null)
  const [assignTaskId, setAssignTaskId] = useState<string | null>(null)
  const [descriptionTarget, setDescriptionTarget] = useState<TaskDetail | null>(null)
  const [breadcrumbs, setBreadcrumbs] = useState<Record<string, string[]>>({})
  const breadcrumbsRef = useRef(breadcrumbs)
  breadcrumbsRef.current = breadcrumbs

  const [activeTasks, setActiveTasks] = useState<ActiveTask[]>([])
  const [activeStatus, setActiveStatus] = useState<'loading' | 'ok' | 'error'>('loading')

  const load = async (currentScale: Scale, currentAnchor: Date) => {
    setLoading(true)
    setError(null)
    try {
      let data: DaySchedule[]
      if (currentScale === 'day') {
        data = [await getDaySchedule(toIsoDate(currentAnchor))]
      } else if (currentScale === 'week') {
        data = await getWeekSchedule(toIsoDate(mondayOf(currentAnchor)))
      } else {
        data = await getMonthSchedule(currentAnchor.getFullYear(), currentAnchor.getMonth() + 1)
      }
      setDays(data)

      const goalIds = Array.from(new Set(data.flatMap((d) => d.tasks.map((t) => t.goalId)))).filter(
        (id) => !(id in breadcrumbsRef.current),
      )
      goalIds.forEach((goalId) => {
        getGoalBreadcrumb(goalId)
          .then((titles) => setBreadcrumbs((prev) => ({ ...prev, [goalId]: titles })))
          .catch(() => {
            // Хлебные крошки не критичны для отображения — молча пропускаем неудачу.
          })
      })
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить расписание')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    load(scale, anchorDate)
  }, [scale, anchorDate])

  const loadActiveTasks = () => {
    getActiveTasks(toIsoDate(new Date()))
      .then((data) => {
        setActiveTasks(data)
        setActiveStatus('ok')
        const missingGoalIds = Array.from(new Set(data.map((t) => t.goalId))).filter((id) => !(id in breadcrumbsRef.current))
        missingGoalIds.forEach((goalId) => {
          getGoalBreadcrumb(goalId)
            .then((titles) => setBreadcrumbs((prev) => ({ ...prev, [goalId]: titles })))
            .catch(() => {
              // Хлебные крошки не критичны для отображения — молча пропускаем неудачу.
            })
        })
      })
      .catch(() => {
        setActiveStatus('error')
      })
  }

  useEffect(() => {
    loadActiveTasks()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const today = new Date()
  const goToday = () => setAnchorDate(new Date())
  const goPrev = () => setAnchorDate((d) => (scale === 'month' ? addMonths(d, -1) : addDays(d, scale === 'day' ? -1 : -7)))
  const goNext = () => setAnchorDate((d) => (scale === 'month' ? addMonths(d, 1) : addDays(d, scale === 'day' ? 1 : 7)))

  const heading =
    scale === 'day' ? formatDayLabel(anchorDate) : scale === 'week' ? formatWeekLabel(mondayOf(anchorDate)) : formatMonthLabel(anchorDate)

  const scheduleByDate = new Map<string, DaySchedule>()
  ;(days ?? []).forEach((d) => scheduleByDate.set(d.date, d))

  const visibleDates: Date[] =
    scale === 'day'
      ? [anchorDate]
      : scale === 'week'
        ? Array.from({ length: 7 }, (_, i) => addDays(mondayOf(anchorDate), i))
        : monthGridDates(anchorDate.getFullYear(), anchorDate.getMonth() + 1)

  const handleAddTask = (date: string) => setPicker({ date })

  const handlePickTask = async (taskId: string) => {
    if (!picker) {
      return
    }
    const date = picker.date
    setPicker(null)
    try {
      await scheduleTask(date, taskId)
      await load(scale, anchorDate)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось запланировать задачу')
    }
  }

  const handleRemoveTask = async (date: string, taskId: string) => {
    try {
      await unscheduleTask(date, taskId)
      await load(scale, anchorDate)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось убрать задачу с дня')
    }
  }

  const handleReorderTask = async (date: string, taskId: string, targetIndex: number) => {
    const tasks = scheduleByDate.get(date)?.tasks
    if (!tasks) {
      return
    }
    const reordered = reorderToVisibleIndex(tasks, tasks, (t) => t.taskId, taskId, targetIndex)
    if (!reordered) {
      return
    }
    try {
      await reorderDayTasks(date, reordered.map((t) => t.taskId))
      await load(scale, anchorDate)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось изменить порядок задач')
    }
  }

  const handleToggleActive = async (date: string, taskId: string) => {
    try {
      await toggleTaskActive(date, taskId)
      await load(scale, anchorDate)
      loadActiveTasks()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось изменить активность задачи')
    }
  }

  // Журнал активной задачи: окну журнала нужен текущий статус, которого нет в списке активных, — подгружаем задачу.
  const handleOpenActiveTaskLog = async (taskId: string) => {
    try {
      const detail = await getTaskById(taskId)
      setTaskLogTarget({ taskId, date: toIsoDate(new Date()), currentStatus: detail.status })
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить задачу')
    }
  }

  const handleShowDescription = async (taskId: string) => {
    try {
      const detail = await getTaskById(taskId)
      setDescriptionTarget(detail)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить задачу')
    }
  }

  const handleEditTask = async (taskId: string, date: string) => {
    try {
      const detail = await getTaskById(taskId)
      setEditing({ taskId, date, detail })
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить задачу')
    }
  }

  const handleEditSubmit = async (values: TaskFormValues) => {
    if (!editing) {
      return
    }
    await updateTask(editing.taskId, {
      title: values.title,
      description: values.description,
      deadline: values.deadline,
      priority: values.priority,
      type: values.type,
      estimatedMinutes: values.estimatedMinutes,
      tags: values.tags,
      links: values.links,
      recurrenceDays: values.recurrenceDays,
    })
    if (values.status !== editing.detail.status) {
      await changeTaskStatus(editing.taskId, values.status)
    }
    setEditing(null)
    await load(scale, anchorDate)
    loadActiveTasks()
  }

  const handleAddNote = (date: string) => setNoteEditing({ date, initialText: '' })
  const handleEditNote = (date: string, currentText: string) => setNoteEditing({ date, initialText: currentText })

  const handleRemoveNote = async (date: string) => {
    try {
      await updateDayNote(date, null)
      await load(scale, anchorDate)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось удалить заметку')
    }
  }

  const handleToggleNotifications = (date: string) => setOpenNotifDay((prev) => (prev === date ? null : date))

  const handleNoteSave = async (text: string) => {
    if (!noteEditing) {
      return
    }
    try {
      await updateDayNote(noteEditing.date, text)
      setNoteEditing(null)
      await load(scale, anchorDate)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось сохранить заметку')
    }
  }

  const renderTile = (date: Date, variant: Scale) => {
    const iso = toIsoDate(date)
    const schedule = scheduleByDate.get(iso) ?? null

    return (
      <DayTile
        key={iso}
        date={date}
        isoDate={iso}
        schedule={schedule}
        variant={variant}
        isToday={isSameDate(date, today)}
        dayKind={dayKindOf(date, today)}
        isWeekend={isWeekendDate(date)}
        busy={loading}
        onAddTask={handleAddTask}
        onEditTask={handleEditTask}
        onRemoveTask={handleRemoveTask}
        onAddNote={handleAddNote}
        onEditNote={handleEditNote}
        onRemoveNote={handleRemoveNote}
        onOpenTaskLog={(taskId, taskDate, currentStatus) => setTaskLogTarget({ taskId, date: taskDate, currentStatus })}
        onOpenNotifications={(taskDate) => setNotificationsTarget(taskDate)}
        notificationsOpen={openNotifDay === iso}
        onToggleNotifications={handleToggleNotifications}
        onReorderTask={handleReorderTask}
        onToggleActive={handleToggleActive}
        onAssignTask={setAssignTaskId}
        onShowDescription={handleShowDescription}
        breadcrumbs={breadcrumbs}
      />
    )
  }

  return (
    <div className="planning-view">
      <div className="toolbar">
        <button type="button" className="toolbar-btn" onClick={goToday}>
          Сегодня
        </button>
        <div className="toolbar-nav">
          <button type="button" onClick={goPrev} aria-label="Назад">
            ‹
          </button>
          <button type="button" onClick={goNext} aria-label="Вперёд">
            ›
          </button>
        </div>
        <span className="toolbar-label">{heading}</span>
        <div className="scale-switch">
          <button type="button" className={scale === 'day' ? 'active' : ''} onClick={() => setScale('day')}>
            День
          </button>
          <button type="button" className={scale === 'week' ? 'active' : ''} onClick={() => setScale('week')}>
            Неделя
          </button>
          <button type="button" className={scale === 'month' ? 'active' : ''} onClick={() => setScale('month')}>
            Месяц
          </button>
        </div>
      </div>

      {error && <p className="tree-error">{error}</p>}

      <div className="planning-layout">
        <div className={`planning-main${scale === 'month' ? ' planning-main--month' : ''}`}>
          {scale === 'day' && <div className="day-scale-wrap">{renderTile(anchorDate, 'day')}</div>}

          {scale === 'week' && <div className="week-row">{visibleDates.map((d) => renderTile(d, 'week'))}</div>}

          {scale === 'month' && (
            <>
              <div className="month-dow-row">
                {MONTH_DOW_LABELS.map((label) => (
                  <span key={label}>{label}</span>
                ))}
              </div>
              <div className="month-grid">{visibleDates.map((d) => renderTile(d, 'month'))}</div>
            </>
          )}
        </div>

        {scale === 'day' && (
          <aside
            className={`active-tasks-section planning-active-tasks${activeStatus === 'ok' && activeTasks.length > 0 ? '' : ' planning-active-tasks--empty'}`}
          >
            <h2>Активные задачи</h2>
            {activeStatus === 'loading' && <p className="planning-active-tasks-empty">Загрузка…</p>}
            {activeStatus === 'error' && (
              <p className="planning-active-tasks-empty">
                Не удалось загрузить. <button type="button" onClick={loadActiveTasks}>Повторить</button>
              </p>
            )}
            {activeStatus === 'ok' && activeTasks.length === 0 && (
              <p className="planning-active-tasks-empty">
                На сегодня нет активных задач. Включите переключатель «Активна» у задачи в плане дня.
              </p>
            )}
            {activeStatus === 'ok' && activeTasks.map((task) => {
              const crumb = breadcrumbs[task.goalId]
              return (
                <div key={task.id} className="active-task-item">
                  {crumb && crumb.length > 0 && <div className="recent-task-breadcrumb">{crumb.join(' › ')}</div>}
                  <div className="active-task-title-row">
                    <div className="active-task-title">{task.title}</div>
                    <span className="active-task-actions">
                      <button type="button" className="icon-button" onClick={() => setAssignTaskId(task.id)} title="Назначить на день">
                        📅
                      </button>
                      <button
                        type="button"
                        className="icon-button active-task-btn--edit"
                        onClick={() => handleEditTask(task.id, toIsoDate(new Date()))}
                        title="Редактировать задачу"
                      >
                        ✎
                      </button>
                      <button
                        type="button"
                        className="icon-button active-task-btn--log"
                        onClick={() => handleOpenActiveTaskLog(task.id)}
                        title="Добавить запись в журнал"
                      >
                        <TaskLogIcon size={14} />
                      </button>
                    </span>
                  </div>
                  {task.description && (
                    <div className="active-task-description">
                      <TaskDescriptionView description={task.description} />
                    </div>
                  )}
                </div>
              )
            })}
          </aside>
        )}
      </div>

      {picker && <TaskPickerModal excludeTaskIds={(scheduleByDate.get(picker.date)?.tasks ?? []).map((t) => t.taskId)} onPick={handlePickTask} onCancel={() => setPicker(null)} />}

      {assignTaskId && (
        <AssignDayModal
          taskId={assignTaskId}
          onChanged={() => {
            load(scale, anchorDate)
            loadActiveTasks()
          }}
          onCancel={() => setAssignTaskId(null)}
        />
      )}

      {editing && (
        <Modal title="Редактировать задачу" persistent className="modal-panel--wide" onClose={() => setEditing(null)}>
          <TaskForm
            submitLabel="Сохранить"
            initialValues={{
              title: editing.detail.title,
              description: editing.detail.description ?? '',
              deadline: editing.detail.deadline ? editing.detail.deadline.slice(0, 10) : '',
              priority: editing.detail.priority,
              status: editing.detail.status,
              type: editing.detail.type,
              estimatedMinutes: editing.detail.estimatedMinutes != null ? String(editing.detail.estimatedMinutes) : '',
              tags: editing.detail.tags.join(', '),
              links: editing.detail.links,
              recurrenceDays: editing.detail.recurrenceDays,
            }}
            attachments={{ taskId: editing.taskId, items: editing.detail.attachments, onChange: () => {} }}
            onSubmit={handleEditSubmit}
            onCancel={() => setEditing(null)}
          />
        </Modal>
      )}

      {noteEditing && <NoteEditorModal initialText={noteEditing.initialText} onSave={handleNoteSave} onCancel={() => setNoteEditing(null)} />}

      {taskLogTarget && (
        <TaskLogModal
          taskId={taskLogTarget.taskId}
          date={taskLogTarget.date}
          currentStatus={taskLogTarget.currentStatus}
          onClose={() => setTaskLogTarget(null)}
          onChanged={() => {
            loadActiveTasks()
            load(scale, anchorDate)
          }}
        />
      )}

      {notificationsTarget && <NotificationsModal date={notificationsTarget} onClose={() => setNotificationsTarget(null)} />}

      {descriptionTarget && (
        <Modal title="Описание задачи" onClose={() => setDescriptionTarget(null)}>
          <TaskDescriptionView description={descriptionTarget.description} />
          <TaskLinksView links={descriptionTarget.links} />
          {descriptionTarget.attachments.length > 0 && (
            <TaskAttachments taskId={descriptionTarget.id} items={descriptionTarget.attachments} editable={false} />
          )}
        </Modal>
      )}

      {openNotifDay && (
        <DayNotificationsPanel
          date={openNotifDay}
          notifications={scheduleByDate.get(openNotifDay)?.notifications ?? []}
          isPast={dayKindOf(parseIsoDate(openNotifDay), today) === 'past'}
          onClose={() => setOpenNotifDay(null)}
          onChanged={() => load(scale, anchorDate)}
        />
      )}
    </div>
  )
}

interface NoteEditorModalProps {
  initialText: string
  onSave: (text: string) => Promise<void>
  onCancel: () => void
}

function NoteEditorModal({ initialText, onSave, onCancel }: NoteEditorModalProps) {
  const [text, setText] = useState(initialText)
  const [saving, setSaving] = useState(false)

  const handleSave = async () => {
    setSaving(true)
    try {
      await onSave(text)
    } finally {
      setSaving(false)
    }
  }

  return (
    <Modal title="Заметка дня" onClose={onCancel}>
      <div className="note-editor">
        <textarea
          className="note-editor-textarea"
          maxLength={500}
          value={text}
          onChange={(event) => setText(event.target.value)}
          autoFocus
        />
        <div className="note-editor-actions">
          <button type="button" onClick={handleSave} disabled={saving}>
            Сохранить
          </button>
          <button type="button" onClick={onCancel} disabled={saving}>
            Отмена
          </button>
        </div>
      </div>
    </Modal>
  )
}
