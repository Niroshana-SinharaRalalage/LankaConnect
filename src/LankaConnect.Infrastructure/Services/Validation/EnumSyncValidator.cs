using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using LankaConnect.Infrastructure.Data; // Wave 6.5.f mirror (2026-07-09 Day 4): AppDbContext direct
namespace LankaConnect.Host.AllInOne.Services.Validation;

/// <summary>
/// Validates that backend enums are synchronized with database reference_values table
/// Phase 6A.109: Prevents enum drift issues like Issue #78
/// Phase 6A.110: Fixed DI lifetime issue - use IServiceProvider to create scope for scoped IApplicationDbContext
/// </summary>
public class EnumSyncValidator : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EnumSyncValidator> _logger;

    public EnumSyncValidator(
        IServiceProvider serviceProvider,
        ILogger<EnumSyncValidator> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("EnumSyncValidator: Starting enum synchronization validation...");

        try
        {
            // Create a scope to resolve scoped IApplicationDbContext
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Validate EventCategory enum
            await ValidateEventCategoryAsync(dbContext, cancellationToken);

            _logger.LogInformation("EnumSyncValidator: All enum validations passed ✓");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EnumSyncValidator: Enum validation failed");
            // Don't throw - log the error but allow app to start
            // This prevents startup failures in environments where DB might not be fully seeded yet
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task ValidateEventCategoryAsync(AppDbContext applicationDbContext, CancellationToken cancellationToken)
    {
        var dbContext = applicationDbContext as DbContext;
        if (dbContext == null)
        {
            _logger.LogWarning("EnumSyncValidator: Cannot validate - DbContext is not available");
            return;
        }

        // Get EventCategory values from database
        var dbCategories = await dbContext.Database
            .SqlQuery<int>($@"
                SELECT int_value
                FROM reference_data.reference_values
                WHERE enum_type = 'EventCategory'
                ORDER BY int_value
            ")
            .ToListAsync(cancellationToken);

        // Get EventCategory enum values
        var enumValues = Enum.GetValues<EventCategory>()
            .Cast<int>()
            .OrderBy(v => v)
            .ToList();

        // Compare counts
        if (dbCategories.Count != enumValues.Count)
        {
            _logger.LogError(
                "EventCategory ENUM MISMATCH: Database has {DbCount} values but enum has {EnumCount} values. " +
                "Database values: [{DbValues}], Enum values: [{EnumValues}]",
                dbCategories.Count,
                enumValues.Count,
                string.Join(", ", dbCategories),
                string.Join(", ", enumValues));

            throw new InvalidOperationException(
                $"EventCategory enum is out of sync with database. " +
                $"Database has {dbCategories.Count} values, enum has {enumValues.Count} values.");
        }

        // Compare individual values
        for (int i = 0; i < dbCategories.Count; i++)
        {
            if (dbCategories[i] != enumValues[i])
            {
                _logger.LogError(
                    "EventCategory VALUE MISMATCH at position {Position}: Database has {DbValue} but enum has {EnumValue}",
                    i, dbCategories[i], enumValues[i]);

                throw new InvalidOperationException(
                    $"EventCategory enum value mismatch at position {i}: " +
                    $"Database={dbCategories[i]}, Enum={enumValues[i]}");
            }
        }

        _logger.LogInformation(
            "EventCategory enum validation SUCCESS: {Count} values in sync between database and enum",
            enumValues.Count);
    }
}
