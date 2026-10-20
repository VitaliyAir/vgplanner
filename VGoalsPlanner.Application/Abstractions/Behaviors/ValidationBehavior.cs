using FluentValidation;
using MediatR;
using VGoalsPlanner.Domain.Abstractions;

namespace VGoalsPlanner.Application.Abstractions.Behaviors
{
    /// <summary>
    /// Прогоняет все зарегистрированные FluentValidation-валидаторы запроса перед хендлером
    /// и превращает ошибки валидации в Result.Failure/Result&lt;T&gt;.Failure вместо исключения.
    /// </summary>
    public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (!_validators.Any())
            {
                return await next();
            }

            var context = new ValidationContext<TRequest>(request);

            var failures = (await Task.WhenAll(
                    _validators.Select(v => v.ValidateAsync(context, cancellationToken))))
                .SelectMany(result => result.Errors)
                .Where(failure => failure is not null)
                .ToList();

            if (failures.Count == 0)
            {
                return await next();
            }

            var error = new Error(
                "Validation.Error",
                string.Join("; ", failures.Select(f => f.ErrorMessage)));

            return CreateValidationResult<TResponse>(error);
        }

        private static TResult CreateValidationResult<TResult>(Error error)
        {
            if (typeof(TResult) == typeof(Result))
            {
                return (TResult)(object)Result.Failure(error);
            }

            var resultType = typeof(TResult);

            if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var valueType = resultType.GetGenericArguments()[0];

                var failureMethod = typeof(Result)
                    .GetMethod(nameof(Result.Failure), 1, new[] { typeof(Error) })!
                    .MakeGenericMethod(valueType);

                return (TResult)failureMethod.Invoke(null, new object[] { error })!;
            }

            throw new InvalidOperationException(
                $"Не удалось создать результат валидации для типа {typeof(TResult).Name}");
        }
    }
}
