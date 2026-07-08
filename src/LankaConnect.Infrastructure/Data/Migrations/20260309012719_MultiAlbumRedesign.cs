using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MultiAlbumRedesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add Name as NULLABLE first (safe for existing rows - per MEMORY.md Phase 6A.123)
            migrationBuilder.AddColumn<string>(
                name: "Name",
                schema: "events",
                table: "photo_albums",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            // Step 2: Backfill existing rows with EventTitle
            migrationBuilder.Sql(
                @"UPDATE events.photo_albums SET ""Name"" = COALESCE(""EventTitle"", 'Untitled Album') WHERE ""Name"" IS NULL;");

            // Step 3: Set NOT NULL
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "events",
                table: "photo_albums",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            // Step 4: Migrate Closed ? Published status
            migrationBuilder.Sql(
                @"UPDATE events.photo_albums SET ""Status"" = 'Published' WHERE ""Status"" = 'Closed';");

            // Step 5: Migrate non-Approved photo statuses ? Approved
            migrationBuilder.Sql(
                @"UPDATE events.album_photos SET ""Status"" = 'Approved' WHERE ""Status"" != 'Approved';");

            // Step 6: Drop old unique index on EventId (single-album constraint)
            migrationBuilder.DropIndex(
                name: "IX_photo_albums_EventId",
                schema: "events",
                table: "photo_albums");

            // Step 7: Drop removed columns
            migrationBuilder.DropColumn(
                name: "ClosedAt",
                schema: "events",
                table: "photo_albums");

            migrationBuilder.DropColumn(
                name: "ModerationMode",
                schema: "events",
                table: "photo_albums");

            migrationBuilder.DropColumn(
                name: "UploadPermission",
                schema: "events",
                table: "photo_albums");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 3, 9, 1, 27, 17, 31, DateTimeKind.Utc).AddTicks(7564));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 3, 9, 1, 27, 17, 31, DateTimeKind.Utc).AddTicks(7634));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 3, 9, 1, 27, 17, 31, DateTimeKind.Utc).AddTicks(7452));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 3, 9, 1, 27, 17, 31, DateTimeKind.Utc).AddTicks(7596));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 3, 9, 1, 27, 17, 31, DateTimeKind.Utc).AddTicks(7612));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 3, 9, 1, 27, 17, 31, DateTimeKind.Utc).AddTicks(7711));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 3, 9, 1, 27, 17, 31, DateTimeKind.Utc).AddTicks(7581));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 3, 9, 1, 27, 17, 31, DateTimeKind.Utc).AddTicks(7478));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 3, 9, 1, 27, 17, 31, DateTimeKind.Utc).AddTicks(7682));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 3, 9, 1, 27, 17, 31, DateTimeKind.Utc).AddTicks(7668));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 3, 9, 1, 27, 17, 31, DateTimeKind.Utc).AddTicks(7649));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 3, 9, 1, 27, 17, 31, DateTimeKind.Utc).AddTicks(7697));

            migrationBuilder.CreateIndex(
                name: "IX_photo_albums_EventId_Name",
                schema: "events",
                table: "photo_albums",
                columns: new[] { "EventId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_photo_albums_EventId_Name",
                schema: "events",
                table: "photo_albums");

            migrationBuilder.DropColumn(
                name: "Name",
                schema: "events",
                table: "photo_albums");

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                schema: "events",
                table: "photo_albums",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModerationMode",
                schema: "events",
                table: "photo_albums",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "PostModeration");

            migrationBuilder.AddColumn<string>(
                name: "UploadPermission",
                schema: "events",
                table: "photo_albums",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "OrganizerOnly");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 23, 10, 25, 36, DateTimeKind.Utc).AddTicks(6521));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 23, 10, 25, 36, DateTimeKind.Utc).AddTicks(6687));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 23, 10, 25, 36, DateTimeKind.Utc).AddTicks(6461));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 23, 10, 25, 36, DateTimeKind.Utc).AddTicks(6640));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 23, 10, 25, 36, DateTimeKind.Utc).AddTicks(6658));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 23, 10, 25, 36, DateTimeKind.Utc).AddTicks(6787));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 23, 10, 25, 36, DateTimeKind.Utc).AddTicks(6618));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 23, 10, 25, 36, DateTimeKind.Utc).AddTicks(6497));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 23, 10, 25, 36, DateTimeKind.Utc).AddTicks(6751));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 23, 10, 25, 36, DateTimeKind.Utc).AddTicks(6733));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 23, 10, 25, 36, DateTimeKind.Utc).AddTicks(6708));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 23, 10, 25, 36, DateTimeKind.Utc).AddTicks(6768));

            migrationBuilder.CreateIndex(
                name: "IX_photo_albums_EventId",
                schema: "events",
                table: "photo_albums",
                column: "EventId",
                unique: true);
        }
    }
}
