import { Priority, VGoalStatus, VTaskStatus, VTaskType } from '../../api/types'

export const PRIORITY_LABEL: Record<Priority, string> = {
  [Priority.Low]: 'Низкий',
  [Priority.Medium]: 'Средний',
  [Priority.High]: 'Высокий',
  [Priority.Critical]: 'Критичный',
}

export const GOAL_STATUS_LABEL: Record<VGoalStatus, string> = {
  [VGoalStatus.New]: 'Новая',
  [VGoalStatus.Started]: 'Начата',
  [VGoalStatus.Planned]: 'Запланирована',
  [VGoalStatus.Paused]: 'На паузе',
  [VGoalStatus.Canceled]: 'Отменена',
  [VGoalStatus.Completed]: 'Завершена',
}

export const TASK_STATUS_LABEL: Record<VTaskStatus, string> = {
  [VTaskStatus.New]: 'Новая',
  [VTaskStatus.InProgress]: 'В работе',
  [VTaskStatus.Completed]: 'Завершена',
  [VTaskStatus.OnHold]: 'На паузе',
  [VTaskStatus.Cancelled]: 'Отменена',
}

export const PRIORITY_OPTIONS: Priority[] = [
  Priority.Low,
  Priority.Medium,
  Priority.High,
  Priority.Critical,
]

export const TASK_TYPE_LABEL: Record<VTaskType, string> = {
  [VTaskType.Normal]: 'Обычная',
  [VTaskType.Repeatable]: 'Повторяющаяся',
}

export const TASK_TYPE_OPTIONS: VTaskType[] = [VTaskType.Normal, VTaskType.Repeatable]

export const TASK_STATUS_OPTIONS: VTaskStatus[] = [
  VTaskStatus.New,
  VTaskStatus.InProgress,
  VTaskStatus.Completed,
  VTaskStatus.OnHold,
  VTaskStatus.Cancelled,
]

// Маска дней недели регулярной задачи (совпадает с RecurrenceDays на бэкенде): Пн=1 … Вс=64.
export const WEEKDAY_OPTIONS: { bit: number; label: string }[] = [
  { bit: 1, label: 'Пн' },
  { bit: 2, label: 'Вт' },
  { bit: 4, label: 'Ср' },
  { bit: 8, label: 'Чт' },
  { bit: 16, label: 'Пт' },
  { bit: 32, label: 'Сб' },
  { bit: 64, label: 'Вс' },
]
