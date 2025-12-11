using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSignUpItemCategorySupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_passes");

            migrationBuilder.DropTable(
                name: "pass_purchases");

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

            migrationBuilder.RenameTable(
                name: "sign_up_lists",
                newName: "sign_up_lists",
                newSchema: "events");

            migrationBuilder.RenameTable(
                name: "sign_up_commitments",
                newName: "sign_up_commitments",
                newSchema: "events");

            migrationBuilder.AddColumn<bool>(
                name: "has_mandatory_items",
                schema: "events",
                table: "sign_up_lists",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "has_preferred_items",
                schema: "events",
                table: "sign_up_lists",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "has_suggested_items",
                schema: "events",
                table: "sign_up_lists",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "notes",
                schema: "events",
                table: "sign_up_commitments",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "sign_up_item_id",
                schema: "events",
                table: "sign_up_commitments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "sign_up_items",
                schema: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sign_up_list_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    item_category = table.Column<int>(type: "integer", nullable: false),
                    remaining_quantity = table.Column<int>(type: "integer", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sign_up_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_sign_up_items_sign_up_lists_sign_up_list_id",
                        column: x => x.sign_up_list_id,
                        principalSchema: "events",
                        principalTable: "sign_up_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sign_up_commitments_sign_up_item_id",
                schema: "events",
                table: "sign_up_commitments",
                column: "sign_up_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_sign_up_items_category",
                schema: "events",
                table: "sign_up_items",
                column: "item_category");

            migrationBuilder.CreateIndex(
                name: "ix_sign_up_items_list_id",
                schema: "events",
                table: "sign_up_items",
                column: "sign_up_list_id");

            migrationBuilder.AddForeignKey(
                name: "FK_sign_up_commitments_sign_up_items_sign_up_item_id",
                schema: "events",
                table: "sign_up_commitments",
                column: "sign_up_item_id",
                principalSchema: "events",
                principalTable: "sign_up_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_sign_up_commitments_sign_up_items_sign_up_item_id",
                schema: "events",
                table: "sign_up_commitments");

            migrationBuilder.DropTable(
                name: "sign_up_items",
                schema: "events");

            migrationBuilder.DropIndex(
                name: "ix_sign_up_commitments_sign_up_item_id",
                schema: "events",
                table: "sign_up_commitments");

            migrationBuilder.DropColumn(
                name: "has_mandatory_items",
                schema: "events",
                table: "sign_up_lists");

            migrationBuilder.DropColumn(
                name: "has_preferred_items",
                schema: "events",
                table: "sign_up_lists");

            migrationBuilder.DropColumn(
                name: "has_suggested_items",
                schema: "events",
                table: "sign_up_lists");

            migrationBuilder.DropColumn(
                name: "notes",
                schema: "events",
                table: "sign_up_commitments");

            migrationBuilder.DropColumn(
                name: "sign_up_item_id",
                schema: "events",
                table: "sign_up_commitments");

            migrationBuilder.EnsureSchema(
                name: "payments");

            migrationBuilder.RenameTable(
                name: "sign_up_lists",
                schema: "events",
                newName: "sign_up_lists");

            migrationBuilder.RenameTable(
                name: "sign_up_commitments",
                schema: "events",
                newName: "sign_up_commitments");

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
                name: "event_passes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reserved_quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_quantity = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    price_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    price_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_passes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_event_passes_events_event_id",
                        column: x => x.event_id,
                        principalSchema: "events",
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pass_purchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_pass_id = table.Column<Guid>(type: "uuid", nullable: false),
                    qr_code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_price_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_price_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pass_purchases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "stripe_customers",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    stripe_created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    stripe_customer_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
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
                    attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    event_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    processed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                name: "ix_event_passes_event_id",
                table: "event_passes",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_pass_purchases_event_id",
                table: "pass_purchases",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_pass_purchases_event_pass_id",
                table: "pass_purchases",
                column: "event_pass_id");

            migrationBuilder.CreateIndex(
                name: "ix_pass_purchases_event_user_status",
                table: "pass_purchases",
                columns: new[] { "event_id", "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_pass_purchases_qr_code",
                table: "pass_purchases",
                column: "qr_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pass_purchases_user_id",
                table: "pass_purchases",
                column: "user_id");

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
    }
}
