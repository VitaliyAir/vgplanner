import { useEffect, useState } from 'react'
import { createGoal, getGoalById, getRootGoals, reorderGoals } from '../../api/goalsApi'
import { ApiError } from '../../api/httpClient'
import { VGoalStatus, type GoalListItem } from '../../api/types'
import { Modal } from '../../components/Modal'
import { ListViewIcon } from '../../components/icons/ListViewIcon'
import { TreeViewIcon } from '../../components/icons/TreeViewIcon'
import { reorderToVisibleIndex } from '../../components/reorder'
import { GoalForm, type GoalFormValues } from './GoalForm'
import { GoalTreeNode } from './GoalTreeNode'
import { RecentTasksSidebar } from './RecentTasksSidebar'
import { ShowDoneContext } from './ShowDoneContext'
import { TaskListView } from './TaskListView'
import './goalsTree.css'

type ViewMode = 'tree' | 'list'

const VIEW_MODE_KEY = 'vgp.viewMode'
const SHOW_DONE_KEY = 'vgp.showDone'

function readViewMode(): ViewMode {
  return sessionStorage.getItem(VIEW_MODE_KEY) === 'list' ? 'list' : 'tree'
}

function readShowDone(): boolean {
  const stored = sessionStorage.getItem(SHOW_DONE_KEY)
  return stored === null ? false : stored === '1'
}

interface GoalsAndTaskViewProps {
  isActive: boolean
}

export function GoalsAndTaskView({ isActive }: GoalsAndTaskViewProps) {
  const [roots, setRoots] = useState<GoalListItem[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [creatingRoot, setCreatingRoot] = useState(false)
  const [viewMode, setViewMode] = useState<ViewMode>(readViewMode)
  const [showDone, setShowDone] = useState<boolean>(readShowDone)

  useEffect(() => {
    sessionStorage.setItem(VIEW_MODE_KEY, viewMode)
  }, [viewMode])

  useEffect(() => {
    sessionStorage.setItem(SHOW_DONE_KEY, showDone ? '1' : '0')
  }, [showDone])

  useEffect(() => {
    let cancelled = false

    getRootGoals()
      .then((data) => {
        if (!cancelled) {
          setRoots(data)
        }
      })
      .catch((err: unknown) => {
        if (cancelled) {
          return
        }
        setError(err instanceof ApiError ? err.message : 'Не удалось загрузить цели')
      })

    return () => {
      cancelled = true
    }
  }, [])

  const handleCreateRoot = async (values: GoalFormValues) => {
    const created = await createGoal({
      title: values.title,
      description: values.description || null,
      priority: values.priority,
    })

    // createGoal отдаёт только id — подтягиваем актуальный GoalListItem одним
    // запросом, чтобы не гадать со status/orderIndex по умолчанию на клиенте.
    const full = await getGoalById(created.id)
    setRoots((prev) => [
      ...(prev ?? []),
      {
        id: full.id,
        title: full.title,
        priority: full.priority,
        status: full.status,
        parentGoalId: full.parentGoalId,
        orderIndex: full.orderIndex,
        hasChildGoals: false,
        hasChildTasks: false,
      },
    ])
    setCreatingRoot(false)
  }

  const handleRootChanged = (updated: GoalListItem) => {
    setRoots((prev) => prev?.map((g) => (g.id === updated.id ? updated : g)) ?? prev)
  }

  const handleRootDeleted = (id: string) => {
    setRoots((prev) => prev?.filter((g) => g.id !== id) ?? prev)
  }

  const visibleRoots = roots?.filter((g) => showDone || g.status !== VGoalStatus.Completed) ?? roots

  // Порядок сохраняем по полному roots (скрытые выполненные остаются на своих местах),
  // а позицию targetIndex считаем среди видимых.
  const handleReorderRoot = async (goalId: string, targetIndex: number) => {
    if (!roots || !visibleRoots) {
      return
    }
    const reordered = reorderToVisibleIndex(roots, visibleRoots, (g) => g.id, goalId, targetIndex)
    if (!reordered) {
      return
    }
    try {
      await reorderGoals(null, reordered.map((g) => g.id))
      setRoots(reordered)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось изменить порядок целей')
    }
  }

  return (
    <div className="goals-page">
      <RecentTasksSidebar isActive={isActive} />

      <div className="goals-and-task-view">
        <div className="goals-and-task-view-header">
          <div className="view-switch">
            <button
              type="button"
              className={viewMode === 'tree' ? 'active' : ''}
              onClick={() => setViewMode('tree')}
              title="Дерево"
              aria-label="Дерево"
              aria-pressed={viewMode === 'tree'}
            >
              <TreeViewIcon />
            </button>
            <button
              type="button"
              className={viewMode === 'list' ? 'active' : ''}
              onClick={() => setViewMode('list')}
              title="Список задач"
              aria-label="Список задач"
              aria-pressed={viewMode === 'list'}
            >
              <ListViewIcon />
            </button>
          </div>

          <h1>Цели и задачи</h1>

          <div className="header-right">
            {!creatingRoot && (
              <button type="button" className="root-add-button" onClick={() => setCreatingRoot(true)}>
                <span className="add-btn add-btn--goal" aria-hidden="true">+</span>
                <span>Новая цель</span>
              </button>
            )}

            <label className="view-controls-checkbox">
              <input type="checkbox" checked={showDone} onChange={(e) => setShowDone(e.target.checked)} />
              Показывать выполненные
            </label>
          </div>
        </div>

        {creatingRoot && (
          <Modal title="Новая цель" className="modal-panel--wide" onClose={() => setCreatingRoot(false)}>
            <GoalForm submitLabel="Создать" onSubmit={handleCreateRoot} onCancel={() => setCreatingRoot(false)} />
          </Modal>
        )}

        {viewMode === 'list' && <TaskListView showDone={showDone} />}

        {viewMode === 'tree' && (
          <ShowDoneContext.Provider value={showDone}>
            {error && <p className="tree-error">{error}</p>}
            {!error && visibleRoots === null && <p className="tree-loading">Загрузка…</p>}
            {!error && visibleRoots !== null && visibleRoots.length === 0 && !creatingRoot && (
              <p className="tree-empty">Нет целей</p>
            )}

            {!error && visibleRoots !== null && visibleRoots.length > 0 && (
              <ul className="goal-tree-root">
                {visibleRoots.map((goal, index) => (
                  <GoalTreeNode
                    key={goal.id}
                    goal={goal}
                    onChanged={handleRootChanged}
                    onDeleted={handleRootDeleted}
                    position={{ index, count: visibleRoots.length, onMoveTo: (target) => handleReorderRoot(goal.id, target) }}
                  />
                ))}
              </ul>
            )}
          </ShowDoneContext.Provider>
        )}
      </div>
    </div>
  )
}
