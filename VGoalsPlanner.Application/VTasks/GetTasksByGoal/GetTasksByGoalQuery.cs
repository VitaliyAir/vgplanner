using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Application.Common;
using VGoalsPlanner.Application.VTasks.Common;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Enums;
using VGoalsPlanner.Domain.Errors;

namespace VGoalsPlanner.Application.VTasks
{
    public sealed record GetTasksByGoalQuery(
        Guid GoalId,
        VTaskStatus? Status,
        int Page = 1,
        int PageSize = 20) : IQuery<PagedResult<TaskListItemResponse>>;

    internal sealed class GetTasksByGoalQueryValidator : AbstractValidator<GetTasksByGoalQuery>
    {
        public GetTasksByGoalQueryValidator()
        {
            RuleFor(x => x.GoalId).NotEmpty();
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    internal sealed class GetTasksByGoalQueryHandler
        : IQueryHandler<GetTasksByGoalQuery, PagedResult<TaskListItemResponse>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetTasksByGoalQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<PagedResult<TaskListItemResponse>>> Handle(
            GetTasksByGoalQuery request,
            CancellationToken cancellationToken)
        {
            var goalExists = await _context.Goals
                .AnyAsync(g => g.Id == request.GoalId && g.UserId == _currentUser.UserId, cancellationToken);

            if (!goalExists)
            {
                return Result.Failure<PagedResult<TaskListItemResponse>>(VTaskErrors.GoalNotFound);
            }

            var query = _context.Tasks.AsNoTracking()
                .Where(t => t.GoalId == request.GoalId && t.UserId == _currentUser.UserId);

            if (request.Status.HasValue)
            {
                query = query.Where(t => t.Status == request.Status);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(t => t.OrderIndex)
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
