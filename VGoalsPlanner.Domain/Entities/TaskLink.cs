using VGoalsPlanner.Domain.Enums;

namespace VGoalsPlanner.Domain.Entities
{
    /// <summary>Ссылка или путь, связанные с задачей (хранится внутри задачи как JSON, отдельной таблицы нет)</summary>
    public class TaskLink
    {
        public TaskLinkKind Kind { get; set; }

        /// <summary>Адрес (http/https) или путь к папке/файлу</summary>
        public string Target { get; set; } = string.Empty;

        /// <summary>Короткое имя для показа вместо адреса (необязательное)</summary>
        public string? Alias { get; set; }
    }
}
