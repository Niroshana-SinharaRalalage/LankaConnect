using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.ValueObjects;
using LankaConnect.Domain.Users.Enums;

namespace LankaConnect.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        
        builder.Property(u => u.Id)
            .ValueGeneratedNever();

        // Configure Email value object
        builder.OwnsOne(u => u.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("email")
                .HasMaxLength(255)
                .IsRequired();
                
            email.HasIndex(e => e.Value)
                .IsUnique()
                .HasDatabaseName("ix_users_email");
        });

        // Configure basic properties
        builder.Property(u => u.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.LastName)
            .HasMaxLength(100)
            .IsRequired();

        // Configure PhoneNumber value object (optional)
        builder.OwnsOne(u => u.PhoneNumber, phone =>
        {
            phone.Property(p => p.Value)
                .HasColumnName("phone_number")
                .HasMaxLength(20);
        });

        // Configure Bio (optional)
        builder.Property(u => u.Bio)
            .HasMaxLength(1000);

        // Configure UserLocation value object (optional - privacy choice)
        builder.OwnsOne(u => u.Location, location =>
        {
            location.Property(l => l.City)
                .HasColumnName("city")
                .HasMaxLength(100);

            location.Property(l => l.State)
                .HasColumnName("state")
                .HasMaxLength(100);

            location.Property(l => l.ZipCode)
                .HasColumnName("zip_code")
                .HasMaxLength(20);

            location.Property(l => l.Country)
                .HasColumnName("country")
                .HasMaxLength(100);
        });

        // Configure status
        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Configure authentication properties
        builder.Property(u => u.IdentityProvider)
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(IdentityProvider.Local);

        builder.Property(u => u.ExternalProviderId)
            .HasMaxLength(255);

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(255);

        builder.Property(u => u.Role)
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(UserRole.GeneralUser);

        builder.Property(u => u.IsEmailVerified)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.EmailVerificationToken)
            .HasMaxLength(255);

        builder.Property(u => u.EmailVerificationTokenExpiresAt);

        builder.Property(u => u.PasswordResetToken)
            .HasMaxLength(255);

        builder.Property(u => u.PasswordResetTokenExpiresAt);

        builder.Property(u => u.FailedLoginAttempts)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(u => u.AccountLockedUntil);

        builder.Property(u => u.LastLoginAt);

        // Configure CulturalInterests collection (0-10 interests allowed, privacy choice)
        builder.OwnsMany(u => u.CulturalInterests, ci =>
        {
            ci.WithOwner().HasForeignKey("UserId");
            ci.ToTable("user_cultural_interests", "users");

            ci.Property<int>("Id")
                .ValueGeneratedOnAdd();

            ci.HasKey("Id");

            // Store only the Code property (enumeration pattern - Code identifies the interest)
            ci.Property(c => c.Code)
                .HasColumnName("interest_code")
                .HasMaxLength(50)
                .IsRequired();

            // Ignore Name property - it's computed from the static enumeration
            ci.Ignore(c => c.Name);

            // Index for efficient querying and uniqueness
            ci.HasIndex("UserId", "Code")
                .HasDatabaseName("ix_user_cultural_interests_user_code")
                .IsUnique(); // Prevent duplicate interests per user
        });

        // Auto-include CulturalInterests when loading User
        builder.Navigation(u => u.CulturalInterests).AutoInclude();

        // Configure Languages collection (1-5 languages required)
        builder.OwnsMany(u => u.Languages, lang =>
        {
            lang.WithOwner().HasForeignKey("UserId");
            lang.ToTable("user_languages", "users");

            lang.Property<int>("Id")
                .ValueGeneratedOnAdd();

            lang.HasKey("Id");

            // Configure LanguageCode value object - store only the Code property
            lang.OwnsOne(l => l.Language, languageCode =>
            {
                languageCode.Property(lc => lc.Code)
                    .HasColumnName("language_code")
                    .HasMaxLength(10)
                    .IsRequired();

                // Ignore Name and NativeName - they're computed from the static enumeration
                languageCode.Ignore(lc => lc.Name);
                languageCode.Ignore(lc => lc.NativeName);

                // Index for efficient querying and uniqueness on language_code column
                languageCode.HasIndex(lc => lc.Code)
                    .HasDatabaseName("ix_user_languages_code");
            });

            // Configure ProficiencyLevel enum
            lang.Property(l => l.Proficiency)
                .HasColumnName("proficiency_level")
                .HasConversion<int>()
                .IsRequired();
        });

        // Auto-include Languages when loading User
        builder.Navigation(u => u.Languages).AutoInclude();

        // Configure RefreshTokens collection
        builder.OwnsMany(u => u.RefreshTokens, rt =>
        {
            rt.WithOwner().HasForeignKey("UserId");
            rt.ToTable("user_refresh_tokens", "identity");

            rt.Property(t => t.Token)
                .HasMaxLength(255)
                .IsRequired();

            rt.Property(t => t.ExpiresAt)
                .IsRequired();

            rt.Property(t => t.CreatedAt)
                .IsRequired();

            rt.Property(t => t.CreatedByIp)
                .HasMaxLength(45)
                .IsRequired();

            rt.Property(t => t.IsRevoked)
                .IsRequired()
                .HasDefaultValue(false);

            rt.Property(t => t.RevokedAt);

            rt.Property(t => t.RevokedByIp)
                .HasMaxLength(45);

            rt.HasIndex(t => t.Token)
                .IsUnique()
                .HasDatabaseName("ix_user_refresh_tokens_token");

            rt.HasIndex(t => t.ExpiresAt)
                .HasDatabaseName("ix_user_refresh_tokens_expires_at");
        });

        // Configure ExternalLogins collection (Epic 1 Phase 2 - Social Login)
        builder.OwnsMany(u => u.ExternalLogins, el =>
        {
            el.WithOwner().HasForeignKey("UserId");
            el.ToTable("external_logins", "identity");

            el.Property(login => login.Provider)
                .HasColumnName("provider")
                .HasConversion<int>()
                .IsRequired();

            el.Property(login => login.ExternalProviderId)
                .HasColumnName("external_provider_id")
                .HasMaxLength(255)
                .IsRequired();

            el.Property(login => login.ProviderEmail)
                .HasColumnName("provider_email")
                .HasMaxLength(255)
                .IsRequired();

            el.Property(login => login.LinkedAt)
                .HasColumnName("linked_at")
                .IsRequired();

            // Composite unique index: Prevent same provider+externalId from being linked twice
            el.HasIndex(login => new { login.Provider, login.ExternalProviderId })
                .IsUnique()
                .HasDatabaseName("ix_external_logins_provider_external_id");

            // Index for userId lookups
            el.HasIndex("UserId")
                .HasDatabaseName("ix_external_logins_user_id");
        });

        // Auto-include ExternalLogins when loading User
        builder.Navigation(u => u.ExternalLogins).AutoInclude();

        // Configure PreferredMetroAreas many-to-many relationship (Phase 5B)
        // CRITICAL FIX: Connect skip navigation to the _preferredMetroAreaIds backing field
        // Following ADR-008: Explicit junction table with proper backing field configuration
        var navigation = builder.HasMany<Domain.Events.MetroArea>()
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "user_preferred_metro_areas",
                j => j
                    .HasOne<Domain.Events.MetroArea>()
                    .WithMany()
                    .HasForeignKey("metro_area_id")
                    .OnDelete(DeleteBehavior.Cascade) // ADR-008: Cascade delete
                    .HasConstraintName("fk_user_preferred_metro_areas_metro_area_id"),
                j => j
                    .HasOne<User>()
                    .WithMany()
                    .HasForeignKey("user_id")
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("fk_user_preferred_metro_areas_user_id"),
                j =>
                {
                    j.ToTable("user_preferred_metro_areas", "identity");

                    // Composite primary key
                    j.HasKey("user_id", "metro_area_id");

                    // Indexes for query performance
                    j.HasIndex("user_id")
                        .HasDatabaseName("ix_user_preferred_metro_areas_user_id");

                    j.HasIndex("metro_area_id")
                        .HasDatabaseName("ix_user_preferred_metro_areas_metro_area_id");

                    // Audit columns
                    j.Property<DateTime>("created_at")
                        .HasDefaultValueSql("NOW()")
                        .IsRequired();
                });

        // CRITICAL: Tell EF Core to track this skip navigation with field access
        // This ensures changes to the backing field are persisted to the junction table
        // The navigation exists only in EF's model metadata, not as a domain model property
        if (navigation?.Metadata != null)
        {
            navigation.Metadata.SetPropertyAccessMode(PropertyAccessMode.FieldDuringConstruction);
        }

        // Configure audit fields
        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(u => u.UpdatedAt);

        // Configure indexes
        builder.HasIndex(u => u.CreatedAt)
            .HasDatabaseName("ix_users_created_at");

        builder.HasIndex(u => u.IsActive)
            .HasDatabaseName("ix_users_is_active");

        builder.HasIndex(u => u.Role)
            .HasDatabaseName("ix_users_role");

        builder.HasIndex(u => u.EmailVerificationToken)
            .HasDatabaseName("ix_users_email_verification_token");

        builder.HasIndex(u => u.PasswordResetToken)
            .HasDatabaseName("ix_users_password_reset_token");

        builder.HasIndex(u => u.IsEmailVerified)
            .HasDatabaseName("ix_users_is_email_verified");

        builder.HasIndex(u => u.LastLoginAt)
            .HasDatabaseName("ix_users_last_login_at");

        // Configure indexes for Entra External ID integration
        builder.HasIndex(u => u.IdentityProvider)
            .HasDatabaseName("ix_users_identity_provider");

        builder.HasIndex(u => u.ExternalProviderId)
            .HasDatabaseName("ix_users_external_provider_id");

        // Composite index for external provider lookups
        builder.HasIndex(u => new { u.IdentityProvider, u.ExternalProviderId })
            .HasDatabaseName("ix_users_identity_provider_external_id");
    }
}