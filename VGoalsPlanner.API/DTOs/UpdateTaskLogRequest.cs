namespace VGoalsPlanner.API.DTOs
{
    public sealed class UpdateTaskLogRequest
    {
        public string? Note { get; init; }
        public string[]? Attachments { get; init; }
    }
}
