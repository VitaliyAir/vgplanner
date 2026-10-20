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
    public sealed record UpdateGoalCommand(
        Guid Id,
        string Title,
        string? Description,
        Priority Priority) : ICommand;

    internal sealed class UpdateGoalCommandValidator : AbstractValidator<UpdateGoalCommand>
    {
        public UpdateGoalCommandValidator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(2000);
            RuleFor(x => x.Priority).IsInEnum();
        }
    }

    internal sealed class UpdateGoalCommandHandler : ICommandHandler<UpdateGoalCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public UpdateGoalCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result> Handle(UpdateGoalCommand request, CancellationToken cancellationToken)
        {
            var goal = await _context.Goals
                .FirstOrDefaultAsync(g => g.Id == request.Id && g.UserId == _currentUser.UserId, cancellationToken);

            if (goal is null)
            {
                return Result.Failure(VGoalErrors.NotFound(request.Id));
            }

            goal.Title = request.Title;
            goal.Description = request.Description;
            goal.Priority = request.Priority;
            goal.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
