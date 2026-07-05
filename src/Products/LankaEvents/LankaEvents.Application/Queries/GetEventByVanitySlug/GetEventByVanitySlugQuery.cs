using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Common;
namespace LankaConnect.Products.LankaEvents.Application.Queries.GetEventByVanitySlug;

/// <summary>
/// Phase 6A.154: resolves a vanity slug to an event. Used by the
/// public-facing <c>/[slug]</c> Next.js route. Returns null when the slug
/// doesn't match any active event (caller renders 404).
/// </summary>
public record GetEventByVanitySlugQuery(string Slug) : IQuery<EventDto?>;
