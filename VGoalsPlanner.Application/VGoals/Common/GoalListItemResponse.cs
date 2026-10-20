using VGoalsPlanner.Domain.Enums;

namespace VGoalsPlanner.Application.VGoals.Common
{
    public sealed class GoalListItemResponse
    {
        public Guid Id { get; init; }
        public required string Title { get; init; }
        public Priority Priority { get; init; }
        public VGoalStatus Status { get; init; }
        public Guid? ParentGoalId { get; init; }
        public int OrderIndex { get; init; }
        public bool HasChildGoals { get; init; }
        public bool HasChildTasks { get; init; }
    }
}
