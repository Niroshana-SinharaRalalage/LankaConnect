using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Business.ValueObjects;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for BusinessLocation value object as owned entity type
/// Resolves constructor parameter binding issues for complex value objects
/// </summary>
public class BusinessLocationConfiguration : IEntityTypeConfiguration<BusinessLocation>
{
    public void Configure(EntityTypeBuilder<BusinessLocation> builder)
    {
        // This configuration is applied when BusinessLocation is owned by another entity
        // Configure as owned entity type to avoid constructor binding issues
        
        // Configure Address as owned entity
        builder.OwnsOne(bl => bl.Address, addressBuilder =>
        {
            addressBuilder.Property(a => a.Street)
                .HasColumnName("Street")
                .HasMaxLength(200)
                .IsRequired();
                
            addressBuilder.Property(a => a.City)
                .HasColumnName("City")
                .HasMaxLength(100)
                .IsRequired();
                
            addressBuilder.Property(a => a.State)
                .HasColumnName("State")
                .HasMaxLength(50)
                .IsRequired();
                
            addressBuilder.Property(a => a.ZipCode)
                .HasColumnName("ZipCode")
                .HasMaxLength(20)
                .IsRequired();
                
            addressBuilder.Property(a => a.Country)
                .HasColumnName("Country")
                .HasMaxLength(100)
                .IsRequired();
        });

        // Configure GeoCoordinate as owned entity (nullable)
        builder.OwnsOne(bl => bl.Coordinates, coordinatesBuilder =>
        {
            coordinatesBuilder.Property(gc => gc.Latitude)
                .HasColumnName("Latitude")
                .HasPrecision(10, 7); // Precision for GPS coordinates
                
            coordinatesBuilder.Property(gc => gc.Longitude)
                .HasColumnName("Longitude")
                .HasPrecision(10, 7); // Precision for GPS coordinates
        });
    }
}