using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Infrastructure.Data;
using LankaConnect.Modules.Forms.Infrastructure.Data;
using LankaConnect.Modules.Media.Infrastructure.Data;
using LankaConnect.Modules.Notifications.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace LankaConnect.Infrastructure.Tests.Configuration;

/// <summary>
/// Gap G1.c (testing-discipline backfill, 2026-06-08): asserts the
/// hotfix Ignore for IAuditable's CreatedBy + UpdatedBy properties is
/// applied to EVERY IAuditable entity across all 4 DbContexts.
/// </summary>
/// <remarks>
/// <para>
/// Background: W3D bulk-migrated 70+ entities to LegacyBaseEntity which
/// implements IAuditable. EF Core auto-maps public properties; CreatedBy +
/// UpdatedBy started appearing in every generated SELECT. Physical
/// columns don't exist yet (Wave 4.9 Phase 1 work). The fix: explicit
/// <c>modelBuilder.Entity(t).Ignore("CreatedBy").Ignore("UpdatedBy")</c>
/// for all IAuditable entities, applied via
/// <c>AppDbContext.IgnoreAuditByActorPropertiesUntilPhase1</c> +
/// per-entity calls in the 3 module DbContexts (commits 7e16272b, 1eaa9c36,
/// ea5755ef).
/// </para>
/// <para>
/// These tests guard the fix: any future entity added to a DbContext that
/// implements IAuditable but is NOT in the Ignore list will fail these
/// tests, preventing the silent SELECT regression from reappearing.
/// </para>
/// <para>
/// Per CLAUDE.md §13.1 triggers T4 (EF configuration) + T6 (DI/DbContext
/// registration).
/// </para>
/// </remarks>
public sealed class IAuditableIgnoreCoverageTests
{
    [Fact]
    public void AppDbContext_Ignores_CreatedBy_For_All_IAuditable_Entities()
    {
        using var ctx = BuildAppDbContext();
        AssertNoIAuditableLeak(ctx, propertyName: "CreatedBy");
    }

    [Fact]
    public void AppDbContext_Ignores_UpdatedBy_For_All_IAuditable_Entities()
    {
        using var ctx = BuildAppDbContext();
        AssertNoIAuditableLeak(ctx, propertyName: "UpdatedBy");
    }

    [Fact]
    public void NotificationsDbContext_Ignores_CreatedBy_For_All_IAuditable_Entities()
    {
        using var ctx = BuildNotificationsDbContext();
        AssertNoIAuditableLeak(ctx, propertyName: "CreatedBy");
    }

    [Fact]
    public void NotificationsDbContext_Ignores_UpdatedBy_For_All_IAuditable_Entities()
    {
        using var ctx = BuildNotificationsDbContext();
        AssertNoIAuditableLeak(ctx, propertyName: "UpdatedBy");
    }

    [Fact]
    public void MediaDbContext_Ignores_CreatedBy_For_All_IAuditable_Entities()
    {
        using var ctx = BuildMediaDbContext();
        AssertNoIAuditableLeak(ctx, propertyName: "CreatedBy");
    }

    [Fact]
    public void MediaDbContext_Ignores_UpdatedBy_For_All_IAuditable_Entities()
    {
        using var ctx = BuildMediaDbContext();
        AssertNoIAuditableLeak(ctx, propertyName: "UpdatedBy");
    }

    [Fact]
    public void FormsDbContext_Ignores_CreatedBy_For_All_IAuditable_Entities()
    {
        using var ctx = BuildFormsDbContext();
        AssertNoIAuditableLeak(ctx, propertyName: "CreatedBy");
    }

    [Fact]
    public void FormsDbContext_Ignores_UpdatedBy_For_All_IAuditable_Entities()
    {
        using var ctx = BuildFormsDbContext();
        AssertNoIAuditableLeak(ctx, propertyName: "UpdatedBy");
    }

    /// <summary>
    /// For every entity in the context model that implements
    /// <see cref="IAuditable"/>, fail if its EF property mapping still
    /// includes the named audit-by-actor property. Builds the model once
    /// per test.
    /// </summary>
    private static void AssertNoIAuditableLeak(DbContext ctx, string propertyName)
    {
        var leaks = new List<string>();
        foreach (var et in ctx.Model.GetEntityTypes())
        {
            if (!typeof(IAuditable).IsAssignableFrom(et.ClrType))
            {
                continue;
            }
            if (et.FindProperty(propertyName) is not null)
            {
                leaks.Add(et.ClrType.FullName ?? et.ClrType.Name);
            }
        }

        leaks.Should().BeEmpty(
            because: $"the hotfix at AppDbContext.IgnoreAuditByActorPropertiesUntilPhase1 + per-entity Ignore() in module DbContexts must apply to EVERY IAuditable entity. Leaking entities will cause EF to emit `{propertyName}` in SELECT clauses against tables that don't have that physical column, breaking runtime queries.\nLeaks: {string.Join(", ", leaks)}");
    }

    private static AppDbContext BuildAppDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"g1c-{Guid.NewGuid():N}")
            .Options;
        var publisher = new Mock<IPublisher>().Object;
        var logger = NullLogger<AppDbContext>.Instance;
        return new AppDbContext(options, publisher, logger);
    }

    private static NotificationsDbContext BuildNotificationsDbContext()
    {
        var options = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseInMemoryDatabase($"g1c-notif-{Guid.NewGuid():N}")
            .Options;
        return new NotificationsDbContext(options);
    }

    private static MediaDbContext BuildMediaDbContext()
    {
        var options = new DbContextOptionsBuilder<MediaDbContext>()
            .UseInMemoryDatabase($"g1c-media-{Guid.NewGuid():N}")
            .Options;
        return new MediaDbContext(options);
    }

    private static FormsDbContext BuildFormsDbContext()
    {
        var options = new DbContextOptionsBuilder<FormsDbContext>()
            .UseInMemoryDatabase($"g1c-forms-{Guid.NewGuid():N}")
            .Options;
        return new FormsDbContext(options);
    }
}
