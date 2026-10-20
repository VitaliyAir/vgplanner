using VGoalsPlanner.Domain.Abstractions;

namespace VGoalsPlanner.Domain.Errors
{
    public static class VDayErrors
    {
        public static readonly Error TaskNotFound = new(
            "VDay.TaskNotFound",
            "Задача для планирования не найдена");

        public static readonly Error DayNotFound = new(
            "VDay.NotFound",
            "На эту дату ничего не запланировано");

        public static readonly Error ReorderMismatch = new(
            "VDay.ReorderMismatch",
            "Список для сортировки не совпадает с набором задач этого дня");

        public static readonly Error TaskNotScheduled = new(
            "VDay.TaskNotScheduled",
            "Задача не запланирована на этот день");
    }
}
