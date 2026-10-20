namespace VGoalsPlanner.Domain.Entities
{
    public class VNotification : AuditableEntity
    {
        /// <summary>Краткое название элемента (обязательное)</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Подробное описание (необязательное)</summary>
        public string? Description { get; set; }

        /// <summary>Желаемый срок выполнения (необязательный)</summary>
        public DateTime? Deadline { get; set; }

        public bool IsRead { get; set; }

        public bool IsCompleted { get; set; }

        // Внешние ключи
        public Guid DayId { get; set; }
        public Guid UserId { get; set; }

        // Навигация
        public virtual VDay Day { get; set; } = null!;
        public virtual VUser User { get; set; } = null!;

    }
}
