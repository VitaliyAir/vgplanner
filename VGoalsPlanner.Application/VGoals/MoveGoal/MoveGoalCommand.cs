using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Errors;

namespace VGoalsPlanner.Application.VGoals
{
    public sealed record MoveGoalCommand(Guid Id, Guid? NewParentGoalId) : ICommand;

    internal sealed class MoveGoalCommandHandler : ICommandHandler<MoveGoalCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public MoveGoalCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result> Handle(MoveGoalCommand request, CancellationToken cancellationToken)
        {
            var goal = await _context.Goals
                .FirstOrDefaultAsync(g => g.Id == request.Id && g.UserId == _currentUser.UserId, cancellationToken);

            if (goal is null)
            {
                return Result.Failure(VGoalErrors.NotFound(request.Id));
            }

            if (request.NewParentGoalId == goal.Id)
            {
                return Result.Failure(VGoalErrors.ParentIsSelf);
            }

            if (request.NewParentGoalId is { } newParentGoalId)
            {
                var newParent = await _context.Goals
                    .FirstOrDefaultAsync(g => g.Id == newParentGoalId && g.UserId == _currentUser.UserId, cancellationToken);

                if (newParent is null)
                {
                    return Result.Failure(VGoalErrors.ParentNotFound);
                }

                // Проверка на цикл: поднимаемся по цепочке родителей нового родителя —
                // если встретим саму перемещаемую цель, значит новый родитель — её потомок.
                var current = newParent;
                while (current.ParentGoalId is not null)
                {
                    if (current.ParentGoalId == goal.Id)
                    {
                        return Result.Failure(VGoalErrors.CircularReference);
                    }

                    current = await _context.Goals
                        .FirstOrDefaultAsync(g => g.Id == current.ParentGoalId, cancellationToken);

                    if (current is null)
                    {
                        break;
                    }
                }
            }

            var siblingsCount = await _context.Goals
                .CountAsync(g => g.ParentGoalId == request.NewParentGoalId && g.UserId == _currentUser.UserId, cancellationToken);

            goal.ParentGoalId = request.NewParentGoalId;
            goal.OrderIndex = siblingsCount;
            goal.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
