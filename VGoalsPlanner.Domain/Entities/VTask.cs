using VGoalsPlanner.Domain.Enums;

namespace VGoalsPlanner.Domain.Entities
{
    /// <summary>
    /// Листовая единица планирования — конкретное действие внутри цели (VGoal).
    /// Собственной иерархии не имеет.
    /// </summary>
    public class VTask : AuditableEntity
    {
        /// <summary>Краткое название элемента (обязательное)</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Подробное описание (необязательное)</summary>
        public string? Description { get; set; }

        /// <summary>Желаемый срок выполнения (необязательный)</summary>
        public DateTime? Deadline { get; set; }

        /// <summary>
        /// Когда начали выполнять
        /// </summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// Когда завершили
        /// </summary>
        public DateTime? CompletedAt { get; set; } 

        public int? EstimatedMinutes { get; set; }

        /// <summary>Приоритет выполнения (обязательный, по умолчанию Medium)</summary>
        public Priority Priority { get; set; } = Priority.Medium;

        /// <summary>Текущий статус (обязательный, по умолчанию New)</summary>
        public VTaskStatus Status { get; set; } = VTaskStatus.New;

        /// <summary>Массив тегов для категоризации и поиска (хранится как JSONB)</summary>
        public string[] Tags { get; set; } = Array.Empty<string>();

        /// <summary>Связанные ссылки и пути к папкам/файлам (хранятся как JSONB внутри задачи)</summary>
        public List<TaskLink> Links { get; set; } = new();

        /// <summary>Тип задачи (одноразовый, повторяющий) — определяет его роль</summary>
        public VTaskType Type { get; set; }

        /// <summary>Дни недели, в которые повторяется регулярная задача (маска, см. RecurrenceDays); 0 — не повторяется</summary>
        public int RecurrenceDays { get; set; }

        /// <summary>До какой даты включительно уже созданы повторения в расписании (null — ещё не создавались)</summary>
        public DateOnly? RecurrenceGeneratedUntil { get; set; }

        /// <summary>Порядок среди элементов с одним родителем (для сортировки)</summary>
        public int OrderIndex { get; set; }

        /// <summary>Флаг мягкого удаления (true — элемент скрыт)</summary>
        public bool IsDeleted { get; set; } = false;

        public int CompletedPercent { get; set;  }

        // --- Внешние ключи ---

        /// <summary>Идентификатор родительского элемента (null, если корневой)</summary>
        public Guid GoalId { get; set; }

        /// <summary>Идентификатор пользователя-владельца</summary>
        public Guid UserId { get; set; }

        // --- Навигационные свойства ---

        /// <summary>Родительский элемент (навигация)</summary>
        public virtual VGoal? Goal { get; set; }


        /// <summary>Пользователь-владелец (навигация)</summary>
        public virtual VUser User { get; set; } = null!;
        /// <summary>Логи выполнения (TaskLog), связанные с этим элементом</summary>
        public virtual ICollection<VTaskLog> TaskLogs { get; set; } = new List<VTaskLog>();

        /// <summary>Вложения (файлы в VStorageAPI)</summary>
        public virtual ICollection<VTaskAttachment> Attachments { get; set; } = new List<VTaskAttachment>();

        /// <summary>Связи с днями (VDayVTasks) — планирование на конкретные даты</summary>
        public virtual ICollection<VDayVTask> DayTasks { get; set; } = new List<VDayVTask>();
    }
}
