using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Entities;
using VGoalsPlanner.Domain.Enums;
using VGoalsPlanner.Domain.Errors;

namespace VGoalsPlanner.Application.VGoals
{
    public sealed record CreateGoalCommand(
        string Title,
        string? Description,
        Priority Priority,
        Guid? ParentGoalId) : ICommand<Guid>;

    internal sealed class CreateGoalCommandValidator : AbstractValidator<CreateGoalCommand>
    {
        public CreateGoalCommandValidator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(2000);
            RuleFor(x => x.Priority).IsInEnum();
        }
    }

    internal sealed class CreateGoalCommandHandler : ICommandHandler<CreateGoalCommand, Guid>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public CreateGoalCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(CreateGoalCommand request, CancellationToken cancellationToken)
        {
            if (request.ParentGoalId is { } parentGoalId)
            {
                var parentExists = await _context.Goals
                    .AnyAsync(g => g.Id == parentGoalId && g.UserId == _currentUser.UserId, cancellationToken);

                if (!parentExists)
                {
                    return Result.Failure<Guid>(VGoalErrors.ParentNotFound);
                }
            }

            var siblingsCount = await _context.Goals
                .CountAsync(g => g.ParentGoalId == request.ParentGoalId && g.UserId == _currentUser.UserId, cancellationToken);

            var now = DateTime.UtcNow;

            var goal = new VGoal
            {
                Title = request.Title,
                Description = request.Description,
                Priority = request.Priority,
                ParentGoalId = request.ParentGoalId,
                OrderIndex = siblingsCount,
                UserId = _currentUser.UserId,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.Goals.Add(goal);
            await _context.SaveChangesAsync(cancellationToken);

            return goal.Id;
        }
    }
}
