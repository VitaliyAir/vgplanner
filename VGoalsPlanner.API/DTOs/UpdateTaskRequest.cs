using VGoalsPlanner.Application.VTasks.Common;
using VGoalsPlanner.Domain.Enums;

namespace VGoalsPlanner.API.DTOs
{
    public sealed class UpdateTaskRequest
    {
        public required string Title { get; init; }
        public string? Description { get; init; }
        public DateTime? Deadline { get; init; }
        public Priority Priority { get; init; }
        public VTaskType Type { get; init; }
        public int? EstimatedMinutes { get; init; }
        public string[]? Tags { get; init; }
        public int RecurrenceDays { get; init; }
        public TaskLinkDto[]? Links { get; init; }
    }
}
