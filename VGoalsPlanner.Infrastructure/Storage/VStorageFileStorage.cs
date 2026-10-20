using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Errors;

namespace VGoalsPlanner.Infrastructure.Storage
{
    /// <summary>
    /// Вложения в VStorageAPI v2: Bearer-токен пользователя (обмен в VStorageTokenProvider), у каждого пользователя
    /// свой bucket, объекты private. Тела передаются потоком, без буферизации в памяти.
    /// </summary>
    public sealed class VStorageFileStorage(
        HttpClient http,
        VStorageTokenProvider tokens,
        IMemoryCache cache,
        IOptions<VStorageOptions> options,
        ICurrentUserService currentUser,
        ILogger<VStorageFileStorage> logger) : IFileStorage
    {
        private static readonly Error TooLarge = new("VTask.AttachmentTooLarge", "Файл слишком большой для хранилища");
        private static readonly Error MissingInStorage = new("VTask.AttachmentNotFound", "Файл вложения не найден в хранилище");

        private sealed record BucketInfo(Guid Id, string Name, string? Role);

        private sealed record StoredObject(Guid Id);

        public async Task<Result<Guid>> UploadAsync(
            string key, string fileName, Stream content, string contentType, long? contentLength, CancellationToken cancellationToken)
        {
            var token = await tokens.GetTokenAsync(cancellationToken);
            if (token.IsFailure)
            {
                return Result.Failure<Guid>(token.Error);
            }

            try
            {
                var bucket = await ResolveBucketAsync(token.Value, cancellationToken);
                if (bucket.IsFailure)
                {
                    return Result.Failure<Guid>(bucket.Error);
                }

                using var request = CreateRequest(
                    HttpMethod.Put,
                    $"api/v1/buckets/{bucket.Value}/objects?key={Uri.EscapeDataString(key)}&visibility=private",
                    token.Value);
                request.Headers.IfNoneMatch.Add(EntityTagHeaderValue.Any);

                var body = new StreamContent(content);
                body.Headers.ContentType = MediaTypeHeaderValue.TryParse(contentType, out var parsed)
                    ? parsed
                    : new MediaTypeHeaderValue("application/octet-stream");
                body.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileNameStar = fileName };
                if (contentLength.HasValue)
                {
                    body.Headers.ContentLength = contentLength;
                }
                request.Content = body;

                using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var stored = await response.Content.ReadFromJsonAsync<StoredObject>(cancellationToken);
                    return stored is { Id: var id } && id != Guid.Empty
                        ? id
                        : Result.Failure<Guid>(VTaskErrors.StorageUnavailable);
                }

                if (response.StatusCode == HttpStatusCode.RequestEntityTooLarge)
                {
                    return Result.Failure<Guid>(TooLarge);
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    // bucket мог быть удалён — в следующий раз найдём или создадим заново
                    cache.Remove(BucketCacheKey);
                }

                await LogFailureAsync("upload", key, response, cancellationToken);
                return Result.Failure<Guid>(VTaskErrors.StorageUnavailable);
            }
            catch (Exception exception) when (IsTransport(exception, cancellationToken))
            {
                logger.LogError(exception, "VStorage upload of {Key} failed", key);
                return Result.Failure<Guid>(VTaskErrors.StorageUnavailable);
            }
        }

        public async Task<Result<Stream>> OpenReadAsync(Guid objectId, CancellationToken cancellationToken)
        {
            var token = await tokens.GetTokenAsync(cancellationToken);
            if (token.IsFailure)
            {
                return Result.Failure<Stream>(token.Error);
            }

            try
            {
                using var request = CreateRequest(HttpMethod.Get, $"api/v1/objects/{objectId}/content", token.Value);
                var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    // Ответ закроется вместе с потоком, который отдаёт вызывающий.
                    return await response.Content.ReadAsStreamAsync(cancellationToken);
                }

                using (response)
                {
                    await LogFailureAsync("download", objectId.ToString(), response, cancellationToken);
                    return Result.Failure<Stream>(
                        response.StatusCode == HttpStatusCode.NotFound ? MissingInStorage : VTaskErrors.StorageUnavailable);
                }
            }
            catch (Exception exception) when (IsTransport(exception, cancellationToken))
            {
                logger.LogError(exception, "VStorage download of {ObjectId} failed", objectId);
                return Result.Failure<Stream>(VTaskErrors.StorageUnavailable);
            }
        }

        public async Task<Result> DeleteAsync(Guid objectId, CancellationToken cancellationToken)
        {
            var token = await tokens.GetTokenAsync(cancellationToken);
            if (token.IsFailure)
            {
                return Result.Failure(token.Error);
            }

            try
            {
                using var request = CreateRequest(HttpMethod.Delete, $"api/v1/objects/{objectId}", token.Value);
                using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
                {
                    return Result.Success();
                }

                await LogFailureAsync("delete", objectId.ToString(), response, cancellationToken);
                return Result.Failure(VTaskErrors.StorageUnavailable);
            }
            catch (Exception exception) when (IsTransport(exception, cancellationToken))
            {
                logger.LogError(exception, "VStorage delete of {ObjectId} failed", objectId);
                return Result.Failure(VTaskErrors.StorageUnavailable);
            }
        }

        private string BucketCacheKey => $"vstorage-bucket:{currentUser.UserId}";

        // Bucket пользователя: найти по имени среди своих, иначе создать (нужна клиентская роль bucket-creator).
        private async Task<Result<Guid>> ResolveBucketAsync(string token, CancellationToken cancellationToken)
        {
            if (cache.TryGetValue(BucketCacheKey, out Guid cached) && cached != Guid.Empty)
            {
                return cached;
            }

            var name = options.Value.BucketName;

            var existing = await FindBucketAsync(token, name, cancellationToken);
            if (existing.IsFailure)
            {
                return Result.Failure<Guid>(existing.Error);
            }

            if (existing.Value is { } foundId)
            {
                cache.Set(BucketCacheKey, foundId, TimeSpan.FromHours(1));
                return foundId;
            }

            using var create = CreateRequest(HttpMethod.Post, "api/v1/buckets", token);
            create.Content = JsonContent.Create(new { name });
            using var response = await http.SendAsync(create, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Created)
            {
                var bucket = await response.Content.ReadFromJsonAsync<BucketInfo>(cancellationToken);
                if (bucket is not null)
                {
                    cache.Set(BucketCacheKey, bucket.Id, TimeSpan.FromHours(1));
                    return bucket.Id;
                }
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                logger.LogWarning(
                    "User {UserId} may not create the VStorage bucket '{Bucket}': the client role bucket-creator of client 'vstorage' is missing",
                    currentUser.UserId, name);
                return Result.Failure<Guid>(VTaskErrors.StorageForbidden);
            }

            await LogFailureAsync("create-bucket", name, response, cancellationToken);
            return Result.Failure<Guid>(VTaskErrors.StorageUnavailable);
        }

        private async Task<Result<Guid?>> FindBucketAsync(string token, string name, CancellationToken cancellationToken)
        {
            using var request = CreateRequest(HttpMethod.Get, "api/v1/buckets", token);
            using var response = await http.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                await LogFailureAsync("list-buckets", name, response, cancellationToken);
                return Result.Failure<Guid?>(VTaskErrors.StorageUnavailable);
            }

            var buckets = await response.Content.ReadFromJsonAsync<List<BucketInfo>>(cancellationToken) ?? [];
            var match = buckets
                .Where(b => string.Equals(b.Name, name, StringComparison.Ordinal))
                .OrderByDescending(b => string.Equals(b.Role, "owner", StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            return Result.Success<Guid?>(match?.Id);
        }

        private static HttpRequestMessage CreateRequest(HttpMethod method, string relativeUrl, string token)
        {
            var request = new HttpRequestMessage(method, relativeUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return request;
        }

        private async Task LogFailureAsync(string operation, string subject, HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning(
                "VStorage {Operation} {Subject} returned {Status}: {Body}",
                operation, subject, (int)response.StatusCode, body.Length > 500 ? body[..500] : body);
        }

        // Сетевые сбои, нечитаемый ответ и таймаут хранилища — "недоступно"; отмену запроса клиентом пробрасываем как есть.
        private static bool IsTransport(Exception exception, CancellationToken cancellationToken)
            => exception is HttpRequestException or JsonException
               || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested);
    }
}
