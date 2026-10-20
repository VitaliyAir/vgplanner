using VGoalsPlanner.Domain.Enums;

namespace VGoalsPlanner.Domain.Entities
{
    public class VGoal : AuditableEntity
    {
        /// <summary>Краткое название элемента (обязательное)</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Подробное описание (необязательное)</summary>
        public string? Description { get; set; }

        /// <summary>Приоритет выполнения (обязательный, по умолчанию Medium)</summary>
        public Priority Priority { get; set; } = Priority.Medium;

        /// <summary>Текущий статус (обязательный, по умолчанию New)</summary>
        public VGoalStatus Status { get; set; } = VGoalStatus.New;

        /// <summary>Порядок среди элементов с одним родителем (для сортировки)</summary>
        public int OrderIndex { get; set; }

        /// <summary>Флаг мягкого удаления (true — элемент скрыт)</summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>Идентификатор родительского элемента (null, если корневой)</summary>
        public Guid? ParentGoalId { get; set; }

        /// <summary>Идентификатор пользователя-владельца</summary>
        public Guid UserId { get; set; }

        /// <summary>Родительский элемент (навигация)</summary>
        public virtual VGoal? ParentGoal { get; set; }

        /// <summary>Коллекция дочерних элементов</summary>
        public virtual ICollection<VGoal> ChildGoals { get; set; } = new List<VGoal>();

        /// <summary>Коллекция дочерних элементов</summary>
        public virtual ICollection<VTask> ChildTasks { get; set; } = new List<VTask>();

        /// <summary>Пользователь-владелец (навигация)</summary>
        public virtual VUser User { get; set; } = null!;
    }
}
