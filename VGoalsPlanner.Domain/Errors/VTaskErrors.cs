using VGoalsPlanner.Domain.Abstractions;

namespace VGoalsPlanner.Domain.Errors
{
    public static class VTaskErrors
    {
        public static Error NotFound(Guid id) => new(
            "VTask.NotFound",
            $"Задача с идентификатором '{id}' не найдена");

        public static readonly Error GoalNotFound = new(
            "VTask.GoalNotFound",
            "Цель, к которой привязывается задача, не найдена");

        public static Error AttachmentNotFound(Guid id) => new(
            "VTask.AttachmentNotFound",
            $"Вложение '{id}' не найдено");

        public static Error AttachmentTooLarge(long maxBytes) => new(
            "VTask.AttachmentTooLarge",
            $"Файл слишком большой: допустимо до {maxBytes / (1024 * 1024)} МБ");

        public static readonly Error AttachmentLengthRequired = new(
            "VTask.AttachmentLengthRequired",
            "Не указан размер загружаемого файла");

        public static readonly Error StorageUnavailable = new(
            "VTask.StorageUnavailable",
            "Хранилище файлов недоступно или не настроено");

        public static readonly Error StorageForbidden = new(
            "VTask.StorageForbidden",
            "У пользователя нет права создавать хранилище файлов (нужна клиентская роль bucket-creator клиента vstorage)");

        public static readonly Error ReorderMismatch = new(
            "VTask.ReorderMismatch",
            "Список для сортировки не совпадает с набором задач этой цели");
    }
}
