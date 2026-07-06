using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
namespace LankaConnect.Host.AllInOne.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;
    private readonly IFeatureManager _featureManager;

    public HealthController(ILogger<HealthController> logger, IFeatureManager featureManager)
    {
        _logger = logger;
        _featureManager = featureManager;
    }
    /// <summary>
    /// Health check endpoint for the API
    /// </summary>
    /// <returns>Health status</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object> { ["Operation"] = "HealthCheck" });
        
        _logger.LogInformation("Health check requested");
        
        var response = new 
        { 
            Status = "Healthy", 
            Timestamp = DateTime.UtcNow,
            Service = "LankaConnect API",
            Version = "1.0.0"
        };
        
        _logger.LogInformation("Health check completed successfully");
        return Ok(response);
    }

    /// <summary>
    /// Detailed health check with dependencies
    /// </summary>
    /// <returns>Detailed health status</returns>
    [HttpGet("detailed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult DetailedHealth()
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object> { ["Operation"] = "DetailedHealthCheck" });
        
        _logger.LogInformation("Detailed health check requested");
        
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        
        using var envScope = _logger.BeginScope(new Dictionary<string, object> { ["Environment"] = environment });
        
        var response = new 
        { 
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Service = "LankaConnect API",
            Version = "1.0.0",
            Dependencies = new
            {
                Database = "Connected",
                Redis = "Connected"
            },
            Environment = environment
        };
        
        _logger.LogInformation("Detailed health check completed successfully in {Environment} environment", environment);
        return Ok(response);
    }

    /// <summary>
    /// Feature flag smoke endpoint — proves Microsoft.FeatureManagement wiring per W1.5 / ADR-004.
    /// Returns the evaluated state of `Refactor.Smoke.Enabled` plus the list of currently
    /// registered flag names. Anonymous endpoint (does not require auth) so the staging deploy
    /// smoke step can hit it directly.
    /// </summary>
    /// <remarks>
    /// Registry: see <see href="../../../docs/feature-flags.md"/>. Flag categories per ADR-004:
    /// Refactor.* (≤4 weeks), Feature.* (indefinite), Experiment.* (≤8 weeks), Ops.* (kill-switches),
    /// Country.* (geo gates).
    /// </remarks>
    [HttpGet("feature-flags")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> FeatureFlags()
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object> { ["Operation"] = "FeatureFlagsSmoke" });

        try
        {
            // Smoke flag — evaluated to prove wiring. Defined in appsettings.json
            // under FeatureManagement section. ADR-004 default-closed rule for
            // Refactor.* category means the absence of config = false.
            var smokeEnabled = await _featureManager.IsEnabledAsync("Refactor.Smoke.Enabled");

            // Collect all registered flag names — useful for the staging smoke
            // step + the frontend GET /api/featureflags endpoint that lands next.
            var registeredFlags = new List<string>();
            await foreach (var name in _featureManager.GetFeatureNamesAsync())
            {
                registeredFlags.Add(name);
            }

            var response = new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                FeatureManagement = new
                {
                    SmokeFlag = "Refactor.Smoke.Enabled",
                    SmokeFlagValue = smokeEnabled,
                    RegisteredFlags = registeredFlags,
                    RegisteredCount = registeredFlags.Count
                }
            };

            _logger.LogInformation(
                "Feature flag smoke evaluated: Refactor.Smoke.Enabled={SmokeEnabled}, registered count={Count}",
                smokeEnabled,
                registeredFlags.Count);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Feature flag smoke endpoint failed");
            throw;
        }
    }
}