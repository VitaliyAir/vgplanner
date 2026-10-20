using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VGoalsPlanner.Domain.Entities;
using VGoalsPlanner.Domain.Seed;

namespace VGoalsPlanner.Infrastructure.Persistence.Database.Configurations
{
    internal class VUserConfiguration : IEntityTypeConfiguration<VUser>
    {
        public void Configure(EntityTypeBuilder<VUser> builder)
        {
            // Таблица
            builder.ToTable("VUsers");

            // Первичный ключ
            builder.HasKey(e => e.UserId);

            // Тестовый пользователь, пока нет аутентификации (см. CurrentUserService в API)
            builder.HasData(new VUser
            {
                UserId = TestUser.Id,
                UserName = TestUser.Name
            });
        }
    }
}
