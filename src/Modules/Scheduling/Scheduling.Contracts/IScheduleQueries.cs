namespace LankaConnect.Modules.Scheduling.Contracts;

/// <summary>
/// Cross-module read API for scheduling primitives. Wave 4.8.a (2026-06-26) skeleton —
/// methods are added as Wave 5 Products carve-out + Cross-cutting cleanup discover
/// concrete cross-module use cases. Today the contract advertises the boundary so
/// the assembly + ProjectReference graph is in place.
/// </summary>
/// <remarks>
/// Future products (LankaTemples puja-slot, LankaSeyla appointment) will inject this
/// surface to ask "what scheduled occurrences exist for resource X?" without needing
/// to depend on the LankaEvents Event aggregate. Read-only by design — mutations go
/// through <see cref="IScheduleCommands"/>.
/// </remarks>
public interface IScheduleQueries
{
}
