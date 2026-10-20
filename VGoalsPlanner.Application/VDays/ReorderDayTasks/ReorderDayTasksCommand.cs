using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Errors;

namespace VGoalsPlanner.Application.VDays
{
    public sealed record ReorderDayTasksCommand(
        DateOnly Date,
        IReadOnlyList<Guid> OrderedTaskIds) : ICommand;

    internal sealed class ReorderDayTasksCommandValidator : AbstractValidator<ReorderDayTasksCommand>
    {
        public ReorderDayTasksCommandValidator()
        {
            RuleFor(x => x.OrderedTaskIds).NotEmpty();
            RuleFor(x => x.OrderedTaskIds)
                .Must(ids => ids.Distinct().Count() == ids.Count)
                .WithMessage("В списке сортировки есть повторяющиеся идентификаторы");
        }
    }

    internal sealed class ReorderDayTasksCommandHandler : ICommandHandler<ReorderDayTasksCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public ReorderDayTasksCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result> Handle(ReorderDayTasksCommand request, CancellationToken cancellationToken)
        {
            var day = await _context.Days
                .FirstOrDefaultAsync(d => d.UserId == _currentUser.UserId && d.Date == request.Date, cancellationToken);

            if (day is null)
            {
                return Result.Failure(VDayErrors.DayNotFound);
            }

            var links = await _context.DayTasks
                .Where(dt => dt.VDayId == day.Id)
                .ToListAsync(cancellationToken);

            var requested = request.OrderedTaskIds.ToHashSet();
            if (requested.Count != links.Count || !links.All(l => requested.Contains(l.VTaskId)))
            {
                return Result.Failure(VDayErrors.ReorderMismatch);
            }

            var position = new Dictionary<Guid, int>();
            for (var i = 0; i < request.OrderedTaskIds.Count; i++)
            {
                position[request.OrderedTaskIds[i]] = i;
            }

            foreach (var link in links)
            {
                link.OrderIndex = position[link.VTaskId];
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
