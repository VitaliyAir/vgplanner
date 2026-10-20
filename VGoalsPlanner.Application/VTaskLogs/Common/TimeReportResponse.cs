namespace VGoalsPlanner.Application.VTaskLogs.Common
{
    public sealed class TimeReportResponse
    {
        public DateOnly From { get; init; }
        public DateOnly To { get; init; }
        public int TotalMinutes { get; init; }
        public IReadOnlyList<TimeReportDayEntry> ByDay { get; init; } = Array.Empty<TimeReportDayEntry>();
        public IReadOnlyList<TimeReportTaskEntry> ByTask { get; init; } = Array.Empty<TimeReportTaskEntry>();
    }

    public sealed class TimeReportDayEntry
    {
        public DateOnly Date { get; init; }
        public int Minutes { get; init; }
    }

    public sealed class TimeReportTaskEntry
    {
        public Guid TaskId { get; init; }
        public required string TaskTitle { get; init; }
        public int Minutes { get; init; }
    }
}
