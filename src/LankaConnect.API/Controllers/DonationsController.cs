using LankaConnect.API.Extensions;
using LankaConnect.Products.LankaEvents.Application.Commands.CreateDonation;
using LankaConnect.Application.Events.Common;
using LankaConnect.Products.LankaEvents.Application.Queries.ExportDonations;
using LankaConnect.Products.LankaEvents.Application.Queries.ExportEventAttendees;
using LankaConnect.Products.LankaEvents.Application.Queries.GetEventById;
using LankaConnect.Products.LankaEvents.Application.Queries.GetEventDonations;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Donation Feature: REST endpoints for event donations.
/// Route: api/events/{eventId}/donations
/// </summary>
[ApiController]
[Route("api/events/{eventId}/donations")]
[Produces("application/json")]
public class DonationsController : BaseController<DonationsController>
{
    private readonly IDonationRepository _donationRepository;

    public DonationsController(
        IMediator mediator,
        ILogger<DonationsController> logger,
        IDonationRepository donationRepository)
        : base(mediator, logger)
    {
        _donationRepository = donationRepository;
    }

    /// <summary>
    /// Creates a standalone donation for an event.
    /// Returns a Stripe checkout URL for payment.
    /// Anonymous users can donate without authentication.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDonation(
        Guid eventId,
        [FromBody] CreateDonationRequest request)
    {
        Logger.LogInformation(
            "CreateDonation: EventId={EventId}, DonorEmail={DonorEmail}, Amount={Amount}",
            eventId, request.DonorEmail, request.Amount);

        var userId = User.Identity?.IsAuthenticated == true
            ? User.TryGetUserId()
            : null;

        var command = new CreateDonationCommand(
            EventId: eventId,
            DonorName: request.DonorName,
            DonorEmail: request.DonorEmail,
            DonorPhone: request.DonorPhone,
            DonorNotes: request.DonorNotes,
            Amount: request.Amount,
            Currency: request.Currency ?? "USD",
            SuccessUrl: request.SuccessUrl,
            CancelUrl: request.CancelUrl,
            UserId: userId);

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets all donations for an event (organizer only).
    /// Authorization: Must be the event organizer to view donor PII.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(EventDonationsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetEventDonations(Guid eventId)
    {
        Logger.LogInformation("GetEventDonations: EventId={EventId}", eventId);

        // Authorization: Verify the caller is the event organizer
        var authResult = await VerifyOrganizerAsync(eventId);
        if (authResult != null) return authResult;

        var result = await Mediator.Send(new GetEventDonationsQuery(eventId));
        return HandleResult(result);
    }

    /// <summary>
    /// Gets donation summary for an event (organizer only).
    /// </summary>
    [HttpGet("summary")]
    [Authorize]
    [ProducesResponseType(typeof(DonationSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDonationSummary(Guid eventId)
    {
        Logger.LogInformation("GetDonationSummary: EventId={EventId}", eventId);

        // Authorization: Verify the caller is the event organizer
        var authResult = await VerifyOrganizerAsync(eventId);
        if (authResult != null) return authResult;

        var result = await Mediator.Send(new GetEventDonationsQuery(eventId));
        if (result.IsSuccess)
        {
            return Ok(result.Value.Summary);
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Exports donations for an event in Excel or CSV format (organizer only).
    /// </summary>
    [HttpGet("export")]
    [Authorize]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportDonations(
        Guid eventId,
        [FromQuery] string format = "excel")
    {
        Logger.LogInformation(
            "ExportDonations: EventId={EventId}, Format={Format}", eventId, format);

        // Authorization: Verify the caller is the event organizer
        var authResult = await VerifyOrganizerAsync(eventId);
        if (authResult != null) return authResult;

        var exportFormat = format.ToLowerInvariant() switch
        {
            "csv" => ExportFormat.Csv,
            _ => ExportFormat.Excel
        };

        var result = await Mediator.Send(new ExportDonationsQuery(eventId, exportFormat));

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
    /// Gets public donation summary for an event (anyone can call).
    /// Only returns data if the organizer has enabled ShowDonationSummary.
    /// Returns: completed donation count + net raised amount.
    /// </summary>
    [HttpGet("public-summary")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PublicDonationSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublicDonationSummary(Guid eventId)
    {
        Logger.LogInformation("GetPublicDonationSummary: EventId={EventId}", eventId);

        // Load event to check ShowDonationSummary toggle
        var eventResult = await Mediator.Send(new GetEventByIdQuery(eventId));
        if (eventResult.IsFailure)
            return NotFound(new { Error = "Event not found" });

        var eventDto = eventResult.Value!;

        // Only return summary if organizer explicitly enabled it
        if (eventDto.DonationConfig == null ||
            !eventDto.DonationConfig.IsEnabled ||
            !eventDto.DonationConfig.ShowDonationSummary)
        {
            return NotFound(new { Error = "Donation summary is not available for this event" });
        }

        // Get donations and compute public summary
        var donationsResult = await Mediator.Send(new GetEventDonationsQuery(eventId));
        if (donationsResult.IsFailure)
            return NotFound(new { Error = "Could not load donations" });

        var summary = donationsResult.Value.Summary;
        var netRaised = summary.TotalAmount - summary.TotalStripeFees - summary.TotalPlatformCommission;

        return Ok(new PublicDonationSummaryResponse
        {
            CompletedDonations = summary.CompletedDonations,
            NetRaisedAmount = netRaised,
            Currency = summary.Currency ?? "USD"
        });
    }

    /// <summary>
    /// Gets the authenticated user's own donations for an event.
    /// Returns individual donation line items for the logged-in user.
    /// </summary>
    [HttpGet("mine")]
    [Authorize]
    [ProducesResponseType(typeof(List<DonationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyDonations(Guid eventId)
    {
        var userId = User.GetUserId();

        Logger.LogInformation(
            "GetMyDonations: EventId={EventId}, UserId={UserId}", eventId, userId);

        var donations = await _donationRepository.GetByUserIdAndEventIdAsync(userId, eventId);

        var donationDtos = donations.Select(d => new DonationDto
        {
            Id = d.Id,
            EventId = d.EventId,
            RegistrationId = d.RegistrationId,
            DonorUserId = d.DonorUserId,
            DonorName = d.DonorName,
            DonorEmail = d.DonorEmail,
            DonorPhone = d.DonorPhone,
            DonorNotes = d.DonorNotes,
            Amount = d.Amount.Amount,
            Currency = d.Amount.Currency.ToString(),
            Status = d.Status.ToString(),
            IsBundled = d.IsBundled,
            StripeFeeAmount = d.StripeFeeAmount?.Amount,
            PlatformCommissionAmount = d.PlatformCommissionAmount?.Amount,
            OrganizerPayoutAmount = d.OrganizerPayoutAmount?.Amount,
            CreatedAt = d.CreatedAt,
            PaymentCompletedAt = d.PaymentCompletedAt,
        }).ToList();

        return Ok(donationDtos);
    }

    /// <summary>
    /// Verifies that the authenticated user is the organizer of the specified event.
    /// Returns null if authorized, or an IActionResult (Forbid/NotFound) if not.
    /// Follows the same pattern as EventsController.GetEventAttendees.
    /// </summary>
    private async Task<IActionResult?> VerifyOrganizerAsync(Guid eventId)
    {
        var userId = User.GetUserId();

        var eventResult = await Mediator.Send(new GetEventByIdQuery(eventId));
        if (eventResult.IsFailure)
        {
            Logger.LogWarning(
                "Donations authorization failed: Event not found - EventId={EventId}, UserId={UserId}",
                eventId, userId);
            return NotFound(new { Error = "Event not found" });
        }

        if (eventResult.Value!.IsCurrentUserOrganizer != true)
        {
            Logger.LogWarning(
                "User {UserId} attempted to access donations for event {EventId} without authorization (organizer: {OrganizerId})",
                userId, eventId, eventResult.Value.OrganizerId);
            return Forbid();
        }

        return null; // Authorized
    }
}

/// <summary>
/// Request body for creating a donation.
/// </summary>
public class CreateDonationRequest
{
    public required string DonorName { get; init; }
    public required string DonorEmail { get; init; }
    public string? DonorPhone { get; init; }
    public string? DonorNotes { get; init; }
    public decimal Amount { get; init; }
    public string? Currency { get; init; }
    public required string SuccessUrl { get; init; }
    public required string CancelUrl { get; init; }
}

/// <summary>
/// Public-facing donation summary response (no PII, no individual donor details).
/// Only returned when organizer has enabled ShowDonationSummary.
/// </summary>
public class PublicDonationSummaryResponse
{
    public int CompletedDonations { get; init; }
    public decimal NetRaisedAmount { get; init; }
    public string Currency { get; init; } = "USD";
}
