using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Domain.Abstractions;

namespace VGoalsPlanner.Application.Statistics
{
    /// <summary>
    /// Снимки статистики по дням за окно в Days дней, заканчивая EndDate включительно; новые дни сверху.
    /// </summary>
    public sealed record GetStatisticsHistoryQuery(DateOnly EndDate, int Days = 30) : IQuery<IReadOnlyList<StatisticsHistoryRow>>;

    public sealed class StatisticsHistoryRow
    {
        public DateOnly Date { get; init; }
        public required GoalStatistics Goals { get; init; }
        public required StatusStatistics Tasks { get; init; }
    }

    internal sealed class GetStatisticsHistoryQueryValidator : AbstractValidator<GetStatisticsHistoryQuery>
    {
        public GetStatisticsHistoryQueryValidator()
        {
            RuleFor(x => x.Days).InclusiveBetween(1, 366);
        }
    }

    internal sealed class GetStatisticsHistoryQueryHandler : IQueryHandler<GetStatisticsHistoryQuery, IReadOnlyList<StatisticsHistoryRow>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetStatisticsHistoryQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<IReadOnlyList<StatisticsHistoryRow>>> Handle(
            GetStatisticsHistoryQuery request,
            CancellationToken cancellationToken)
        {
            var startDate = request.EndDate.AddDays(-(request.Days - 1));

            var snapshots = await _context.DayStatistics.AsNoTracking()
                .Where(s => s.UserId == _currentUser.UserId && s.Date >= startDate && s.Date <= request.EndDate)
                .OrderByDescending(s => s.Date)
                .ToListAsync(cancellationToken);

            return snapshots
                .Select(s =>
                {
                    var stats = StatisticsReader.FromSnapshot(s);
                    return new StatisticsHistoryRow { Date = s.Date, Goals = stats.Goals, Tasks = stats.Tasks };
                })
                .ToList();
        }
    }
}
