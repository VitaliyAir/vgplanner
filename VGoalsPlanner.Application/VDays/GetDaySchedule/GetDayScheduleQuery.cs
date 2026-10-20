using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Application.VDays.Common;
using VGoalsPlanner.Domain.Abstractions;

namespace VGoalsPlanner.Application.VDays
{
    public sealed record GetDayScheduleQuery(DateOnly Date) : IQuery<DayScheduleResponse>;

    internal sealed class GetDayScheduleQueryHandler : IQueryHandler<GetDayScheduleQuery, DayScheduleResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetDayScheduleQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<DayScheduleResponse>> Handle(GetDayScheduleQuery request, CancellationToken cancellationToken)
        {
            var days = await DayScheduleAssembler.BuildAsync(
                _context,
                _currentUser.UserId,
                new[] { request.Date },
                cancellationToken);

            return days[0];
        }
    }
}
