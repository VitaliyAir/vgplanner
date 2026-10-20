namespace VGoalsPlanner.Domain.Enums
{
    /// <summary>
    /// Приоритет выполнения задачи.
    /// </summary>
    public enum Priority
    {
        /// <summary>Низкий приоритет — можно выполнить в последнюю очередь</summary>
        Low = 0,

        /// <summary>Средний приоритет (по умолчанию)</summary>
        Medium = 1,

        /// <summary>Высокий приоритет — требует внимания</summary>
        High = 2,

        /// <summary>Критический — необходимо выполнить срочно</summary>
        Critical = 3
    }
}
