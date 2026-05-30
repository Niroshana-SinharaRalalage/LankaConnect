using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A156_AddSponsorshipPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "included_ticket_count_snapshot",
                schema: "events",
                table: "sponsors",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "package_name_snapshot",
                schema: "events",
                table: "sponsors",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "package_price_amount_snapshot",
                schema: "events",
                table: "sponsors",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "package_price_currency_snapshot",
                schema: "events",
                table: "sponsors",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "package_tier_snapshot",
                schema: "events",
                table: "sponsors",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "registration_id",
                schema: "events",
                table: "sponsors",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "sponsorship_package_id",
                schema: "events",
                table: "sponsors",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "sponsorship_packages",
                schema: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    price_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    price_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    quantity_limit = table.Column<int>(type: "integer", nullable: true),
                    quantity_sold = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    image_blob_name = table.Column<string>(type: "text", nullable: true),
                    tier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    perks = table.Column<List<string>>(type: "text[]", nullable: true),
                    included_ticket_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sponsorship_packages", x => x.id);
                    table.ForeignKey(
                        name: "FK_sponsorship_packages_events_event_id",
                        column: x => x.event_id,
                        principalSchema: "events",
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 5, 30, 15, 38, 2, 445, DateTimeKind.Utc).AddTicks(5164));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 5, 30, 15, 38, 2, 445, DateTimeKind.Utc).AddTicks(5227));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 5, 30, 15, 38, 2, 445, DateTimeKind.Utc).AddTicks(5114));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 5, 30, 15, 38, 2, 445, DateTimeKind.Utc).AddTicks(5197));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 5, 30, 15, 38, 2, 445, DateTimeKind.Utc).AddTicks(5213));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 5, 30, 15, 38, 2, 445, DateTimeKind.Utc).AddTicks(5321));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 5, 30, 15, 38, 2, 445, DateTimeKind.Utc).AddTicks(5179));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 5, 30, 15, 38, 2, 445, DateTimeKind.Utc).AddTicks(5147));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 5, 30, 15, 38, 2, 445, DateTimeKind.Utc).AddTicks(5287));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 5, 30, 15, 38, 2, 445, DateTimeKind.Utc).AddTicks(5272));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 5, 30, 15, 38, 2, 445, DateTimeKind.Utc).AddTicks(5244));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 5, 30, 15, 38, 2, 445, DateTimeKind.Utc).AddTicks(5301));

            migrationBuilder.CreateIndex(
                name: "ix_sponsors_registration_id",
                schema: "events",
                table: "sponsors",
                column: "registration_id",
                filter: "\"registration_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_sponsors_sponsorship_package_id",
                schema: "events",
                table: "sponsors",
                column: "sponsorship_package_id",
                filter: "\"sponsorship_package_id\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "chk_sponsors_package_snapshot",
                schema: "events",
                table: "sponsors",
                sql: "(sponsorship_package_id IS NULL) OR (package_name_snapshot IS NOT NULL AND package_price_amount_snapshot IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "ix_sponsorship_packages_event_active_sort",
                schema: "events",
                table: "sponsorship_packages",
                columns: new[] { "event_id", "is_active", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_sponsorship_packages_event_id",
                schema: "events",
                table: "sponsorship_packages",
                column: "event_id");

            migrationBuilder.AddForeignKey(
                name: "FK_sponsors_registrations_registration_id",
                schema: "events",
                table: "sponsors",
                column: "registration_id",
                principalSchema: "events",
                principalTable: "registrations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_sponsors_sponsorship_packages_sponsorship_package_id",
                schema: "events",
                table: "sponsors",
                column: "sponsorship_package_id",
                principalSchema: "events",
                principalTable: "sponsorship_packages",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_sponsors_registrations_registration_id",
                schema: "events",
                table: "sponsors");

            migrationBuilder.DropForeignKey(
                name: "FK_sponsors_sponsorship_packages_sponsorship_package_id",
                schema: "events",
                table: "sponsors");

            migrationBuilder.DropTable(
                name: "sponsorship_packages",
                schema: "events");

            migrationBuilder.DropIndex(
                name: "ix_sponsors_registration_id",
                schema: "events",
                table: "sponsors");

            migrationBuilder.DropIndex(
                name: "ix_sponsors_sponsorship_package_id",
                schema: "events",
                table: "sponsors");

            migrationBuilder.DropCheckConstraint(
                name: "chk_sponsors_package_snapshot",
                schema: "events",
                table: "sponsors");

            migrationBuilder.DropColumn(
                name: "included_ticket_count_snapshot",
                schema: "events",
                table: "sponsors");

            migrationBuilder.DropColumn(
                name: "package_name_snapshot",
                schema: "events",
                table: "sponsors");

            migrationBuilder.DropColumn(
                name: "package_price_amount_snapshot",
                schema: "events",
                table: "sponsors");

            migrationBuilder.DropColumn(
                name: "package_price_currency_snapshot",
                schema: "events",
                table: "sponsors");

            migrationBuilder.DropColumn(
                name: "package_tier_snapshot",
                schema: "events",
                table: "sponsors");

            migrationBuilder.DropColumn(
                name: "registration_id",
                schema: "events",
                table: "sponsors");

            migrationBuilder.DropColumn(
                name: "sponsorship_package_id",
                schema: "events",
                table: "sponsors");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 5, 29, 0, 46, 18, 940, DateTimeKind.Utc).AddTicks(1873));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 5, 29, 0, 46, 18, 940, DateTimeKind.Utc).AddTicks(1988));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 5, 29, 0, 46, 18, 940, DateTimeKind.Utc).AddTicks(1799));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 5, 29, 0, 46, 18, 940, DateTimeKind.Utc).AddTicks(1921));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 5, 29, 0, 46, 18, 940, DateTimeKind.Utc).AddTicks(1944));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 5, 29, 0, 46, 18, 940, DateTimeKind.Utc).AddTicks(2146));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 5, 29, 0, 46, 18, 940, DateTimeKind.Utc).AddTicks(1894));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 5, 29, 0, 46, 18, 940, DateTimeKind.Utc).AddTicks(1846));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 5, 29, 0, 46, 18, 940, DateTimeKind.Utc).AddTicks(2083));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 5, 29, 0, 46, 18, 940, DateTimeKind.Utc).AddTicks(2059));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 5, 29, 0, 46, 18, 940, DateTimeKind.Utc).AddTicks(2012));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 5, 29, 0, 46, 18, 940, DateTimeKind.Utc).AddTicks(2124));
        }
    }
}
