using Microsoft.Extensions.Options;
using VGoalsPlanner.API.Auth;
using VGoalsPlanner.API.Options;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Infrastructure.Storage;

namespace VGoalsPlanner.API.Startup
{
    public static class VStorageConfiguration
    {
        /// <summary>
        /// Вложения задач в VStorageAPI v2. Если раздел "VStorage" не заполнен (BaseUrl, секрет клиента),
        /// приложение стартует как обычно, а операции с вложениями отвечают "хранилище недоступно или не настроено".
        /// </summary>
        public static IServiceCollection AddVStorage(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMemoryCache();
            services.Configure<VStorageOptions>(configuration.GetSection(VStorageOptions.SectionName));
            services.PostConfigure<VStorageOptions>(options =>
            {
                var authority = configuration["Authentication:Authority"];
                if (string.IsNullOrWhiteSpace(options.TokenEndpoint) && !string.IsNullOrWhiteSpace(authority))
                {
                    options.TokenEndpoint = $"{authority.TrimEnd('/')}/protocol/openid-connect/token";
                }
            });
            services.Configure<AttachmentOptions>(configuration.GetSection(AttachmentOptions.SectionName));

            // Docker: обмен токена идёт через внутренний адрес Keycloak (Authentication:InternalAuthority).
            services.AddHttpClient<VStorageTokenProvider>(client => client.Timeout = TimeSpan.FromSeconds(30))
                .ConfigurePrimaryHttpMessageHandler(() => KeycloakBackchannelHandler.Create(
                    configuration["Authentication:Authority"], configuration["Authentication:InternalAuthority"]) ?? new SocketsHttpHandler());

            services.AddHttpClient<IFileStorage, VStorageFileStorage>((serviceProvider, client) =>
            {
                var settings = serviceProvider.GetRequiredService<IOptions<VStorageOptions>>().Value;

                if (Uri.TryCreate(settings.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var baseAddress))
                {
                    client.BaseAddress = baseAddress;
                }

                client.Timeout = settings.Timeout;
            });

            return services;
        }
    }
}
