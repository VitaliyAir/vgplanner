using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Errors;

namespace VGoalsPlanner.Application.VGoals
{
    public sealed record DeleteGoalCommand(Guid Id) : ICommand;

    internal sealed class DeleteGoalCommandHandler : ICommandHandler<DeleteGoalCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public DeleteGoalCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result> Handle(DeleteGoalCommand request, CancellationToken cancellationToken)
        {
            var goalExists = await _context.Goals
                .AnyAsync(g => g.Id == request.Id && g.UserId == _currentUser.UserId, cancellationToken);

            if (!goalExists)
            {
                return Result.Failure(VGoalErrors.NotFound(request.Id));
            }

            // Каскадное мягкое удаление: сама цель + все дочерние цели (рекурсивно) + все задачи внутри них.
            var goalIds = await CollectSubtreeGoalIdsAsync(request.Id, _currentUser.UserId, cancellationToken);

            var now = DateTime.UtcNow;

            var goals = await _context.Goals
                .Where(g => goalIds.Contains(g.Id))
                .ToListAsync(cancellationToken);

            foreach (var goal in goals)
            {
                goal.IsDeleted = true;
                goal.UpdatedAt = now;
            }

            var tasks = await _context.Tasks
                .Where(t => goalIds.Contains(t.GoalId))
                .ToListAsync(cancellationToken);

            foreach (var task in tasks)
            {
                task.IsDeleted = true;
                task.UpdatedAt = now;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }

        private async Task<List<Guid>> CollectSubtreeGoalIdsAsync(
            Guid rootGoalId,
            Guid userId,
            CancellationToken cancellationToken)
        {
            var result = new List<Guid> { rootGoalId };
            var frontier = new List<Guid> { rootGoalId };

            while (frontier.Count > 0)
            {
                var children = await _context.Goals
                    .Where(g => g.ParentGoalId != null && frontier.Contains(g.ParentGoalId!.Value) && g.UserId == userId)
                    .Select(g => g.Id)
                    .ToListAsync(cancellationToken);

                if (children.Count == 0)
                {
                    break;
                }

                result.AddRange(children);
                frontier = children;
            }

            return result;
        }
    }
}
