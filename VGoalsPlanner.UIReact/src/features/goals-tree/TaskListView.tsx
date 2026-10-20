import { useEffect, useRef, useState } from 'react'
import type { MouseEvent } from 'react'
import { getGoalBreadcrumb } from '../../api/goalsApi'
import { ApiError } from '../../api/httpClient'
import { changeTaskStatus, deleteTask, filterTasks, getTaskById, updateTask } from '../../api/tasksApi'
import { scheduleTask } from '../../api/vdaysApi'
import { VTaskStatus, type Priority, type TaskDetail, type TaskListItem } from '../../api/types'
import { Modal } from '../../components/Modal'
import { PriorityFlag } from '../../components/PriorityFlag'
import { TaskAttachments } from '../../components/TaskAttachments'
import { TaskDescriptionView } from '../../components/TaskDescriptionView'
import { TaskLinksView } from '../../components/TaskLinksView'
import { TaskStatusIcon } from '../../components/TaskStatusIcon'
import { taskStatusBackground } from '../../components/taskStatusBackground'
import { TrashIcon } from '../../components/icons/TrashIcon'
import { AssignDayModal } from './AssignDayModal'
import { PRIORITY_LABEL, PRIORITY_OPTIONS, TASK_STATUS_LABEL, TASK_STATUS_OPTIONS } from './labels'
import { TaskForm, type TaskFormValues } from './TaskForm'

const DEBOUNCE_MS = 300

interface TaskListViewProps {
  showDone: boolean
}

