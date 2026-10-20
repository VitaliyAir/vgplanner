using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Application.VTasks.Common;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Entities;
using VGoalsPlanner.Domain.Enums;
using VGoalsPlanner.Domain.Errors;

namespace VGoalsPlanner.Application.VTasks
{
    public sealed record CreateTaskCommand(
        Guid GoalId,
        string Title,
        string? Description,
        DateTime? Deadline,
        Priority Priority,
        VTaskType Type,
        int? EstimatedMinutes,
        string[] Tags,
        int RecurrenceDays,
        TaskLinkDto[] Links) : ICommand<Guid>;

    internal sealed class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
    {
        public CreateTaskCommandValidator()
        {
            RuleFor(x => x.GoalId).NotEmpty();
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(2000);
            RuleFor(x => x.Priority).IsInEnum();
            RuleFor(x => x.Type).IsInEnum();
            RuleFor(x => x.RecurrenceDays).InclusiveBetween(0, VGoalsPlanner.Domain.Enums.RecurrenceDays.All);
            RuleFor(x => x.EstimatedMinutes).GreaterThanOrEqualTo(0).When(x => x.EstimatedMinutes.HasValue);
            RuleFor(x => x.Tags).Must(t => t.Length <= 20).WithMessage("Не более 20 тегов");
            RuleForEach(x => x.Tags).MaximumLength(50);
            RuleFor(x => x.Links).Must(l => l.Length <= TaskLinks.MaxCount).WithMessage("Не более 30 ссылок");
            RuleForEach(x => x.Links).SetValidator(new TaskLinkDtoValidator());
        }
    }

    internal sealed class CreateTaskCommandHandler : ICommandHandler<CreateTaskCommand, Guid>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public CreateTaskCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
        {
            var goalExists = await _context.Goals
                .AnyAsync(g => g.Id == request.GoalId && g.UserId == _currentUser.UserId, cancellationToken);

            if (!goalExists)
            {
                return Result.Failure<Guid>(VTaskErrors.GoalNotFound);
            }

            var siblingsCount = await _context.Tasks
                .CountAsync(t => t.GoalId == request.GoalId && t.UserId == _currentUser.UserId, cancellationToken);

            var now = DateTime.UtcNow;

            // Дни недели имеют смысл только у регулярной задачи; первое повторение — сегодня.
            var isRegular = request.Type == VTaskType.Repeatable && request.RecurrenceDays != 0;

            var task = new VTask
            {
                GoalId = request.GoalId,
                Title = request.Title,
                Description = request.Description,
                Deadline = request.Deadline,
                Priority = request.Priority,
                Type = request.Type,
                EstimatedMinutes = request.EstimatedMinutes,
                Tags = request.Tags ?? Array.Empty<string>(),
                Links = TaskLinks.Normalize(request.Links),
                RecurrenceDays = isRegular ? request.RecurrenceDays : 0,
                RecurrenceGeneratedUntil = isRegular ? DateOnly.FromDateTime(now).AddDays(-1) : null,
                OrderIndex = siblingsCount,
                UserId = _currentUser.UserId,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync(cancellationToken);

            return task.Id;
        }
    }
}
