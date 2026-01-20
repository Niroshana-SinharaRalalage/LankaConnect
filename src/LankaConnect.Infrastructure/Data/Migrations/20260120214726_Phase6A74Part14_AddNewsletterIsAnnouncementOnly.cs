using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A74Part14_AddNewsletterIsAnnouncementOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_registrations_valid_format",
                schema: "events",
                table: "registrations");

            migrationBuilder.AddColumn<bool>(
                name: "is_announcement_only",
                schema: "communications",
                table: "newsletters",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 21, 47, 23, 659, DateTimeKind.Utc).AddTicks(7642));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 21, 47, 23, 659, DateTimeKind.Utc).AddTicks(7792));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 21, 47, 23, 659, DateTimeKind.Utc).AddTicks(7585));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 21, 47, 23, 659, DateTimeKind.Utc).AddTicks(7756));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 21, 47, 23, 659, DateTimeKind.Utc).AddTicks(7773));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 21, 47, 23, 659, DateTimeKind.Utc).AddTicks(7877));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 21, 47, 23, 659, DateTimeKind.Utc).AddTicks(7737));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 21, 47, 23, 659, DateTimeKind.Utc).AddTicks(7622));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 21, 47, 23, 659, DateTimeKind.Utc).AddTicks(7845));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 21, 47, 23, 659, DateTimeKind.Utc).AddTicks(7828));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 21, 47, 23, 659, DateTimeKind.Utc).AddTicks(7812));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 21, 47, 23, 659, DateTimeKind.Utc).AddTicks(7862));

            migrationBuilder.AddCheckConstraint(
                name: "ck_registrations_valid_format",
                schema: "events",
                table: "registrations",
                sql: "(\n                    (\"UserId\" IS NOT NULL AND attendee_info IS NULL) OR\n                    (\"UserId\" IS NULL AND attendee_info IS NOT NULL) OR\n                    (attendees IS NOT NULL AND contact IS NOT NULL)\n                )");

            migrationBuilder.CreateIndex(
                name: "ix_newsletters_is_announcement_only",
                schema: "communications",
                table: "newsletters",
                column: "is_announcement_only");

            migrationBuilder.CreateIndex(
                name: "ix_newsletters_status_is_announcement_only",
                schema: "communications",
                table: "newsletters",
                columns: new[] { "status", "is_announcement_only" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_registrations_valid_format",
                schema: "events",
                table: "registrations");

            migrationBuilder.DropIndex(
                name: "ix_newsletters_is_announcement_only",
                schema: "communications",
                table: "newsletters");

            migrationBuilder.DropIndex(
                name: "ix_newsletters_status_is_announcement_only",
                schema: "communications",
                table: "newsletters");

            migrationBuilder.DropColumn(
                name: "is_announcement_only",
                schema: "communications",
                table: "newsletters");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 1, 19, 19, 48, 14, 900, DateTimeKind.Utc).AddTicks(997));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 1, 19, 19, 48, 14, 900, DateTimeKind.Utc).AddTicks(1144));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 1, 19, 19, 48, 14, 900, DateTimeKind.Utc).AddTicks(916));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 1, 19, 19, 48, 14, 900, DateTimeKind.Utc).AddTicks(1058));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 1, 19, 19, 48, 14, 900, DateTimeKind.Utc).AddTicks(1086));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 1, 19, 19, 48, 14, 900, DateTimeKind.Utc).AddTicks(1429));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 1, 19, 19, 48, 14, 900, DateTimeKind.Utc).AddTicks(1027));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 1, 19, 19, 48, 14, 900, DateTimeKind.Utc).AddTicks(966));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 1, 19, 19, 48, 14, 900, DateTimeKind.Utc).AddTicks(1241));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 1, 19, 19, 48, 14, 900, DateTimeKind.Utc).AddTicks(1212));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 1, 19, 19, 48, 14, 900, DateTimeKind.Utc).AddTicks(1174));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 1, 19, 19, 48, 14, 900, DateTimeKind.Utc).AddTicks(1393));

            migrationBuilder.AddCheckConstraint(
                name: "ck_registrations_valid_format",
                schema: "events",
                table: "registrations",
                sql: "(\r\n                    (\"UserId\" IS NOT NULL AND attendee_info IS NULL) OR\r\n                    (\"UserId\" IS NULL AND attendee_info IS NOT NULL) OR\r\n                    (attendees IS NOT NULL AND contact IS NOT NULL)\r\n                )");
        }
    }
}
