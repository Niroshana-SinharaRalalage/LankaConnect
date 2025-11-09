using Microsoft.AspNetCore.Mvc;
using LankaConnect.Application.Communications.Common;

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
    private readonly ILogger<NewsletterController> _logger;

    public NewsletterController(ILogger<NewsletterController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Subscribe to newsletter with location preferences
    /// </summary>
    /// <param name="request">Subscription details including email and location preferences</param>
    /// <returns>Subscription result</returns>
    [HttpPost("subscribe")]
    [ProducesResponseType(typeof(NewsletterSubscriptionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(NewsletterSubscriptionResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Subscribe([FromBody] NewsletterSubscriptionDto request)
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

            // Validate email format
            if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            {
                return BadRequest(new NewsletterSubscriptionResponseDto
                {
                    Success = false,
                    Message = "Invalid email format",
                    ErrorCode = "INVALID_EMAIL"
                });
            }

            // Validate location requirement
            if (!request.ReceiveAllLocations && string.IsNullOrWhiteSpace(request.MetroAreaId))
            {
                return BadRequest(new NewsletterSubscriptionResponseDto
                {
                    Success = false,
                    Message = "Location is required when not receiving all locations",
                    ErrorCode = "LOCATION_REQUIRED"
                });
            }

            // Log subscription (Phase 1 - will be replaced with database save in Phase 2)
            _logger.LogInformation(
                "Newsletter subscription received: Email={Email}, MetroAreaId={MetroAreaId}, ReceiveAllLocations={ReceiveAllLocations}, Timestamp={Timestamp}",
                request.Email,
                request.MetroAreaId ?? "null",
                request.ReceiveAllLocations,
                request.Timestamp
            );

            // TODO: Phase 2 - Save to database
            // var subscriberId = await _newsletterService.SubscribeAsync(request);

            // Return success response
            return Ok(new NewsletterSubscriptionResponseDto
            {
                Success = true,
                Message = "Successfully subscribed to newsletter",
                SubscriberId = $"temp-{DateTime.UtcNow.Ticks}" // Phase 2: Replace with actual DB ID
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
}
