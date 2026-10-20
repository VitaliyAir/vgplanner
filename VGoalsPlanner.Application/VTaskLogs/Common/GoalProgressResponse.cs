namespace VGoalsPlanner.Application.VTaskLogs.Common
{
    public sealed class GoalProgressResponse
    {
        public Guid GoalId { get; init; }
        public int TaskCount { get; init; }
        public int CompletedTaskCount { get; init; }
        public double AverageCompletedPercent { get; init; }
    }
}
