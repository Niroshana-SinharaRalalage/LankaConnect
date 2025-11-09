using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Infrastructure.Data.Seeders;

namespace LankaConnect.Infrastructure.Data;

/// <summary>
/// Database initializer for seeding initial data
/// Call this from Program.cs or API startup to populate the database
/// </summary>
public class DbInitializer
{
    private readonly AppDbContext _context;
    private readonly ILogger<DbInitializer> _logger;

    public DbInitializer(AppDbContext context, ILogger<DbInitializer> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seeds the database with initial event data
    /// Idempotent - safe to call multiple times
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            // Ensure database is created and migrations are applied
            await _context.Database.MigrateAsync();

            // Check if events already exist
            var existingEventsCount = await _context.Events.CountAsync();
            if (existingEventsCount > 0)
            {
                _logger.LogInformation("Database already contains {Count} events. Skipping seed.", existingEventsCount);
                return;
            }

            _logger.LogInformation("Seeding events...");

            // Get seed events from EventSeeder
            var seedEvents = EventSeeder.GetSeedEvents();

            // Add events to context
            await _context.Events.AddRangeAsync(seedEvents);

            // Save changes
            var savedCount = await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully seeded {Count} events to the database.", savedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
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
            var existingEvents = await _context.Events.ToListAsync();
            _context.Events.RemoveRange(existingEvents);
            await _context.SaveChangesAsync();

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
