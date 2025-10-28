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
            .HasDefaultValue(UserRole.User);

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