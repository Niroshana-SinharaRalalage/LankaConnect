using LankaConnect.BuildingBlocks.Infrastructure.Idempotency;
using LankaConnect.BuildingBlocks.Infrastructure.Outbox;
using LankaConnect.Modules.Media.Domain;
using LankaConnect.Modules.Media.Domain.Entities;
using LankaConnect.Modules.Media.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;
namespace LankaConnect.Modules.Media.Infrastructure.Data;

/// <summary>
/// Module-owned <see cref="DbContext"/> for the Media bounded context.
/// Maps <c>media.photo_albums</c> + <c>media.album_photos</c> (renamed
/// from the legacy <c>events.*</c> schema by Wave 4.9.3 architect ruling
/// 2026-06-09 via <c>ALTER TABLE ... SET SCHEMA</c>).
/// </summary>
/// <remarks>
/// <para>
/// Wave 4.9.3 (2026-06-09) closed the W4.2 cross-schema deferral: both legacy
/// aggregate tables physically moved from <c>events</c> to <c>media</c>. The
/// previous <c>LegacyTableSchema</c> constant + <c>ToTable(..., LegacyTableSchema)</c>
/// overrides have been removed; PhotoAlbum / AlbumPhoto now live under the
/// default schema (<c>media</c>, per <see cref="HasDefaultSchema"/>).
/// </para>
/// <para>
/// Operational tables (<c>media.outbox</c>, <c>media.outbox_dead_letter</c>,
/// <c>media.idempotency_keys</c>, <c>media.__EFMigrationsHistory</c>) were
/// already in the <c>media</c> schema since W4.2 baseline.
/// </para>
/// </remarks>
public sealed class MediaDbContext : DbContext
{
    /// <summary>Postgres schema for all Media-owned tables (post-Wave 4.9.3).</summary>
    public const string SchemaName = "media";

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

        // Wave 4.9.3 (2026-06-09): legacy events.photo_albums / events.album_photos
        // physically moved to the media schema. PhotoAlbum + AlbumPhoto now use
        // the default schema (`media`) via HasDefaultSchema above; no override
        // is needed.

        // Module-owned operational tables — `media.outbox`, etc.
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new DeadLetterMessageConfiguration());
        modelBuilder.ApplyConfiguration(new IdempotencyKeyConfiguration());

        // Wave4.9.2.10c.b Phase 1.10c.b (2026-06-09): physical CreatedBy/UpdatedBy
        // columns landed on media.photo_albums / album_photos via
        // Phase1_10c_b_AddCreatedByUpdatedByToMediaTables migration. Per-entity
        // configs map both to snake_case columns.

        base.OnModelCreating(modelBuilder);
    }
}
