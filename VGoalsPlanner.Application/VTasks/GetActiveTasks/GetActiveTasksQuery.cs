using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Application.VTasks.Common;
using VGoalsPlanner.Domain.Abstractions;

namespace VGoalsPlanner.Application.VTasks
{
    /// <summary>
    /// Задачи, отмеченные активными именно сегодня (VDayVTask.IsActive для VDay.Date == сегодня) —
    /// не исторический хвост из прошлых дней, которые никто не деактивировал.
    /// Date — «сегодня» по часам клиента (его локальная дата); без неё берётся дата по UTC.
    /// </summary>
    public sealed record GetActiveTasksQuery(DateOnly? Date = null) : IQuery<IReadOnlyList<ActiveTaskResponse>>;

    internal sealed class GetActiveTasksQueryHandler : IQueryHandler<GetActiveTasksQuery, IReadOnlyList<ActiveTaskResponse>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetActiveTasksQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<IReadOnlyList<ActiveTaskResponse>>> Handle(GetActiveTasksQuery request, CancellationToken cancellationToken)
        {
            var today = request.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);

            var tasks = await (
                from dt in _context.DayTasks.AsNoTracking()
                join d in _context.Days.AsNoTracking() on dt.VDayId equals d.Id
                join t in _context.Tasks.AsNoTracking() on dt.VTaskId equals t.Id
                where dt.IsActive && d.Date == today && d.UserId == _currentUser.UserId
                select new ActiveTaskResponse
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    GoalId = t.GoalId
                }).ToListAsync(cancellationToken);

            return tasks;
        }
    }
}
