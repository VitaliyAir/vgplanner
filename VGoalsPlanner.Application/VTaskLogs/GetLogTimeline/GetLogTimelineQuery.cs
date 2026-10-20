using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Application.VTaskLogs.Common;
using VGoalsPlanner.Domain.Abstractions;

namespace VGoalsPlanner.Application.VTaskLogs
{
    /// <summary>
    /// Лента записей лога за фиксированное календарное окно (Days дней,
    /// заканчивая EndDate включительно), сгруппированная по дням - дни по
    /// убыванию даты, записи внутри дня по возрастанию времени создания.
    /// Дни без заметки и без единой записи лога в ответ не попадают.
    /// </summary>
    public sealed record GetLogTimelineQuery(DateOnly EndDate, int Days = 7) : IQuery<IReadOnlyList<TimelineDayResponse>>;

    internal sealed class GetLogTimelineQueryValidator : AbstractValidator<GetLogTimelineQuery>
    {
        public GetLogTimelineQueryValidator()
        {
            RuleFor(x => x.Days).InclusiveBetween(1, 31);
        }
    }

    internal sealed class GetLogTimelineQueryHandler : IQueryHandler<GetLogTimelineQuery, IReadOnlyList<TimelineDayResponse>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetLogTimelineQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<IReadOnlyList<TimelineDayResponse>>> Handle(
            GetLogTimelineQuery request,
            CancellationToken cancellationToken)
        {
            var startDate = request.EndDate.AddDays(-(request.Days - 1));

            var days = await _context.Days.AsNoTracking()
                .Where(d => d.UserId == _currentUser.UserId && d.Date >= startDate && d.Date <= request.EndDate)
                .ToListAsync(cancellationToken);

            var dayIds = days.Select(d => d.Id).ToList();

            var logs = dayIds.Count == 0
                ? new()
                : await (
                    from l in _context.TaskLogs.AsNoTracking()
                    join t in _context.Tasks on l.VTaskId equals t.Id
                    where dayIds.Contains(l.DayId)
                    orderby l.CreatedAt
                    select new
                    {
                        l.DayId,
                        l.Id,
                        TaskId = t.Id,
                        TaskTitle = t.Title,
                        l.Note,
                        l.Attachments,
                        l.TimeSpentMinutes,
                        l.OldStatus,
                        l.NewStatus,
                        l.CreatedAt,
                        l.UpdatedAt
                    }).ToListAsync(cancellationToken);

            var logsByDayId = logs
                .GroupBy(l => l.DayId)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<TimelineLogEntryResponse>)g
                        .Select(l => new TimelineLogEntryResponse
                        {
                            Id = l.Id,
                            TaskId = l.TaskId,
                            TaskTitle = l.TaskTitle,
                            Note = l.Note,
                            Attachments = l.Attachments,
                            TimeSpentMinutes = l.TimeSpentMinutes,
                            OldStatus = l.OldStatus,
                            NewStatus = l.NewStatus,
                            CreatedAt = l.CreatedAt,
                            UpdatedAt = l.UpdatedAt
                        })
                        .ToList());

            var result = days
                .Where(d => !string.IsNullOrEmpty(d.SpecialNote) || logsByDayId.ContainsKey(d.Id))
                .OrderByDescending(d => d.Date)
                .Select(d => new TimelineDayResponse
                {
                    Date = d.Date,
                    SpecialNote = d.SpecialNote,
                    Logs = logsByDayId.TryGetValue(d.Id, out var list) ? list : Array.Empty<TimelineLogEntryResponse>()
                })
                .ToList();

            return result;
        }
    }
}
