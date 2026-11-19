using Quartz;
using VGoalsPlanner.API.Jobs;

namespace VGoalsPlanner.API.Startup
{
    /// <summary>
    /// Фоновые задания Quartz (in-memory store, без собственных таблиц, бесплатный):
    /// ежедневный снимок статистики и ротация журнала аудита.
    /// Планировщик стартует один раз и только если включено хотя бы одно задание —
    /// поэтому hosted service регистрируется здесь, а не в каждом конфигураторе.
    /// </summary>
    public static class ScheduledJobsConfiguration
    {
        private const string DailySnapshotJobKey = "daily-statistics-snapshot";
        private const string AuditRecycleJobKey = "audit-log-recycle";

        public static IServiceCollection AddScheduledJobs(this IServiceCollection services, IConfiguration configuration)
        {
            var snapshotSection = configuration.GetSection(DailySnapshotOptions.SectionName);
            var auditSection = configuration.GetSection(AuditRecycleOptions.SectionName);

            services.Configure<DailySnapshotOptions>(snapshotSection);
            services.Configure<AuditRecycleOptions>(auditSection);

            var snapshot = snapshotSection.Get<DailySnapshotOptions>() ?? new DailySnapshotOptions();
            var audit = auditSection.Get<AuditRecycleOptions>() ?? new AuditRecycleOptions();

            if (!snapshot.Enabled && !audit.RecycleAudit)
            {
                return services;
            }

            services.AddQuartz(q =>
            {
                if (snapshot.Enabled)
                {
                    var key = new JobKey(DailySnapshotJobKey);

                    q.AddJob<DailyStatisticsSnapshotJob>(j => j.WithIdentity(key));
                    q.AddTrigger(t => t
                        .ForJob(key)
                        .WithIdentity($"{DailySnapshotJobKey}-trigger")
                        .WithCronSchedule(snapshot.Cron, c => c.InTimeZone(snapshot.ResolveTimeZone())));
                }

                if (audit.RecycleAudit)
                {
                    var key = new JobKey(AuditRecycleJobKey);

                    q.AddJob<AuditLogRecycleJob>(j => j.WithIdentity(key));
                    q.AddTrigger(t => t
                        .ForJob(key)
                        .WithIdentity($"{AuditRecycleJobKey}-trigger")
                        .WithCronSchedule(audit.Cron, c => c.InTimeZone(audit.ResolveTimeZone())));
                }
            });

            services.AddQuartzHostedService(o => o.WaitForJobsToComplete = true);

            return services;
        }
    }
}
