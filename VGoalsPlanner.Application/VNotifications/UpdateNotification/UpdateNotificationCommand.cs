using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Errors;

namespace VGoalsPlanner.Application.VNotifications
{
    public sealed record UpdateNotificationCommand(
        Guid Id,
        string Title,
        string? Description,
        DateTime? Deadline) : ICommand;

    internal sealed class UpdateNotificationCommandValidator : AbstractValidator<UpdateNotificationCommand>
    {
        public UpdateNotificationCommandValidator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(2000);
        }
    }

    internal sealed class UpdateNotificationCommandHandler : ICommandHandler<UpdateNotificationCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public UpdateNotificationCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result> Handle(UpdateNotificationCommand request, CancellationToken cancellationToken)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == request.Id && n.UserId == _currentUser.UserId, cancellationToken);

            if (notification is null)
            {
                return Result.Failure(VNotificationErrors.NotFound(request.Id));
            }

            notification.Title = request.Title;
            notification.Description = request.Description;
            notification.Deadline = request.Deadline;
            notification.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
