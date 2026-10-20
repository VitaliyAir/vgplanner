using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VGoalsPlanner.Domain.Entities;

namespace VGoalsPlanner.Infrastructure.Persistence.Database.Configurations
{
    internal class VDayConfiguration : IEntityTypeConfiguration<VDay>
    {
        public void Configure(EntityTypeBuilder<VDay> builder)
        {
            // Таблица
            builder.ToTable("VDays");

            // Первичный ключ
            builder.HasKey(e => e.Id);

            // Поля
            builder.Property(e => e.Date)
                .IsRequired();

            builder.Property(e => e.SpecialNote)
                .HasMaxLength(500)
                .IsRequired(false);

            builder.Property(e => e.UserId)
                .IsRequired();

            // Индексы
            builder.HasIndex(e => new { e.UserId, e.Date })
                .IsUnique() // У одного пользователя не может быть двух дней с одинаковой датой
                .HasDatabaseName("IX_VDays_UserId_Date");

            // Связь с пользователем (пока без внешнего ключа, т.к. VUser — не Identity)
            builder.HasOne(e => e.User)
                .WithMany(u => u.Days)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Навигационные коллекции настраиваются автоматически, 
            // но можно добавить каскадное удаление для VDayVTask и TaskLog
            // Они настраиваются в своих конфигурациях
        }
    }
}
