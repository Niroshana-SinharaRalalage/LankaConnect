using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSeatingVenueLayouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "seating_mode",
                schema: "events",
                table: "events",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "GeneralAdmission");

            migrationBuilder.AddColumn<Guid>(
                name: "venue_layout_id",
                schema: "events",
                table: "events",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "seat_holds",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    seat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    held_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seat_holds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "seat_reservations",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    seat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attendee_index = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seat_reservations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "venue_layouts",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    layout_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_template = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_venue_layouts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "venue_zones",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    venue_layout_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ticket_tier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_venue_zones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_venue_zones_venue_layouts_venue_layout_id",
                        column: x => x.venue_layout_id,
                        principalSchema: "events",
                        principalTable: "venue_layouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "seats",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    venue_zone_id = table.Column<Guid>(type: "uuid", nullable: false),
                    row = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    label = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_accessible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    x = table.Column<double>(type: "double precision", nullable: true),
                    y = table.Column<double>(type: "double precision", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_seats_venue_zones_venue_zone_id",
                        column: x => x.venue_zone_id,
                        principalSchema: "events",
                        principalTable: "venue_zones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 4, 17, 19, 3, 35, 610, DateTimeKind.Utc).AddTicks(8204));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 4, 17, 19, 3, 35, 610, DateTimeKind.Utc).AddTicks(8269));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 4, 17, 19, 3, 35, 610, DateTimeKind.Utc).AddTicks(8166));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 4, 17, 19, 3, 35, 610, DateTimeKind.Utc).AddTicks(8231));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 4, 17, 19, 3, 35, 610, DateTimeKind.Utc).AddTicks(8243));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 4, 17, 19, 3, 35, 610, DateTimeKind.Utc).AddTicks(8341));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 4, 17, 19, 3, 35, 610, DateTimeKind.Utc).AddTicks(8217));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 4, 17, 19, 3, 35, 610, DateTimeKind.Utc).AddTicks(8190));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 4, 17, 19, 3, 35, 610, DateTimeKind.Utc).AddTicks(8313));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 4, 17, 19, 3, 35, 610, DateTimeKind.Utc).AddTicks(8301));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 4, 17, 19, 3, 35, 610, DateTimeKind.Utc).AddTicks(8283));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 4, 17, 19, 3, 35, 610, DateTimeKind.Utc).AddTicks(8326));

            migrationBuilder.CreateIndex(
                name: "ix_seat_holds_seat_id_active",
                schema: "events",
                table: "seat_holds",
                column: "seat_id",
                unique: true,
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "ix_seat_holds_session_id",
                schema: "events",
                table: "seat_holds",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "ix_seat_holds_status_expires_at",
                schema: "events",
                table: "seat_holds",
                columns: new[] { "status", "expires_at" },
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "ix_seat_holds_user_id",
                schema: "events",
                table: "seat_holds",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_seat_reservations_event_id",
                schema: "events",
                table: "seat_reservations",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_seat_reservations_registration_id",
                schema: "events",
                table: "seat_reservations",
                column: "registration_id");

            migrationBuilder.CreateIndex(
                name: "ix_seat_reservations_seat_id",
                schema: "events",
                table: "seat_reservations",
                column: "seat_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_seats_venue_zone_id",
                schema: "events",
                table: "seats",
                column: "venue_zone_id");

            migrationBuilder.CreateIndex(
                name: "ix_seats_zone_id_label",
                schema: "events",
                table: "seats",
                columns: new[] { "venue_zone_id", "label" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_venue_layouts_created_by_user_id",
                schema: "events",
                table: "venue_layouts",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_venue_layouts_event_id",
                schema: "events",
                table: "venue_layouts",
                column: "event_id",
                filter: "event_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_venue_layouts_event_id_name",
                schema: "events",
                table: "venue_layouts",
                columns: new[] { "event_id", "name" },
                unique: true,
                filter: "event_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_venue_zones_layout_id_name",
                schema: "events",
                table: "venue_zones",
                columns: new[] { "venue_layout_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_venue_zones_ticket_tier_id",
                schema: "events",
                table: "venue_zones",
                column: "ticket_tier_id",
                filter: "ticket_tier_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_venue_zones_venue_layout_id",
                schema: "events",
                table: "venue_zones",
                column: "venue_layout_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "seat_holds",
                schema: "events");

            migrationBuilder.DropTable(
                name: "seat_reservations",
                schema: "events");

            migrationBuilder.DropTable(
                name: "seats",
                schema: "events");

            migrationBuilder.DropTable(
                name: "venue_zones",
                schema: "events");

            migrationBuilder.DropTable(
                name: "venue_layouts",
                schema: "events");

            migrationBuilder.DropColumn(
                name: "seating_mode",
                schema: "events",
                table: "events");

            migrationBuilder.DropColumn(
                name: "venue_layout_id",
                schema: "events",
                table: "events");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 4, 15, 20, 37, 47, 445, DateTimeKind.Utc).AddTicks(6920));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 4, 15, 20, 37, 47, 445, DateTimeKind.Utc).AddTicks(7035));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 4, 15, 20, 37, 47, 445, DateTimeKind.Utc).AddTicks(6849));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 4, 15, 20, 37, 47, 445, DateTimeKind.Utc).AddTicks(6987));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 4, 15, 20, 37, 47, 445, DateTimeKind.Utc).AddTicks(7011));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 4, 15, 20, 37, 47, 445, DateTimeKind.Utc).AddTicks(7151));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 4, 15, 20, 37, 47, 445, DateTimeKind.Utc).AddTicks(6963));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 4, 15, 20, 37, 47, 445, DateTimeKind.Utc).AddTicks(6894));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 4, 15, 20, 37, 47, 445, DateTimeKind.Utc).AddTicks(7104));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 4, 15, 20, 37, 47, 445, DateTimeKind.Utc).AddTicks(7082));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 4, 15, 20, 37, 47, 445, DateTimeKind.Utc).AddTicks(7059));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 4, 15, 20, 37, 47, 445, DateTimeKind.Utc).AddTicks(7128));
        }
    }
}
