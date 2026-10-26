using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Domain.Entities;
using VGoalsPlanner.Domain.Enums;

namespace VGoalsPlanner.Application.Statistics
{
    /// <summary>Единое место подсчёта статистики: используется и запросом GET /api/statistics, и ежедневным снимком.</summary>
    public static class StatisticsReader
    {
        /// <summary>Текущая статистика пользователя: целей всего, задач всего и по каждому статусу.</summary>
        public static async Task<StatisticsResponse> ReadAsync(
            IApplicationDbContext context,
            Guid userId,
            CancellationToken cancellationToken)
        {
            var goalsTotal = await context.Goals.AsNoTracking()
                .CountAsync(g => g.UserId == userId, cancellationToken);

            var taskCounts = await context.Tasks.AsNoTracking()
                .Where(t => t.UserId == userId)
                .GroupBy(t => t.Status)
                .Select(t => new { Status = t.Key, Count = t.Count() })
                .ToListAsync(cancellationToken);

            return Build(goalsTotal, taskCounts.ToDictionary(x => x.Status, x => x.Count));
        }

        public static StatisticsResponse FromSnapshot(VDayStatistics snapshot) =>
            Build(snapshot.GoalsTotal, new Dictionary<VTaskStatus, int>
            {
                [VTaskStatus.New] = snapshot.TasksNew,
                [VTaskStatus.InProgress] = snapshot.TasksInProgress,
                [VTaskStatus.Completed] = snapshot.TasksCompleted,
                [VTaskStatus.OnHold] = snapshot.TasksOnHold,
                [VTaskStatus.Cancelled] = snapshot.TasksCancelled
            });

        public static VDayStatistics ToSnapshot(this StatisticsResponse stats, Guid userId, DateOnly date, DateTime now)
        {
            int Count(VTaskStatus status) => stats.Tasks.ByStatus.FirstOrDefault(c => c.Status == (int)status)?.Count ?? 0;

            return new VDayStatistics
            {
                UserId = userId,
                Date = date,
                GoalsTotal = stats.Goals.Total,
                TasksNew = Count(VTaskStatus.New),
                TasksInProgress = Count(VTaskStatus.InProgress),
                TasksCompleted = Count(VTaskStatus.Completed),
                TasksOnHold = Count(VTaskStatus.OnHold),
                TasksCancelled = Count(VTaskStatus.Cancelled),
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = userId
            };
        }

        // Все статусы по порядку enum (с нулём, если записей нет) + общий итог.
        private static StatisticsResponse Build(int goalsTotal, Dictionary<VTaskStatus, int> counts)
        {
            var byStatus = Enum.GetValues<VTaskStatus>()
                .Select(s => new StatusCount((int)s, counts.GetValueOrDefault(s)))
                .ToList();

            return new StatisticsResponse
            {
                Goals = new GoalStatistics { Total = goalsTotal },
                Tasks = new StatusStatistics { Total = byStatus.Sum(c => c.Count), ByStatus = byStatus }
            };
        }
    }
}
