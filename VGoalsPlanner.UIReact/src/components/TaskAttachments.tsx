import { useRef, useState } from 'react'
import type { ChangeEvent } from 'react'
import { deleteTaskAttachment, downloadTaskAttachment, uploadTaskAttachment } from '../api/attachmentsApi'
import { ApiError } from '../api/httpClient'
import type { TaskAttachment } from '../api/types'
import { TrashIcon } from './icons/TrashIcon'
import './taskAttachments.css'

// Должен совпадать с Attachments:MaxFileSizeBytes на сервере (там действует настоящее ограничение).
const MAX_ATTACHMENT_BYTES = 50 * 1024 * 1024

const DATE_FMT = new Intl.DateTimeFormat('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric' })

export function formatFileSize(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} Б`
  }
  if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(1)} КБ`
  }
  return `${(bytes / (1024 * 1024)).toFixed(1)} МБ`
}

interface TaskAttachmentsProps {
  taskId: string
  items: TaskAttachment[]
  editable: boolean
  onChange?: (items: TaskAttachment[]) => void
}

export function TaskAttachments({ taskId, items: initialItems, editable, onChange }: TaskAttachmentsProps) {
  const [items, setItems] = useState(initialItems)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const fileInput = useRef<HTMLInputElement>(null)

  const update = (next: TaskAttachment[]) => {
    setItems(next)
    onChange?.(next)
  }

  const errorText = (err: unknown, fallback: string) => (err instanceof ApiError ? err.message : fallback)

  const handleFiles = async (event: ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(event.target.files ?? [])
    event.target.value = ''
    if (files.length === 0) {
      return
    }

    setBusy(true)
    setError(null)

    let current = items
    for (const file of files) {
      if (file.size > MAX_ATTACHMENT_BYTES) {
        setError(`Файл «${file.name}» больше ${MAX_ATTACHMENT_BYTES / (1024 * 1024)} МБ`)
        continue
      }

      try {
        const created = await uploadTaskAttachment(taskId, file)
        current = [...current, created]
        update(current)
      } catch (err) {
        setError(errorText(err, `Не удалось загрузить «${file.name}»`))
        break
      }
    }

    setBusy(false)
  }

  const handleDownload = async (attachment: TaskAttachment) => {
    setError(null)
    try {
      await downloadTaskAttachment(taskId, attachment)
    } catch (err) {
      setError(errorText(err, `Не удалось скачать «${attachment.fileName}»`))
    }
  }

  const handleDelete = async (attachment: TaskAttachment) => {
    if (!window.confirm(`Удалить вложение «${attachment.fileName}»?`)) {
      return
    }

    setBusy(true)
    setError(null)
    try {
      await deleteTaskAttachment(taskId, attachment.id)
      update(items.filter((item) => item.id !== attachment.id))
    } catch (err) {
      setError(errorText(err, 'Не удалось удалить вложение'))
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="task-attachments">
      {items.length === 0 && <p className="task-attachments-empty">Нет вложений</p>}

      {items.length > 0 && (
        <ul className="task-attachments-list">
          {items.map((attachment) => (
            <li key={attachment.id} className="task-attachment">
              <button
                type="button"
                className="task-attachment-name"
                onClick={() => void handleDownload(attachment)}
                title="Скачать"
              >
                📎 {attachment.fileName}
              </button>
              <span className="task-attachment-meta">
                {formatFileSize(attachment.sizeBytes)} · {DATE_FMT.format(new Date(attachment.createdAt))}
              </span>
              {editable && (
                <button
                  type="button"
                  className="icon-button icon-button--danger"
                  onClick={() => void handleDelete(attachment)}
                  disabled={busy}
                  title="Удалить вложение"
                >
                  <TrashIcon />
                </button>
              )}
            </li>
          ))}
        </ul>
      )}

      {editable && (
        <div className="task-attachments-actions">
          <input ref={fileInput} type="file" multiple hidden onChange={(event) => void handleFiles(event)} />
          <button type="button" onClick={() => fileInput.current?.click()} disabled={busy}>
            {busy ? 'Загрузка…' : 'Прикрепить файл'}
          </button>
        </div>
      )}

      {error && <p className="tree-error tree-error--inline">{error}</p>}
    </div>
  )
}
