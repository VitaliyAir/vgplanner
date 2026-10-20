// Ручные TS-зеркала response DTO бэкенда (см.
// VGoalsPlanner.Application/VGoals/Common и VGoalsPlanner.Application/VTasks/Common).
// ASP.NET Core сериализует enum-поля как числа и PascalCase-свойства как camelCase —
// значения ниже скопированы из соответствующих C#-файлов в VGoalsPlanner.Domain/Enums.
//
// Используем "const object + union type" вместо `enum` — tsconfig.app.json включает
// erasableSyntaxOnly (совместимость с нативным стриппингом типов Node), а настоящий
// numeric enum генерирует рантайм-код и под этим флагом запрещён.

export const Priority = {
  Low: 0,
  Medium: 1,
  High: 2,
  Critical: 3,
} as const
export type Priority = (typeof Priority)[keyof typeof Priority]

export const VGoalStatus = {
  New: 0,
  Started: 1,
  Planned: 2,
  Paused: 3,
  Canceled: 4,
  Completed: 5,
} as const
export type VGoalStatus = (typeof VGoalStatus)[keyof typeof VGoalStatus]

export const VTaskStatus = {
  New: 0,
  InProgress: 1,
  Completed: 2,
  OnHold: 3,
  Cancelled: 4,
} as const
export type VTaskStatus = (typeof VTaskStatus)[keyof typeof VTaskStatus]

export const VTaskType = {
  Normal: 0,
  Repeatable: 1,
} as const
export type VTaskType = (typeof VTaskType)[keyof typeof VTaskType]

export const TaskLinkKind = {
  Url: 0,
  Path: 1,
} as const
export type TaskLinkKind = (typeof TaskLinkKind)[keyof typeof TaskLinkKind]

// TaskLinkDto.cs — ссылка (веб-адрес) или путь к папке/файлу, связанные с задачей
export interface TaskLink {
  kind: TaskLinkKind
  target: string
  alias: string | null
}

// GoalListItemResponse.cs
export interface GoalListItem {
  id: string
  title: string
  priority: Priority
  status: VGoalStatus
  parentGoalId: string | null
  orderIndex: number
  hasChildGoals: boolean
  hasChildTasks: boolean
}

// GoalTaskSummaryResponse.cs
export interface GoalTaskSummary {
  id: string
  title: string
  status: VTaskStatus
  priority: Priority
  deadline: string | null
  tags: string[]
  attachmentCount: number
}

// GoalDetailResponse.cs
export interface GoalDetail {
  id: string
  title: string
  description: string | null
  priority: Priority
  status: VGoalStatus
  parentGoalId: string | null
  orderIndex: number
  createdAt: string
  updatedAt: string
  childGoals: GoalListItem[]
  childTasks: GoalTaskSummary[]
}

// Форма ошибки VGoalsPlanner.Domain.Abstractions.Error, которую отдают все контроллеры.
export interface ApiErrorBody {
  code: string
  description: string
}

// CreateGoalRequest.cs
export interface CreateGoalRequest {
  title: string
  description?: string | null
  priority: Priority
  parentGoalId?: string | null
}

// UpdateGoalRequest.cs
export interface UpdateGoalRequest {
  title: string
  description?: string | null
  priority: Priority
}

// TaskDetailResponse.cs — намеренное подмножество: Logs/Schedule этой фиче не
// нужны, здесь только поля, которые редактирует TaskForm.
export interface TaskDetail {
  id: string
  title: string
  description: string | null
  status: VTaskStatus
  priority: Priority
  type: VTaskType
  deadline: string | null
  estimatedMinutes: number | null
  tags: string[]
  links: TaskLink[]
  recurrenceDays: number
  attachments: TaskAttachment[]
  goalId: string
}

// TaskAttachmentResponse.cs
export interface TaskAttachment {
  id: string
  fileName: string
  contentType: string
  sizeBytes: number
  createdAt: string
}

// CreateTaskRequest.cs
export interface CreateTaskRequest {
  goalId: string
  title: string
  description?: string | null
  deadline?: string | null
  priority: Priority
  type: VTaskType
  estimatedMinutes?: number | null
  tags?: string[]
  links?: TaskLink[]
  recurrenceDays: number
}

// UpdateTaskRequest.cs
export interface UpdateTaskRequest {
  title: string
  description?: string | null
  deadline?: string | null
  priority: Priority
  type: VTaskType
  estimatedMinutes?: number | null
  tags?: string[]
  links?: TaskLink[]
  recurrenceDays: number
}

// PagedResult<T>.cs
export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

// ActiveTaskResponse.cs
export interface ActiveTask {
  id: string
  title: string
  description: string | null
  goalId: string
}

// TaskListItemResponse.cs
export interface TaskListItem {
  id: string
  title: string
  status: VTaskStatus
  priority: Priority
  type: VTaskType
  deadline: string | null
  orderIndex: number
  completedPercent: number
  goalId: string
  tags: string[]
  attachmentCount: number
}

// ScheduledTaskResponse.cs
export interface ScheduledTask {
  taskId: string
  title: string
  status: VTaskStatus
  priority: Priority
  type: VTaskType
  goalId: string
  deadline: string | null
  completedPercent: number
  orderIndex: number
  hasLog: boolean
  isActive: boolean
  isRecurring: boolean
  isVirtual: boolean
}

// DayScheduleResponse.cs
export interface DaySchedule {
  date: string
  specialNote: string | null
  tasks: ScheduledTask[]
  notifications: VNotificationItem[]
}

// NotificationResponse.cs
export interface VNotificationItem {
  id: string
  dayId: string
  date: string
  title: string
  description: string | null
  deadline: string | null
  isRead: boolean
  isCompleted: boolean
  createdAt: string
  updatedAt: string
}

// TaskLogResponse.cs
export interface TaskLog {
  id: string
  taskId: string
  dayId: string
  date: string
  note: string | null
  attachments: string[]
  timeSpentMinutes: number | null
  oldStatus: VTaskStatus | null
  newStatus: VTaskStatus | null
  createdByUserId: string
  createdAt: string
  updatedAt: string
}

// TimelineLogEntryResponse.cs
export interface TimelineLogEntry {
  id: string
  taskId: string
  taskTitle: string
  note: string | null
  attachments: string[]
  timeSpentMinutes: number | null
  oldStatus: VTaskStatus | null
  newStatus: VTaskStatus | null
  createdAt: string
  updatedAt: string
}

// TimelineDayResponse.cs
export interface TimelineDay {
  date: string
  specialNote: string | null
  logs: TimelineLogEntry[]
}

// StatisticsResponse.cs
export interface StatusCount {
  status: number
  count: number
}

export interface StatusStatistics {
  total: number
  byStatus: StatusCount[]
}

export interface GoalStatistics {
  total: number
}

export interface Statistics {
  goals: GoalStatistics
  tasks: StatusStatistics
}
