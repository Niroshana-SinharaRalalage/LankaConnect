using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    /// <param name="resetUsers">If true, delete and re-create admin users (for fixing corrupted/stale data)</param>
    /// <returns>Success message with seeding results</returns>
    [HttpPost("seed")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SeedDatabase([FromQuery] string seedType = "all", [FromQuery] bool resetUsers = false)
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
                    // Log user count BEFORE seeding
                    var userCountBefore = await _context.Users.CountAsync();
                    Logger.LogInformation("User count BEFORE seeding: {UserCount}", userCountBefore);

                    // If resetUsers flag is set, delete and recreate admin users
                    if (resetUsers)
                    {
                        Logger.LogWarning("resetUsers=true: Deleting existing admin users to force re-seeding");
                        var adminEmails = new[]
                        {
                            "admin@lankaconnect.com",
                            "admin1@lankaconnect.com",
                            "organizer@lankaconnect.com",
                            "user@lankaconnect.com"
                        };

                        var adminUsers = await _context.Users
                            .Where(u => adminEmails.Contains(u.Email.Value))
                            .ToListAsync();

                        if (adminUsers.Any())
                        {
                            Logger.LogWarning("Found {Count} admin users to delete: {Emails}",
                                adminUsers.Count,
                                string.Join(", ", adminUsers.Select(u => u.Email.Value)));

                            _context.Users.RemoveRange(adminUsers);
                            await _context.SaveChangesAsync();
                            Logger.LogInformation("Successfully deleted {Count} admin users for re-seeding", adminUsers.Count);
                        }
                        else
                        {
                            Logger.LogInformation("No admin users found to delete");
                        }
                    }

                    await UserSeeder.SeedAsync(_context, _passwordHashingService);

                    // Log user count AFTER seeding to verify persistence
                    var userCountAfter = await _context.Users.CountAsync();
                    Logger.LogInformation("User count AFTER seeding: {UserCount}", userCountAfter);

                    if (userCountAfter == userCountBefore && !resetUsers)
                    {
                        Logger.LogWarning("WARNING: User count did not change after seeding! Check idempotency or database. Hint: Try seedType=users&resetUsers=true");
                    }
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

            // Include database counts in response for verification
            var finalUserCount = await _context.Users.CountAsync();
            var finalEventCount = await _context.Events.CountAsync();
            var finalMetroAreaCount = await _context.MetroAreas.CountAsync();

            return Ok(new
            {
                message = $"Database seeding completed successfully for type: {seedType}",
                environment = _environment.EnvironmentName,
                timestamp = DateTime.UtcNow,
                seedType = seedType,
                databaseState = new
                {
                    userCount = finalUserCount,
                    eventCount = finalEventCount,
                    metroAreaCount = finalMetroAreaCount
                }
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
