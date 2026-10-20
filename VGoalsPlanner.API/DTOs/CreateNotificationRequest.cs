namespace VGoalsPlanner.API.DTOs
{
    public sealed class CreateNotificationRequest
    {
        public DateOnly Date { get; init; }
        public required string Title { get; init; }
        public string? Description { get; init; }
        public DateTime? Deadline { get; init; }
    }
}
