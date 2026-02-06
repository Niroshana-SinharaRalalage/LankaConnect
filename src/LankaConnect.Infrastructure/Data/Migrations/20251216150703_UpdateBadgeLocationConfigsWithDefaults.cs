using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBadgeLocationConfigsWithDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.31a: Update existing NULL values in badge location config columns
            // This ensures existing badges have valid location configs before adding default constraints
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

            migrationBuilder.AlterColumn<decimal>(
                name: "size_width_listing",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                defaultValue: 0.26m,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "size_width_featured",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                defaultValue: 0.26m,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "size_width_detail",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                defaultValue: 0.21m,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "size_height_listing",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                defaultValue: 0.26m,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "size_height_featured",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                defaultValue: 0.26m,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "size_height_detail",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                defaultValue: 0.21m,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "rotation_listing",
                schema: "badges",
                table: "badges",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0.0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "rotation_featured",
                schema: "badges",
                table: "badges",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0.0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "rotation_detail",
                schema: "badges",
                table: "badges",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0.0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "position_y_listing",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                defaultValue: 0.0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "position_y_featured",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                defaultValue: 0.0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "position_y_detail",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                defaultValue: 0.0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "position_x_listing",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                defaultValue: 1.0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "position_x_featured",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                defaultValue: 1.0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "position_x_detail",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                defaultValue: 1.0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,4)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "size_width_listing",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,4)",
                oldDefaultValue: 0.26m);

            migrationBuilder.AlterColumn<decimal>(
                name: "size_width_featured",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,4)",
                oldDefaultValue: 0.26m);

            migrationBuilder.AlterColumn<decimal>(
                name: "size_width_detail",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,4)",
                oldDefaultValue: 0.21m);

            migrationBuilder.AlterColumn<decimal>(
                name: "size_height_listing",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,4)",
                oldDefaultValue: 0.26m);

            migrationBuilder.AlterColumn<decimal>(
                name: "size_height_featured",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,4)",
                oldDefaultValue: 0.26m);

            migrationBuilder.AlterColumn<decimal>(
                name: "size_height_detail",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,4)",
                oldDefaultValue: 0.21m);

            migrationBuilder.AlterColumn<decimal>(
                name: "rotation_listing",
                schema: "badges",
                table: "badges",
                type: "numeric(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldDefaultValue: 0.0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "rotation_featured",
                schema: "badges",
                table: "badges",
                type: "numeric(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldDefaultValue: 0.0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "rotation_detail",
                schema: "badges",
                table: "badges",
                type: "numeric(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldDefaultValue: 0.0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "position_y_listing",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,4)",
                oldDefaultValue: 0.0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "position_y_featured",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,4)",
                oldDefaultValue: 0.0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "position_y_detail",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,4)",
                oldDefaultValue: 0.0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "position_x_listing",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,4)",
                oldDefaultValue: 1.0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "position_x_featured",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,4)",
                oldDefaultValue: 1.0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "position_x_detail",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,4)",
                oldDefaultValue: 1.0m);
        }
    }
}
