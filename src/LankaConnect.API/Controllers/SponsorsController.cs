using LankaConnect.API.Extensions;
using LankaConnect.Application.Events.Commands.CreateSponsor;
using LankaConnect.Application.Events.Common;
using LankaConnect.Application.Events.Queries.ExportEventAttendees;
using LankaConnect.Application.Events.Queries.ExportSponsors;
using LankaConnect.Application.Events.Queries.GetEventById;
using LankaConnect.Application.Events.Queries.GetEventSponsors;
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
    public SponsorsController(
        IMediator mediator,
        ILogger<SponsorsController> logger)
        : base(mediator, logger)
    {
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
