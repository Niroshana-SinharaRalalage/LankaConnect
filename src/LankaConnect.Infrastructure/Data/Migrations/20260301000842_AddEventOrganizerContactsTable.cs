using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventOrganizerContactsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Create the new table FIRST (before dropping old columns)
            migrationBuilder.CreateTable(
                name: "event_organizer_contacts",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    contact_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    contact_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    linked_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_organizer_contacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_event_organizer_contacts_events_event_id",
                        column: x => x.event_id,
                        principalSchema: "events",
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Step 2: Migrate existing organizer contact data from old columns to new table
            // Only migrate events that have organizer_contact_name set (non-null, non-empty)
            migrationBuilder.Sql(@"
                INSERT INTO events.event_organizer_contacts (
                    ""Id"", event_id, contact_name, contact_email, contact_phone,
                    is_primary, sort_order, created_at
                )
                SELECT
                    gen_random_uuid(),
                    ""Id"",
                    organizer_contact_name,
                    organizer_contact_email,
                    organizer_contact_phone,
                    true,
                    0,
                    NOW()
                FROM events.events
                WHERE organizer_contact_name IS NOT NULL
                  AND TRIM(organizer_contact_name) <> ''
            ");

            // Step 3: Drop old scalar columns (data is now in the new table)
            migrationBuilder.DropColumn(
                name: "organizer_contact_email",
                schema: "events",
                table: "events");

            migrationBuilder.DropColumn(
                name: "organizer_contact_name",
                schema: "events",
                table: "events");

            migrationBuilder.DropColumn(
                name: "organizer_contact_phone",
                schema: "events",
                table: "events");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 3, 1, 0, 8, 39, 261, DateTimeKind.Utc).AddTicks(8575));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 3, 1, 0, 8, 39, 261, DateTimeKind.Utc).AddTicks(9070));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 3, 1, 0, 8, 39, 261, DateTimeKind.Utc).AddTicks(8378));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 3, 1, 0, 8, 39, 261, DateTimeKind.Utc).AddTicks(8689));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 3, 1, 0, 8, 39, 261, DateTimeKind.Utc).AddTicks(8966));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 3, 1, 0, 8, 39, 261, DateTimeKind.Utc).AddTicks(9357));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 3, 1, 0, 8, 39, 261, DateTimeKind.Utc).AddTicks(8631));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 3, 1, 0, 8, 39, 261, DateTimeKind.Utc).AddTicks(8511));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 3, 1, 0, 8, 39, 261, DateTimeKind.Utc).AddTicks(9255));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 3, 1, 0, 8, 39, 261, DateTimeKind.Utc).AddTicks(9202));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 3, 1, 0, 8, 39, 261, DateTimeKind.Utc).AddTicks(9125));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 3, 1, 0, 8, 39, 261, DateTimeKind.Utc).AddTicks(9307));

            migrationBuilder.CreateIndex(
                name: "ix_event_organizer_contacts_event_id",
                schema: "events",
                table: "event_organizer_contacts",
                column: "event_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_organizer_contacts",
                schema: "events");

            migrationBuilder.AddColumn<string>(
                name: "organizer_contact_email",
                schema: "events",
                table: "events",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "organizer_contact_name",
                schema: "events",
                table: "events",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "organizer_contact_phone",
                schema: "events",
                table: "events",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 2, 28, 5, 33, 39, 149, DateTimeKind.Utc).AddTicks(5040));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 2, 28, 5, 33, 39, 149, DateTimeKind.Utc).AddTicks(5192));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 2, 28, 5, 33, 39, 149, DateTimeKind.Utc).AddTicks(4939));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 2, 28, 5, 33, 39, 149, DateTimeKind.Utc).AddTicks(5105));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 2, 28, 5, 33, 39, 149, DateTimeKind.Utc).AddTicks(5137));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 2, 28, 5, 33, 39, 149, DateTimeKind.Utc).AddTicks(5516));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 2, 28, 5, 33, 39, 149, DateTimeKind.Utc).AddTicks(5072));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 2, 28, 5, 33, 39, 149, DateTimeKind.Utc).AddTicks(5004));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 2, 28, 5, 33, 39, 149, DateTimeKind.Utc).AddTicks(5454));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 2, 28, 5, 33, 39, 149, DateTimeKind.Utc).AddTicks(5416));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 2, 28, 5, 33, 39, 149, DateTimeKind.Utc).AddTicks(5357));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 2, 28, 5, 33, 39, 149, DateTimeKind.Utc).AddTicks(5486));
        }
    }
}
