using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixEventImagePrimaryConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.13: Fix data consistency for primary images
            // Step 1: For each event with multiple primary images, unmark all but the one with lowest DisplayOrder
            migrationBuilder.Sql(@"
                UPDATE events.""EventImages"" ei
                SET ""IsPrimary"" = false
                WHERE ei.""IsPrimary"" = true
                  AND ei.""EventId"" IN (
                    -- Find events with multiple primary images
                    SELECT ""EventId""
                    FROM events.""EventImages""
                    WHERE ""IsPrimary"" = true
                    GROUP BY ""EventId""
                    HAVING COUNT(*) > 1
                  )
                  AND ei.""Id"" NOT IN (
                    -- Keep the one with lowest DisplayOrder for each event
                    SELECT DISTINCT ON (ei2.""EventId"") ei2.""Id""
                    FROM events.""EventImages"" ei2
                    WHERE ei2.""IsPrimary"" = true
                    ORDER BY ei2.""EventId"", ei2.""DisplayOrder"" ASC
                  );
            ");

            // Step 2: Ensure each event with images has at least one primary image
            migrationBuilder.Sql(@"
                WITH events_with_images AS (
                    SELECT DISTINCT ei.""EventId""
                    FROM events.""EventImages"" ei
                ),
                events_without_primary AS (
                    SELECT ewa.""EventId""
                    FROM events_with_images ewa
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM events.""EventImages"" ei
                        WHERE ei.""EventId"" = ewa.""EventId""
                          AND ei.""IsPrimary"" = true
                    )
                ),
                first_image_per_event AS (
                    SELECT DISTINCT ON (ei.""EventId"") ei.""Id""
                    FROM events.""EventImages"" ei
                    WHERE ei.""EventId"" IN (SELECT ""EventId"" FROM events_without_primary)
                    ORDER BY ei.""EventId"", ei.""DisplayOrder"" ASC
                )
                UPDATE events.""EventImages"" ei
                SET ""IsPrimary"" = true
                WHERE ei.""Id"" IN (SELECT ""Id"" FROM first_image_per_event);
            ");

            // Step 3: Create unique partial index to enforce only one primary image per event
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_EventImages_EventId_IsPrimary_True""
                ON events.""EventImages"" (""EventId"")
                WHERE ""IsPrimary"" = true;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the unique constraint
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS events.""IX_EventImages_EventId_IsPrimary_True"";
            ");
        }
    }
}
