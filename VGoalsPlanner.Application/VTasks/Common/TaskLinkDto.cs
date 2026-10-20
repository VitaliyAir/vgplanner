using VGoalsPlanner.Domain.Enums;

namespace VGoalsPlanner.Application.VTasks.Common
{
    public sealed record TaskLinkDto(TaskLinkKind Kind, string Target, string? Alias);
}
