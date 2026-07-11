using LankaConnect.API.Extensions;
using LankaConnect.Products.LankaEvents.Application.Commands.ClearSponsorshipPackageImage;
using LankaConnect.Products.LankaEvents.Application.Commands.CreatePackageSponsor;
using LankaConnect.Products.LankaEvents.Application.Commands.CreateSponsorshipPackage;
using LankaConnect.Products.LankaEvents.Application.Commands.DeleteSponsorshipPackage;
using LankaConnect.Products.LankaEvents.Application.Commands.SetSponsorshipPackageImage;
using LankaConnect.Products.LankaEvents.Application.Commands.UpdateSponsorshipPackage;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.Products.LankaEvents.Application.Queries.GetActiveSponsorshipPackages;
using LankaConnect.Products.LankaEvents.Application.Queries.GetEventById;
using LankaConnect.Products.LankaEvents.Application.Queries.GetEventSponsorshipPackages;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace LankaConnect.Products.LankaEvents.Api.Controllers;

/// <summary>
/// Phase 6A.156 — REST endpoints for organizer-defined sponsorship packages
/// (Gold/Silver/Bronze tiers). Mirrors the <see cref="AddOnsController"/>
/// organizer surface: CRUD + image upload/clear, all gated by
/// <see cref="VerifyOrganizerAsync"/>.
///
/// PUBLIC (anonymous) read of active packages + the buyer purchase flow land
/// in 6A.157. In 6A.156, every endpoint requires an authenticated organizer.
///
/// Route: <c>api/events/{eventId}/sponsorship-packages</c>
/// </summary>
[ApiController]
[Route("api/events/{eventId}/sponsorship-packages")]
[Produces("application/json")]
public class SponsorshipPackagesController : BaseController<SponsorshipPackagesController>
{
    public SponsorshipPackagesController(
        IMediator mediator,
        ILogger<SponsorshipPackagesController> logger)
        : base(mediator, logger)
    {
    }

    // ──────────────────────────────────────────────
    // Organizer list
    // ──────────────────────────────────────────────

    /// <summary>
    /// Lists all sponsorship packages for an event (organizer only).
    /// Includes inactive (soft-deleted) packages so the organizer can re-activate them.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<SponsorshipPackageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSponsorshipPackages(Guid eventId)
    {
        Logger.LogInformation("GetSponsorshipPackages: EventId={EventId}", eventId);

        var authResult = await VerifyOrganizerAsync(eventId);
        if (authResult != null) return authResult;

        var result = await Mediator.Send(new GetEventSponsorshipPackagesQuery(eventId, IncludeInactive: true));
        return HandleResult(result);
    }

    // ──────────────────────────────────────────────
    // Phase 6A.157 — PUBLIC (anonymous) buyer endpoints
    // ──────────────────────────────────────────────

