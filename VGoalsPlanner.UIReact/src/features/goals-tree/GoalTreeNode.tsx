import { useReducer, useState } from 'react'
import { createGoal, deleteGoal, getGoalById, moveGoal, reorderGoals, updateGoal } from '../../api/goalsApi'
import { createTask, reorderTasksInGoal } from '../../api/tasksApi'
import { ApiError } from '../../api/httpClient'
import { VGoalStatus, VTaskStatus, type GoalDetail, type GoalListItem, type GoalTaskSummary } from '../../api/types'
import { MarkdownView } from '../../components/MarkdownView'
import { Modal } from '../../components/Modal'
import { PositionInput, type ListPosition } from '../../components/PositionInput'
import { PriorityFlag } from '../../components/PriorityFlag'
import { reorderToVisibleIndex } from '../../components/reorder'
import { TrashIcon } from '../../components/icons/TrashIcon'
import { InfoIcon } from '../../components/icons/InfoIcon'
import { GoalForm, type GoalFormValues } from './GoalForm'
import { GoalPickerModal } from './GoalPickerModal'
import { TaskForm, type TaskFormValues } from './TaskForm'
import { TaskLeaf } from './TaskLeaf'
import { GOAL_STATUS_LABEL } from './labels'
import { useShowDone } from './ShowDoneContext'

type Mode = 'view' | 'edit' | 'add-child' | 'add-task' | 'details'
type EditableMode = Exclude<Mode, 'view'>

interface State {
  expanded: boolean
  detail: GoalDetail | null
  mode: Mode
  loading: boolean
  busy: boolean
  error: string | null
}

type EditPatch = Pick<GoalDetail, 'title' | 'description' | 'priority'>

type Action =
  | { type: 'TOGGLE_EXPANDED' }
  | { type: 'EXPAND_LOAD_START' }
  | { type: 'EXPAND_LOAD_SUCCESS'; detail: GoalDetail }
  | { type: 'EXPAND_LOAD_FAILURE'; error: string }
  | { type: 'OPEN_MODE'; mode: EditableMode; expand?: boolean }
  | { type: 'ACTION_LOAD_START' }
  | { type: 'ACTION_LOAD_SUCCESS'; detail: GoalDetail; mode: EditableMode; expand?: boolean }
  | { type: 'ACTION_LOAD_FAILURE'; error: string }
  | { type: 'EDIT_SAVED'; patch: EditPatch }
  | { type: 'CHILD_SAVED'; detail: GoalDetail }
  | { type: 'DELETE_START' }
  | { type: 'DELETE_FAILURE'; error: string }
  | { type: 'MOVE_FAILURE'; error: string }
  | { type: 'CLOSE' }
  | { type: 'CHILD_GOAL_CHANGED'; updated: GoalListItem }
  | { type: 'CHILD_GOAL_DELETED'; id: string }
  | { type: 'CHILD_TASK_CHANGED'; updated: GoalTaskSummary }
  | { type: 'CHILD_TASK_DELETED'; id: string }
  | { type: 'CHILD_TASKS_REORDERED'; childTasks: GoalTaskSummary[] }
  | { type: 'CHILD_GOALS_REORDERED'; childGoals: GoalListItem[] }

const initialState: State = {
  expanded: false,
  detail: null,
  mode: 'view',
  loading: false,
  busy: false,
  error: null,
}

