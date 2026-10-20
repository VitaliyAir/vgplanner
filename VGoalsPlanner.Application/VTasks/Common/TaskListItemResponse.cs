using VGoalsPlanner.Domain.Enums;

namespace VGoalsPlanner.Application.VTasks.Common
{
    public sealed class TaskListItemResponse
    {
        public Guid Id { get; init; }
        public required string Title { get; init; }
        public VTaskStatus Status { get; init; }
        public Priority Priority { get; init; }
        public VTaskType Type { get; init; }
        public DateTime? Deadline { get; init; }
        public int OrderIndex { get; init; }
        public int CompletedPercent { get; init; }
        public Guid GoalId { get; init; }
        public string[] Tags { get; init; } = Array.Empty<string>();
        public int AttachmentCount { get; init; }
    }
}
