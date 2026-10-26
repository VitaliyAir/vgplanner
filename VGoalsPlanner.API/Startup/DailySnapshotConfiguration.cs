using Quartz;
using VGoalsPlanner.API.Jobs;

namespace VGoalsPlanner.API.Startup
{
    public static class DailySnapshotConfiguration
    {
        /// <summary>Ежедневное задание Quartz (в памяти, без таблиц), которое пишет снимок статистики каждого пользователя.</summary>
        public static IServiceCollection AddDailyStatisticsSnapshot(this IServiceCollection services, IConfiguration configuration)
        {
            var section = configuration.GetSection(DailySnapshotOptions.SectionName);
            services.Configure<DailySnapshotOptions>(section);

            var options = section.Get<DailySnapshotOptions>() ?? new DailySnapshotOptions();
            if (!options.Enabled)
            {
                return services;
            }

            services.AddQuartz(q =>
            {
                var key = new JobKey("daily-statistics-snapshot");

                q.AddJob<DailyStatisticsSnapshotJob>(j => j.WithIdentity(key));
                q.AddTrigger(t => t
                    .ForJob(key)
                    .WithIdentity("daily-statistics-snapshot-trigger")
                    .WithCronSchedule(options.Cron, c => c.InTimeZone(options.ResolveTimeZone())));
            });

            services.AddQuartzHostedService(o => o.WaitForJobsToComplete = true);

            return services;
        }
    }
}
