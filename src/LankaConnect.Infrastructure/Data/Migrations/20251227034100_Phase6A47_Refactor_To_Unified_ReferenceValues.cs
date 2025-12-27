using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A47_Refactor_To_Unified_ReferenceValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Create unified reference_values table
            migrationBuilder.CreateTable(
                name: "reference_values",
                schema: "reference_data",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    enum_type = table.Column<string>(maxLength: 100, nullable: false),
                    code = table.Column<string>(maxLength: 100, nullable: false),
                    int_value = table.Column<int>(nullable: false),
                    name = table.Column<string>(maxLength: 255, nullable: false),
                    description = table.Column<string>(nullable: true),
                    display_order = table.Column<int>(nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(nullable: false, defaultValue: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reference_values", x => x.id);
                    table.UniqueConstraint("uq_reference_values_type_int_value", x => new { x.enum_type, x.int_value });
                    table.UniqueConstraint("uq_reference_values_type_code", x => new { x.enum_type, x.code });
                    table.CheckConstraint("ck_reference_values_enum_type",
                        "enum_type IN ('EventCategory', 'EventStatus', 'UserRole')");
                });

            // Step 2: Create indexes
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
                name: "idx_reference_values_display_order",
                schema: "reference_data",
                table: "reference_values",
                column: "display_order");

            migrationBuilder.Sql(@"
                CREATE INDEX idx_reference_values_metadata
                ON reference_data.reference_values USING GIN (metadata);
            ");

            // Step 3: Migrate data from event_categories
            migrationBuilder.Sql(@"
                INSERT INTO reference_data.reference_values
                    (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
                SELECT
                    id,
                    'EventCategory' as enum_type,
                    code,
                    -- Map code to int_value based on enum definition
                    CASE code
                        WHEN 'Religious' THEN 0
                        WHEN 'Cultural' THEN 1
                        WHEN 'Community' THEN 2
                        WHEN 'Educational' THEN 3
                        WHEN 'Social' THEN 4
                        WHEN 'Business' THEN 5
                        WHEN 'Charity' THEN 6
                        WHEN 'Entertainment' THEN 7
                        ELSE 0
                    END as int_value,
                    name,
                    description,
                    display_order,
                    is_active,
                    jsonb_build_object('iconUrl', icon_url) as metadata,
                    created_at,
                    updated_at
                FROM reference_data.event_categories;
            ");

            // Step 4: Migrate data from event_statuses
            migrationBuilder.Sql(@"
                INSERT INTO reference_data.reference_values
                    (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
                SELECT
                    id,
                    'EventStatus' as enum_type,
                    code,
                    -- Map code to int_value based on enum definition
                    CASE code
                        WHEN 'Draft' THEN 0
                        WHEN 'Published' THEN 1
                        WHEN 'Active' THEN 2
                        WHEN 'Postponed' THEN 3
                        WHEN 'Cancelled' THEN 4
                        WHEN 'Completed' THEN 5
                        WHEN 'Archived' THEN 6
                        WHEN 'UnderReview' THEN 7
                        ELSE 0
                    END as int_value,
                    name,
                    description,
                    display_order,
                    is_active,
                    jsonb_build_object(
                        'allowsRegistration', allows_registration,
                        'isFinalState', is_final_state
                    ) as metadata,
                    created_at,
                    updated_at
                FROM reference_data.event_statuses;
            ");

            // Step 5: Migrate data from user_roles
            migrationBuilder.Sql(@"
                INSERT INTO reference_data.reference_values
                    (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
                SELECT
                    id,
                    'UserRole' as enum_type,
                    code,
                    -- Map code to int_value based on enum definition
                    CASE code
                        WHEN 'GeneralUser' THEN 1
                        WHEN 'BusinessOwner' THEN 2
                        WHEN 'EventOrganizer' THEN 3
                        WHEN 'EventOrganizerAndBusinessOwner' THEN 4
                        WHEN 'Admin' THEN 5
                        WHEN 'AdminManager' THEN 6
                        ELSE 1
                    END as int_value,
                    name,
                    description,
                    display_order,
                    is_active,
                    jsonb_build_object(
                        'canManageUsers', can_manage_users,
                        'canCreateEvents', can_create_events,
                        'canModerateContent', can_moderate_content,
                        'isEventOrganizer', is_event_organizer,
                        'isAdmin', is_admin,
                        'requiresSubscription', requires_subscription,
                        'canCreateBusinessProfile', can_create_business_profile,
                        'canCreatePosts', can_create_posts,
                        'monthlySubscriptionPrice', monthly_price
                    ) as metadata,
                    created_at,
                    updated_at
                FROM reference_data.user_roles;
            ");

            // Step 6: Drop old tables
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Step 1: Recreate event_categories table
            migrationBuilder.CreateTable(
                name: "event_categories",
                schema: "reference_data",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    code = table.Column<string>(maxLength: 100, nullable: false),
                    name = table.Column<string>(maxLength: 255, nullable: false),
                    description = table.Column<string>(nullable: true),
                    display_order = table.Column<int>(nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(nullable: false, defaultValue: true),
                    icon_url = table.Column<string>(nullable: true),
                    created_at = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_categories", x => x.id);
                    table.UniqueConstraint("uq_event_categories_code", x => x.code);
                });

            // Step 2: Recreate event_statuses table
            migrationBuilder.CreateTable(
                name: "event_statuses",
                schema: "reference_data",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    code = table.Column<string>(maxLength: 100, nullable: false),
                    name = table.Column<string>(maxLength: 255, nullable: false),
                    description = table.Column<string>(nullable: true),
                    display_order = table.Column<int>(nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(nullable: false, defaultValue: true),
                    allows_registration = table.Column<bool>(nullable: false),
                    is_final_state = table.Column<bool>(nullable: false),
                    created_at = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_statuses", x => x.id);
                    table.UniqueConstraint("uq_event_statuses_code", x => x.code);
                });

            // Step 3: Recreate user_roles table
            migrationBuilder.CreateTable(
                name: "user_roles",
                schema: "reference_data",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    code = table.Column<string>(maxLength: 100, nullable: false),
                    name = table.Column<string>(maxLength: 255, nullable: false),
                    description = table.Column<string>(nullable: true),
                    display_order = table.Column<int>(nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(nullable: false, defaultValue: true),
                    can_manage_users = table.Column<bool>(nullable: false),
                    can_create_events = table.Column<bool>(nullable: false),
                    can_moderate_content = table.Column<bool>(nullable: false),
                    is_event_organizer = table.Column<bool>(nullable: false),
                    is_admin = table.Column<bool>(nullable: false),
                    requires_subscription = table.Column<bool>(nullable: false),
                    can_create_business_profile = table.Column<bool>(nullable: false),
                    can_create_posts = table.Column<bool>(nullable: false),
                    monthly_price = table.Column<decimal>(nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => x.id);
                    table.UniqueConstraint("uq_user_roles_code", x => x.code);
                });

            // Step 4: Restore data to event_categories
            migrationBuilder.Sql(@"
                INSERT INTO reference_data.event_categories
                    (id, code, name, description, display_order, is_active, icon_url, created_at, updated_at)
                SELECT
                    id,
                    code,
                    name,
                    description,
                    display_order,
                    is_active,
                    metadata->>'iconUrl' as icon_url,
                    created_at,
                    updated_at
                FROM reference_data.reference_values
                WHERE enum_type = 'EventCategory';
            ");

            // Step 5: Restore data to event_statuses
            migrationBuilder.Sql(@"
                INSERT INTO reference_data.event_statuses
                    (id, code, name, description, display_order, is_active, allows_registration, is_final_state, created_at, updated_at)
                SELECT
                    id,
                    code,
                    name,
                    description,
                    display_order,
                    is_active,
                    (metadata->>'allowsRegistration')::boolean as allows_registration,
                    (metadata->>'isFinalState')::boolean as is_final_state,
                    created_at,
                    updated_at
                FROM reference_data.reference_values
                WHERE enum_type = 'EventStatus';
            ");

            // Step 6: Restore data to user_roles
            migrationBuilder.Sql(@"
                INSERT INTO reference_data.user_roles
                    (id, code, name, description, display_order, is_active, can_manage_users, can_create_events,
                     can_moderate_content, is_event_organizer, is_admin, requires_subscription,
                     can_create_business_profile, can_create_posts, monthly_price, created_at, updated_at)
                SELECT
                    id,
                    code,
                    name,
                    description,
                    display_order,
                    is_active,
                    (metadata->>'canManageUsers')::boolean as can_manage_users,
                    (metadata->>'canCreateEvents')::boolean as can_create_events,
                    (metadata->>'canModerateContent')::boolean as can_moderate_content,
                    (metadata->>'isEventOrganizer')::boolean as is_event_organizer,
                    (metadata->>'isAdmin')::boolean as is_admin,
                    (metadata->>'requiresSubscription')::boolean as requires_subscription,
                    (metadata->>'canCreateBusinessProfile')::boolean as can_create_business_profile,
                    (metadata->>'canCreatePosts')::boolean as can_create_posts,
                    (metadata->>'monthlySubscriptionPrice')::decimal as monthly_price,
                    created_at,
                    updated_at
                FROM reference_data.reference_values
                WHERE enum_type = 'UserRole';
            ");

            // Step 7: Drop unified table
            migrationBuilder.DropTable(
                name: "reference_values",
                schema: "reference_data");

            // Step 8: Recreate indexes on old tables
            migrationBuilder.CreateIndex(
                name: "idx_event_categories_code",
                schema: "reference_data",
                table: "event_categories",
                column: "code");

            migrationBuilder.CreateIndex(
                name: "idx_event_statuses_code",
                schema: "reference_data",
                table: "event_statuses",
                column: "code");

            migrationBuilder.CreateIndex(
                name: "idx_user_roles_code",
                schema: "reference_data",
                table: "user_roles",
                column: "code");
        }
    }
}
