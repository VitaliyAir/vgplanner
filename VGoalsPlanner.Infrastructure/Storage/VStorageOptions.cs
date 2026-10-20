namespace VGoalsPlanner.Infrastructure.Storage
{
    /// <summary>
    /// Настройки доступа к VStorageAPI v2. ClientSecret — секрет (user-secrets / переменные окружения),
    /// в appsettings его класть нельзя.
    /// </summary>
    public sealed class VStorageOptions
    {
        public const string SectionName = "VStorage";

        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>Имя bucket'а пользователя; создаётся при первой загрузке.</summary>
        public string BucketName { get; set; } = "vgplanner-attachments";

        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Token endpoint Keycloak для обмена токена (Standard Token Exchange, RFC 8693).
        /// Если пусто — строится из Authentication:Authority.
        /// </summary>
        public string TokenEndpoint { get; set; } = string.Empty;

        /// <summary>Confidential-клиент этого API в Keycloak.</summary>
        public string ClientId { get; set; } = "vgplanner-api";

        /// <summary>Секрет клиента — только user-secrets / переменные окружения.</summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>Какой aud должен получить обменянный токен.</summary>
        public string Audience { get; set; } = "vstorage";

        public string Scope { get; set; } = "vstorage-audience";

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(BaseUrl)
            && !string.IsNullOrWhiteSpace(BucketName)
            && !string.IsNullOrWhiteSpace(TokenEndpoint)
            && !string.IsNullOrWhiteSpace(ClientId)
            && !string.IsNullOrWhiteSpace(ClientSecret);
    }
}
