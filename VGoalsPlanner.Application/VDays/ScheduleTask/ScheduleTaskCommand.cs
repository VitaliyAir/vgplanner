using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Entities;
using VGoalsPlanner.Domain.Errors;

namespace VGoalsPlanner.Application.VDays
{
    public sealed record ScheduleTaskCommand(DateOnly Date, Guid TaskId, int? OrderIndex) : ICommand;

    internal sealed class ScheduleTaskCommandValidator : AbstractValidator<ScheduleTaskCommand>
    {
        public ScheduleTaskCommandValidator()
        {
            RuleFor(x => x.TaskId).NotEmpty();
            RuleFor(x => x.OrderIndex).GreaterThanOrEqualTo(0).When(x => x.OrderIndex.HasValue);
        }
    }

    internal sealed class ScheduleTaskCommandHandler : ICommandHandler<ScheduleTaskCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public ScheduleTaskCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result> Handle(ScheduleTaskCommand request, CancellationToken cancellationToken)
        {
            var taskExists = await _context.Tasks
                .AnyAsync(t => t.Id == request.TaskId && t.UserId == _currentUser.UserId, cancellationToken);

            if (!taskExists)
            {
                return Result.Failure(VDayErrors.TaskNotFound);
            }

            var now = DateTime.UtcNow;

            var day = await _context.Days
                .FirstOrDefaultAsync(d => d.UserId == _currentUser.UserId && d.Date == request.Date, cancellationToken);

            if (day is null)
            {
                day = new VDay
                {
                    Date = request.Date,
                    UserId = _currentUser.UserId,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _context.Days.Add(day);
            }

            var link = await _context.DayTasks
                .FirstOrDefaultAsync(dt => dt.VDayId == day.Id && dt.VTaskId == request.TaskId, cancellationToken);

            if (link is null)
            {
                var order = request.OrderIndex
                    ?? await _context.DayTasks.CountAsync(dt => dt.VDayId == day.Id, cancellationToken);

                _context.DayTasks.Add(new VDayVTask
                {
                    VDayId = day.Id,
                    VTaskId = request.TaskId,
                    OrderIndex = order
                });
            }
            else if (request.OrderIndex.HasValue)
            {
                link.OrderIndex = request.OrderIndex.Value;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
