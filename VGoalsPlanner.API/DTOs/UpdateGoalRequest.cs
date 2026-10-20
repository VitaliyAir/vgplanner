using VGoalsPlanner.Domain.Enums;

namespace VGoalsPlanner.API.DTOs
{
    public sealed class UpdateGoalRequest
    {
        public required string Title { get; init; }
        public string? Description { get; init; }
        public Priority Priority { get; init; }
    }
}
