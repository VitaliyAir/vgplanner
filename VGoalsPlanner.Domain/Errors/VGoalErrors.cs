using VGoalsPlanner.Domain.Abstractions;

namespace VGoalsPlanner.Domain.Errors
{
    public static class VGoalErrors
    {
        public static Error NotFound(Guid id) => new(
            "VGoal.NotFound",
            $"Цель с идентификатором '{id}' не найдена");

        public static readonly Error ParentNotFound = new(
            "VGoal.ParentNotFound",
            "Родительская цель не найдена");

        public static readonly Error ParentIsSelf = new(
            "VGoal.ParentIsSelf",
            "Цель не может быть родителем самой себя");

        public static readonly Error CircularReference = new(
            "VGoal.CircularReference",
            "Перемещение создаст циклическую ссылку в иерархии целей");

        public static readonly Error ReorderMismatch = new(
            "VGoal.ReorderMismatch",
            "Список сортировки не совпадает с набором целей");

        public static readonly Error HasChildren = new(
            "VGoal.HasChildren",
            "Нельзя удалить цель, пока у неё есть дочерние цели или задачи");
    }
}
