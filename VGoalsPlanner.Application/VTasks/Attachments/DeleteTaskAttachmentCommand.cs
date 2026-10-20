using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Errors;

namespace VGoalsPlanner.Application.VTasks
{
    public sealed record DeleteTaskAttachmentCommand(Guid TaskId, Guid AttachmentId) : ICommand;

    internal sealed class DeleteTaskAttachmentCommandHandler : ICommandHandler<DeleteTaskAttachmentCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IFileStorage _storage;

        public DeleteTaskAttachmentCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, IFileStorage storage)
        {
            _context = context;
            _currentUser = currentUser;
            _storage = storage;
        }

        public async Task<Result> Handle(DeleteTaskAttachmentCommand request, CancellationToken cancellationToken)
        {
            var attachment = await _context.TaskAttachments
                .FirstOrDefaultAsync(
                    a => a.Id == request.AttachmentId && a.VTaskId == request.TaskId && a.UserId == _currentUser.UserId,
                    cancellationToken);

            if (attachment is null)
            {
                return Result.Failure(VTaskErrors.AttachmentNotFound(request.AttachmentId));
            }

            // Сначала хранилище: если оно недоступно, строка остаётся и удаление можно повторить.
            var deleted = await _storage.DeleteAsync(attachment.StorageObjectId, cancellationToken);
            if (deleted.IsFailure)
            {
                return deleted;
            }

            _context.TaskAttachments.Remove(attachment);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
