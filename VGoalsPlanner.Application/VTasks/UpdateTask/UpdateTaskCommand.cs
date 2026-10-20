using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Application.VTasks.Common;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Enums;
using VGoalsPlanner.Domain.Errors;

namespace VGoalsPlanner.Application.VTasks
{
    public sealed record UpdateTaskCommand(
        Guid Id,
        string Title,
        string? Description,
        DateTime? Deadline,
        Priority Priority,
        VTaskType Type,
        int? EstimatedMinutes,
        string[] Tags,
        int RecurrenceDays,
        TaskLinkDto[]? Links) : ICommand;

    internal sealed class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
    {
        public UpdateTaskCommandValidator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(2000);
            RuleFor(x => x.Priority).IsInEnum();
            RuleFor(x => x.Type).IsInEnum();
            RuleFor(x => x.RecurrenceDays).InclusiveBetween(0, VGoalsPlanner.Domain.Enums.RecurrenceDays.All);
            RuleFor(x => x.EstimatedMinutes).GreaterThanOrEqualTo(0).When(x => x.EstimatedMinutes.HasValue);
            RuleFor(x => x.Tags).Must(t => t.Length <= 20).WithMessage("Не более 20 тегов");
            RuleForEach(x => x.Tags).MaximumLength(50);
            RuleFor(x => x.Links).Must(l => l!.Length <= TaskLinks.MaxCount).When(x => x.Links is not null).WithMessage("Не более 30 ссылок");
            RuleForEach(x => x.Links).SetValidator(new TaskLinkDtoValidator()).When(x => x.Links is not null);
        }
    }

    internal sealed class UpdateTaskCommandHandler : ICommandHandler<UpdateTaskCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public UpdateTaskCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == request.Id && t.UserId == _currentUser.UserId, cancellationToken);

            if (task is null)
            {
                return Result.Failure(VTaskErrors.NotFound(request.Id));
            }

            task.Title = request.Title;
            task.Description = request.Description;
            task.Deadline = request.Deadline;
            task.Priority = request.Priority;
            task.Type = request.Type;
            task.EstimatedMinutes = request.EstimatedMinutes;
            task.Tags = request.Tags ?? Array.Empty<string>();
            if (request.Links is not null)
            {
                task.Links = TaskLinks.Normalize(request.Links);
            }
            task.UpdatedAt = DateTime.UtcNow;

            // Правило повторения: при смене дней (или когда задача только стала регулярной)
            // отметка отодвигается на "вчера", чтобы новое правило применилось начиная с сегодня,
            // но более старые дни не переписываются; если отметка старше — оставляем её,
            // чтобы не потерять пропущенные дни. Обычное сохранение правку не трогает.
            var isRegular = request.Type == VTaskType.Repeatable && request.RecurrenceDays != 0;
            if (isRegular)
            {
                var ruleChanged = task.RecurrenceDays != request.RecurrenceDays || task.RecurrenceGeneratedUntil is null;
                if (ruleChanged)
                {
                    var yesterday = DateOnly.FromDateTime(task.UpdatedAt).AddDays(-1);
                    task.RecurrenceGeneratedUntil = task.RecurrenceGeneratedUntil is { } previous && previous < yesterday
                        ? previous
                        : yesterday;
                }
                task.RecurrenceDays = request.RecurrenceDays;
            }
            else
            {
                task.RecurrenceDays = 0;
                task.RecurrenceGeneratedUntil = null;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
