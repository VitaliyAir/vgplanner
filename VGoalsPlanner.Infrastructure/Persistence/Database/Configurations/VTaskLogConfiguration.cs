using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Domain.Entities;

namespace VGoalsPlanner.Infrastructure.Persistence.Database.Configurations
{
    internal class VTaskLogConfiguration : IEntityTypeConfiguration<VTaskLog>
    {
        public void Configure(EntityTypeBuilder<VTaskLog> builder)
        {
            // Таблица
            builder.ToTable("VTaskLogs");

            // Первичный ключ
            builder.HasKey(e => e.Id);

            // Поля
            builder.Property(e => e.Note)
                .HasMaxLength(20000)
                .IsRequired(false);

            builder.Property(e => e.Attachments)
                .HasColumnType("jsonb")
                .HasDefaultValueSql("'[]'::jsonb")
                .IsRequired(false);

            builder.Property(e => e.TimeSpentMinutes)
                .IsRequired(false);

            builder.Property(e => e.OldStatus)
                .IsRequired(false);

            builder.Property(e => e.NewStatus)
                .IsRequired(false);

            builder.Property(e => e.VTaskId)
                .IsRequired();

            builder.Property(e => e.CreatedByUserId)
                .IsRequired();

            builder.Property(e => e.DayId)
                .IsRequired();

            builder.Property(e => e.CreatedAt)
                .IsRequired();

            builder.Property(e => e.UpdatedAt)
                .IsRequired();

            // Индексы
            builder.HasIndex(e => e.VTaskId)
                .HasDatabaseName("IX_VTaskLogs_VTaskId");

            builder.HasIndex(e => e.DayId)
                .HasDatabaseName("IX_VTaskLogs_DayId");

            builder.HasIndex(e => e.CreatedByUserId)
                .HasDatabaseName("IX_VTaskLogs_CreatedByUserId");

            // Связь с задачей
            builder.HasOne(e => e.VTask)
                .WithMany(v => v.TaskLogs)
                .HasForeignKey(e => e.VTaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // Связь с пользователем (кто создал лог)
            builder.HasOne(e => e.CreatedByUser)
                .WithMany(u => u.TaskLogs)
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Связь с днём
            builder.HasOne(e => e.Day)
                .WithMany(d => d.TaskLogs)
                .HasForeignKey(e => e.DayId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
