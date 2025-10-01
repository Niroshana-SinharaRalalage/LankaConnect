using LankaConnect.Domain.Business;
using LankaConnect.Domain.Business.Enums;
using LankaConnect.Domain.Business.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace LankaConnect.Infrastructure.Data.Configurations;

public class BusinessConfiguration : IEntityTypeConfiguration<Business>
{
    public void Configure(EntityTypeBuilder<Business> builder)
    {
        builder.ToTable("Businesses");
        builder.HasKey(b => b.Id);

        // Profile (Owned Entity)
        builder.OwnsOne(b => b.Profile, p =>
        {
            p.Property(profile => profile.Name)
                .HasColumnName("Name")
                .HasMaxLength(200)
                .IsRequired();

            p.Property(profile => profile.Description)
                .HasColumnName("Description")
                .HasMaxLength(2000)
                .IsRequired();
        });

        // Contact Info (Owned Entity)
        builder.OwnsOne(b => b.ContactInfo, ci =>
        {
            ci.OwnsOne(c => c.PhoneNumber, phone =>
            {
                phone.Property(p => p.Value)
                    .HasColumnName("ContactPhone")
                    .HasMaxLength(20);
            });

            ci.OwnsOne(c => c.Email, email =>
            {
                email.Property(e => e.Value)
                    .HasColumnName("ContactEmail")
                    .HasMaxLength(100);
            });

            ci.Property(c => c.Website)
                .HasColumnName("ContactWebsite")
                .HasMaxLength(200);

            ci.Property(c => c.FacebookPage)
                .HasColumnName("ContactFacebook")
                .HasMaxLength(100);

            ci.Property(c => c.InstagramHandle)
                .HasColumnName("ContactInstagram")
                .HasMaxLength(50);

            ci.Property(c => c.TwitterHandle)
                .HasColumnName("ContactTwitter")
                .HasMaxLength(50);
        });

        // Location (Owned Entity)
        builder.OwnsOne(b => b.Location, l =>
        {
            l.OwnsOne(loc => loc.Address, a =>
            {
                a.Property(addr => addr.Street)
                    .HasColumnName("AddressStreet")
                    .HasMaxLength(200)
                    .IsRequired();

                a.Property(addr => addr.City)
                    .HasColumnName("AddressCity")
                    .HasMaxLength(50)
                    .IsRequired();

                a.Property(addr => addr.State)
                    .HasColumnName("AddressState")
                    .HasMaxLength(50)
                    .IsRequired();

                a.Property(addr => addr.ZipCode)
                    .HasColumnName("AddressZipCode")
                    .HasMaxLength(10)
                    .IsRequired();

                a.Property(addr => addr.Country)
                    .HasColumnName("AddressCountry")
                    .HasMaxLength(50)
                    .IsRequired();
            });

            l.OwnsOne(loc => loc.Coordinates, c =>
            {
                c.Property(coord => coord.Latitude)
                    .HasColumnName("LocationLatitude")
                    .HasColumnType("decimal(10,8)");

                c.Property(coord => coord.Longitude)
                    .HasColumnName("LocationLongitude")
                    .HasColumnType("decimal(11,8)");
            });
        });

        // Business Hours - stored as JSON using simple value converter
        builder.Property(b => b.Hours)
            .HasColumnName("BusinessHours")
            .HasColumnType("json")
            .IsRequired()
            .HasConversion(
                new BusinessHoursConverter()
            );

        // Category enum
        builder.Property(b => b.Category)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Status enum
        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(b => b.OwnerId)
            .IsRequired();

        builder.Property(b => b.Rating)
            .HasColumnType("decimal(3,2)");

        builder.Property(b => b.ReviewCount)
            .HasDefaultValue(0);

        builder.Property(b => b.IsVerified)
            .HasDefaultValue(false);

        builder.Property(b => b.VerifiedAt);

        builder.Property(b => b.CreatedAt)
            .IsRequired();

        builder.Property(b => b.UpdatedAt)
            .IsRequired();

        // Services Navigation
        builder.HasMany(b => b.Services)
            .WithOne(s => s.Business)
            .HasForeignKey(s => s.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        // Reviews Navigation
        builder.HasMany(b => b.Reviews)
            .WithOne()
            .HasForeignKey("BusinessId")
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance (on direct properties only for now)
        // Note: Indexes on owned entity properties will be added in a future migration

        builder.HasIndex(b => b.IsVerified)
            .HasDatabaseName("IX_Business_IsVerified");

        builder.HasIndex(b => b.Rating)
            .HasDatabaseName("IX_Business_Rating");

        builder.HasIndex(b => b.Category)
            .HasDatabaseName("IX_Business_Category");

        builder.HasIndex(b => b.Status)
            .HasDatabaseName("IX_Business_Status");

        builder.HasIndex(b => b.OwnerId)
            .HasDatabaseName("IX_Business_OwnerId");

        builder.HasIndex(b => b.CreatedAt)
            .HasDatabaseName("IX_Business_CreatedAt");
    }

}

// Value converter for BusinessHours
public class BusinessHoursConverter : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<BusinessHours, string>
{
    public BusinessHoursConverter() : base(
        businessHours => ConvertToJson(businessHours),
        json => ConvertFromJson(json))
    {
    }

    private static string ConvertToJson(BusinessHours businessHours)
    {
        var serializable = new Dictionary<string, object?>();
        
        foreach (var kvp in businessHours.WeeklyHours)
        {
            serializable[kvp.Key.ToString()] = kvp.Value != null 
                ? new { Start = kvp.Value.Start.ToString("HH:mm"), End = kvp.Value.End.ToString("HH:mm") }
                : null;
        }
        
        return JsonSerializer.Serialize(serializable);
    }

    private static BusinessHours ConvertFromJson(string json)
    {
        try
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (data == null) return BusinessHours.CreateAlwaysClosed();

            var hours = new Dictionary<DayOfWeek, (TimeOnly? open, TimeOnly? close)>();
            
            foreach (var kvp in data)
            {
                if (Enum.TryParse<DayOfWeek>(kvp.Key, out var dayOfWeek))
                {
                    if (kvp.Value.ValueKind == JsonValueKind.Null)
                    {
                        hours[dayOfWeek] = (null, null);
                    }
                    else if (kvp.Value.ValueKind == JsonValueKind.Object)
                    {
                        var startStr = kvp.Value.GetProperty("Start").GetString();
                        var endStr = kvp.Value.GetProperty("End").GetString();
                        
                        if (TimeOnly.TryParse(startStr, out var start) && TimeOnly.TryParse(endStr, out var end))
                        {
                            hours[dayOfWeek] = (start, end);
                        }
                        else
                        {
                            hours[dayOfWeek] = (null, null);
                        }
                    }
                }
            }

            var result = BusinessHours.Create(hours);
            return result.IsSuccess ? result.Value : BusinessHours.CreateAlwaysClosed();
        }
        catch
        {
            return BusinessHours.CreateAlwaysClosed();
        }
    }
}