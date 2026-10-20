namespace VGoalsPlanner.Application.Abstractions.Services
{
    public interface ICurrentUserService
    {
        Guid UserId { get; }
        string UserName { get; }

        /// <summary>Bearer-токен текущего запроса (нужен для обмена на токен VStorage); null вне HTTP-запроса.</summary>
        string? AccessToken { get; }
    }
}
