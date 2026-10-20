using VGoalsPlanner.Domain.Enums;

namespace VGoalsPlanner.Application.VTaskLogs.Common
{
    public sealed class TaskLogResponse
    {
        public Guid Id { get; init; }
        public Guid TaskId { get; init; }
        public Guid DayId { get; init; }
        public DateOnly Date { get; init; }
        public string? Note { get; init; }
        public string[] Attachments { get; init; } = Array.Empty<string>();
        public int? TimeSpentMinutes { get; init; }
        public VTaskStatus? OldStatus { get; init; }
        public VTaskStatus? NewStatus { get; init; }
        public Guid CreatedByUserId { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}
