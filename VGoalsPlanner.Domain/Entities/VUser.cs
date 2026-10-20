namespace VGoalsPlanner.Domain.Entities
{
    /// <summary>
    /// Пользователь
    /// </summary>
    public class VUser   // : IdentityUser<Guid>
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Ник пользователя
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        public virtual ICollection<VTask> VTasks { get; set; } = new List<VTask>(); // было GoalItems
        public virtual ICollection<VGoal> Goals { get; set; } = new List<VGoal>();
        public virtual ICollection<VDay> Days { get; set; } = new List<VDay>();
        public virtual ICollection<VTaskLog> TaskLogs { get; set; } = new List<VTaskLog>();
        public virtual ICollection<VNotification> Notifications { get; set; } = new List<VNotification>();
        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    }
}
