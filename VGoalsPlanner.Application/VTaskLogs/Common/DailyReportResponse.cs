namespace VGoalsPlanner.Application.VTaskLogs.Common
{
    public sealed class DailyReportResponse
    {
        public DateOnly Date { get; init; }
        public IReadOnlyList<DailyReportTaskGroup> Tasks { get; init; } = Array.Empty<DailyReportTaskGroup>();
    }

    public sealed class DailyReportTaskGroup
    {
        public Guid TaskId { get; init; }
        public required string TaskTitle { get; init; }
        public int TotalTimeSpentMinutes { get; init; }
        public IReadOnlyList<TaskLogResponse> Entries { get; init; } = Array.Empty<TaskLogResponse>();
    }
}
