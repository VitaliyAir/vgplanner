namespace VGoalsPlanner.API.Startup
{
    /// <summary>Настройки циклической записи журнала аудита (секция AuditRecycle).</summary>
    public sealed class AuditRecycleOptions
    {
        public const string SectionName = "AuditRecycle";

        /// <summary>
        /// Хранить аудит по кругу: записи старше <see cref="MaxDaysAuditRecordLifecycle"/> удаляются
        /// раз в месяц. false — задание не планируется, журнал копится целиком (по умолчанию true).
        /// </summary>
        public bool RecycleAudit { get; set; } = true;

        /// <summary>Сколько дней хранить записи. Всё, что старше «сегодня − N дней», удаляется.</summary>
        public int MaxDaysAuditRecordLifecycle { get; set; } = 365;

        /// <summary>Cron Quartz (с секундами). По умолчанию 03:00 первого числа каждого месяца.</summary>
        public string Cron { get; set; } = "0 0 3 1 * ?";

        /// <summary>Часовой пояс расписания (IANA или Windows id) — чтобы «1-е число» было местным.</summary>
        public string TimeZone { get; set; } = "UTC";

        public TimeZoneInfo ResolveTimeZone() => TimeZoneInfo.FindSystemTimeZoneById(TimeZone);
    }
}
