namespace VGoalsPlanner.Domain.Entities
{
    /// <summary>
    /// Базовый класс для сущностей, поддерживающих аудит времени создания и обновления.
    /// </summary>
    public abstract class AuditableEntity
    {
        /// <summary>
        /// Уникальный идентификатор сущности (генерируется при создании).
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Дата и время создания записи (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Дата и время последнего обновления записи (UTC).
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Кем создано
        /// </summary>
        public Guid CreatedBy { get; set; }

        /// <summary>
        /// Кем обновлено
        /// </summary>
        public Guid? UpdatedBy { get; set; }
    }
}
