using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Application.VTasks.Common;
using VGoalsPlanner.Domain.Abstractions;

namespace VGoalsPlanner.Application.VDays
{
    /// <summary>Задачи пользователя, не привязанные ни к одному дню.</summary>
    public sealed record GetUnscheduledTasksQuery : IQuery<IReadOnlyList<TaskListItemResponse>>;

    internal sealed class GetUnscheduledTasksQueryHandler
        : IQueryHandler<GetUnscheduledTasksQuery, IReadOnlyList<TaskListItemResponse>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetUnscheduledTasksQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<IReadOnlyList<TaskListItemResponse>>> Handle(
            GetUnscheduledTasksQuery request,
            CancellationToken cancellationToken)
        {
            var tasks = await _context.Tasks.AsNoTracking()
                .Where(t => t.UserId == _currentUser.UserId
                    && !_context.DayTasks.Any(dt => dt.VTaskId == t.Id))
                .OrderBy(t => t.Deadline == null)
                .ThenBy(t => t.Deadline)
                .ThenBy(t => t.Title)
                .Select(t => new TaskListItemResponse
                {
                    Id = t.Id,
                    Title = t.Title,
                    Status = t.Status,
                    Priority = t.Priority,
                    Type = t.Type,
                    Deadline = t.Deadline,
                    OrderIndex = t.OrderIndex,
                    CompletedPercent = t.CompletedPercent,
                    GoalId = t.GoalId,
                    Tags = t.Tags
                })
                .ToListAsync(cancellationToken);

            return tasks;
        }
    }
}
