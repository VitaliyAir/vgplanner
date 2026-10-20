import { InMemoryWebStorage, UserManager, WebStorageStateStore } from 'oidc-client-ts'

// Токены живут только в памяти вкладки; незавершённое состояние входа (PKCE) хранится в localStorage
// (по умолчанию oidc-client-ts), поэтому переход на Keycloak и возврат работают. После перезагрузки
// страницы приложение сразу перенаправляет на Keycloak и возвращается, пока жива SSO-сессия.
export const userManager = new UserManager({
  authority: import.meta.env.VITE_OIDC_AUTHORITY,
  client_id: import.meta.env.VITE_OIDC_CLIENT_ID,
  redirect_uri: `${window.location.origin}/`,
  post_logout_redirect_uri: `${window.location.origin}/`,
  response_type: 'code',
  scope: 'openid profile',
  automaticSilentRenew: true,
  userStore: new WebStorageStateStore({ store: new InMemoryWebStorage() }),
})
