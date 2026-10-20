using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Domain.Entities;

namespace VGoalsPlanner.Infrastructure.Persistence.Database.Configurations
{
    internal class VTaskConfiguration : IEntityTypeConfiguration<VTask>
    {
        public void Configure(EntityTypeBuilder<VTask> builder)
        {
            // Таблица
            builder.ToTable("VTasks");

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

            builder.Property(e => e.Priority)
                .IsRequired();

            builder.Property(e => e.Status)
                .IsRequired();

            builder.Property(e => e.Tags)
                .HasColumnType("jsonb") // для PostgreSQL
                .HasDefaultValueSql("'[]'::jsonb") // пустой массив по умолчанию
                .IsRequired(false);

            // Ссылки хранятся одним JSON-столбцом (jsonb) внутри строки задачи
            builder.OwnsMany(e => e.Links, link => link.ToJson());

            builder.Property(e => e.Type)
                .IsRequired();

            builder.Property(e => e.RecurrenceDays)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(e => e.RecurrenceGeneratedUntil)
                .IsRequired(false);

            builder.Property(e => e.OrderIndex)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(e => e.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(e => e.UserId)
                .IsRequired();

            // Индексы (для быстрых запросов)
            builder.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_VTasks_UserId");

            builder.HasIndex(e => e.GoalId)
                .HasDatabaseName("IX_VTasks_GoalId");

            builder.HasIndex(e => new { e.UserId, e.GoalId })
                .HasDatabaseName("IX_VTasks_UserId_GoalId");

            builder.HasIndex(e => e.Status)
                .HasDatabaseName("IX_VTasks_Status");

            builder.HasIndex(e => e.Deadline)
                .HasDatabaseName("IX_VTasks_Deadline");

            // Связь с целью (каждая задача обязательно принадлежит цели)
            builder.HasOne(e => e.Goal)
                .WithMany(g => g.ChildTasks)
                .HasForeignKey(e => e.GoalId)
                .OnDelete(DeleteBehavior.Cascade) // при удалении цели удаляются её задачи
                .IsRequired();

            // Связь с пользователем
            builder.HasOne(e => e.User)
                .WithMany(u => u.VTasks)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Фильтр для мягкого удаления (чтобы не возвращать IsDeleted = true)
            builder.HasQueryFilter(e => !e.IsDeleted);
        }
    }
}
