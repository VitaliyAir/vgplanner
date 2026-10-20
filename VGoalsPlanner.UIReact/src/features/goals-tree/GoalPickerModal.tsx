import { useEffect, useRef, useState } from 'react'
import { getGoalBreadcrumb, searchGoals } from '../../api/goalsApi'
import { ApiError } from '../../api/httpClient'
import type { GoalListItem } from '../../api/types'
import { Modal } from '../../components/Modal'

interface GoalPickerModalProps {
  title: string
  excludeGoalId?: string
  allowRoot: boolean
  onPick: (goalId: string | null) => void
  onCancel: () => void
}

export function GoalPickerModal({ title, excludeGoalId, allowRoot, onPick, onCancel }: GoalPickerModalProps) {
  const [query, setQuery] = useState('')
  const [results, setResults] = useState<GoalListItem[] | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [breadcrumbs, setBreadcrumbs] = useState<Record<string, string[]>>({})
  const breadcrumbsRef = useRef(breadcrumbs)
  breadcrumbsRef.current = breadcrumbs

  useEffect(() => {
    let cancelled = false
    setLoading(true)

    const timeout = setTimeout(() => {
      searchGoals(query)
        .then((page) => {
          if (!cancelled) {
            setResults(page.items)
            setError(null)

            const missingGoalIds = Array.from(new Set(page.items.map((g) => g.id))).filter(
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
                  // Хлебные крошки необязательны для выбора цели — молча пропускаем неудачу.
                })
            })
          }
        })
        .catch((err: unknown) => {
          if (!cancelled) {
            setError(err instanceof ApiError ? err.message : 'Не удалось загрузить цели')
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

  const visibleResults = (results ?? []).filter((goal) => goal.id !== excludeGoalId)

  return (
    <Modal title={title} onClose={onCancel}>
      <div className="goal-picker">
        {allowRoot && (
          <button type="button" className="goal-picker-root-option" onClick={() => onPick(null)}>
            Без родителя (сделать целью верхнего уровня)
          </button>
        )}

        <input
          type="text"
          className="goal-picker-search"
          placeholder="Поиск по названию…"
          value={query}
          onChange={(event) => setQuery(event.target.value)}
          autoFocus
        />

        {loading && <p className="tree-loading">Загрузка…</p>}
        {error && <p className="tree-error">{error}</p>}

        {!loading && !error && visibleResults.length === 0 && <p className="tree-empty">Ничего не найдено</p>}

        {!loading && !error && visibleResults.length > 0 && (
          <ul className="goal-picker-list">
            {visibleResults.map((goal) => {
              const crumb = breadcrumbs[goal.id]
              return (
                <li key={goal.id}>
                  <button type="button" className="goal-picker-item" onClick={() => onPick(goal.id)}>
                    {crumb && crumb.length > 0 && <div className="goal-picker-breadcrumb">{crumb.join(' › ')}</div>}
                    {goal.title}
                  </button>
                </li>
              )
            })}
          </ul>
        )}
      </div>
    </Modal>
  )
}
