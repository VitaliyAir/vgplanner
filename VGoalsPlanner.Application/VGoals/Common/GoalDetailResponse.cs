using VGoalsPlanner.Domain.Enums;

namespace VGoalsPlanner.Application.VGoals.Common
{
    public sealed class GoalDetailResponse
    {
        public Guid Id { get; init; }
        public required string Title { get; init; }
        public string? Description { get; init; }
        public Priority Priority { get; init; }
        public VGoalStatus Status { get; init; }
        public Guid? ParentGoalId { get; init; }
        public int OrderIndex { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
        public IReadOnlyList<GoalListItemResponse> ChildGoals { get; init; } = Array.Empty<GoalListItemResponse>();
        public IReadOnlyList<GoalTaskSummaryResponse> ChildTasks { get; init; } = Array.Empty<GoalTaskSummaryResponse>();
    }
}
