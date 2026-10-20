using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Application.VTaskLogs.Common;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Enums;
using VGoalsPlanner.Domain.Errors;

namespace VGoalsPlanner.Application.VTaskLogs
{
    /// <summary>Агрегированный прогресс по цели: средний % выполнения её задач.</summary>
    public sealed record GetGoalProgressQuery(Guid GoalId) : IQuery<GoalProgressResponse>;

    internal sealed class GetGoalProgressQueryHandler : IQueryHandler<GetGoalProgressQuery, GoalProgressResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetGoalProgressQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<GoalProgressResponse>> Handle(GetGoalProgressQuery request, CancellationToken cancellationToken)
        {
            var goalExists = await _context.Goals
                .AnyAsync(g => g.Id == request.GoalId && g.UserId == _currentUser.UserId, cancellationToken);

            if (!goalExists)
            {
                return Result.Failure<GoalProgressResponse>(VGoalErrors.NotFound(request.GoalId));
            }

            var tasks = _context.Tasks.AsNoTracking()
                .Where(t => t.GoalId == request.GoalId && t.UserId == _currentUser.UserId);

            var stats = await tasks
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    TaskCount = g.Count(),
                    CompletedTaskCount = g.Count(t => t.Status == VTaskStatus.Completed),
                    AveragePercent = g.Average(t => (double)t.CompletedPercent)
                })
                .FirstOrDefaultAsync(cancellationToken);

            return new GoalProgressResponse
            {
                GoalId = request.GoalId,
                TaskCount = stats?.TaskCount ?? 0,
                CompletedTaskCount = stats?.CompletedTaskCount ?? 0,
                AverageCompletedPercent = stats is null ? 0 : Math.Round(stats.AveragePercent, 2)
            };
        }
    }
}
