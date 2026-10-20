namespace VGoalsPlanner.Domain.Entities
{
    public class VDayVTask
    {
        public Guid VDayId { get; set; }
        public Guid VTaskId { get; set; }   // было GoalItemId
        public int OrderIndex { get; set; }
        public bool IsActive { get; set; }

        public virtual VDay Day { get; set; } = null!;
        public virtual VTask Task { get; set; } = null!; // было GoalItem
    }
}
