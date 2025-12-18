using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixBadgeLocationConfigZeroValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.32: Fix for badge location config ZERO values issue
            // ROOT CAUSE: When PostgreSQL converts nullable columns to NOT NULL via AlterColumn,
            // existing NULL values are converted to the type's default (0 for numeric) rather than
            // using the DEFAULT constraint value. Previous migrations used COALESCE which only
            // handles NULL, not zeros.
            //
            // SOLUTION: Directly UPDATE all badges with correct default values without COALESCE.
            // This ensures badges with zero values (from NULL→NOT NULL conversion) get proper defaults.

            migrationBuilder.Sql(@"
                UPDATE badges.badges
                SET
                    -- Listing config: TopRight (x=1.0, y=0.0) with 26% size
                    position_x_listing = 1.0,
                    position_y_listing = 0.0,
                    size_width_listing = 0.26,
                    size_height_listing = 0.26,
                    rotation_listing = 0.0,

                    -- Featured config: TopRight (x=1.0, y=0.0) with 26% size
                    position_x_featured = 1.0,
                    position_y_featured = 0.0,
                    size_width_featured = 0.26,
                    size_height_featured = 0.26,
                    rotation_featured = 0.0,

                    -- Detail config: TopRight (x=1.0, y=0.0) with 21% size (smaller for large images)
                    position_x_detail = 1.0,
                    position_y_detail = 0.0,
                    size_width_detail = 0.21,
                    size_height_detail = 0.21,
                    rotation_detail = 0.0,

                    updated_at = NOW()
                WHERE
                    -- Only update badges with incorrect zero values (result of NULL→NOT NULL conversion)
                    position_x_listing = 0 OR size_width_listing = 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No rollback needed - this is a data-only fix
            // Rolling back would restore broken zero values
        }
    }
}
