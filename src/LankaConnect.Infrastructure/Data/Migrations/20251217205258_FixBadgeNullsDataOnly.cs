using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixBadgeNullsDataOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.31c: DATA-ONLY migration with raw SQL to bypass PostgreSQL's WHERE clause injection
            // This migration ONLY updates data, no schema changes
            // Unconditional UPDATE ensures ALL badges get valid location config values

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
                    rotation_detail = COALESCE(rotation_detail, 0.0);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
