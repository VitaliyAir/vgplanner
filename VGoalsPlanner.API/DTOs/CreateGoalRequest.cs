using VGoalsPlanner.Domain.Enums;

namespace VGoalsPlanner.API.DTOs
{
    public sealed class CreateGoalRequest
    {
        public required string Title { get; init; }
        public string? Description { get; init; }
        public Priority Priority { get; init; } = Priority.Medium;
        public Guid? ParentGoalId { get; init; }
    }
}
