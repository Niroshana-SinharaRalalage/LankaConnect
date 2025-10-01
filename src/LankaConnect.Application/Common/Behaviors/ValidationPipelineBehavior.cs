using MediatR;
using FluentValidation;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Behaviors;

public class ValidationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : class
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationPipelineBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken))
        );

        var failures = validationResults
            .Where(r => r != null && r.Errors.Any())
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures.Any())
        {
            var errorMessages = failures.Select(x => x.ErrorMessage).ToList();

            // Handle Result<T> types
            if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var resultType = typeof(TResponse).GetGenericArguments()[0];
                var failureMethod = typeof(Result<>).MakeGenericType(resultType)
                    .GetMethod(nameof(Result<object>.Failure), new[] { typeof(IEnumerable<string>) });
                
                return (TResponse)failureMethod!.Invoke(null, new[] { errorMessages })!;
            }

            // Handle Result types
            if (typeof(TResponse) == typeof(Result))
            {
                return (TResponse)(object)Result.Failure(errorMessages);
            }

            throw new ValidationException(failures);
        }

        return await next();
    }
}