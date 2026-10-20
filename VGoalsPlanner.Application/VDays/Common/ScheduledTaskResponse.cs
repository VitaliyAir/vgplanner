using VGoalsPlanner.Domain.Enums;

namespace VGoalsPlanner.Application.VDays.Common
{
    public sealed class ScheduledTaskResponse
    {
        public Guid TaskId { get; init; }
        public required string Title { get; init; }
        public VTaskStatus Status { get; init; }
        public Priority Priority { get; init; }
        public VTaskType Type { get; init; }
        public Guid GoalId { get; init; }
        public DateTime? Deadline { get; init; }
        public int CompletedPercent { get; init; }

        /// <summary>Порядок задачи в рамках дня (VDayVTask.OrderIndex).</summary>
        public int OrderIndex { get; init; }

        /// <summary>Есть ли у задачи запись TaskLog именно в этот день.</summary>
        public bool HasLog { get; init; }

        /// <summary>Отмечена ли задача как активная именно в этот день (VDayVTask.IsActive).</summary>
        public bool IsActive { get; init; }

        /// <summary>Повторение регулярной задачи (по дням недели).</summary>
        public bool IsRecurring { get; init; }

        /// <summary>Будущее повторение, ещё не записанное в расписание (только отображение, без порядка/удаления).</summary>
        public bool IsVirtual { get; init; }
    }
}
