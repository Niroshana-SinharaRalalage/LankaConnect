using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ApplyBadgeLocationConfigDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.31a: DATA-ONLY migration to fix existing NULL values in badge location config columns
            // Previous migrations (20251216150703 and 20251217030432) were marked as applied but UPDATE didn't execute
            // This fresh migration with new timestamp ensures the UPDATE statement runs
            migrationBuilder.Sql(@"
                UPDATE badges.badges
                SET
                    position_x_listing = COALESCE(position_x_listing, 1.0),
                    position_y_listing = COALESCE(position_y_listing, 0.0),
                    size_width_listing = COALESCE(size_width_listing, 0.26),
                    size_height_listing = COALESCE(size_height_listing, 0.26),
                    rotation_listing = COALESCE(rotation_listing, 0.0),
                    position_x_featured = COALESCE(position_x_featured, 1.0),
                    position_y_featured = COALESCE(position_y_featured, 0.0),
                    size_width_featured = COALESCE(size_width_featured, 0.26),
                    size_height_featured = COALESCE(size_height_featured, 0.26),
                    rotation_featured = COALESCE(rotation_featured, 0.0),
                    position_x_detail = COALESCE(position_x_detail, 1.0),
                    position_y_detail = COALESCE(position_y_detail, 0.0),
                    size_width_detail = COALESCE(size_width_detail, 0.21),
                    size_height_detail = COALESCE(size_height_detail, 0.21),
                    rotation_detail = COALESCE(rotation_detail, 0.0)
                WHERE
                    position_x_listing IS NULL OR position_y_listing IS NULL OR
                    size_width_listing IS NULL OR size_height_listing IS NULL OR rotation_listing IS NULL OR
                    position_x_featured IS NULL OR position_y_featured IS NULL OR
                    size_width_featured IS NULL OR size_height_featured IS NULL OR rotation_featured IS NULL OR
                    position_x_detail IS NULL OR position_y_detail IS NULL OR
                    size_width_detail IS NULL OR size_height_detail IS NULL OR rotation_detail IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
