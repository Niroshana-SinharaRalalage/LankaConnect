using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Modules.Media.Domain;
using LankaConnect.Modules.Media.Domain.Entities;
using LankaConnect.Modules.Media.Domain.Enums;

namespace LankaConnect.Modules.Media.Infrastructure.Data.Configurations;

public class PhotoAlbumConfiguration : IEntityTypeConfiguration<PhotoAlbum>
{
    public void Configure(EntityTypeBuilder<PhotoAlbum> builder)
    {
        builder.ToTable("photo_albums", "events");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.EventId).IsRequired();

        // Composite unique: one album name per event (multi-album support)
        builder.HasIndex(a => new { a.EventId, a.Name }).IsUnique();

        builder.Property(a => a.OrganizerId).IsRequired();

        builder.Property(a => a.EventTitle)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(a => a.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(AlbumStatus.Draft);

        builder.Property(a => a.Description)
            .HasColumnType("text");

        builder.Property(a => a.CoverPhotoUrl)
            .HasColumnType("text");

        builder.Property(a => a.RetentionDays)
            .IsRequired()
            .HasDefaultValue(7);

        builder.Property(a => a.PhotoCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(a => a.PublishedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(a => a.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        // Configure Photos collection
        builder.HasMany(a => a.Photos)
            .WithOne()
            .HasForeignKey(p => p.AlbumId)
            .OnDelete(DeleteBehavior.Cascade);

        // Access the backing field for the photos collection
        builder.Navigation(a => a.Photos)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class AlbumPhotoConfiguration : IEntityTypeConfiguration<AlbumPhoto>
{
    public void Configure(EntityTypeBuilder<AlbumPhoto> builder)
    {
        builder.ToTable("album_photos", "events");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.AlbumId).IsRequired();
        builder.HasIndex(p => p.AlbumId);

        builder.Property(p => p.UploaderId).IsRequired();

        builder.Property(p => p.UploaderName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.OriginalUrl)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(p => p.OriginalBlobName)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(p => p.ThumbnailUrl)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(p => p.ThumbnailBlobName)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(p => p.MediumUrl)
            .HasColumnType("text")
            .IsRequired(false);  // Nullable — videos have no medium variant

        builder.Property(p => p.MediumBlobName)
            .HasColumnType("text")
            .IsRequired(false);  // Nullable — videos have no medium variant

        builder.Property(p => p.Caption)
            .HasMaxLength(500);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(AlbumPhotoStatus.Approved);

        builder.Property(p => p.MediaType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(AlbumMediaType.Photo);

        builder.Property(p => p.FileSizeBytes)
            .IsRequired();

        builder.Property(p => p.DurationSeconds)
            .IsRequired(false);

        builder.Property(p => p.UploadedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(p => p.ExpiresAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(p => p.DisplayOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        // Composite indexes for common queries
        builder.HasIndex(p => new { p.AlbumId, p.Status });
        builder.HasIndex(p => new { p.AlbumId, p.UploadedAt });
        builder.HasIndex(p => p.ExpiresAt); // For cleanup job
    }
}
