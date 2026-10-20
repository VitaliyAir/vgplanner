import { useReducer, useState } from 'react'
import { changeTaskStatus, getTaskById, moveTask, updateTask, deleteTask } from '../../api/tasksApi'
import { scheduleTask } from '../../api/vdaysApi'
import { ApiError } from '../../api/httpClient'
import type { GoalTaskSummary, TaskAttachment, TaskDetail } from '../../api/types'
import { Modal } from '../../components/Modal'
import { PositionInput, type ListPosition } from '../../components/PositionInput'
import { PriorityFlag } from '../../components/PriorityFlag'
import { TaskAttachments } from '../../components/TaskAttachments'
import { TaskDescriptionView } from '../../components/TaskDescriptionView'
import { TaskLinksView } from '../../components/TaskLinksView'
import { TaskStatusIcon } from '../../components/TaskStatusIcon'
import { taskStatusBackground } from '../../components/taskStatusBackground'
import { TrashIcon } from '../../components/icons/TrashIcon'
import { InfoIcon } from '../../components/icons/InfoIcon'
import { AssignDayModal } from './AssignDayModal'
import { GoalPickerModal } from './GoalPickerModal'
import { TaskForm, type TaskFormValues } from './TaskForm'
import { TASK_STATUS_LABEL } from './labels'

type Mode = 'view' | 'edit' | 'details'
type EditableMode = Exclude<Mode, 'view'>

interface State {
  mode: Mode
  detail: TaskDetail | null
  busy: boolean
  error: string | null
}

type EditPatch = Pick<
  TaskDetail,
  'title' | 'description' | 'deadline' | 'priority' | 'status' | 'type' | 'estimatedMinutes' | 'tags' | 'links' | 'recurrenceDays'
>

type Action =
  | { type: 'OPEN_MODE'; mode: EditableMode }
  | { type: 'LOAD_START' }
  | { type: 'LOAD_SUCCESS'; detail: TaskDetail; mode: EditableMode }
  | { type: 'LOAD_FAILURE'; error: string }
  | { type: 'EDIT_SAVED'; patch: EditPatch }
  | { type: 'ATTACHMENTS_CHANGED'; attachments: TaskAttachment[] }
  | { type: 'DELETE_START' }
  | { type: 'DELETE_FAILURE'; error: string }
  | { type: 'MOVE_FAILURE'; error: string }
  | { type: 'ASSIGN_DAY_FAILURE'; error: string }
  | { type: 'CLOSE' }

const initialState: State = { mode: 'view', detail: null, busy: false, error: null }

function reducer(state: State, action: Action): State {
  switch (action.type) {
    case 'OPEN_MODE':
      return { ...state, mode: action.mode, error: null }
    case 'LOAD_START':
      return { ...state, busy: true, error: null }
    case 'LOAD_SUCCESS':
      return { ...state, detail: action.detail, mode: action.mode, busy: false }
    case 'LOAD_FAILURE':
      return { ...state, busy: false, error: action.error }
    case 'EDIT_SAVED':
      return { ...state, mode: 'view', detail: state.detail ? { ...state.detail, ...action.patch } : state.detail }
    case 'ATTACHMENTS_CHANGED':
      return state.detail ? { ...state, detail: { ...state.detail, attachments: action.attachments } } : state
    case 'DELETE_START':
      return { ...state, busy: true, error: null }
    case 'DELETE_FAILURE':
      return { ...state, busy: false, error: action.error }
    case 'MOVE_FAILURE':
      return { ...state, error: action.error }
    case 'ASSIGN_DAY_FAILURE':
      return { ...state, error: action.error }
    case 'CLOSE':
      return { ...state, mode: 'view' }
    default:
      return state
  }
}

interface TaskLeafProps {
  task: GoalTaskSummary
  onChanged?: (updated: GoalTaskSummary) => void
  onDeleted?: (id: string) => void
  position?: ListPosition
}

