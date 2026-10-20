namespace VGoalsPlanner.Domain.Entities
{
    /// <summary>
    /// Файл, прикреплённый к задаче. Сами байты лежат в VStorageAPI, здесь — только метаданные и ключ объекта.
    /// </summary>
    public class VTaskAttachment : AuditableEntity
    {
        /// <summary>Исходное имя файла (для показа и скачивания)</summary>
        public string FileName { get; set; } = string.Empty;

        public string ContentType { get; set; } = "application/octet-stream";

        public long SizeBytes { get; set; }

        /// <summary>Ключ объекта в хранилище: "{taskId}/{attachmentId}" (имя файла в ключ не попадает).</summary>
        public string StorageKey { get; set; } = string.Empty;

        /// <summary>Идентификатор объекта в VStorage: скачивание и удаление идут по нему.</summary>
        public Guid StorageObjectId { get; set; }

        public Guid VTaskId { get; set; }

        /// <summary>Владелец (пользователь, загрузивший файл)</summary>
        public Guid UserId { get; set; }

        public virtual VTask VTask { get; set; } = null!;
    }
}
