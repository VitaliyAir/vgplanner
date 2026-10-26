using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Domain.Abstractions;

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
            => await StatisticsReader.ReadAsync(_context, _currentUser.UserId, cancellationToken);
    }
}
