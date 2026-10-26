using VGoalsPlanner.Application.Statistics;

namespace VGoalsPlanner.Application.VTaskLogs.Common
{
    public sealed class TimelineDayResponse
    {
        public DateOnly Date { get; init; }
        public string? SpecialNote { get; init; }
        public IReadOnlyList<TimelineLogEntryResponse> Logs { get; init; } = Array.Empty<TimelineLogEntryResponse>();

        /// <summary>Снимок статистики на конец дня (если за этот день он есть).</summary>
        public StatisticsResponse? Statistics { get; init; }
    }
}
