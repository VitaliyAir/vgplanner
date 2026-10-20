using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Errors;

namespace VGoalsPlanner.Application.VGoals
{
    public sealed record GetGoalBreadcrumbQuery(Guid GoalId) : IQuery<IReadOnlyList<string>>;

    internal sealed class GetGoalBreadcrumbQueryHandler : IQueryHandler<GetGoalBreadcrumbQuery, IReadOnlyList<string>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetGoalBreadcrumbQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<IReadOnlyList<string>>> Handle(GetGoalBreadcrumbQuery request, CancellationToken cancellationToken)
        {
            var goal = await _context.Goals.AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == request.GoalId && g.UserId == _currentUser.UserId, cancellationToken);

            if (goal is null)
            {
                return Result.Failure<IReadOnlyList<string>>(VGoalErrors.NotFound(request.GoalId));
            }

            // Поднимаемся по цепочке родителей до корня, как в MoveGoalCommandHandler —
            // без рекурсивного Include (EF Core не умеет произвольную глубину), по одному запросу на уровень.
            var titles = new List<string> { goal.Title };
            var current = goal;
            while (current.ParentGoalId is not null)
            {
                current = await _context.Goals.AsNoTracking()
                    .FirstOrDefaultAsync(g => g.Id == current.ParentGoalId, cancellationToken);

                if (current is null)
                {
                    break;
                }

                titles.Add(current.Title);
            }

            titles.Reverse();

            return titles;
        }
    }
}
