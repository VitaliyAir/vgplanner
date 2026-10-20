using VGoalsPlanner.Domain.Enums;

namespace VGoalsPlanner.API.DTOs
{
    public sealed class ChangeGoalStatusRequest
    {
        public VGoalStatus NewStatus { get; init; }
    }
}
