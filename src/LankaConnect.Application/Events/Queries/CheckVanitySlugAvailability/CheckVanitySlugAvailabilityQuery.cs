using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Queries.CheckVanitySlugAvailability;

/// <summary>
/// Phase 6A.154: real-time availability check for an organizer-entered vanity
/// slug. Returns the validation outcome (shape + reservation + uniqueness) so
/// the organizer form can give instant feedback before submit.
/// </summary>
public record CheckVanitySlugAvailabilityQuery(string Slug) : IQuery<VanitySlugAvailabilityResult>;

/// <param name="Available">true when the slug passes ALL checks.</param>
/// <param name="Reason">
///   When Available=false, one of: <c>"invalid"</c>, <c>"reserved"</c>,
///   <c>"taken"</c>. Null when Available=true.
/// </param>
/// <param name="Message">Human-readable explanation for the FE field error.</param>
public record VanitySlugAvailabilityResult(bool Available, string? Reason, string Message);
