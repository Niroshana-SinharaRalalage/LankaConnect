using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using LankaConnect.API.Controllers;
using LankaConnect.Products.LankaEvents.Application.Common;
using Xunit;

namespace LankaConnect.IntegrationTests.Controllers;

/// <summary>
/// Phase 6A.150 (hotfix) — pins the contract for the NEW public sponsors
/// endpoint added to fix the production login-redirect bug.
///
/// **The bug**: anonymous visitors to a paid event with sponsors enabled were
/// redirected to /login. Empirical RCA: `SponsorsPreviewStrip` + `SponsorSection`
/// call `useEventSponsors` which hits `GET /api/events/{id}/sponsors`, an
/// `[Authorize]` endpoint that returns rich PII (emails, phones, donation
/// amounts, Stripe fee detail). The 401 cascades through the api-client's
/// refresh path (which POSTs /Auth/refresh with no refresh token, returns 400),
/// and `AuthProvider.onUnauthorized` unconditionally redirects to /login.
///
/// **Path-B fix**: a NEW `[AllowAnonymous] GET /api/events/{id}/sponsors/public`
/// endpoint returns a sanitized DTO with ONLY the fields the public preview
/// strip actually displays — `Id`, `SponsorOrganization`, `SponsorName`,
/// `ItemName`, `ImageUrl`, `SponsorType`. Amount, email, phone, notes,
/// estimated value, currency, Stripe fee detail, and the internal user/blob
/// identifiers are PHYSICALLY ABSENT from the public DTO (compile-time PII
/// guarantee, reflection-asserted below).
///
/// The original organizer endpoint at <see cref="SponsorsController.GetEventSponsors"/>
/// stays `[Authorize]` (organizer-only PII view) — pinned by
/// <see cref="GetEventSponsors_Should_Remain_Authorize"/>.
/// </summary>
public class SponsorsControllerPublicEndpointTests
{
    // ---------- Endpoint attribute contract ----------

    [Fact]
    public void GetPublicEventSponsors_Should_Exist()
    {
        // The new public action must exist on the controller.
        var method = typeof(SponsorsController).GetMethod("GetPublicEventSponsors");
        Assert.NotNull(method);
    }

    [Fact]
    public void GetPublicEventSponsors_Should_Be_AllowAnonymous()
    {
        var method = typeof(SponsorsController).GetMethod("GetPublicEventSponsors");
        Assert.NotNull(method);
        var hasAllowAnonymous = method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true).Any();
        Assert.True(hasAllowAnonymous,
            "GetPublicEventSponsors must be [AllowAnonymous]: the whole purpose of this endpoint is to give " +
            "anonymous visitors a PII-free view of who has sponsored the event so SponsorsPreviewStrip + " +
            "SponsorSection can render publicly without triggering the auth-redirect chain.");
    }

    [Fact]
    public void GetEventSponsors_Should_Remain_Authorize()
    {
        // Regression guard: the original organizer endpoint MUST remain [Authorize].
        // It returns the full PII shape (emails, phones, amounts, Stripe fee detail).
        var method = typeof(SponsorsController).GetMethod(nameof(SponsorsController.GetEventSponsors));
        Assert.NotNull(method);
        var hasAuthorize = method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false).Any();
        Assert.True(hasAuthorize,
            "GetEventSponsors must remain [Authorize] — it returns the full PII shape (emails, donation amounts, " +
            "Stripe fee detail) and is only safe for organizers. The Phase 6A.150 fix adds a SEPARATE sanitized " +
            "public endpoint; it does NOT open the existing one.");
    }

    // ---------- PublicSponsorDto compile-time PII guarantee ----------

    [Fact]
    public void PublicSponsorDto_Should_Exist()
    {
        var t = typeof(PublicSponsorDto);
        Assert.NotNull(t);
    }

    [Fact]
    public void PublicSponsorDto_Exposes_OnlySafeFields()
    {
        // Whitelist of fields we INTEND to expose. If a future edit adds a field
        // not on this list, the test should be updated AFTER a privacy review.
        var safeProperties = new[]
        {
            "Id",
            "SponsorName",            // displayed under organization name on the card
            "SponsorOrganization",    // displayed as primary label on the card
            "ItemName",               // shown for Item-type sponsors
            "ImageUrl",               // sponsor logo
            "BrochureUrl",            // Phase 6A.162 — optional brochure/flyer; click-to-popup
            "SponsorType",            // "Money" or "Item" — drives card rendering
        };

        var actualProperties = typeof(PublicSponsorDto)
            .GetProperties()
            .Select(p => p.Name)
            .OrderBy(n => n)
            .ToArray();

        var unexpectedFields = actualProperties.Except(safeProperties).ToArray();
        Assert.True(unexpectedFields.Length == 0,
            $"PublicSponsorDto exposes unexpected fields that may leak PII: {string.Join(", ", unexpectedFields)}. " +
            "If adding a new field is intentional, update the safeProperties whitelist after a privacy review.");
    }

    [Theory]
    [InlineData("SponsorEmail")]
    [InlineData("SponsorPhone")]
    [InlineData("SponsorNotes")]
    [InlineData("SponsorUserId")]
    [InlineData("Amount")]
    [InlineData("EstimatedValue")]
    [InlineData("Currency")]
    [InlineData("StripeFeeAmount")]
    [InlineData("PlatformCommissionAmount")]
    [InlineData("OrganizerPayoutAmount")]
    [InlineData("ImageBlobName")]
    [InlineData("BrochureBlobName")] // Phase 6A.162 — brochure blob name MUST stay absent (PII contract)
    [InlineData("Status")]
    [InlineData("PaymentCompletedAt")]
    [InlineData("CreatedAt")]
    [InlineData("EventId")]
    [InlineData("ItemDescription")]
    public void PublicSponsorDto_DoesNotExpose_PiiOrFinancialOrInternalField(string forbiddenField)
    {
        // Defense-in-depth: every individual PII / financial / internal-detail field
        // must be physically absent. Reflection asserts each one explicitly so a
        // future field addition trips a clear, named failure.
        var property = typeof(PublicSponsorDto).GetProperty(forbiddenField);
        Assert.Null(property);
    }

    // ---------- PublicEventSponsorsResponse wrapper ----------

    [Fact]
    public void PublicEventSponsorsResponse_Should_Exist_AndCarryOnly_Sponsors()
    {
        // The top-level response wraps the list. Should NOT carry the organizer-only
        // SponsorSummaryDto (financial totals, count by status, etc.).
        var t = typeof(PublicEventSponsorsResponse);
        Assert.NotNull(t);

        var props = t.GetProperties().Select(p => p.Name).OrderBy(n => n).ToArray();
        // EventId is fine (it's the URL path parameter). Sponsors is the data.
        // No "Summary" field — that's organizer-only financials.
        Assert.Contains("Sponsors", props);
        Assert.DoesNotContain("Summary", props);
    }
}
