using System.Security.Claims;
using VGoalsPlanner.Application.Abstractions.Services;

namespace VGoalsPlanner.API.Services
{
    /// <summary>Текущий пользователь из токена Keycloak: `sub` — идентификатор, preferred_username — имя.</summary>
    public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
    {
        private ClaimsPrincipal User =>
            httpContextAccessor.HttpContext?.User
            ?? throw new InvalidOperationException("Текущий пользователь недоступен вне HTTP-запроса.");

        public Guid UserId =>
            Guid.TryParse(User.FindFirstValue("sub"), out var id)
                ? id
                : throw new UnauthorizedAccessException("В токене нет корректного claim 'sub'.");

        public string UserName => User.FindFirstValue("preferred_username") ?? UserId.ToString();

        public string? AccessToken
        {
            get
            {
                var header = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
                const string prefix = "Bearer ";

                return header is not null && header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                    ? header[prefix.Length..].Trim()
                    : null;
            }
        }
    }
}
