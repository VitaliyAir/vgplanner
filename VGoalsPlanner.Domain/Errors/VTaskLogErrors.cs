using VGoalsPlanner.Domain.Abstractions;

namespace VGoalsPlanner.Domain.Errors
{
    public static class VTaskLogErrors
    {
        public static Error NotFound(Guid id) => new(
            "VTaskLog.NotFound",
            $"Запись лога с идентификатором '{id}' не найдена");
    }
}
