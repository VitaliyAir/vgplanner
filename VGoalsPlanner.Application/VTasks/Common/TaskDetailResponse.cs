using VGoalsPlanner.Domain.Enums;

namespace VGoalsPlanner.Application.VTasks.Common
{
    public sealed class TaskDetailResponse
    {
        public Guid Id { get; init; }
        public required string Title { get; init; }
        public string? Description { get; init; }
        public VTaskStatus Status { get; init; }
        public Priority Priority { get; init; }
        public VTaskType Type { get; init; }
        public DateTime? Deadline { get; init; }
        public DateTime? StartedAt { get; init; }
        public DateTime? CompletedAt { get; init; }
        public int? EstimatedMinutes { get; init; }
        public int RecurrenceDays { get; init; }
        public int CompletedPercent { get; init; }
        public int OrderIndex { get; init; }
        public Guid GoalId { get; init; }
        public string[] Tags { get; init; } = Array.Empty<string>();
        public IReadOnlyList<TaskLinkDto> Links { get; init; } = Array.Empty<TaskLinkDto>();
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
        public IReadOnlyList<TaskLogSummaryResponse> Logs { get; init; } = Array.Empty<TaskLogSummaryResponse>();
        public IReadOnlyList<TaskScheduleEntryResponse> Schedule { get; init; } = Array.Empty<TaskScheduleEntryResponse>();
        public IReadOnlyList<TaskAttachmentResponse> Attachments { get; init; } = Array.Empty<TaskAttachmentResponse>();
    }
}
