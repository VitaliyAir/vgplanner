import { useEffect, useRef, useState } from 'react'
import { getGoalBreadcrumb } from '../../api/goalsApi'
import { getStatistics } from '../../api/statisticsApi'
import { searchTasks } from '../../api/tasksApi'
import { ApiError } from '../../api/httpClient'
import type { Statistics, TaskListItem } from '../../api/types'
import { CollapsibleSection } from '../../components/CollapsibleSection'
import { PriorityFlag } from '../../components/PriorityFlag'
import { TaskStatusIcon } from '../../components/TaskStatusIcon'
import { taskStatusBackground } from '../../components/taskStatusBackground'
import { TASK_STATUS_LABEL } from './labels'
import { StatisticsSummary } from './StatisticsSummary'

const POLL_INTERVAL_MS = 30_000

interface RecentTasksSidebarProps {
  isActive: boolean
}

export function RecentTasksSidebar({ isActive }: RecentTasksSidebarProps) {
  const [tasks, setTasks] = useState<TaskListItem[] | null>(null)
  const [stats, setStats] = useState<Statistics | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [breadcrumbs, setBreadcrumbs] = useState<Record<string, string[]>>({})
  const breadcrumbsRef = useRef(breadcrumbs)
  breadcrumbsRef.current = breadcrumbs
  const cancelledRef = useRef(false)

  const loadBreadcrumbsFor = (goalIds: string[]) => {
    const missingGoalIds = Array.from(new Set(goalIds)).filter((id) => !(id in breadcrumbsRef.current))

    missingGoalIds.forEach((goalId) => {
      getGoalBreadcrumb(goalId)
        .then((titles) => {
          if (cancelledRef.current) {
            return
          }
          setBreadcrumbs((prev) => ({ ...prev, [goalId]: titles }))
        })
        .catch(() => {
          // Хлебные крошки не критичны для отображения — молча пропускаем неудачу.
        })
    })
  }

  // searchTasks сортирует по UpdatedAt desc на бэкенде — то, что нужно для "последних задач".
  const load = () => {
    searchTasks('', 1, 10)
      .then((data) => {
        if (cancelledRef.current) {
          return
        }
        setTasks(data.items)
        setError(null)
        loadBreadcrumbsFor(data.items.map((t) => t.goalId))
      })
      .catch((err: unknown) => {
        if (cancelledRef.current) {
          return
        }
        setError(err instanceof ApiError ? err.message : 'Не удалось загрузить последние задачи')
      })

    getStatistics()
      .then((data) => {
        if (!cancelledRef.current) {
          setStats(data)
        }
      })
      .catch(() => {
        // Статистика необязательна — молча пропускаем неудачу.
      })
  }

  useEffect(() => {
    cancelledRef.current = false
    load()
    const timer = setInterval(load, POLL_INTERVAL_MS)

    return () => {
      cancelledRef.current = true
      clearInterval(timer)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  // Переключение на эту вкладку форсирует немедленное обновление вместо
  // ожидания следующего 30-секундного тика.
  useEffect(() => {
    if (isActive) {
      load()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isActive])

  return (
    <aside className="recent-tasks-sidebar">
      {stats && <StatisticsSummary stats={stats} />}

      <CollapsibleSection title="Последние обновления">
        {error && <p className="tree-error">{error}</p>}
        {!error && tasks === null && <p className="tree-loading">Загрузка…</p>}
        {!error && tasks !== null && tasks.length === 0 && <p className="tree-empty">Нет задач</p>}

        {!error && tasks !== null && tasks.length > 0 && (
          <ul className="recent-tasks-list">
            {tasks.map((task) => {
              const crumb = breadcrumbs[task.goalId]
              return (
                <li key={task.id} className={`recent-task-item ${taskStatusBackground(task.status)}`}>
                  <PriorityFlag priority={task.priority} />
                  {crumb && crumb.length > 0 && <div className="recent-task-breadcrumb">{crumb.join(' › ')}</div>}
                  <div className="recent-task-title">{task.title}</div>
                  <div className="recent-task-pills">
                    <span className={`pill pill-task-status-${task.status}`}>
                      <TaskStatusIcon status={task.status} size={14} />
                      {TASK_STATUS_LABEL[task.status]}
                    </span>
                  </div>
                </li>
              )
            })}
          </ul>
        )}
      </CollapsibleSection>
    </aside>
  )
}
