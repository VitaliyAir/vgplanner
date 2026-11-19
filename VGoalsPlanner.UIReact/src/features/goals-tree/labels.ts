import { ElaborationLevel, Priority, VGoalStatus, VTaskStatus, VTaskType } from '../../api/types'

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

// Степень проработанности задачи (VTaskElaborationLevel.cs): подпись в выпадающем списке,
// краткая метка в карточке и расшифровка «что это значит» в подсказке.
export const ELABORATION_LEVEL_LABEL: Record<ElaborationLevel, string> = {
  [ElaborationLevel.Reminder]: 'Напоминание (0–30%)',
  [ElaborationLevel.Research]: 'Исследование (30–70%)',
  [ElaborationLevel.Actionable]: 'Готова к работе (70–90%)',
  [ElaborationLevel.Detailed]: 'Детально проработана (90–100%)',
}

export const ELABORATION_LEVEL_SHORT: Record<ElaborationLevel, string> = {
  [ElaborationLevel.Reminder]: 'Reminder',
  [ElaborationLevel.Research]: 'Research',
  [ElaborationLevel.Actionable]: 'Actionable',
  [ElaborationLevel.Detailed]: 'Detailed',
}

export const ELABORATION_LEVEL_HINT: Record<ElaborationLevel, string> = {
  [ElaborationLevel.Reminder]: 'Идея, ссылки, черновик (0–30%)',
  [ElaborationLevel.Research]: 'Нужно изучить, найти, уточнить (30–70%)',
  [ElaborationLevel.Actionable]: 'Готова к работе: есть понятные шаги (70–90%)',
  [ElaborationLevel.Detailed]: 'Детально проработана: шаги + оценка + ресурсы (90–100%)',
}

export const ELABORATION_LEVEL_OPTIONS: ElaborationLevel[] = [
  ElaborationLevel.Reminder,
  ElaborationLevel.Research,
  ElaborationLevel.Actionable,
  ElaborationLevel.Detailed,
]

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
