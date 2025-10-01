using LankaConnect.Domain.Business;
using LankaConnect.Domain.Business.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.Infrastructure.Data.Configurations;

public class BusinessConfigurationSimplified : IEntityTypeConfiguration<Business>
{
    public void Configure(EntityTypeBuilder<Business> builder)
    {
        builder.ToTable("Businesses");
        builder.HasKey(b => b.Id);

        // Basic properties - we'll expand the value objects later
        builder.Property(b => b.Id).ValueGeneratedNever();
        
        // Profile properties (flattened for now)
        builder.Property<string>("Name")
            .HasColumnName("Name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property<string>("Description")
            .HasColumnName("Description")
            .HasMaxLength(2000)
            .IsRequired();

        // Location properties (flattened)
        builder.Property<string>("Address")
            .HasColumnName("Address")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property<string>("City")
            .HasColumnName("City")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property<string>("Province")
            .HasColumnName("Province")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property<string>("PostalCode")
            .HasColumnName("PostalCode")
            .HasMaxLength(20);

        builder.Property<double>("Latitude")
            .HasColumnName("Latitude")
            .HasColumnType("double precision");

        builder.Property<double>("Longitude")
            .HasColumnName("Longitude")
            .HasColumnType("double precision");

        // Contact properties (flattened)
        builder.Property<string>("ContactPhone")
            .HasColumnName("ContactPhone")
            .HasMaxLength(20);

        builder.Property<string>("ContactEmail")
            .HasColumnName("ContactEmail")
            .HasMaxLength(100);

        builder.Property<string>("Website")
            .HasColumnName("Website")
            .HasMaxLength(200);

        // Business Hours (simplified as JSON for now)
        builder.Property<string>("BusinessHours")
            .HasColumnName("BusinessHours")
            .HasColumnType("text");

        // Enum properties
        builder.Property(b => b.Category)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Other properties
        builder.Property(b => b.OwnerId).IsRequired();
        builder.Property(b => b.Rating).HasColumnType("decimal(3,2)");
        builder.Property(b => b.ReviewCount).HasDefaultValue(0);
        builder.Property(b => b.IsVerified).HasDefaultValue(false);
        builder.Property(b => b.VerifiedAt);
        builder.Property(b => b.CreatedAt).IsRequired();
        builder.Property(b => b.UpdatedAt).IsRequired();

        // Indexes for performance
        builder.HasIndex(b => b.IsVerified).HasDatabaseName("IX_Business_IsVerified");
        builder.HasIndex(b => b.Rating).HasDatabaseName("IX_Business_Rating");
        builder.HasIndex(b => b.Category).HasDatabaseName("IX_Business_Category");
        builder.HasIndex(b => b.Status).HasDatabaseName("IX_Business_Status");
        builder.HasIndex(b => b.OwnerId).HasDatabaseName("IX_Business_OwnerId");
        builder.HasIndex(b => b.CreatedAt).HasDatabaseName("IX_Business_CreatedAt");

        // Geographic index (PostgreSQL specific)
        builder.HasIndex(new[] { "Latitude", "Longitude" }).HasDatabaseName("IX_Business_Location");

        // Navigation properties (ignore for now since services and reviews are complex)
        builder.Ignore(b => b.Services);
        builder.Ignore(b => b.Reviews);
        
        // Ignore complex value objects for now
        builder.Ignore(b => b.Profile);
        builder.Ignore(b => b.Location);
        builder.Ignore(b => b.ContactInfo);
        builder.Ignore(b => b.Hours);
    }
}