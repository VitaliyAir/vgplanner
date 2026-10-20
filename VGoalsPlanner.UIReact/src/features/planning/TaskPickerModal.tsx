import { useEffect, useState } from 'react'
import { searchTasks } from '../../api/tasksApi'
import { ApiError } from '../../api/httpClient'
import type { TaskListItem } from '../../api/types'
import { Modal } from '../../components/Modal'
import { PRIORITY_LABEL } from '../goals-tree/labels'

interface TaskPickerModalProps {
  excludeTaskIds: string[]
  onPick: (taskId: string) => void
  onCancel: () => void
}

export function TaskPickerModal({ excludeTaskIds, onPick, onCancel }: TaskPickerModalProps) {
  const [query, setQuery] = useState('')
  const [results, setResults] = useState<TaskListItem[] | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    setLoading(true)

    const timeout = setTimeout(() => {
      searchTasks(query)
        .then((page) => {
          if (!cancelled) {
            setResults(page.items)
            setError(null)
          }
        })
        .catch((err: unknown) => {
          if (!cancelled) {
            setError(err instanceof ApiError ? err.message : 'Не удалось загрузить задачи')
          }
        })
        .finally(() => {
          if (!cancelled) {
            setLoading(false)
          }
        })
    }, 300)

    return () => {
      cancelled = true
      clearTimeout(timeout)
    }
  }, [query])

  const visibleResults = (results ?? []).filter((task) => !excludeTaskIds.includes(task.id))

  return (
    <Modal title="Добавить задачу на день" onClose={onCancel}>
      <div className="task-picker">
        <input
          type="text"
          className="task-picker-search"
          placeholder="Поиск по названию или тегам…"
          value={query}
          onChange={(event) => setQuery(event.target.value)}
          autoFocus
        />

        {loading && <p className="tree-loading">Загрузка…</p>}
        {error && <p className="tree-error">{error}</p>}

        {!loading && !error && visibleResults.length === 0 && <p className="tree-empty">Ничего не найдено</p>}

        {!loading && !error && visibleResults.length > 0 && (
          <ul className="task-picker-list">
            {visibleResults.map((task) => (
              <li key={task.id}>
                <button type="button" className="task-picker-item" onClick={() => onPick(task.id)}>
                  <span className="task-picker-item-title">{task.title}</span>
                  <span className="task-picker-item-meta">
                    {PRIORITY_LABEL[task.priority]}
                    {task.tags.length > 0 ? ` · ${task.tags.join(', ')}` : ''}
                  </span>
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>
    </Modal>
  )
}
