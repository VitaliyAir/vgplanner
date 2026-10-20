import { useState } from 'react'
import type { FormEvent } from 'react'
import { ApiError } from '../../api/httpClient'
import { Priority, VTaskStatus, VTaskType } from '../../api/types'
import type { TaskAttachment, TaskLink } from '../../api/types'
import { MarkdownEditor } from '../../components/MarkdownEditor'
import { PriorityDotsPicker } from '../../components/PriorityDotsPicker'
import { TaskAttachments } from '../../components/TaskAttachments'
import { TaskLinksEditor } from '../../components/TaskLinksEditor'
import { TaskStatusIcon } from '../../components/TaskStatusIcon'
import { TASK_STATUS_LABEL, TASK_STATUS_OPTIONS, WEEKDAY_OPTIONS } from './labels'

const ALL_WEEKDAYS = 127

export interface TaskFormValues {
  title: string
  description: string | null
  deadline: string | null
  priority: Priority
  status: VTaskStatus
  type: VTaskType
  estimatedMinutes: number | null
  tags: string[]
  links: TaskLink[]
  recurrenceDays: number
}

interface TaskFormInitialValues {
  title: string
  description: string
  deadline: string
  priority: Priority
  status: VTaskStatus
  type: VTaskType
  estimatedMinutes: string
  tags: string
  links: TaskLink[]
  recurrenceDays: number
}

interface TaskFormAttachments {
  taskId: string
  items: TaskAttachment[]
  onChange: (items: TaskAttachment[]) => void
}

interface TaskFormProps {
  initialValues?: TaskFormInitialValues
  attachments?: TaskFormAttachments
  submitLabel: string
  onSubmit: (values: TaskFormValues) => Promise<void>
  onCancel: () => void
}

const EMPTY_VALUES: TaskFormInitialValues = {
  title: '',
  description: '',
  deadline: '',
  priority: Priority.Low,
  status: VTaskStatus.New,
  type: VTaskType.Normal,
  estimatedMinutes: '',
  tags: '',
  links: [],
  recurrenceDays: 0,
}

