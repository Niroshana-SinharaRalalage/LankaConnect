using LankaConnect.Products.LankaEvents.Contracts;
using LankaConnect.Hosts.AllInOne.Extensions;
using LankaConnect.Products.LankaEvents.Application.Commands.ClearSponsorBrochure;
using LankaConnect.Products.LankaEvents.Application.Commands.ClearSponsorImage;
using LankaConnect.Products.LankaEvents.Application.Commands.SetSponsorBrochure;
using LankaConnect.Products.LankaEvents.Application.Commands.CreateOffPlatformSponsor;
using LankaConnect.Products.LankaEvents.Application.Commands.CreateSponsor;
using LankaConnect.Products.LankaEvents.Application.Commands.SetSponsorImage;
using LankaConnect.Products.LankaEvents.Application.Commands.UpdateSponsor;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.Products.LankaEvents.Application.Queries.ExportEventAttendees;
using LankaConnect.Products.LankaEvents.Application.Queries.ExportSponsors;
using LankaConnect.Products.LankaEvents.Application.Queries.GetEventById;
using LankaConnect.Products.LankaEvents.Application.Queries.GetEventSponsors;
using LankaConnect.Products.LankaEvents.Application.Queries.GetPublicEventSponsors;  // Phase 6A.150
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.SharedKernel.Money; // 4C.d.xiii: Currency VO
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace LankaConnect.Products.LankaEvents.Api.Controllers;

/// <summary>
/// Sponsor Feature: REST endpoints for event sponsorships.
/// Supports two creation modes:
///   - Money sponsors → Stripe checkout (returns URL)
///   - Item sponsors → No payment, immediate recording (returns sponsor ID)
/// Route: api/events/{eventId}/sponsors
/// </summary>
[ApiController]
[Route("api/events/{eventId}/sponsors")]
[Produces("application/json")]
public class SponsorsController : BaseController<SponsorsController>
{
    private readonly ISponsorRepository _sponsorRepository;

    public SponsorsController(
        IMediator mediator,
        ILogger<SponsorsController> logger,
        ISponsorRepository sponsorRepository)
        : base(mediator, logger)
    {
        _sponsorRepository = sponsorRepository;
    }

