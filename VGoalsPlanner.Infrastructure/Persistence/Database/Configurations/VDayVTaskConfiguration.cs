using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Domain.Entities;

namespace VGoalsPlanner.Infrastructure.Persistence.Database.Configurations
{
    internal class VDayVTaskConfiguration : IEntityTypeConfiguration<VDayVTask>
    {
        public void Configure(EntityTypeBuilder<VDayVTask> builder)
        {
            // Таблица
            builder.ToTable("VDayVTasks");

            // Составной первичный ключ
            builder.HasKey(e => new { e.VDayId, e.VTaskId });

            // Поля
            builder.Property(e => e.OrderIndex)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(false);

            // Связь с VDay
            builder.HasOne(e => e.Day)
                .WithMany(d => d.DayTasks)
                .HasForeignKey(e => e.VDayId)
                .OnDelete(DeleteBehavior.Cascade);

            // Связь с VTask
            builder.HasOne(e => e.Task)
                .WithMany(v => v.DayTasks)
                .HasForeignKey(e => e.VTaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // Индексы
            builder.HasIndex(e => e.VDayId)
                .HasDatabaseName("IX_VDayVTasks_VDayId");

            builder.HasIndex(e => e.VTaskId)
                .HasDatabaseName("IX_VDayVTasks_VTaskId");
        }
    }
}
