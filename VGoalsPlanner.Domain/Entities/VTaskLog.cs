using VGoalsPlanner.Domain.Enums;

namespace VGoalsPlanner.Domain.Entities
{
    public class VTaskLog : AuditableEntity
    {
        public string? Note { get; set; }
        public string[] Attachments { get; set; } = Array.Empty<string>();
        public int? TimeSpentMinutes { get; set; }
        public VTaskStatus? OldStatus { get; set; }
        public VTaskStatus? NewStatus { get; set; }

        public Guid VTaskId { get; set; }
        public Guid CreatedByUserId { get; set; }
        public Guid DayId { get; set; }

        public virtual VTask VTask { get; set; } = null!;        // было GoalItem
        public virtual VUser CreatedByUser { get; set; } = null!;
        public virtual VDay Day { get; set; } = null!;
    }
}
