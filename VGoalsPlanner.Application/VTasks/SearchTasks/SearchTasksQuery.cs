using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Application.Common;
using VGoalsPlanner.Application.VTasks.Common;
using VGoalsPlanner.Domain.Abstractions;

namespace VGoalsPlanner.Application.VTasks
{
    public sealed record SearchTasksQuery(
        string? Query,
        int Page = 1,
        int PageSize = 20) : IQuery<PagedResult<TaskListItemResponse>>;

    internal sealed class SearchTasksQueryValidator : AbstractValidator<SearchTasksQuery>
    {
        public SearchTasksQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    internal sealed class SearchTasksQueryHandler
        : IQueryHandler<SearchTasksQuery, PagedResult<TaskListItemResponse>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public SearchTasksQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<PagedResult<TaskListItemResponse>>> Handle(
            SearchTasksQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.Tasks.AsNoTracking()
                .Where(t => t.UserId == _currentUser.UserId);

            if (!string.IsNullOrWhiteSpace(request.Query))
            {
                var term = request.Query.Trim();
                var pattern = term.ToLower();

                query = query.Where(t =>
                    t.Title.ToLower().Contains(pattern) ||
                    (t.Description != null && t.Description.ToLower().Contains(pattern)) ||
                    t.Tags.Contains(term));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(t => t.UpdatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
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
                    Tags = t.Tags
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<TaskListItemResponse>
            {
                Items = items,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }
    }
}
