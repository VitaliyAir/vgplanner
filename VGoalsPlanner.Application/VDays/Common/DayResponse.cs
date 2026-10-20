namespace VGoalsPlanner.Application.VDays.Common
{
    public sealed class DayResponse
    {
        public DateOnly Date { get; init; }
        public string? SpecialNote { get; init; }
    }
}
