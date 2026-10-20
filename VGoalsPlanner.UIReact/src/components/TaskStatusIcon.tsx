import { VTaskStatus } from '../api/types'
import { TASK_STATUS_LABEL } from '../features/goals-tree/labels'
import './taskStatusIcon.css'

const GREY = '#dbdbdb'
const YELLOW = '#fdd835'
const BLUE = '#00b0ff'
const GREEN = '#00c853'
const RED = '#ff1744'

// Иконки статусов задач (как в легенде): кольцо — ещё не завершена, залитый круг — завершена/отменена.
export function TaskStatusIcon({ status, size = 16 }: { status: VTaskStatus; size?: number }) {
  const label = TASK_STATUS_LABEL[status]

  return (
    <svg className="task-status-icon" viewBox="0 0 24 24" width={size} height={size} role="img" aria-label={label}>
      <title>{label}</title>
      {status === VTaskStatus.New && <circle cx="12" cy="12" r="9.5" fill="none" stroke={GREY} strokeWidth="2.5" />}
      {status === VTaskStatus.OnHold && (
        <>
          <circle cx="12" cy="12" r="9.5" fill="none" stroke={YELLOW} strokeWidth="2.5" />
          <path d="M12 6.5V12l3.5 2.5" fill="none" stroke={YELLOW} strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round" />
        </>
      )}
      {status === VTaskStatus.InProgress && (
        <>
          <circle cx="12" cy="12" r="9.5" fill="none" stroke={GREEN} strokeWidth="2.5" />
          <path d="M12 12V6a6 6 0 1 0 6 6z" fill={GREEN} />
        </>
      )}
      {status === VTaskStatus.Completed && (
        <>
          <circle cx="12" cy="12" r="11" fill={BLUE} />
          <path d="M7 12.5l3.5 3.5L17.5 8.5" fill="none" stroke="#fff" strokeWidth="2.6" strokeLinecap="round" strokeLinejoin="round" />
        </>
      )}
      {status === VTaskStatus.Cancelled && (
        <>
          <circle cx="12" cy="12" r="11" fill={RED} />
          <path d="M8 8l8 8M16 8l-8 8" fill="none" stroke="#fff" strokeWidth="2.6" strokeLinecap="round" />
        </>
      )}
    </svg>
  )
}
