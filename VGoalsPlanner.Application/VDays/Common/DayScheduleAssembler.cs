using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.VNotifications.Common;
using VGoalsPlanner.Domain.Entities;
using VGoalsPlanner.Domain.Enums;

namespace VGoalsPlanner.Application.VDays.Common
{
    /// <summary>
    /// Собирает расписание (день + отсортированные задачи) для набора дат.
    /// Строки без VDay-записи возвращаются с пустым списком задач — календарь всегда полный.
    /// </summary>
    internal static class DayScheduleAssembler
    {
        public static async Task<List<DayScheduleResponse>> BuildAsync(
            IApplicationDbContext context,
            Guid userId,
            IReadOnlyList<DateOnly> dates,
            CancellationToken cancellationToken)
        {
            if (dates.Count == 0)
            {
                return new List<DayScheduleResponse>();
            }

            await RecurringTaskMaterializer.EnsureUpToDateAsync(context, userId, cancellationToken);

            var first = dates.Min();
            var last = dates.Max();

            var days = await context.Days.AsNoTracking()
                .Where(d => d.UserId == userId && d.Date >= first && d.Date <= last)
                .ToListAsync(cancellationToken);

            var dayByDate = days.ToDictionary(d => d.Date);
            var dateByDayId = days.ToDictionary(d => d.Id, d => d.Date);
            var dayIds = days.Select(d => d.Id).ToList();

            var loggedPairs = dayIds.Count == 0
                ? new HashSet<(Guid DayId, Guid TaskId)>()
                : (await context.TaskLogs.AsNoTracking()
                    .Where(l => dayIds.Contains(l.DayId))
                    .Select(l => new { l.DayId, l.VTaskId })
                    .Distinct()
                    .ToListAsync(cancellationToken))
                    .Select(x => (x.DayId, x.VTaskId))
                    .ToHashSet();

            var notifications = dayIds.Count == 0
                ? new()
                : await context.Notifications.AsNoTracking()
                    .Where(n => dayIds.Contains(n.DayId))
                    .ToListAsync(cancellationToken);

            var notificationsByDayId = notifications
                .GroupBy(n => n.DayId)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<NotificationResponse>)g
                        .OrderBy(n => n.CreatedAt)
                        .Select(n => new NotificationResponse
                        {
                            Id = n.Id,
                            DayId = n.DayId,
                            Date = dateByDayId[n.DayId],
                            Title = n.Title,
                            Description = n.Description,
                            Deadline = n.Deadline,
                            IsRead = n.IsRead,
                            IsCompleted = n.IsCompleted,
                            CreatedAt = n.CreatedAt,
                            UpdatedAt = n.UpdatedAt
                        })
                        .ToList());

            var scheduled = dayIds.Count == 0
                ? new()
                : await (
                    from dt in context.DayTasks.AsNoTracking()
                    join t in context.Tasks on dt.VTaskId equals t.Id
                    where dayIds.Contains(dt.VDayId)
                    select new
                    {
                        dt.VDayId,
                        dt.OrderIndex,
                        dt.IsActive,
                        TaskId = t.Id,
                        t.Title,
                        t.Status,
                        t.Priority,
                        t.Type,
                        t.GoalId,
                        t.Deadline,
                        t.CompletedPercent,
                        t.RecurrenceDays
                    }).ToListAsync(cancellationToken);

            var tasksByDayId = scheduled
                .GroupBy(r => r.VDayId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(r => r.OrderIndex)
                        .Select(r => new ScheduledTaskResponse
                        {
                            TaskId = r.TaskId,
                            Title = r.Title,
                            Status = r.Status,
                            Priority = r.Priority,
                            Type = r.Type,
                            GoalId = r.GoalId,
                            Deadline = r.Deadline,
                            CompletedPercent = r.CompletedPercent,
                            OrderIndex = r.OrderIndex,
                            HasLog = loggedPairs.Contains((g.Key, r.TaskId)),
                            IsActive = r.IsActive,
                            IsRecurring = r.Type == VTaskType.Repeatable && r.RecurrenceDays != 0
                        })
                        .ToList());

            // Будущие повторения регулярных задач (после горизонта материализации) — только для показа.
            var horizon = RecurringTaskMaterializer.Horizon;
            var recurringTasks = dates.Any(d => d > horizon)
                ? await context.Tasks.AsNoTracking()
                    .Where(t => t.UserId == userId
                        && t.Type == VTaskType.Repeatable
                        && t.RecurrenceDays != 0
                        && (t.Status == VTaskStatus.New || t.Status == VTaskStatus.InProgress))
                    .OrderBy(t => t.CreatedAt)
                    .ToListAsync(cancellationToken)
                : new List<VTask>();

            var result = new List<DayScheduleResponse>(dates.Count);
            foreach (var date in dates)
            {
                dayByDate.TryGetValue(date, out var day);

                IReadOnlyList<ScheduledTaskResponse> tasks =
                    day is not null && tasksByDayId.TryGetValue(day.Id, out var list)
                        ? list
                        : Array.Empty<ScheduledTaskResponse>();

                if (date > horizon && recurringTasks.Count > 0)
                {
                    var scheduledIds = tasks.Select(t => t.TaskId).ToHashSet();
                    var withVirtual = tasks.ToList();
                    foreach (var task in recurringTasks)
                    {
                        if (scheduledIds.Contains(task.Id) || !RecurrenceDays.Includes(task.RecurrenceDays, date.DayOfWeek))
                        {
                            continue;
                        }

                        withVirtual.Add(new ScheduledTaskResponse
                        {
                            TaskId = task.Id,
                            Title = task.Title,
                            Status = task.Status,
                            Priority = task.Priority,
                            Type = task.Type,
                            GoalId = task.GoalId,
                            Deadline = task.Deadline,
                            CompletedPercent = task.CompletedPercent,
                            OrderIndex = withVirtual.Count,
                            IsRecurring = true,
                            IsVirtual = true
                        });
                    }
                    tasks = withVirtual;
                }

                IReadOnlyList<NotificationResponse> dayNotifications =
                    day is not null && notificationsByDayId.TryGetValue(day.Id, out var notifList)
                        ? notifList
                        : Array.Empty<NotificationResponse>();

                result.Add(new DayScheduleResponse
                {
                    Date = date,
                    SpecialNote = day?.SpecialNote,
                    Tasks = tasks,
                    Notifications = dayNotifications
                });
            }

            return result;
        }
    }
}
