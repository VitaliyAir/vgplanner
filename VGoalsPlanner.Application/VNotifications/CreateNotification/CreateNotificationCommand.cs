using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Entities;

namespace VGoalsPlanner.Application.VNotifications
{
    public sealed record CreateNotificationCommand(
        DateOnly Date,
        string Title,
        string? Description,
        DateTime? Deadline) : ICommand<Guid>;

    internal sealed class CreateNotificationCommandValidator : AbstractValidator<CreateNotificationCommand>
    {
        public CreateNotificationCommandValidator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(2000);
        }
    }

    internal sealed class CreateNotificationCommandHandler : ICommandHandler<CreateNotificationCommand, Guid>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public CreateNotificationCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
        {
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

            var notification = new VNotification
            {
                DayId = day.Id,
                UserId = _currentUser.UserId,
                Title = request.Title,
                Description = request.Description,
                Deadline = request.Deadline,
                IsRead = false,
                IsCompleted = false,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync(cancellationToken);

            return notification.Id;
        }
    }
}
