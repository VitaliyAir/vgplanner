using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Entities;

namespace VGoalsPlanner.Application.VDays
{
    public sealed record UpdateDayNoteCommand(DateOnly Date, string? SpecialNote) : ICommand;

    internal sealed class UpdateDayNoteCommandValidator : AbstractValidator<UpdateDayNoteCommand>
    {
        public UpdateDayNoteCommandValidator()
        {
            RuleFor(x => x.SpecialNote).MaximumLength(500);
        }
    }

    internal sealed class UpdateDayNoteCommandHandler : ICommandHandler<UpdateDayNoteCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public UpdateDayNoteCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result> Handle(UpdateDayNoteCommand request, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;

            var day = await _context.Days
                .FirstOrDefaultAsync(d => d.UserId == _currentUser.UserId && d.Date == request.Date, cancellationToken);

            if (day is null)
            {
                day = new VDay
                {
                    Date = request.Date,
                    UserId = _currentUser.UserId,
                    CreatedAt = now
                };
                _context.Days.Add(day);
            }

            day.SpecialNote = request.SpecialNote;
            day.UpdatedAt = now;

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
