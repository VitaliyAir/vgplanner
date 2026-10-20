import { VTaskStatus } from '../api/types'
import { PRIORITY_LABEL, PRIORITY_OPTIONS, TASK_STATUS_LABEL } from '../features/goals-tree/labels'
import { Modal } from './Modal'
import { PriorityFlag } from './PriorityFlag'
import { PRIORITY_COLOR_NAME } from './priorityPalette'
import { TaskStatusIcon } from './TaskStatusIcon'
import './legendModal.css'

const LEGEND_STATUSES: VTaskStatus[] = [
  VTaskStatus.New,
  VTaskStatus.OnHold,
  VTaskStatus.InProgress,
  VTaskStatus.Completed,
  VTaskStatus.Cancelled,
]

export function LegendModal({ onClose }: { onClose: () => void }) {
  return (
    <Modal title="Легенда" onClose={onClose}>
      <section className="legend-section">
        <h3>Приоритеты</h3>
        <p className="legend-note">Цвет флажка у целей и задач и выбора приоритета в форме.</p>
        <ul className="legend-list">
          {PRIORITY_OPTIONS.map((option) => (
            <li key={option} className="legend-row">
              <PriorityFlag priority={option} />
              <span className="legend-name">{PRIORITY_LABEL[option]}</span>
              <span className="legend-color">{PRIORITY_COLOR_NAME[option]}</span>
            </li>
          ))}
        </ul>
      </section>

      <section className="legend-section">
        <h3>Статусы задач</h3>
        <ul className="legend-list">
          {LEGEND_STATUSES.map((status) => (
            <li key={status} className="legend-row">
              <TaskStatusIcon status={status} size={20} />
              <span className="legend-name">{TASK_STATUS_LABEL[status]}</span>
            </li>
          ))}
        </ul>
      </section>
    </Modal>
  )
}
