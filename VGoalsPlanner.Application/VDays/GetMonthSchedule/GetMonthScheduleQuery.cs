using FluentValidation;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Application.VDays.Common;
using VGoalsPlanner.Domain.Abstractions;

namespace VGoalsPlanner.Application.VDays
{
    /// <summary>Расписание на все дни календарного месяца (Year/Month).</summary>
    public sealed record GetMonthScheduleQuery(int Year, int Month) : IQuery<IReadOnlyList<DayScheduleResponse>>;

    internal sealed class GetMonthScheduleQueryValidator : AbstractValidator<GetMonthScheduleQuery>
    {
        public GetMonthScheduleQueryValidator()
        {
            RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
            RuleFor(x => x.Month).InclusiveBetween(1, 12);
        }
    }

    internal sealed class GetMonthScheduleQueryHandler
        : IQueryHandler<GetMonthScheduleQuery, IReadOnlyList<DayScheduleResponse>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetMonthScheduleQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<IReadOnlyList<DayScheduleResponse>>> Handle(
            GetMonthScheduleQuery request,
            CancellationToken cancellationToken)
        {
            var daysInMonth = DateTime.DaysInMonth(request.Year, request.Month);

            var dates = Enumerable.Range(1, daysInMonth)
                .Select(day => new DateOnly(request.Year, request.Month, day))
                .ToList();

            var schedule = await DayScheduleAssembler.BuildAsync(
                _context, _currentUser.UserId, dates, cancellationToken);

            return schedule;
        }
    }
}
