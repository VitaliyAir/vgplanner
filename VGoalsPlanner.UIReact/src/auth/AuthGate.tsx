import { useEffect, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import { useAuth } from 'react-oidc-context'
import { onForbidden } from './authEvents'
import './auth.css'

interface AuthGateProps {
  children: ReactNode
}

export function AuthGate({ children }: AuthGateProps) {
  const auth = useAuth()
  const [forbidden, setForbidden] = useState(false)
  const redirectStarted = useRef(false)

  useEffect(() => onForbidden(() => setForbidden(true)), [])

  useEffect(() => {
    if (auth.isLoading || auth.isAuthenticated || auth.activeNavigator || auth.error || redirectStarted.current) {
      return
    }
    redirectStarted.current = true
    void auth.signinRedirect()
  }, [auth])

  if (auth.error) {
    return (
      <div className="auth-screen">
        <p>Не удалось выполнить вход: {auth.error.message}</p>
        <button type="button" onClick={() => void auth.signinRedirect()}>
          Повторить вход
        </button>
      </div>
    )
  }

  if (!auth.isAuthenticated) {
    return <div className="auth-screen">Переход на страницу входа…</div>
  }

  if (forbidden) {
    return (
      <div className="auth-screen">
        <p>У вашего пользователя нет доступа к этому приложению. Обратитесь к администратору.</p>
        <button type="button" onClick={() => void auth.signoutRedirect()}>
          Выйти
        </button>
      </div>
    )
  }

  return <>{children}</>
}
