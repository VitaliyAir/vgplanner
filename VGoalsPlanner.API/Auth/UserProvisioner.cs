using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Infrastructure.Persistence.Database;

namespace VGoalsPlanner.API.Auth
{
    /// <summary>
    /// Создаёт строку VUsers для пользователя Keycloak при первом запросе: все внешние ключи UserId
    /// ссылаются на VUsers, а `sub` из токена приходит из внешней системы.
    /// </summary>
    public sealed class UserProvisioner(IDbContextFactory<VGoalsPlannerDbContext> contextFactory)
    {
        private readonly ConcurrentDictionary<Guid, byte> _known = new();

        public async Task EnsureAsync(Guid userId, string userName, CancellationToken cancellationToken)
        {
            if (_known.ContainsKey(userId))
            {
                return;
            }

            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

            // Атомарно: при первой загрузке UI шлёт несколько запросов одновременно.
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""INSERT INTO "VUsers" ("UserId", "UserName") VALUES ({userId}, {userName}) ON CONFLICT ("UserId") DO NOTHING""",
                cancellationToken);

            _known[userId] = 0;
        }
    }
}
