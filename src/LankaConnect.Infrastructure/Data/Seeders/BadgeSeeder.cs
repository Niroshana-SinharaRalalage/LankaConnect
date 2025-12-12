using LankaConnect.Domain.Badges;
using LankaConnect.Domain.Badges.Enums;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.Infrastructure.Data.Seeders;

/// <summary>
/// Seeds predefined system badges for the Badge Management System
/// Phase 6A.25: Badge Management System
///
/// System badges have IsSystem=true and cannot be deleted, only deactivated.
/// Images are empty placeholders - admin will upload actual badge images via UI.
/// </summary>
public static class BadgeSeeder
{
    /// <summary>
    /// Seed predefined badges into database
    /// </summary>
    public static async Task SeedAsync(AppDbContext context)
    {
        // Check if any badges already exist
        if (await context.Badges.AnyAsync())
        {
            return; // Already seeded
        }

        var badges = new List<Badge>
        {
            // 1. New Event - for newly created events
            Badge.CreateSystemBadge(
                name: "New Event",
                imageUrl: "",
                blobName: "",
                position: BadgePosition.TopRight,
                displayOrder: 1
            ),

            // 2. New - generic "new" badge
            Badge.CreateSystemBadge(
                name: "New",
                imageUrl: "",
                blobName: "",
                position: BadgePosition.TopRight,
                displayOrder: 2
            ),

            // 3. Canceled - for canceled events
            Badge.CreateSystemBadge(
                name: "Canceled",
                imageUrl: "",
                blobName: "",
                position: BadgePosition.TopLeft,
                displayOrder: 3
            ),

            // 4. New Year - for New Year celebrations
            Badge.CreateSystemBadge(
                name: "New Year",
                imageUrl: "",
                blobName: "",
                position: BadgePosition.TopRight,
                displayOrder: 4
            ),

            // 5. Valentines - for Valentine's Day events
            Badge.CreateSystemBadge(
                name: "Valentines",
                imageUrl: "",
                blobName: "",
                position: BadgePosition.TopRight,
                displayOrder: 5
            ),

            // 6. Christmas - for Christmas celebrations
            Badge.CreateSystemBadge(
                name: "Christmas",
                imageUrl: "",
                blobName: "",
                position: BadgePosition.TopRight,
                displayOrder: 6
            ),

            // 7. Thanksgiving - for Thanksgiving celebrations
            Badge.CreateSystemBadge(
                name: "Thanksgiving",
                imageUrl: "",
                blobName: "",
                position: BadgePosition.TopRight,
                displayOrder: 7
            ),

            // 8. Halloween - for Halloween events
            Badge.CreateSystemBadge(
                name: "Halloween",
                imageUrl: "",
                blobName: "",
                position: BadgePosition.TopRight,
                displayOrder: 8
            ),

            // 9. Easter - for Easter celebrations
            Badge.CreateSystemBadge(
                name: "Easter",
                imageUrl: "",
                blobName: "",
                position: BadgePosition.TopRight,
                displayOrder: 9
            ),

            // 10. Sinhala Tamil New Year - Sri Lankan cultural celebration
            Badge.CreateSystemBadge(
                name: "Sinhala Tamil New Year",
                imageUrl: "",
                blobName: "",
                position: BadgePosition.TopRight,
                displayOrder: 10
            ),

            // 11. Vesak - Buddhist celebration
            Badge.CreateSystemBadge(
                name: "Vesak",
                imageUrl: "",
                blobName: "",
                position: BadgePosition.TopRight,
                displayOrder: 11
            )
        };

        await context.Badges.AddRangeAsync(badges);
        await context.SaveChangesAsync();
    }
}
