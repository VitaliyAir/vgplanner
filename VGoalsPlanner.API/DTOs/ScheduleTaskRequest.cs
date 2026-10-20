namespace VGoalsPlanner.API.DTOs
{
    public sealed class ScheduleTaskRequest
    {
        public Guid TaskId { get; init; }
        public int? OrderIndex { get; init; }
    }
}
