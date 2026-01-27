using LankaConnect.Shared.Email.Configuration;
using LankaConnect.Shared.Email.Observability;
using LankaConnect.Shared.Email.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Shared.Email.Extensions;

/// <summary>
/// Phase 6A.87: DI registration extensions for the hybrid email system.
///
/// Usage in Startup/Program.cs:
///
///   // Register core email services (in Shared)
///   services.AddTypedEmailServices(configuration);
///
///   // Register bridge adapter (in Application)
///   services.AddEmailServiceBridge();
///
/// Configuration (appsettings.json):
///
///   "EmailFeatureFlags": {
///     "UseTypedParameters": false,
///     "EnableLogging": true,
///     "EnableValidation": true,
///     "HandlerOverrides": {
///       "EventReminderJob": true
///     }
///   }
/// </summary>
public static class EmailServiceExtensions
{
    /// <summary>
    /// Registers the core typed email services from the Shared project.
    /// Call this in the Application/API startup.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration (for feature flags)</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddTypedEmailServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register feature flags from configuration
        services.Configure<EmailFeatureFlags>(
            configuration.GetSection("EmailFeatureFlags"));

        // Register as singleton for consistent flag checking
        services.AddSingleton(sp =>
        {
            var flags = new EmailFeatureFlags();
            configuration.GetSection("EmailFeatureFlags").Bind(flags);
            return flags;
        });

        // Register logger implementation
        services.AddSingleton<IEmailLogger, DefaultEmailLogger>();

        // Register metrics implementation (singleton for aggregation)
        services.AddSingleton<IEmailMetrics, DefaultEmailMetrics>();

        // Register typed email service adapter
        // Note: IEmailServiceBridge must be registered separately (in Application project)
        services.AddScoped<ITypedEmailService, TypedEmailServiceAdapter>();

        return services;
    }
}

/// <summary>
/// Default implementation of IEmailLogger using Microsoft.Extensions.Logging.
/// </summary>
public class DefaultEmailLogger : IEmailLogger
{
    private readonly ILogger<DefaultEmailLogger> _logger;

    public DefaultEmailLogger(ILogger<DefaultEmailLogger> logger)
    {
        _logger = logger;
    }

    public string GenerateCorrelationId()
    {
        return Guid.NewGuid().ToString();
    }

    public void LogEmailSendStart(string correlationId, string templateName, string recipientEmail)
    {
        _logger.LogInformation(
            "[{CorrelationId}] Starting email send: Template={TemplateName}, Recipient={RecipientEmail}",
            correlationId, templateName, recipientEmail);
    }

    public void LogEmailSendSuccess(string correlationId, string templateName, int durationMs)
    {
        _logger.LogInformation(
            "[{CorrelationId}] Email sent successfully: Template={TemplateName}, Duration={DurationMs}ms",
            correlationId, templateName, durationMs);
    }

    public void LogEmailSendFailure(string correlationId, string templateName, string errorMessage, Exception? exception = null)
    {
        _logger.LogError(exception,
            "[{CorrelationId}] Email send failed: Template={TemplateName}, Error={ErrorMessage}",
            correlationId, templateName, errorMessage);
    }

    public void LogParameterValidationFailure(string correlationId, string templateName, List<string> errors)
    {
        _logger.LogWarning(
            "[{CorrelationId}] Parameter validation failed: Template={TemplateName}, Errors={ErrorCount} ({Errors})",
            correlationId, templateName, errors.Count, string.Join(", ", errors));
    }

    public void LogTemplateNotFound(string correlationId, string templateName)
    {
        _logger.LogError(
            "[{CorrelationId}] Email template not found: Template={TemplateName}",
            correlationId, templateName);
    }

    public void LogFeatureFlagCheck(string handlerName, bool isEnabled, string reason)
    {
        _logger.LogDebug(
            "Feature flag check: Handler={HandlerName}, Enabled={IsEnabled}, Reason={Reason}",
            handlerName, isEnabled, reason);
    }
}

/// <summary>
/// Default implementation of IEmailMetrics with in-memory storage.
/// For production, consider using Application Insights or Prometheus.
/// </summary>
public class DefaultEmailMetrics : IEmailMetrics
{
    private readonly Dictionary<string, TemplateMetrics> _templateMetrics = new();
    private readonly Dictionary<string, HandlerMetrics> _handlerMetrics = new();
    private readonly object _lock = new();

    public void RecordEmailSent(string templateName, int durationMs, bool success)
    {
        lock (_lock)
        {
            var metrics = GetOrCreateTemplateMetrics(templateName);
            metrics.TotalSent++;
            metrics.TotalDurationMs += durationMs;
            metrics.AverageDurationMs = metrics.TotalDurationMs / metrics.TotalSent;

            if (success)
                metrics.SuccessCount++;
            else
                metrics.FailureCount++;
        }
    }

    public void RecordParameterValidationFailure(string templateName)
    {
        lock (_lock)
        {
            var metrics = GetOrCreateTemplateMetrics(templateName);
            metrics.ValidationFailures++;
        }
    }

    public void RecordTemplateNotFound(string templateName)
    {
        lock (_lock)
        {
            var metrics = GetOrCreateTemplateMetrics(templateName);
            metrics.TemplateNotFoundCount++;
        }
    }

    public void RecordHandlerUsage(string handlerName, bool usedTypedParameters)
    {
        lock (_lock)
        {
            var metrics = GetOrCreateHandlerMetrics(handlerName);
            metrics.TotalEmailsSent++;

            if (usedTypedParameters)
                metrics.TypedParameterUsageCount++;
            else
                metrics.DictionaryParameterUsageCount++;
        }
    }

    public TemplateMetrics GetStatsByTemplate(string templateName)
    {
        lock (_lock)
        {
            return _templateMetrics.GetValueOrDefault(templateName) ?? new TemplateMetrics();
        }
    }

    public HandlerMetrics GetStatsByHandler(string handlerName)
    {
        lock (_lock)
        {
            return _handlerMetrics.GetValueOrDefault(handlerName) ?? new HandlerMetrics();
        }
    }

    public GlobalMetrics GetGlobalStats()
    {
        lock (_lock)
        {
            return new GlobalMetrics
            {
                TotalEmailsSent = _templateMetrics.Values.Sum(m => m.TotalSent),
                TotalSuccesses = _templateMetrics.Values.Sum(m => m.SuccessCount),
                TotalFailures = _templateMetrics.Values.Sum(m => m.FailureCount)
            };
        }
    }

    public void ResetMetrics()
    {
        lock (_lock)
        {
            _templateMetrics.Clear();
            _handlerMetrics.Clear();
        }
    }

    private TemplateMetrics GetOrCreateTemplateMetrics(string templateName)
    {
        if (!_templateMetrics.ContainsKey(templateName))
            _templateMetrics[templateName] = new TemplateMetrics();
        return _templateMetrics[templateName];
    }

    private HandlerMetrics GetOrCreateHandlerMetrics(string handlerName)
    {
        if (!_handlerMetrics.ContainsKey(handlerName))
            _handlerMetrics[handlerName] = new HandlerMetrics();
        return _handlerMetrics[handlerName];
    }
}
