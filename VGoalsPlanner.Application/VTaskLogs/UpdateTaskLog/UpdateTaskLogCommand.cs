using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Errors;

namespace VGoalsPlanner.Application.VTaskLogs
{
    public sealed record UpdateTaskLogCommand(
        Guid Id,
        string? Note,
        string[] Attachments) : ICommand;

    internal sealed class UpdateTaskLogCommandValidator : AbstractValidator<UpdateTaskLogCommand>
    {
        public UpdateTaskLogCommandValidator()
        {
            RuleFor(x => x.Note).MaximumLength(20000);
            RuleFor(x => x.Attachments).Must(a => a.Length <= 20).WithMessage("Не более 20 вложений");
            RuleForEach(x => x.Attachments).MaximumLength(500);
        }
    }

    internal sealed class UpdateTaskLogCommandHandler : ICommandHandler<UpdateTaskLogCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public UpdateTaskLogCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result> Handle(UpdateTaskLogCommand request, CancellationToken cancellationToken)
        {
            var log = await _context.TaskLogs
                .FirstOrDefaultAsync(l => l.Id == request.Id && l.CreatedByUserId == _currentUser.UserId, cancellationToken);

            if (log is null)
            {
                return Result.Failure(VTaskLogErrors.NotFound(request.Id));
            }

            log.Note = request.Note;
            log.Attachments = request.Attachments ?? Array.Empty<string>();
            log.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
