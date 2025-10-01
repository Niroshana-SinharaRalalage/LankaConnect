using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace LankaConnect.Application.Common.Behaviors;

public class LoggingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingPipelineBehavior<TRequest, TResponse>> _logger;

    public LoggingPipelineBehavior(ILogger<LoggingPipelineBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var requestId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();

        // Create structured scope for logging
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["RequestId"] = requestId,
            ["RequestName"] = requestName,
            ["RequestType"] = typeof(TRequest).FullName ?? "",
            ["ResponseType"] = typeof(TResponse).FullName ?? ""
        });

        _logger.LogInformation("Processing {RequestName} with ID {RequestId}", requestName, requestId);
        
        try
        {
            var response = await next();
            
            stopwatch.Stop();
            
            _logger.LogInformation(
                "Successfully completed {RequestName} with ID {RequestId} in {ElapsedMilliseconds}ms", 
                requestName, 
                requestId,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex, 
                "Failed to process {RequestName} with ID {RequestId} after {ElapsedMilliseconds}ms: {ErrorMessage}", 
                requestName, 
                requestId,
                stopwatch.ElapsedMilliseconds,
                ex.Message);
            
            throw;
        }
    }

    private object? SerializeRequest(TRequest request)
    {
        try
        {
            // Create a safe representation of the request without sensitive data
            var requestType = typeof(TRequest);
            var properties = requestType.GetProperties()
                .Where(p => p.CanRead && 
                           !p.Name.ToLower().Contains("password") && 
                           !p.Name.ToLower().Contains("secret") &&
                           !p.Name.ToLower().Contains("key"))
                .ToDictionary(
                    p => p.Name, 
                    p => p.GetValue(request)?.ToString() ?? "null");

            return properties;
        }
        catch
        {
            return new { RequestType = typeof(TRequest).Name, Message = "Could not serialize request safely" };
        }
    }
}