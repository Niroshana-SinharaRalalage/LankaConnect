using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A47_Create_ReferenceData_Tables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "reference_data");

            migrationBuilder.CreateTable(
                name: "event_categories",
                schema: "reference_data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    icon_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "event_statuses",
                schema: "reference_data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    allows_registration = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_final_state = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                schema: "reference_data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    can_manage_users = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    can_create_events = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    can_moderate_content = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    can_create_business_profile = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    can_create_posts = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    requires_subscription = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    monthly_price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0.00m),
                    requires_approval = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_event_categories_code",
                schema: "reference_data",
                table: "event_categories",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_event_categories_display_order",
                schema: "reference_data",
                table: "event_categories",
                column: "display_order");

            migrationBuilder.CreateIndex(
                name: "idx_event_categories_is_active",
                schema: "reference_data",
                table: "event_categories",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "idx_event_categories_name",
                schema: "reference_data",
                table: "event_categories",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "idx_event_statuses_allows_registration",
                schema: "reference_data",
                table: "event_statuses",
                column: "allows_registration");

            migrationBuilder.CreateIndex(
                name: "idx_event_statuses_code",
                schema: "reference_data",
                table: "event_statuses",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_event_statuses_display_order",
                schema: "reference_data",
                table: "event_statuses",
                column: "display_order");

            migrationBuilder.CreateIndex(
                name: "idx_event_statuses_is_active",
                schema: "reference_data",
                table: "event_statuses",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "idx_user_roles_can_create_events",
                schema: "reference_data",
                table: "user_roles",
                column: "can_create_events");

            migrationBuilder.CreateIndex(
                name: "idx_user_roles_code",
                schema: "reference_data",
                table: "user_roles",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_user_roles_display_order",
                schema: "reference_data",
                table: "user_roles",
                column: "display_order");

            migrationBuilder.CreateIndex(
                name: "idx_user_roles_is_active",
                schema: "reference_data",
                table: "user_roles",
                column: "is_active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_categories",
                schema: "reference_data");

            migrationBuilder.DropTable(
                name: "event_statuses",
                schema: "reference_data");

            migrationBuilder.DropTable(
                name: "user_roles",
                schema: "reference_data");
        }
    }
}
