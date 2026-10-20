import { useState } from 'react'
import type { KeyboardEvent } from 'react'
import { TaskLinkKind, type TaskLink } from '../api/types'
import { CopyLinkButton } from './CopyLinkButton'
import { MAX_TASK_LINKS, linkIcon, linkLabel, normalizeTarget, sameLink, validateTarget } from './taskLinks'
import './taskLinks.css'

interface TaskLinksEditorProps {
  links: TaskLink[]
  onChange: (links: TaskLink[]) => void
  disabled?: boolean
}

interface KindPickerProps {
  value: TaskLinkKind
  onChange: (kind: TaskLinkKind) => void
  disabled?: boolean
}

// Сначала выбирается тип записи значком: веб-ссылка или путь к папке/файлу.
function KindPicker({ value, onChange, disabled }: KindPickerProps) {
  return (
    <span className="task-links-kinds" role="group" aria-label="Тип записи">
      <button
        type="button"
        className={`task-links-kind${value === TaskLinkKind.Url ? ' active' : ''}`}
        aria-pressed={value === TaskLinkKind.Url}
        title="Веб-ссылка"
        disabled={disabled}
        onClick={() => onChange(TaskLinkKind.Url)}
      >
        {linkIcon(TaskLinkKind.Url)}
      </button>
      <button
        type="button"
        className={`task-links-kind${value === TaskLinkKind.Path ? ' active' : ''}`}
        aria-pressed={value === TaskLinkKind.Path}
        title="Путь к папке или файлу"
        disabled={disabled}
        onClick={() => onChange(TaskLinkKind.Path)}
      >
        {linkIcon(TaskLinkKind.Path)}
      </button>
    </span>
  )
}

export function TaskLinksEditor({ links, onChange, disabled = false }: TaskLinksEditorProps) {
  const [kind, setKind] = useState<TaskLinkKind>(TaskLinkKind.Url)
  const [target, setTarget] = useState('')
  const [alias, setAlias] = useState('')
  const [error, setError] = useState<string | null>(null)

  const [editIndex, setEditIndex] = useState<number | null>(null)
  const [editKind, setEditKind] = useState<TaskLinkKind>(TaskLinkKind.Url)
  const [editTarget, setEditTarget] = useState('')
  const [editAlias, setEditAlias] = useState('')
  const [editError, setEditError] = useState<string | null>(null)

  const handleAdd = () => {
    const normalized = normalizeTarget(kind, target)
    const message = validateTarget(kind, normalized)
    if (message) {
      setError(message)
      return
    }
    if (links.length >= MAX_TASK_LINKS) {
      setError(`Не более ${MAX_TASK_LINKS} ссылок`)
      return
    }
    if (links.some((link) => sameLink(link, kind, normalized))) {
      setError('Такая запись уже есть')
      return
    }
    onChange([...links, { kind, target: normalized, alias: alias.trim() || null }])
    setTarget('')
    setAlias('')
    setError(null)
  }

  const startEdit = (index: number) => {
    const link = links[index]
    setEditIndex(index)
    setEditKind(link.kind)
    setEditTarget(link.target)
    setEditAlias(link.alias ?? '')
    setEditError(null)
  }

  const cancelEdit = () => {
    setEditIndex(null)
    setEditError(null)
  }

  const saveEdit = () => {
    if (editIndex === null) {
      return
    }
    const normalized = normalizeTarget(editKind, editTarget)
    const message = validateTarget(editKind, normalized)
    if (message) {
      setEditError(message)
      return
    }
    if (links.some((link, index) => index !== editIndex && sameLink(link, editKind, normalized))) {
      setEditError('Такая запись уже есть')
      return
    }
    onChange(links.map((link, index) => (index === editIndex ? { kind: editKind, target: normalized, alias: editAlias.trim() || null } : link)))
    cancelEdit()
  }

  const handleDelete = (index: number) => {
    if (editIndex === index) {
      cancelEdit()
    }
    onChange(links.filter((_, i) => i !== index))
  }

  // Enter в полях не должен отправлять форму задачи.
  const onAddKeyDown = (event: KeyboardEvent) => {
    if (event.key === 'Enter') {
      event.preventDefault()
      handleAdd()
    }
  }

  const onEditKeyDown = (event: KeyboardEvent) => {
    if (event.key === 'Enter') {
      event.preventDefault()
      saveEdit()
    } else if (event.key === 'Escape') {
      event.preventDefault()
      event.stopPropagation()
      cancelEdit()
    }
  }

  return (
    <div className="task-links-editor">
      {links.length > 0 && (
        <ul className="task-links-list">
          {links.map((link, index) =>
            editIndex === index ? (
              <li key={index} className="task-links-item task-links-item--editing">
                <div className="task-links-add">
                  <KindPicker value={editKind} onChange={setEditKind} disabled={disabled} />
                  <input
                    type="text"
                    className="task-links-input"
                    value={editTarget}
                    maxLength={1000}
                    placeholder="Ссылка или путь"
                    onChange={(event) => setEditTarget(event.target.value)}
                    onKeyDown={onEditKeyDown}
                    disabled={disabled}
                    autoFocus
                  />
                  <input
                    type="text"
                    className="task-links-input task-links-input--alias"
                    value={editAlias}
                    maxLength={100}
                    placeholder="Название (необязательно)"
                    onChange={(event) => setEditAlias(event.target.value)}
                    onKeyDown={onEditKeyDown}
                    disabled={disabled}
                  />
                  <button type="button" className="icon-button" title="Сохранить" onClick={saveEdit} disabled={disabled}>
                    ✓
                  </button>
                  <button type="button" className="icon-button" title="Отмена" onClick={cancelEdit} disabled={disabled}>
                    ✕
                  </button>
                </div>
                {editError && <p className="task-links-error">{editError}</p>}
              </li>
            ) : (
              <li key={index} className="task-links-item">
                <span aria-hidden="true">{linkIcon(link.kind)}</span>
                <span className="task-links-text" title={link.target}>
                  {linkLabel(link)}
                </span>
                <CopyLinkButton text={link.target} />
                <button type="button" className="icon-button" title="Редактировать" onClick={() => startEdit(index)} disabled={disabled}>
                  ✎
                </button>
                <button
                  type="button"
                  className="icon-button icon-button--danger"
                  title="Удалить"
                  onClick={() => handleDelete(index)}
                  disabled={disabled}
                >
                  🗑
                </button>
              </li>
            ),
          )}
        </ul>
      )}

      <div className="task-links-add">
        <KindPicker value={kind} onChange={setKind} disabled={disabled} />
        <input
          type="text"
          className="task-links-input"
          value={target}
          maxLength={1000}
          placeholder={kind === TaskLinkKind.Url ? 'Вставьте веб-адрес' : 'Вставьте путь к папке или файлу'}
          onChange={(event) => setTarget(event.target.value)}
          onKeyDown={onAddKeyDown}
          disabled={disabled}
        />
        <input
          type="text"
          className="task-links-input task-links-input--alias"
          value={alias}
          maxLength={100}
          placeholder="Название (необязательно)"
          onChange={(event) => setAlias(event.target.value)}
          onKeyDown={onAddKeyDown}
          disabled={disabled}
        />
        <button type="button" onClick={handleAdd} disabled={disabled}>
          Добавить
        </button>
      </div>
      {error && <p className="task-links-error">{error}</p>}
    </div>
  )
}
