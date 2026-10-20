using Microsoft.EntityFrameworkCore;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Application.Abstractions.Messaging;
using VGoalsPlanner.Application.Abstractions.Services;
using VGoalsPlanner.Application.VGoals.Common;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Errors;

namespace VGoalsPlanner.Application.VGoals
{
    public sealed record GetGoalByIdQuery(Guid Id) : IQuery<GoalDetailResponse>;

    internal sealed class GetGoalByIdQueryHandler : IQueryHandler<GetGoalByIdQuery, GoalDetailResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetGoalByIdQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<GoalDetailResponse>> Handle(GetGoalByIdQuery request, CancellationToken cancellationToken)
        {
            var goal = await _context.Goals.AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == request.Id && g.UserId == _currentUser.UserId, cancellationToken);

            if (goal is null)
            {
                return Result.Failure<GoalDetailResponse>(VGoalErrors.NotFound(request.Id));
            }

            var childGoals = await _context.Goals.AsNoTracking()
                .Where(g => g.ParentGoalId == goal.Id && g.UserId == _currentUser.UserId)
                .OrderBy(g => g.OrderIndex)
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

            var childTasks = await _context.Tasks.AsNoTracking()
                .Where(t => t.GoalId == goal.Id && t.UserId == _currentUser.UserId)
                .OrderBy(t => t.OrderIndex)
                .Select(t => new GoalTaskSummaryResponse
                {
                    Id = t.Id,
                    Title = t.Title,
                    Status = t.Status,
                    Priority = t.Priority,
                    Deadline = t.Deadline,
                    Tags = t.Tags,
                    AttachmentCount = t.Attachments.Count
                })
                .ToListAsync(cancellationToken);

            return new GoalDetailResponse
            {
                Id = goal.Id,
                Title = goal.Title,
                Description = goal.Description,
                Priority = goal.Priority,
                Status = goal.Status,
                ParentGoalId = goal.ParentGoalId,
                OrderIndex = goal.OrderIndex,
                CreatedAt = goal.CreatedAt,
                UpdatedAt = goal.UpdatedAt,
                ChildGoals = childGoals,
                ChildTasks = childTasks
            };
        }
    }
}
