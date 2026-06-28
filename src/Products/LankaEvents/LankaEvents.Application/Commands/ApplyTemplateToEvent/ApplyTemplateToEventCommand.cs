using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Products.LankaEvents.Application.Commands.ApplyTemplateToEvent;

/// <summary>
/// Slice 9.2: atomic "apply one of my saved templates to my event" command.
/// Mirror of <see cref="ApplyPresetToEvent.ApplyPresetToEventCommand"/> for
/// the user-template path. Replaces from-template + assign two-step.
/// </summary>
public record ApplyTemplateToEventCommand(
    Guid SourceTemplateId,
    Guid EventId,
    Guid AppliedByUserId,
    string? LayoutName
) : ICommand<VenueLayoutDto>;
