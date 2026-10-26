using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VGoalsPlanner.Domain.Entities;

namespace VGoalsPlanner.Infrastructure.Persistence.Database.Configurations
{
    internal class VDayStatisticsConfiguration : IEntityTypeConfiguration<VDayStatistics>
    {
        public void Configure(EntityTypeBuilder<VDayStatistics> builder)
        {
            // Таблица
            builder.ToTable("VDayStatistics");

            // Первичный ключ
            builder.HasKey(e => e.Id);

            // Поля
            builder.Property(e => e.UserId).IsRequired();
            builder.Property(e => e.Date).IsRequired();
            builder.Property(e => e.GoalsTotal).IsRequired();
            builder.Property(e => e.TasksNew).IsRequired();
            builder.Property(e => e.TasksInProgress).IsRequired();
            builder.Property(e => e.TasksCompleted).IsRequired();
            builder.Property(e => e.TasksOnHold).IsRequired();
            builder.Property(e => e.TasksCancelled).IsRequired();
            builder.Property(e => e.CreatedAt).IsRequired();
            builder.Property(e => e.UpdatedAt).IsRequired();

            // Один снимок на пользователя и дату (защита от повторного запуска задания)
            builder.HasIndex(e => new { e.UserId, e.Date })
                .IsUnique()
                .HasDatabaseName("IX_VDayStatistics_UserId_Date");

            // Связь с пользователем
            builder.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
