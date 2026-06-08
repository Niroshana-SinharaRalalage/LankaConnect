using LankaConnect.BuildingBlocks.Infrastructure.Idempotency;
using LankaConnect.BuildingBlocks.Infrastructure.Outbox;
using LankaConnect.Modules.Media.Domain;
using LankaConnect.Modules.Media.Domain.Entities;
using LankaConnect.Modules.Media.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.Modules.Media.Infrastructure.Data;

/// <summary>
/// Module-owned <see cref="DbContext"/> for the Media bounded context.
/// Maps the <c>events.photo_albums</c> + <c>events.album_photos</c> tables
/// (physically owned by legacy AppDbContext migrations
/// <c>20260307222001_AddPhotoAlbumTables</c> and
/// <c>20260329224025_AddVideoSupportToAlbumPhotos</c>).
/// </summary>
/// <remarks>
/// <para>
/// <b>W4.2 cross-schema pattern</b> (architect ruling 2026-06-06): legacy tables
/// remain on the <c>events</c> physical schema; <see cref="MediaDbContext"/>'s
/// own operational tables (<c>media.outbox</c>, <c>media.outbox_dead_letter</c>,
/// <c>media.idempotency_keys</c>, <c>media.__EFMigrationsHistory</c>) live in
/// the <c>media</c> schema. Schema realignment to <c>media.photo_albums</c>
/// is deferred to a coordinated Wave 4.9 schema-realignment pass after local
/// Postgres is available for migration dry-run.
/// </para>
/// <para>
/// Mirrors the W3.5 <see cref="LankaConnect.Modules.Notifications.Infrastructure.Data.NotificationsDbContext"/>
/// baseline pattern — the <c>Baseline_Media</c> migration is empty
/// (history-row-only) for the physical tables since they already exist.
/// </para>
/// </remarks>
public sealed class MediaDbContext : DbContext
{
    /// <summary>Postgres schema for module-owned operational tables.</summary>
    public const string SchemaName = "media";

    /// <summary>
    /// Physical schema where the legacy PhotoAlbum/AlbumPhoto tables live.
    /// Per W4.2 cross-schema pattern, these tables remain owned by the
    /// <c>events</c> schema until Wave 4.9 schema realignment.
    /// </summary>
    public const string LegacyTableSchema = "events";

    public MediaDbContext(DbContextOptions<MediaDbContext> options)
        : base(options)
    {
    }

    public DbSet<PhotoAlbum> PhotoAlbums => Set<PhotoAlbum>();
    public DbSet<AlbumPhoto> AlbumPhotos => Set<AlbumPhoto>();

    /// <summary>Per-module outbox table (<c>media.outbox</c>).</summary>
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();

    /// <summary>Per-module dead-letter table (<c>media.outbox_dead_letter</c>).</summary>
    public DbSet<DeadLetterMessage> OutboxDeadLetter => Set<DeadLetterMessage>();

    /// <summary>Per-module idempotency-keys table (<c>media.idempotency_keys</c>).</summary>
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // Operational tables (outbox, idempotency, dead-letter, history) land in `media` schema.
        modelBuilder.HasDefaultSchema(SchemaName);

        modelBuilder.ApplyConfiguration(new PhotoAlbumConfiguration());
        modelBuilder.ApplyConfiguration(new AlbumPhotoConfiguration());

        // Cross-schema explicit overrides: legacy aggregate tables remain physically
        // in the `events` schema (W4.2 architect ruling 2026-06-06).
        modelBuilder.Entity<PhotoAlbum>().ToTable("photo_albums", LegacyTableSchema);
        modelBuilder.Entity<AlbumPhoto>().ToTable("album_photos", LegacyTableSchema);

        // Module-owned operational tables — `media.outbox`, etc.
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new DeadLetterMessageConfiguration());
        modelBuilder.ApplyConfiguration(new IdempotencyKeyConfiguration());

        // 2026-06-08 hotfix (matches AppDbContext.IgnoreAuditByActorPropertiesUntilPhase1):
        // Phase 1.9 of Wave 4.9 will add physical CreatedBy/UpdatedBy columns to the
        // photo_albums + album_photos tables; until then, ignore the auto-mapped
        // properties so SELECTs don't try to read non-existent columns.
        modelBuilder.Entity<PhotoAlbum>().Ignore("CreatedBy").Ignore("UpdatedBy");
        modelBuilder.Entity<AlbumPhoto>().Ignore("CreatedBy").Ignore("UpdatedBy");

        base.OnModelCreating(modelBuilder);
    }
}
