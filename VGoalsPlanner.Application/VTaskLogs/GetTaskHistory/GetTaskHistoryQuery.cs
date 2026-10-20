using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Application.VTaskLogs.Common;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Errors;

namespace VGoalsPlanner.Application.VTaskLogs
{
    public sealed record GetTaskHistoryQuery(Guid TaskId) : IQuery<IReadOnlyList<TaskLogResponse>>;

    internal sealed class GetTaskHistoryQueryHandler
        : IQueryHandler<GetTaskHistoryQuery, IReadOnlyList<TaskLogResponse>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetTaskHistoryQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<IReadOnlyList<TaskLogResponse>>> Handle(
            GetTaskHistoryQuery request,
            CancellationToken cancellationToken)
        {
            var taskExists = await _context.Tasks
                .AnyAsync(t => t.Id == request.TaskId && t.UserId == _currentUser.UserId, cancellationToken);

            if (!taskExists)
            {
                return Result.Failure<IReadOnlyList<TaskLogResponse>>(VTaskErrors.NotFound(request.TaskId));
            }

            var logs = await (
                from l in _context.TaskLogs.AsNoTracking()
                join d in _context.Days on l.DayId equals d.Id
                where l.VTaskId == request.TaskId
                orderby l.CreatedAt
                select new TaskLogResponse
                {
                    Id = l.Id,
                    TaskId = l.VTaskId,
                    DayId = l.DayId,
                    Date = d.Date,
                    Note = l.Note,
                    Attachments = l.Attachments,
                    TimeSpentMinutes = l.TimeSpentMinutes,
                    OldStatus = l.OldStatus,
                    NewStatus = l.NewStatus,
                    CreatedByUserId = l.CreatedByUserId,
                    CreatedAt = l.CreatedAt,
                    UpdatedAt = l.UpdatedAt
                }).ToListAsync(cancellationToken);

            return logs;
        }
    }
}
