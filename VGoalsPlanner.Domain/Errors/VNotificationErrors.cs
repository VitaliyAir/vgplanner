using VGoalsPlanner.Domain.Abstractions;

namespace VGoalsPlanner.Domain.Errors
{
    public static class VNotificationErrors
    {
        public static Error NotFound(Guid id) => new(
            "VNotification.NotFound",
            $"Заметка с идентификатором '{id}' не найдена");
    }
}
