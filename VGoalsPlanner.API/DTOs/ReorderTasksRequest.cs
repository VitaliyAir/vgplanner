namespace VGoalsPlanner.API.DTOs
{
    public sealed class ReorderTasksRequest
    {
        public Guid GoalId { get; init; }
        public List<Guid> OrderedTaskIds { get; init; } = new();
    }
}
