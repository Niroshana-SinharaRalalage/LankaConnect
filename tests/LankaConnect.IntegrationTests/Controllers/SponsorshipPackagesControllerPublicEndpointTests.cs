using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using LankaConnect.API.Controllers;
using Xunit;

namespace LankaConnect.IntegrationTests.Controllers;

/// <summary>
/// Phase 6A.157 [4/6] — pins the public-endpoint contract for sponsorship
/// package buyers. Two endpoints added to <see cref="SponsorshipPackagesController"/>
/// alongside the existing organizer-only CRUD:
///
///   1. <c>GET  /api/events/{eventId}/sponsorship-packages/active</c>  →  anonymous list of buyable packages
///   2. <c>POST /api/events/{eventId}/sponsorship-packages/{packageId}/purchase</c>  →  anonymous purchase / Stripe checkout
///
/// Tests are reflection-based per the same rationale as the deferred
/// handler tests in commit [2/6] (full event-aggregate fixtures are
/// prohibitively expensive without an Event test-builder). The real behaviour
/// validation comes from the staging API smoke post-deploy. These tests pin
/// the load-bearing contract: route, HTTP verb, anonymous gate, and DTO
/// shape — caught at build time so a future refactor can't accidentally
/// flip Authorize on the buyer endpoints (which would silently break the
/// public purchase flow per CLAUDE.md memory feedback_401_does_not_prove_feature_reachable).
/// </summary>
public class SponsorshipPackagesControllerPublicEndpointTests
{
    // ──────────────────────────────────────────────
    // GET /active — anonymous list of buyable packages
    // ──────────────────────────────────────────────

    [Fact]
    public void GetActiveSponsorshipPackages_Should_Exist()
    {
        var method = typeof(SponsorshipPackagesController).GetMethod("GetActiveSponsorshipPackages");
        Assert.NotNull(method);
    }

    [Fact]
    public void GetActiveSponsorshipPackages_Should_Be_AllowAnonymous()
    {
        var method = typeof(SponsorshipPackagesController).GetMethod("GetActiveSponsorshipPackages");
        Assert.NotNull(method);
        var hasAllowAnonymous = method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true).Any();
        Assert.True(hasAllowAnonymous,
            "GetActiveSponsorshipPackages must be [AllowAnonymous]: the whole purpose of this endpoint " +
            "is to give anonymous buyers the package list so the buyer cards can render on the public " +
            "event page without triggering the auth-redirect chain (per the same RCA as 6A.150).");
    }

    [Fact]
    public void GetActiveSponsorshipPackages_Should_Have_Active_Sub_Route()
    {
        var method = typeof(SponsorshipPackagesController).GetMethod("GetActiveSponsorshipPackages");
        Assert.NotNull(method);
        var httpGet = method!
            .GetCustomAttributes(typeof(HttpGetAttribute), inherit: true)
            .Cast<HttpGetAttribute>()
            .FirstOrDefault();
        Assert.NotNull(httpGet);
        Assert.True(
            httpGet!.Template == "active",
            $"Endpoint must be mounted at /active so the URL is " +
            $"GET /api/events/{{eventId}}/sponsorship-packages/active — distinct from the organizer " +
            $"list (GET /) which stays [Authorize] and returns inactive rows too. Actual template: '{httpGet.Template}'.");
    }

    // ──────────────────────────────────────────────
    // POST /{packageId}/purchase — anonymous purchase
    // ──────────────────────────────────────────────

    [Fact]
    public void PurchaseSponsorshipPackage_Should_Exist()
    {
        var method = typeof(SponsorshipPackagesController).GetMethod("PurchaseSponsorshipPackage");
        Assert.NotNull(method);
    }

    [Fact]
    public void PurchaseSponsorshipPackage_Should_Be_AllowAnonymous()
    {
        var method = typeof(SponsorshipPackagesController).GetMethod("PurchaseSponsorshipPackage");
        Assert.NotNull(method);
        var hasAllowAnonymous = method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true).Any();
        Assert.True(hasAllowAnonymous,
            "PurchaseSponsorshipPackage must be [AllowAnonymous]: buyers without an account must be " +
            "able to start a Stripe checkout. The CreatePackageSponsorCommand accepts UserId = null " +
            "for anonymous flows per 6A.157 final scope.");
    }

    [Fact]
    public void PurchaseSponsorshipPackage_Should_Have_PackageId_Purchase_Route()
    {
        var method = typeof(SponsorshipPackagesController).GetMethod("PurchaseSponsorshipPackage");
        Assert.NotNull(method);
        var httpPost = method!
            .GetCustomAttributes(typeof(HttpPostAttribute), inherit: true)
            .Cast<HttpPostAttribute>()
            .FirstOrDefault();
        Assert.NotNull(httpPost);
        Assert.True(
            httpPost!.Template == "{packageId}/purchase",
            $"Endpoint must be mounted at {{packageId}}/purchase so the URL is " +
            $"POST /api/events/{{eventId}}/sponsorship-packages/{{packageId}}/purchase — distinct from the " +
            $"organizer CRUD POST (POST /) which stays [Authorize] and creates new package DEFINITIONS. " +
            $"Actual template: '{httpPost.Template}'.");
    }

    // ──────────────────────────────────────────────
    // Request DTO contract — what buyers must send
    // ──────────────────────────────────────────────

    [Fact]
    public void PurchaseSponsorshipPackageRequest_Should_Carry_Buyer_Identity_And_Redirect_Urls()
    {
        // CreatePackageSponsorRequest must expose the fields the Stripe checkout
        // session needs (buyer identity for the Sponsor row + redirect URLs for
        // success/cancel after Stripe). Pinned by name so a rename doesn't
        // silently break the FE request shape.
        var t = Type.GetType("LankaConnect.API.Controllers.CreatePackageSponsorRequest, LankaConnect.API");
        Assert.NotNull(t);

        // Required fields the buyer MUST send
        Assert.NotNull(t!.GetProperty("BuyerName"));
        Assert.NotNull(t!.GetProperty("BuyerEmail"));
        Assert.NotNull(t!.GetProperty("SuccessUrl"));
        Assert.NotNull(t!.GetProperty("CancelUrl"));

        // Optional fields the buyer MAY send (snapshot onto Sponsor row)
        Assert.NotNull(t!.GetProperty("BuyerPhone"));
        Assert.NotNull(t!.GetProperty("BuyerOrganization"));
        Assert.NotNull(t!.GetProperty("BuyerNotes"));
    }
}
