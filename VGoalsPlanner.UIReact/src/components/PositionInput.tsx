import { useRef, useState } from 'react'
import './positionInput.css'

// Положение элемента в списке + как его переместить на выбранную позицию (0 — самый верх).
export interface ListPosition {
  index: number
  count: number
  onMoveTo: (targetIndex: number) => void
}

interface PositionInputProps extends ListPosition {
  disabled?: boolean
}

export function PositionInput({ index, count, onMoveTo, disabled }: PositionInputProps) {
  // null — поле не редактируется, показываем реальную позицию (после ошибки сохранения само вернётся к ней).
  const [draft, setDraft] = useState<string | null>(null)
  const cancelled = useRef(false)

  const commit = () => {
    const text = draft
    setDraft(null)
    if (cancelled.current) {
      cancelled.current = false
      return
    }
    const parsed = Number.parseInt(text ?? '', 10)
    if (Number.isNaN(parsed)) {
      return
    }
    const target = Math.min(Math.max(parsed, 0), count - 1)
    if (target !== index) {
      onMoveTo(target)
    }
  }

  return (
    <input
      type="number"
      className="position-input"
      min={0}
      max={Math.max(count - 1, 0)}
      value={draft ?? String(index)}
      disabled={disabled || count < 2}
      title={`Позиция в списке: 0 — вверху, ${Math.max(count - 1, 0)} — внизу. Enter — переместить`}
      aria-label="Позиция в списке"
      onFocus={(e) => {
        setDraft(String(index))
        e.currentTarget.select()
      }}
      onChange={(e) => setDraft(e.target.value)}
      onBlur={commit}
      onKeyDown={(e) => {
        if (e.key === 'Enter') {
          e.currentTarget.blur()
        } else if (e.key === 'Escape') {
          cancelled.current = true
          e.currentTarget.blur()
        }
      }}
    />
  )
}
