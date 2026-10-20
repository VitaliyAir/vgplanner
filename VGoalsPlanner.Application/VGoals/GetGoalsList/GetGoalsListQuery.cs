using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Application.Common;
using VGoalsPlanner.Application.VGoals.Common;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Enums;

namespace VGoalsPlanner.Application.VGoals
{
    public sealed record GetGoalsListQuery(
        VGoalStatus? Status,
        Priority? Priority,
        Guid? ParentGoalId,
        string? Search,
        int Page = 1,
        int PageSize = 20) : IQuery<PagedResult<GoalListItemResponse>>;

    internal sealed class GetGoalsListQueryValidator : AbstractValidator<GetGoalsListQuery>
    {
        public GetGoalsListQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    internal sealed class GetGoalsListQueryHandler
        : IQueryHandler<GetGoalsListQuery, PagedResult<GoalListItemResponse>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetGoalsListQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<PagedResult<GoalListItemResponse>>> Handle(
            GetGoalsListQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.Goals.AsNoTracking().Where(g => g.UserId == _currentUser.UserId);

            if (request.Status.HasValue)
            {
                query = query.Where(g => g.Status == request.Status);
            }

            if (request.Priority.HasValue)
            {
                query = query.Where(g => g.Priority == request.Priority);
            }

            if (request.ParentGoalId.HasValue)
            {
                query = query.Where(g => g.ParentGoalId == request.ParentGoalId);
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var pattern = request.Search.Trim().ToLower();
                query = query.Where(g =>
                    g.Title.ToLower().Contains(pattern) ||
                    (g.Description != null && g.Description.ToLower().Contains(pattern)));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(g => g.ParentGoalId)
                .ThenBy(g => g.OrderIndex)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(g => new GoalListItemResponse
                {
                    Id = g.Id,
                    Title = g.Title,
                    Priority = g.Priority,
                    Status = g.Status,
                    ParentGoalId = g.ParentGoalId,
                    OrderIndex = g.OrderIndex,
                    HasChildGoals = g.ChildGoals.Any(),
                    HasChildTasks = g.ChildTasks.Any()
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<GoalListItemResponse>
            {
                Items = items,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }
    }
}
