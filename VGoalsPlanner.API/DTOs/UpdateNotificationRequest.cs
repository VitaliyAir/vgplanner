namespace VGoalsPlanner.API.DTOs
{
    public sealed class UpdateNotificationRequest
    {
        public required string Title { get; init; }
        public string? Description { get; init; }
        public DateTime? Deadline { get; init; }
    }
}
