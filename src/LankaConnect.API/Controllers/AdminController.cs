using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LankaConnect.Infrastructure.Data;
using LankaConnect.Infrastructure.Data.Seeders;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.BackgroundJobs;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Administrative endpoints for system maintenance
/// Phase 6A.9: Database seeding endpoints for Development/Staging environments
/// Phase 6A.75: Added manual trigger for EventReminderJob
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
    private readonly IBackgroundJobClient _backgroundJobClient;

    public AdminController(
        IMediator mediator,
        ILogger<AdminController> logger,
        AppDbContext context,
        IPasswordHashingService passwordHashingService,
        IWebHostEnvironment environment,
        ILoggerFactory loggerFactory,
        IBackgroundJobClient backgroundJobClient)
        : base(mediator, logger)
    {
        _context = context;
        _passwordHashingService = passwordHashingService;
        _environment = environment;
        _loggerFactory = loggerFactory;
        _backgroundJobClient = backgroundJobClient;
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

    /// <summary>
    /// Phase 6A.75: Manually trigger EventReminderJob for testing
    /// ONLY available in Development and Staging environments
    /// This allows testing reminder emails without waiting for the hourly schedule
    /// </summary>
    /// <returns>Job ID of the enqueued reminder job</returns>
    [HttpPost("trigger-reminder-job")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult TriggerReminderJob()
    {
        // Only allow in Development or Staging
        if (!_environment.IsDevelopment() && !_environment.IsStaging())
        {
            Logger.LogWarning("[Phase 6A.75] Trigger reminder job endpoint called in {Environment} - DENIED", _environment.EnvironmentName);
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = "Manual job triggering is only available in Development and Staging environments"
            });
        }

        try
        {
            Logger.LogInformation("[Phase 6A.75] Manually triggering EventReminderJob...");

            // Enqueue the job to run immediately
            var jobId = _backgroundJobClient.Enqueue<EventReminderJob>(job => job.ExecuteAsync());

            Logger.LogInformation("[Phase 6A.75] EventReminderJob enqueued with JobId: {JobId}", jobId);

            return Ok(new
            {
                message = "EventReminderJob has been enqueued and will execute shortly",
                jobId = jobId,
                environment = _environment.EnvironmentName,
                timestamp = DateTime.UtcNow,
                note = "Check the Hangfire dashboard at /hangfire for job status and logs"
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[Phase 6A.75] Error triggering EventReminderJob");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Job Trigger Error",
                Detail = ex.Message,
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Phase 6A.75: Get upcoming events in reminder windows for diagnostic purposes
    /// Shows which events would receive reminders if the job ran now
    /// </summary>
    /// <returns>Events in each reminder window</returns>
    [HttpGet("reminder-diagnostics")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetReminderDiagnostics()
    {
        // Only allow in Development or Staging
        if (!_environment.IsDevelopment() && !_environment.IsStaging())
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = "Diagnostics only available in Development and Staging environments"
            });
        }

        try
        {
            var now = DateTime.UtcNow;

            // Check 7-day window (167-169 hours)
            var sevenDayStart = now.AddHours(167);
            var sevenDayEnd = now.AddHours(169);
            var sevenDayEvents = await _context.Events
                .AsNoTracking()
                .Include(e => e.Registrations)
                .Where(e => e.StartDate >= sevenDayStart && e.StartDate <= sevenDayEnd)
                .Where(e => e.Status == Domain.Events.Enums.EventStatus.Published || e.Status == Domain.Events.Enums.EventStatus.Active)
                .Select(e => new { e.Id, Title = e.Title.Value, e.StartDate, RegistrationCount = e.Registrations.Count })
                .ToListAsync();

            // Check 2-day window (47-49 hours)
            var twoDayStart = now.AddHours(47);
            var twoDayEnd = now.AddHours(49);
            var twoDayEvents = await _context.Events
                .AsNoTracking()
                .Include(e => e.Registrations)
                .Where(e => e.StartDate >= twoDayStart && e.StartDate <= twoDayEnd)
                .Where(e => e.Status == Domain.Events.Enums.EventStatus.Published || e.Status == Domain.Events.Enums.EventStatus.Active)
                .Select(e => new { e.Id, Title = e.Title.Value, e.StartDate, RegistrationCount = e.Registrations.Count })
                .ToListAsync();

            // Check 1-day window (23-25 hours)
            var oneDayStart = now.AddHours(23);
            var oneDayEnd = now.AddHours(25);
            var oneDayEvents = await _context.Events
                .AsNoTracking()
                .Include(e => e.Registrations)
                .Where(e => e.StartDate >= oneDayStart && e.StartDate <= oneDayEnd)
                .Where(e => e.Status == Domain.Events.Enums.EventStatus.Published || e.Status == Domain.Events.Enums.EventStatus.Active)
                .Select(e => new { e.Id, Title = e.Title.Value, e.StartDate, RegistrationCount = e.Registrations.Count })
                .ToListAsync();

            // Get reminders already sent today using raw SQL (table is not mapped as entity)
            var todayStart = DateTime.UtcNow.Date;
            var remindersSentToday = 0;
            try
            {
                using var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM events.event_reminders_sent WHERE sent_at >= @todayStart";
                var param = command.CreateParameter();
                param.ParameterName = "@todayStart";
                param.Value = todayStart;
                command.Parameters.Add(param);
                var result = await command.ExecuteScalarAsync();
                remindersSentToday = Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "[Phase 6A.75] Could not retrieve reminder count from database");
            }

            return Ok(new
            {
                timestamp = now,
                windows = new
                {
                    sevenDay = new
                    {
                        windowStart = sevenDayStart,
                        windowEnd = sevenDayEnd,
                        eventsFound = sevenDayEvents.Count,
                        events = sevenDayEvents
                    },
                    twoDay = new
                    {
                        windowStart = twoDayStart,
                        windowEnd = twoDayEnd,
                        eventsFound = twoDayEvents.Count,
                        events = twoDayEvents
                    },
                    oneDay = new
                    {
                        windowStart = oneDayStart,
                        windowEnd = oneDayEnd,
                        eventsFound = oneDayEvents.Count,
                        events = oneDayEvents
                    }
                },
                remindersSentToday = remindersSentToday,
                note = "Events must have status Published or Active and have registrations to receive reminders"
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[Phase 6A.75] Error getting reminder diagnostics");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Diagnostics Error",
                Detail = ex.Message,
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }
}
