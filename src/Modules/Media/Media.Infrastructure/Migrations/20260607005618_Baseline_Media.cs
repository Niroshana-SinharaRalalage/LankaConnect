using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Modules.Media.Infrastructure.Migrations
{
    /// <summary>
    /// Phase A W4.2 BASELINE migration for <see cref="LankaConnect.Modules.Media.Infrastructure.Data.MediaDbContext"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Mirrors the W3.5 Notifications baseline pattern, with one extension: the
    /// W4.2 architect ruling (2026-06-06) said NEW operational tables
    /// (outbox / outbox_dead_letter / idempotency_keys in the <c>media</c>
    /// schema) ship in this baseline, while the LEGACY aggregate tables
    /// (<c>events.photo_albums</c> + <c>events.album_photos</c>) remain owned
    /// by the original AppDbContext migrations and are NOT recreated here.
    /// </para>
    /// <para>
    /// <b>Legacy aggregate tables — DO NOT CREATE:</b>
    /// <c>events.photo_albums</c> + <c>events.album_photos</c> + their indexes +
    /// the FK + the unique constraint on (EventId, Name) were created by the
    /// AppDbContext migrations:
    /// <list type="bullet">
    ///   <item><c>20260307222001_AddPhotoAlbumTables</c></item>
    ///   <item><c>20260329224025_AddVideoSupportToAlbumPhotos</c></item>
    /// </list>
    /// MediaDbContext maps those tables via cross-schema <c>ToTable(...)</c>
    /// overrides; the EF model snapshot in the companion <c>.Designer.cs</c>
    /// captures their shape so subsequent migrations can diff cleanly, but
    /// the physical DDL is owned by the legacy assembly until Wave 4.9 schema
    /// realignment.
    /// </para>
    /// <para>
    /// <b>Deployment notes</b>:
    /// </para>
    /// <list type="bullet">
    ///   <item>On staging/prod databases where the legacy tables ALREADY exist:
    ///   running <c>dotnet ef database update --context MediaDbContext</c>
    ///   executes the Up() below — which creates <c>media.outbox</c>,
    ///   <c>media.outbox_dead_letter</c>, <c>media.idempotency_keys</c>, and the
    ///   <c>media.__EFMigrationsHistory</c> row. The legacy tables are
    ///   untouched.</item>
    ///   <item>On a fresh local dev database: the AppDbContext migration chain
    ///   must run FIRST to create <c>events.photo_albums</c> + <c>events.album_photos</c>,
    ///   THEN MediaDbContext's <c>Baseline_Media</c> runs to create the
    ///   operational tables.</item>
    /// </list>
    /// </remarks>
    public partial class Baseline_Media : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "media");

            migrationBuilder.CreateTable(
                name: "idempotency_keys",
                schema: "media",
                columns: table => new
                {
                    Key = table.Column<Guid>(type: "uuid", nullable: false),
                    SerializedResponse = table.Column<string>(type: "jsonb", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_keys", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "outbox",
                schema: "media",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_dead_letter",
                schema: "media",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalOutboxId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeadLetteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_dead_letter", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_idempotency_keys_expires_at",
                schema: "media",
                table: "idempotency_keys",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_pending_occurred_at",
                schema: "media",
                table: "outbox",
                column: "OccurredAt",
                filter: "\"ProcessedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_dead_letter_dead_lettered_at",
                schema: "media",
                table: "outbox_dead_letter",
                column: "DeadLetteredAt");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_dead_letter_original_outbox_id",
                schema: "media",
                table: "outbox_dead_letter",
                column: "OriginalOutboxId");

            // events.photo_albums + events.album_photos + their indexes/FK/unique-constraint
            // are intentionally NOT created here — owned by legacy AppDbContext migrations
            // 20260307222001_AddPhotoAlbumTables and 20260329224025_AddVideoSupportToAlbumPhotos.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "idempotency_keys",
                schema: "media");

            migrationBuilder.DropTable(
                name: "outbox",
                schema: "media");

            migrationBuilder.DropTable(
                name: "outbox_dead_letter",
                schema: "media");

            // events.photo_albums + events.album_photos NOT dropped — owned by legacy migrations.
        }
    }
}
