namespace VGoalsPlanner.Domain.Entities
{
    /// <summary>
    /// Календарный день пользователя, содержащий запланированные задачи и заметки.
    /// </summary>
    public class VDay : AuditableEntity
    {
        /// <summary>Дата дня (без времени) — уникальна для каждого пользователя</summary>
        public DateOnly Date { get; set; }

        /// <summary>Особая заметка о дне (например, "Выходной", "Дождливый день")</summary>
        public string? SpecialNote { get; set; }

        /// <summary>Идентификатор пользователя-владельца</summary>
        public Guid UserId { get; set; }

        // --- Навигационные свойства ---

        /// <summary>Пользователь-владелец</summary>
        public virtual VUser User { get; set; } = null!;

        /// <summary>Задачи, запланированные на этот день (через DayItems)</summary>
        public virtual ICollection<VDayVTask> DayTasks { get; set; } = new List<VDayVTask>();

        /// <summary>Логи выполнения (TaskLog), привязанные к этому дню</summary>
        public virtual ICollection<VTaskLog> TaskLogs { get; set; } = new List<VTaskLog>();

        public virtual ICollection<VNotification> Notifications { get; set; } = new List<VNotification>();
    }
}
