namespace LankaConnect.Application.Events.Common;

/// <summary>
/// Phase 6A.150 — sanitized, PII-FREE projection of a sponsor for the public event
/// detail page. Returned ONLY by the <c>[AllowAnonymous] GET /api/events/{id}/sponsors/public</c>
/// endpoint. The full <see cref="SponsorDto"/> (with email, phone, amount, Stripe fee
/// detail, notes, internal blob names) stays gated behind the organizer-only
/// <c>[Authorize] GET /api/events/{id}/sponsors</c>.
///
/// Compile-time PII guarantee: the fields below are the ONLY fields on this record.
/// A reflection-asserted whitelist test in <c>SponsorsControllerPublicEndpointTests</c>
/// trips if a future edit adds a property that wasn't on the whitelist after a
/// privacy review.
///
/// Fields intentionally absent (each individually reflection-tested):
///   SponsorEmail, SponsorPhone, SponsorNotes, SponsorUserId,
///   Amount, EstimatedValue, Currency,
///   StripeFeeAmount, PlatformCommissionAmount, OrganizerPayoutAmount,
///   ImageBlobName (internal storage identifier),
///   BrochureBlobName (internal storage identifier — Phase 6A.162),
///   Status (filter is done server-side; client doesn't need to discriminate),
///   PaymentCompletedAt, CreatedAt (timing-correlation surface),
///   EventId (already in the URL path),
///   ItemDescription (free-text — possible PII).
///
/// Phase 6A.162 — added <see cref="BrochureUrl"/> to the public projection so
/// the click-to-popup brochure flow works for anonymous visitors. The blob
/// name STAYS ABSENT (internal storage identifier).
/// </summary>
public record PublicSponsorDto
{
    public Guid Id { get; init; }
    /// <summary>The sponsor's organization label if provided; otherwise the SponsorName form is the primary card title.</summary>
    public string? SponsorOrganization { get; init; }
    /// <summary>Display name as the sponsor provided it (used when SponsorOrganization is null).</summary>
    public string SponsorName { get; init; } = string.Empty;
    /// <summary>For Item-type sponsors, the item label shown on the card.</summary>
    public string? ItemName { get; init; }
    /// <summary>Sponsor logo / image URL — the whole reason the public preview exists.</summary>
    public string? ImageUrl { get; init; }
    /// <summary>
    /// Phase 6A.162 — optional brochure/flyer URL. When set, the click-to-popup flow
    /// on the public sponsor strip shows the brochure full-size; when null, the
    /// popup falls back to the logo. <c>BrochureBlobName</c> STAYS ABSENT from this
    /// public projection (internal storage identifier; PII contract preserved).
    /// </summary>
    public string? BrochureUrl { get; init; }
    /// <summary>"Money" or "Item" — drives client-side rendering branch (Item gets an item-name caption).</summary>
    public string SponsorType { get; init; } = string.Empty;
}
