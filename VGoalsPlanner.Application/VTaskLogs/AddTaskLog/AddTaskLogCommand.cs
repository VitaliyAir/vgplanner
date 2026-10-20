using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Entities;
using VGoalsPlanner.Domain.Enums;
using VGoalsPlanner.Domain.Errors;

namespace VGoalsPlanner.Application.VTaskLogs
{
    public sealed record AddTaskLogCommand(
        Guid TaskId,
        DateOnly Date,
        string? Note,
        string[] Attachments,
        int? TimeSpentMinutes,
        VTaskStatus? OldStatus,
        VTaskStatus? NewStatus) : ICommand<Guid>;

    internal sealed class AddTaskLogCommandValidator : AbstractValidator<AddTaskLogCommand>
    {
        public AddTaskLogCommandValidator()
        {
            RuleFor(x => x.TaskId).NotEmpty();
            RuleFor(x => x.Note).MaximumLength(20000);
            RuleFor(x => x.TimeSpentMinutes).GreaterThanOrEqualTo(0).When(x => x.TimeSpentMinutes.HasValue);
            RuleFor(x => x.OldStatus).IsInEnum().When(x => x.OldStatus.HasValue);
            RuleFor(x => x.NewStatus).IsInEnum().When(x => x.NewStatus.HasValue);
            RuleFor(x => x.Attachments).Must(a => a.Length <= 20).WithMessage("Не более 20 вложений");
            RuleForEach(x => x.Attachments).MaximumLength(500);
        }
    }

    internal sealed class AddTaskLogCommandHandler : ICommandHandler<AddTaskLogCommand, Guid>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public AddTaskLogCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(AddTaskLogCommand request, CancellationToken cancellationToken)
        {
            var taskExists = await _context.Tasks
                .AnyAsync(t => t.Id == request.TaskId && t.UserId == _currentUser.UserId, cancellationToken);

            if (!taskExists)
            {
                return Result.Failure<Guid>(VTaskErrors.NotFound(request.TaskId));
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

            var log = new VTaskLog
            {
                VTaskId = request.TaskId,
                DayId = day.Id,
                CreatedByUserId = _currentUser.UserId,
                Note = request.Note,
                Attachments = request.Attachments ?? Array.Empty<string>(),
                TimeSpentMinutes = request.TimeSpentMinutes,
                OldStatus = request.OldStatus,
                NewStatus = request.NewStatus,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.TaskLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);

            return log.Id;
        }
    }
}
