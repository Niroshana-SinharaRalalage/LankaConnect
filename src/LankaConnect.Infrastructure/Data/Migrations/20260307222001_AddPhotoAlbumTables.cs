using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPhotoAlbumTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "photo_albums",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizerId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Draft"),
                    UploadPermission = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "OrganizerOnly"),
                    ModerationMode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "PostModeration"),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CoverPhotoUrl = table.Column<string>(type: "text", nullable: true),
                    RetentionDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 7),
                    PhotoCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_photo_albums", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "album_photos",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlbumId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploaderId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploaderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OriginalUrl = table.Column<string>(type: "text", nullable: false),
                    OriginalBlobName = table.Column<string>(type: "text", nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "text", nullable: false),
                    ThumbnailBlobName = table.Column<string>(type: "text", nullable: false),
                    MediumUrl = table.Column<string>(type: "text", nullable: false),
                    MediumBlobName = table.Column<string>(type: "text", nullable: false),
                    Caption = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Approved"),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_album_photos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_album_photos_photo_albums_AlbumId",
                        column: x => x.AlbumId,
                        principalSchema: "events",
                        principalTable: "photo_albums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 19, 59, 807, DateTimeKind.Utc).AddTicks(6635));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 19, 59, 807, DateTimeKind.Utc).AddTicks(6700));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 19, 59, 807, DateTimeKind.Utc).AddTicks(6578));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 19, 59, 807, DateTimeKind.Utc).AddTicks(6668));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 19, 59, 807, DateTimeKind.Utc).AddTicks(6684));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 19, 59, 807, DateTimeKind.Utc).AddTicks(6845));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 19, 59, 807, DateTimeKind.Utc).AddTicks(6652));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 19, 59, 807, DateTimeKind.Utc).AddTicks(6616));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 19, 59, 807, DateTimeKind.Utc).AddTicks(6816));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 19, 59, 807, DateTimeKind.Utc).AddTicks(6798));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 19, 59, 807, DateTimeKind.Utc).AddTicks(6728));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 19, 59, 807, DateTimeKind.Utc).AddTicks(6830));

            migrationBuilder.CreateIndex(
                name: "IX_album_photos_AlbumId",
                schema: "events",
                table: "album_photos",
                column: "AlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_album_photos_AlbumId_Status",
                schema: "events",
                table: "album_photos",
                columns: new[] { "AlbumId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_album_photos_AlbumId_UploadedAt",
                schema: "events",
                table: "album_photos",
                columns: new[] { "AlbumId", "UploadedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_album_photos_ExpiresAt",
                schema: "events",
                table: "album_photos",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_photo_albums_EventId",
                schema: "events",
                table: "photo_albums",
                column: "EventId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "album_photos",
                schema: "events");

            migrationBuilder.DropTable(
                name: "photo_albums",
                schema: "events");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 16, 35, 307, DateTimeKind.Utc).AddTicks(5087));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 16, 35, 307, DateTimeKind.Utc).AddTicks(5147));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 16, 35, 307, DateTimeKind.Utc).AddTicks(5044));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 16, 35, 307, DateTimeKind.Utc).AddTicks(5117));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 16, 35, 307, DateTimeKind.Utc).AddTicks(5133));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 16, 35, 307, DateTimeKind.Utc).AddTicks(5268));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 16, 35, 307, DateTimeKind.Utc).AddTicks(5102));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 16, 35, 307, DateTimeKind.Utc).AddTicks(5071));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 16, 35, 307, DateTimeKind.Utc).AddTicks(5240));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 16, 35, 307, DateTimeKind.Utc).AddTicks(5226));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 16, 35, 307, DateTimeKind.Utc).AddTicks(5161));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 16, 35, 307, DateTimeKind.Utc).AddTicks(5254));
        }
    }
}
