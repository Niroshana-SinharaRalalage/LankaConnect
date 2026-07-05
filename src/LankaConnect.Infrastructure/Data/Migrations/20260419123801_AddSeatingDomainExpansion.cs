using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <inheritdoc />
    public partial class AddSeatingDomainExpansion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_seats_venue_zone_id",
                schema: "events",
                table: "seats");

            migrationBuilder.DropIndex(
                name: "ix_seats_zone_id_label",
                schema: "events",
                table: "seats");

            migrationBuilder.AddColumn<string>(
                name: "geometry",
                schema: "events",
                table: "venue_zones",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<string>(
                name: "shape",
                schema: "events",
                table: "venue_zones",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Rect");

            migrationBuilder.AddColumn<string>(
                name: "canvas_bg_color",
                schema: "events",
                table: "venue_layouts",
                type: "character varying(9)",
                maxLength: 9,
                nullable: false,
                defaultValue: "#ffffff");

            migrationBuilder.AddColumn<int>(
                name: "canvas_height",
                schema: "events",
                table: "venue_layouts",
                type: "integer",
                nullable: false,
                defaultValue: 800);

            migrationBuilder.AddColumn<double>(
                name: "canvas_scale",
                schema: "events",
                table: "venue_layouts",
                type: "double precision",
                nullable: false,
                defaultValue: 1.0);

            migrationBuilder.AddColumn<int>(
                name: "canvas_width",
                schema: "events",
                table: "venue_layouts",
                type: "integer",
                nullable: false,
                defaultValue: 1200);

            migrationBuilder.AlterColumn<Guid>(
                name: "venue_zone_id",
                schema: "events",
                table: "seats",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<double>(
                name: "angle_deg",
                schema: "events",
                table: "seats",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "venue_table_id",
                schema: "events",
                table: "seats",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "venue_decorations",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    venue_layout_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    geometry = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    properties = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_venue_decorations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_venue_decorations_venue_layouts_venue_layout_id",
                        column: x => x.venue_layout_id,
                        principalSchema: "events",
                        principalTable: "venue_layouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "venue_tables",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    venue_layout_id = table.Column<Guid>(type: "uuid", nullable: false),
                    venue_zone_id = table.Column<Guid>(type: "uuid", nullable: true),
                    label = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    shape = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    geometry = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_venue_tables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_venue_tables_venue_layouts_venue_layout_id",
                        column: x => x.venue_layout_id,
                        principalSchema: "events",
                        principalTable: "venue_layouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 12, 37, 55, 357, DateTimeKind.Utc).AddTicks(7999));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 12, 37, 55, 357, DateTimeKind.Utc).AddTicks(8286));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 12, 37, 55, 357, DateTimeKind.Utc).AddTicks(7854));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 12, 37, 55, 357, DateTimeKind.Utc).AddTicks(8103));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 12, 37, 55, 357, DateTimeKind.Utc).AddTicks(8153));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 12, 37, 55, 357, DateTimeKind.Utc).AddTicks(8569));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 12, 37, 55, 357, DateTimeKind.Utc).AddTicks(8052));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 12, 37, 55, 357, DateTimeKind.Utc).AddTicks(7945));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 12, 37, 55, 357, DateTimeKind.Utc).AddTicks(8470));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 12, 37, 55, 357, DateTimeKind.Utc).AddTicks(8418));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 12, 37, 55, 357, DateTimeKind.Utc).AddTicks(8339));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 12, 37, 55, 357, DateTimeKind.Utc).AddTicks(8519));

            migrationBuilder.CreateIndex(
                name: "ix_seats_table_id_label",
                schema: "events",
                table: "seats",
                columns: new[] { "venue_table_id", "label" },
                unique: true,
                filter: "venue_table_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_seats_venue_table_id",
                schema: "events",
                table: "seats",
                column: "venue_table_id",
                filter: "venue_table_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_seats_venue_zone_id",
                schema: "events",
                table: "seats",
                column: "venue_zone_id",
                filter: "venue_zone_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_seats_zone_id_label",
                schema: "events",
                table: "seats",
                columns: new[] { "venue_zone_id", "label" },
                unique: true,
                filter: "venue_zone_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_venue_decorations_venue_layout_id",
                schema: "events",
                table: "venue_decorations",
                column: "venue_layout_id");

            migrationBuilder.CreateIndex(
                name: "ix_venue_tables_layout_id_label",
                schema: "events",
                table: "venue_tables",
                columns: new[] { "venue_layout_id", "label" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_venue_tables_venue_layout_id",
                schema: "events",
                table: "venue_tables",
                column: "venue_layout_id");

            migrationBuilder.CreateIndex(
                name: "ix_venue_tables_venue_zone_id",
                schema: "events",
                table: "venue_tables",
                column: "venue_zone_id",
                filter: "venue_zone_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_seats_venue_tables_venue_table_id",
                schema: "events",
                table: "seats",
                column: "venue_table_id",
                principalSchema: "events",
                principalTable: "venue_tables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // Slice 2+3A (architect decision #13): enforce XOR at the DB level —
            // a seat belongs to exactly ONE parent (zone OR table), never both,
            // never neither. Domain factories guard the same invariant; this CHECK
            // is the last line of defence against hand-written SQL / future bugs.
            migrationBuilder.Sql(@"
                ALTER TABLE events.seats
                ADD CONSTRAINT ck_seats_zone_xor_table
                CHECK ((venue_zone_id IS NULL) <> (venue_table_id IS NULL));
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop XOR CHECK constraint first — it references the zone_id / table_id columns.
            migrationBuilder.Sql("ALTER TABLE events.seats DROP CONSTRAINT IF EXISTS ck_seats_zone_xor_table;");

            migrationBuilder.DropForeignKey(
                name: "FK_seats_venue_tables_venue_table_id",
                schema: "events",
                table: "seats");

            migrationBuilder.DropTable(
                name: "venue_decorations",
                schema: "events");

            migrationBuilder.DropTable(
                name: "venue_tables",
                schema: "events");

            migrationBuilder.DropIndex(
                name: "ix_seats_table_id_label",
                schema: "events",
                table: "seats");

            migrationBuilder.DropIndex(
                name: "ix_seats_venue_table_id",
                schema: "events",
                table: "seats");

            migrationBuilder.DropIndex(
                name: "ix_seats_venue_zone_id",
                schema: "events",
                table: "seats");

            migrationBuilder.DropIndex(
                name: "ix_seats_zone_id_label",
                schema: "events",
                table: "seats");

            migrationBuilder.DropColumn(
                name: "geometry",
                schema: "events",
                table: "venue_zones");

            migrationBuilder.DropColumn(
                name: "shape",
                schema: "events",
                table: "venue_zones");

            migrationBuilder.DropColumn(
                name: "canvas_bg_color",
                schema: "events",
                table: "venue_layouts");

            migrationBuilder.DropColumn(
                name: "canvas_height",
                schema: "events",
                table: "venue_layouts");

            migrationBuilder.DropColumn(
                name: "canvas_scale",
                schema: "events",
                table: "venue_layouts");

            migrationBuilder.DropColumn(
                name: "canvas_width",
                schema: "events",
                table: "venue_layouts");

            migrationBuilder.DropColumn(
                name: "angle_deg",
                schema: "events",
                table: "seats");

            migrationBuilder.DropColumn(
                name: "venue_table_id",
                schema: "events",
                table: "seats");

            migrationBuilder.AlterColumn<Guid>(
                name: "venue_zone_id",
                schema: "events",
                table: "seats",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

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
        }
    }
}
