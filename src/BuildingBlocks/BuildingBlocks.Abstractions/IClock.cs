namespace LankaConnect.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Abstracts wall-clock time so domain + infrastructure code can be tested
/// deterministically. Replaces direct <c>DateTime.UtcNow</c> calls so tests
/// can supply a fixed/advancing clock and audit-stamped behavior becomes
/// asserted-on rather than tolerated.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why an interface, not just <c>TimeProvider</c></b>: TimeProvider lands
/// in .NET 8 and is the future-proof choice for many scenarios, but our test
/// patterns (FakeClock that advances on Tick(), AdvanceableClock that exposes
/// AdvanceBy(TimeSpan)) are simpler with a dedicated interface than with
/// TimeProvider's broader surface. SystemClock implementation is a thin
/// wrapper around TimeProvider.System.GetUtcNow() so the .NET integration
/// stays clean.
/// </para>
/// <para>
/// <b>Lifetime</b>: typically singleton in DI (SystemClock has no per-request
/// state). Test contexts inject a per-test FakeClock as scoped/transient.
/// </para>
/// <para>
/// <b>UTC only</b>: all timestamps in LankaConnect are stored UTC. Local-time
/// formatting is a presentation concern handled by <c>ITimeZoneService</c>
/// in <c>SharedKernel.Time</c>.
/// </para>
/// </remarks>
public interface IClock
{
    /// <summary>Current UTC wall-clock time.</summary>
    DateTime UtcNow { get; }
}
