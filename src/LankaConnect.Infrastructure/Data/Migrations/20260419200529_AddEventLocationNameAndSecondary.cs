using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventLocationNameAndSecondary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "has_secondary_location",
                schema: "events",
                table: "events",
                type: "boolean",
                nullable: true,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "location_name",
                schema: "events",
                table: "events",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "secondary_address_city",
                schema: "events",
                table: "events",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "secondary_address_country",
                schema: "events",
                table: "events",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "secondary_address_state",
                schema: "events",
                table: "events",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "secondary_address_street",
                schema: "events",
                table: "events",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "secondary_address_zip_code",
                schema: "events",
                table: "events",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "secondary_coordinates_latitude",
                schema: "events",
                table: "events",
                type: "numeric(10,7)",
                precision: 10,
                scale: 7,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "secondary_coordinates_longitude",
                schema: "events",
                table: "events",
                type: "numeric(10,7)",
                precision: 10,
                scale: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "secondary_location_name",
                schema: "events",
                table: "events",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "secondary_location_type",
                schema: "events",
                table: "events",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 20, 5, 26, 975, DateTimeKind.Utc).AddTicks(6880));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 20, 5, 26, 975, DateTimeKind.Utc).AddTicks(6941));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 20, 5, 26, 975, DateTimeKind.Utc).AddTicks(6823));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 20, 5, 26, 975, DateTimeKind.Utc).AddTicks(6916));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 20, 5, 26, 975, DateTimeKind.Utc).AddTicks(6929));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 20, 5, 26, 975, DateTimeKind.Utc).AddTicks(7002));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 20, 5, 26, 975, DateTimeKind.Utc).AddTicks(6893));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 20, 5, 26, 975, DateTimeKind.Utc).AddTicks(6864));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 20, 5, 26, 975, DateTimeKind.Utc).AddTicks(6979));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 20, 5, 26, 975, DateTimeKind.Utc).AddTicks(6967));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 20, 5, 26, 975, DateTimeKind.Utc).AddTicks(6955));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 4, 19, 20, 5, 26, 975, DateTimeKind.Utc).AddTicks(6989));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "has_secondary_location",
                schema: "events",
                table: "events");

            migrationBuilder.DropColumn(
                name: "location_name",
                schema: "events",
                table: "events");

            migrationBuilder.DropColumn(
                name: "secondary_address_city",
                schema: "events",
                table: "events");

            migrationBuilder.DropColumn(
                name: "secondary_address_country",
                schema: "events",
                table: "events");

            migrationBuilder.DropColumn(
                name: "secondary_address_state",
                schema: "events",
                table: "events");

            migrationBuilder.DropColumn(
                name: "secondary_address_street",
                schema: "events",
                table: "events");

            migrationBuilder.DropColumn(
                name: "secondary_address_zip_code",
                schema: "events",
                table: "events");

            migrationBuilder.DropColumn(
                name: "secondary_coordinates_latitude",
                schema: "events",
                table: "events");

            migrationBuilder.DropColumn(
                name: "secondary_coordinates_longitude",
                schema: "events",
                table: "events");

            migrationBuilder.DropColumn(
                name: "secondary_location_name",
                schema: "events",
                table: "events");

            migrationBuilder.DropColumn(
                name: "secondary_location_type",
                schema: "events",
                table: "events");

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
        }
    }
}
