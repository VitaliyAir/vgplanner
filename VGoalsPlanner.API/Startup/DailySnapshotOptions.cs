namespace VGoalsPlanner.API.Startup
{
    /// <summary>Настройки ежедневного снимка статистики (секция DailyStatisticsSnapshot).</summary>
    public sealed class DailySnapshotOptions
    {
        public const string SectionName = "DailyStatisticsSnapshot";

        public bool Enabled { get; set; } = true;

        /// <summary>Cron Quartz (с секундами). По умолчанию 23:55 каждый день.</summary>
        public string Cron { get; set; } = "0 55 23 * * ?";

        /// <summary>Часовой пояс «конца дня» (IANA или Windows id). По умолчанию UTC, как «сегодня» в остальном приложении.</summary>
        public string TimeZone { get; set; } = "UTC";

        public TimeZoneInfo ResolveTimeZone() => TimeZoneInfo.FindSystemTimeZoneById(TimeZone);
    }
}
