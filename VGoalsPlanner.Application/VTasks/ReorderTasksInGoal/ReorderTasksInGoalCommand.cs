using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Errors;

namespace VGoalsPlanner.Application.VTasks
{
    public sealed record ReorderTasksInGoalCommand(
        Guid GoalId,
        IReadOnlyList<Guid> OrderedTaskIds) : ICommand;

    internal sealed class ReorderTasksInGoalCommandValidator : AbstractValidator<ReorderTasksInGoalCommand>
    {
        public ReorderTasksInGoalCommandValidator()
        {
            RuleFor(x => x.GoalId).NotEmpty();
            RuleFor(x => x.OrderedTaskIds).NotEmpty();
            RuleFor(x => x.OrderedTaskIds)
                .Must(ids => ids.Distinct().Count() == ids.Count)
                .WithMessage("В списке сортировки есть повторяющиеся идентификаторы");
        }
    }

    internal sealed class ReorderTasksInGoalCommandHandler : ICommandHandler<ReorderTasksInGoalCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public ReorderTasksInGoalCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result> Handle(ReorderTasksInGoalCommand request, CancellationToken cancellationToken)
        {
            var goalExists = await _context.Goals
                .AnyAsync(g => g.Id == request.GoalId && g.UserId == _currentUser.UserId, cancellationToken);

            if (!goalExists)
            {
                return Result.Failure(VTaskErrors.GoalNotFound);
            }

            var tasks = await _context.Tasks
                .Where(t => t.GoalId == request.GoalId && t.UserId == _currentUser.UserId)
                .ToListAsync(cancellationToken);

            var requested = request.OrderedTaskIds.ToHashSet();
            if (requested.Count != tasks.Count || !tasks.All(t => requested.Contains(t.Id)))
            {
                return Result.Failure(VTaskErrors.ReorderMismatch);
            }

            var position = new Dictionary<Guid, int>();
            for (var i = 0; i < request.OrderedTaskIds.Count; i++)
            {
                position[request.OrderedTaskIds[i]] = i;
            }

            var now = DateTime.UtcNow;
            foreach (var task in tasks)
            {
                var newIndex = position[task.Id];
                if (task.OrderIndex != newIndex)
                {
                    task.OrderIndex = newIndex;
                    task.UpdatedAt = now;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
