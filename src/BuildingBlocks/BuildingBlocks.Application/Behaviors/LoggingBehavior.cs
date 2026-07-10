using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
namespace LankaConnect.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Wraps every MediatR request in a structured Serilog scope with a
/// per-request correlation id, logs start / completion / failure with elapsed
/// time, and re-throws so downstream middleware (e.g. <see cref="ICommand{T}"/>
/// transactions) see the exception.
/// </summary>
/// <remarks>
/// <para>
/// Per ADR-002 + ADR-004 observability requirements, EVERY command/query
/// gets a correlation id used to stitch logs across the request, the
/// outbox dispatcher, downstream module handlers, and Application Insights
/// distributed traces.
/// </para>
/// <para>
/// The behavior is intentionally allocation-light on the happy path: a single
/// <see cref="Stopwatch"/> + a Dictionary for the logging scope.
/// </para>
/// </remarks>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var requestName = typeof(TRequest).Name;
        var correlationId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["RequestName"] = requestName,
        });

        _logger.LogInformation("Handling {RequestName} (correlation {CorrelationId})", requestName, correlationId);

        try
        {
            var response = await next();
            stopwatch.Stop();
            _logger.LogInformation(
                "Handled {RequestName} (correlation {CorrelationId}) in {ElapsedMs}ms",
                requestName,
                correlationId,
                stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Failed {RequestName} (correlation {CorrelationId}) after {ElapsedMs}ms: {ErrorMessage}",
                requestName,
                correlationId,
                stopwatch.ElapsedMilliseconds,
                ex.Message);
            throw;
        }
    }
}
