using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Errors;

namespace VGoalsPlanner.Application.VNotifications
{
    public sealed record MarkNotificationAsReadCommand(Guid Id) : ICommand;

    internal sealed class MarkNotificationAsReadCommandHandler : ICommandHandler<MarkNotificationAsReadCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public MarkNotificationAsReadCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == request.Id && n.UserId == _currentUser.UserId, cancellationToken);

            if (notification is null)
            {
                return Result.Failure(VNotificationErrors.NotFound(request.Id));
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return Result.Success();
        }
    }
}
