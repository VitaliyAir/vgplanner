using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Errors;

namespace VGoalsPlanner.Application.VTasks
{
    public sealed record MoveTaskCommand(Guid Id, Guid NewGoalId) : ICommand;

    internal sealed class MoveTaskCommandValidator : AbstractValidator<MoveTaskCommand>
    {
        public MoveTaskCommandValidator()
        {
            RuleFor(x => x.NewGoalId).NotEmpty();
        }
    }

    internal sealed class MoveTaskCommandHandler : ICommandHandler<MoveTaskCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public MoveTaskCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result> Handle(MoveTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == request.Id && t.UserId == _currentUser.UserId, cancellationToken);

            if (task is null)
            {
                return Result.Failure(VTaskErrors.NotFound(request.Id));
            }

            if (task.GoalId == request.NewGoalId)
            {
                return Result.Success();
            }

            var goalExists = await _context.Goals
                .AnyAsync(g => g.Id == request.NewGoalId && g.UserId == _currentUser.UserId, cancellationToken);

            if (!goalExists)
            {
                return Result.Failure(VTaskErrors.GoalNotFound);
            }

            var siblingsCount = await _context.Tasks
                .CountAsync(t => t.GoalId == request.NewGoalId && t.UserId == _currentUser.UserId, cancellationToken);

            task.GoalId = request.NewGoalId;
            task.OrderIndex = siblingsCount;
            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
