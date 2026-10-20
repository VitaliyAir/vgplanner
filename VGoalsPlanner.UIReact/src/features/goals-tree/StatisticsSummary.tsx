import type { Statistics, VTaskStatus } from '../../api/types'
import { TaskStatusIcon } from '../../components/TaskStatusIcon'
import { TASK_STATUS_LABEL } from './labels'

// Короткая статистика над списком последних задач: у целей — только всего, у задач — всего и по статусам.
export function StatisticsSummary({ stats }: { stats: Statistics }) {
  return (
    <div className="stats-summary">
      <h2>Статистика</h2>
      <div className="stats-summary-row stats-summary-row--main">
        <span>Цели</span>
        <b>{stats.goals.total}</b>
      </div>
      <div className="stats-summary-row stats-summary-row--main">
        <span>Задачи</span>
        <b>{stats.tasks.total}</b>
      </div>
      <ul className="stats-summary-statuses">
        {stats.tasks.byStatus.map((item) => (
          <li key={item.status} className="stats-summary-row">
            <span>
              <TaskStatusIcon status={item.status as VTaskStatus} size={14} />
              {TASK_STATUS_LABEL[item.status as VTaskStatus]}
            </span>
            <b>{item.count}</b>
          </li>
        ))}
      </ul>
    </div>
  )
}
