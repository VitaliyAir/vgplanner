using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Application.VNotifications.Common;
using VGoalsPlanner.Domain.Abstractions;

namespace VGoalsPlanner.Application.VNotifications
{
    public sealed record GetDayNotificationsQuery(DateOnly Date) : IQuery<IReadOnlyList<NotificationResponse>>;

    internal sealed class GetDayNotificationsQueryHandler
        : IQueryHandler<GetDayNotificationsQuery, IReadOnlyList<NotificationResponse>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetDayNotificationsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<IReadOnlyList<NotificationResponse>>> Handle(
            GetDayNotificationsQuery request,
            CancellationToken cancellationToken)
        {
            var notifications = await (
                from n in _context.Notifications.AsNoTracking()
                join d in _context.Days on n.DayId equals d.Id
                where n.UserId == _currentUser.UserId && d.Date == request.Date
                orderby n.CreatedAt
                select new NotificationResponse
                {
                    Id = n.Id,
                    DayId = n.DayId,
                    Date = d.Date,
                    Title = n.Title,
                    Description = n.Description,
                    Deadline = n.Deadline,
                    IsRead = n.IsRead,
                    IsCompleted = n.IsCompleted,
                    CreatedAt = n.CreatedAt,
                    UpdatedAt = n.UpdatedAt
                }).ToListAsync(cancellationToken);

            return notifications;
        }
    }
}
