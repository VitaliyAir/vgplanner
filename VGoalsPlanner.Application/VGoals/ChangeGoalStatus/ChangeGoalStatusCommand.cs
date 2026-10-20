using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Enums;
using VGoalsPlanner.Domain.Errors;

namespace VGoalsPlanner.Application.VGoals
{
    public sealed record ChangeGoalStatusCommand(Guid Id, VGoalStatus NewStatus) : ICommand;

    internal sealed class ChangeGoalStatusCommandValidator : AbstractValidator<ChangeGoalStatusCommand>
    {
        public ChangeGoalStatusCommandValidator()
        {
            RuleFor(x => x.NewStatus).IsInEnum();
        }
    }

    internal sealed class ChangeGoalStatusCommandHandler : ICommandHandler<ChangeGoalStatusCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public ChangeGoalStatusCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result> Handle(ChangeGoalStatusCommand request, CancellationToken cancellationToken)
        {
            var goal = await _context.Goals
                .FirstOrDefaultAsync(g => g.Id == request.Id && g.UserId == _currentUser.UserId, cancellationToken);

            if (goal is null)
            {
                return Result.Failure(VGoalErrors.NotFound(request.Id));
            }

            goal.Status = request.NewStatus;
            goal.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
