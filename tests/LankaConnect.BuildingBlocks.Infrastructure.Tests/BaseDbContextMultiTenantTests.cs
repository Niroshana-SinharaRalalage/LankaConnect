using Microsoft.EntityFrameworkCore;

namespace LankaConnect.BuildingBlocks.Infrastructure.Tests;

/// <summary>
/// W1B verification: IMultiTenant&lt;TTenantId&gt; query-filter auto-application.
/// </summary>
public sealed class BaseDbContextMultiTenantTests
{
    [Fact]
    public async Task Query_ReturnsOnlyRowsForCurrentTenant()
    {
        var tenantA = TestTenantId.New();
        var tenantB = TestTenantId.New();
        var db = MultiTenantTestDbContextBuilder.Build(tenantA);

        db.Tenants.Add(new MultiTenantEntity { Name = "A1", TenantId = tenantA });
        db.Tenants.Add(new MultiTenantEntity { Name = "A2", TenantId = tenantA });
        db.Tenants.Add(new MultiTenantEntity { Name = "B1", TenantId = tenantB });
        await db.SaveChangesAsync();

        var rows = await db.Tenants.ToListAsync();

        rows.Should().HaveCount(2, "current tenant is A; B-tenant rows must be filtered out");
        rows.Should().OnlyContain(r => r.TenantId.Equals(tenantA));
    }

    [Fact]
    public async Task Query_FromDifferentDbContext_WithDifferentTenant_SeesOnlyItsTenantRows()
    {
        // The per-DbContext-instance tenant capture is the production pattern:
        // each HTTP request creates a fresh DbContext bound to that request's
        // tenant. This test simulates two requests against the same underlying
        // database, each scoped to a different tenant.
        var tenantA = TestTenantId.New();
        var tenantB = TestTenantId.New();
        var sharedDbName = Guid.NewGuid().ToString();

        // Request 1 (tenant A): insert rows for both tenants.
        var dbA = MultiTenantTestDbContextBuilder.Build(tenantA, sharedDbName);
        dbA.Tenants.Add(new MultiTenantEntity { Name = "A1", TenantId = tenantA });
        dbA.Tenants.Add(new MultiTenantEntity { Name = "B1", TenantId = tenantB });
        dbA.Tenants.Add(new MultiTenantEntity { Name = "B2", TenantId = tenantB });
        await dbA.SaveChangesAsync();

        // Request 2 (tenant B): query — should see only B rows.
        var dbB = MultiTenantTestDbContextBuilder.Build(tenantB, sharedDbName);
        var rowsB = await dbB.Tenants.ToListAsync();

        rowsB.Should().HaveCount(2, "tenant B DbContext sees only B rows");
        rowsB.Should().OnlyContain(r => r.TenantId.Equals(tenantB));
    }

    [Fact]
    public async Task Query_IgnoreQueryFilters_ReturnsAllRowsAcrossTenants()
    {
        var tenantA = TestTenantId.New();
        var tenantB = TestTenantId.New();
        var db = MultiTenantTestDbContextBuilder.Build(tenantA);

        db.Tenants.Add(new MultiTenantEntity { Name = "A1", TenantId = tenantA });
        db.Tenants.Add(new MultiTenantEntity { Name = "B1", TenantId = tenantB });
        await db.SaveChangesAsync();

        // Admin-recovery escape hatch must bypass the tenant filter.
        var rows = await db.Tenants.IgnoreQueryFilters().ToListAsync();

        rows.Should().HaveCount(2);
    }

    [Fact]
    public async Task Insert_RespectsExplicitTenantAssignment_DoesNotForceCurrentTenant()
    {
        // The filter scopes READS; writes are responsibility of the caller. This
        // documents the boundary: the filter is NOT an auto-tenant-stamper.
        var tenantA = TestTenantId.New();
        var tenantB = TestTenantId.New();
        var db = MultiTenantTestDbContextBuilder.Build(tenantA);

        db.Tenants.Add(new MultiTenantEntity { Name = "explicit-B", TenantId = tenantB });
        await db.SaveChangesAsync();

        // The write went through — but the read filter scopes it out for tenant A
        var visible = await db.Tenants.ToListAsync();
        visible.Should().BeEmpty();

        var all = await db.Tenants.IgnoreQueryFilters().ToListAsync();
        all.Should().HaveCount(1);
    }
}
