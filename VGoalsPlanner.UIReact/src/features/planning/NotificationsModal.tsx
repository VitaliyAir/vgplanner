import { useEffect, useState } from 'react'
import {
  createNotification,
  deleteNotification,
  getDayNotifications,
  markNotificationCompleted,
  updateNotification,
} from '../../api/notificationsApi'
import { ApiError } from '../../api/httpClient'
import type { VNotificationItem } from '../../api/types'
import { Modal } from '../../components/Modal'

interface NotificationsModalProps {
  date: string
  onClose: () => void
}

function toUtcIso(localValue: string): string {
  return new Date(localValue).toISOString()
}

function toLocalInputValue(isoUtc: string): string {
  const d = new Date(isoUtc)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

export function NotificationsModal({ date, onClose }: NotificationsModalProps) {
  const [items, setItems] = useState<VNotificationItem[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const [editingId, setEditingId] = useState<string | null>(null)
  const [editTitle, setEditTitle] = useState('')
  const [editDescription, setEditDescription] = useState('')
  const [editDeadline, setEditDeadline] = useState('')

  const [newTitle, setNewTitle] = useState('')
  const [newDescription, setNewDescription] = useState('')
  const [newDeadline, setNewDeadline] = useState('')

  const reload = () => {
    getDayNotifications(date)
      .then(setItems)
      .catch((err: unknown) => setError(err instanceof ApiError ? err.message : 'Не удалось загрузить напоминания'))
  }

  useEffect(() => {
    reload()
  }, [date])

  const startEdit = (item: VNotificationItem) => {
    setEditingId(item.id)
    setEditTitle(item.title)
    setEditDescription(item.description ?? '')
    setEditDeadline(item.deadline ? toLocalInputValue(item.deadline) : '')
  }

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
      reload()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось сохранить напоминание')
    } finally {
      setBusy(false)
    }
  }

  const complete = async (id: string) => {
    setBusy(true)
    setError(null)
    try {
      await markNotificationCompleted(id)
      reload()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось отметить выполненным')
    } finally {
      setBusy(false)
    }
  }

  const remove = async (id: string) => {
    setBusy(true)
    setError(null)
    try {
      await deleteNotification(id)
      reload()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось удалить напоминание')
    } finally {
      setBusy(false)
    }
  }

  const add = async () => {
    setBusy(true)
    setError(null)
    try {
      await createNotification(date, {
        title: newTitle,
        description: newDescription || null,
        deadline: toUtcIso(newDeadline),
      })
      setNewTitle('')
      setNewDescription('')
      setNewDeadline('')
      reload()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось добавить напоминание')
    } finally {
      setBusy(false)
    }
  }

  return (
    <Modal title="Напоминания дня" onClose={onClose}>
      <div className="task-log-modal">
        {error && <p className="tree-error">{error}</p>}
        {items === null && !error && <p className="tree-loading">Загрузка…</p>}
        {items !== null && items.length === 0 && <p className="tree-empty">Напоминаний пока нет</p>}

        {items !== null && items.length > 0 && (
          <ul className="task-log-list">
            {items.map((item) => (
              <li key={item.id} className="task-log-entry">
                {editingId === item.id ? (
                  <div className="task-log-edit">
                    <input type="text" value={editTitle} onChange={(event) => setEditTitle(event.target.value)} autoFocus />
                    <textarea value={editDescription} onChange={(event) => setEditDescription(event.target.value)} />
                    <input type="datetime-local" value={editDeadline} onChange={(event) => setEditDeadline(event.target.value)} required />
                    <div className="task-log-edit-actions">
                      <button type="button" onClick={saveEdit} disabled={busy || editTitle.trim() === '' || editDeadline === ''}>
                        Сохранить
                      </button>
                      <button type="button" onClick={() => setEditingId(null)} disabled={busy}>
                        Отмена
                      </button>
                    </div>
                  </div>
                ) : (
                  <>
                    <label className="notification-completed">
                      <input type="checkbox" checked={item.isCompleted} disabled={item.isCompleted || busy} onChange={() => complete(item.id)} />
                    </label>
                    <span className="task-log-entry-text">
                      <strong>{item.title}</strong>
                      {item.description ? ` — ${item.description}` : ''}
                      {item.deadline ? ` · ${new Date(item.deadline).toLocaleString('ru-RU')}` : ''}
                    </span>
                    <button type="button" className="task-log-entry-edit" onClick={() => startEdit(item)} disabled={busy} title="Редактировать">
                      ✎
                    </button>
                    <button type="button" className="task-log-entry-edit danger" onClick={() => remove(item.id)} disabled={busy} title="Удалить">
                      ✕
                    </button>
                  </>
                )}
              </li>
            ))}
          </ul>
        )}

        <div className="task-log-add">
          <input type="text" placeholder="Название" value={newTitle} onChange={(event) => setNewTitle(event.target.value)} />
          <textarea placeholder="Описание (необязательно)" value={newDescription} onChange={(event) => setNewDescription(event.target.value)} />
          <div className="task-log-add-row">
            <input type="datetime-local" value={newDeadline} onChange={(event) => setNewDeadline(event.target.value)} required />
            <button type="button" onClick={add} disabled={busy || newTitle.trim() === '' || newDeadline === ''}>
              Добавить
            </button>
          </div>
        </div>
      </div>
    </Modal>
  )
}