export function TaskForm({ initialValues, attachments, submitLabel, onSubmit, onCancel }: TaskFormProps) {
  const values = initialValues ?? EMPTY_VALUES

  const [title, setTitle] = useState(values.title)
  const [description, setDescription] = useState(values.description)
  const [deadline, setDeadline] = useState(values.deadline)
  const [priority, setPriority] = useState<Priority>(values.priority)
  const [status, setStatus] = useState<VTaskStatus>(values.status)
  const [type, setType] = useState<VTaskType>(values.type)
  const [estimatedMinutes, setEstimatedMinutes] = useState(values.estimatedMinutes)
  const [tags, setTags] = useState(values.tags)
  const [links, setLinks] = useState<TaskLink[]>(values.links)
  const [recurrenceDays, setRecurrenceDays] = useState(values.recurrenceDays)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()

    const trimmedTitle = title.trim()
    if (trimmedTitle.length === 0) {
      setError('Название обязательно')
      return
    }

    let minutes: number | null = null
    if (estimatedMinutes.trim().length > 0) {
      const parsed = Number(estimatedMinutes)
      if (!Number.isFinite(parsed) || parsed < 0) {
        setError('Оценка времени должна быть неотрицательным числом')
        return
      }
      minutes = parsed
    }

    const isRegular = type === VTaskType.Repeatable
    if (isRegular && recurrenceDays === 0) {
      setError('Для регулярной задачи выберите хотя бы один день недели')
      return
    }

    setSubmitting(true)
    setError(null)

    onSubmit({
      title: trimmedTitle,
      description: description.trim() || null,
      deadline: deadline ? `${deadline}T00:00:00.000Z` : null,
      priority,
      status,
      type,
      estimatedMinutes: minutes,
      tags: tags
        .split(',')
        .map((tag) => tag.trim())
        .filter((tag) => tag.length > 0),
      links,
      recurrenceDays: isRegular ? recurrenceDays : 0,
    })
      .catch((err: unknown) => {
        setError(err instanceof ApiError ? err.message : 'Не удалось сохранить задачу')
      })
      .finally(() => setSubmitting(false))
  }

  return (
    <form className="goal-form" onSubmit={handleSubmit}>
      <div className="goal-form-field">
        <label htmlFor="task-form-title">Название</label>
        <input
          id="task-form-title"
          type="text"
          value={title}
          maxLength={200}
          onChange={(event) => setTitle(event.target.value)}
          disabled={submitting}
          autoFocus
        />
      </div>

      <div className="goal-form-field">
        <label htmlFor="task-form-description">Описание</label>
        <MarkdownEditor
          id="task-form-description"
          value={description}
          maxLength={2000}
          compact
          onChange={setDescription}
          disabled={submitting}
        />
      </div>

      <div className="goal-form-field">
        <label>Ссылки и пути</label>
        <TaskLinksEditor links={links} onChange={setLinks} disabled={submitting} />
      </div>

      <div className="task-form-status-priority">
        <div className="task-form-priority-group">
          <span className="task-form-priority-label">Приоритет:</span>
          <PriorityDotsPicker name="task-form-priority" value={priority} onChange={setPriority} disabled={submitting} />
        </div>

        {initialValues && (
          <label className="task-form-status-inline">
            Статус
            <TaskStatusIcon status={status} />
            <select
              id="task-form-status"
              className="task-form-status-compact"
              value={status}
              onChange={(event) => setStatus(Number(event.target.value) as VTaskStatus)}
              disabled={submitting}
            >
              {TASK_STATUS_OPTIONS.map((option) => (
                <option key={option} value={option}>
                  {TASK_STATUS_LABEL[option]}
                </option>
              ))}
            </select>
          </label>
        )}
      </div>

      <div className="goal-form-row">
        <div className="goal-form-field">
          <label htmlFor="task-form-deadline">Дедлайн</label>
          <input
            id="task-form-deadline"
            type="date"
            value={deadline}
            onChange={(event) => setDeadline(event.target.value)}
            disabled={submitting}
          />
        </div>

        <div className="goal-form-field">
          <label htmlFor="task-form-estimated">Оценка, мин</label>
          <input
            id="task-form-estimated"
            type="number"
            min={0}
            value={estimatedMinutes}
            onChange={(event) => setEstimatedMinutes(event.target.value)}
            disabled={submitting}
          />
        </div>
      </div>

      <div className="task-form-recurring">
        <label className="task-form-recurring-label">
          <input
            type="checkbox"
            checked={type === VTaskType.Repeatable}
            onChange={(event) => {
              setType(event.target.checked ? VTaskType.Repeatable : VTaskType.Normal)
              if (event.target.checked && recurrenceDays === 0) {
                setRecurrenceDays(ALL_WEEKDAYS)
              }
            }}
            disabled={submitting}
          />
          Регулярная задача
        </label>

        {type === VTaskType.Repeatable && (
          <div className="weekday-checks" role="group" aria-label="Дни недели">
            {WEEKDAY_OPTIONS.map((day) => (
              <label key={day.bit} className="weekday-check">
                <input
                  type="checkbox"
                  checked={(recurrenceDays & day.bit) !== 0}
                  onChange={(event) => setRecurrenceDays((prev) => (event.target.checked ? prev | day.bit : prev & ~day.bit))}
                  disabled={submitting}
                />
                {day.label}
              </label>
            ))}
          </div>
        )}
      </div>

      <div className="goal-form-field">
        <label htmlFor="task-form-tags">Теги (через запятую)</label>
        <input
          id="task-form-tags"
          type="text"
          value={tags}
          onChange={(event) => setTags(event.target.value)}
          disabled={submitting}
        />
      </div>

      <div className="goal-form-field">
        <label>Вложения</label>
        {attachments ? (
          <TaskAttachments taskId={attachments.taskId} items={attachments.items} editable onChange={attachments.onChange} />
        ) : (
          <p className="task-attachments-empty">Вложения можно добавить после создания задачи</p>
        )}
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
