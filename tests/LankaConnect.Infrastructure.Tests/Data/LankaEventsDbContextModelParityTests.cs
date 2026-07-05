using FluentAssertions;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LankaConnect.Infrastructure.Tests.Data;

/// <summary>
/// Wave 6.5.f.5-hotfix acceptance criterion §3.4 (architect ruling 2026-07-04).
///
/// Guards against the 6.5.e Ignore trap that produced 46 API smoke failures on staging.
/// Root cause of that regression: <c>modelBuilder.Ignore&lt;EventEmailGroupLink&gt;()</c>
/// and <c>modelBuilder.Ignore&lt;EventBadge&gt;()</c> in <see cref="LankaEventsDbContext.OnModelCreating"/>
/// collided with <c>EventConfiguration.HasMany(e =&gt; e.EmailGroupLinks)</c> and
/// <c>HasMany(e =&gt; e.Badges)</c> that were registered via
/// <see cref="ModelBuilder.ApplyConfigurationsFromAssembly"/>. Model build succeeded
/// silently; queries failed at runtime with "not a property access."
///
/// This test asserts both junction entity types are MAPPED as
/// <see cref="Microsoft.EntityFrameworkCore.Metadata.IEntityType"/>s in the finalized
/// LankaEventsDbContext model. It also asserts the two far-principal types
/// (Communications.EmailGroup, Badges.Badge) are NOT mapped — codifying the
/// "junction mapped, far-principal foreign" pattern from Blueprint §7.8.
///
/// Wave 6.5.f.7 will promote this pattern to permanent parity tests for all 6 DbContexts.
/// </summary>
public sealed class LankaEventsDbContextModelParityTests
{
    private static LankaEventsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LankaEventsDbContext>()
            .UseInMemoryDatabase("parity-check")
            .Options;
        return new LankaEventsDbContext(options);
    }

    [Fact]
    public void EventEmailGroupLink_IsMapped_InLankaEventsDbContext()
    {
        using var ctx = CreateContext();

        // Force model build
        var entityType = ctx.Model.FindEntityType(typeof(EventEmailGroupLink));

        entityType.Should().NotBeNull(
            "Wave 6.5.f.5-hotfix un-Ignored EventEmailGroupLink so EventRepository.GetByIdAsync's " +
            ".Include(e => e.EmailGroupLinks) chain resolves. If this test fails, 6.5.e's Ignore " +
            "trap has been re-introduced somehow.");
    }

    [Fact]
    public void EventBadge_IsMapped_InLankaEventsDbContext()
    {
        using var ctx = CreateContext();

        var entityType = ctx.Model.FindEntityType(typeof(EventBadge));

        entityType.Should().NotBeNull(
            "Wave 6.5.f.5-hotfix un-Ignored EventBadge so EventRepository.GetEventsWithBadgeAsync's " +
            ".Include(e => e.Badges) chain resolves. If this test fails, the Ignore trap has been " +
            "re-introduced somehow.");
    }

    [Fact]
    public void EmailGroup_IsNotMapped_InLankaEventsDbContext()
    {
        using var ctx = CreateContext();

        var entityType = ctx.Model.FindEntityType(
            typeof(LankaConnect.Modules.Communications.Domain.Entities.EmailGroup));

        entityType.Should().BeNull(
            "EmailGroup is a cross-module principal owned by Communications module. " +
            "Blueprint §7.8: far principals stay foreign; junction FK stays scalar. " +
            "If this test fails, an accidental cross-module map has been introduced.");
    }

    [Fact]
    public void Badge_IsNotMapped_InLankaEventsDbContext()
    {
        using var ctx = CreateContext();

        var entityType = ctx.Model.FindEntityType(
            typeof(LankaConnect.Products.LankaEvents.Domain.Badges.Badge));

        entityType.Should().BeNull(
            "Badge is a cross-module principal (LankaConnect.Domain.Badges). " +
            "Blueprint §7.8: junction (EventBadge) mapped, far principal (Badge) foreign. " +
            "EventBadge.BadgeId remains scalar FK; hydration happens at application layer " +
            "via IBadgeRepository.");
    }
}
