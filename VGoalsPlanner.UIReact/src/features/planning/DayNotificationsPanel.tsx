import { useState } from 'react'
import { deleteNotification, updateNotification } from '../../api/notificationsApi'
import { ApiError } from '../../api/httpClient'
import type { VNotificationItem } from '../../api/types'

const TIME_FMT = new Intl.DateTimeFormat('ru-RU', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' })

function toUtcIso(localValue: string): string {
  return new Date(localValue).toISOString()
}

function toLocalInputValue(isoUtc: string): string {
  const d = new Date(isoUtc)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

interface DayNotificationsPanelProps {
  date: string
  notifications: VNotificationItem[]
  isPast: boolean
  onClose: () => void
  onChanged: () => void
}

export function DayNotificationsPanel({ date, notifications, isPast, onClose, onChanged }: DayNotificationsPanelProps) {
  const [editingId, setEditingId] = useState<string | null>(null)
  const [editTitle, setEditTitle] = useState('')
  const [editDescription, setEditDescription] = useState('')
  const [editDeadline, setEditDeadline] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const startEdit = (item: VNotificationItem) => {
    setEditingId(item.id)
    setEditTitle(item.title)
    setEditDescription(item.description ?? '')
    setEditDeadline(item.deadline ? toLocalInputValue(item.deadline) : '')
    setError(null)
  }

  const cancelEdit = () => setEditingId(null)

  const saveEdit = async () => {
    if (!editingId) {
      return
    }
    setBusy(true)
    setError(null)
    try {
      await updateNotification(editingId, {
        title: editTitle,
        description: editDescription || null,
        deadline: editDeadline ? toUtcIso(editDeadline) : null,
      })
      setEditingId(null)
      onChanged()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось сохранить')
    } finally {
      setBusy(false)
    }
  }

  const remove = async (id: string) => {
    setBusy(true)
    setError(null)
    try {
      await deleteNotification(id)
      onChanged()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось удалить')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="day-notifications-panel">
      <div className="day-notifications-panel-header">
        <span>Уведомления · {date}</span>
        <button type="button" onClick={onClose} title="Закрыть">
          ✕
        </button>
      </div>

      {error && <p className="tree-error">{error}</p>}

      {notifications.map((item) => (
        <div key={item.id} className={`day-notification-card${isPast ? '' : ' editable'}`}>
          {!isPast && editingId === item.id ? (
            <div className="day-notification-edit">
              <input type="text" value={editTitle} onChange={(event) => setEditTitle(event.target.value)} autoFocus />
              <textarea value={editDescription} onChange={(event) => setEditDescription(event.target.value)} />
              <input type="datetime-local" value={editDeadline} onChange={(event) => setEditDeadline(event.target.value)} />
              <div className="day-notification-edit-actions">
                <button type="button" onClick={saveEdit} disabled={busy || editTitle.trim() === ''} title="Сохранить">
                  ✓
                </button>
                <button type="button" onClick={cancelEdit} disabled={busy} title="Отмена">
                  ✕
                </button>
              </div>
            </div>
          ) : (
            <>
              <div className="day-notification-title">{item.title}</div>
              {item.description && <div className="day-notification-description">{item.description}</div>}
              {item.deadline && <div className="day-notification-deadline">{TIME_FMT.format(new Date(item.deadline))}</div>}
              {!isPast && (
                <div className="day-notification-actions">
                  <button type="button" onClick={() => startEdit(item)} disabled={busy} title="Редактировать">
                    ✎
                  </button>
                  <button type="button" onClick={() => remove(item.id)} disabled={busy} title="Удалить">
                    ✕
                  </button>
                </div>
              )}
            </>
          )}
        </div>
      ))}
    </div>
  )
}
