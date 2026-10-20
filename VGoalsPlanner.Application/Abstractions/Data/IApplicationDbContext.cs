using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Domain.Entities;

namespace VGoalsPlanner.Application.Abstractions.Data
{
    /// <summary>
    /// Абстракция над VGoalsPlannerDbContext, чтобы Application не зависел от Infrastructure/EF-провайдера.
    /// </summary>
    public interface IApplicationDbContext
    {
        DbSet<VGoal> Goals { get; }
        DbSet<VTask> Tasks { get; }
        DbSet<VNotification> Notifications { get; }
        DbSet<VDay> Days { get; }
        DbSet<VDayVTask> DayTasks { get; }
        DbSet<VTaskLog> TaskLogs { get; }
        DbSet<VTaskAttachment> TaskAttachments { get; }
        DbSet<AuditLog> AuditLogs { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
