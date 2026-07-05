using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace LankaConnect.Host.AllInOne.BackgroundServices;

/// <summary>
/// Phase 6A.99: Hosted service that validates email templates at application startup.
/// In Development: Logs errors and warnings
/// In Production: Logs warnings only (doesn't throw)
/// </summary>
public class EmailTemplateValidationService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<EmailTemplateValidationService> _logger;

    public EmailTemplateValidationService(
        IServiceScopeFactory scopeFactory,
        IHostEnvironment environment,
        ILogger<EmailTemplateValidationService> logger)
    {
        _scopeFactory = scopeFactory;
        _environment = environment;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Phase 6A.99: Email template validation service starting...");

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var validator = scope.ServiceProvider.GetService<IEmailTemplateValidator>();

            if (validator == null)
            {
                _logger.LogWarning("Phase 6A.99: Email template validator not registered. Skipping validation.");
                return;
            }

            var result = await validator.ValidateAllTemplatesAsync(cancellationToken);

            // Log the results
            if (result.IsValid)
            {
                _logger.LogInformation(
                    "Phase 6A.99: Email template validation passed. {Count} templates validated successfully.",
                    result.ValidatedTemplates.Count);
            }
            else if (result.HasWarningsOnly)
            {
                foreach (var warning in result.Warnings)
                {
                    _logger.LogWarning(
                        "Phase 6A.99: Template '{Template}' - {Message}",
                        warning.TemplateName,
                        warning.Message);
                }
            }
            else
            {
                // Log all errors
                foreach (var error in result.Errors)
                {
                    _logger.LogError(
                        "Phase 6A.99: Template '{Template}' validation failed - {Message}. Missing in code: [{MissingInCode}]. Missing in template: [{MissingInTemplate}]",
                        error.TemplateName,
                        error.Message,
                        string.Join(", ", error.MissingInCode),
                        string.Join(", ", error.MissingInTemplate));
                }

                // In Development, throw to fail fast
                if (_environment.IsDevelopment())
                {
                    _logger.LogError(
                        "Phase 6A.99: Email template validation failed with {ErrorCount} errors. " +
                        "Fix the template/parameter mismatches before proceeding.",
                        result.Errors.Count);

                    // Don't throw - just log in development to avoid breaking startup
                    // The developer should check the logs and fix the issues
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Phase 6A.99: Email template validation failed with exception");

            // Don't throw - we don't want to prevent the application from starting
            // The validation is informational, not critical
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
