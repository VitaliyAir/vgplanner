import { useEffect, useState } from 'react'
import { getUnreadNotifications, markNotificationRead } from '../api/notificationsApi'
import type { VNotificationItem } from '../api/types'
import './notificationAlerts.css'

const POLL_INTERVAL_MS = 30000

function isDue(item: VNotificationItem): boolean {
  return item.deadline !== null && new Date(item.deadline) <= new Date()
}

export function NotificationAlerts() {
  const [due, setDue] = useState<VNotificationItem[]>([])

  useEffect(() => {
    let cancelled = false

    const check = () => {
      getUnreadNotifications()
        .then((items) => {
          if (!cancelled) {
            setDue(items.filter(isDue))
          }
        })
        .catch(() => {
          // тихо игнорируем сбой опроса - следующий тик попробует снова
        })
    }

    check()
    const interval = setInterval(check, POLL_INTERVAL_MS)

    return () => {
      cancelled = true
      clearInterval(interval)
    }
  }, [])

  const handleOk = (id: string) => {
    setDue((prev) => prev.filter((item) => item.id !== id))
    markNotificationRead(id).catch(() => {
      // напоминание останется в очереди на следующий опрос, если запрос не прошёл
    })
  }

  if (due.length === 0) {
    return null
  }

  return (
    <div className="notification-alerts">
      {due.map((item) => (
        <div key={item.id} className="notification-alert-card">
          <div className="notification-alert-title">{item.title}</div>
          {item.description && <div className="notification-alert-description">{item.description}</div>}
          <button type="button" className="notification-alert-ok" onClick={() => handleOk(item.id)}>
            OK
          </button>
        </div>
      ))}
    </div>
  )
}
