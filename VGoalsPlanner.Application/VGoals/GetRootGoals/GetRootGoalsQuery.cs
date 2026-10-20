using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Application.VGoals.Common;
using VGoalsPlanner.Domain.Abstractions;

namespace VGoalsPlanner.Application.VGoals
{
    /// <summary>
    /// Верхний уровень дерева: цели пользователя без родителя (ParentGoalId == null).
    /// Дальше дерево разворачивается лениво через GetGoalByIdQuery.
    /// </summary>
    public sealed record GetRootGoalsQuery : IQuery<IReadOnlyList<GoalListItemResponse>>;

    internal sealed class GetRootGoalsQueryHandler
        : IQueryHandler<GetRootGoalsQuery, IReadOnlyList<GoalListItemResponse>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetRootGoalsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<IReadOnlyList<GoalListItemResponse>>> Handle(
            GetRootGoalsQuery request,
            CancellationToken cancellationToken)
        {
            var roots = await _context.Goals.AsNoTracking()
                .Where(g => g.ParentGoalId == null && g.UserId == _currentUser.UserId)
                .OrderBy(g => g.OrderIndex)
                .Select(g => new GoalListItemResponse
                {
                    Id = g.Id,
                    Title = g.Title,
                    Priority = g.Priority,
                    Status = g.Status,
                    ParentGoalId = g.ParentGoalId,
                    OrderIndex = g.OrderIndex,
                    HasChildGoals = g.ChildGoals.Any(),
                    HasChildTasks = g.ChildTasks.Any()
                })
                .ToListAsync(cancellationToken);

            return roots;
        }
    }
}
