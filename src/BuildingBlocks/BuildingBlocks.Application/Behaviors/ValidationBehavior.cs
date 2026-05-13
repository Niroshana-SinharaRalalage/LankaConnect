using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace LankaConnect.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Runs all registered <see cref="IValidator{TRequest}"/> instances for the
/// incoming request and throws <see cref="ValidationException"/> on any
/// failures. Handlers downstream can therefore assume valid input.
/// </summary>
/// <remarks>
/// <para>
/// Returning a <c>Result.Failure</c> with a structured validation-errors
/// payload is also a reasonable design, but the FluentValidation ecosystem +
/// ProblemDetails middleware in W2.6 already translate <see cref="ValidationException"/>
/// into a clean 400 response — exception-style is the lower-friction choice.
/// </para>
/// <para>
/// Validators are resolved from DI — they're typically scanned in via
/// <c>AddValidatorsFromAssembly</c> at composition root.
/// </para>
/// </remarks>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
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
        ArgumentNullException.ThrowIfNull(next);

        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var results = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = results
            .Where(r => r is not null && r.Errors.Count > 0)
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        return await next();
    }
}