    /// <summary>
    /// Phase 6A.157 — anonymous list of buyable sponsorship packages for an
    /// event. Server-side filters: event Published + SponsorConfig.IsEnabled
    /// + EnablePackages + package.IsActive + (unlimited OR has remaining
    /// stock). Any gate failure returns an empty list (NOT an error) so the
    /// public FE stays quiet for events that haven't opted into packages.
    /// </summary>
    [HttpGet("active")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<SponsorshipPackagePublicDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetActiveSponsorshipPackages(Guid eventId)
    {
        Logger.LogInformation("GetActiveSponsorshipPackages: EventId={EventId}", eventId);

        var result = await Mediator.Send(new GetActiveSponsorshipPackagesQuery(eventId));
        return HandleResult(result);
    }

    /// <summary>
    /// Phase 6A.157 — anonymous purchase of a sponsorship package. Atomically
    /// reserves stock, snapshots package fields onto the Sponsor row, then
    /// either creates a Stripe Checkout session (paid packages) or completes
    /// the sponsor immediately with a sentinel intent (free $0 packages).
    ///
    /// Returns <see cref="CreatePackageSponsorResult"/>: <c>CheckoutUrl</c>
    /// (Stripe URL for paid; SuccessUrl directly for free) + <c>SponsorId</c>
    /// (so the FE can attach a buyer logo to the Pending sponsor BEFORE the
    /// Stripe redirect — mirrors 6A.145's widened CreateMoneySponsor pattern).
    ///
    /// On any post-stock-reservation failure, stock is restored idempotently
    /// per the same recovery pattern as PurchaseAddOnCommandHandler.
    /// </summary>
    [HttpPost("{packageId}/purchase")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CreatePackageSponsorResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PurchaseSponsorshipPackage(
        Guid eventId,
        Guid packageId,
        [FromBody] CreatePackageSponsorRequest request)
    {
        Logger.LogInformation(
            "PurchaseSponsorshipPackage: EventId={EventId}, PackageId={PackageId}, BuyerEmail={BuyerEmail}, BuyerOrg={BuyerOrg}",
            eventId, packageId, request.BuyerEmail, request.BuyerOrganization);

        // Allow either anonymous OR authenticated buyers. UserId snapshot lets
        // logged-in buyers see their package sponsorships in "My Sponsorships"
        // later; nullable so anonymous flows work without an account.
        var userId = User.Identity?.IsAuthenticated == true
            ? User.TryGetUserId()
            : null;

        var command = new CreatePackageSponsorCommand(
            EventId: eventId,
            PackageId: packageId,
            BuyerName: request.BuyerName,
            BuyerEmail: request.BuyerEmail,
            BuyerPhone: request.BuyerPhone,
            BuyerOrganization: request.BuyerOrganization,
            BuyerNotes: request.BuyerNotes,
            SuccessUrl: request.SuccessUrl,
            CancelUrl: request.CancelUrl,
            UserId: userId);

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    // ──────────────────────────────────────────────
    // Organizer CRUD
    // ──────────────────────────────────────────────

    /// <summary>
    /// Creates a new sponsorship package for an event (organizer only).
    /// Returns the new package ID so the FE can re-fetch and select it.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateSponsorshipPackage(
        Guid eventId,
        [FromBody] CreateSponsorshipPackageRequest request)
    {
        Logger.LogInformation(
            "CreateSponsorshipPackage: EventId={EventId}, Name={Name}, Price={Price} {Currency}, QuantityLimit={QuantityLimit}, IncludedTickets={IncludedTickets}",
            eventId, request.Name, request.Price, request.Currency, request.QuantityLimit, request.IncludedTicketCount);

        var authResult = await VerifyOrganizerAsync(eventId);
        if (authResult != null) return authResult;

        var command = new CreateSponsorshipPackageCommand(
            EventId: eventId,
            Name: request.Name,
            Description: request.Description,
            Price: request.Price,
            Currency: request.Currency ?? "USD",
            QuantityLimit: request.QuantityLimit,
            SortOrder: request.SortOrder,
            Tier: request.Tier,
            Perks: request.Perks,
            IncludedTicketCount: request.IncludedTicketCount);

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Updates an existing sponsorship package (organizer only).
    /// Setting <see cref="UpdateSponsorshipPackageRequest.IsActive"/> to false is the
    /// canonical soft-deactivate path (mirrors the AddOn pattern).
    /// </summary>
    [HttpPut("{packageId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSponsorshipPackage(
        Guid eventId,
        Guid packageId,
        [FromBody] UpdateSponsorshipPackageRequest request)
    {
        Logger.LogInformation(
            "UpdateSponsorshipPackage: EventId={EventId}, PackageId={PackageId}, Name={Name}, IsActive={IsActive}",
            eventId, packageId, request.Name, request.IsActive);

        var authResult = await VerifyOrganizerAsync(eventId);
        if (authResult != null) return authResult;

        var command = new UpdateSponsorshipPackageCommand(
            EventId: eventId,
            PackageId: packageId,
            Name: request.Name,
            Description: request.Description,
            Price: request.Price,
            Currency: request.Currency ?? "USD",
            QuantityLimit: request.QuantityLimit,
            SortOrder: request.SortOrder,
            Tier: request.Tier,
            Perks: request.Perks,
            IncludedTicketCount: request.IncludedTicketCount,
            IsActive: request.IsActive);

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Deletes a sponsorship package (organizer only).
    /// Soft-deletes (sets IsActive=false) when sponsors reference the package;
    /// hard-deletes when no sponsors reference it.
    /// </summary>
    [HttpDelete("{packageId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSponsorshipPackage(
        Guid eventId,
        Guid packageId)
    {
        Logger.LogInformation(
            "DeleteSponsorshipPackage: EventId={EventId}, PackageId={PackageId}",
            eventId, packageId);

        var authResult = await VerifyOrganizerAsync(eventId);
        if (authResult != null) return authResult;

        var result = await Mediator.Send(new DeleteSponsorshipPackageCommand(eventId, packageId));
        return result.IsSuccess ? NoContent() : HandleResult(result);
    }

    // ──────────────────────────────────────────────
    // Image upload / clear (organizer only)
    // ──────────────────────────────────────────────

    /// <summary>
    /// Uploads (or replaces) the display image for a sponsorship package.
    /// Returns the new image URL and blob name so the FE can update its cache.
    /// </summary>
    [HttpPost("{packageId}/image")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(SetSponsorshipPackageImageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetSponsorshipPackageImage(
        Guid eventId,
        Guid packageId,
        IFormFile image)
    {
        Logger.LogInformation(
            "SetSponsorshipPackageImage: EventId={EventId}, PackageId={PackageId}, FileName={FileName}, Size={Size}",
            eventId, packageId, image?.FileName, image?.Length);

        var authResult = await VerifyOrganizerAsync(eventId);
        if (authResult != null) return authResult;

        if (image is null || image.Length == 0)
            return BadRequest(new ProblemDetails { Title = "An image file is required." });

        using var ms = new MemoryStream();
        await image.CopyToAsync(ms);

        var command = new SetSponsorshipPackageImageCommand
        {
            EventId = eventId,
            PackageId = packageId,
            ImageData = ms.ToArray(),
            FileName = image.FileName
        };

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Clears the display image from a sponsorship package. Idempotent.
    /// </summary>
    [HttpDelete("{packageId}/image")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ClearSponsorshipPackageImage(Guid eventId, Guid packageId)
    {
        Logger.LogInformation(
            "ClearSponsorshipPackageImage: EventId={EventId}, PackageId={PackageId}",
            eventId, packageId);

        var authResult = await VerifyOrganizerAsync(eventId);
        if (authResult != null) return authResult;

        var result = await Mediator.Send(new ClearSponsorshipPackageImageCommand
        {
            EventId = eventId,
            PackageId = packageId
        });
        return result.IsSuccess ? NoContent() : HandleResult(result);
    }

    // ──────────────────────────────────────────────
    // Authorization helper (same shape as AddOnsController.VerifyOrganizerAsync)
    // ──────────────────────────────────────────────

    private async Task<IActionResult?> VerifyOrganizerAsync(Guid eventId)
    {
        var userId = User.GetUserId();

        var eventResult = await Mediator.Send(new GetEventByIdQuery(eventId));
        if (eventResult.IsFailure || eventResult.Value == null)
        {
            Logger.LogWarning(
                "SponsorshipPackages authorization failed: Event not found - EventId={EventId}, UserId={UserId}",
                eventId, userId);
            return NotFound(new { Error = "Event not found" });
        }

        if (eventResult.Value!.IsCurrentUserOrganizer != true)
        {
            Logger.LogWarning(
                "User {UserId} attempted to access sponsorship packages for event {EventId} without authorization (organizer: {OrganizerId})",
                userId, eventId, eventResult.Value.OrganizerId);
            return Forbid();
        }

        return null; // Authorized
    }
}

// ──────────────────────────────────────────────
// Request DTOs
// ──────────────────────────────────────────────

/// <summary>
/// Request body for creating a sponsorship package.
/// </summary>
public class CreateSponsorshipPackageRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public string? Currency { get; init; }
    public int? QuantityLimit { get; init; }
    public int SortOrder { get; init; }
    public string? Tier { get; init; }
    public List<string>? Perks { get; init; }
    public int IncludedTicketCount { get; init; }
}

/// <summary>
/// Phase 6A.157 — request body for the public package-purchase endpoint.
/// Buyer fields are snapshotted onto the Sponsor row by
/// <see cref="CreatePackageSponsorCommand"/>; Success/Cancel URLs are forwarded
/// to Stripe Checkout for paid packages and used directly for free ones.
/// </summary>
public class CreatePackageSponsorRequest
{
    public required string BuyerName { get; init; }
    public required string BuyerEmail { get; init; }
    public string? BuyerPhone { get; init; }
    public string? BuyerOrganization { get; init; }
    public string? BuyerNotes { get; init; }
    public required string SuccessUrl { get; init; }
    public required string CancelUrl { get; init; }
}

/// <summary>
/// Request body for updating a sponsorship package. Set
/// <see cref="IsActive"/> = false to soft-deactivate.
/// </summary>
public class UpdateSponsorshipPackageRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public string? Currency { get; init; }
    public int? QuantityLimit { get; init; }
    public int SortOrder { get; init; }
    public string? Tier { get; init; }
    public List<string>? Perks { get; init; }
    public int IncludedTicketCount { get; init; }
    public bool IsActive { get; init; } = true;
}
