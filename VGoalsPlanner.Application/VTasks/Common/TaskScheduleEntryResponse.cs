namespace VGoalsPlanner.Application.VTasks.Common
{
    public sealed class TaskScheduleEntryResponse
    {
        public Guid DayId { get; init; }
        public DateOnly Date { get; init; }
        public int OrderIndex { get; init; }
    }
}
