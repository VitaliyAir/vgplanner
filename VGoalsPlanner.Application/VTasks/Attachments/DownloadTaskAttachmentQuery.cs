using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Errors;

namespace VGoalsPlanner.Application.VTasks
{
    public sealed record DownloadTaskAttachmentQuery(Guid TaskId, Guid AttachmentId) : IQuery<TaskAttachmentDownload>;

    /// <summary>Открытый поток файла; закрывает его тот, кто отдаёт ответ клиенту.</summary>
    public sealed record TaskAttachmentDownload(Stream Content, string FileName, string ContentType);

    internal sealed class DownloadTaskAttachmentQueryHandler : IQueryHandler<DownloadTaskAttachmentQuery, TaskAttachmentDownload>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IFileStorage _storage;

        public DownloadTaskAttachmentQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser, IFileStorage storage)
        {
            _context = context;
            _currentUser = currentUser;
            _storage = storage;
        }

        public async Task<Result<TaskAttachmentDownload>> Handle(DownloadTaskAttachmentQuery request, CancellationToken cancellationToken)
        {
            var attachment = await _context.TaskAttachments.AsNoTracking()
                .FirstOrDefaultAsync(
                    a => a.Id == request.AttachmentId && a.VTaskId == request.TaskId && a.UserId == _currentUser.UserId,
                    cancellationToken);

            if (attachment is null)
            {
                return Result.Failure<TaskAttachmentDownload>(VTaskErrors.AttachmentNotFound(request.AttachmentId));
            }

            var stream = await _storage.OpenReadAsync(attachment.StorageObjectId, cancellationToken);
            if (stream.IsFailure)
            {
                return Result.Failure<TaskAttachmentDownload>(stream.Error);
            }

            return new TaskAttachmentDownload(stream.Value, attachment.FileName, attachment.ContentType);
        }
    }
}
