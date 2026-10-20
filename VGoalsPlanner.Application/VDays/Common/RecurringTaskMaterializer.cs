using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Domain.Entities;
using VGoalsPlanner.Domain.Enums;

namespace VGoalsPlanner.Application.VDays.Common
{
    /// <summary>
    /// Превращает правила регулярных задач (дни недели) в обычные записи расписания (VDay + VDayVTask)
    /// для сегодняшнего и прошедших дней, чтобы порядок, "активность", логи и удаление работали
    /// как у любой запланированной задачи. Для будущих дней записи не создаются — их показывает
    /// DayScheduleAssembler виртуально.
    /// </summary>
    internal static class RecurringTaskMaterializer
    {
        // Запас в сутки вперёд: у пользователей "впереди" UTC (например, UTC+3) локальная дата
        // уже может быть следующей относительно UTC-даты.
        public static DateOnly Horizon => DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

        // Не создаём больше ~13 месяцев пропущенных дней за один заход.
        private const int MaxBackfillDays = 400;

        // Параллельные запросы расписания одного пользователя не должны вставлять одно и то же дважды.
        private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> UserLocks = new();

        public static bool IsActiveStatus(VTaskStatus status)
            => status == VTaskStatus.New || status == VTaskStatus.InProgress;

        public static async Task EnsureUpToDateAsync(
            IApplicationDbContext context,
            Guid userId,
            CancellationToken cancellationToken)
        {
            var gate = UserLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync(cancellationToken);
            try
            {
                await GenerateAsync(context, userId, cancellationToken);
            }
            finally
            {
                gate.Release();
            }
        }

        private static async Task GenerateAsync(IApplicationDbContext context, Guid userId, CancellationToken cancellationToken)
        {
            var horizon = Horizon;
            var earliest = horizon.AddDays(-MaxBackfillDays);

            var tasks = await context.Tasks
                .Where(t => t.UserId == userId
                    && t.Type == VTaskType.Repeatable
                    && t.RecurrenceDays != 0
                    && (t.RecurrenceGeneratedUntil == null || t.RecurrenceGeneratedUntil < horizon))
                .ToListAsync(cancellationToken);

            if (tasks.Count == 0)
            {
                return;
            }

            // Для каждой задачи — даты, на которые нужно создать запись.
            var wanted = new List<(Guid TaskId, DateOnly Date)>();
            foreach (var task in tasks)
            {
                var from = (task.RecurrenceGeneratedUntil ?? horizon.AddDays(-2)).AddDays(1);
                if (from < earliest)
                {
                    from = earliest;
                }

                if (IsActiveStatus(task.Status))
                {
                    for (var date = from; date <= horizon; date = date.AddDays(1))
                    {
                        if (RecurrenceDays.Includes(task.RecurrenceDays, date.DayOfWeek))
                        {
                            wanted.Add((task.Id, date));
                        }
                    }
                }

                // Отметка двигается всегда — и пока задача на паузе/завершена: пропущенное не "дозаливается",
                // а удалённое пользователем повторение не появляется снова.
                task.RecurrenceGeneratedUntil = horizon;
            }

            if (wanted.Count > 0)
            {
                var now = DateTime.UtcNow;
                var min = wanted.Min(w => w.Date);
                var max = wanted.Max(w => w.Date);

                var days = await context.Days
                    .Where(d => d.UserId == userId && d.Date >= min && d.Date <= max)
                    .ToDictionaryAsync(d => d.Date, cancellationToken);

                var existingDayIds = days.Values.Select(d => d.Id).ToList();
                var existingLinks = existingDayIds.Count == 0
                    ? new List<(Guid VDayId, Guid VTaskId)>()
                    : (await context.DayTasks.AsNoTracking()
                        .Where(dt => existingDayIds.Contains(dt.VDayId))
                        .Select(dt => new { dt.VDayId, dt.VTaskId })
                        .ToListAsync(cancellationToken))
                        .Select(l => (l.VDayId, l.VTaskId))
                        .ToList();

                var linkSet = existingLinks.ToHashSet();
                var nextOrder = existingLinks
                    .GroupBy(l => l.VDayId)
                    .ToDictionary(g => g.Key, g => g.Count());

                foreach (var (taskId, date) in wanted)
                {
                    if (!days.TryGetValue(date, out var day))
                    {
                        day = new VDay { Date = date, UserId = userId, CreatedAt = now, UpdatedAt = now };
                        context.Days.Add(day);
                        days[date] = day;
                    }

                    if (linkSet.Contains((day.Id, taskId)))
                    {
                        continue;
                    }

                    var order = nextOrder.GetValueOrDefault(day.Id);
                    nextOrder[day.Id] = order + 1;

                    context.DayTasks.Add(new VDayVTask { VDayId = day.Id, VTaskId = taskId, OrderIndex = order });
                    linkSet.Add((day.Id, taskId));
                }
            }

            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
