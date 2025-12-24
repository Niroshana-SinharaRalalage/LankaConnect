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

        // Configure Subject value object with custom conversion
        // Phase 6A.41 Fix: Use HasConversion instead of OwnsOne to handle Result pattern
        // When loading from database, EmailSubject.Create() may return a failed Result if value is null/empty
        // This causes EF Core to throw "Cannot access value of a failed result" during query materialization
        builder.Property(e => e.SubjectTemplate)
            .HasColumnName("subject_template")
            .HasMaxLength(200)
            .IsRequired()
            .HasConversion(
                // Convert EmailSubject to string for database
                subject => subject.Value,
                // Convert string from database to EmailSubject
                // Use FromDatabase() to bypass validation during hydration
                value => LankaConnect.Domain.Communications.ValueObjects.EmailSubject.FromDatabase(value));

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