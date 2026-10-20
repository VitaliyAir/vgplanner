using VGoalsPlanner.Domain.Enums;

namespace VGoalsPlanner.API.DTOs
{
    public sealed class ChangeTaskStatusRequest
    {
        public VTaskStatus NewStatus { get; init; }
    }
}