    /// <summary>
    /// Creates a money-based sponsorship for an event.
    /// Returns a Stripe checkout URL for payment.
    /// Anonymous users can sponsor without authentication.
    /// </summary>
    [HttpPost("money")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CreateMoneySponsorResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateMoneySponsor(
        Guid eventId,
        [FromBody] CreateMoneySponsorRequest request)
    {
        Logger.LogInformation(
            "CreateMoneySponsor: EventId={EventId}, SponsorEmail={SponsorEmail}, Amount={Amount}",
            eventId, request.SponsorEmail, request.Amount);

        var userId = User.Identity?.IsAuthenticated == true
            ? User.TryGetUserId()
            : null;

        var command = new CreateMoneySponsorCommand(
            EventId: eventId,
            SponsorName: request.SponsorName,
            SponsorEmail: request.SponsorEmail,
            SponsorPhone: request.SponsorPhone,
            SponsorOrganization: request.SponsorOrganization,
            SponsorNotes: request.SponsorNotes,
            Amount: request.Amount,
            Currency: request.Currency ?? "USD",
            SuccessUrl: request.SuccessUrl,
            CancelUrl: request.CancelUrl,
            UserId: userId);

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Creates an item-based sponsorship for an event.
    /// No payment required — the sponsor entity is recorded immediately.
    /// Returns the sponsor ID.
    /// Anonymous users can sponsor without authentication.
    /// </summary>
    [HttpPost("item")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateItemSponsor(
        Guid eventId,
        [FromBody] CreateItemSponsorRequest request)
    {
        Logger.LogInformation(
            "CreateItemSponsor: EventId={EventId}, SponsorEmail={SponsorEmail}, ItemName={ItemName}",
            eventId, request.SponsorEmail, request.ItemName);

        var userId = User.Identity?.IsAuthenticated == true
            ? User.TryGetUserId()
            : null;

        var command = new CreateItemSponsorCommand(
            EventId: eventId,
            SponsorName: request.SponsorName,
            SponsorEmail: request.SponsorEmail,
            SponsorPhone: request.SponsorPhone,
            SponsorOrganization: request.SponsorOrganization,
            SponsorNotes: request.SponsorNotes,
            ItemName: request.ItemName,
            ItemDescription: request.ItemDescription,
            EstimatedValue: request.EstimatedValue,
            UserId: userId);

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Phase 6A.145 Commit 6 — uploads (or replaces) a sponsor's image. Any sponsor
    /// can attach an image regardless of amount (threshold gate removed per UAT).
    /// Public access by sponsor-id knowledge (the sponsor ID was just returned to the
    /// caller from CreateMoneySponsor or CreateItemSponsor); organizer auth not required.
    /// </summary>
    [HttpPost("{sponsorId:guid}/image")]
    [AllowAnonymous]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(SetSponsorImageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetSponsorImage(
        Guid eventId,
        Guid sponsorId,
        IFormFile image)
    {
        Logger.LogInformation(
            "SetSponsorImage: EventId={EventId}, SponsorId={SponsorId}, FileName={FileName}, Size={Size}",
            eventId, sponsorId, image?.FileName, image?.Length);

        if (image is null || image.Length == 0)
            return BadRequest(new ProblemDetails { Title = "An image file is required." });

        using var ms = new MemoryStream();
        await image.CopyToAsync(ms);

        var command = new SetSponsorImageCommand
        {
            EventId = eventId,
            SponsorId = sponsorId,
            ImageData = ms.ToArray(),
            FileName = image.FileName,
        };

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Phase 6A.145 — clears a sponsor's image. Idempotent.
    /// Phase 6A.151 H9 — authorization extended: allowed if caller is the
    /// sponsor's owner (non-anonymous, JWT subject matches Sponsor.SponsorUserId)
    /// OR an organizer of the parent event. Anonymous sponsors remain
    /// organizer-only (claim-by-email magic-link deferred to a separate phase).
    /// </summary>
    [HttpDelete("{sponsorId:guid}/image")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ClearSponsorImage(Guid eventId, Guid sponsorId)
    {
        Logger.LogInformation(
            "ClearSponsorImage: EventId={EventId}, SponsorId={SponsorId}", eventId, sponsorId);

        // Phase 6A.151 H9 — sponsor-self pre-check before falling through to organizer auth.
        // Allows a non-anonymous sponsor to clear their own image without organizer rights.
        var currentUserId = User.TryGetUserId();
        var sponsor = await _sponsorRepository.GetByIdAsync(sponsorId);
        if (sponsor != null && sponsor.EventId == eventId
            && sponsor.SponsorUserId.HasValue
            && currentUserId.HasValue
            && sponsor.SponsorUserId.Value == currentUserId.Value)
        {
            Logger.LogInformation(
                "ClearSponsorImage: sponsor-self path — SponsorUserId={SponsorUserId}",
                sponsor.SponsorUserId);
        }
        else
        {
            var authResult = await VerifyOrganizerAsync(eventId);
            if (authResult != null) return authResult;
        }

        var result = await Mediator.Send(new ClearSponsorImageCommand
        {
            EventId = eventId,
            SponsorId = sponsorId,
        });
        return result.IsSuccess ? NoContent() : HandleResult(result);
    }

    /// <summary>
    /// Phase 6A.162 — uploads (or replaces) a sponsor's brochure/flyer image
    /// (the optional sibling slot to the logo). Mirrors <see cref="SetSponsorImage"/>
    /// authz model exactly: public access by sponsor-id knowledge (the ID was just
    /// returned from the create-sponsor command); same 5MB cap + MIME guards
    /// enforced server-side by <c>IImageService.ValidateImage</c>.
    /// </summary>
    [HttpPost("{sponsorId:guid}/brochure")]
    [AllowAnonymous]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(SetSponsorBrochureResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetSponsorBrochure(
        Guid eventId,
        Guid sponsorId,
        IFormFile image)
    {
        Logger.LogInformation(
            "SetSponsorBrochure: EventId={EventId}, SponsorId={SponsorId}, FileName={FileName}, Size={Size}",
            eventId, sponsorId, image?.FileName, image?.Length);

        if (image is null || image.Length == 0)
            return BadRequest(new ProblemDetails { Title = "A brochure image file is required." });

        using var ms = new MemoryStream();
        await image.CopyToAsync(ms);

        var command = new SetSponsorBrochureCommand
        {
            EventId = eventId,
            SponsorId = sponsorId,
            ImageData = ms.ToArray(),
            FileName = image.FileName,
        };

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Phase 6A.162 — clears a sponsor's brochure. Idempotent. Mirrors
    /// <see cref="ClearSponsorImage"/> authz model verbatim: allowed if caller is the
    /// sponsor's owner OR organizer of the parent event.
    /// </summary>
    [HttpDelete("{sponsorId:guid}/brochure")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ClearSponsorBrochure(Guid eventId, Guid sponsorId)
    {
        Logger.LogInformation(
            "ClearSponsorBrochure: EventId={EventId}, SponsorId={SponsorId}", eventId, sponsorId);

        // Phase 6A.151 H9 sponsor-self pre-check, lifted from ClearSponsorImage.
        var currentUserId = User.TryGetUserId();
        var sponsor = await _sponsorRepository.GetByIdAsync(sponsorId);
        if (sponsor != null && sponsor.EventId == eventId
            && sponsor.SponsorUserId.HasValue
            && currentUserId.HasValue
            && sponsor.SponsorUserId.Value == currentUserId.Value)
        {
            Logger.LogInformation(
                "ClearSponsorBrochure: sponsor-self path — SponsorUserId={SponsorUserId}",
                sponsor.SponsorUserId);
        }
        else
        {
            var authResult = await VerifyOrganizerAsync(eventId);
            if (authResult != null) return authResult;
        }

        var result = await Mediator.Send(new ClearSponsorBrochureCommand
        {
            EventId = eventId,
            SponsorId = sponsorId,
        });
        return result.IsSuccess ? NoContent() : HandleResult(result);
    }

    /// <summary>
    /// Phase 6A.151 — updates content fields on an existing sponsor (PATCH).
    /// Authorization: organizer of the parent event OR the sponsor themselves
    /// (non-anonymous JWT subject matching Sponsor.SponsorUserId). Both checks
    /// run inside the handler; the controller just requires `[Authorize]`.
    ///
    /// PATCH semantics: every field is optional; null = leave unchanged. The
    /// domain layer enforces the state-edit matrix per-field — see
    /// <see cref="LankaConnect.Products.LankaEvents.Domain.Sponsor"/> for the cell-by-cell
    /// rules. Image edits flow through the existing POST/DELETE
    /// `/sponsors/{id}/image` endpoints (DELETE authz was extended in H9 above).
    /// </summary>
    [HttpPatch("{sponsorId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(SponsorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSponsor(
        Guid eventId,
        Guid sponsorId,
        [FromBody] UpdateSponsorRequest request)
    {
        var actingUserId = User.TryGetUserId();
        if (actingUserId is null)
        {
            Logger.LogWarning(
                "UpdateSponsor: missing user id on authenticated request — SponsorId={SponsorId}",
                sponsorId);
            return Unauthorized();
        }

        Logger.LogInformation(
            "UpdateSponsor: EventId={EventId}, SponsorId={SponsorId}, ActorUserId={ActorUserId}",
            eventId, sponsorId, actingUserId);

        var command = new UpdateSponsorCommand(
            EventId: eventId,
            SponsorId: sponsorId,
            ActingUserId: actingUserId.Value,
            Name: request.Name,
            Notes: request.Notes,
            Organization: request.Organization,
            Amount: request.Amount,
            Currency: request.Currency,
            ItemName: request.ItemName,
            ItemDescription: request.ItemDescription,
            EstimatedValue: request.EstimatedValue);

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Phase 6A.151 — request body for PATCH /sponsors/{id}. All fields nullable.
    /// </summary>
    public record UpdateSponsorRequest(
        string? Name,
        string? Notes,
        string? Organization,
        decimal? Amount,
        string? Currency,
        string? ItemName,
        string? ItemDescription,
        decimal? EstimatedValue);

    /// <summary>
    /// Phase 6A.151 — staging-blob endpoint for the registration-form inline
    /// sponsor panel image upload. Inline panel submits via the parent
    /// registration command which creates the Stripe Checkout session
    /// server-side, so a Sponsor row doesn't exist pre-Stripe to attach a
    /// blob to. Workflow:
    ///   1. FE picks image → POST here → receives {correlationId, blobName, blobUrl}
    ///   2. FE submits registration with sponsorStagingBlob carried in payload
    ///   3. Registration handler creates Sponsor in-tx with ticket purchase,
    ///      calls Sponsor.SetImage(blobUrl, blobName)
    ///   4. Janitor sweeps unclaimed blobs >6h old (deferred; see TODO)
    ///
    /// Hardening (architect H1):
    ///   - [AllowAnonymous] because registration is anon (Phase 6A.44)
    ///   - 5 MB cap (rejected if exceeded — matches existing image-upload limit)
    ///   - MIME allowlist: jpeg / png / webp only
    ///   - Per-IP rate limit 10/hour via [EnableRateLimiting("sponsor-staging-upload")]
    ///   - SERVER-generated correlation GUID (single-use; never client-chosen).
    ///     This kills the "different user submits with stolen ID" race.
    /// </summary>
    [HttpPost("staging-image")]
    [AllowAnonymous]
    [EnableRateLimiting("sponsor-staging-upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(SponsorStagingImageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> UploadSponsorStagingImage(
        Guid eventId,
        IFormFile image,
        [FromServices] IAzureBlobStorageService blobStorage,
        CancellationToken cancellationToken)
    {
        const long MaxBytes = 5L * 1024 * 1024; // 5 MB
        var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/webp" };

        if (image is null || image.Length == 0)
            return BadRequest(new ProblemDetails { Title = "An image file is required." });

        if (image.Length > MaxBytes)
            return BadRequest(new ProblemDetails
            {
                Title = "Image is too large.",
                Detail = $"Maximum size is {MaxBytes / 1024 / 1024} MB."
            });

        var contentType = image.ContentType?.ToLowerInvariant() ?? string.Empty;
        if (!allowedContentTypes.Contains(contentType))
            return BadRequest(new ProblemDetails
            {
                Title = "Unsupported image type.",
                Detail = $"Allowed types: {string.Join(", ", allowedContentTypes)}."
            });

        try
        {
            // Server-generated correlation GUID. Single-use. The registration
            // handler will look up the blob by this GUID's blobName.
            var correlationId = Guid.NewGuid();
            var ext = Path.GetExtension(image.FileName);
            if (string.IsNullOrEmpty(ext))
                ext = contentType switch
                {
                    "image/jpeg" => ".jpg",
                    "image/png" => ".png",
                    "image/webp" => ".webp",
                    _ => ".bin"
                };
            var fileName = $"sponsors-staging/{correlationId}{ext}";

            await using var stream = image.OpenReadStream();
            var (blobName, blobUrl) = await blobStorage.UploadFileAsync(
                fileName: fileName,
                fileStream: stream,
                contentType: contentType,
                containerName: null, // default container "event-media"
                cancellationToken: cancellationToken);

            Logger.LogInformation(
                "UploadSponsorStagingImage OK: EventId={EventId}, CorrelationId={CorrelationId}, Size={Size}, BlobName={BlobName}",
                eventId, correlationId, image.Length, blobName);

            return Ok(new SponsorStagingImageResult(correlationId, blobName, blobUrl));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "UploadSponsorStagingImage FAILED: EventId={EventId}, Size={Size}",
                eventId, image.Length);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Failed to stage image.",
                Detail = "Please retry shortly."
            });
        }
    }

    /// <summary>
    /// Phase 6A.151 — return shape for POST /sponsors/staging-image.
    /// </summary>
    public record SponsorStagingImageResult(Guid CorrelationId, string BlobName, string BlobUrl);

    /// <summary>
    /// Phase 6A.145 — organizer records an off-platform sponsorship (cash money or
    /// in-kind item collected outside the platform). Bypasses Stripe entirely.
    /// Money sponsors are marked Completed immediately; Item sponsors RecordedItem.
    /// Optional image upload — threshold is bypassed since the organizer is recording
    /// on behalf of the sponsor (architect E-1 organizer override).
    /// Authorization: organizer-only.
    /// </summary>
    [HttpPost("off-platform")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CreateOffPlatformSponsorResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateOffPlatformSponsor(
        Guid eventId,
        [FromForm] CreateOffPlatformSponsorRequest request)
    {
        Logger.LogInformation(
            "CreateOffPlatformSponsor: EventId={EventId}, Type={Type}, Email={Email}, HasImage={HasImage}",
            eventId, request.Type, request.SponsorEmail, request.Image?.Length > 0);

        var authResult = await VerifyOrganizerAsync(eventId);
        if (authResult != null) return authResult;

        byte[]? imageData = null;
        string? imageFileName = null;
        if (request.Image is not null && request.Image.Length > 0)
        {
            using var ms = new MemoryStream();
            await request.Image.CopyToAsync(ms);
            imageData = ms.ToArray();
            imageFileName = request.Image.FileName;
        }

        var command = new CreateOffPlatformSponsorCommand
        {
            EventId = eventId,
            Type = request.Type,
            SponsorName = request.SponsorName,
            SponsorEmail = request.SponsorEmail,
            SponsorPhone = request.SponsorPhone,
            SponsorOrganization = request.SponsorOrganization,
            SponsorNotes = request.SponsorNotes,
            Amount = request.Amount,
            Currency = request.Currency,
            ItemName = request.ItemName,
            ItemDescription = request.ItemDescription,
            EstimatedValue = request.EstimatedValue,
            ImageData = imageData,
            ImageFileName = imageFileName,
        };

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Phase 6A.150 — public, PII-redacted sponsor list for the event detail page.
    /// Returns ONLY confirmed sponsors with images (Money/Completed + Item/RecordedItem),
    /// sorted server-side by contribution magnitude (the magnitudes themselves are
    /// NOT exposed). Used by <c>SponsorsPreviewStrip</c> and <c>SponsorSection</c> on
    /// the anonymous-accessible event detail page. The full-PII organizer-only
    /// variant remains at <see cref="GetEventSponsors"/>.
    /// </summary>
    [HttpGet("public")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PublicEventSponsorsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublicEventSponsors(Guid eventId)
    {
        Logger.LogInformation("GetPublicEventSponsors: EventId={EventId}", eventId);
        var query = new GetPublicEventSponsorsQuery(eventId);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets all sponsors for an event (organizer only).
    /// Authorization: Must be the event organizer to view sponsor PII.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(EventSponsorsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetEventSponsors(Guid eventId)
    {
        Logger.LogInformation("GetEventSponsors: EventId={EventId}", eventId);

        // Authorization: Verify the caller is the event organizer
        var authResult = await VerifyOrganizerAsync(eventId);
        if (authResult != null) return authResult;

        var result = await Mediator.Send(new GetEventSponsorsQuery(eventId));
        return HandleResult(result);
    }

    /// <summary>
    /// Gets sponsor summary for an event (organizer only).
    /// </summary>
    [HttpGet("summary")]
    [Authorize]
    [ProducesResponseType(typeof(SponsorSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSponsorSummary(Guid eventId)
    {
        Logger.LogInformation("GetSponsorSummary: EventId={EventId}", eventId);

        // Authorization: Verify the caller is the event organizer
        var authResult = await VerifyOrganizerAsync(eventId);
        if (authResult != null) return authResult;

        var result = await Mediator.Send(new GetEventSponsorsQuery(eventId));
        if (result.IsSuccess)
        {
            return Ok(result.Value.Summary);
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Exports sponsors for an event in Excel or CSV format (organizer only).
    /// </summary>
    [HttpGet("export")]
    [Authorize]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportSponsors(
        Guid eventId,
        [FromQuery] string format = "excel")
    {
        Logger.LogInformation(
            "ExportSponsors: EventId={EventId}, Format={Format}", eventId, format);

        var authResult = await VerifyOrganizerAsync(eventId);
        if (authResult != null) return authResult;

        var exportFormat = format.ToLowerInvariant() switch
        {
            "csv" => ExportFormat.Csv,
            _ => ExportFormat.Excel
        };

        var result = await Mediator.Send(new ExportSponsorsQuery(eventId, exportFormat));

        if (result.IsSuccess)
        {
            return File(
                result.Value.FileContent,
                result.Value.ContentType,
                result.Value.FileName);
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Gets the authenticated user's own sponsorships for an event.
    /// Returns individual sponsor line items for the logged-in user.
    /// </summary>
    [HttpGet("mine")]
    [Authorize]
    [ProducesResponseType(typeof(List<SponsorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySponsors(Guid eventId)
    {
        var userId = User.GetUserId();

        Logger.LogInformation(
            "GetMySponsors: EventId={EventId}, UserId={UserId}", eventId, userId);

        try
        {
            var sponsors = await _sponsorRepository.GetByUserIdAndEventIdAsync(userId, eventId);

            var sponsorDtos = sponsors.Select(s => new SponsorDto
            {
                Id = s.Id,
                EventId = s.EventId,
                SponsorUserId = s.SponsorUserId,
                SponsorName = s.SponsorName,
                SponsorEmail = s.SponsorEmail,
                SponsorPhone = s.SponsorPhone,
                SponsorOrganization = s.SponsorOrganization,
                SponsorNotes = s.SponsorNotes,
                SponsorType = s.Type.ToString(),
                Amount = s.Amount?.Amount,
                Currency = s.Amount?.Currency.ToString(),
                Status = s.Status.ToString(),
                StripeFeeAmount = s.StripeFeeAmount?.Amount,
                PlatformCommissionAmount = s.PlatformCommissionAmount?.Amount,
                OrganizerPayoutAmount = s.OrganizerPayoutAmount?.Amount,
                ItemName = s.ItemName,
                ItemDescription = s.ItemDescription,
                EstimatedValue = s.EstimatedValue,
                ImageUrl = s.ImageUrl,
                ImageBlobName = s.ImageBlobName,
                CreatedAt = s.CreatedAt,
                PaymentCompletedAt = s.PaymentCompletedAt,
            }).ToList();

            Logger.LogInformation(
                "GetMySponsors: Found {Count} sponsorships for UserId={UserId}, EventId={EventId}",
                sponsorDtos.Count, userId, eventId);

            return Ok(sponsorDtos);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "GetMySponsors FAILED: EventId={EventId}, UserId={UserId}",
                eventId, userId);
            return StatusCode(500, new { Error = "Failed to retrieve your sponsorships" });
        }
    }

    /// <summary>
    /// Verifies that the authenticated user is the organizer of the specified event.
    /// Returns null if authorized, or an IActionResult (Forbid/NotFound) if not.
    /// </summary>
    private async Task<IActionResult?> VerifyOrganizerAsync(Guid eventId)
    {
        var userId = User.GetUserId();

        var eventResult = await Mediator.Send(new GetEventByIdQuery(eventId));
        if (eventResult.IsFailure || eventResult.Value == null)
        {
            Logger.LogWarning(
                "Sponsors authorization failed: Event not found - EventId={EventId}, UserId={UserId}",
                eventId, userId);
            return NotFound(new { Error = "Event not found" });
        }

        if (eventResult.Value!.IsCurrentUserOrganizer != true)
        {
            Logger.LogWarning(
                "User {UserId} attempted to access sponsors for event {EventId} without authorization (organizer: {OrganizerId})",
                userId, eventId, eventResult.Value.OrganizerId);
            return Forbid();
        }

        return null; // Authorized
    }
}

/// <summary>
/// Request body for creating a money-based sponsorship.
/// </summary>
public class CreateMoneySponsorRequest
{
    public required string SponsorName { get; init; }
    public required string SponsorEmail { get; init; }
    public string? SponsorPhone { get; init; }
    public string? SponsorOrganization { get; init; }
    public string? SponsorNotes { get; init; }
    public decimal Amount { get; init; }
    public string? Currency { get; init; }
    public required string SuccessUrl { get; init; }
    public required string CancelUrl { get; init; }
}

/// <summary>
/// Request body for creating an item-based sponsorship.
/// </summary>
public class CreateItemSponsorRequest
{
    public required string SponsorName { get; init; }
    public required string SponsorEmail { get; init; }
    public string? SponsorPhone { get; init; }
    public string? SponsorOrganization { get; init; }
    public string? SponsorNotes { get; init; }
    public required string ItemName { get; init; }
    public string? ItemDescription { get; init; }
    public decimal? EstimatedValue { get; init; }
}

/// <summary>
/// Phase 6A.145 — request body for the organizer-add-off-platform-sponsor endpoint
/// (POST /sponsors/off-platform). Multipart so an optional image file can ride
/// alongside the form fields. Type discriminates between Money + Item branches —
/// Money requires Amount + Currency; Item requires ItemName.
/// </summary>
public class CreateOffPlatformSponsorRequest
{
    public SponsorType Type { get; init; }
    public string SponsorName { get; init; } = string.Empty;
    public string SponsorEmail { get; init; } = string.Empty;
    public string? SponsorPhone { get; init; }
    public string? SponsorOrganization { get; init; }
    public string? SponsorNotes { get; init; }
    // Money branch
    public decimal? Amount { get; init; }
    public Currency? Currency { get; init; }
    // Item branch
    public string? ItemName { get; init; }
    public string? ItemDescription { get; init; }
    public decimal? EstimatedValue { get; init; }
    // Optional image
    public IFormFile? Image { get; init; }
}
