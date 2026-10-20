namespace VGoalsPlanner.API.DTOs
{
    public sealed class ReorderGoalsRequest
    {
        public Guid? ParentGoalId { get; init; }
        public List<Guid> OrderedGoalIds { get; init; } = new();
    }
}
