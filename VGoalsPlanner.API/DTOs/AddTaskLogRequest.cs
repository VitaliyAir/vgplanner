using VGoalsPlanner.Domain.Enums;

namespace VGoalsPlanner.API.DTOs
{
    public sealed class AddTaskLogRequest
    {
        public Guid TaskId { get; init; }
        public DateOnly Date { get; init; }
        public string? Note { get; init; }
        public string[]? Attachments { get; init; }
        public int? TimeSpentMinutes { get; init; }
        public VTaskStatus? OldStatus { get; init; }
        public VTaskStatus? NewStatus { get; init; }
    }
}
