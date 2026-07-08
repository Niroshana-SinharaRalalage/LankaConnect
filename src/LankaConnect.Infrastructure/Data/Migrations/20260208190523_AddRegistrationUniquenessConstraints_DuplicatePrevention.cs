using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationUniquenessConstraints_DuplicatePrevention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 2, 8, 19, 5, 19, 73, DateTimeKind.Utc).AddTicks(8262));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 2, 8, 19, 5, 19, 73, DateTimeKind.Utc).AddTicks(8386));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 2, 8, 19, 5, 19, 73, DateTimeKind.Utc).AddTicks(8115));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 2, 8, 19, 5, 19, 73, DateTimeKind.Utc).AddTicks(8325));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 2, 8, 19, 5, 19, 73, DateTimeKind.Utc).AddTicks(8355));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 2, 8, 19, 5, 19, 73, DateTimeKind.Utc).AddTicks(8765));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 2, 8, 19, 5, 19, 73, DateTimeKind.Utc).AddTicks(8291));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 2, 8, 19, 5, 19, 73, DateTimeKind.Utc).AddTicks(8202));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 2, 8, 19, 5, 19, 73, DateTimeKind.Utc).AddTicks(8473));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 2, 8, 19, 5, 19, 73, DateTimeKind.Utc).AddTicks(8445));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 2, 8, 19, 5, 19, 73, DateTimeKind.Utc).AddTicks(8415));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 2, 8, 19, 5, 19, 73, DateTimeKind.Utc).AddTicks(8709));

            migrationBuilder.CreateIndex(
                name: "uix_registrations_event_user_active",
                schema: "events",
                table: "registrations",
                columns: new[] { "EventId", "UserId" },
                unique: true,
                filter: "\"UserId\" IS NOT NULL AND \"Status\" NOT IN ('Cancelled', 'Refunded', 'RefundRequested', 'Abandoned', 'Preliminary', 'Pending')");

            // JSONB expression index for email-based dedup (EF Core doesn't support JSONB indexes natively)
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX uix_registrations_event_email_active
                ON events.registrations (""EventId"", (contact->>'email'))
                WHERE contact IS NOT NULL
                  AND contact->>'email' IS NOT NULL
                  AND ""Status"" NOT IN ('Cancelled', 'Refunded', 'RefundRequested', 'Abandoned', 'Preliminary', 'Pending');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uix_registrations_event_user_active",
                schema: "events",
                table: "registrations");

            migrationBuilder.Sql(@"DROP INDEX IF EXISTS events.uix_registrations_event_email_active;");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 2, 5, 22, 31, 36, 900, DateTimeKind.Utc).AddTicks(8554));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 2, 5, 22, 31, 36, 900, DateTimeKind.Utc).AddTicks(8679));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 2, 5, 22, 31, 36, 900, DateTimeKind.Utc).AddTicks(8510));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 2, 5, 22, 31, 36, 900, DateTimeKind.Utc).AddTicks(8646));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 2, 5, 22, 31, 36, 900, DateTimeKind.Utc).AddTicks(8663));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 2, 5, 22, 31, 36, 900, DateTimeKind.Utc).AddTicks(8752));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 2, 5, 22, 31, 36, 900, DateTimeKind.Utc).AddTicks(8569));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 2, 5, 22, 31, 36, 900, DateTimeKind.Utc).AddTicks(8536));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 2, 5, 22, 31, 36, 900, DateTimeKind.Utc).AddTicks(8723));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 2, 5, 22, 31, 36, 900, DateTimeKind.Utc).AddTicks(8709));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 2, 5, 22, 31, 36, 900, DateTimeKind.Utc).AddTicks(8694));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 2, 5, 22, 31, 36, 900, DateTimeKind.Utc).AddTicks(8739));
        }
    }
}
