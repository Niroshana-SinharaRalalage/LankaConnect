using LankaConnect.API.Extensions;
using LankaConnect.Application.Events.Commands.ClearSponsorImage;
using LankaConnect.Application.Events.Commands.CreateOffPlatformSponsor;
using LankaConnect.Application.Events.Commands.CreateSponsor;
using LankaConnect.Application.Events.Commands.SetSponsorImage;
using LankaConnect.Application.Events.Common;
using LankaConnect.Application.Events.Queries.ExportEventAttendees;
using LankaConnect.Application.Events.Queries.ExportSponsors;
using LankaConnect.Application.Events.Queries.GetEventById;
using LankaConnect.Application.Events.Queries.GetEventSponsors;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LankaConnect.API.Controllers;

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
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
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
    /// Phase 6A.145 — uploads (or replaces) a sponsor's image. Threshold-gated:
    /// sponsors whose contribution meets <c>SponsorConfiguration.MinAmountForSponsorImage</c>
    /// can upload. Organizer override bypasses the threshold check.
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

        // Determine organizer status — used by the handler to bypass the threshold gate.
        // Public callers (anonymous or non-organizer) are subject to the threshold.
        bool isOrganizer = false;
        if (User.Identity?.IsAuthenticated == true)
        {
            var authResult = await VerifyOrganizerAsync(eventId);
            isOrganizer = authResult == null;
        }

        using var ms = new MemoryStream();
        await image.CopyToAsync(ms);

        var command = new SetSponsorImageCommand
        {
            EventId = eventId,
            SponsorId = sponsorId,
            ImageData = ms.ToArray(),
            FileName = image.FileName,
            IsOrganizer = isOrganizer,
        };

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Phase 6A.145 — clears a sponsor's image. Idempotent.
    /// Authorization: organizer-only.
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

        var authResult = await VerifyOrganizerAsync(eventId);
        if (authResult != null) return authResult;

        var result = await Mediator.Send(new ClearSponsorImageCommand
        {
            EventId = eventId,
            SponsorId = sponsorId,
        });
        return result.IsSuccess ? NoContent() : HandleResult(result);
    }

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
        if (eventResult.IsFailure)
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
