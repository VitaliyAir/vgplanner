using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Application.VTasks.Common;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Enums;

namespace VGoalsPlanner.Application.VTasks
{
    /// <summary>
    /// Полный список задач пользователя (без пагинации) с независимыми,
    /// комбинируемыми фильтрами — для полноценного "Список задач" в UI
    /// (в отличие от SearchTasksQuery — одна нечёткая строка + пагинация,
    /// используется пикером задач в Планировании).
    /// </summary>
    public sealed record FilterTasksQuery(
        string? Title,
        string? Tag,
        VTaskStatus? Status,
        Priority? Priority,
        bool RegularOnly) : IQuery<IReadOnlyList<TaskListItemResponse>>;

    internal sealed class FilterTasksQueryHandler
        : IQueryHandler<FilterTasksQuery, IReadOnlyList<TaskListItemResponse>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public FilterTasksQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<IReadOnlyList<TaskListItemResponse>>> Handle(
            FilterTasksQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.Tasks.AsNoTracking()
                .Where(t => t.UserId == _currentUser.UserId);

            if (!string.IsNullOrWhiteSpace(request.Title))
            {
                var pattern = request.Title.Trim().ToLower();
                query = query.Where(t => t.Title.ToLower().Contains(pattern));
            }

            if (request.Status.HasValue)
            {
                query = query.Where(t => t.Status == request.Status.Value);
            }

            if (request.Priority.HasValue)
            {
                query = query.Where(t => t.Priority == request.Priority.Value);
            }

            if (request.RegularOnly)
            {
                query = query.Where(t => t.Type == VTaskType.Repeatable);
            }

            var candidates = await query
                .OrderByDescending(t => t.UpdatedAt)
                .Select(t => new TaskListItemResponse
                {
                    Id = t.Id,
                    Title = t.Title,
                    Status = t.Status,
                    Priority = t.Priority,
                    Type = t.Type,
                    Deadline = t.Deadline,
                    OrderIndex = t.OrderIndex,
                    CompletedPercent = t.CompletedPercent,
                    GoalId = t.GoalId,
                    Tags = t.Tags,
                    AttachmentCount = t.Attachments.Count
                })
                .ToListAsync(cancellationToken);

            // Tags хранится как jsonb — точечный per-элемент ToLower().Contains()
            // не транслируется в SQL, поэтому фильтр по тегу применяется в памяти
            // уже поверх набора, суженного по Title/Status/Priority в базе.
            IEnumerable<TaskListItemResponse> results = candidates;
            if (!string.IsNullOrWhiteSpace(request.Tag))
            {
                var tagPattern = request.Tag.Trim().ToLower();
                results = results.Where(r => r.Tags.Any(tag => tag.ToLower().Contains(tagPattern)));
            }

            return results.ToList();
        }
    }
}
