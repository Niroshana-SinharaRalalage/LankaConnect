using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixEventImagePrimaryDataConsistency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.13: Fix data consistency for primary images
            // Ensure only one image per event is marked as primary
            // Keep the image with lowest DisplayOrder (first in sequence) as primary

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
                    -- Keep the one with lowest DisplayOrder
                    SELECT ei2.""Id""
                    FROM events.""EventImages"" ei2
                    WHERE ei2.""EventId"" = ei.""EventId""
                      AND ei2.""IsPrimary"" = true
                    ORDER BY ei2.""DisplayOrder"" ASC
                    LIMIT 1
                  );
            ");

            // Step 2: Ensure each event has at least one primary image (if it has any images)
            // Set the first image (by DisplayOrder) as primary if no primary exists
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
                )
                UPDATE events.""EventImages"" ei
                SET ""IsPrimary"" = true
                WHERE ei.""EventId"" IN (SELECT ""EventId"" FROM events_without_primary)
                  AND ei.""DisplayOrder"" = 1;
            ");

            migrationBuilder.AlterColumn<bool>(
                name: "IsPrimary",
                schema: "events",
                table: "EventImages",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "IsPrimary",
                schema: "events",
                table: "EventImages",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);
        }
    }
}
