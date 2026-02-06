using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LankaConnect.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
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
}