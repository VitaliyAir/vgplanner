using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Errors;

namespace VGoalsPlanner.Application.VGoals
{
    public sealed record ReorderGoalsCommand(
        Guid? ParentGoalId,
        IReadOnlyList<Guid> OrderedGoalIds) : ICommand;

    internal sealed class ReorderGoalsCommandValidator : AbstractValidator<ReorderGoalsCommand>
    {
        public ReorderGoalsCommandValidator()
        {
            RuleFor(x => x.OrderedGoalIds).NotEmpty();
            RuleFor(x => x.OrderedGoalIds)
                .Must(ids => ids.Distinct().Count() == ids.Count)
                .WithMessage("В списке сортировки есть повторяющиеся идентификаторы");
        }
    }

    internal sealed class ReorderGoalsCommandHandler : ICommandHandler<ReorderGoalsCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public ReorderGoalsCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result> Handle(ReorderGoalsCommand request, CancellationToken cancellationToken)
        {
            if (request.ParentGoalId is { } parentGoalId)
            {
                var parentExists = await _context.Goals
                    .AnyAsync(g => g.Id == parentGoalId && g.UserId == _currentUser.UserId, cancellationToken);

                if (!parentExists)
                {
                    return Result.Failure(VGoalErrors.ParentNotFound);
                }
            }

            var siblings = await _context.Goals
                .Where(g => g.ParentGoalId == request.ParentGoalId && g.UserId == _currentUser.UserId)
                .ToListAsync(cancellationToken);

            var requested = request.OrderedGoalIds.ToHashSet();
            if (requested.Count != siblings.Count || !siblings.All(g => requested.Contains(g.Id)))
            {
                return Result.Failure(VGoalErrors.ReorderMismatch);
            }

            var position = new Dictionary<Guid, int>();
            for (var i = 0; i < request.OrderedGoalIds.Count; i++)
            {
                position[request.OrderedGoalIds[i]] = i;
            }

            var now = DateTime.UtcNow;
            foreach (var goal in siblings)
            {
                var newIndex = position[goal.Id];
                if (goal.OrderIndex != newIndex)
                {
                    goal.OrderIndex = newIndex;
                    goal.UpdatedAt = now;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
