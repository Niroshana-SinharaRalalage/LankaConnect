using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Communications.Commands.CreateNewsletter;
using LankaConnect.Application.Communications.Commands.UpdateNewsletter;
using LankaConnect.Application.Communications.Commands.DeleteNewsletter;
using LankaConnect.Application.Communications.Commands.PublishNewsletter;
using LankaConnect.Application.Communications.Commands.UnpublishNewsletter;
using LankaConnect.Application.Communications.Commands.SendNewsletter;
using LankaConnect.Application.Communications.Commands.ReactivateNewsletter;
using LankaConnect.Application.Communications.Queries.GetNewsletterById;
using LankaConnect.Application.Communications.Queries.GetNewslettersByCreator;
using LankaConnect.Application.Communications.Queries.GetNewslettersByEvent;
using LankaConnect.Application.Communications.Queries.GetPublishedNewsletters;
using LankaConnect.Application.Communications.Queries.GetRecipientPreview;
using LankaConnect.Application.Communications.Common;
using LankaConnect.API.Extensions;

namespace LankaConnect.API.Controllers;

[Authorize(Roles = "EventOrganizer,Admin,AdminManager")]
public class NewslettersController : BaseController<NewslettersController>
{
    public NewslettersController(IMediator mediator, ILogger<NewslettersController> logger) : base(mediator, logger)
    {
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateNewsletter([FromBody] CreateNewsletterRequest request)
    {
        Logger.LogInformation("[Phase 6A.74] Creating newsletter '{Title}'", request.Title);

        var command = new CreateNewsletterCommand(
            request.Title,
            request.Description,
            request.EmailGroupIds ?? new List<Guid>(),
            request.IncludeNewsletterSubscribers,
            request.EventId,
            request.MetroAreaIds,
            request.TargetAllLocations);

        var result = await Mediator.Send(command);

        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetNewsletterById), new { id = result.Value }, result.Value);
        }

        return HandleResult(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateNewsletter(Guid id, [FromBody] UpdateNewsletterRequest request)
    {
        Logger.LogInformation("[Phase 6A.74] Updating newsletter {Id}", id);

        var command = new UpdateNewsletterCommand(
            id,
            request.Title,
            request.Description,
            request.EmailGroupIds ?? new List<Guid>(),
            request.IncludeNewsletterSubscribers,
            request.EventId,
            request.MetroAreaIds,
            request.TargetAllLocations);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteNewsletter(Guid id)
    {
        Logger.LogInformation("[Phase 6A.74] Deleting newsletter {Id}", id);

        var command = new DeleteNewsletterCommand(id);
        var result = await Mediator.Send(command);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        return HandleResult(result);
    }

    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PublishNewsletter(Guid id)
    {
        Logger.LogInformation("[Phase 6A.74] Publishing newsletter {Id}", id);

        var command = new PublishNewsletterCommand(id);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    [HttpPost("{id:guid}/unpublish")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UnpublishNewsletter(Guid id)
    {
        Logger.LogInformation("[Phase 6A.74 Part 9A] Unpublishing newsletter {Id}", id);

        var command = new UnpublishNewsletterCommand(id);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    [HttpPost("{id:guid}/send")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendNewsletter(Guid id)
    {
        Logger.LogInformation("[Phase 6A.74] Sending newsletter {Id}", id);

        var command = new SendNewsletterCommand(id);
        var result = await Mediator.Send(command);

        if (result.IsSuccess)
        {
            return Accepted();
        }

        return HandleResult(result);
    }

    [HttpPost("{id:guid}/reactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReactivateNewsletter(Guid id)
    {
        Logger.LogInformation("[Phase 6A.74 Hotfix] Reactivating newsletter {Id}", id);

        var command = new ReactivateNewsletterCommand(id);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(NewsletterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNewsletterById(Guid id)
    {
        Logger.LogInformation("[Phase 6A.74] Getting newsletter {Id}", id);

        var query = new GetNewsletterByIdQuery(id);
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    [HttpGet("my-newsletters")]
    [ProducesResponseType(typeof(List<NewsletterDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyNewsletters()
    {
        Logger.LogInformation("[Phase 6A.74] Getting my newsletters");

        var query = new GetNewslettersByCreatorQuery();
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    [HttpGet("event/{eventId:guid}")]
    [ProducesResponseType(typeof(List<NewsletterDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNewslettersByEvent(Guid eventId)
    {
        Logger.LogInformation("[Phase 6A.74] Getting newsletters for event {EventId}", eventId);

        var query = new GetNewslettersByEventQuery(eventId);
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    [HttpGet("{id:guid}/recipient-preview")]
    [ProducesResponseType(typeof(RecipientPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRecipientPreview(Guid id)
    {
        Logger.LogInformation("[Phase 6A.74] Getting recipient preview for newsletter {Id}", id);

        var query = new GetRecipientPreviewQuery(id);
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    [HttpGet("published")]
    [AllowAnonymous] // Public endpoint - anyone can view published newsletters
    [ProducesResponseType(typeof(List<NewsletterDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublishedNewsletters(
        [FromQuery] DateTime? publishedFrom = null,
        [FromQuery] DateTime? publishedTo = null,
        [FromQuery] string? state = null,
        [FromQuery] List<Guid>? metroAreaIds = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] decimal? latitude = null,
        [FromQuery] decimal? longitude = null)
    {
        Logger.LogInformation(
            "[Phase 6A.74 Parts 10/11] Getting published newsletters - SearchTerm: {SearchTerm}, State: {State}, MetroCount: {MetroCount}",
            searchTerm,
            state,
            metroAreaIds?.Count ?? 0);

        var query = new GetPublishedNewslettersQuery(
            PublishedFrom: publishedFrom,
            PublishedTo: publishedTo,
            State: state,
            UserId: userId,
            Latitude: latitude,
            Longitude: longitude,
            MetroAreaIds: metroAreaIds,
            SearchTerm: searchTerm);

        var result = await Mediator.Send(query);

        return HandleResult(result);
    }
}
