using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Application.VDays.Common;
using VGoalsPlanner.Domain.Abstractions;

namespace VGoalsPlanner.Application.VDays
{
    /// <summary>Расписание на 7 дней подряд, начиная с StartDate.</summary>
    public sealed record GetWeekScheduleQuery(DateOnly StartDate) : IQuery<IReadOnlyList<DayScheduleResponse>>;

    internal sealed class GetWeekScheduleQueryHandler
        : IQueryHandler<GetWeekScheduleQuery, IReadOnlyList<DayScheduleResponse>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetWeekScheduleQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<IReadOnlyList<DayScheduleResponse>>> Handle(
            GetWeekScheduleQuery request,
            CancellationToken cancellationToken)
        {
            var dates = Enumerable.Range(0, 7)
                .Select(offset => request.StartDate.AddDays(offset))
                .ToList();

            var schedule = await DayScheduleAssembler.BuildAsync(
                _context, _currentUser.UserId, dates, cancellationToken);

            return schedule;
        }
    }
}
