using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Products.LankaEvents.Application.Commands.ApplyPresetToEvent;

/// <summary>
/// Slice 9.2: atomic "pick a preset and attach it to my event" command.
///
/// <para>
/// Replaces the broken from-preset → assign two-step flow. In one transaction:
/// <list type="number">
///   <item>Build a layout from the supplied preset id.</item>
///   <item>Persist the layout (with zones / tables / decorations / seats — all
///         tier-less; the organiser maps tiers in the canvas editor later).</item>
///   <item>Detach any layout currently attached to the event (the previous
///         layout becomes an orphan candidate, cleaned by the next sweep of
///         the <c>Slice93HardDeleteOrphanLayouts</c> migration / housekeeping
///         job).</item>
///   <item>Call <c>Event.EnableAssignedSeating(newLayoutId)</c> — sets
///         <c>VenueLayoutId</c> + flips <c>SeatingMode</c> to
///         <c>AssignedSeating</c> in one step.</item>
///   <item>Commit. Rollback semantics: if any step fails the layout never
///         persists and the event is unchanged.</item>
/// </list>
/// </para>
///
/// <para>
/// Per architect Rev 3 (no auto-tier-mapping): zones arrive with no
/// <c>tier_assignments</c>. <c>VenueLayout.ValidateForEvent</c> is called
/// with <c>requireTierMapping: false</c>, so the structural check is the
/// only gate at apply time. Strict tier-mapping enforcement happens at
/// publish time via Slice 9.1's <c>Event.CheckLayoutPublishReadiness</c>.
/// </para>
/// </summary>
public record ApplyPresetToEventCommand(
    string PresetId,
    Guid EventId,
    Guid AppliedByUserId
) : ICommand<VenueLayoutDto>;
