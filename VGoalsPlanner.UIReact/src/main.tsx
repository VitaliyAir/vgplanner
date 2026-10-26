import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { AuthProvider } from 'react-oidc-context'
import './index.css'
import App from './App.tsx'
import { AuthGate } from './auth/AuthGate'
import { userManager } from './auth/oidcConfig'
import './mobile.css'

// После возврата из Keycloak убираем ?code=...&state=... из адресной строки.
const removeSigninParams = () => window.history.replaceState({}, document.title, window.location.pathname)

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AuthProvider userManager={userManager} onSigninCallback={removeSigninParams}>
      <AuthGate>
        <App />
      </AuthGate>
    </AuthProvider>
  </StrictMode>,
)