export function TaskLeaf({ task, onChanged, onDeleted, position }: TaskLeafProps) {
  const [state, dispatch] = useReducer(reducer, initialState)
  const { mode, detail, busy, error } = state
  const [movePickerOpen, setMovePickerOpen] = useState(false)
  const [assignDayOpen, setAssignDayOpen] = useState(false)

  const openInMode = (targetMode: EditableMode) => {
    if (busy) {
      return
    }

    if (detail !== null) {
      dispatch({ type: 'OPEN_MODE', mode: targetMode })
      return
    }

    dispatch({ type: 'LOAD_START' })
    getTaskById(task.id)
      .then((data) => dispatch({ type: 'LOAD_SUCCESS', detail: data, mode: targetMode }))
      .catch((err: unknown) => {
        dispatch({ type: 'LOAD_FAILURE', error: err instanceof ApiError ? err.message : 'Не удалось загрузить задачу' })
      })
  }

  const openEdit = () => openInMode('edit')
  const openDetails = () => openInMode('details')

  const handleEditSubmit = async (values: TaskFormValues) => {
    await updateTask(task.id, {
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

    if (detail && values.status !== detail.status) {
      await changeTaskStatus(task.id, values.status)
    }

    dispatch({
      type: 'EDIT_SAVED',
      patch: {
        title: values.title,
        description: values.description,
        deadline: values.deadline,
        priority: values.priority,
        status: values.status,
        type: values.type,
        estimatedMinutes: values.estimatedMinutes,
        tags: values.tags,
        links: values.links,
        recurrenceDays: values.recurrenceDays,
      },
    })
    onChanged?.({ ...task, title: values.title, priority: values.priority, status: values.status, deadline: values.deadline })
  }

  const handleAttachmentsChanged = (attachments: TaskAttachment[]) => {
    dispatch({ type: 'ATTACHMENTS_CHANGED', attachments })
    onChanged?.({ ...task, attachmentCount: attachments.length })
  }

  const handleDelete = () => {
    if (busy) {
      return
    }

    const confirmed = window.confirm(`Удалить задачу «${task.title}»?`)
    if (!confirmed) {
      return
    }

    dispatch({ type: 'DELETE_START' })

    deleteTask(task.id)
      .then(() => onDeleted?.(task.id))
      .catch((err: unknown) => {
        dispatch({ type: 'DELETE_FAILURE', error: err instanceof ApiError ? err.message : 'Не удалось удалить задачу' })
      })
  }

  const handleMovePick = async (newGoalId: string | null) => {
    setMovePickerOpen(false)
    if (newGoalId === null) {
      return
    }
    try {
      await moveTask(task.id, newGoalId)
      onDeleted?.(task.id)
    } catch (err) {
      dispatch({ type: 'MOVE_FAILURE', error: err instanceof ApiError ? err.message : 'Не удалось перенести задачу' })
    }
  }

  const handleAssignDay = async (isoDate: string) => {
    setAssignDayOpen(false)
    try {
      await scheduleTask(isoDate, task.id)
    } catch (err) {
      dispatch({ type: 'ASSIGN_DAY_FAILURE', error: err instanceof ApiError ? err.message : 'Не удалось назначить задачу на день' })
    }
  }

  return (
    <li className={`task-leaf ${taskStatusBackground(task.status)}`}>
      <PriorityFlag priority={task.priority} />
      <div className="task-leaf-row">
        {position && <PositionInput {...position} disabled={busy} />}

        <button type="button" className="icon-button icon-button--info" onClick={openDetails} disabled={busy} title="Показать описание">
          <InfoIcon />
        </button>

        <span className="task-leaf-title">{task.title}</span>

        {task.attachmentCount > 0 && (
          <span className="attachment-badge" title="Вложения">
            📎 {task.attachmentCount}
          </span>
        )}

        <span className="task-leaf-actions">
          <button type="button" className="icon-button" onClick={() => setAssignDayOpen(true)} disabled={busy} title="Назначить на день">
            📅
          </button>
          <button type="button" className="icon-button" onClick={() => setMovePickerOpen(true)} disabled={busy} title="Перенести">
            ⇄
          </button>
          <button type="button" className="icon-button" onClick={openEdit} disabled={busy} title="Редактировать">
            ✎
          </button>
          <button type="button" className="icon-button icon-button--danger" onClick={handleDelete} disabled={busy} title="Удалить">
            <TrashIcon />
          </button>
        </span>
      </div>

      {task.tags.length > 0 && (
        <div className="task-leaf-tags">
          {task.tags.map((tag) => (
            <span key={tag} className="task-tag-pill">
              {tag}
            </span>
          ))}
        </div>
      )}

      <div className="node-foot">
        <span className={`pill pill-task-status-${task.status}`}>
          <TaskStatusIcon status={task.status} size={14} />
          {TASK_STATUS_LABEL[task.status]}
        </span>
      </div>

      {error && <p className="tree-error tree-error--inline">{error}</p>}

      {mode === 'edit' && detail && (
        <Modal title="Редактировать задачу" persistent className="modal-panel--wide" onClose={() => dispatch({ type: 'CLOSE' })}>
          <TaskForm
            submitLabel="Сохранить"
            initialValues={{
              title: detail.title,
              description: detail.description ?? '',
              deadline: detail.deadline ? detail.deadline.slice(0, 10) : '',
              priority: detail.priority,
              status: detail.status,
              type: detail.type,
              estimatedMinutes: detail.estimatedMinutes != null ? String(detail.estimatedMinutes) : '',
              tags: detail.tags.join(', '),
              links: detail.links,
              recurrenceDays: detail.recurrenceDays,
            }}
            attachments={{ taskId: task.id, items: detail.attachments, onChange: handleAttachmentsChanged }}
            onSubmit={handleEditSubmit}
            onCancel={() => dispatch({ type: 'CLOSE' })}
          />
        </Modal>
      )}

      {mode === 'details' && detail && (
        <Modal title="Описание задачи" onClose={() => dispatch({ type: 'CLOSE' })}>
          <TaskDescriptionView description={detail.description} />
          <TaskLinksView links={detail.links} />
          {detail.attachments.length > 0 && <TaskAttachments taskId={detail.id} items={detail.attachments} editable={false} />}
        </Modal>
      )}

      {movePickerOpen && (
        <GoalPickerModal title="Перенести задачу" allowRoot={false} onPick={handleMovePick} onCancel={() => setMovePickerOpen(false)} />
      )}

      {assignDayOpen && <AssignDayModal taskId={task.id} onPick={handleAssignDay} onCancel={() => setAssignDayOpen(false)} />}
    </li>
  )
}
