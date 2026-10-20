namespace VGoalsPlanner.Domain.Seed
{
    /// <summary>
    /// Устаревший тестовый пользователь из времён, когда не было аутентификации (теперь UserId берётся из токена Keycloak).
    /// Строка VUser остаётся в seed-данных: её удаление миграцией каскадно удалило бы все данные этого пользователя.
    /// Перенести его данные на реального пользователя можно скриптом scripts/claim-legacy-data.sql.
    /// </summary>
    public static class TestUser
    {
        public static readonly Guid Id = Guid.Parse("11111111-1111-1111-1111-111111111111");

        public const string Name = "test-user";
    }
}
