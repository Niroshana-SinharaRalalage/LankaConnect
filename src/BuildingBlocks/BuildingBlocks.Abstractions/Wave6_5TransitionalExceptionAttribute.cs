namespace LankaConnect.BuildingBlocks.Abstractions;

/// <summary>
/// Marks a type as having a known, time-bounded dependency on the legacy
/// <c>LankaConnect.Infrastructure.Data</c> + <c>LankaConnect.Infrastructure.Data.Repositories</c>
/// namespaces. These dependencies exist solely because Wave 5.0 declared
/// <c>AppDbContext</c> and the generic <c>Repository&lt;T&gt;</c> base as transitional anchors
/// that Products.LankaEvents.Infrastructure consumes via ProjectReference.
/// </summary>
/// <remarks>
/// <para>
/// Architect-mandated 2026-06-29 (Wave 5.5.a). The transitional dependency itself
/// is governed by an explicit ArchTest in <c>ProductsLayerRules</c>: classes that
/// reference the allowed legacy namespaces MUST carry this attribute, and classes
/// that carry the attribute MUST be removed when Wave 6.5 (Outbox cutover) ships
/// <c>LankaEventsDbContext</c> + extracts the legacy AppDbContext binding.
/// </para>
/// <para>
/// Wave 6.5 contributors: <c>grep -rln "Wave6_5TransitionalException" src/Products/</c>
/// returns the exact set of files that need re-wiring against the new module DbContext.
/// Once the cycle to legacy is broken, this attribute can be deleted along with every
/// usage site.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class Wave6_5TransitionalExceptionAttribute : Attribute
{
    /// <summary>
    /// Short description of why this type carries the transitional dependency.
    /// Surfaces in Wave 6.5 cleanup logs to make the debt explicit.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Creates the attribute with a required reason.
    /// </summary>
    /// <param name="reason">
    /// E.g. <c>"Uses LankaConnect.Infrastructure.Data.AppDbContext"</c> or
    /// <c>"Inherits LankaConnect.Infrastructure.Data.Repositories.Repository&lt;T&gt; base class"</c>.
    /// </param>
    public Wave6_5TransitionalExceptionAttribute(string reason)
    {
        Reason = reason;
    }
}
