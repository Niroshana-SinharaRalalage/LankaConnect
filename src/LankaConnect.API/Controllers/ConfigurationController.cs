using LankaConnect.BuildingBlocks.Application.Common.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
namespace LankaConnect.Host.AllInOne.Controllers;

/// <summary>
/// Phase 6A.95: Controller for exposing application configuration and feature flags.
/// Public endpoint for frontend to retrieve feature flag states.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ConfigurationController : ControllerBase
{
    private readonly ILogger<ConfigurationController> _logger;
    private readonly SalesTaxSettings _salesTaxSettings;
    private readonly CommissionSettings _commissionSettings;

    public ConfigurationController(
        ILogger<ConfigurationController> logger,
        IOptions<SalesTaxSettings> salesTaxSettings,
        IOptions<CommissionSettings> commissionSettings)
    {
        _logger = logger;
        _salesTaxSettings = salesTaxSettings.Value;
        _commissionSettings = commissionSettings.Value;
    }

    /// <summary>
    /// Returns feature flag configuration for frontend consumption.
    /// This endpoint is public and cacheable.
    /// </summary>
    /// <returns>Feature flags object</returns>
    [HttpGet("features")]
    [ResponseCache(Duration = 60)] // Cache for 1 minute
    [ProducesResponseType(typeof(FeatureFlagsResponse), StatusCodes.Status200OK)]
    public ActionResult<FeatureFlagsResponse> GetFeatureFlags()
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = "GetFeatureFlags"
        });

        _logger.LogDebug(
            "Feature flags requested. SalesTax.Enabled={SalesTaxEnabled}",
            _salesTaxSettings.Enabled);

        var response = new FeatureFlagsResponse
        {
            SalesTaxEnabled = _salesTaxSettings.Enabled
        };

        return Ok(response);
    }

    /// <summary>
    /// Returns commission/fee settings for frontend revenue calculation preview.
    /// This endpoint is public and cacheable.
    /// </summary>
    /// <returns>Commission settings for revenue preview calculation</returns>
    [HttpGet("commission-settings")]
    [ResponseCache(Duration = 300)] // Cache for 5 minutes
    [ProducesResponseType(typeof(CommissionSettingsResponse), StatusCodes.Status200OK)]
    public ActionResult<CommissionSettingsResponse> GetCommissionSettings()
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = "GetCommissionSettings"
        });

        _logger.LogDebug(
            "Commission settings requested. PlatformRate={PlatformRate}%, StripeRate={StripeRate}% + ${StripeFeeFixed}",
            _commissionSettings.PlatformCommissionRate * 100,
            _commissionSettings.StripeFeeRate * 100,
            _commissionSettings.StripeFeeFixed);

        var response = new CommissionSettingsResponse
        {
            PlatformCommissionRate = _commissionSettings.PlatformCommissionRate,
            StripeFeeRate = _commissionSettings.StripeFeeRate,
            StripeFeeFixed = _commissionSettings.StripeFeeFixed,
            SalesTaxEnabled = _salesTaxSettings.Enabled
        };

        return Ok(response);
    }
}

/// <summary>
/// Phase 6A.95: Response DTO for feature flags endpoint
/// </summary>
public record FeatureFlagsResponse
{
    /// <summary>
    /// Indicates whether sales tax collection is enabled.
    /// When false, tax calculations return 0 and frontend should hide tax breakdown.
    /// </summary>
    public bool SalesTaxEnabled { get; init; }
}

/// <summary>
/// Phase 6A.95: Response DTO for commission settings endpoint
/// </summary>
public record CommissionSettingsResponse
{
    /// <summary>
    /// Platform commission rate (e.g., 0.02 for 2%)
    /// </summary>
    public decimal PlatformCommissionRate { get; init; }

    /// <summary>
    /// Stripe percentage fee rate (e.g., 0.029 for 2.9%)
    /// </summary>
    public decimal StripeFeeRate { get; init; }

    /// <summary>
    /// Stripe fixed fee per transaction (e.g., 0.30 for $0.30)
    /// </summary>
    public decimal StripeFeeFixed { get; init; }

    /// <summary>
    /// Whether sales tax collection is enabled
    /// </summary>
    public bool SalesTaxEnabled { get; init; }
}
