import { useState } from 'react'
import type { FormEvent } from 'react'
import { ApiError } from '../../api/httpClient'
import { Priority } from '../../api/types'
import { MarkdownEditor } from '../../components/MarkdownEditor'
import { PRIORITY_LABEL, PRIORITY_OPTIONS } from './labels'

export interface GoalFormValues {
  title: string
  description: string
  priority: Priority
}

interface GoalFormProps {
  initialValues?: GoalFormValues
  submitLabel: string
  onSubmit: (values: GoalFormValues) => Promise<void>
  onCancel: () => void
}

const EMPTY_VALUES: GoalFormValues = {
  title: '',
  description: '',
  priority: Priority.Medium,
}

export function GoalForm({ initialValues, submitLabel, onSubmit, onCancel }: GoalFormProps) {
  const [title, setTitle] = useState(initialValues?.title ?? EMPTY_VALUES.title)
  const [description, setDescription] = useState(initialValues?.description ?? EMPTY_VALUES.description)
  const [priority, setPriority] = useState<Priority>(initialValues?.priority ?? EMPTY_VALUES.priority)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()

    const trimmedTitle = title.trim()
    if (trimmedTitle.length === 0) {
      setError('Название обязательно')
      return
    }

    setSubmitting(true)
    setError(null)

    onSubmit({ title: trimmedTitle, description: description.trim(), priority })
      .catch((err: unknown) => {
        setError(err instanceof ApiError ? err.message : 'Не удалось сохранить цель')
      })
      .finally(() => setSubmitting(false))
  }

  return (
    <form className="goal-form" onSubmit={handleSubmit}>
      <div className="goal-form-field">
        <label htmlFor="goal-form-title">Название</label>
        <input
          id="goal-form-title"
          type="text"
          value={title}
          maxLength={200}
          onChange={(event) => setTitle(event.target.value)}
          disabled={submitting}
          autoFocus
        />
      </div>

      <div className="goal-form-field">
        <label htmlFor="goal-form-description">Описание</label>
        <MarkdownEditor
          id="goal-form-description"
          value={description}
          maxLength={2000}
          compact
          onChange={setDescription}
          disabled={submitting}
        />
      </div>

      <div className="goal-form-field">
        <label htmlFor="goal-form-priority">Приоритет</label>
        <select
          id="goal-form-priority"
          value={priority}
          onChange={(event) => setPriority(Number(event.target.value) as Priority)}
          disabled={submitting}
        >
          {PRIORITY_OPTIONS.map((option) => (
            <option key={option} value={option}>
              {PRIORITY_LABEL[option]}
            </option>
          ))}
        </select>
      </div>

      {error && <p className="tree-error tree-error--inline">{error}</p>}

      <div className="goal-form-actions">
        <button type="submit" disabled={submitting}>
          {submitting ? 'Сохранение…' : submitLabel}
        </button>
        <button type="button" onClick={onCancel} disabled={submitting}>
          Отмена
        </button>
      </div>
    </form>
  )
}
