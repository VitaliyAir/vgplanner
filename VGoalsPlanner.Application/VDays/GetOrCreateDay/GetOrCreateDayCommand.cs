using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Application.VDays.Common;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Entities;

namespace VGoalsPlanner.Application.VDays
{
    public sealed record GetOrCreateDayCommand(DateOnly Date) : ICommand<DayResponse>;

    internal sealed class GetOrCreateDayCommandHandler : ICommandHandler<GetOrCreateDayCommand, DayResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetOrCreateDayCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<DayResponse>> Handle(GetOrCreateDayCommand request, CancellationToken cancellationToken)
        {
            var day = await _context.Days
                .FirstOrDefaultAsync(d => d.UserId == _currentUser.UserId && d.Date == request.Date, cancellationToken);

            if (day is null)
            {
                var now = DateTime.UtcNow;
                day = new VDay
                {
                    Date = request.Date,
                    UserId = _currentUser.UserId,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.Days.Add(day);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return new DayResponse { Date = day.Date, SpecialNote = day.SpecialNote };
        }
    }
}
