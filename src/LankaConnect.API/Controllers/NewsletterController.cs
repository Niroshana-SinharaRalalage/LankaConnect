using MediatR;
using Microsoft.AspNetCore.Mvc;
using LankaConnect.Application.Communications.Common;
using LankaConnect.Application.Communications.Commands.SubscribeToNewsletter;
using LankaConnect.Application.Communications.Commands.ConfirmNewsletterSubscription;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Newsletter subscription endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class NewsletterController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<NewsletterController> _logger;

    public NewsletterController(IMediator mediator, ILogger<NewsletterController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Subscribe to newsletter with location preferences
    /// </summary>
    /// <param name="request">Subscription details including email and location preferences</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Subscription result</returns>
    [HttpPost("subscribe")]
    [ProducesResponseType(typeof(NewsletterSubscriptionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(NewsletterSubscriptionResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Subscribe(
        [FromBody] NewsletterSubscriptionDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate model state
            if (!ModelState.IsValid)
            {
                return BadRequest(new NewsletterSubscriptionResponseDto
                {
                    Success = false,
                    Message = "Invalid request data",
                    ErrorCode = "INVALID_REQUEST"
                });
            }

            // Parse MetroAreaIds if provided
            List<Guid>? metroAreaIds = null;
            if (request.MetroAreaIds != null && request.MetroAreaIds.Count > 0)
            {
                metroAreaIds = new List<Guid>();
                foreach (var metroIdString in request.MetroAreaIds)
                {
                    if (Guid.TryParse(metroIdString, out var parsedId))
                    {
                        metroAreaIds.Add(parsedId);
                    }
                    else
                    {
                        return BadRequest(new NewsletterSubscriptionResponseDto
                        {
                            Success = false,
                            Message = $"Invalid metro area ID format: {metroIdString}",
                            ErrorCode = "INVALID_METRO_AREA_ID"
                        });
                    }
                }
            }

            // Create command
            var command = new SubscribeToNewsletterCommand(
                request.Email,
                metroAreaIds,
                request.ReceiveAllLocations);

            // Execute command
            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(new NewsletterSubscriptionResponseDto
                {
                    Success = false,
                    Message = result.Error,
                    ErrorCode = "SUBSCRIPTION_FAILED"
                });
            }

            // Return success response
            return Ok(new NewsletterSubscriptionResponseDto
            {
                Success = true,
                Message = "Successfully subscribed to newsletter. Please check your email to confirm.",
                SubscriberId = result.Value.Id.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing newsletter subscription for email: {Email}", request.Email);
            return StatusCode(500, new NewsletterSubscriptionResponseDto
            {
                Success = false,
                Message = "Internal server error",
                ErrorCode = "SERVER_ERROR"
            });
        }
    }

    /// <summary>
    /// Confirm newsletter subscription with token from email
    /// </summary>
    /// <param name="token">Confirmation token from email</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Confirmation result</returns>
    [HttpGet("confirm")]
    [ProducesResponseType(typeof(NewsletterSubscriptionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(NewsletterSubscriptionResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Confirm(
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new NewsletterSubscriptionResponseDto
                {
                    Success = false,
                    Message = "Confirmation token is required",
                    ErrorCode = "INVALID_TOKEN"
                });
            }

            // Create command
            var command = new ConfirmNewsletterSubscriptionCommand(token);

            // Execute command
            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(new NewsletterSubscriptionResponseDto
                {
                    Success = false,
                    Message = result.Error,
                    ErrorCode = "CONFIRMATION_FAILED"
                });
            }

            // Return success response
            return Ok(new NewsletterSubscriptionResponseDto
            {
                Success = true,
                Message = "Newsletter subscription confirmed successfully!",
                SubscriberId = result.Value.Id.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming newsletter subscription with token: {Token}", token);
            return StatusCode(500, new NewsletterSubscriptionResponseDto
            {
                Success = false,
                Message = "Internal server error",
                ErrorCode = "SERVER_ERROR"
            });
        }
    }
}