function reducer(state: State, action: Action): State {
  switch (action.type) {
    case 'TOGGLE_EXPANDED':
      return { ...state, expanded: !state.expanded }
    case 'EXPAND_LOAD_START':
      return { ...state, loading: true, error: null }
    case 'EXPAND_LOAD_SUCCESS':
      return { ...state, detail: action.detail, expanded: true, loading: false }
    case 'EXPAND_LOAD_FAILURE':
      return { ...state, loading: false, error: action.error }
    case 'OPEN_MODE':
      return { ...state, mode: action.mode, expanded: action.expand ? true : state.expanded, error: null }
    case 'ACTION_LOAD_START':
      return { ...state, busy: true, error: null }
    case 'ACTION_LOAD_SUCCESS':
      return {
        ...state,
        detail: action.detail,
        mode: action.mode,
        expanded: action.expand ? true : state.expanded,
        busy: false,
      }
    case 'ACTION_LOAD_FAILURE':
      return { ...state, busy: false, error: action.error }
    case 'EDIT_SAVED':
      return { ...state, mode: 'view', detail: state.detail ? { ...state.detail, ...action.patch } : state.detail }
    case 'CHILD_SAVED':
      return { ...state, mode: 'view', detail: action.detail }
    case 'DELETE_START':
      return { ...state, busy: true, error: null }
    case 'DELETE_FAILURE':
      return { ...state, busy: false, error: action.error }
    case 'MOVE_FAILURE':
      return { ...state, error: action.error }
    case 'CLOSE':
      return { ...state, mode: 'view' }
    case 'CHILD_GOAL_CHANGED':
      return state.detail
        ? {
            ...state,
            detail: {
              ...state.detail,
              childGoals: state.detail.childGoals.map((g) => (g.id === action.updated.id ? action.updated : g)),
            },
          }
        : state
    case 'CHILD_GOAL_DELETED':
      return state.detail
        ? { ...state, detail: { ...state.detail, childGoals: state.detail.childGoals.filter((g) => g.id !== action.id) } }
        : state
    case 'CHILD_TASK_CHANGED':
      return state.detail
        ? {
            ...state,
            detail: {
              ...state.detail,
              childTasks: state.detail.childTasks.map((t) => (t.id === action.updated.id ? action.updated : t)),
            },
          }
        : state
    case 'CHILD_TASK_DELETED':
      return state.detail
        ? { ...state, detail: { ...state.detail, childTasks: state.detail.childTasks.filter((t) => t.id !== action.id) } }
        : state
    case 'CHILD_TASKS_REORDERED':
      return state.detail ? { ...state, detail: { ...state.detail, childTasks: action.childTasks } } : state
    case 'CHILD_GOALS_REORDERED':
      return state.detail ? { ...state, detail: { ...state.detail, childGoals: action.childGoals } } : state
    default:
      return state
  }
}

interface GoalTreeNodeProps {
  goal: GoalListItem
  onChanged?: (updated: GoalListItem) => void
  onDeleted?: (id: string) => void
  position?: ListPosition
}

