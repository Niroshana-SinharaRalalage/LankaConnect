using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHasOpenItemsToSignUpLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.28: Add has_open_items column to sign_up_lists table
            migrationBuilder.AddColumn<bool>(
                name: "has_open_items",
                schema: "events",
                table: "sign_up_lists",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "position_x_detail",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "position_x_featured",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "position_x_listing",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "position_y_detail",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "position_y_featured",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "position_y_listing",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "rotation_detail",
                schema: "badges",
                table: "badges",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "rotation_featured",
                schema: "badges",
                table: "badges",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "rotation_listing",
                schema: "badges",
                table: "badges",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "size_height_detail",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "size_height_featured",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "size_height_listing",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "size_width_detail",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "size_width_featured",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "size_width_listing",
                schema: "badges",
                table: "badges",
                type: "numeric(5,4)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.28: Remove has_open_items column from sign_up_lists table
            migrationBuilder.DropColumn(
                name: "has_open_items",
                schema: "events",
                table: "sign_up_lists");

            migrationBuilder.DropColumn(
                name: "position_x_detail",
                schema: "badges",
                table: "badges");

            migrationBuilder.DropColumn(
                name: "position_x_featured",
                schema: "badges",
                table: "badges");

            migrationBuilder.DropColumn(
                name: "position_x_listing",
                schema: "badges",
                table: "badges");

            migrationBuilder.DropColumn(
                name: "position_y_detail",
                schema: "badges",
                table: "badges");

            migrationBuilder.DropColumn(
                name: "position_y_featured",
                schema: "badges",
                table: "badges");

            migrationBuilder.DropColumn(
                name: "position_y_listing",
                schema: "badges",
                table: "badges");

            migrationBuilder.DropColumn(
                name: "rotation_detail",
                schema: "badges",
                table: "badges");

            migrationBuilder.DropColumn(
                name: "rotation_featured",
                schema: "badges",
                table: "badges");

            migrationBuilder.DropColumn(
                name: "rotation_listing",
                schema: "badges",
                table: "badges");

            migrationBuilder.DropColumn(
                name: "size_height_detail",
                schema: "badges",
                table: "badges");

            migrationBuilder.DropColumn(
                name: "size_height_featured",
                schema: "badges",
                table: "badges");

            migrationBuilder.DropColumn(
                name: "size_height_listing",
                schema: "badges",
                table: "badges");

            migrationBuilder.DropColumn(
                name: "size_width_detail",
                schema: "badges",
                table: "badges");

            migrationBuilder.DropColumn(
                name: "size_width_featured",
                schema: "badges",
                table: "badges");

            migrationBuilder.DropColumn(
                name: "size_width_listing",
                schema: "badges",
                table: "badges");
        }
    }
}
