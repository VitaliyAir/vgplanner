namespace VGoalsPlanner.Application.Statistics
{
    public sealed record StatusCount(int Status, int Count);

    public sealed class StatusStatistics
    {
        public int Total { get; init; }
        public IReadOnlyList<StatusCount> ByStatus { get; init; } = Array.Empty<StatusCount>();
    }

    public sealed class GoalStatistics
    {
        public int Total { get; init; }
    }

    public sealed class StatisticsResponse
    {
        public required GoalStatistics Goals { get; init; }
        public required StatusStatistics Tasks { get; init; }
    }
}
