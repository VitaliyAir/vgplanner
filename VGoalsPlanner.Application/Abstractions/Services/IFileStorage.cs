using VGoalsPlanner.Domain.Abstractions;

namespace VGoalsPlanner.Application.Abstractions.Services
{
    /// <summary>Хранилище байтов вложений (VStorageAPI): у каждого пользователя свой bucket.</summary>
    public interface IFileStorage
    {
        /// <summary>Загружает файл в хранилище текущего пользователя; возвращает идентификатор объекта.</summary>
        Task<Result<Guid>> UploadAsync(string key, string fileName, Stream content, string contentType, long? contentLength, CancellationToken cancellationToken);

        /// <summary>Открывает поток чтения; вызывающий обязан его закрыть.</summary>
        Task<Result<Stream>> OpenReadAsync(Guid objectId, CancellationToken cancellationToken);

        /// <summary>Удаляет объект; отсутствие объекта — не ошибка.</summary>
        Task<Result> DeleteAsync(Guid objectId, CancellationToken cancellationToken);
    }
}
