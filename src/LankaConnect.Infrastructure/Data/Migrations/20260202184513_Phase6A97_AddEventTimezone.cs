using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A97_AddEventTimezone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                schema: "events",
                table: "events",
                type: "text",
                nullable: true);

            // Phase 6A.97: Backfill TimeZoneId for existing US events based on state
            // All events in LankaConnect are in the USA (app serves Sri Lankans living in America)
            migrationBuilder.Sql(@"
                UPDATE events.events e
                SET ""TimeZoneId"" = CASE
                    -- Eastern Time States
                    WHEN UPPER(e.address_state) IN ('OH', 'OHIO', 'NY', 'NEW YORK', 'PA', 'PENNSYLVANIA',
                        'FL', 'FLORIDA', 'GA', 'GEORGIA', 'NC', 'NORTH CAROLINA', 'SC', 'SOUTH CAROLINA',
                        'VA', 'VIRGINIA', 'MI', 'MICHIGAN', 'IN', 'INDIANA', 'KY', 'KENTUCKY',
                        'TN', 'TENNESSEE', 'MA', 'MASSACHUSETTS', 'CT', 'CONNECTICUT', 'NJ', 'NEW JERSEY',
                        'MD', 'MARYLAND', 'DE', 'DELAWARE', 'ME', 'MAINE', 'NH', 'NEW HAMPSHIRE',
                        'VT', 'VERMONT', 'RI', 'RHODE ISLAND', 'DC', 'DISTRICT OF COLUMBIA', 'WV', 'WEST VIRGINIA')
                    THEN 'America/New_York'
                    -- Central Time States
                    WHEN UPPER(e.address_state) IN ('IL', 'ILLINOIS', 'TX', 'TEXAS', 'MN', 'MINNESOTA',
                        'WI', 'WISCONSIN', 'IA', 'IOWA', 'MO', 'MISSOURI', 'AR', 'ARKANSAS',
                        'LA', 'LOUISIANA', 'MS', 'MISSISSIPPI', 'AL', 'ALABAMA', 'OK', 'OKLAHOMA',
                        'KS', 'KANSAS', 'NE', 'NEBRASKA', 'SD', 'SOUTH DAKOTA', 'ND', 'NORTH DAKOTA')
                    THEN 'America/Chicago'
                    -- Mountain Time States
                    WHEN UPPER(e.address_state) IN ('CO', 'COLORADO', 'NM', 'NEW MEXICO', 'UT', 'UTAH',
                        'WY', 'WYOMING', 'MT', 'MONTANA', 'ID', 'IDAHO')
                    THEN 'America/Denver'
                    -- Arizona (no DST)
                    WHEN UPPER(e.address_state) IN ('AZ', 'ARIZONA')
                    THEN 'America/Phoenix'
                    -- Pacific Time States
                    WHEN UPPER(e.address_state) IN ('CA', 'CALIFORNIA', 'WA', 'WASHINGTON', 'OR', 'OREGON',
                        'NV', 'NEVADA')
                    THEN 'America/Los_Angeles'
                    -- Alaska
                    WHEN UPPER(e.address_state) IN ('AK', 'ALASKA')
                    THEN 'America/Anchorage'
                    -- Hawaii
                    WHEN UPPER(e.address_state) IN ('HI', 'HAWAII')
                    THEN 'Pacific/Honolulu'
                    -- Default to Eastern Time (most Sri Lankan communities in USA are on East Coast)
                    ELSE 'America/New_York'
                END
                WHERE e.address_state IS NOT NULL;
            ");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 18, 45, 8, 208, DateTimeKind.Utc).AddTicks(3539));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 18, 45, 8, 208, DateTimeKind.Utc).AddTicks(3669));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 18, 45, 8, 208, DateTimeKind.Utc).AddTicks(3405));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 18, 45, 8, 208, DateTimeKind.Utc).AddTicks(3607));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 18, 45, 8, 208, DateTimeKind.Utc).AddTicks(3638));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 18, 45, 8, 208, DateTimeKind.Utc).AddTicks(3841));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 18, 45, 8, 208, DateTimeKind.Utc).AddTicks(3572));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 18, 45, 8, 208, DateTimeKind.Utc).AddTicks(3499));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 18, 45, 8, 208, DateTimeKind.Utc).AddTicks(3778));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 18, 45, 8, 208, DateTimeKind.Utc).AddTicks(3746));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 18, 45, 8, 208, DateTimeKind.Utc).AddTicks(3702));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 18, 45, 8, 208, DateTimeKind.Utc).AddTicks(3811));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                schema: "events",
                table: "events");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 3, 33, 19, 513, DateTimeKind.Utc).AddTicks(9242));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 3, 33, 19, 513, DateTimeKind.Utc).AddTicks(9328));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 3, 33, 19, 513, DateTimeKind.Utc).AddTicks(9156));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 3, 33, 19, 513, DateTimeKind.Utc).AddTicks(9285));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 3, 33, 19, 513, DateTimeKind.Utc).AddTicks(9309));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 3, 33, 19, 513, DateTimeKind.Utc).AddTicks(9552));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 3, 33, 19, 513, DateTimeKind.Utc).AddTicks(9264));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 3, 33, 19, 513, DateTimeKind.Utc).AddTicks(9201));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 3, 33, 19, 513, DateTimeKind.Utc).AddTicks(9515));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 3, 33, 19, 513, DateTimeKind.Utc).AddTicks(9489));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 3, 33, 19, 513, DateTimeKind.Utc).AddTicks(9347));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 3, 33, 19, 513, DateTimeKind.Utc).AddTicks(9533));
        }
    }
}
