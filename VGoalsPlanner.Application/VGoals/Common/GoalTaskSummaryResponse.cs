using VGoalsPlanner.Domain.Enums;

namespace VGoalsPlanner.Application.VGoals.Common
{
    public sealed class GoalTaskSummaryResponse
    {
        public Guid Id { get; init; }
        public required string Title { get; init; }
        public VTaskStatus Status { get; init; }
        public Priority Priority { get; init; }
        public DateTime? Deadline { get; init; }
        public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
        public int AttachmentCount { get; init; }
    }
}
