using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LankaConnect.Infrastructure.Data;
using LankaConnect.Infrastructure.Data.Seeders;
using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Administrative endpoints for system maintenance
/// Phase 6A.9: Database seeding endpoints for Development/Staging environments
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AdminController : BaseController<AdminController>
{
    private readonly AppDbContext _context;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILoggerFactory _loggerFactory;

    public AdminController(
        IMediator mediator,
        ILogger<AdminController> logger,
        AppDbContext context,
        IPasswordHashingService passwordHashingService,
        IWebHostEnvironment environment,
        ILoggerFactory loggerFactory)
        : base(mediator, logger)
    {
        _context = context;
        _passwordHashingService = passwordHashingService;
        _environment = environment;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Manually trigger database seeding (metro areas, users, events)
    /// ONLY available in Development and Staging environments
    /// Phase 6A.9: Allows on-demand seeding to unblock testing
    /// NOTE: No authentication required to allow initial database population
    /// </summary>
    /// <param name="seedType">Type of data to seed: "all" (default), "users", "events", "metroareas", "eventtemplates"</param>
    /// <returns>Success message with seeding results</returns>
    [HttpPost("seed")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SeedDatabase([FromQuery] string seedType = "all")
    {
        // Only allow seeding in Development or Staging environments
        if (!_environment.IsDevelopment() && !_environment.IsStaging())
        {
            Logger.LogWarning("Seeding endpoint called in {Environment} environment - DENIED", _environment.EnvironmentName);
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = "Database seeding is only available in Development and Staging environments",
                environment = _environment.EnvironmentName
            });
        }

        try
        {
            Logger.LogInformation("Admin {AdminUserId} triggering database seeding in {Environment} environment (seedType: {SeedType})",
                User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? "unknown",
                _environment.EnvironmentName,
                seedType);

            var dbInitializerLogger = _loggerFactory.CreateLogger<DbInitializer>();
            var dbInitializer = new DbInitializer(_context, dbInitializerLogger, _passwordHashingService);

            switch (seedType.ToLower())
            {
                case "all":
                    await dbInitializer.SeedAsync();
                    break;
                case "users":
                    await UserSeeder.SeedAsync(_context, _passwordHashingService);
                    break;
                case "metroareas":
                    await MetroAreaSeeder.SeedAsync(_context);
                    break;
                case "events":
                    var seedEvents = EventSeeder.GetSeedEvents();
                    await _context.Events.AddRangeAsync(seedEvents);
                    await _context.SaveChangesAsync();
                    break;
                case "eventtemplates":
                    await EventTemplateSeeder.SeedAsync(_context);
                    break;
                default:
                    return BadRequest(new
                    {
                        error = "Invalid seedType. Valid options: all, users, metroareas, events, eventtemplates",
                        providedValue = seedType
                    });
            }

            Logger.LogInformation("Database seeding completed successfully for type: {SeedType}", seedType);

            return Ok(new
            {
                message = $"Database seeding completed successfully for type: {seedType}",
                environment = _environment.EnvironmentName,
                timestamp = DateTime.UtcNow,
                seedType = seedType
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred during database seeding");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Database Seeding Error",
                Detail = ex.Message,
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Get system environment information
    /// Phase 6A.9: Diagnostic endpoint to verify environment configuration
    /// </summary>
    /// <returns>Environment information</returns>
    [HttpGet("environment")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetEnvironment()
    {
        Logger.LogInformation("Admin retrieving environment information");

        return Ok(new
        {
            environmentName = _environment.EnvironmentName,
            isDevelopment = _environment.IsDevelopment(),
            isStaging = _environment.IsStaging(),
            isProduction = _environment.IsProduction(),
            applicationName = _environment.ApplicationName
        });
    }
}
