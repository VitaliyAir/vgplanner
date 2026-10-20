using Microsoft.AspNetCore.Authorization;
using VGoalsPlanner.API.Auth;

namespace VGoalsPlanner.API.Startup
{
    public static class AuthorizationConfiguration
    {
        /// <summary>
        /// Безопасно по умолчанию: любой эндпоинт без явных атрибутов авторизации требует вошедшего
        /// пользователя с ролью клиента (Owner). Открытым эндпоинтам нужен явный [AllowAnonymous].
        /// </summary>
        public static IServiceCollection AddKeycloakAuthorization(this IServiceCollection services, IConfiguration configuration)
        {
            var options = AuthOptions.From(configuration);

            services.AddAuthorizationBuilder()
                .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .RequireRole(options.RequiredRole)
                    .Build());

            return services;
        }
    }
}