export function GoalTreeNode({ goal, onChanged, onDeleted, position }: GoalTreeNodeProps) {
  const [state, dispatch] = useReducer(reducer, initialState)
  const { expanded, detail, mode, loading, busy, error } = state
  const [movePickerOpen, setMovePickerOpen] = useState(false)
  const showDone = useShowDone()

  // Фильтрация по "показывать выполненные" — только для отображения, canExpand
  // (см. ниже) сознательно считается по несмфильтрованным данным, иначе стрелка
  // разворачивания то появлялась бы, то исчезала при переключении фильтра.
  const visibleChildGoals = detail ? detail.childGoals.filter((g) => showDone || g.status !== VGoalStatus.Completed) : []
  const visibleChildTasks = detail ? detail.childTasks.filter((t) => showDone || t.status !== VTaskStatus.Completed) : []

  // У свежедобавленного (в этой же сессии) ребёнка родитель мог изначально
  // не показывать стрелку (hasChildGoals/hasChildTasks пришли из списка ДО
  // добавления) — учитываем уже загруженный detail как источник истины тоже.
  const canExpand =
    goal.hasChildGoals ||
    goal.hasChildTasks ||
    (detail !== null && (detail.childGoals.length > 0 || detail.childTasks.length > 0))

  const handleToggle = () => {
    if (!canExpand || loading) {
      return
    }

    if (detail !== null) {
      dispatch({ type: 'TOGGLE_EXPANDED' })
      return
    }

    dispatch({ type: 'EXPAND_LOAD_START' })
    getGoalById(goal.id)
      .then((data) => dispatch({ type: 'EXPAND_LOAD_SUCCESS', detail: data }))
      .catch((err: unknown) => {
        dispatch({ type: 'EXPAND_LOAD_FAILURE', error: err instanceof ApiError ? err.message : 'Не удалось загрузить цель' })
      })
  }

  // Общий "открыть режим" для edit/add-child/add-task: если детали узла уже
  // загружены — переключаемся сразу, иначе сначала подгружаем (OPEN_MODE vs
  // ACTION_LOAD_*). Разные потоки отличаются только целевым mode и тем, нужно
  // ли форсировать expanded.
  const openInMode = (targetMode: EditableMode, expand?: boolean) => {
    if (busy) {
      return
    }

    if (detail !== null) {
      dispatch({ type: 'OPEN_MODE', mode: targetMode, expand })
      return
    }

    dispatch({ type: 'ACTION_LOAD_START' })
    getGoalById(goal.id)
      .then((data) => dispatch({ type: 'ACTION_LOAD_SUCCESS', detail: data, mode: targetMode, expand }))
      .catch((err: unknown) => {
        dispatch({ type: 'ACTION_LOAD_FAILURE', error: err instanceof ApiError ? err.message : 'Не удалось загрузить цель' })
      })
  }

  const openEdit = () => openInMode('edit')
  const openAddChild = () => openInMode('add-child', true)
  const openAddTask = () => openInMode('add-task', true)
  const openDetails = () => openInMode('details')

  const handleEditSubmit = async (values: GoalFormValues) => {
    await updateGoal(goal.id, {
      title: values.title,
      description: values.description || null,
      priority: values.priority,
    })

    dispatch({
      type: 'EDIT_SAVED',
      patch: { title: values.title, description: values.description || null, priority: values.priority },
    })
    onChanged?.({ ...goal, title: values.title, priority: values.priority })
  }

  const handleAddChildSubmit = async (values: GoalFormValues) => {
    await createGoal({
      title: values.title,
      description: values.description || null,
      priority: values.priority,
      parentGoalId: goal.id,
    })

    // Перезапрашиваем узел, а не собираем новый GoalListItem вручную —
    // createGoal возвращает только id, а форму (status/orderIndex/...) должен
    // выставить бэкенд.
    const refreshed = await getGoalById(goal.id)
    dispatch({ type: 'CHILD_SAVED', detail: refreshed })
  }

  const handleAddTaskSubmit = async (values: TaskFormValues) => {
    await createTask({
      goalId: goal.id,
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

    // Как и для add-child: перезапрашиваем узел вместо ручной сборки
    // GoalTaskSummary — createTask отдаёт только id.
    const refreshed = await getGoalById(goal.id)
    dispatch({ type: 'CHILD_SAVED', detail: refreshed })
  }

  const handleDelete = () => {
    if (busy) {
      return
    }

    const confirmed = window.confirm(`Удалить цель «${goal.title}» со всем содержимым?`)
    if (!confirmed) {
      return
    }

    dispatch({ type: 'DELETE_START' })

    deleteGoal(goal.id)
      .then(() => onDeleted?.(goal.id))
      .catch((err: unknown) => {
        dispatch({ type: 'DELETE_FAILURE', error: err instanceof ApiError ? err.message : 'Не удалось удалить цель' })
      })
  }

  const handleMovePick = async (newParentGoalId: string | null) => {
    setMovePickerOpen(false)
    try {
      await moveGoal(goal.id, newParentGoalId)
      onDeleted?.(goal.id)
    } catch (err) {
      dispatch({ type: 'MOVE_FAILURE', error: err instanceof ApiError ? err.message : 'Не удалось перенести цель' })
    }
  }

  const handleChildChanged = (updated: GoalListItem) => dispatch({ type: 'CHILD_GOAL_CHANGED', updated })
  const handleChildDeleted = (id: string) => dispatch({ type: 'CHILD_GOAL_DELETED', id })
  const handleTaskChanged = (updated: GoalTaskSummary) => dispatch({ type: 'CHILD_TASK_CHANGED', updated })
  const handleTaskDeleted = (id: string) => dispatch({ type: 'CHILD_TASK_DELETED', id })

  // Сохраняем порядок по полному detail.childTasks (скрытые выполненные остаются на местах),
  // а позицию targetIndex считаем среди видимых visibleChildTasks.
  const handleReorderTask = async (taskId: string, targetIndex: number) => {
    if (!detail) {
      return
    }
    const reordered = reorderToVisibleIndex(detail.childTasks, visibleChildTasks, (t) => t.id, taskId, targetIndex)
    if (!reordered) {
      return
    }
    try {
      await reorderTasksInGoal(goal.id, reordered.map((t) => t.id))
      dispatch({ type: 'CHILD_TASKS_REORDERED', childTasks: reordered })
    } catch (err) {
      dispatch({ type: 'ACTION_LOAD_FAILURE', error: err instanceof ApiError ? err.message : 'Не удалось изменить порядок задач' })
    }
  }

  const handleReorderChildGoal = async (goalId: string, targetIndex: number) => {
    if (!detail) {
      return
    }
    const reordered = reorderToVisibleIndex(detail.childGoals, visibleChildGoals, (g) => g.id, goalId, targetIndex)
    if (!reordered) {
      return
    }
    try {
      await reorderGoals(goal.id, reordered.map((g) => g.id))
      dispatch({ type: 'CHILD_GOALS_REORDERED', childGoals: reordered })
    } catch (err) {
      dispatch({ type: 'ACTION_LOAD_FAILURE', error: err instanceof ApiError ? err.message : 'Не удалось изменить порядок целей' })
    }
  }

  return (
    <li className="goal-node">
      <PriorityFlag priority={goal.priority} />
      <div className="goal-node-row">
        <button
          type="button"
          className={`goal-node-toggle${canExpand ? '' : ' goal-node-toggle--hidden'}`}
          onClick={handleToggle}
          disabled={!canExpand || loading}
          aria-label={expanded ? 'Свернуть' : 'Развернуть'}
        >
          {loading ? '…' : canExpand ? (expanded ? '▾' : '▸') : ''}
        </button>

        {position && <PositionInput {...position} disabled={busy} />}

        <button type="button" className="icon-button icon-button--info" onClick={openDetails} disabled={busy} title="Показать описание">
          <InfoIcon />
        </button>

        <span className="goal-node-title">{goal.title}</span>

        <span className="goal-node-actions">
          <span className="add-stack">
            <button type="button" className="add-btn add-btn--goal" onClick={openAddChild} disabled={busy} title="Добавить дочернюю цель">
              +
            </button>
            <button type="button" className="add-btn add-btn--task" onClick={openAddTask} disabled={busy} title="Добавить задачу">
              +
            </button>
          </span>
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

      <div className="node-foot">
        <span className={`pill pill-goal-status-${goal.status}`}>{GOAL_STATUS_LABEL[goal.status]}</span>
      </div>

      {error && <p className="tree-error tree-error--inline">{error}</p>}

      {expanded && detail && (
        <ul className="goal-node-children">
          {visibleChildGoals.map((child, index) => (
            <GoalTreeNode
              key={child.id}
              goal={child}
              onChanged={handleChildChanged}
              onDeleted={handleChildDeleted}
              position={{ index, count: visibleChildGoals.length, onMoveTo: (target) => handleReorderChildGoal(child.id, target) }}
            />
          ))}

          {visibleChildTasks.map((task, index) => (
            <TaskLeaf
              key={task.id}
              task={task}
              onChanged={handleTaskChanged}
              onDeleted={handleTaskDeleted}
              position={{ index, count: visibleChildTasks.length, onMoveTo: (target) => handleReorderTask(task.id, target) }}
            />
          ))}

          {visibleChildGoals.length === 0 && visibleChildTasks.length === 0 && (
            <li className="task-leaf task-leaf--empty">Пусто</li>
          )}
        </ul>
      )}

      {mode === 'edit' && detail && (
        <Modal title="Редактировать цель" className="modal-panel--wide" onClose={() => dispatch({ type: 'CLOSE' })}>
          <GoalForm
            submitLabel="Сохранить"
            initialValues={{ title: detail.title, description: detail.description ?? '', priority: detail.priority }}
            onSubmit={handleEditSubmit}
            onCancel={() => dispatch({ type: 'CLOSE' })}
          />
        </Modal>
      )}

      {mode === 'add-child' && detail && (
        <Modal title="Новая дочерняя цель" className="modal-panel--wide" onClose={() => dispatch({ type: 'CLOSE' })}>
          <GoalForm submitLabel="Добавить" onSubmit={handleAddChildSubmit} onCancel={() => dispatch({ type: 'CLOSE' })} />
        </Modal>
      )}

      {mode === 'add-task' && detail && (
        <Modal title="Новая задача" persistent className="modal-panel--wide" onClose={() => dispatch({ type: 'CLOSE' })}>
          <TaskForm submitLabel="Добавить" onSubmit={handleAddTaskSubmit} onCancel={() => dispatch({ type: 'CLOSE' })} />
        </Modal>
      )}

      {mode === 'details' && detail && (
        <Modal title="Описание цели" onClose={() => dispatch({ type: 'CLOSE' })}>
          {detail.description ? <MarkdownView source={detail.description} /> : <p>Без описания</p>}
        </Modal>
      )}

      {movePickerOpen && (
        <GoalPickerModal
          title="Перенести цель"
          excludeGoalId={goal.id}
          allowRoot
          onPick={handleMovePick}
          onCancel={() => setMovePickerOpen(false)}
        />
      )}
    </li>
  )
}
