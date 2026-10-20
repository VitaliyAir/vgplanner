namespace VGoalsPlanner.API.DTOs
{
    public sealed class ReorderDayTasksRequest
    {
        public List<Guid> OrderedTaskIds { get; init; } = new();
    }
}
