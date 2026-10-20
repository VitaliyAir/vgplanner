using VGoalsPlanner.Domain.Enums;

namespace VGoalsPlanner.Domain.Entities
{
    public class AuditLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? ChangesJson { get; set; }
        public Guid UserId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public virtual VUser User { get; set; } = null!;
    }
}
