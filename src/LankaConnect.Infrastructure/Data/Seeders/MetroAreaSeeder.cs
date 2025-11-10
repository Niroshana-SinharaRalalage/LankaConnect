using LankaConnect.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.Infrastructure.Data.Seeders;

/// <summary>
/// Seeds initial metro area data for the LankaConnect platform
/// Phase 5C: Metro Areas System
/// </summary>
public static class MetroAreaSeeder
{
    /// <summary>
    /// Seed metro areas data into database
    /// </summary>
    public static async Task SeedAsync(AppDbContext context)
    {
        // Check if any metro areas already exist
        if (await context.MetroAreas.AnyAsync())
        {
            return; // Already seeded
        }

        var metroAreas = new List<MetroArea>
        {
            // Ohio State-level area
            MetroArea.Create(
                id: Guid.Parse("00000000-0000-0000-0000-000000000001"),
                name: "All Ohio",
                state: "OH",
                centerLatitude: 40.4173,
                centerLongitude: -82.9071,
                radiusMiles: 200,
                isStateLevelArea: true,
                isActive: true
            ),

            // Cleveland Metro Area
            MetroArea.Create(
                id: Guid.Parse("11111111-0000-0000-0000-000000000001"),
                name: "Cleveland",
                state: "OH",
                centerLatitude: 41.4993,
                centerLongitude: -81.6944,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),

            // Columbus Metro Area
            MetroArea.Create(
                id: Guid.Parse("11111111-0000-0000-0000-000000000002"),
                name: "Columbus",
                state: "OH",
                centerLatitude: 39.9612,
                centerLongitude: -82.9988,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),

            // Cincinnati Metro Area
            MetroArea.Create(
                id: Guid.Parse("11111111-0000-0000-0000-000000000003"),
                name: "Cincinnati",
                state: "OH",
                centerLatitude: 39.1031,
                centerLongitude: -84.5120,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),

            // Toledo Metro Area
            MetroArea.Create(
                id: Guid.Parse("11111111-0000-0000-0000-000000000004"),
                name: "Toledo",
                state: "OH",
                centerLatitude: 41.6528,
                centerLongitude: -83.5379,
                radiusMiles: 25,
                isStateLevelArea: false,
                isActive: true
            ),

            // Akron Metro Area
            MetroArea.Create(
                id: Guid.Parse("11111111-0000-0000-0000-000000000005"),
                name: "Akron",
                state: "OH",
                centerLatitude: 41.0814,
                centerLongitude: -81.5190,
                radiusMiles: 25,
                isStateLevelArea: false,
                isActive: true
            ),

            // Dayton Metro Area
            MetroArea.Create(
                id: Guid.Parse("11111111-0000-0000-0000-000000000006"),
                name: "Dayton",
                state: "OH",
                centerLatitude: 39.7589,
                centerLongitude: -84.1916,
                radiusMiles: 25,
                isStateLevelArea: false,
                isActive: true
            ),

            // New York State-level area
            MetroArea.Create(
                id: Guid.Parse("00000000-0000-0000-0000-000000000002"),
                name: "All New York",
                state: "NY",
                centerLatitude: 42.1657,
                centerLongitude: -74.9481,
                radiusMiles: 250,
                isStateLevelArea: true,
                isActive: true
            ),

            // New York City Metro Area
            MetroArea.Create(
                id: Guid.Parse("22222222-0000-0000-0000-000000000001"),
                name: "New York City",
                state: "NY",
                centerLatitude: 40.7128,
                centerLongitude: -74.0060,
                radiusMiles: 40,
                isStateLevelArea: false,
                isActive: true
            ),

            // Buffalo Metro Area
            MetroArea.Create(
                id: Guid.Parse("22222222-0000-0000-0000-000000000002"),
                name: "Buffalo",
                state: "NY",
                centerLatitude: 42.8864,
                centerLongitude: -78.8784,
                radiusMiles: 25,
                isStateLevelArea: false,
                isActive: true
            ),

            // Pennsylvania State-level area
            MetroArea.Create(
                id: Guid.Parse("00000000-0000-0000-0000-000000000003"),
                name: "All Pennsylvania",
                state: "PA",
                centerLatitude: 40.5908,
                centerLongitude: -77.2098,
                radiusMiles: 200,
                isStateLevelArea: true,
                isActive: true
            ),

            // Philadelphia Metro Area
            MetroArea.Create(
                id: Guid.Parse("33333333-0000-0000-0000-000000000001"),
                name: "Philadelphia",
                state: "PA",
                centerLatitude: 39.9526,
                centerLongitude: -75.1652,
                radiusMiles: 35,
                isStateLevelArea: false,
                isActive: true
            ),

            // Pittsburgh Metro Area
            MetroArea.Create(
                id: Guid.Parse("33333333-0000-0000-0000-000000000002"),
                name: "Pittsburgh",
                state: "PA",
                centerLatitude: 40.4406,
                centerLongitude: -79.9959,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            )
        };

        await context.MetroAreas.AddRangeAsync(metroAreas);
        await context.SaveChangesAsync();
    }
}
