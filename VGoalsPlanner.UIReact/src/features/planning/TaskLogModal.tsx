import { useEffect, useState } from 'react'
import { addTaskLog, getTaskHistory, updateTaskLog } from '../../api/taskLogsApi'
import { changeTaskStatus } from '../../api/tasksApi'
import { ApiError } from '../../api/httpClient'
import type { TaskLog, VTaskStatus } from '../../api/types'
import { MarkdownEditor } from '../../components/MarkdownEditor'
import { MarkdownView } from '../../components/MarkdownView'
import { Modal } from '../../components/Modal'
import { TaskStatusIcon } from '../../components/TaskStatusIcon'
import { TASK_STATUS_LABEL, TASK_STATUS_OPTIONS } from '../goals-tree/labels'

interface TaskLogModalProps {
  taskId: string
  date: string
  currentStatus: VTaskStatus
  onClose: () => void
  onChanged: () => void
}

export function TaskLogModal({ taskId, date, currentStatus, onClose, onChanged }: TaskLogModalProps) {
  const [logs, setLogs] = useState<TaskLog[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const [editingId, setEditingId] = useState<string | null>(null)
  const [editText, setEditText] = useState('')

  const [newNote, setNewNote] = useState('')
  const [newMinutes, setNewMinutes] = useState('')
  const [newStatus, setNewStatus] = useState<VTaskStatus | ''>('')

  const reload = () => {
    getTaskHistory(taskId)
      .then((all) => setLogs(all.filter((l) => l.date === date)))
      .catch((err: unknown) => setError(err instanceof ApiError ? err.message : 'Не удалось загрузить журнал'))
  }

  useEffect(() => {
    reload()
  }, [taskId, date])

  const startEdit = (log: TaskLog) => {
    setEditingId(log.id)
    setEditText(log.note ?? '')
  }

  const saveEdit = async (log: TaskLog) => {
    setBusy(true)
    try {
      await updateTaskLog(log.id, editText, log.attachments)
      setEditingId(null)
      reload()
      onChanged()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось сохранить запись')
    } finally {
      setBusy(false)
    }
  }

  const addEntry = async () => {
    setBusy(true)
    try {
      if (newStatus !== '') {
        await changeTaskStatus(taskId, newStatus)
      }
      await addTaskLog({
        taskId,
        date,
        note: newNote,
        timeSpentMinutes: newMinutes.trim() === '' ? null : Number(newMinutes),
        oldStatus: newStatus === '' ? null : currentStatus,
        newStatus: newStatus === '' ? null : newStatus,
      })
      setNewNote('')
      setNewMinutes('')
      setNewStatus('')
      reload()
      onChanged()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось добавить запись')
    } finally {
      setBusy(false)
    }
  }

  return (
    <Modal title="Журнал задачи" onClose={onClose} className="modal-panel--xl">
      <div className="task-log-modal">
        {error && <p className="tree-error">{error}</p>}
        {logs === null && !error && <p className="tree-loading">Загрузка…</p>}

        {logs !== null && logs.length === 0 && <p className="tree-empty">Записей за этот день пока нет</p>}

        {logs !== null && logs.length > 0 && (
          <ul className="task-log-list">
            {logs.map((log) => (
              <li key={log.id} className="task-log-entry">
                {editingId === log.id ? (
                  <div className="task-log-edit">
                    <MarkdownEditor value={editText} onChange={setEditText} disabled={busy} autoFocus />
                    <div className="task-log-edit-actions">
                      <button type="button" onClick={() => saveEdit(log)} disabled={busy}>
                        Сохранить
                      </button>
                      <button type="button" onClick={() => setEditingId(null)} disabled={busy}>
                        Отмена
                      </button>
                    </div>
                  </div>
                ) : (
                  <>
                    <div className="task-log-entry-text">
                      {log.note ? <MarkdownView source={log.note} /> : <em>Без текста</em>}
                      {log.timeSpentMinutes != null && <small> · {log.timeSpentMinutes} мин</small>}
                    </div>
                    <button type="button" className="task-log-entry-edit" onClick={() => startEdit(log)} title="Редактировать">
                      ✎
                    </button>
                  </>
                )}
              </li>
            ))}
          </ul>
        )}

        <div className="task-log-add">
          <MarkdownEditor placeholder="Что сделано…" value={newNote} onChange={setNewNote} disabled={busy} />
          <div className="task-log-add-row">
            <input
              type="number"
              min={0}
              placeholder="Мин."
              value={newMinutes}
              onChange={(event) => setNewMinutes(event.target.value)}
            />
          </div>
          <div className="task-log-status-row">
            <span className="task-log-status-current">
              Текущий статус: <TaskStatusIcon status={currentStatus} size={14} /> {TASK_STATUS_LABEL[currentStatus]}
            </span>
            <span className="status-select-row">
              {newStatus !== '' && <TaskStatusIcon status={newStatus} />}
              <select value={newStatus} onChange={(event) => setNewStatus(event.target.value === '' ? '' : (Number(event.target.value) as VTaskStatus))}>
                <option value="">Без изменения статуса</option>
                {TASK_STATUS_OPTIONS.map((option) => (
                  <option key={option} value={option}>
                    {TASK_STATUS_LABEL[option]}
                  </option>
                ))}
              </select>
            </span>
          </div>
          <button type="button" onClick={addEntry} disabled={busy || newNote.trim() === ''}>
            Добавить запись
          </button>
        </div>
      </div>
    </Modal>
  )
}
