import { useEffect, useState } from 'react'
import { useAuth } from 'react-oidc-context'
import { getAppVersion } from './api/versionApi'
import type { AppVersion } from './api/types'
import { GoalsAndTaskView } from './features/goals-tree/GoalsAndTaskView'
import { PlanningView } from './features/planning/PlanningView'
import { HistoryView } from './features/history/HistoryView'
import { StatisticsView } from './features/statistics/StatisticsView'
import { LegendModal } from './components/LegendModal'
import { NotificationAlerts } from './components/NotificationAlerts'
import './App.css'

type Tab = 'tree' | 'planning' | 'history' | 'statistics'

// Версия UI вшивается при сборке из VGoalsPlanner.UIReact/version.txt (см. vite.config.ts),
// коммит — из build-arg. Показываем обе версии рядом: UI и API деплоятся отдельными образами.
const UI_VERSION = __APP_VERSION__
const UI_COMMIT = __APP_COMMIT__

function versionTitle(api: AppVersion | null, failed: boolean): string {
  const ui = `UI: ${UI_VERSION}${UI_COMMIT ? ` (${UI_COMMIT})` : ''}`
  if (!api) {
    return `${ui}\nAPI: ${failed ? 'недоступен' : 'запрашивается…'}`
  }

  const builtAt = new Date(api.builtAt)
  return [
    ui,
    `API: ${api.version}${api.commit ? ` (${api.commit.slice(0, 7)})` : ''}`,
    `окружение API: ${api.environment}`,
    `сборка API: ${Number.isNaN(builtAt.getTime()) ? api.builtAt : builtAt.toLocaleString()}`,
  ].join('\n')
}

function App() {
  const [tab, setTab] = useState<Tab>('planning')
  const [legendOpen, setLegendOpen] = useState(false)
  const [apiVersion, setApiVersion] = useState<AppVersion | null>(null)
  const [apiVersionFailed, setApiVersionFailed] = useState(false)
  const auth = useAuth()

  useEffect(() => {
    let cancelled = false

    getAppVersion()
      .then((version) => {
        if (!cancelled) {
          setApiVersion(version)
        }
      })
      .catch(() => {
        // Версия — диагностика, а не функция: её недоступность не должна ломать приложение.
        if (!cancelled) {
          setApiVersionFailed(true)
        }
      })

    return () => {
      cancelled = true
    }
  }, [])

  // UI и API деплоятся отдельными образами и могут разъехаться — подсвечиваем это.
  const versionsDiffer = apiVersion !== null && apiVersion.version !== UI_VERSION

  return (
    <div>
      <div className="app-tabs">
        <div className="app-tabs-list">
          <button type="button" className={tab === 'planning' ? 'active' : ''} onClick={() => setTab('planning')}>
            Планирование
          </button>
          <button type="button" className={tab === 'tree' ? 'active' : ''} onClick={() => setTab('tree')}>
            Цели и задачи
          </button>
          <button type="button" className={tab === 'history' ? 'active' : ''} onClick={() => setTab('history')}>
            История
          </button>
          <button type="button" className={tab === 'statistics' ? 'active' : ''} onClick={() => setTab('statistics')}>
            Статистика
          </button>
        </div>

        <div className="app-user">
          <span>{auth.user?.profile.preferred_username}</span>
          <button type="button" onClick={() => setLegendOpen(true)}>
            Легенда
          </button>
          <button type="button" onClick={() => void auth.signoutRedirect()}>
            Выйти
          </button>
          <span
            className={`app-version${versionsDiffer ? ' app-version--mismatch' : ''}`}
            title={versionTitle(apiVersion, apiVersionFailed)}
          >
            UI {UI_VERSION} · API {apiVersion?.version ?? (apiVersionFailed ? '?' : '…')}
          </span>
        </div>
      </div>

      <div className={`app-tab-panel${tab === 'planning' ? '' : ' hidden'}`}>
        <PlanningView />
      </div>
      <div className={`app-tab-panel${tab === 'tree' ? '' : ' hidden'}`}>
        <GoalsAndTaskView isActive={tab === 'tree'} />
      </div>
      <div className={`app-tab-panel${tab === 'history' ? '' : ' hidden'}`}>
        <HistoryView />
      </div>
      <div className={`app-tab-panel${tab === 'statistics' ? '' : ' hidden'}`}>
        <StatisticsView isActive={tab === 'statistics'} />
      </div>

      <NotificationAlerts />
      {legendOpen && <LegendModal onClose={() => setLegendOpen(false)} />}
    </div>
  )
}

export default App
