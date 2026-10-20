namespace VGoalsPlanner.Application.VTasks.Common
{
    public sealed class ActiveTaskResponse
    {
        public Guid Id { get; init; }
        public required string Title { get; init; }
        public string? Description { get; init; }
        public Guid GoalId { get; init; }
    }
}
