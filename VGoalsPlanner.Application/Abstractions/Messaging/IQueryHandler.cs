using MediatR;
using VGoalsPlanner.Domain.Abstractions;

namespace VGoalsPlanner.Application.Abstractions.Messaging
{
    public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
        where TQuery : IQuery<TResponse>
    {
    }
}
