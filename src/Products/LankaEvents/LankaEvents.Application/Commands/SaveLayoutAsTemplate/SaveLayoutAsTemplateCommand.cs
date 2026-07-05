using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.Domain.Common;
namespace LankaConnect.Products.LankaEvents.Application.Commands.SaveLayoutAsTemplate;

/// <summary>
/// Slice 8 S8.9b: clones an existing venue layout as a per-user template.
/// The new layout has <c>EventId == null</c>, <c>IsTemplate == true</c>,
/// <c>CreatedByUserId == NewOwnerUserId</c>, and a fresh server-side ID.
/// Zones, tables, decorations, canvas, and per-seat <c>IsEnabled</c> /
/// <c>IsAccessible</c> flags round-trip via
/// <see cref="LankaConnect.Products.LankaEvents.Domain.Entities.VenueLayout.CloneAsTemplate"/>.
/// Tier mappings live on the <c>TicketTier</c> aggregate (owned by the
/// source's event) and are deliberately dropped — templates are tier-free.
///
/// Authorization: caller must be allowed to write the source layout
/// (template-creator for templates, event-organizer for event-attached
/// layouts) — we reuse <c>ILayoutAuthorizationService.AuthorizeAsync</c>.
/// "Anyone-with-view" cloning is deferred until view-only roles exist.
/// </summary>
public record SaveLayoutAsTemplateCommand(
    Guid SourceLayoutId,
    Guid NewOwnerUserId,
    string TemplateName
) : ICommand<VenueLayoutDto>;
