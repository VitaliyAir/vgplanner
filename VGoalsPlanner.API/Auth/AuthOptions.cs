namespace VGoalsPlanner.API.Auth
{
    public sealed class AuthOptions
    {
        public const string SectionName = "Authentication";

        /// <summary>Адрес realm Keycloak, например http://localhost:8083/realms/VEco (издатель токенов).</summary>
        public string Authority { get; set; } = string.Empty;

        /// <summary>Идентификатор клиента Keycloak этого приложения: он же обязательная audience токена и источник ролей.</summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>Роль клиента, дающая доступ к API.</summary>
        public string RequiredRole { get; set; } = AppRoles.Owner;

        /// <summary>Выключается только для локального http-Keycloak в Development.</summary>
        public bool RequireHttpsMetadata { get; set; } = true;

        /// <summary>
        /// Только Docker: адрес, по которому этот API достаёт Keycloak, если он отличается от <see cref="Authority"/>
        /// в браузере, например http://host.docker.internal:8083. Пусто — ходить по Authority (dev, тесты).
        /// </summary>
        public string? InternalAuthority { get; set; }

        public int ClockSkewSeconds { get; set; } = 30;

        public static AuthOptions From(IConfiguration configuration)
        {
            var options = configuration.GetSection(SectionName).Get<AuthOptions>() ?? new AuthOptions();

            if (string.IsNullOrWhiteSpace(options.Authority) || string.IsNullOrWhiteSpace(options.ClientId))
            {
                throw new InvalidOperationException(
                    $"{SectionName}:Authority and {SectionName}:ClientId must be configured.");
            }

            return options;
        }
    }
}
