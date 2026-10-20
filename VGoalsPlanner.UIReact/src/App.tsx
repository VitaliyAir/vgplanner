import { useState } from 'react'
import { useAuth } from 'react-oidc-context'
import { GoalsAndTaskView } from './features/goals-tree/GoalsAndTaskView'
import { PlanningView } from './features/planning/PlanningView'
import { HistoryView } from './features/history/HistoryView'
import { LegendModal } from './components/LegendModal'
import { NotificationAlerts } from './components/NotificationAlerts'
import './App.css'

type Tab = 'tree' | 'planning' | 'history'

function App() {
  const [tab, setTab] = useState<Tab>('planning')
  const [legendOpen, setLegendOpen] = useState(false)
  const auth = useAuth()

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
        </div>

        <div className="app-user">
          <span>{auth.user?.profile.preferred_username}</span>
          <button type="button" onClick={() => setLegendOpen(true)}>
            Легенда
          </button>
          <button type="button" onClick={() => void auth.signoutRedirect()}>
            Выйти
          </button>
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

      <NotificationAlerts />
      {legendOpen && <LegendModal onClose={() => setLegendOpen(false)} />}
    </div>
  )
}

export default App
