import { VTaskStatus } from '../../api/types'

// Терминальные статусы задачи. В дереве и в списке такие задачи скрыты, пока не включён
// чекбокс «Показывать выполненные»: раньше так было только для «Завершена», теперь и для «Отменена».
const HIDDEN_TASK_STATUSES: VTaskStatus[] = [VTaskStatus.Completed, VTaskStatus.Cancelled]

export function isHiddenTaskStatus(status: VTaskStatus): boolean {
  return HIDDEN_TASK_STATUSES.includes(status)
}
