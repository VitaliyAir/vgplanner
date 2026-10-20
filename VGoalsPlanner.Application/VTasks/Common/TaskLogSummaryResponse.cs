using VGoalsPlanner.Domain.Enums;

namespace VGoalsPlanner.Application.VTasks.Common
{
    public sealed class TaskLogSummaryResponse
    {
        public Guid Id { get; init; }
        public string? Note { get; init; }
        public string[] Attachments { get; init; } = Array.Empty<string>();
        public int? TimeSpentMinutes { get; init; }
        public VTaskStatus? OldStatus { get; init; }
        public VTaskStatus? NewStatus { get; init; }
        public Guid DayId { get; init; }
        public Guid CreatedByUserId { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}
