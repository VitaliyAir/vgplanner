using MediatR;
using VGoalsPlanner.Domain.Abstractions;

namespace VGoalsPlanner.Application.Abstractions.Messaging
{
    public interface IQuery<TResponse> : IRequest<Result<TResponse>>
    {
    }
}
