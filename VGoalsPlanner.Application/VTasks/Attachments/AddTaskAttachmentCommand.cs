using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Application.VTasks.Common;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Entities;
using VGoalsPlanner.Domain.Errors;

namespace VGoalsPlanner.Application.VTasks
{
    /// <summary>Загрузка файла во вложения задачи: поток уходит в хранилище как есть, без буферизации в памяти.</summary>
    public sealed record AddTaskAttachmentCommand(
        Guid TaskId,
        string FileName,
        string ContentType,
        Stream Content,
        long? ContentLength) : ICommand<TaskAttachmentResponse>;

    internal sealed class AddTaskAttachmentCommandValidator : AbstractValidator<AddTaskAttachmentCommand>
    {
        public AddTaskAttachmentCommandValidator()
        {
            RuleFor(x => x.TaskId).NotEmpty();
            RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
            RuleFor(x => x.ContentType).NotEmpty().MaximumLength(200);
        }
    }

    internal sealed class AddTaskAttachmentCommandHandler : ICommandHandler<AddTaskAttachmentCommand, TaskAttachmentResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IFileStorage _storage;

        public AddTaskAttachmentCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, IFileStorage storage)
        {
            _context = context;
            _currentUser = currentUser;
            _storage = storage;
        }

        public async Task<Result<TaskAttachmentResponse>> Handle(AddTaskAttachmentCommand request, CancellationToken cancellationToken)
        {
            var taskExists = await _context.Tasks
                .AnyAsync(t => t.Id == request.TaskId && t.UserId == _currentUser.UserId, cancellationToken);

            if (!taskExists)
            {
                return Result.Failure<TaskAttachmentResponse>(VTaskErrors.NotFound(request.TaskId));
            }

            var attachmentId = Guid.NewGuid();
            var storageKey = $"{request.TaskId}/{attachmentId}";
            var counting = new CountingReadStream(request.Content);

            var upload = await _storage.UploadAsync(
                storageKey, request.FileName, counting, request.ContentType, request.ContentLength, cancellationToken);
            if (upload.IsFailure)
            {
                return Result.Failure<TaskAttachmentResponse>(upload.Error);
            }

            var now = DateTime.UtcNow;
            var attachment = new VTaskAttachment
            {
                Id = attachmentId,
                VTaskId = request.TaskId,
                UserId = _currentUser.UserId,
                FileName = request.FileName,
                ContentType = request.ContentType,
                SizeBytes = counting.BytesRead,
                StorageKey = storageKey,
                StorageObjectId = upload.Value,
                CreatedBy = _currentUser.UserId,
                CreatedAt = now,
                UpdatedAt = now
            };

            try
            {
                _context.TaskAttachments.Add(attachment);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                // Строки в БД нет — убираем уже загруженный объект, чтобы не оставлять "сирот" в хранилище.
                await _storage.DeleteAsync(upload.Value, CancellationToken.None);
                throw;
            }

            return new TaskAttachmentResponse
            {
                Id = attachment.Id,
                FileName = attachment.FileName,
                ContentType = attachment.ContentType,
                SizeBytes = attachment.SizeBytes,
                CreatedAt = attachment.CreatedAt
            };
        }
    }
}
