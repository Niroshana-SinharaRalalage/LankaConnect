using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventTemplatesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_free_trial_ends_at",
                schema: "identity",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_stripe_customer_id",
                schema: "identity",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_subscription_status",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "FreeTrialEndsAt",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "FreeTrialStartedAt",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "StripeCustomerId",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "StripeSubscriptionId",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "SubscriptionActivatedAt",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "SubscriptionCanceledAt",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "SubscriptionStatus",
                schema: "identity",
                table: "users");

            migrationBuilder.CreateTable(
                name: "event_templates",
                schema: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    thumbnail_svg = table.Column<string>(type: "text", nullable: false),
                    template_data_json = table.Column<string>(type: "jsonb", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_templates", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_event_templates_active_category_order",
                schema: "events",
                table: "event_templates",
                columns: new[] { "is_active", "category", "display_order" });

            migrationBuilder.CreateIndex(
                name: "idx_event_templates_category",
                schema: "events",
                table: "event_templates",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "idx_event_templates_display_order",
                schema: "events",
                table: "event_templates",
                column: "display_order");

            migrationBuilder.CreateIndex(
                name: "idx_event_templates_is_active",
                schema: "events",
                table: "event_templates",
                column: "is_active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_templates",
                schema: "events");

            migrationBuilder.AddColumn<DateTime>(
                name: "FreeTrialEndsAt",
                schema: "identity",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FreeTrialStartedAt",
                schema: "identity",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeCustomerId",
                schema: "identity",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeSubscriptionId",
                schema: "identity",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionActivatedAt",
                schema: "identity",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionCanceledAt",
                schema: "identity",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionStatus",
                schema: "identity",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_users_free_trial_ends_at",
                schema: "identity",
                table: "users",
                column: "FreeTrialEndsAt");

            migrationBuilder.CreateIndex(
                name: "ix_users_stripe_customer_id",
                schema: "identity",
                table: "users",
                column: "StripeCustomerId");

            migrationBuilder.CreateIndex(
                name: "ix_users_subscription_status",
                schema: "identity",
                table: "users",
                column: "SubscriptionStatus");
        }
    }
}
