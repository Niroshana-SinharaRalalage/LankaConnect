namespace LankaConnect.BuildingBlocks.Domain;

/// <summary>
/// Opt-in marker for entities scoped to a specific tenant. The per-Capability
/// <c>DbContext</c> applies a global query filter so reads only see rows for
/// the current tenant — the mechanical enforcement of ADR-002 tenancy.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why a marker interface rather than convention</b>: a marker is
/// machine-detectable. <c>BaseDbContext</c> walks every entity in the model;
/// if it implements <see cref="IMultiTenant{TTenantId}"/>, the query filter
/// is applied automatically. Convention-based "we'll remember to add
/// HasQueryFilter on Commerce entities" rots — the marker makes it
/// mechanical and ArchTest-enforceable
/// (<c>MultiTenant_Entities_HaveQueryFilter</c> rule per blueprint §5).
/// </para>
/// <para>
/// <b>TTenantId</b>: typically <c>StorefrontId</c> for Commerce / Marketplace
/// products (per ADR-002). Identity products may use <c>OrganizationId</c>.
/// Both are defined as typed-ID value objects in <c>SharedKernel.Identity</c>
/// (W1D / W1G).
/// </para>
/// <para>
/// <b>Wiring</b>: per-Capability DbContext calls
/// <c>BaseDbContext.ApplyMultiTenantFilter&lt;TTenantId&gt;(modelBuilder, tenantProvider)</c>
/// from <c>OnModelCreating</c>. The <c>tenantProvider</c> closes over a
/// scoped DI accessor (e.g. <c>ICurrentStorefrontAccessor</c>) — wired in
/// W1G alongside <c>IUserContext</c>.
/// </para>
/// <para>
/// <b>Getter only</b>: TenantId is set ONCE at aggregate creation and never
/// mutated (changing tenant is a re-creation, not an update).
/// </para>
/// </remarks>
/// <typeparam name="TTenantId">
/// The tenant-identifier type — typed value object (e.g. <c>StorefrontId</c>),
/// not a raw Guid.
/// </typeparam>
public interface IMultiTenant<TTenantId>
{
    /// <summary>The tenant this row belongs to. Immutable after creation.</summary>
    TTenantId TenantId { get; }
}
