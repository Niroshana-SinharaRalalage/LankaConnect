using LankaConnect.Modules.Media.Domain;
using LankaConnect.Modules.Media.Domain.Entities;
using LankaConnect.Modules.Media.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.Modules.Media.Infrastructure.Tests;

/// <summary>
/// Wave 4.9.3 (2026-06-09): asserts that PhotoAlbum + AlbumPhoto are mapped
/// to the <c>media</c> schema (not the legacy <c>events</c> schema) after the
/// ALTER TABLE ... SET SCHEMA migration. Guards the cross-schema-override
/// pattern from re-introduction.
/// </summary>
/// <remarks>
/// Uses the Npgsql model-builder-only pattern (no DB connection) per the
/// architect P5 ruling 2026-06-08 (same approach as
/// <c>IAuditableIgnoreCoverageTests</c>). Pure metadata inspection.
/// </remarks>
public sealed class MediaDbContextSchemaTests
{
    [Fact]
    public void PhotoAlbum_Is_Mapped_To_Media_Schema()
    {
        var model = BuildMediaDbContextModel();
        var entityType = model.FindEntityType(typeof(PhotoAlbum));

        entityType.Should().NotBeNull(because: "PhotoAlbum must be configured in MediaDbContext");
        entityType!.GetSchema().Should().Be("media",
            because: "Wave 4.9.3 (2026-06-09) moved events.photo_albums -> media.photo_albums; any reintroduction of cross-schema override would silently break runtime queries.");
        entityType!.GetTableName().Should().Be("photo_albums");
    }

    [Fact]
    public void AlbumPhoto_Is_Mapped_To_Media_Schema()
    {
        var model = BuildMediaDbContextModel();
        var entityType = model.FindEntityType(typeof(AlbumPhoto));

        entityType.Should().NotBeNull(because: "AlbumPhoto must be configured in MediaDbContext");
        entityType!.GetSchema().Should().Be("media",
            because: "Wave 4.9.3 (2026-06-09) moved events.album_photos -> media.album_photos.");
        entityType!.GetTableName().Should().Be("album_photos");
    }

    [Fact]
    public void MediaDbContext_Has_No_Cross_Schema_Override()
    {
        var model = BuildMediaDbContextModel();

        foreach (var et in model.GetEntityTypes())
        {
            var schema = et.GetSchema();
            schema.Should().NotBe("events",
                because: $"MediaDbContext must not map any entity to the legacy events schema post-Wave 4.9.3. Offender: {et.ClrType.FullName} -> {schema}.{et.GetTableName()}");
        }
    }

    private static Microsoft.EntityFrameworkCore.Metadata.IModel BuildMediaDbContextModel()
    {
        var options = new DbContextOptionsBuilder<MediaDbContext>()
            .UseNpgsql("Host=fake;Database=fake;Username=fake;Password=fake")
            .Options;
        using var ctx = new MediaDbContext(options);
        return ctx.Model;
    }
}
