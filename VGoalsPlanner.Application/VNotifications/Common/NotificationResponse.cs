namespace VGoalsPlanner.Application.VNotifications.Common
{
    public sealed class NotificationResponse
    {
        public Guid Id { get; init; }
        public Guid DayId { get; init; }
        public DateOnly Date { get; init; }
        public required string Title { get; init; }
        public string? Description { get; init; }
        public DateTime? Deadline { get; init; }
        public bool IsRead { get; init; }
        public bool IsCompleted { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}
