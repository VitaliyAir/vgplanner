using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Quartz;
using VGoalsPlanner.API.Startup;
using VGoalsPlanner.Application.Statistics;
using VGoalsPlanner.Domain.Entities;
using VGoalsPlanner.Infrastructure.Persistence.Database;

namespace VGoalsPlanner.API.Jobs
{
    /// <summary>
    /// Раз в сутки записывает каждому пользователю снимок статистики (VDayStatistics).
    /// Идемпотентно: для пользователей, у которых снимок за эту дату уже есть, ничего не добавляется.
    /// </summary>
    [DisallowConcurrentExecution]
    public sealed class DailyStatisticsSnapshotJob(
        IDbContextFactory<VGoalsPlannerDbContext> dbFactory,
        IOptions<DailySnapshotOptions> options,
        ILogger<DailyStatisticsSnapshotJob> logger) : IJob
    {
        public async ValueTask Execute(IJobExecutionContext context, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(now, options.Value.ResolveTimeZone()));

            await using var db = await dbFactory.CreateDbContextAsync(ct);

            var userIds = await db.Set<VUser>().AsNoTracking().Select(u => u.UserId).ToListAsync(ct);
            var done = await db.DayStatistics.AsNoTracking()
                .Where(s => s.Date == date)
                .Select(s => s.UserId)
                .ToListAsync(ct);

            var created = 0;
            foreach (var userId in userIds.Except(done))
            {
                var stats = await StatisticsReader.ReadAsync(db, userId, ct);
                db.DayStatistics.Add(stats.ToSnapshot(userId, date, now));
                created++;
            }

            // Обычный SaveChangesAsync контекста (без аудита): аудит берёт UserId из HTTP-запроса, в фоновом задании его нет.
            await db.SaveChangesAsync(ct);

            logger.LogInformation("Снимок статистики за {Date}: создано {Created}, уже было {Existing}", date, created, done.Count);
        }
    }
}
