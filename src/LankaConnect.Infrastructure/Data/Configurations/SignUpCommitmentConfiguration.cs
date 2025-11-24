using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Events.Entities;

namespace LankaConnect.Infrastructure.Data.Configurations;

public class SignUpCommitmentConfiguration : IEntityTypeConfiguration<SignUpCommitment>
{
    public void Configure(EntityTypeBuilder<SignUpCommitment> builder)
    {
        builder.ToTable("sign_up_commitments");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(c => c.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(c => c.ItemDescription)
            .HasColumnName("item_description")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(c => c.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Property(c => c.CommittedAt)
            .HasColumnName("committed_at")
            .IsRequired();

        // Shadow properties for BaseEntity
        builder.Property<DateTime>("CreatedAt")
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property<DateTime?>("UpdatedAt")
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(c => c.UserId)
            .HasDatabaseName("ix_sign_up_commitments_user_id");
    }
}
