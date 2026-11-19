import type { ElaborationLevel } from '../../api/types'
import { ELABORATION_LEVEL_HINT, ELABORATION_LEVEL_LABEL, ELABORATION_LEVEL_SHORT } from '../../features/goals-tree/labels'

// Цвет заполненных сегментов по уровню: серый (идея) -> жёлтый -> голубой -> зелёный (готово).
// Меняется в одном месте — здесь.
const FILL_COLOR: Record<ElaborationLevel, string> = {
  0: '#9ca3af',
  1: '#f59e0b',
  2: '#38bdf8',
  3: '#22c55e',
}

const EMPTY_COLOR = '#e5e7eb'

const SEGMENTS = [0, 1, 2]

interface ElaborationIconProps {
  level: ElaborationLevel
  /** Высота сегмента в px; ширина считается из пропорции 22:10. */
  size?: number
}

/**
 * Степень проработанности без текста: три сегмента, заполненных по уровню
 * (0 — пусто, 1 — один жёлтый, 2 — два голубых, 3 — три зелёных).
 * Название и расшифровка — в тултипе и aria-label.
 */
export function ElaborationIcon({ level, size = 10 }: ElaborationIconProps) {
  const color = FILL_COLOR[level] ?? FILL_COLOR[0]

  return (
    <svg
      className="elaboration-icon"
      width={Math.round((size * 22) / 10)}
      height={size}
      viewBox="0 0 22 10"
      role="img"
      aria-label={`Проработанность: ${ELABORATION_LEVEL_LABEL[level]}`}
    >
      <title>{`${ELABORATION_LEVEL_SHORT[level]} — ${ELABORATION_LEVEL_HINT[level]}`}</title>
      {SEGMENTS.map((segment) => (
        <rect
          key={segment}
          x={segment * 8}
          y={0}
          width={6}
          height={10}
          rx={3}
          fill={segment < level ? color : EMPTY_COLOR}
        />
      ))}
    </svg>
  )
}
