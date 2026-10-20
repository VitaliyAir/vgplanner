using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Application.VTaskLogs.Common;
using VGoalsPlanner.Domain.Abstractions;

namespace VGoalsPlanner.Application.VTaskLogs
{
    /// <summary>Все записи лога за день, сгруппированные по задачам.</summary>
    public sealed record GetDailyReportQuery(DateOnly Date) : IQuery<DailyReportResponse>;

    internal sealed class GetDailyReportQueryHandler : IQueryHandler<GetDailyReportQuery, DailyReportResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetDailyReportQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<DailyReportResponse>> Handle(GetDailyReportQuery request, CancellationToken cancellationToken)
        {
            var rows = await (
                from l in _context.TaskLogs.AsNoTracking()
                join d in _context.Days on l.DayId equals d.Id
                join t in _context.Tasks on l.VTaskId equals t.Id
                where d.UserId == _currentUser.UserId && d.Date == request.Date
                orderby l.CreatedAt
                select new
                {
                    TaskId = t.Id,
                    TaskTitle = t.Title,
                    l.Id,
                    l.DayId,
                    DayDate = d.Date,
                    l.Note,
                    l.Attachments,
                    l.TimeSpentMinutes,
                    l.OldStatus,
                    l.NewStatus,
                    l.CreatedByUserId,
                    l.CreatedAt,
                    l.UpdatedAt
                }).ToListAsync(cancellationToken);

            var groups = rows
                .GroupBy(r => new { r.TaskId, r.TaskTitle })
                .Select(g => new DailyReportTaskGroup
                {
                    TaskId = g.Key.TaskId,
                    TaskTitle = g.Key.TaskTitle,
                    TotalTimeSpentMinutes = g.Sum(r => r.TimeSpentMinutes ?? 0),
                    Entries = g.Select(r => new TaskLogResponse
                    {
                        Id = r.Id,
                        TaskId = r.TaskId,
                        DayId = r.DayId,
                        Date = r.DayDate,
                        Note = r.Note,
                        Attachments = r.Attachments,
                        TimeSpentMinutes = r.TimeSpentMinutes,
                        OldStatus = r.OldStatus,
                        NewStatus = r.NewStatus,
                        CreatedByUserId = r.CreatedByUserId,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt
                    }).ToList()
                })
                .OrderByDescending(x => x.TotalTimeSpentMinutes)
                .ThenBy(x => x.TaskTitle)
                .ToList();

            return new DailyReportResponse { Date = request.Date, Tasks = groups };
        }
    }
}
