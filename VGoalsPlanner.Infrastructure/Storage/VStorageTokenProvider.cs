using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Errors;

namespace VGoalsPlanner.Infrastructure.Storage
{
    /// <summary>Обменивает токен текущего пользователя на токен для VStorage (aud = vstorage), кэш на пользователя до exp - 30 с.</summary>
    public sealed class VStorageTokenProvider(
        HttpClient http,
        IMemoryCache cache,
        IOptions<VStorageOptions> options,
        ICurrentUserService currentUser,
        ILogger<VStorageTokenProvider> logger)
    {
        private const int SafetySeconds = 30;

        public async Task<Result<string>> GetTokenAsync(CancellationToken cancellationToken)
        {
            var settings = options.Value;
            var subjectToken = currentUser.AccessToken;

            if (!settings.IsConfigured || string.IsNullOrEmpty(subjectToken))
            {
                return Result.Failure<string>(VTaskErrors.StorageUnavailable);
            }

            var cacheKey = $"vstorage-token:{currentUser.UserId}";
            if (cache.TryGetValue(cacheKey, out string? cached) && !string.IsNullOrEmpty(cached))
            {
                return cached;
            }

            try
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    settings.TokenEndpoint)
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["grant_type"] = "urn:ietf:params:oauth:grant-type:token-exchange",
                        ["client_id"] = settings.ClientId,
                        ["client_secret"] = settings.ClientSecret,
                        ["subject_token"] = subjectToken,
                        ["subject_token_type"] = "urn:ietf:params:oauth:token-type:access_token",
                        ["requested_token_type"] = "urn:ietf:params:oauth:token-type:access_token",
                        ["audience"] = settings.Audience,
                        ["scope"] = settings.Scope
                    })
                };

                using var response = await http.SendAsync(request, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning(
                        "Token exchange for VStorage failed: {Status} {Body}",
                        (int)response.StatusCode, body.Length > 500 ? body[..500] : body);
                    return Result.Failure<string>(VTaskErrors.StorageUnavailable);
                }

                using var json = JsonDocument.Parse(body);
                var token = json.RootElement.GetProperty("access_token").GetString();
                var expiresIn = json.RootElement.TryGetProperty("expires_in", out var expires) && expires.TryGetInt32(out var seconds)
                    ? seconds
                    : 60;

                if (string.IsNullOrEmpty(token))
                {
                    return Result.Failure<string>(VTaskErrors.StorageUnavailable);
                }

                cache.Set(cacheKey, token, TimeSpan.FromSeconds(Math.Max(expiresIn - SafetySeconds, 5)));
                return token;
            }
            catch (Exception exception) when (
                exception is HttpRequestException or JsonException or KeyNotFoundException
                || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested))
            {
                logger.LogError(exception, "Token exchange for VStorage failed");
                return Result.Failure<string>(VTaskErrors.StorageUnavailable);
            }
        }
    }
}
