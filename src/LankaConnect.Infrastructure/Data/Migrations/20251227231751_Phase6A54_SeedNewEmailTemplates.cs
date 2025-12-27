using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A54_SeedNewEmailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.CreateTable(
                name: "reference_values",
                schema: "reference_data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    enum_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    int_value = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reference_values", x => x.id);
                });

            migrationBuilder.InsertData(
                schema: "reference_data",
                table: "reference_values",
                columns: new[] { "id", "code", "created_at", "description", "display_order", "enum_type", "int_value", "is_active", "metadata", "name" },
                values: new object[,]
                {
                    { new Guid("0344df2b-671f-4746-bdd1-4da7f9de8b4d"), "Religious", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(8702), null, 1, "EventCategory", 0, true, "{\"iconUrl\":\"\"}", "Religious" },
                    { new Guid("053ab1e2-bef1-41e9-861b-6f17348515e9"), "Archived", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(9131), null, 7, "EventStatus", 6, true, "{\"allowsRegistration\":false,\"isFinalState\":true}", "Archived" },
                    { new Guid("3348075e-3540-42f4-85b5-dc628940a5a9"), "Cancelled", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(9099), null, 5, "EventStatus", 4, true, "{\"allowsRegistration\":false,\"isFinalState\":true}", "Cancelled" },
                    { new Guid("3779825f-bdab-4751-819f-4ff2175fe04f"), "BusinessOwner", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(9264), null, 2, "UserRole", 2, true, "{\"canManageUsers\":false,\"canCreateEvents\":false,\"canModerateContent\":false,\"isEventOrganizer\":false,\"isAdmin\":false,\"requiresSubscription\":true,\"canCreateBusinessProfile\":true,\"canCreatePosts\":false,\"monthlySubscriptionPrice\":10.00}", "Business Owner" },
                    { new Guid("3cc17d1b-2fd3-4109-bb7a-c8fe76f45be9"), "UnderReview", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(9144), null, 8, "EventStatus", 7, true, "{\"allowsRegistration\":false,\"isFinalState\":false}", "Under Review" },
                    { new Guid("3ccab949-3cff-4cec-adef-117703ed4afd"), "Draft", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(9026), null, 1, "EventStatus", 0, true, "{\"allowsRegistration\":false,\"isFinalState\":false}", "Draft" },
                    { new Guid("3e66d4a2-2a78-4935-908d-05b3eb39ddcd"), "EventOrganizer", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(9300), null, 3, "UserRole", 3, true, "{\"canManageUsers\":false,\"canCreateEvents\":true,\"canModerateContent\":false,\"isEventOrganizer\":true,\"isAdmin\":false,\"requiresSubscription\":true,\"canCreateBusinessProfile\":false,\"canCreatePosts\":true,\"monthlySubscriptionPrice\":10.00}", "Event Organizer" },
                    { new Guid("4b7b550c-6887-4783-833a-72b7026c3ef6"), "Postponed", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(9085), null, 4, "EventStatus", 3, true, "{\"allowsRegistration\":false,\"isFinalState\":false}", "Postponed" },
                    { new Guid("5503fb87-17f1-47b7-af23-fd9e5d9b9618"), "GeneralUser", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(9227), null, 1, "UserRole", 1, true, "{\"canManageUsers\":false,\"canCreateEvents\":false,\"canModerateContent\":false,\"isEventOrganizer\":false,\"isAdmin\":false,\"requiresSubscription\":false,\"canCreateBusinessProfile\":false,\"canCreatePosts\":false,\"monthlySubscriptionPrice\":0.00}", "General User" },
                    { new Guid("5a679eb6-dee5-467f-81c1-f988ecea4f50"), "Active", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(9070), null, 3, "EventStatus", 2, true, "{\"allowsRegistration\":true,\"isFinalState\":false}", "Active" },
                    { new Guid("5bb58a1a-d39c-4f59-8ae1-eb3f1dff2b3b"), "Entertainment", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(8910), null, 8, "EventCategory", 7, true, "{\"iconUrl\":\"\"}", "Entertainment" },
                    { new Guid("69eb63f2-6dd6-49a7-ac93-3fc161a85b59"), "Social", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(8860), null, 5, "EventCategory", 4, true, "{\"iconUrl\":\"\"}", "Social" },
                    { new Guid("6c84b73f-40ae-4517-926c-502f59cbb089"), "EventOrganizerAndBusinessOwner", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(9356), null, 4, "UserRole", 4, true, "{\"canManageUsers\":false,\"canCreateEvents\":true,\"canModerateContent\":false,\"isEventOrganizer\":false,\"isAdmin\":false,\"requiresSubscription\":true,\"canCreateBusinessProfile\":true,\"canCreatePosts\":true,\"monthlySubscriptionPrice\":15.00}", "Event Organizer + Business Owner" },
                    { new Guid("7c9df654-fd21-4a1a-abdd-e853a1b5f0d8"), "Educational", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(8848), null, 4, "EventCategory", 3, true, "{\"iconUrl\":\"\"}", "Educational" },
                    { new Guid("93886285-5265-4501-9b0a-2ef075e2708c"), "Completed", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(9118), null, 6, "EventStatus", 5, true, "{\"allowsRegistration\":false,\"isFinalState\":true}", "Completed" },
                    { new Guid("ac12aead-6531-47ac-ba2c-9c65d3ebbe44"), "Published", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(9041), null, 2, "EventStatus", 1, true, "{\"allowsRegistration\":true,\"isFinalState\":false}", "Published" },
                    { new Guid("b99309a3-d195-4a48-a2da-7182fe2ccdfb"), "AdminManager", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(9446), null, 6, "UserRole", 6, true, "{\"canManageUsers\":true,\"canCreateEvents\":true,\"canModerateContent\":true,\"isEventOrganizer\":false,\"isAdmin\":true,\"requiresSubscription\":false,\"canCreateBusinessProfile\":true,\"canCreatePosts\":true,\"monthlySubscriptionPrice\":0.00}", "Admin Manager" },
                    { new Guid("baf10753-9e12-4454-b476-fd537241543d"), "Admin", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(9403), null, 5, "UserRole", 5, true, "{\"canManageUsers\":true,\"canCreateEvents\":true,\"canModerateContent\":true,\"isEventOrganizer\":false,\"isAdmin\":true,\"requiresSubscription\":false,\"canCreateBusinessProfile\":true,\"canCreatePosts\":true,\"monthlySubscriptionPrice\":0.00}", "Administrator" },
                    { new Guid("d1e82f54-9c79-4b3e-a8d8-dd397ead5a65"), "Business", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(8887), null, 6, "EventCategory", 5, true, "{\"iconUrl\":\"\"}", "Business" },
                    { new Guid("db35acb4-5433-4003-a7f3-31ef1065c6cc"), "Community", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(8834), null, 3, "EventCategory", 2, true, "{\"iconUrl\":\"\"}", "Community" },
                    { new Guid("eb16ba17-265f-4dd9-bc9a-c6fd9a10f951"), "Charity", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(8898), null, 7, "EventCategory", 6, true, "{\"iconUrl\":\"\"}", "Charity" },
                    { new Guid("f1e7e1cb-f560-438c-ab76-f30722235605"), "Cultural", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(8816), null, 2, "EventCategory", 1, true, "{\"iconUrl\":\"\"}", "Cultural" }
                });

            migrationBuilder.CreateIndex(
                name: "idx_reference_values_display_order",
                schema: "reference_data",
                table: "reference_values",
                column: "display_order");

            migrationBuilder.CreateIndex(
                name: "idx_reference_values_enum_type",
                schema: "reference_data",
                table: "reference_values",
                column: "enum_type");

            migrationBuilder.CreateIndex(
                name: "idx_reference_values_is_active",
                schema: "reference_data",
                table: "reference_values",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "uq_reference_values_type_code",
                schema: "reference_data",
                table: "reference_values",
                columns: new[] { "enum_type", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_reference_values_type_int_value",
                schema: "reference_data",
                table: "reference_values",
                columns: new[] { "enum_type", "int_value" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reference_values",
                schema: "reference_data");

            migrationBuilder.CreateTable(
                name: "event_categories",
                schema: "reference_data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    icon_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_final_state = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                    can_create_business_profile = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    can_create_events = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    can_create_posts = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    can_manage_users = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    can_moderate_content = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    monthly_price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0.00m),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    requires_approval = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    requires_subscription = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
    }
}
