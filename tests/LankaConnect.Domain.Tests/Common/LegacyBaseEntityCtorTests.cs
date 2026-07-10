using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.BuildingBlocks.Domain;

namespace LankaConnect.Domain.Tests.Common;

/// <summary>
/// Gap G1.a (testing-discipline backfill, 2026-06-08): verifies the
/// <see cref="LegacyBaseEntity"/> ctor sets audit fields correctly. These
/// tests would have caught the silent <c>CreatedAt = DateTime.MinValue</c>
/// bug that shipped quietly between W3D and today's first successful
/// post-Actions-budget deploy.
/// </summary>
/// <remarks>
/// Per CLAUDE.md §13.1 trigger T1 (new public ctor behaviour) + T2 (mutator
/// touching IAuditable). The Wave 3 follow-up commit a8351981 restored the
/// behaviour these tests assert; this commit adds the test coverage that
/// should have shipped alongside that fix.
/// </remarks>
public sealed class LegacyBaseEntityCtorTests
{
    /// <summary>Minimal concrete subclass so we can instantiate the abstract base.</summary>
    private sealed class TestEntity : LegacyBaseEntity
    {
        public TestEntity() : base() { }
        public TestEntity(Guid id) : base(id) { }
    }

    [Fact]
    public void DefaultCtor_Sets_Id_To_NewGuid()
    {
        var entity = new TestEntity();

        entity.Id.Should().NotBe(Guid.Empty,
            because: "LegacyBaseEntity ctor must initialise Id to a fresh Guid so entities saved through AppDbContext (no interceptor) have a real primary key.");
    }

    [Fact]
    public void DefaultCtor_Sets_CreatedAt_WithinOneSecond_Of_UtcNow()
    {
        var before = DateTime.UtcNow;

        var entity = new TestEntity();

        var after = DateTime.UtcNow;
        entity.CreatedAt.Should().BeOnOrAfter(before.AddSeconds(-1));
        entity.CreatedAt.Should().BeOnOrBefore(after.AddSeconds(1));
        entity.CreatedAt.Kind.Should().Be(DateTimeKind.Utc,
            because: "CreatedAt must be UTC so cross-timezone reporting + retention windows behave correctly.");
    }

    [Fact]
    public void DefaultCtor_Leaves_UpdatedAt_Null_On_Fresh_Create()
    {
        var entity = new TestEntity();

        entity.UpdatedAt.Should().BeNull(
            because: "fresh-create rows should not have UpdatedAt populated; the AuditableInterceptor or domain mutator sets it on first modification.");
    }

    [Fact]
    public void DefaultCtor_Implements_IAuditable_Contract()
    {
        var entity = new TestEntity();

        entity.Should().BeAssignableTo<IAuditable>(
            because: "LegacyBaseEntity is the IAuditable bridge for the 60+ entities that still derive from it; severing this contract would silently break every existing audit-aware query.");
    }

    [Fact]
    public void ExplicitIdCtor_Preserves_Caller_Provided_Id_And_Still_Sets_CreatedAt()
    {
        var fixedId = Guid.NewGuid();

        var entity = new TestEntity(fixedId);

        entity.Id.Should().Be(fixedId,
            because: "callers (e.g. EF Core materialisation, test fixtures, replay scenarios) need to control Id while still getting the audit-field semantics.");
        entity.CreatedAt.Should().NotBe(DateTime.MinValue,
            because: "explicit-id ctor must still init CreatedAt; otherwise the AppDbContext (no interceptor) path silently writes MinValue.");
    }

    [Fact]
    public void Two_Sequential_DefaultCtor_Calls_Yield_Distinct_Ids()
    {
        var a = new TestEntity();
        var b = new TestEntity();

        a.Id.Should().NotBe(b.Id,
            because: "every fresh instance must get its own Guid; collisions would corrupt the primary-key index.");
    }
}
