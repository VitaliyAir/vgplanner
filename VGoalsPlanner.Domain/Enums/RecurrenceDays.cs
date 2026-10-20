namespace VGoalsPlanner.Domain.Enums
{
    /// <summary>
    /// Маска дней недели для регулярных задач: Пн=1, Вт=2, Ср=4, Чт=8, Пт=16, Сб=32, Вс=64; 0 — не повторяется.
    /// </summary>
    public static class RecurrenceDays
    {
        public const int None = 0;
        public const int All = 127;

        public static int MaskOf(DayOfWeek day) => 1 << (((int)day + 6) % 7);

        public static bool Includes(int mask, DayOfWeek day) => (mask & MaskOf(day)) != 0;
    }
}
