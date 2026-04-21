using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSignUpItemDisplayOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "display_order",
                schema: "events",
                table: "sign_up_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Phase 6A.132: Backfill DisplayOrder for pre-existing rows using the authoritative
            // insertion order (created_at, id as tiebreaker). Without this, every existing row
            // would have display_order = 0 and `ORDER BY display_order` would return a random
            // permutation. row_number() is 1-based; subtract 1 so the aggregate's zero-based
            // invariant (first item = 0) holds across fresh and backfilled data.
            migrationBuilder.Sql(@"
                UPDATE events.sign_up_items AS target
                SET display_order = ordered.new_order
                FROM (
                    SELECT id,
                           (row_number() OVER (PARTITION BY sign_up_list_id ORDER BY created_at, id) - 1)::int AS new_order
                    FROM events.sign_up_items
                ) AS ordered
                WHERE target.id = ordered.id;
            ");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 4, 1, 50, 884, DateTimeKind.Utc).AddTicks(8294));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 4, 1, 50, 884, DateTimeKind.Utc).AddTicks(8419));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 4, 1, 50, 884, DateTimeKind.Utc).AddTicks(8204));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 4, 1, 50, 884, DateTimeKind.Utc).AddTicks(8349));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 4, 1, 50, 884, DateTimeKind.Utc).AddTicks(8376));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 4, 1, 50, 884, DateTimeKind.Utc).AddTicks(8555));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 4, 1, 50, 884, DateTimeKind.Utc).AddTicks(8319));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 4, 1, 50, 884, DateTimeKind.Utc).AddTicks(8267));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 4, 1, 50, 884, DateTimeKind.Utc).AddTicks(8504));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 4, 1, 50, 884, DateTimeKind.Utc).AddTicks(8479));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 4, 1, 50, 884, DateTimeKind.Utc).AddTicks(8443));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 4, 1, 50, 884, DateTimeKind.Utc).AddTicks(8529));

            migrationBuilder.CreateIndex(
                name: "ix_sign_up_items_list_id_display_order",
                schema: "events",
                table: "sign_up_items",
                columns: new[] { "sign_up_list_id", "display_order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_sign_up_items_list_id_display_order",
                schema: "events",
                table: "sign_up_items");

            migrationBuilder.DropColumn(
                name: "display_order",
                schema: "events",
                table: "sign_up_items");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 2, 30, 6, 49, DateTimeKind.Utc).AddTicks(4883));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 2, 30, 6, 49, DateTimeKind.Utc).AddTicks(4986));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 2, 30, 6, 49, DateTimeKind.Utc).AddTicks(4805));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 2, 30, 6, 49, DateTimeKind.Utc).AddTicks(4929));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 2, 30, 6, 49, DateTimeKind.Utc).AddTicks(4952));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 2, 30, 6, 49, DateTimeKind.Utc).AddTicks(5100));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 2, 30, 6, 49, DateTimeKind.Utc).AddTicks(4906));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 2, 30, 6, 49, DateTimeKind.Utc).AddTicks(4858));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 2, 30, 6, 49, DateTimeKind.Utc).AddTicks(5057));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 2, 30, 6, 49, DateTimeKind.Utc).AddTicks(5034));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 2, 30, 6, 49, DateTimeKind.Utc).AddTicks(5007));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 2, 30, 6, 49, DateTimeKind.Utc).AddTicks(5078));
        }
    }
}
