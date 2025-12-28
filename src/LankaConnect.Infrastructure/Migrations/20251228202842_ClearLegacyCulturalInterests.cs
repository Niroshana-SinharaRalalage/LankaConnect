using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LankaConnect.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ClearLegacyCulturalInterests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.47: Clear legacy hardcoded cultural interests
            // Users will re-select interests from new EventCategory-based system
            migrationBuilder.Sql("DELETE FROM users.user_cultural_interests;");

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
                    { new Guid("1263a35a-381b-415f-b36c-d2c9b82767a2"), "GeneralUser", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7936), null, 1, "UserRole", 1, true, "{\"canManageUsers\":false,\"canCreateEvents\":false,\"canModerateContent\":false,\"isEventOrganizer\":false,\"isAdmin\":false,\"requiresSubscription\":false,\"canCreateBusinessProfile\":false,\"canCreatePosts\":false,\"monthlySubscriptionPrice\":0.00}", "General User" },
                    { new Guid("18f71846-cfa7-4f20-a9d8-24667fda36f0"), "Draft", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7865), null, 1, "EventStatus", 0, true, "{\"allowsRegistration\":false,\"isFinalState\":false}", "Draft" },
                    { new Guid("1da1b9eb-b439-4a6c-b08a-de487bbf16f9"), "Active", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7875), null, 3, "EventStatus", 2, true, "{\"allowsRegistration\":true,\"isFinalState\":false}", "Active" },
                    { new Guid("26684714-c077-4e67-930b-575faecc6ded"), "Cancelled", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7886), null, 5, "EventStatus", 4, true, "{\"allowsRegistration\":false,\"isFinalState\":true}", "Cancelled" },
                    { new Guid("380b0b07-2810-4497-b13c-9805aa5fa41a"), "BusinessOwner", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7950), null, 2, "UserRole", 2, true, "{\"canManageUsers\":false,\"canCreateEvents\":false,\"canModerateContent\":false,\"isEventOrganizer\":false,\"isAdmin\":false,\"requiresSubscription\":true,\"canCreateBusinessProfile\":true,\"canCreatePosts\":false,\"monthlySubscriptionPrice\":10.00}", "Business Owner" },
                    { new Guid("3a09df11-5c25-474b-a2bf-ce90d6dd6c20"), "Published", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7870), null, 2, "EventStatus", 1, true, "{\"allowsRegistration\":true,\"isFinalState\":false}", "Published" },
                    { new Guid("4003c27a-72c8-4899-a27b-c361cfa25aba"), "Business", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7797), null, 6, "EventCategory", 5, true, "{\"iconUrl\":\"\"}", "Business" },
                    { new Guid("4e83018d-39dd-41b1-9781-7ffe93b5ed5f"), "Postponed", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7880), null, 4, "EventStatus", 3, true, "{\"allowsRegistration\":false,\"isFinalState\":false}", "Postponed" },
                    { new Guid("53ce4686-daed-49d1-bc59-c7a776f24149"), "Cultural", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7777), null, 2, "EventCategory", 1, true, "{\"iconUrl\":\"\"}", "Cultural" },
                    { new Guid("579fca6a-65b2-424f-949b-93e14c213327"), "Archived", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7896), null, 7, "EventStatus", 6, true, "{\"allowsRegistration\":false,\"isFinalState\":true}", "Archived" },
                    { new Guid("59fe3162-c1d4-40bc-a00d-0d670ad5634f"), "Admin", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7996), null, 5, "UserRole", 5, true, "{\"canManageUsers\":true,\"canCreateEvents\":true,\"canModerateContent\":true,\"isEventOrganizer\":false,\"isAdmin\":true,\"requiresSubscription\":false,\"canCreateBusinessProfile\":true,\"canCreatePosts\":true,\"monthlySubscriptionPrice\":0.00}", "Administrator" },
                    { new Guid("60655c1a-ba9f-4632-9f92-f2269b7e2541"), "Charity", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7811), null, 7, "EventCategory", 6, true, "{\"iconUrl\":\"\"}", "Charity" },
                    { new Guid("7aab297e-44d1-4dc8-8126-616c9b7e8690"), "Social", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7792), null, 5, "EventCategory", 4, true, "{\"iconUrl\":\"\"}", "Social" },
                    { new Guid("8ea00faf-36e5-4f87-ab2b-4d1ce65102c4"), "Completed", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7891), null, 6, "EventStatus", 5, true, "{\"allowsRegistration\":false,\"isFinalState\":true}", "Completed" },
                    { new Guid("8f373b66-11f9-460c-9e96-655cbcd2cdcd"), "Community", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7783), null, 3, "EventCategory", 2, true, "{\"iconUrl\":\"\"}", "Community" },
                    { new Guid("bc7a07cd-8d99-4869-b711-d1d003fe0185"), "EventOrganizer", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7969), null, 3, "UserRole", 3, true, "{\"canManageUsers\":false,\"canCreateEvents\":true,\"canModerateContent\":false,\"isEventOrganizer\":true,\"isAdmin\":false,\"requiresSubscription\":true,\"canCreateBusinessProfile\":false,\"canCreatePosts\":true,\"monthlySubscriptionPrice\":10.00}", "Event Organizer" },
                    { new Guid("cb665c73-0949-4348-aa82-5033bd136316"), "UnderReview", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7904), null, 8, "EventStatus", 7, true, "{\"allowsRegistration\":false,\"isFinalState\":false}", "Under Review" },
                    { new Guid("d0afebb9-6db1-48b8-a251-87e3e008eab2"), "Educational", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7788), null, 4, "EventCategory", 3, true, "{\"iconUrl\":\"\"}", "Educational" },
                    { new Guid("d1810c17-b408-4a1e-b85a-81f573a6b670"), "Religious", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7762), null, 1, "EventCategory", 0, true, "{\"iconUrl\":\"\"}", "Religious" },
                    { new Guid("d6fd2868-c5cf-424b-9422-3b56e8241ffe"), "EventOrganizerAndBusinessOwner", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7983), null, 4, "UserRole", 4, true, "{\"canManageUsers\":false,\"canCreateEvents\":true,\"canModerateContent\":false,\"isEventOrganizer\":false,\"isAdmin\":false,\"requiresSubscription\":true,\"canCreateBusinessProfile\":true,\"canCreatePosts\":true,\"monthlySubscriptionPrice\":15.00}", "Event Organizer + Business Owner" },
                    { new Guid("d9d84eda-15a3-4455-8048-a6a756a8422b"), "AdminManager", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(8009), null, 6, "UserRole", 6, true, "{\"canManageUsers\":true,\"canCreateEvents\":true,\"canModerateContent\":true,\"isEventOrganizer\":false,\"isAdmin\":true,\"requiresSubscription\":false,\"canCreateBusinessProfile\":true,\"canCreatePosts\":true,\"monthlySubscriptionPrice\":0.00}", "Admin Manager" },
                    { new Guid("f2a13555-346f-46c3-8690-db61c1d01ff4"), "Entertainment", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7825), null, 8, "EventCategory", 7, true, "{\"iconUrl\":\"\"}", "Entertainment" }
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
