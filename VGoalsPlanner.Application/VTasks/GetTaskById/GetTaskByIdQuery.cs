using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Application.VTasks.Common;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Errors;

namespace VGoalsPlanner.Application.VTasks
{
    public sealed record GetTaskByIdQuery(Guid Id) : IQuery<TaskDetailResponse>;

    internal sealed class GetTaskByIdQueryHandler : IQueryHandler<GetTaskByIdQuery, TaskDetailResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetTaskByIdQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<TaskDetailResponse>> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
        {
            var task = await _context.Tasks.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == request.Id && t.UserId == _currentUser.UserId, cancellationToken);

            if (task is null)
            {
                return Result.Failure<TaskDetailResponse>(VTaskErrors.NotFound(request.Id));
            }

            var logs = await _context.TaskLogs.AsNoTracking()
                .Where(l => l.VTaskId == task.Id)
                .OrderBy(l => l.CreatedAt)
                .Select(l => new TaskLogSummaryResponse
                {
                    Id = l.Id,
                    Note = l.Note,
                    Attachments = l.Attachments,
                    TimeSpentMinutes = l.TimeSpentMinutes,
                    OldStatus = l.OldStatus,
                    NewStatus = l.NewStatus,
                    DayId = l.DayId,
                    CreatedByUserId = l.CreatedByUserId,
                    CreatedAt = l.CreatedAt
                })
                .ToListAsync(cancellationToken);

            var schedule = await _context.DayTasks.AsNoTracking()
                .Where(dt => dt.VTaskId == task.Id)
                .Select(dt => new TaskScheduleEntryResponse
                {
                    DayId = dt.VDayId,
                    Date = dt.Day.Date,
                    OrderIndex = dt.OrderIndex
                })
                .OrderBy(x => x.Date)
                .ToListAsync(cancellationToken);

            var attachments = await _context.TaskAttachments.AsNoTracking()
                .Where(a => a.VTaskId == task.Id)
                .OrderBy(a => a.CreatedAt)
                .Select(a => new TaskAttachmentResponse
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    ContentType = a.ContentType,
                    SizeBytes = a.SizeBytes,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return new TaskDetailResponse
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                Priority = task.Priority,
                Type = task.Type,
                Deadline = task.Deadline,
                StartedAt = task.StartedAt,
                CompletedAt = task.CompletedAt,
                EstimatedMinutes = task.EstimatedMinutes,
                CompletedPercent = task.CompletedPercent,
                RecurrenceDays = task.RecurrenceDays,
                OrderIndex = task.OrderIndex,
                GoalId = task.GoalId,
                Tags = task.Tags,
                Links = task.Links.Select(l => new TaskLinkDto(l.Kind, l.Target, l.Alias)).ToList(),
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt,
                Logs = logs,
                Schedule = schedule,
                Attachments = attachments
            };
        }
    }
}
