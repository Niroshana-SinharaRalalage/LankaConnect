using FluentAssertions;
using LankaConnect.Modules.Identity.Domain.Entities;
using LankaConnect.Modules.Identity.Infrastructure.Data;
using LankaConnect.Products.LankaEvents.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LankaConnect.Infrastructure.Tests.Data;

/// <summary>
/// Sub-slice 4C.e (2026-07-08) permanent parity test per Rule 5e
/// ([[feedback-parity-tests-permanent]]) + Rule 5j (config-relocation audit).
///
/// Guards the IdentityDbContext model shape after the UserConfiguration
/// relocation from LankaConnect.Infrastructure.Data.Configurations to
/// LankaConnect.Modules.Identity.Infrastructure.Data.Configurations. Follows
/// the LankaEventsDbContext parity-test template introduced by Wave 6.5.f.5
/// hotfix acceptance criterion §3.4.
///
/// Assertions (Rule 5e §1-4):
///   1) User aggregate IS mapped in IdentityDbContext (via ApplyConfigurationsFromAssembly).
///   2) User table lives in the `identity` schema and is named `users`
///      (physical schema authority per AppDbContext.ConfigureSchemas line 452).
///   3) MetroArea (cross-module principal owned by LankaEvents) is NOT mapped
///      — the `Ignore&lt;MetroArea&gt;()` call in
///      <see cref="IdentityDbContext.OnModelCreating"/> keeps the
///      user_preferred_metro_areas junction FK scalar per Blueprint §7.8.
/// </summary>
public sealed class IdentityDbContextModelParityTests
{
    private static IdentityDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase("identity-parity-check")
            .Options;
        return new IdentityDbContext(options);
    }

    [Fact]
    public void User_IsMapped_InIdentityDbContext()
    {
        using var ctx = CreateContext();

        var entityType = ctx.Model.FindEntityType(typeof(User));

        entityType.Should().NotBeNull(
            "UserConfiguration was relocated in 4C.e from LankaConnect.Infrastructure to " +
            "Identity.Infrastructure. If this test fails, the relocated config file did " +
            "not get picked up by IdentityDbContext.ApplyConfigurationsFromAssembly — " +
            "likely a namespace mismatch or missing IEntityTypeConfiguration<User> impl.");
    }

    [Fact]
    public void User_TableName_IsUsers()
    {
        using var ctx = CreateContext();

        var entityType = ctx.Model.FindEntityType(typeof(User));

        entityType!.GetTableName().Should().Be(
            "users",
            "physical table name authority (AppDbContext.ConfigureSchemas:452) must survive " +
            "the UserConfiguration relocation to Identity.Infrastructure. If this test fails, " +
            "IdentityDbContext produces a different table name than AppDbContext for User — a " +
            "silent write-loss bug family per the Rule 5e checklist.");
    }

    [Fact]
    public void User_Schema_IsIdentity()
    {
        using var ctx = CreateContext();

        var entityType = ctx.Model.FindEntityType(typeof(User));

        entityType!.GetSchema().Should().Be(
            "identity",
            "IdentityDbContext.HasDefaultSchema(\"identity\") + the AppDbContext override " +
            "modelBuilder.Entity<User>().ToTable(\"users\", \"identity\") both anchor User in " +
            "the identity schema. If this test fails, IdentityDbContext will read/write from " +
            "the wrong schema.");
    }

    [Fact]
    public void MetroArea_IsNotMapped_InIdentityDbContext()
    {
        using var ctx = CreateContext();

        var entityType = ctx.Model.FindEntityType(typeof(MetroArea));

        entityType.Should().BeNull(
            "MetroArea is a cross-module principal owned by the LankaEvents product. " +
            "Blueprint §7.8: far principals stay foreign; the user_preferred_metro_areas " +
            "junction FK stays scalar. If this test fails, an accidental cross-module map " +
            "was introduced (would pull MetroArea + its owned graph into IdentityDbContext " +
            "and produce a divergent-schema silent-write-loss bug).");
    }
}
