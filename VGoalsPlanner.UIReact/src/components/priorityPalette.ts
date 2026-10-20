import { Priority } from '../api/types'

// Единая палитра приоритетов (цвета из матрицы Эйзенхауэра) — источник правды для легенды и всего UI:
// Шкала "нагрева": зелёный — низкий, жёлтый — средний, оранжевый — высокий, красный — критичный.
export const PRIORITY_COLOR: Record<Priority, string> = {
  [Priority.Low]: '#7fbf7b',
  [Priority.Medium]: '#f2cf3d',
  [Priority.High]: '#f07d2b',
  [Priority.Critical]: '#d8343d',
}

// Тёмные оттенки тех же цветов — для текста на белом фоне.
export const PRIORITY_TEXT_COLOR: Record<Priority, string> = {
  [Priority.Low]: '#2d6a1f',
  [Priority.Medium]: '#8a5a00',
  [Priority.High]: '#9a4a05',
  [Priority.Critical]: '#8f1d1d',
}

// Как цвет назван в легенде.
export const PRIORITY_COLOR_NAME: Record<Priority, string> = {
  [Priority.Low]: 'зелёный',
  [Priority.Medium]: 'жёлтый',
  [Priority.High]: 'оранжевый',
  [Priority.Critical]: 'красный',
}
