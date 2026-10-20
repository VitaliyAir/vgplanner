using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Domain.Entities;

namespace VGoalsPlanner.Infrastructure.Persistence.Database.Configurations
{
    internal class VNotificationConfiguration : IEntityTypeConfiguration<VNotification>
    {
        public void Configure(EntityTypeBuilder<VNotification> builder)
        {
            // Таблица
            builder.ToTable("VNotifications");

            // Первичный ключ
            builder.HasKey(e => e.Id);

            // Поля
            builder.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(e => e.Description)
                .HasMaxLength(2000)
                .IsRequired(false);

            builder.Property(e => e.Deadline)
                .IsRequired(false);

            builder.Property(e => e.IsRead)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(e => e.IsCompleted)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(e => e.DayId)
                .IsRequired();

            builder.Property(e => e.UserId)
                .IsRequired();

            // Индексы
            builder.HasIndex(e => e.DayId)
                .HasDatabaseName("IX_VNotifications_DayId");

            builder.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_VNotifications_UserId");

            builder.HasIndex(e => e.IsRead)
                .HasDatabaseName("IX_VNotifications_IsRead");

            // Связь с днём
            builder.HasOne(e => e.Day)
                .WithMany(d => d.Notifications)
                .HasForeignKey(e => e.DayId)
                .OnDelete(DeleteBehavior.Cascade);

            // Связь с пользователем
            builder.HasOne(e => e.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
