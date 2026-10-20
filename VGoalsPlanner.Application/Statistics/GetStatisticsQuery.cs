using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Enums;

namespace VGoalsPlanner.Application.Statistics
{
    /// <summary>Количество целей и задач пользователя: всего и по каждому статусу.</summary>
    public sealed record GetStatisticsQuery : IQuery<StatisticsResponse>;

    internal sealed class GetStatisticsQueryHandler : IQueryHandler<GetStatisticsQuery, StatisticsResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetStatisticsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<StatisticsResponse>> Handle(GetStatisticsQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId;

            var goalsTotal = await _context.Goals.AsNoTracking()
                .CountAsync(g => g.UserId == userId, cancellationToken);

            var taskCounts = await _context.Tasks.AsNoTracking()
                .Where(t => t.UserId == userId)
                .GroupBy(t => t.Status)
                .Select(t => new { Status = t.Key, Count = t.Count() })
                .ToListAsync(cancellationToken);

            return new StatisticsResponse
            {
                Goals = new GoalStatistics { Total = goalsTotal },
                Tasks = Build(Enum.GetValues<VTaskStatus>(), taskCounts.ToDictionary(x => x.Status, x => x.Count))
            };
        }

        // Все статусы по порядку enum (с нулём, если записей нет) + общий итог.
        private static StatusStatistics Build<TStatus>(IEnumerable<TStatus> statuses, Dictionary<TStatus, int> counts)
            where TStatus : struct, Enum
        {
            var byStatus = statuses
                .Select(s => new StatusCount(Convert.ToInt32(s), counts.GetValueOrDefault(s)))
                .ToList();

            return new StatusStatistics { Total = byStatus.Sum(c => c.Count), ByStatus = byStatus };
        }
    }
}
