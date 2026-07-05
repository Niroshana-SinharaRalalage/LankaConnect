using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <inheritdoc />
    public partial class Phase7FD_AddHeadCountDeltaToRegistrationAddition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "head_count_delta",
                schema: "events",
                table: "registration_additions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "registration_mode",
                schema: "events",
                table: "registration_additions",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            // Phase 7F-D (architect edit #6): CHECK constraint enforcing Mode-A vs Mode-B
            // mutual exclusion at the storage layer. Mode A (registration_mode = 0) MUST
            // have a null head_count_delta (Mode-A path uses _new_attendees jsonb). Mode B
            // (registration_mode > 0) MUST have a non-null head_count_delta. Catches the
            // polymorphic mistake that would otherwise corrupt the discriminator.
            migrationBuilder.Sql(@"
                ALTER TABLE events.registration_additions
                ADD CONSTRAINT ck_registration_additions_mode_xor
                CHECK (
                    (registration_mode = 0 AND head_count_delta IS NULL)
                    OR
                    (registration_mode > 0 AND head_count_delta IS NOT NULL)
                );");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 13, 39, 47, 100, DateTimeKind.Utc).AddTicks(1883));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 13, 39, 47, 100, DateTimeKind.Utc).AddTicks(1951));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 13, 39, 47, 100, DateTimeKind.Utc).AddTicks(1841));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 13, 39, 47, 100, DateTimeKind.Utc).AddTicks(1913));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 13, 39, 47, 100, DateTimeKind.Utc).AddTicks(1929));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 13, 39, 47, 100, DateTimeKind.Utc).AddTicks(2032));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 13, 39, 47, 100, DateTimeKind.Utc).AddTicks(1899));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 13, 39, 47, 100, DateTimeKind.Utc).AddTicks(1867));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 13, 39, 47, 100, DateTimeKind.Utc).AddTicks(1997));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 13, 39, 47, 100, DateTimeKind.Utc).AddTicks(1984));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 13, 39, 47, 100, DateTimeKind.Utc).AddTicks(1966));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 13, 39, 47, 100, DateTimeKind.Utc).AddTicks(2019));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Phase 7F-D: drop the CHECK constraint before the columns it references.
            migrationBuilder.Sql("ALTER TABLE events.registration_additions DROP CONSTRAINT IF EXISTS ck_registration_additions_mode_xor;");

            migrationBuilder.DropColumn(
                name: "head_count_delta",
                schema: "events",
                table: "registration_additions");

            migrationBuilder.DropColumn(
                name: "registration_mode",
                schema: "events",
                table: "registration_additions");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 3, 2, 35, 192, DateTimeKind.Utc).AddTicks(8768));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 3, 2, 35, 192, DateTimeKind.Utc).AddTicks(8866));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 3, 2, 35, 192, DateTimeKind.Utc).AddTicks(8700));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 3, 2, 35, 192, DateTimeKind.Utc).AddTicks(8811));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 3, 2, 35, 192, DateTimeKind.Utc).AddTicks(8831));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 3, 2, 35, 192, DateTimeKind.Utc).AddTicks(9069));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 3, 2, 35, 192, DateTimeKind.Utc).AddTicks(8789));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 3, 2, 35, 192, DateTimeKind.Utc).AddTicks(8740));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 3, 2, 35, 192, DateTimeKind.Utc).AddTicks(8943));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 3, 2, 35, 192, DateTimeKind.Utc).AddTicks(8920));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 3, 2, 35, 192, DateTimeKind.Utc).AddTicks(8888));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 3, 2, 35, 192, DateTimeKind.Utc).AddTicks(8962));
        }
    }
}
