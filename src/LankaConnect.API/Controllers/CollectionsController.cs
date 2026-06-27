using LankaConnect.API.Extensions;
using LankaConnect.Application.Events.Commands.CreateCollection;
using LankaConnect.Application.Events.Common;
using LankaConnect.Application.Events.Queries.ExportCollections;
using LankaConnect.Application.Events.Queries.ExportEventAttendees;
using LankaConnect.Application.Events.Queries.GetEventById;
using LankaConnect.Application.Events.Queries.GetEventCollections;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Collection (Event Fund) Feature: REST endpoints for event fund collections.
/// Route: api/events/{eventId}/collections
/// </summary>
[ApiController]
[Route("api/events/{eventId}/collections")]
[Produces("application/json")]
public class CollectionsController : BaseController<CollectionsController>
{
    private readonly ICollectionRepository _collectionRepository;

    public CollectionsController(
        IMediator mediator,
        ILogger<CollectionsController> logger,
        ICollectionRepository collectionRepository)
        : base(mediator, logger)
    {
        _collectionRepository = collectionRepository;
    }

    /// <summary>
    /// Creates a standalone collection (event fund contribution) for an event.
    /// Returns a Stripe checkout URL for payment.
    /// Anonymous users can contribute without authentication.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCollection(
        Guid eventId,
        [FromBody] CreateCollectionRequest request)
    {
        Logger.LogInformation(
            "CreateCollection: EventId={EventId}, ContributorEmail={ContributorEmail}, Amount={Amount}",
            eventId, request.ContributorEmail, request.Amount);

        var userId = User.Identity?.IsAuthenticated == true
            ? User.TryGetUserId()
            : null;

        var command = new CreateCollectionCommand(
            EventId: eventId,
            ContributorName: request.ContributorName,
            ContributorEmail: request.ContributorEmail,
            ContributorPhone: request.ContributorPhone,
            ContributorNotes: request.ContributorNotes,
            Amount: request.Amount,
            Currency: request.Currency ?? "USD",
            SuccessUrl: request.SuccessUrl,
            CancelUrl: request.CancelUrl,
            UserId: userId);

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets all collections for an event (organizer only).
    /// Authorization: Must be the event organizer to view contributor PII.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(EventCollectionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetEventCollections(Guid eventId)
    {
        Logger.LogInformation("GetEventCollections: EventId={EventId}", eventId);

        // Authorization: Verify the caller is the event organizer
        var authResult = await VerifyOrganizerAsync(eventId);
        if (authResult != null) return authResult;

        var result = await Mediator.Send(new GetEventCollectionsQuery(eventId));
        return HandleResult(result);
    }

    /// <summary>
    /// Gets collection summary for an event (organizer only).
    /// </summary>
    [HttpGet("summary")]
    [Authorize]
    [ProducesResponseType(typeof(CollectionSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCollectionSummary(Guid eventId)
    {
        Logger.LogInformation("GetCollectionSummary: EventId={EventId}", eventId);

        // Authorization: Verify the caller is the event organizer
        var authResult = await VerifyOrganizerAsync(eventId);
        if (authResult != null) return authResult;

        var result = await Mediator.Send(new GetEventCollectionsQuery(eventId));
        if (result.IsSuccess)
        {
            return Ok(result.Value.Summary);
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Exports collections for an event in Excel or CSV format (organizer only).
    /// </summary>
    [HttpGet("export")]
    [Authorize]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportCollections(
        Guid eventId,
        [FromQuery] string format = "excel")
    {
        Logger.LogInformation(
            "ExportCollections: EventId={EventId}, Format={Format}", eventId, format);

        var authResult = await VerifyOrganizerAsync(eventId);
        if (authResult != null) return authResult;

        var exportFormat = format.ToLowerInvariant() switch
        {
            "csv" => ExportFormat.Csv,
            _ => ExportFormat.Excel
        };

        var result = await Mediator.Send(new ExportCollectionsQuery(eventId, exportFormat));

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
    /// Gets public collection summary for an event (anyone can call).
    /// Only returns data if collections are enabled.
    /// Returns: total amount, goal progress, contributor count.
    /// </summary>
    [HttpGet("public-summary")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PublicCollectionSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublicCollectionSummary(Guid eventId)
    {
        Logger.LogInformation("GetPublicCollectionSummary: EventId={EventId}", eventId);

        try
        {
            var eventResult = await Mediator.Send(new GetEventByIdQuery(eventId));
            if (eventResult.IsFailure)
                return NotFound(new { Error = "Event not found" });

            var eventDto = eventResult.Value!;

            if (eventDto.CollectionConfig == null || !eventDto.CollectionConfig.IsEnabled)
            {
                return NotFound(new { Error = "Collections are not available for this event" });
            }

            var collectionsResult = await Mediator.Send(new GetEventCollectionsQuery(eventId));
            if (collectionsResult.IsFailure)
                return NotFound(new { Error = "Could not load collections" });

            var summary = collectionsResult.Value.Summary;

            return Ok(new PublicCollectionSummaryResponse
            {
                TotalAmount = summary.TotalAmount,
                GoalAmount = summary.GoalAmount,
                GoalProgressPercent = summary.GoalProgressPercent,
                CompletedCollections = summary.CompletedCollections,
                ContributorCount = summary.ContributorCount,
                Currency = summary.Currency ?? "USD",
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "GetPublicCollectionSummary FAILED: EventId={EventId}", eventId);
            return StatusCode(500, new { Error = "Failed to retrieve collection summary" });
        }
    }

    /// <summary>
    /// Gets the authenticated user's own collections for an event.
    /// Returns individual contribution line items for the logged-in user.
    /// </summary>
    [HttpGet("mine")]
    [Authorize]
    [ProducesResponseType(typeof(List<CollectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyCollections(Guid eventId)
    {
        var userId = User.GetUserId();

        Logger.LogInformation(
            "GetMyCollections: EventId={EventId}, UserId={UserId}", eventId, userId);

        try
        {
            var collections = await _collectionRepository.GetByUserIdAndEventIdAsync(userId, eventId);

            var collectionDtos = collections.Select(c => new CollectionDto
            {
                Id = c.Id,
                EventId = c.EventId,
                ContributorUserId = c.ContributorUserId,
                ContributorName = c.ContributorName,
                ContributorEmail = c.ContributorEmail,
                ContributorPhone = c.ContributorPhone,
                ContributorNotes = c.ContributorNotes,
                Amount = c.Amount.Amount,
                Currency = c.Amount.Currency.ToString(),
                Status = c.Status.ToString(),
                StripeFeeAmount = c.StripeFeeAmount?.Amount,
                PlatformCommissionAmount = c.PlatformCommissionAmount?.Amount,
                OrganizerPayoutAmount = c.OrganizerPayoutAmount?.Amount,
                CreatedAt = c.CreatedAt,
                PaymentCompletedAt = c.PaymentCompletedAt,
            }).ToList();

            Logger.LogInformation(
                "GetMyCollections: Found {Count} contributions for UserId={UserId}, EventId={EventId}",
                collectionDtos.Count, userId, eventId);

            return Ok(collectionDtos);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "GetMyCollections FAILED: EventId={EventId}, UserId={UserId}",
                eventId, userId);
            return StatusCode(500, new { Error = "Failed to retrieve your contributions" });
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
                "Collections authorization failed: Event not found - EventId={EventId}, UserId={UserId}",
                eventId, userId);
            return NotFound(new { Error = "Event not found" });
        }

        if (eventResult.Value!.IsCurrentUserOrganizer != true)
        {
            Logger.LogWarning(
                "User {UserId} attempted to access collections for event {EventId} without authorization (organizer: {OrganizerId})",
                userId, eventId, eventResult.Value.OrganizerId);
            return Forbid();
        }

        return null; // Authorized
    }
}

/// <summary>
/// Public-facing collection summary response (no PII, no individual contributor details).
/// </summary>
public class PublicCollectionSummaryResponse
{
    public decimal TotalAmount { get; init; }
    public decimal? GoalAmount { get; init; }
    public decimal? GoalProgressPercent { get; init; }
    public int CompletedCollections { get; init; }
    public int ContributorCount { get; init; }
    public string Currency { get; init; } = "USD";
}

/// <summary>
/// Request body for creating a collection (event fund contribution).
/// </summary>
public class CreateCollectionRequest
{
    public required string ContributorName { get; init; }
    public required string ContributorEmail { get; init; }
    public string? ContributorPhone { get; init; }
    public string? ContributorNotes { get; init; }
    public decimal Amount { get; init; }
    public string? Currency { get; init; }
    public required string SuccessUrl { get; init; }
    public required string CancelUrl { get; init; }
}
