using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Errors;

namespace VGoalsPlanner.Application.VNotifications
{
    public sealed record DeleteNotificationCommand(Guid Id) : ICommand;

    internal sealed class DeleteNotificationCommandHandler : ICommandHandler<DeleteNotificationCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public DeleteNotificationCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result> Handle(DeleteNotificationCommand request, CancellationToken cancellationToken)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == request.Id && n.UserId == _currentUser.UserId, cancellationToken);

            if (notification is null)
            {
                return Result.Failure(VNotificationErrors.NotFound(request.Id));
            }

            // Полное (жёсткое) удаление — у заметок нет мягкого удаления.
            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
