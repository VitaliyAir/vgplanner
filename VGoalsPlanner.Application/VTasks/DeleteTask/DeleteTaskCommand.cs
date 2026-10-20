using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Errors;

namespace VGoalsPlanner.Application.VTasks
{
    public sealed record DeleteTaskCommand(Guid Id) : ICommand;

    internal sealed class DeleteTaskCommandHandler : ICommandHandler<DeleteTaskCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public DeleteTaskCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == request.Id && t.UserId == _currentUser.UserId, cancellationToken);

            if (task is null)
            {
                return Result.Failure(VTaskErrors.NotFound(request.Id));
            }

            // Мягкое удаление. Логи (VTaskLog) намеренно не трогаем — история выполнения
            // остаётся доступной по VTaskId (GetTaskHistory).
            task.IsDeleted = true;
            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
