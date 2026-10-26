using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Domain.Entities;
using VGoalsPlanner.Infrastructure.Persistence.Database.Configurations;

namespace VGoalsPlanner.Infrastructure.Persistence.Database
{
    public class VGoalsPlannerDbContext : DbContext, IApplicationDbContext
    {
        public DbSet<AuditLog> AuditLogs { get; set; } = default!;
        public DbSet<VDay> Days { get; set; }
        public DbSet<VGoal> Goals { get; set; }
        public DbSet<VTask> Tasks { get; set; }
        public DbSet<VNotification> Notifications { get; set; }
        public DbSet<VDayVTask> DayTasks { get; set; }
        public DbSet<VTaskLog> TaskLogs { get; set; }
        public DbSet<VTaskAttachment> TaskAttachments { get; set; }
        public DbSet<VDayStatistics> DayStatistics { get; set; }


        private readonly ICurrentUserService _currentUser;


        public VGoalsPlannerDbContext(
            DbContextOptions<VGoalsPlannerDbContext> options,
            ICurrentUserService currentUser)
        : base(options) 
        {
            _currentUser = currentUser;
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new AuditLogConfiguration());
            modelBuilder.ApplyConfiguration(new VDayConfiguration());
            modelBuilder.ApplyConfiguration(new VDayVTaskConfiguration());
            modelBuilder.ApplyConfiguration(new VGoalConfiguration());
            modelBuilder.ApplyConfiguration(new VTaskConfiguration());
            modelBuilder.ApplyConfiguration(new VNotificationConfiguration());
            modelBuilder.ApplyConfiguration(new VTaskLogConfiguration());
            modelBuilder.ApplyConfiguration(new VTaskAttachmentConfiguration());
            modelBuilder.ApplyConfiguration(new VDayStatisticsConfiguration());
            modelBuilder.ApplyConfiguration(new VUserConfiguration());
        }

        /// <summary>
        /// Явная реализация IApplicationDbContext.SaveChangesAsync — делегирует в SaveChangesWithAuditAsync,
        /// чтобы вызовы через абстракцию (из Application) тоже писали AuditLog, а не тихо использовали
        /// базовый DbContext.SaveChangesAsync (та же сигнатура иначе перекрыла бы её неявно).
        /// </summary>
        Task<int> IApplicationDbContext.SaveChangesAsync(CancellationToken cancellationToken)
            => SaveChangesWithAuditAsync(cancellationToken);

        /// <summary>
        /// Сохраняет изменения с записью аудита.
        /// </summary>
        public async Task<int> SaveChangesWithAuditAsync(
            CancellationToken cancellationToken = default)
        {
            var addedEntries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added)
                .ToList();

            var modifiedEntries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Modified)
                .ToList();

            var deletedEntries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Deleted)
                .ToList();

            // Can be added Switcher to turn On/Off Audit
            if (addedEntries.Any() || modifiedEntries.Any() || deletedEntries.Any())
            {
                var auditLogs = new List<AuditLog>();
                auditLogs.AddRange(BuildAuditLogs(EntityState.Added, addedEntries));
                auditLogs.AddRange(BuildAuditLogs(EntityState.Modified, modifiedEntries));
                auditLogs.AddRange(BuildAuditLogs(EntityState.Deleted, deletedEntries));

                if (auditLogs.Any())
                {
                    AuditLogs.AddRange(auditLogs);
                }
            }

            var baseSaveChanges = await base.SaveChangesAsync(cancellationToken);

            return baseSaveChanges;
        }

        /// <summary>
        /// Строит список логов для записей с определённым состоянием.
        /// </summary>
        private List<AuditLog> BuildAuditLogs(
            EntityState state,
            List<EntityEntry> auditEntries)
        {
            var logs = new List<AuditLog>();
            foreach (var entry in auditEntries)
            {
                // Пропускаем аудит для системных сущностей (например, сам AuditLog)
                if (entry.Entity is AuditLog) continue;

                var log = new AuditLog
                {
                    EntityType = entry.Entity.GetType().Name,
                    EntityId = GetEntityId(entry),
                    Action = state.ToString(), // или преобразовать в AuditAction
                    UserId = _currentUser.UserId,
                    Timestamp = DateTime.UtcNow,
                    ChangesJson = GetChangesJson(entry, state)
                };

                logs.Add(log);
            }
            return logs;
        }

        /// <summary>
        /// Извлекает первичный ключ сущности в виде строки.
        /// </summary>
        private static string GetEntityId(EntityEntry entry)
        {
            var key = entry.Metadata.FindPrimaryKey();
            if (key == null) return string.Empty;

            // Берём значения ключевых свойств из OriginalValues (для Modified/Deleted) или CurrentValues (для Added)
            var values = entry.State == EntityState.Added
                ? entry.CurrentValues
                : entry.OriginalValues;

            var keyParts = key.Properties
                .Select(p => values[p]?.ToString() ?? string.Empty)
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            return keyParts.Count == 1 ? keyParts[0] : string.Join("-", keyParts);
        }

        /// <summary>
        /// Возвращает JSON-строку с изменениями (для Added – полная сериализация, для Modified – только изменённые поля).
        /// </summary>
        private static string GetChangesJson(EntityEntry entry, EntityState state)
        {
            if (state == EntityState.Added)
            {
                var props = entry.CurrentValues.Properties
                    .Where(p => !p.IsPrimaryKey() && !p.IsForeignKey()) // исключаем ключи и внешние ключи (они уже есть в EntityId)
                    .ToDictionary(p => p.Name, p => entry.CurrentValues[p]);
                return JsonSerializer.Serialize(props);
            }

            if (state == EntityState.Deleted)
            {
                // Для удаления логируем только ключ и факт удаления (можно добавить дату)
                return JsonSerializer.Serialize(new { DeletedAt = DateTime.UtcNow });
            }

            // Для Modified – только изменённые свойства
            var changes = new Dictionary<string, object>();
            foreach (var property in entry.OriginalValues.Properties)
            {
                var original = entry.OriginalValues[property];
                var current = entry.CurrentValues[property];
                if (!Equals(original, current))
                {
                    changes[property.Name] = new { Old = original, New = current };
                }
            }
            return changes.Any() ? JsonSerializer.Serialize(changes) : "{}";
        }
    }
}