export function TaskListView({ showDone }: TaskListViewProps) {
  const [titleFilter, setTitleFilter] = useState('')
  const [tagFilter, setTagFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState<VTaskStatus | ''>('')
  const [priorityFilter, setPriorityFilter] = useState<Priority | ''>('')
  const [regularOnly, setRegularOnly] = useState(false)

  const [tasks, setTasks] = useState<TaskListItem[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [breadcrumbs, setBreadcrumbs] = useState<Record<string, string[]>>({})
  const breadcrumbsRef = useRef(breadcrumbs)
  breadcrumbsRef.current = breadcrumbs

  const [descriptionTarget, setDescriptionTarget] = useState<TaskDetail | null>(null)
  const [editing, setEditing] = useState<TaskDetail | null>(null)
  const [assignTaskId, setAssignTaskId] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false

    const timeout = setTimeout(() => {
      filterTasks({
        title: titleFilter || undefined,
        tag: tagFilter || undefined,
        status: statusFilter === '' ? undefined : statusFilter,
        priority: priorityFilter === '' ? undefined : priorityFilter,
        regular: regularOnly || undefined,
      })
        .then((data) => {
          if (cancelled) {
            return
          }
          setTasks(data)
          setError(null)

          const missingGoalIds = Array.from(new Set(data.map((t) => t.goalId))).filter(
            (id) => !(id in breadcrumbsRef.current),
          )
          missingGoalIds.forEach((goalId) => {
            getGoalBreadcrumb(goalId)
              .then((titles) => {
                if (cancelled) {
                  return
                }
                setBreadcrumbs((prev) => ({ ...prev, [goalId]: titles }))
              })
              .catch(() => {
                // Хлебные крошки не критичны для отображения — молча пропускаем неудачу.
              })
          })
        })
        .catch((err: unknown) => {
          if (cancelled) {
            return
          }
          setError(err instanceof ApiError ? err.message : 'Не удалось загрузить задачи')
        })
    }, DEBOUNCE_MS)

    return () => {
      cancelled = true
      clearTimeout(timeout)
    }
  }, [titleFilter, tagFilter, statusFilter, priorityFilter, regularOnly])

  const handleRowClick = async (taskId: string) => {
    try {
      const detail = await getTaskById(taskId)
      setDescriptionTarget(detail)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить задачу')
    }
  }

  const handleEditClick = async (event: MouseEvent, taskId: string) => {
    event.stopPropagation()
    try {
      const detail = await getTaskById(taskId)
      setEditing(detail)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить задачу')
    }
  }

  const handleEditSubmit = async (values: TaskFormValues) => {
    if (!editing) {
      return
    }
    await updateTask(editing.id, {
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
    if (values.status !== editing.status) {
      await changeTaskStatus(editing.id, values.status)
    }
    setTasks((prev) =>
      prev?.map((t) =>
        t.id === editing.id ? { ...t, title: values.title, priority: values.priority, status: values.status, deadline: values.deadline } : t,
      ) ?? prev,
    )
    setEditing(null)
  }

  const handleAssignClick = (event: MouseEvent, taskId: string) => {
    event.stopPropagation()
    setAssignTaskId(taskId)
  }

  const handleAssignPick = async (isoDate: string) => {
    const taskId = assignTaskId
    setAssignTaskId(null)
    if (!taskId) {
      return
    }
    try {
      await scheduleTask(isoDate, taskId)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось назначить задачу на день')
    }
  }

  const handleAttachmentsChanged = (taskId: string, count: number) => {
    setTasks((prev) => prev?.map((t) => (t.id === taskId ? { ...t, attachmentCount: count } : t)) ?? prev)
  }

  const handleDeleteClick = async (event: MouseEvent, task: TaskListItem) => {
    event.stopPropagation()
    const confirmed = window.confirm(`Удалить задачу «${task.title}»?`)
    if (!confirmed) {
      return
    }
    try {
      await deleteTask(task.id)
      setTasks((prev) => prev?.filter((t) => t.id !== task.id) ?? prev)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось удалить задачу')
    }
  }

  // "Показывать выполненные" снято — завершённые задачи скрываем; явный фильтр по статусу "Завершена" важнее галочки.
  const visibleTasks =
    tasks?.filter((t) => showDone || statusFilter === VTaskStatus.Completed || t.status !== VTaskStatus.Completed) ?? null

  return (
    <div className="task-list-view">
      <div className="task-list-filters">
        <input
          type="text"
          className="task-list-filter-input"
          placeholder="Фильтр по заголовку…"
          value={titleFilter}
          onChange={(e) => setTitleFilter(e.target.value)}
        />
        <input
          type="text"
          className="task-list-filter-input"
          placeholder="Фильтр по тегу…"
          value={tagFilter}
          onChange={(e) => setTagFilter(e.target.value)}
        />
        <select
          className="task-list-filter-select"
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value === '' ? '' : (Number(e.target.value) as VTaskStatus))}
        >
          <option value="">Все статусы</option>
          {TASK_STATUS_OPTIONS.map((option) => (
            <option key={option} value={option}>
              {TASK_STATUS_LABEL[option]}
            </option>
          ))}
        </select>
        <select
          className="task-list-filter-select"
          value={priorityFilter}
          onChange={(e) => setPriorityFilter(e.target.value === '' ? '' : (Number(e.target.value) as Priority))}
        >
          <option value="">Все приоритеты</option>
          {PRIORITY_OPTIONS.map((option) => (
            <option key={option} value={option}>
              {PRIORITY_LABEL[option]}
            </option>
          ))}
        </select>
        <label className="task-list-filter-checkbox">
          <input type="checkbox" checked={regularOnly} onChange={(e) => setRegularOnly(e.target.checked)} />
          Регулярные
        </label>
      </div>

      {!error && visibleTasks !== null && <p className="task-list-count">Найдено: {visibleTasks.length}</p>}

      {error && <p className="tree-error">{error}</p>}
      {!error && visibleTasks === null && <p className="tree-loading">Загрузка…</p>}
      {!error && visibleTasks !== null && visibleTasks.length === 0 && <p className="tree-empty">Нет задач</p>}

      {!error && visibleTasks !== null && visibleTasks.length > 0 && (
        <ul className="task-list-rows">
          {visibleTasks.map((task) => {
            const crumb = breadcrumbs[task.goalId]
            return (
              <li key={task.id} className={`task-list-row ${taskStatusBackground(task.status)}`} onClick={() => handleRowClick(task.id)}>
                <PriorityFlag priority={task.priority} />
                <div className="task-list-row-main">
                  {crumb && crumb.length > 0 && <div className="recent-task-breadcrumb">{crumb.join(' › ')}</div>}
                  <div className="task-list-row-title">{task.title}</div>
                  <div className="task-list-row-pills">
                    <span className={`pill pill-task-status-${task.status}`}>
                      <TaskStatusIcon status={task.status} size={14} />
                      {TASK_STATUS_LABEL[task.status]}
                    </span>
                    {task.attachmentCount > 0 && (
                      <span className="attachment-badge" title="Вложения">
                        📎 {task.attachmentCount}
                      </span>
                    )}
                  </div>
                </div>
                <div className="task-list-row-actions">
                  <button type="button" className="icon-button" onClick={(e) => handleAssignClick(e, task.id)} title="Назначить на день">
                    📅
                  </button>
                  <button type="button" className="icon-button" onClick={(e) => handleEditClick(e, task.id)} title="Редактировать">
                    ✎
                  </button>
                  <button
                    type="button"
                    className="icon-button icon-button--danger"
                    onClick={(e) => handleDeleteClick(e, task)}
                    title="Удалить"
                  >
                    <TrashIcon />
                  </button>
                </div>
              </li>
            )
          })}
        </ul>
      )}

      {assignTaskId && <AssignDayModal taskId={assignTaskId} onPick={handleAssignPick} onCancel={() => setAssignTaskId(null)} />}

      {descriptionTarget && (
        <Modal title="Описание задачи" onClose={() => setDescriptionTarget(null)}>
          <TaskDescriptionView description={descriptionTarget.description} />
          <TaskLinksView links={descriptionTarget.links} />
          {descriptionTarget.attachments.length > 0 && (
            <TaskAttachments taskId={descriptionTarget.id} items={descriptionTarget.attachments} editable={false} />
          )}
        </Modal>
      )}

      {editing && (
        <Modal title="Редактировать задачу" persistent className="modal-panel--wide" onClose={() => setEditing(null)}>
          <TaskForm
            submitLabel="Сохранить"
            initialValues={{
              title: editing.title,
              description: editing.description ?? '',
              deadline: editing.deadline ? editing.deadline.slice(0, 10) : '',
              priority: editing.priority,
              status: editing.status,
              type: editing.type,
              estimatedMinutes: editing.estimatedMinutes != null ? String(editing.estimatedMinutes) : '',
              tags: editing.tags.join(', '),
              links: editing.links,
              recurrenceDays: editing.recurrenceDays,
            }}
            attachments={{
              taskId: editing.id,
              items: editing.attachments,
              onChange: (items) => handleAttachmentsChanged(editing.id, items.length),
            }}
            onSubmit={handleEditSubmit}
            onCancel={() => setEditing(null)}
          />
        </Modal>
      )}
    </div>
  )
}
