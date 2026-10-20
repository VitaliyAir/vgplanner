namespace VGoalsPlanner.Application.VTasks.Common
{
    public sealed class TaskAttachmentResponse
    {
        public Guid Id { get; init; }
        public required string FileName { get; init; }
        public required string ContentType { get; init; }
        public long SizeBytes { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}
