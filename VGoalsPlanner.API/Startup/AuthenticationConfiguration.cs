using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using VGoalsPlanner.API.Auth;

namespace VGoalsPlanner.API.Startup
{
    public static class AuthenticationConfiguration
    {
        public static IServiceCollection AddKeycloakAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var options = AuthOptions.From(configuration);

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(jwt =>
                {
                    jwt.Authority = options.Authority;
                    jwt.RequireHttpsMetadata = options.RequireHttpsMetadata;
                    jwt.MapInboundClaims = false;

                    // Docker: discovery и ключи подписи берём через внутренний адрес Keycloak.
                    if (KeycloakBackchannelHandler.Create(options.Authority, options.InternalAuthority) is { } backchannel)
                    {
                        jwt.BackchannelHttpHandler = backchannel;
                    }

                    jwt.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidAudience = options.ClientId,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ClockSkew = TimeSpan.FromSeconds(options.ClockSkewSeconds),
                        NameClaimType = "preferred_username",
                        RoleClaimType = ClaimTypes.Role
                    };

                    jwt.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            if (context.Principal?.Identity is not ClaimsIdentity identity)
                            {
                                return;
                            }

                            MapClientRoles(identity, options.ClientId);

                            // Строку пользователя создаём только тем, у кого есть доступ к приложению.
                            if (identity.HasClaim(ClaimTypes.Role, options.RequiredRole)
                                && Guid.TryParse(identity.FindFirst("sub")?.Value, out var userId))
                            {
                                var userName = identity.FindFirst("preferred_username")?.Value ?? userId.ToString();
                                var provisioner = context.HttpContext.RequestServices.GetRequiredService<UserProvisioner>();
                                await provisioner.EnsureAsync(userId, userName, context.HttpContext.RequestAborted);
                            }
                        }
                    };
                });

            return services;
        }

        // Роли клиента лежат во вложенном claim resource_access.<clientId>.roles — ASP.NET сам их не читает.
        // Берём роли только своего клиента: роли чужого приложения доступа сюда не дают.
        private static void MapClientRoles(ClaimsIdentity identity, string clientId)
        {
            var raw = identity.FindFirst("resource_access")?.Value;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return;
            }

            using var document = JsonDocument.Parse(raw);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty(clientId, out var client)
                || !client.TryGetProperty("roles", out var roles)
                || roles.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (var role in roles.EnumerateArray())
            {
                var value = role.GetString();
                if (!string.IsNullOrWhiteSpace(value) && !identity.HasClaim(ClaimTypes.Role, value))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, value));
                }
            }
        }
    }
}
