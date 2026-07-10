using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Common;
namespace LankaConnect.Products.LankaEvents.Application.Commands.CreateLayoutFromTemplate;

/// <summary>
/// Slice 8 S8.10: applies a saved template to a target event. Mirror of
/// <c>CreateLayoutFromPresetCommand</c> for user-saved templates: the source
/// is a <see cref="LankaConnect.Products.LankaEvents.Domain.Entities.VenueLayout"/> with
/// <c>IsTemplate == true</c> + <c>EventId == null</c>; the result is a fresh
/// event-attached layout via <see cref="LankaConnect.Products.LankaEvents.Domain.Entities.VenueLayout.CloneFromTemplate"/>.
///
/// Authorization: caller must own the source template (<c>CreatedByUserId</c>
/// match) AND the target event (<c>OrganizerId</c> match) — same gate the
/// preset flow applies for the event side, plus the template-ownership gate
/// is the same one save-as-template uses.
/// </summary>
public record CreateLayoutFromTemplateCommand(
    Guid SourceTemplateId,
    Guid CreatedByUserId,
    Guid EventId,
    string? LayoutName
) : ICommand<VenueLayoutDto>;
