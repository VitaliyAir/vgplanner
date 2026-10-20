import type { Priority } from '../api/types'
import { PRIORITY_LABEL } from '../features/goals-tree/labels'
import { PRIORITY_COLOR } from './priorityPalette'
import './priorityFlag.css'

// Флажок цвета приоритета — только полотнище, висит на верхней рамке элемента внутри него (см. priorityFlag.css).
// inline — флажок в строке текста (например, в плотной строке планирования), а не на рамке элемента.
export function PriorityFlag({ priority, inline = false }: { priority: Priority; inline?: boolean }) {
  const label = `Приоритет: ${PRIORITY_LABEL[priority]}`

  return (
    <svg
      className={inline ? 'priority-flag priority-flag--inline' : 'priority-flag'}
      viewBox="0 0 14 8"
      width="14"
      height="8"
      role="img"
      aria-label={label}
    >
      <title>{label}</title>
      <path d="M0 0h14l-3 4 3 4H0z" fill={PRIORITY_COLOR[priority]} />
    </svg>
  )
}
