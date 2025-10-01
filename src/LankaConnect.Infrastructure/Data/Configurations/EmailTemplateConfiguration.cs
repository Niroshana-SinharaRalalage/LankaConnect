using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Communications.Entities;

namespace LankaConnect.Infrastructure.Data.Configurations;

public class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        builder.ToTable("email_templates", "communications");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        // Configure basic properties
        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(500)
            .IsRequired();

        // Configure Subject value object (OwnsOne pattern)
        builder.OwnsOne(e => e.SubjectTemplate, subject =>
        {
            subject.Property(s => s.Value)
                .HasColumnName("subject_template")
                .HasMaxLength(200)
                .IsRequired();
        });

        // Configure template content
        builder.Property(e => e.TextTemplate)
            .HasColumnName("text_template")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(e => e.HtmlTemplate)
            .HasColumnName("html_template")
            .HasColumnType("text");

        // Configure enum with string conversion for better readability
        builder.Property(e => e.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Configure EmailTemplateCategory value object (stores as string)
        builder.Property(e => e.Category)
            .HasColumnName("category")
            .HasConversion(
                category => category.Value, // Convert to string for database
                value => LankaConnect.Domain.Communications.ValueObjects.EmailTemplateCategory.FromValue(value).Value) // Convert from string
            .HasMaxLength(50)
            .IsRequired();

        // Configure status properties
        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.Tags)
            .HasColumnName("tags")
            .HasMaxLength(500);

        // Configure audit fields from BaseEntity
        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        // Performance indexes for email template queries
        builder.HasIndex(e => e.Name)
            .IsUnique()
            .HasDatabaseName("IX_EmailTemplates_Name");

        builder.HasIndex(e => e.Type)
            .HasDatabaseName("IX_EmailTemplates_Type");

        builder.HasIndex(e => e.Category)
            .HasDatabaseName("IX_EmailTemplates_Category");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_EmailTemplates_IsActive");

        builder.HasIndex(e => new { e.Type, e.IsActive })
            .HasDatabaseName("IX_EmailTemplates_Type_IsActive");

        builder.HasIndex(e => new { e.Category, e.IsActive })
            .HasDatabaseName("IX_EmailTemplates_Category_IsActive");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_EmailTemplates_CreatedAt");
    }
}