using VGoalsPlanner.Application.VNotifications.Common;

namespace VGoalsPlanner.Application.VDays.Common
{
    public sealed class DayScheduleResponse
    {
        public DateOnly Date { get; init; }
        public string? SpecialNote { get; init; }
        public IReadOnlyList<ScheduledTaskResponse> Tasks { get; init; } = Array.Empty<ScheduledTaskResponse>();
        public IReadOnlyList<NotificationResponse> Notifications { get; init; } = Array.Empty<NotificationResponse>();
    }
}
