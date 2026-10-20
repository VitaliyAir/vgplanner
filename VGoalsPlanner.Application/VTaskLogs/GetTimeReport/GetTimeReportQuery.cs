using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Application.VTaskLogs.Common;
using VGoalsPlanner.Domain.Abstractions;

namespace VGoalsPlanner.Application.VTaskLogs
{
    /// <summary>Статистика по потраченному времени за период [From..To].</summary>
    public sealed record GetTimeReportQuery(DateOnly From, DateOnly To) : IQuery<TimeReportResponse>;

    internal sealed class GetTimeReportQueryValidator : AbstractValidator<GetTimeReportQuery>
    {
        public GetTimeReportQueryValidator()
        {
            RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From)
                .WithMessage("Дата окончания периода не может быть раньше даты начала");
        }
    }

    internal sealed class GetTimeReportQueryHandler : IQueryHandler<GetTimeReportQuery, TimeReportResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetTimeReportQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<TimeReportResponse>> Handle(GetTimeReportQuery request, CancellationToken cancellationToken)
        {
            var rows = await (
                from l in _context.TaskLogs.AsNoTracking()
                join d in _context.Days on l.DayId equals d.Id
                join t in _context.Tasks on l.VTaskId equals t.Id
                where d.UserId == _currentUser.UserId
                    && d.Date >= request.From
                    && d.Date <= request.To
                    && l.TimeSpentMinutes != null
                select new
                {
                    d.Date,
                    TaskId = t.Id,
                    TaskTitle = t.Title,
                    Minutes = l.TimeSpentMinutes!.Value
                }).ToListAsync(cancellationToken);

            var byDay = rows
                .GroupBy(r => r.Date)
                .Select(g => new TimeReportDayEntry { Date = g.Key, Minutes = g.Sum(r => r.Minutes) })
                .OrderBy(x => x.Date)
                .ToList();

            var byTask = rows
                .GroupBy(r => new { r.TaskId, r.TaskTitle })
                .Select(g => new TimeReportTaskEntry
                {
                    TaskId = g.Key.TaskId,
                    TaskTitle = g.Key.TaskTitle,
                    Minutes = g.Sum(r => r.Minutes)
                })
                .OrderByDescending(x => x.Minutes)
                .ThenBy(x => x.TaskTitle)
                .ToList();

            return new TimeReportResponse
            {
                From = request.From,
                To = request.To,
                TotalMinutes = rows.Sum(r => r.Minutes),
                ByDay = byDay,
                ByTask = byTask
            };
        }
    }
}
