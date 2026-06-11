using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.Modules.Forms.Infrastructure.Tests;

/// <summary>
/// Wave 4.9.4 (2026-06-10): asserts that Form + FormQuestion + FormResponse
/// + FormAnswer are mapped to the <c>forms</c> schema (not the legacy
/// <c>events</c> schema) after the ALTER TABLE ... SET SCHEMA migration.
/// Guards against cross-schema-override reintroduction. Mirrors the Wave 4.9.3
/// MediaDbContextSchemaTests pattern.
/// </summary>
public sealed class FormsDbContextSchemaTests
{
    [Fact]
    public void EventForm_Is_Mapped_To_Forms_Schema()
    {
        var model = BuildFormsDbContextModel();
        var entityType = model.FindEntityType(typeof(Form));

        entityType.Should().NotBeNull(because: "Form must be configured in FormsDbContext");
        entityType!.GetSchema().Should().Be("forms",
            because: "Wave 4.9.4 (2026-06-10) moved events.event_forms -> forms.event_forms.");
        entityType!.GetTableName().Should().Be("event_forms");
    }

    [Fact]
    public void FormQuestion_Is_Mapped_To_Forms_Schema()
    {
        var model = BuildFormsDbContextModel();
        var entityType = model.FindEntityType(typeof(FormQuestion));

        entityType.Should().NotBeNull();
        entityType!.GetSchema().Should().Be("forms");
        entityType!.GetTableName().Should().Be("form_questions");
    }

    [Fact]
    public void FormResponse_Is_Mapped_To_Forms_Schema()
    {
        var model = BuildFormsDbContextModel();
        var entityType = model.FindEntityType(typeof(FormResponse));

        entityType.Should().NotBeNull();
        entityType!.GetSchema().Should().Be("forms");
        entityType!.GetTableName().Should().Be("form_responses");
    }

    [Fact]
    public void FormAnswer_Is_Mapped_To_Forms_Schema()
    {
        var model = BuildFormsDbContextModel();
        var entityType = model.FindEntityType(typeof(FormAnswer));

        entityType.Should().NotBeNull();
        entityType!.GetSchema().Should().Be("forms");
        entityType!.GetTableName().Should().Be("form_answers");
    }

    [Fact]
    public void Form_Maps_OwnerEntityId_And_OwnerEntityType_Columns()
    {
        var model = BuildFormsDbContextModel();
        var entityType = model.FindEntityType(typeof(Form));

        entityType.Should().NotBeNull();

        var ownerIdProp = entityType!.FindProperty("OwnerEntityId");
        ownerIdProp.Should().NotBeNull(because: "Wave 5.2c removed b.Ignore() and mapped the property.");
        ownerIdProp!.GetColumnName().Should().Be("owner_entity_id");
        ownerIdProp!.IsNullable.Should().BeFalse();

        var ownerTypeProp = entityType!.FindProperty("OwnerEntityType");
        ownerTypeProp.Should().NotBeNull();
        ownerTypeProp!.GetColumnName().Should().Be("owner_entity_type");
        ownerTypeProp!.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void Form_OwnerEntityType_Persists_As_String()
    {
        var model = BuildFormsDbContextModel();
        var entityType = model.FindEntityType(typeof(Form));
        var ownerTypeProp = entityType!.FindProperty("OwnerEntityType")!;

        ownerTypeProp.GetProviderClrType().Should().Be(typeof(string),
            because: "Wave 5.2c configures HasConversion<string>() so backfill SQL can use 'Event' literal + values survive enum reordering.");
        ownerTypeProp.GetMaxLength().Should().Be(50);
    }

    [Fact]
    public void FormsDbContext_Has_No_Cross_Schema_Override()
    {
        var model = BuildFormsDbContextModel();

        foreach (var et in model.GetEntityTypes())
        {
            var schema = et.GetSchema();
            schema.Should().NotBe("events",
                because: $"FormsDbContext must not map any entity to the legacy events schema post-Wave 4.9.4. Offender: {et.ClrType.FullName} -> {schema}.{et.GetTableName()}");
        }
    }

    private static Microsoft.EntityFrameworkCore.Metadata.IModel BuildFormsDbContextModel()
    {
        var options = new DbContextOptionsBuilder<FormsDbContext>()
            .UseNpgsql("Host=fake;Database=fake;Username=fake;Password=fake")
            .Options;
        using var ctx = new FormsDbContext(options);
        return ctx.Model;
    }
}
