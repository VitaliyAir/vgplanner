using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Domain.Abstractions;

namespace VGoalsPlanner.Application.VDays
{
    public sealed record UnscheduleTaskCommand(DateOnly Date, Guid TaskId) : ICommand;

    internal sealed class UnscheduleTaskCommandHandler : ICommandHandler<UnscheduleTaskCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public UnscheduleTaskCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result> Handle(UnscheduleTaskCommand request, CancellationToken cancellationToken)
        {
            // Идемпотентно: если дня или связи нет — считаем, что снимать нечего.
            var link = await _context.DayTasks
                .Where(dt => dt.VTaskId == request.TaskId
                    && dt.Day.Date == request.Date
                    && dt.Day.UserId == _currentUser.UserId)
                .FirstOrDefaultAsync(cancellationToken);

            if (link is not null)
            {
                _context.DayTasks.Remove(link);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return Result.Success();
        }
    }
}
