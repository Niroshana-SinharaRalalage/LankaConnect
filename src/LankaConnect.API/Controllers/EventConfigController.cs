using LankaConnect.API.Extensions;
using LankaConnect.Application.Events.Commands.ClearSponsorConfigImage;
using LankaConnect.Application.Events.Commands.SetSponsorConfigImage;
using LankaConnect.Application.Events.Commands.UpdateCollectionConfig;
using LankaConnect.Application.Events.Commands.UpdateSponsorConfig;
using LankaConnect.Application.Events.Commands.UpdateAddOnConfig;
using LankaConnect.Application.Events.Queries.GetEventById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Phase 4: REST endpoints for event financial configuration (collection, sponsor, add-on).
/// Route: api/events/{eventId}
/// Separated from EventsController to keep controller sizes manageable.
/// </summary>
[ApiController]
[Route("api/events/{eventId}")]
[Produces("application/json")]
public class EventConfigController : BaseController<EventConfigController>
{
    public EventConfigController(
        IMediator mediator,
        ILogger<EventConfigController> logger)
        : base(mediator, logger)
    {
    }

    /// <summary>
    /// Updates the collection (event fund) configuration for an event.
    /// Authorization: Must be the event organizer.
    /// </summary>
    [HttpPut("collection-config")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCollectionConfig(
        Guid eventId,
        [FromBody] UpdateCollectionConfigRequest request)
    {
        Logger.LogInformation(
            "UpdateCollectionConfig: EventId={EventId}, IsEnabled={IsEnabled}",
            eventId, request.IsEnabled);

        var authResult = await VerifyOrganizerAsync(eventId);
        if (authResult != null) return authResult;

        var command = new UpdateCollectionConfigCommand(
            EventId: eventId,
            IsEnabled: request.IsEnabled,
            GoalAmount: request.GoalAmount,
            ShowProgress: request.ShowProgress,
            SuggestedAmounts: request.SuggestedAmounts,
            AllowCustomAmount: request.AllowCustomAmount,
            MinAmount: request.MinAmount,
            MaxAmount: request.MaxAmount,
            CollectionMessage: request.CollectionMessage,
            ShowContributorCount: request.ShowContributorCount);

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Updates the sponsor configuration for an event.
    /// Authorization: Must be the event organizer.
    /// </summary>
    [HttpPut("sponsor-config")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSponsorConfig(
        Guid eventId,
        [FromBody] UpdateSponsorConfigRequest request)
    {
        Logger.LogInformation(
            "UpdateSponsorConfig: EventId={EventId}, IsEnabled={IsEnabled}",
            eventId, request.IsEnabled);

        var authResult = await VerifyOrganizerAsync(eventId);
        if (authResult != null) return authResult;

        var command = new UpdateSponsorConfigCommand(
            EventId: eventId,
            IsEnabled: request.IsEnabled,
            AcceptMoneySponsors: request.AcceptMoneySponsors,
            AcceptItemSponsors: request.AcceptItemSponsors,
            MinSponsorAmount: request.MinSponsorAmount,
            SponsorMessage: request.SponsorMessage,
            ShowSponsorList: request.ShowSponsorList);

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Phase 6A.143 — uploads (or replaces) the sponsor banner image for an event.
    /// Requires sponsor config to exist and be enabled. Authorization: organizer only.
    /// </summary>
    [HttpPost("sponsor-config/image")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(SetSponsorConfigImageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetSponsorConfigImage(
        Guid eventId,
        IFormFile image)
    {
        Logger.LogInformation(
            "SetSponsorConfigImage: EventId={EventId}, FileName={FileName}, Size={Size}",
            eventId, image?.FileName, image?.Length);

        var authResult = await VerifyOrganizerAsync(eventId);
        if (authResult != null) return authResult;

        if (image is null || image.Length == 0)
            return BadRequest(new ProblemDetails { Title = "An image file is required." });

        using var ms = new MemoryStream();
        await image.CopyToAsync(ms);

        var command = new SetSponsorConfigImageCommand
        {
            EventId = eventId,
            ImageData = ms.ToArray(),
            FileName = image.FileName
        };

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Phase 6A.143 — clears the sponsor banner image. Idempotent.
    /// Authorization: organizer only.
    /// </summary>
    [HttpDelete("sponsor-config/image")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ClearSponsorConfigImage(Guid eventId)
    {
        Logger.LogInformation("ClearSponsorConfigImage: EventId={EventId}", eventId);

        var authResult = await VerifyOrganizerAsync(eventId);
        if (authResult != null) return authResult;

        var result = await Mediator.Send(new ClearSponsorConfigImageCommand { EventId = eventId });
        return result.IsSuccess ? NoContent() : HandleResult(result);
    }

    /// <summary>
    /// Updates the add-on configuration for an event.
    /// Authorization: Must be the event organizer.
    /// </summary>
    [HttpPut("add-on-config")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAddOnConfig(
        Guid eventId,
        [FromBody] UpdateAddOnConfigRequest request)
    {
        Logger.LogInformation(
            "UpdateAddOnConfig: EventId={EventId}, IsEnabled={IsEnabled}",
            eventId, request.IsEnabled);

        var authResult = await VerifyOrganizerAsync(eventId);
        if (authResult != null) return authResult;

        var command = new UpdateAddOnConfigCommand(
            EventId: eventId,
            IsEnabled: request.IsEnabled,
            AvailableDuringRegistration: request.AvailableDuringRegistration,
            AvailableStandalone: request.AvailableStandalone,
            AddOnMessage: request.AddOnMessage);

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Verifies that the authenticated user is the organizer of the specified event.
    /// Returns null if authorized, or an IActionResult (Forbid/NotFound) if not.
    /// Same pattern as DonationsController.VerifyOrganizerAsync.
    /// </summary>
    private async Task<IActionResult?> VerifyOrganizerAsync(Guid eventId)
    {
        var userId = User.GetUserId();

        var eventResult = await Mediator.Send(new GetEventByIdQuery(eventId));
        if (eventResult.IsFailure)
        {
            Logger.LogWarning(
                "EventConfig authorization failed: Event not found - EventId={EventId}, UserId={UserId}",
                eventId, userId);
            return NotFound(new { Error = "Event not found" });
        }

        if (eventResult.Value!.IsCurrentUserOrganizer != true)
        {
            Logger.LogWarning(
                "User {UserId} attempted to update config for event {EventId} without authorization (organizer: {OrganizerId})",
                userId, eventId, eventResult.Value.OrganizerId);
            return Forbid();
        }

        return null; // Authorized
    }
}

/// <summary>
/// Request body for updating collection (event fund) configuration.
/// </summary>
public class UpdateCollectionConfigRequest
{
    public bool IsEnabled { get; init; }
    public decimal? GoalAmount { get; init; }
    public bool ShowProgress { get; init; }
    public List<decimal>? SuggestedAmounts { get; init; }
    public bool AllowCustomAmount { get; init; }
    public decimal? MinAmount { get; init; }
    public decimal? MaxAmount { get; init; }
    public string? CollectionMessage { get; init; }
    public bool ShowContributorCount { get; init; }
}

/// <summary>
/// Request body for updating sponsor configuration.
/// </summary>
public class UpdateSponsorConfigRequest
{
    public bool IsEnabled { get; init; }
    public bool AcceptMoneySponsors { get; init; }
    public bool AcceptItemSponsors { get; init; }
    public decimal? MinSponsorAmount { get; init; }
    public string? SponsorMessage { get; init; }
    public bool ShowSponsorList { get; init; }
}

/// <summary>
/// Request body for updating add-on configuration.
/// </summary>
public class UpdateAddOnConfigRequest
{
    public bool IsEnabled { get; init; }
    public bool AvailableDuringRegistration { get; init; }
    public bool AvailableStandalone { get; init; }
    public string? AddOnMessage { get; init; }
}
