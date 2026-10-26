namespace VGoalsPlanner.Domain.Entities
{
    /// <summary>
    /// Снимок статистики пользователя на конец дня; пишется ежедневным заданием Quartz.
    /// </summary>
    public class VDayStatistics : AuditableEntity
    {
        public Guid UserId { get; set; }

        /// <summary>Дата дня (в часовом поясе, заданном в DailyStatisticsSnapshot:TimeZone)</summary>
        public DateOnly Date { get; set; }

        public int GoalsTotal { get; set; }

        // По колонке на статус (VTaskStatus): простая схема без jsonb; новый статус = новая колонка + миграция.
        public int TasksNew { get; set; }
        public int TasksInProgress { get; set; }
        public int TasksCompleted { get; set; }
        public int TasksOnHold { get; set; }
        public int TasksCancelled { get; set; }

        public virtual VUser User { get; set; } = null!;
    }
}
