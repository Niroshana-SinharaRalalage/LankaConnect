using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Products.LankaEvents.Application.Queries.GetUserTemplates;

/// <summary>
/// Slice 8 S8.10: lists every <see cref="LankaConnect.Products.LankaEvents.Domain.Entities.VenueLayout"/>
/// where <c>IsTemplate == true</c> and <c>CreatedByUserId == UserId</c>, ordered
/// most-recent-first (matches the repo's <c>OrderByDescending(CreatedAt)</c>).
/// Powers the canvas editor's "My Templates" picker tab. Tier assignments are
/// always empty for templates by S8.9b's design — the DTO mapper produces an
/// empty <c>TicketTierIds</c> list per zone/table without a database hit.
/// </summary>
public record GetUserTemplatesQuery(Guid UserId) : IQuery<IReadOnlyList<VenueLayoutDto>>;
