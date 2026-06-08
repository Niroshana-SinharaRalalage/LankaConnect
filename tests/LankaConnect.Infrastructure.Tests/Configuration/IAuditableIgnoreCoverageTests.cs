using FluentAssertions;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Infrastructure.Data;
using LankaConnect.Modules.Forms.Infrastructure.Data;
using LankaConnect.Modules.Media.Infrastructure.Data;
using LankaConnect.Modules.Notifications.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LankaConnect.Infrastructure.Tests.Configuration;

/// <summary>
/// Wave4.9.1.3 (2026-06-08): asserts every IAuditable entity across all
/// 4 DbContexts has its CreatedBy + UpdatedBy properties ignored
/// (per <c>IgnoreAuditByActorPropertiesUntilPhase1</c> hotfix). Phase
/// 1.x of Wave4.9.2 will progressively REMOVE the ignore + add the
/// physical columns per schema group — this test will then update to
/// allow snake_case-mapped properties as a valid alternative to ignored.
/// </summary>
/// <remarks>
/// Architect P5 ruling 2026-06-08: use Npgsql model-builder-only (no real
/// DB connection). <c>UseNpgsql("Host=fake;...")</c> lets EF Core build
/// the model graph without connecting; we then inspect <c>IModel</c>
/// metadata via reflection. ~10x faster than Testcontainers + tests the
/// actual production model configuration.
///
/// Per CLAUDE.md §13.1 trigger T4 (EF Core configuration coverage) + T6
/// (DbContext registration assertions).
/// </remarks>
public sealed class IAuditableIgnoreCoverageTests
{
    private const string FakeConn = "Host=fake;Database=fake;Username=fake;Password=fake";

    [Fact]
    public void AppDbContext_Ignores_CreatedBy_For_All_IAuditable_Entities()
    {
        var model = BuildAppDbContextModel();
        AssertNoIAuditableLeak(model, propertyName: "CreatedBy", contextName: nameof(AppDbContext));
    }

    [Fact]
    public void AppDbContext_Ignores_UpdatedBy_For_All_IAuditable_Entities()
    {
        var model = BuildAppDbContextModel();
        AssertNoIAuditableLeak(model, propertyName: "UpdatedBy", contextName: nameof(AppDbContext));
    }

    [Fact]
    public void NotificationsDbContext_Ignores_CreatedBy_For_All_IAuditable_Entities()
    {
        var model = BuildNotificationsDbContextModel();
        AssertNoIAuditableLeak(model, propertyName: "CreatedBy", contextName: nameof(NotificationsDbContext));
    }

    [Fact]
    public void NotificationsDbContext_Ignores_UpdatedBy_For_All_IAuditable_Entities()
    {
        var model = BuildNotificationsDbContextModel();
        AssertNoIAuditableLeak(model, propertyName: "UpdatedBy", contextName: nameof(NotificationsDbContext));
    }

    [Fact]
    public void MediaDbContext_Ignores_CreatedBy_For_All_IAuditable_Entities()
    {
        var model = BuildMediaDbContextModel();
        AssertNoIAuditableLeak(model, propertyName: "CreatedBy", contextName: nameof(MediaDbContext));
    }

    [Fact]
    public void MediaDbContext_Ignores_UpdatedBy_For_All_IAuditable_Entities()
    {
        var model = BuildMediaDbContextModel();
        AssertNoIAuditableLeak(model, propertyName: "UpdatedBy", contextName: nameof(MediaDbContext));
    }

    [Fact]
    public void FormsDbContext_Ignores_CreatedBy_For_All_IAuditable_Entities()
    {
        var model = BuildFormsDbContextModel();
        AssertNoIAuditableLeak(model, propertyName: "CreatedBy", contextName: nameof(FormsDbContext));
    }

    [Fact]
    public void FormsDbContext_Ignores_UpdatedBy_For_All_IAuditable_Entities()
    {
        var model = BuildFormsDbContextModel();
        AssertNoIAuditableLeak(model, propertyName: "UpdatedBy", contextName: nameof(FormsDbContext));
    }

    /// <summary>
    /// For every entity in the model that implements <see cref="IAuditable"/>,
    /// fail if its EF property mapping still includes the named audit-by-actor
    /// property. Pre-Wave4.9.2 invariant: ALL such properties must be
    /// <c>builder.Ignore()</c>'d. Post-Wave4.9.2.x ships a schema group, the
    /// invariant relaxes to "ignored OR mapped with snake_case HasColumnName".
    /// </summary>
    private static void AssertNoIAuditableLeak(IModel model, string propertyName, string contextName)
    {
        var iauditableType = typeof(IAuditable);
        var leaks = new List<string>();
        var auditableEntityCount = 0;

        foreach (var et in model.GetEntityTypes())
        {
            if (!iauditableType.IsAssignableFrom(et.ClrType))
            {
                continue;
            }
            auditableEntityCount++;

            if (et.FindProperty(propertyName) is not null)
            {
                leaks.Add(et.ClrType.FullName ?? et.ClrType.Name);
            }
        }

        // Sanity check: we must find at least one IAuditable entity per context
        // (otherwise the test silently passes via empty-set tautology).
        auditableEntityCount.Should().BeGreaterThan(0,
            because: $"{contextName} should contain at least one IAuditable-implementing entity; the IAuditable bridge across the codebase guarantees this. If 0, either the model didn't load or IAuditable was severed.");

        leaks.Should().BeEmpty(
            because: $"the hotfix at {contextName}.IgnoreAuditByActorPropertiesUntilPhase1 + per-entity Ignore() must apply to EVERY IAuditable entity until Wave4.9.2.x lands the physical {propertyName} columns. Leaking entities will cause EF to emit '{propertyName}' in SELECT clauses against tables that don't have that physical column, breaking runtime queries with PostgreSQL 42703 errors.\nLeaks in {contextName}: {string.Join(", ", leaks)}");
    }

    private static IModel BuildAppDbContextModel()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(FakeConn)
            .Options;
        var publisher = new Mock<IPublisher>().Object;
        var logger = NullLogger<AppDbContext>.Instance;
        using var ctx = new AppDbContext(options, publisher, logger);
        return ctx.Model;
    }

    private static IModel BuildNotificationsDbContextModel()
    {
        var options = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseNpgsql(FakeConn)
            .Options;
        using var ctx = new NotificationsDbContext(options);
        return ctx.Model;
    }

    private static IModel BuildMediaDbContextModel()
    {
        var options = new DbContextOptionsBuilder<MediaDbContext>()
            .UseNpgsql(FakeConn)
            .Options;
        using var ctx = new MediaDbContext(options);
        return ctx.Model;
    }

    private static IModel BuildFormsDbContextModel()
    {
        var options = new DbContextOptionsBuilder<FormsDbContext>()
            .UseNpgsql(FakeConn)
            .Options;
        using var ctx = new FormsDbContext(options);
        return ctx.Model;
    }
}
