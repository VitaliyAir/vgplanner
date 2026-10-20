import type { Priority } from '../api/types'
import { PRIORITY_LABEL, PRIORITY_OPTIONS } from '../features/goals-tree/labels'
import { PRIORITY_COLOR, PRIORITY_TEXT_COLOR } from './priorityPalette'
import './priorityDotsPicker.css'

// Цвет рамки, названия и точки зависит от выбранного приоритета (фон бокса всегда белый): зелёный → жёлтый → оранжевый → красный.

interface PriorityDotsPickerProps {
  name: string
  value: Priority
  onChange: (value: Priority) => void
  disabled?: boolean
}

export function PriorityDotsPicker({ name, value, onChange, disabled = false }: PriorityDotsPickerProps) {
  const selected = { dot: PRIORITY_COLOR[value], text: PRIORITY_TEXT_COLOR[value] }

  return (
    <div
      className="priority-dots"
      role="radiogroup"
      aria-label="Приоритет"
      style={{ color: selected.text, borderColor: selected.dot }}
    >
      <span className="priority-dots-name">{PRIORITY_LABEL[value]}</span>

      <span className="priority-dots-circles">
        {PRIORITY_OPTIONS.map((option) => {
          const isSelected = option === value
          return (
            <label
              key={option}
              className="priority-dots-circle"
              title={PRIORITY_LABEL[option]}
              style={{
                borderColor: selected.dot,
                background: isSelected ? selected.dot : 'transparent',
              }}
            >
              <input
                type="radio"
                name={name}
                value={option}
                checked={isSelected}
                onChange={() => onChange(option)}
                disabled={disabled}
                aria-label={PRIORITY_LABEL[option]}
              />
            </label>
          )
        })}
      </span>
    </div>
  )
}
