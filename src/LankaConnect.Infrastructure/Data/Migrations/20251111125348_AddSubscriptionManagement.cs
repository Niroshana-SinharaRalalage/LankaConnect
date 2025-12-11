using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
        }
    }
}
