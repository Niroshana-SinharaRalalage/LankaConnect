using LankaConnect.Application.Common.Options;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Services;
using LankaConnect.Domain.Tax.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Serilog;

namespace LankaConnect.Infrastructure.Services;

/// <summary>
/// Phase 6A.X: Database-backed sales tax service with in-memory caching
/// Phase 6A.95: Added feature flag support for enabling/disabling tax collection
/// Retrieves US state sales tax rates from StateTaxRates table
/// </summary>
public class DatabaseSalesTaxService : ISalesTaxService
{
    private readonly IStateTaxRateRepository _taxRateRepository;
    private readonly IMemoryCache _cache;
    private readonly SalesTaxSettings _salesTaxSettings;
    private readonly ILogger _logger;
    private const string CacheKeyPrefix = "StateTaxRate_";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(24);

    public DatabaseSalesTaxService(
        IStateTaxRateRepository taxRateRepository,
        IMemoryCache cache,
        IOptions<SalesTaxSettings> salesTaxSettings,
        ILogger logger)
    {
        _taxRateRepository = taxRateRepository;
        _cache = cache;
        _salesTaxSettings = salesTaxSettings.Value;
        _logger = logger;

        _logger.Information(
            "DatabaseSalesTaxService initialized. Sales tax feature enabled: {Enabled}",
            _salesTaxSettings.Enabled);
    }

    /// <summary>
    /// Phase 6A.95: Indicates whether sales tax collection is currently enabled
    /// </summary>
    public bool IsEnabled => _salesTaxSettings.Enabled;

    public async Task<Result<decimal>> GetStateTaxRateAsync(string stateCode, CancellationToken cancellationToken = default)
    {
        // Phase 6A.95: Check feature flag first
        if (!_salesTaxSettings.Enabled)
        {
            _logger.Debug(
                "Sales tax feature is disabled. Returning 0% rate for state {StateCode}",
                stateCode ?? "N/A");
            return Result<decimal>.Success(_salesTaxSettings.DefaultRateWhenDisabled);
        }

        if (string.IsNullOrWhiteSpace(stateCode))
            return Result<decimal>.Failure("State code is required");

        var normalizedCode = stateCode.Trim().ToUpperInvariant();

        // Phase 6A.95: Check if this specific state is enabled (for graduated rollout)
        if (!_salesTaxSettings.IsTaxEnabledForState(normalizedCode))
        {
            _logger.Debug(
                "Sales tax is not enabled for state {StateCode} (not in EnabledStates list). Returning 0%",
                normalizedCode);
            return Result<decimal>.Success(0m);
        }

        // Check if full state name provided (convert to code)
        if (normalizedCode.Length > 2)
        {
            normalizedCode = ConvertStateNameToCode(normalizedCode);
            if (normalizedCode == null)
                return Result<decimal>.Failure($"Invalid state: {stateCode}");
        }

        // Try cache first
        var cacheKey = $"{CacheKeyPrefix}{normalizedCode}";
        if (_cache.TryGetValue<decimal>(cacheKey, out var cachedRate))
        {
            _logger.Debug("Tax rate for {StateCode} retrieved from cache: {TaxRate}", normalizedCode, cachedRate);
            return Result<decimal>.Success(cachedRate);
        }

        // Query database
        var taxRate = await _taxRateRepository.GetActiveByStateCodeAsync(normalizedCode, cancellationToken);

        if (taxRate == null)
        {
            _logger.Warning("No active tax rate found for state code: {StateCode}", normalizedCode);
            return Result<decimal>.Failure($"No tax rate found for state: {stateCode}");
        }

        // Cache the result
        _cache.Set(cacheKey, taxRate.TaxRate, CacheExpiration);

        _logger.Information("Tax rate for {StateCode} retrieved from database and cached: {TaxRate}",
            normalizedCode, taxRate.TaxRate);

        return Result<decimal>.Success(taxRate.TaxRate);
    }

    /// <summary>
    /// Converts full state name to two-letter code
    /// </summary>
    private string? ConvertStateNameToCode(string stateName)
    {
        var stateMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "ALABAMA", "AL" }, { "ALASKA", "AK" }, { "ARIZONA", "AZ" }, { "ARKANSAS", "AR" },
            { "CALIFORNIA", "CA" }, { "COLORADO", "CO" }, { "CONNECTICUT", "CT" }, { "DELAWARE", "DE" },
            { "FLORIDA", "FL" }, { "GEORGIA", "GA" }, { "HAWAII", "HI" }, { "IDAHO", "ID" },
            { "ILLINOIS", "IL" }, { "INDIANA", "IN" }, { "IOWA", "IA" }, { "KANSAS", "KS" },
            { "KENTUCKY", "KY" }, { "LOUISIANA", "LA" }, { "MAINE", "ME" }, { "MARYLAND", "MD" },
            { "MASSACHUSETTS", "MA" }, { "MICHIGAN", "MI" }, { "MINNESOTA", "MN" }, { "MISSISSIPPI", "MS" },
            { "MISSOURI", "MO" }, { "MONTANA", "MT" }, { "NEBRASKA", "NE" }, { "NEVADA", "NV" },
            { "NEW HAMPSHIRE", "NH" }, { "NEW JERSEY", "NJ" }, { "NEW MEXICO", "NM" }, { "NEW YORK", "NY" },
            { "NORTH CAROLINA", "NC" }, { "NORTH DAKOTA", "ND" }, { "OHIO", "OH" }, { "OKLAHOMA", "OK" },
            { "OREGON", "OR" }, { "PENNSYLVANIA", "PA" }, { "RHODE ISLAND", "RI" }, { "SOUTH CAROLINA", "SC" },
            { "SOUTH DAKOTA", "SD" }, { "TENNESSEE", "TN" }, { "TEXAS", "TX" }, { "UTAH", "UT" },
            { "VERMONT", "VT" }, { "VIRGINIA", "VA" }, { "WASHINGTON", "WA" }, { "WEST VIRGINIA", "WV" },
            { "WISCONSIN", "WI" }, { "WYOMING", "WY" }, { "DISTRICT OF COLUMBIA", "DC" }
        };

        return stateMap.TryGetValue(stateName, out var code) ? code : null;
    }
}
