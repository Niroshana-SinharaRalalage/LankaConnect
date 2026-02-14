using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Infrastructure.Services.Validation;

/// <summary>
/// Validates that backend enums are synchronized with database reference_values table
/// Phase 6A.109: Prevents enum drift issues like Issue #78
/// </summary>
public class EnumSyncValidator : IHostedService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<EnumSyncValidator> _logger;

    public EnumSyncValidator(
        IApplicationDbContext dbContext,
        ILogger<EnumSyncValidator> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("EnumSyncValidator: Starting enum synchronization validation...");

        try
        {
            // Validate EventCategory enum
            await ValidateEventCategoryAsync(cancellationToken);

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

    private async Task ValidateEventCategoryAsync(CancellationToken cancellationToken)
    {
        var dbContext = _dbContext as DbContext;
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
