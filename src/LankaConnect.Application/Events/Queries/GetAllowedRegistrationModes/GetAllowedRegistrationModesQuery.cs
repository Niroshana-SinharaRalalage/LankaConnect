using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Application.Events.Queries.GetAllowedRegistrationModes;

/// <summary>
/// Phase 7E.2: Query the set of <see cref="RegistrationMode"/> values that are compatible
/// with a given event shape. Drives the frontend mode picker so disabled options match
/// the server-side validator (architect hot-spot #5: re-query on every form-state change
/// so toggling pricing/seating after Mode C is selected auto-clears the invalid selection).
///
/// Shape parameters describe a draft event the organiser is building or editing. All are
/// optional and default to <c>false</c> (= safest: most modes allowed).
///
/// The handler delegates to <see cref="Domain.Events.Services.RegistrationModeCompatibility.AllowedModes"/>
/// — single source of truth shared with <c>CreateEventCommandHandler</c> + <c>UpdateEventCommandHandler</c>.
/// </summary>
public record GetAllowedRegistrationModesQuery(
    bool IsFreeAttendance = false,
    bool HasSeating = false,
    bool HasNamedSeating = false,
    bool RequiresAttendeeNameOnTicket = false,
    bool HasDualPricing = false,
    bool HasGroupTiers = false,
    bool HasTicketTiers = false,
    bool HasIdentityBoundAddOn = false,
    bool HasMatrixPricing = false
) : IQuery<IReadOnlyList<RegistrationMode>>;
