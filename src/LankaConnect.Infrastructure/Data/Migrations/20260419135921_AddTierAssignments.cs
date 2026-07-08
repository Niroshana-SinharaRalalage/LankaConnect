using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTierAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tier_assignments",
                schema: "events",
                columns: table => new
                {
                    tier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignable_kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    assignable_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tier_assignments", x => new { x.tier_id, x.assignable_kind, x.assignable_id });
                    table.ForeignKey(
                        name: "FK_tier_assignments_ticket_tiers_tier_id",
                        column: x => x.tier_id,
                        principalTable: "ticket_tiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 13, 59, 17, 71, DateTimeKind.Utc).AddTicks(2438));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 13, 59, 17, 71, DateTimeKind.Utc).AddTicks(2509));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 13, 59, 17, 71, DateTimeKind.Utc).AddTicks(2387));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 13, 59, 17, 71, DateTimeKind.Utc).AddTicks(2469));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 13, 59, 17, 71, DateTimeKind.Utc).AddTicks(2485));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 13, 59, 17, 71, DateTimeKind.Utc).AddTicks(2716));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 13, 59, 17, 71, DateTimeKind.Utc).AddTicks(2454));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 13, 59, 17, 71, DateTimeKind.Utc).AddTicks(2420));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 13, 59, 17, 71, DateTimeKind.Utc).AddTicks(2564));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 13, 59, 17, 71, DateTimeKind.Utc).AddTicks(2550));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 13, 59, 17, 71, DateTimeKind.Utc).AddTicks(2526));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 13, 59, 17, 71, DateTimeKind.Utc).AddTicks(2696));

            migrationBuilder.CreateIndex(
                name: "ix_tier_assignments_assignable",
                schema: "events",
                table: "tier_assignments",
                columns: new[] { "assignable_kind", "assignable_id" });

            // Slice 4 Release N — backfill tier_assignments from the legacy venue_zones.ticket_tier_id column.
            // The column stays nullable in DB (Release N dual-read window); Release N+1 drops it after
            // =1 week in production with no rollback triggered (architect decision #11).
            // ON CONFLICT DO NOTHING guards against re-apply on environments where backfill already ran.
            // NOTE: "Id" is quoted because venue_zones.Id was created without HasColumnName override,
            // so EF's default naming produced a mixed-case quoted identifier at table-creation time.
            migrationBuilder.Sql(@"
                INSERT INTO events.tier_assignments (tier_id, assignable_kind, assignable_id, created_at)
                SELECT ticket_tier_id, 'Zone', ""Id"", NOW()
                FROM events.venue_zones
                WHERE ticket_tier_id IS NOT NULL
                ON CONFLICT DO NOTHING;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tier_assignments",
                schema: "events");

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
        }
    }
}
