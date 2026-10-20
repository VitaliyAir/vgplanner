using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Domain.Entities;

namespace VGoalsPlanner.Infrastructure.Persistence.Database.Configurations
{
    internal class VGoalConfiguration : IEntityTypeConfiguration<VGoal>
    {
        public void Configure(EntityTypeBuilder<VGoal> builder)
        {
            // Таблица
            builder.ToTable("VGoals");

            // Первичный ключ
            builder.HasKey(e => e.Id);

            // Поля
            builder.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(e => e.Description)
                .HasMaxLength(2000)
                .IsRequired(false);

            builder.Property(e => e.Priority)
                .IsRequired();

            builder.Property(e => e.Status)
                .IsRequired();

            builder.Property(e => e.OrderIndex)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(e => e.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(e => e.UserId)
                .IsRequired();

            // Индексы
            builder.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_VGoals_UserId");

            builder.HasIndex(e => e.ParentGoalId)
                .HasDatabaseName("IX_VGoals_ParentGoalId");

            builder.HasIndex(e => new { e.UserId, e.ParentGoalId })
                .HasDatabaseName("IX_VGoals_UserId_ParentGoalId");

            builder.HasIndex(e => e.Status)
                .HasDatabaseName("IX_VGoals_Status");

            // Self-reference (иерархия целей)
            builder.HasOne(e => e.ParentGoal)
                .WithMany(e => e.ChildGoals)
                .HasForeignKey(e => e.ParentGoalId)
                .OnDelete(DeleteBehavior.Cascade) // при удалении родительской цели удаляются дочерние
                .IsRequired(false); // ParentGoalId может быть null (корневая цель)

            // Связь с пользователем
            builder.HasOne(e => e.User)
                .WithMany(u => u.Goals)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Фильтр для мягкого удаления (чтобы не возвращать IsDeleted = true)
            builder.HasQueryFilter(e => !e.IsDeleted);
        }
    }
}
