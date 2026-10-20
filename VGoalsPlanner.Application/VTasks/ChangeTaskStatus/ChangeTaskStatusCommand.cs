using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Application.VDays.Common;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Enums;
using VGoalsPlanner.Domain.Errors;

namespace VGoalsPlanner.Application.VTasks
{
    public sealed record ChangeTaskStatusCommand(Guid Id, VTaskStatus NewStatus) : ICommand;

    internal sealed class ChangeTaskStatusCommandValidator : AbstractValidator<ChangeTaskStatusCommand>
    {
        public ChangeTaskStatusCommandValidator()
        {
            RuleFor(x => x.NewStatus).IsInEnum();
        }
    }

    internal sealed class ChangeTaskStatusCommandHandler : ICommandHandler<ChangeTaskStatusCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public ChangeTaskStatusCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result> Handle(ChangeTaskStatusCommand request, CancellationToken cancellationToken)
        {
            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == request.Id && t.UserId == _currentUser.UserId, cancellationToken);

            if (task is null)
            {
                return Result.Failure(VTaskErrors.NotFound(request.Id));
            }

            var now = DateTime.UtcNow;
            var leavingCompleted = task.Status == VTaskStatus.Completed && request.NewStatus != VTaskStatus.Completed;

            // Автоматическая установка меток времени по факту смены статуса.
            if (request.NewStatus is VTaskStatus.InProgress or VTaskStatus.Completed && task.StartedAt is null)
            {
                task.StartedAt = now;
            }

            if (request.NewStatus == VTaskStatus.Completed)
            {
                task.CompletedAt = now;
                task.CompletedPercent = 100;
            }
            else if (leavingCompleted)
            {
                task.CompletedAt = null;
            }

            var wasScheduling = RecurringTaskMaterializer.IsActiveStatus(task.Status);
            var willSchedule = RecurringTaskMaterializer.IsActiveStatus(request.NewStatus);
            var isRegular = task.Type == VTaskType.Repeatable && task.RecurrenceDays != 0;

            task.Status = request.NewStatus;
            task.UpdatedAt = now;

            if (isRegular && wasScheduling && !willSchedule)
            {
                // Завершена / на паузе / отменена: убираем сегодняшние и будущие повторения,
                // кроме дней, где уже есть лог (история).
                var today = DateOnly.FromDateTime(now);
                var upcoming = await _context.DayTasks
                    .Where(dt => dt.VTaskId == task.Id
                        && dt.Day.Date >= today
                        && !_context.TaskLogs.Any(l => l.DayId == dt.VDayId && l.VTaskId == dt.VTaskId))
                    .ToListAsync(cancellationToken);
                _context.DayTasks.RemoveRange(upcoming);
            }
            else if (isRegular && !wasScheduling && willSchedule)
            {
                // Вернулась в работу: сегодняшнее повторение создаётся заново, пропущенные дни не доливаются.
                task.RecurrenceGeneratedUntil = DateOnly.FromDateTime(now).AddDays(-1);
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
