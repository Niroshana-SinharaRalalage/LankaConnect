using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStripePaymentInfrastructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "payments");

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
                name: "SubscriptionEndDate",
                schema: "identity",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionStartDate",
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
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "TrialEndDate",
                schema: "identity",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "stripe_customers",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stripe_customer_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    stripe_created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stripe_customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "stripe_webhook_events",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    processed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stripe_webhook_events", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_users_stripe_customer_id",
                schema: "identity",
                table: "users",
                column: "StripeCustomerId",
                unique: true,
                filter: "stripe_customer_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_users_stripe_subscription_id",
                schema: "identity",
                table: "users",
                column: "StripeSubscriptionId",
                unique: true,
                filter: "stripe_subscription_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_users_subscription_status",
                schema: "identity",
                table: "users",
                column: "SubscriptionStatus");

            migrationBuilder.CreateIndex(
                name: "ix_users_trial_end_date",
                schema: "identity",
                table: "users",
                column: "TrialEndDate");

            migrationBuilder.CreateIndex(
                name: "ix_stripe_customers_stripe_customer_id",
                schema: "payments",
                table: "stripe_customers",
                column: "stripe_customer_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stripe_customers_user_id",
                schema: "payments",
                table: "stripe_customers",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stripe_webhook_events_event_id",
                schema: "payments",
                table: "stripe_webhook_events",
                column: "event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stripe_webhook_events_event_type",
                schema: "payments",
                table: "stripe_webhook_events",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "ix_stripe_webhook_events_processed",
                schema: "payments",
                table: "stripe_webhook_events",
                column: "processed");

            migrationBuilder.CreateIndex(
                name: "ix_stripe_webhook_events_processed_created_at",
                schema: "payments",
                table: "stripe_webhook_events",
                columns: new[] { "processed", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stripe_customers",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "stripe_webhook_events",
                schema: "payments");

            migrationBuilder.DropIndex(
                name: "ix_users_stripe_customer_id",
                schema: "identity",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_stripe_subscription_id",
                schema: "identity",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_subscription_status",
                schema: "identity",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_trial_end_date",
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
                name: "SubscriptionEndDate",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "SubscriptionStartDate",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "SubscriptionStatus",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "TrialEndDate",
                schema: "identity",
                table: "users");
        }
    }
}
