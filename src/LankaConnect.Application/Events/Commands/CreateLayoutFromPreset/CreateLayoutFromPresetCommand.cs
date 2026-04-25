using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Application.Events.Commands.CreateLayoutFromPreset;

/// <summary>
/// Slice 6 Chunk S6.3: creates a new venue layout from a preset ID.
///
/// When <paramref name="EventId"/> is null the layout is created as a per-user
/// template (reusable in future events). When supplied, the layout is attached
/// to the event and the caller is expected to have already validated event
/// ownership at the controller / authorization layer.
/// </summary>
public record CreateLayoutFromPresetCommand(
    string PresetId,
    Guid CreatedByUserId,
    Guid? EventId
) : ICommand<VenueLayoutDto>;
