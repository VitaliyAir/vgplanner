import { VTaskStatus } from '../api/types'
import './taskStatusBackground.css'

// Класс фона элемента задачи по статусу: в работе — светло-зелёный, завершена — светло-голубой,
// на паузе — светло-жёлтый, отменена — светло-серый.
export function taskStatusBackground(status: VTaskStatus): string {
  switch (status) {
    case VTaskStatus.InProgress:
      return 'task-bg-inprogress'
    case VTaskStatus.Completed:
      return 'task-bg-completed'
    case VTaskStatus.OnHold:
      return 'task-bg-onhold'
    case VTaskStatus.Cancelled:
      return 'task-bg-cancelled'
    default:
      return ''
  }
}
