using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Domain.Entities;

namespace VGoalsPlanner.Infrastructure.Persistence.Database.Configurations
{
    internal class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            // Таблица
            builder.ToTable("AuditLogs");

            // Первичный ключ
            builder.HasKey(e => e.Id);

            // Поля
            builder.Property(e => e.EntityType)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(e => e.EntityId)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(e => e.Action)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(e => e.ChangesJson)
                .HasColumnType("jsonb")
                .IsRequired(false);

            builder.Property(e => e.UserId)
                .IsRequired();

            builder.Property(e => e.Timestamp)
                .IsRequired();

            // Индексы
            builder.HasIndex(e => e.EntityType)
                .HasDatabaseName("IX_AuditLogs_EntityType");

            builder.HasIndex(e => e.EntityId)
                .HasDatabaseName("IX_AuditLogs_EntityId");

            builder.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_AuditLogs_UserId");

            builder.HasIndex(e => e.Timestamp)
                .HasDatabaseName("IX_AuditLogs_Timestamp");

            builder.HasIndex(e => new { e.EntityType, e.EntityId })
                .HasDatabaseName("IX_AuditLogs_EntityType_EntityId");

            // Связь с пользователем
            builder.HasOne(e => e.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Не удаляем аудит при удалении пользователя
        }
    }
}
