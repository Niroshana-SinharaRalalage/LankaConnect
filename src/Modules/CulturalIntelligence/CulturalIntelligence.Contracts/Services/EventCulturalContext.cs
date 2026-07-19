namespace LankaConnect.Modules.CulturalIntelligence.Contracts.Services;

/// <summary>
/// Cultural-projection snapshot of an event used by <see cref="ICulturalCalendar"/>.
///
/// Wave 8.5 Tech Lead D-13 (Option A, 2026-07-19):
/// Introduced so ICulturalCalendar can live in CulturalIntelligence.Contracts without
/// type-depending on <c>LankaConnect.Products.LankaEvents.Domain.Event</c> — the
/// precise inversion that blocked LayerInversion Inversion 3.
///
/// Callers (e.g. <c>EventRecommendationEngine</c> in LankaEvents.Domain) build this
/// DTO from an Event at the call boundary.  Cultural-calendar impls consume only
/// primitives + this DTO — never a Product-layer aggregate.
/// </summary>
/// <param name="EventId">Primary key of the source event.</param>
/// <param name="StartDate">Event start date/time; null for TBD events.</param>
/// <param name="Category">Event category label (e.g. "Religious", "Cultural", "Secular"). Optional.</param>
/// <param name="LocationRegion">Broad region/city for diaspora awareness (e.g. "Toronto", "Colombo"). Optional.</param>
public sealed record EventCulturalContext(
    Guid EventId,
    DateTime? StartDate,
    string? Category = null,
    string? LocationRegion = null);
