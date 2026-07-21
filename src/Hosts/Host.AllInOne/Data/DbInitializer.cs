using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Infrastructure.Data;                                          // 4C.e.3: AppDbContext
using LankaConnect.Infrastructure.Data.Seeders;                                  // MetroAreaSeeder, BadgeSeeder, EventSeeder (still legacy)
using LankaConnect.Modules.Identity.Infrastructure.Data;                         // 4C.e.3: IdentityDbContext
using LankaConnect.Modules.Identity.Infrastructure.Data.Seeders;                 // 4C.e.3: UserSeeder (moved)
using LankaConnect.Products.LankaEvents.Infrastructure.Data;                     // Consult #20/21: LankaEventsDbContext for LankaEvents seeders
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;

// 4C.e.3 (2026-07-08): relocated from LankaConnect.Infrastructure to the
// LankaConnect.Hosts.AllInOne host so it can PR Identity.Infrastructure without a cycle
// (Identity.Infrastructure -> LankaConnect.Infrastructure already exists).
// Only callers were the host (Program.cs + AdminController) so no cross-module
// leak.
namespace LankaConnect.Host.AllInOne.Data;

/// <summary>
/// Database initializer for seeding initial data.
/// Call this from Program.cs or API startup to populate the database.
/// </summary>
public class DbInitializer
{
    private readonly AppDbContext _context;
    // 4C.e.3: separate injection for Users seeding path (module DbContext).
    private readonly IdentityDbContext _identityContext;
    // Consult #20/21 (2026-07-10): separate injection for LankaEvents seeding paths
    // (MetroAreaSeeder, BadgeSeeder, EventTemplateSeeder) — those entities are
    // Ignore<>()d on AppDbContext post-Consult-#20 sweep.
    private readonly LankaEventsDbContext _lankaEventsContext;
    private readonly ILogger<DbInitializer> _logger;
    private readonly IPasswordHashingService _passwordHashingService;

    public DbInitializer(
        AppDbContext context,
        IdentityDbContext identityContext,
        LankaEventsDbContext lankaEventsContext,
        ILogger<DbInitializer> logger,
        IPasswordHashingService passwordHashingService)
    {
        _context = context;
        _identityContext = identityContext;
        _lankaEventsContext = lankaEventsContext;
        _logger = logger;
        _passwordHashingService = passwordHashingService;
    }

    /// <summary>
    /// Seeds the database with initial data (metro areas, events, etc.)
    /// Idempotent - safe to call multiple times
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            // Ensure database is created and migrations are applied
            await _context.Database.MigrateAsync();

            // Seed users first (Phase 6A.1) - required for event organizers
            await SeedUsersAsync();

            // Seed metro areas (Phase 5C)
            await SeedMetroAreasAsync();

            // Seed badges (Phase 6A.25)
            await SeedBadgesAsync();

            // Seed events
            await SeedEventsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    /// <summary>
    /// Seeds admin users into the database
    /// Phase 6A.1: Admin User Seeding
    /// </summary>
    private async Task SeedUsersAsync()
    {
        // 4C.e.3 (2026-07-08): route Users read + seed through IdentityDbContext.
        var existingUsersCount = await _identityContext.Users.CountAsync();
        if (existingUsersCount > 0)
        {
            _logger.LogInformation("Database already contains {Count} users. Skipping seed.", existingUsersCount);
            return;
        }

        _logger.LogInformation("Seeding admin users...");
        await UserSeeder.SeedAsync(_identityContext, _passwordHashingService);
        _logger.LogInformation("Successfully seeded admin users to the database.");
    }

    /// <summary>
    /// Seeds metro areas into the database
    /// Phase 5C: Metro Areas System
    /// Phase 6A.70: Removed early return to allow incremental metro area additions
    /// </summary>
    private async Task SeedMetroAreasAsync()
    {
        var existingMetroAreasCount = await _lankaEventsContext.MetroAreas.CountAsync();
        _logger.LogInformation("Database currently contains {Count} metro areas. Checking for missing metros...", existingMetroAreasCount);

        // Phase 6A.70: Always call seeder - it handles incremental additions internally
        await MetroAreaSeeder.SeedAsync(_lankaEventsContext);

        var finalCount = await _lankaEventsContext.MetroAreas.CountAsync();
        _logger.LogInformation("Metro area seeding complete. Total metros: {FinalCount} (added {Added})",
            finalCount, finalCount - existingMetroAreasCount);
    }

    /// <summary>
    /// Seeds predefined badges into the database
    /// Phase 6A.25: Badge Management System
    /// Phase 6A.28: Changed to check only for system badges, so seeding works even if custom badges exist
    /// </summary>
    private async Task SeedBadgesAsync()
    {
        // Badge stays on AppDbContext (Consult #20 OUT-OF-SCOPE — Badge DbSet not on LankaEventsDbContext).
        var existingSystemBadgesCount = await _context.Badges.CountAsync(b => b.IsSystem);
        if (existingSystemBadgesCount > 0)
        {
            _logger.LogInformation("Database already contains {Count} system badges. Skipping seed.", existingSystemBadgesCount);
            return;
        }

        var existingTotalBadgesCount = await _context.Badges.CountAsync();
        _logger.LogInformation("Seeding predefined system badges... (found {Count} existing custom badges)", existingTotalBadgesCount);
        await BadgeSeeder.SeedAsync(_context);
        _logger.LogInformation("Successfully seeded predefined system badges to the database.");
    }

    /// <summary>
    /// Seeds events into the database
    /// </summary>
    private async Task SeedEventsAsync()
    {
        var existingEventsCount = await _lankaEventsContext.Events.CountAsync();
        if (existingEventsCount > 0)
        {
            _logger.LogInformation("Database already contains {Count} events. Skipping seed.", existingEventsCount);
            return;
        }

        _logger.LogInformation("Seeding events...");

        // Get seed events from EventSeeder
        var seedEvents = EventSeeder.GetSeedEvents();

        // Add events to context
        await _lankaEventsContext.Events.AddRangeAsync(seedEvents);

        // Save changes
        var savedCount = await _lankaEventsContext.SaveChangesAsync();

        _logger.LogInformation("Successfully seeded {Count} events to the database.", savedCount);
    }

    /// <summary>
    /// Clears all existing events and reseeds (use with caution!)
    /// </summary>
    public async Task ReseedAsync()
    {
        try
        {
            _logger.LogWarning("Clearing existing events...");

            // Remove all existing events
            var existingEvents = await _lankaEventsContext.Events.ToListAsync();
            _lankaEventsContext.Events.RemoveRange(existingEvents);
            await _lankaEventsContext.SaveChangesAsync();

            _logger.LogInformation("Cleared {Count} existing events.", existingEvents.Count);

            // Reseed
            await SeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while reseeding the database.");
            throw;
        }
    }
}
