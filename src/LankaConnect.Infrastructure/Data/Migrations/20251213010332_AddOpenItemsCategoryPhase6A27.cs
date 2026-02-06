using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOpenItemsCategoryPhase6A27 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Note: Stripe tables already exist from previous migration (Phase 6A.4)
            // Only apply Open Items category changes here

            migrationBuilder.AddColumn<bool>(
                name: "has_open_items",
                schema: "events",
                table: "sign_up_lists",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_user_id",
                schema: "events",
                table: "sign_up_items",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Only rollback Open Items category changes
            // Stripe tables remain (managed by separate migration)

            migrationBuilder.DropColumn(
                name: "has_open_items",
                schema: "events",
                table: "sign_up_lists");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                schema: "events",
                table: "sign_up_items");
        }
    }
}
