using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Quartz;
using VGoalsPlanner.API.Startup;
using VGoalsPlanner.Infrastructure.Persistence.Database;

namespace VGoalsPlanner.API.Jobs
{
    /// <summary>
    /// Циклическая запись журнала аудита: раз в месяц удаляет записи старше
    /// <see cref="AuditRecycleOptions.MaxDaysAuditRecordLifecycle"/> дней.
    /// Удаление идёт пакетами и через ExecuteDeleteAsync (в обход SaveChanges), поэтому сама чистка
    /// не порождает новых записей аудита и не требует HTTP-пользователя.
    /// </summary>
    [DisallowConcurrentExecution]
    public sealed class AuditLogRecycleJob(
        IDbContextFactory<VGoalsPlannerDbContext> dbFactory,
        IOptions<AuditRecycleOptions> options,
        ILogger<AuditLogRecycleJob> logger) : IJob
    {
        // Пакет ограничивает длину одной транзакции: за год записей может накопиться много.
        private const int BatchSize = 10_000;

        public async ValueTask Execute(IJobExecutionContext context, CancellationToken ct)
        {
            var settings = options.Value;

            if (!settings.RecycleAudit)
            {
                logger.LogInformation("Ротация аудита выключена (AuditRecycle:RecycleAudit = false)");
                return;
            }

            if (settings.MaxDaysAuditRecordLifecycle <= 0)
            {
                // Защита от опечатки в конфиге: ноль означал бы «удалить всё».
                logger.LogWarning(
                    "Ротация аудита пропущена: MaxDaysAuditRecordLifecycle = {Days}, ожидается положительное число дней",
                    settings.MaxDaysAuditRecordLifecycle);
                return;
            }

            var cutoff = DateTime.UtcNow.AddDays(-settings.MaxDaysAuditRecordLifecycle);
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            var deleted = 0;

            while (true)
            {
                var ids = await db.AuditLogs
                    .AsNoTracking()
                    .Where(a => a.Timestamp < cutoff)
                    .OrderBy(a => a.Timestamp)
                    .Select(a => a.Id)
                    .Take(BatchSize)
                    .ToListAsync(ct);

                if (ids.Count == 0)
                {
                    break;
                }

                var removed = await db.AuditLogs
                    .Where(a => ids.Contains(a.Id))
                    .ExecuteDeleteAsync(ct);

                deleted += removed;

                // Параллельная чистка/удаление могли убрать строки между выборкой и удалением —
                // тогда не крутимся в бесконечном цикле.
                if (removed == 0)
                {
                    break;
                }
            }

            if (deleted == 0)
            {
                logger.LogInformation("Ротация аудита: записей старше {Cutoff:u} нет", cutoff);
                return;
            }

            logger.LogInformation(
                "Ротация аудита: удалено {Deleted} записей старше {Cutoff:u} (хранение {Days} дн.)",
                deleted,
                cutoff,
                settings.MaxDaysAuditRecordLifecycle);
        }
    }
}
