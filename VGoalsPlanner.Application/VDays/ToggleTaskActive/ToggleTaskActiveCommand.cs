using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Errors;

namespace VGoalsPlanner.Application.VDays
{
    public sealed record ToggleTaskActiveCommand(DateOnly Date, Guid TaskId) : ICommand;

    internal sealed class ToggleTaskActiveCommandHandler : ICommandHandler<ToggleTaskActiveCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public ToggleTaskActiveCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result> Handle(ToggleTaskActiveCommand request, CancellationToken cancellationToken)
        {
            var taskExists = await _context.Tasks
                .AnyAsync(t => t.Id == request.TaskId && t.UserId == _currentUser.UserId, cancellationToken);

            if (!taskExists)
            {
                return Result.Failure(VDayErrors.TaskNotFound);
            }

            var day = await _context.Days
                .FirstOrDefaultAsync(d => d.UserId == _currentUser.UserId && d.Date == request.Date, cancellationToken);

            if (day is null)
            {
                return Result.Failure(VDayErrors.DayNotFound);
            }

            var link = await _context.DayTasks
                .FirstOrDefaultAsync(dt => dt.VDayId == day.Id && dt.VTaskId == request.TaskId, cancellationToken);

            if (link is null)
            {
                return Result.Failure(VDayErrors.TaskNotScheduled);
            }

            link.IsActive = !link.IsActive;

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
