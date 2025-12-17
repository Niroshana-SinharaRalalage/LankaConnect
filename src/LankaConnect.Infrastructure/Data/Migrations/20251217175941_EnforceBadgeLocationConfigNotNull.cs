using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnforceBadgeLocationConfigNotNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.31b: Comprehensive fix for Badge location config NULL values
            // This migration addresses schema drift where columns are NULLABLE in DB but NOT NULL in EF model

            // STEP 1: Update ALL existing rows to ensure non-NULL values
            // Using COALESCE to set defaults WITHOUT any WHERE clause - this guarantees ALL rows are updated
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

            // STEP 2: Alter columns to NOT NULL with DEFAULT constraints
            // This prevents future NULL insertions at database level

            // Listing config columns
            migrationBuilder.AlterColumn<decimal>(
                name: "position_x_listing",
                schema: "badges",
                table: "badges",
                type: "decimal(5,4)",
                nullable: false,
                defaultValue: 1.0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "position_y_listing",
                schema: "badges",
                table: "badges",
                type: "decimal(5,4)",
                nullable: false,
                defaultValue: 0.0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "size_width_listing",
                schema: "badges",
                table: "badges",
                type: "decimal(5,4)",
                nullable: false,
                defaultValue: 0.26m,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "size_height_listing",
                schema: "badges",
                table: "badges",
                type: "decimal(5,4)",
                nullable: false,
                defaultValue: 0.26m,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "rotation_listing",
                schema: "badges",
                table: "badges",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0.0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldNullable: true);

            // Featured config columns
            migrationBuilder.AlterColumn<decimal>(
                name: "position_x_featured",
                schema: "badges",
                table: "badges",
                type: "decimal(5,4)",
                nullable: false,
                defaultValue: 1.0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "position_y_featured",
                schema: "badges",
                table: "badges",
                type: "decimal(5,4)",
                nullable: false,
                defaultValue: 0.0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "size_width_featured",
                schema: "badges",
                table: "badges",
                type: "decimal(5,4)",
                nullable: false,
                defaultValue: 0.26m,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "size_height_featured",
                schema: "badges",
                table: "badges",
                type: "decimal(5,4)",
                nullable: false,
                defaultValue: 0.26m,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "rotation_featured",
                schema: "badges",
                table: "badges",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0.0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldNullable: true);

            // Detail config columns
            migrationBuilder.AlterColumn<decimal>(
                name: "position_x_detail",
                schema: "badges",
                table: "badges",
                type: "decimal(5,4)",
                nullable: false,
                defaultValue: 1.0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "position_y_detail",
                schema: "badges",
                table: "badges",
                type: "decimal(5,4)",
                nullable: false,
                defaultValue: 0.0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "size_width_detail",
                schema: "badges",
                table: "badges",
                type: "decimal(5,4)",
                nullable: false,
                defaultValue: 0.21m,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "size_height_detail",
                schema: "badges",
                table: "badges",
                type: "decimal(5,4)",
                nullable: false,
                defaultValue: 0.21m,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "rotation_detail",
                schema: "badges",
                table: "badges",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0.0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback: Make columns nullable again (not recommended)
            migrationBuilder.AlterColumn<decimal>(
                name: "position_x_listing",
                schema: "badges",
                table: "badges",
                type: "decimal(5,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "position_y_listing",
                schema: "badges",
                table: "badges",
                type: "decimal(5,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "size_width_listing",
                schema: "badges",
                table: "badges",
                type: "decimal(5,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "size_height_listing",
                schema: "badges",
                table: "badges",
                type: "decimal(5,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "rotation_listing",
                schema: "badges",
                table: "badges",
                type: "decimal(5,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "position_x_featured",
                schema: "badges",
                table: "badges",
                type: "decimal(5,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "position_y_featured",
                schema: "badges",
                table: "badges",
                type: "decimal(5,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "size_width_featured",
                schema: "badges",
                table: "badges",
                type: "decimal(5,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "size_height_featured",
                schema: "badges",
                table: "badges",
                type: "decimal(5,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "rotation_featured",
                schema: "badges",
                table: "badges",
                type: "decimal(5,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "position_x_detail",
                schema: "badges",
                table: "badges",
                type: "decimal(5,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "position_y_detail",
                schema: "badges",
                table: "badges",
                type: "decimal(5,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "size_width_detail",
                schema: "badges",
                table: "badges",
                type: "decimal(5,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "size_height_detail",
                schema: "badges",
                table: "badges",
                type: "decimal(5,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "rotation_detail",
                schema: "badges",
                table: "badges",
                type: "decimal(5,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)");
        }
    }
}
