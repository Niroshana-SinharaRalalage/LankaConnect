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

                    // Check which admin users actually exist BEFORE deletion
                    var allUsersBeforeDelete = await _context.Users.ToListAsync();
                    var requiredAdminEmails = new[]
                    {
                        "admin@lankaconnect.com",
                        "admin1@lankaconnect.com",
                        "organizer@lankaconnect.com",
                        "user@lankaconnect.com"
                    };
                    var existingAdminEmails = allUsersBeforeDelete
                        .Select(u => u.Email.Value)
                        .Where(email => requiredAdminEmails.Contains(email))
                        .ToList();

                    Logger.LogInformation("BEFORE deletion - Admin users that exist: {Emails}",
                        existingAdminEmails.Any() ? string.Join(", ", existingAdminEmails) : "(none)");

                    // If resetUsers flag is set, delete and recreate admin users
                    if (resetUsers)
                    {
                        Logger.LogWarning("resetUsers=true: Deleting existing admin users to force re-seeding");

                        try
                        {
                            // First, get all existing admin users - need to load them first due to EF Core translation limits
                            // Then filter in memory to ensure we get exact matches
                            var allUsers = await _context.Users.ToListAsync();
                            Logger.LogInformation("Loaded {Count} total users from database", allUsers.Count);

                            var adminEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                            {
                                "admin@lankaconnect.com",
                                "admin1@lankaconnect.com",
                                "organizer@lankaconnect.com",
                                "user@lankaconnect.com"
                            };

                            // Debug: Log all user emails to see what we're working with
                            var allEmails = allUsers.Select(u => u.Email.Value).ToList();
                            Logger.LogInformation("Existing emails in DB: {Emails}", string.Join(", ", allEmails.Take(20)));

                            var adminUsers = allUsers
                                .Where(u => adminEmails.Contains(u.Email.Value))
                                .ToList();

                            Logger.LogInformation("Found {Count} matching admin users for deletion", adminUsers.Count);

                            if (adminUsers.Any())
                            {
                                Logger.LogWarning("Found {Count} admin users to delete: {Emails}",
                                    adminUsers.Count,
                                    string.Join(", ", adminUsers.Select(u => u.Email.Value)));

                                _context.Users.RemoveRange(adminUsers);
                                var deleteResult = await _context.SaveChangesAsync();
                                Logger.LogInformation("SaveChangesAsync returned {RowsAffected} rows affected", deleteResult);
                                Logger.LogInformation("Successfully deleted {Count} admin users for re-seeding", adminUsers.Count);

                                // Verify deletion actually occurred
                                var usersAfterDelete = await _context.Users.CountAsync();
                                Logger.LogInformation("User count immediately after deletion: {UserCount}", usersAfterDelete);
                            }
                            else
                            {
                                Logger.LogInformation("No admin users found to delete");
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, "Error during resetUsers deletion: {Message}", ex.Message);
                            throw;
                        }
                    }

                    try
                    {
                        Logger.LogInformation("Starting UserSeeder.SeedAsync...");
                        await UserSeeder.SeedAsync(_context, _passwordHashingService);
                        Logger.LogInformation("UserSeeder.SeedAsync completed without exception");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error in UserSeeder.SeedAsync: {Message}", ex.Message);
                        throw;
                    }

                    // Log user count AFTER seeding to verify persistence
                    var userCountAfter = await _context.Users.CountAsync();
                    Logger.LogInformation("User count AFTER seeding: {UserCount}", userCountAfter);

                    // Check which admin users exist AFTER seeding
                    var allUsersAfterSeed = await _context.Users.ToListAsync();
                    var existingAdminEmailsAfterSeed = allUsersAfterSeed
                        .Select(u => u.Email.Value)
                        .Where(email => requiredAdminEmails.Contains(email))
                        .ToList();

                    Logger.LogInformation("AFTER seeding - Admin users that exist: {Emails}",
                        existingAdminEmailsAfterSeed.Any() ? string.Join(", ", existingAdminEmailsAfterSeed) : "(none)");

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
    /// Fix admin user passwords (force reset to known defaults)
    /// ONLY available in Development and Staging for testing/debugging
    /// Phase 6A.9: Fixes login issues by resetting passwords to Admin@2025!, Organizer@2025!, User@2025!
    /// </summary>
    /// <returns>Success message with password reset results</returns>
    [HttpPost("fix-passwords")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> FixAdminPasswords()
    {
        // Only allow in Development or Staging
        if (!_environment.IsDevelopment() && !_environment.IsStaging())
        {
            Logger.LogWarning("Fix passwords endpoint called in {Environment} - DENIED", _environment.EnvironmentName);
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = "Password fixing is only available in Development and Staging environments"
            });
        }

        try
        {
            Logger.LogInformation("Starting admin password reset process...");

            // Load all users
            var allUsers = await _context.Users.ToListAsync();
            Logger.LogInformation("Loaded {Count} users from database", allUsers.Count);

            // Define password mappings (passwords must meet validation: 8+ chars, upper+lower+digit+special, no sequential chars)
            var passwordMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "admin@lankaconnect.com", "Admin@2025!" },
                { "admin1@lankaconnect.com", "Admin@2025!" },
                { "organizer@lankaconnect.com", "Organizer@2025!" },
                { "user@lankaconnect.com", "User@2025!" }
            };

            var updated = 0;
            foreach (var kvp in passwordMappings)
            {
                var email = kvp.Key;
                var password = kvp.Value;

                var user = allUsers.FirstOrDefault(u => u.Email.Value.Equals(email, StringComparison.OrdinalIgnoreCase));
                if (user != null)
                {
                    Logger.LogWarning("Resetting password for {Email}", email);
                    var hashResult = _passwordHashingService.HashPassword(password);
                    if (hashResult.IsSuccess)
                    {
                        user.SetPassword(hashResult.Value);
                        // Ensure email is verified so login works
                        if (!user.IsEmailVerified)
                        {
                            user.GenerateEmailVerificationToken();
                            user.VerifyEmail(user.EmailVerificationToken!);
                            Logger.LogWarning("Email verification enabled for {Email}", email);
                        }
                        updated++;
                    }
                }
                else
                {
                    Logger.LogWarning("Admin user not found: {Email}", email);
                }
            }

            if (updated > 0)
            {
                await _context.SaveChangesAsync();
                Logger.LogInformation("Successfully updated {Count} admin user passwords", updated);
            }

            return Ok(new
            {
                message = "Admin password reset completed",
                environment = _environment.EnvironmentName,
                timestamp = DateTime.UtcNow,
                passwordsReset = updated,
                details = "Passwords reset to: Admin@2025! for admin/admin1, Organizer@2025! for organizer, User@2025! for user"
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error resetting admin passwords");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Password Reset Error",
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
