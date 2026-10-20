using MediatR;
using VGoalsPlanner.Domain.Abstractions;

namespace VGoalsPlanner.Application.Abstractions.Messaging
{
    public interface ICommand : IRequest<Result>
    {
    }

    public interface ICommand<TResponse> : IRequest<Result<TResponse>>
    {
    }
}
