namespace VGoalsPlanner.Domain.Enums
{
    /// <summary>
    /// Статус выполнения задачи.
    /// </summary>
    public enum VTaskStatus
    {
        /// <summary>Только что создана, работа не начиналась</summary>
        New = 0,

        /// <summary>В процессе выполнения</summary>
        InProgress = 1,

        /// <summary>Завершена успешно</summary>
        Completed = 2,

        /// <summary>Приостановлена (например, ожидание чего-либо)</summary>
        OnHold = 3,

        /// <summary>Отменена (не будет выполняться)</summary>
        Cancelled = 4
    }
}
