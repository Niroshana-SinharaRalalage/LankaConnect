using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using LankaConnect.Domain.Payments;
using LankaConnect.Infrastructure.Payments.Configuration;
using LankaConnect.Domain.Users;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Payments controller for Stripe integration
/// Phase 6A.4: Stripe Payment Integration - MVP
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IStripeClient _stripeClient;
    private readonly IStripeCustomerRepository _customerRepository;
    private readonly IStripeWebhookEventRepository _webhookEventRepository;
    private readonly IUserRepository _userRepository;
    private readonly StripeOptions _stripeOptions;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IStripeClient stripeClient,
        IStripeCustomerRepository customerRepository,
        IStripeWebhookEventRepository webhookEventRepository,
        IUserRepository userRepository,
        IOptions<StripeOptions> stripeOptions,
        ILogger<PaymentsController> logger)
    {
        _stripeClient = stripeClient;
        _customerRepository = customerRepository;
        _webhookEventRepository = webhookEventRepository;
        _userRepository = userRepository;
        _stripeOptions = stripeOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Creates a Stripe Checkout session for subscription upgrade
    /// </summary>
    /// <param name="request">Checkout session request</param>
    /// <returns>Checkout session URL</returns>
    [HttpPost("create-checkout-session")]
    [ProducesResponseType(typeof(CreateCheckoutSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutSessionRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("Unable to extract user ID from claims");
                return Unauthorized(new { Error = "User ID not found in token" });
            }

            _logger.LogInformation("Creating checkout session for user {UserId}", userId);

            // Get or create Stripe customer
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return NotFound(new { Error = "User not found" });
            }

            var stripeCustomerId = await _customerRepository.GetStripeCustomerIdByUserIdAsync(userId);

            if (string.IsNullOrEmpty(stripeCustomerId))
            {
                // Create new Stripe customer
                var customerService = new CustomerService(_stripeClient);
                var customerOptions = new CustomerCreateOptions
                {
                    Email = user.Email.Value,
                    Name = user.FullName,
                    Metadata = new Dictionary<string, string>
                    {
                        ["user_id"] = userId.ToString()
                    }
                };

                var customer = await customerService.CreateAsync(customerOptions);
                stripeCustomerId = customer.Id;

                // Save to database
                await _customerRepository.SaveStripeCustomerAsync(
                    userId,
                    customer.Id,
                    user.Email.Value,
                    user.FullName,
                    customer.Created);

                _logger.LogInformation("Created Stripe customer {CustomerId} for user {UserId}", customer.Id, userId);
            }

            // Create checkout session
            var sessionService = new SessionService(_stripeClient);
            var sessionOptions = new SessionCreateOptions
            {
                Customer = stripeCustomerId,
                PaymentMethodTypes = new List<string> { "card" },
                Mode = "subscription",
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = request.PriceId,
                        Quantity = 1
                    }
                },
                SuccessUrl = request.SuccessUrl,
                CancelUrl = request.CancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    ["user_id"] = userId.ToString()
                }
            };

            var session = await sessionService.CreateAsync(sessionOptions);

            _logger.LogInformation("Created checkout session {SessionId} for user {UserId}", session.Id, userId);

            return Ok(new CreateCheckoutSessionResponse
            {
                SessionId = session.Id,
                SessionUrl = session.Url
            });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating checkout session");
            return BadRequest(new { Error = "Payment processing error", Details = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating checkout session");
            return StatusCode(500, new { Error = "Internal server error" });
        }
    }

    /// <summary>
    /// Creates a Stripe Customer Portal session for subscription management
    /// </summary>
    /// <param name="request">Portal session request</param>
    /// <returns>Portal session URL</returns>
    [HttpPost("create-portal-session")]
    [ProducesResponseType(typeof(CreatePortalSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreatePortalSession([FromBody] CreatePortalSessionRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("Unable to extract user ID from claims");
                return Unauthorized(new { Error = "User ID not found in token" });
            }

            _logger.LogInformation("Creating portal session for user {UserId}", userId);

            var stripeCustomerId = await _customerRepository.GetStripeCustomerIdByUserIdAsync(userId);
            if (string.IsNullOrEmpty(stripeCustomerId))
            {
                _logger.LogWarning("User {UserId} does not have a Stripe customer", userId);
                return BadRequest(new { Error = "No subscription found" });
            }

            // Create portal session
            var sessionService = new Stripe.BillingPortal.SessionService(_stripeClient);
            var sessionOptions = new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = stripeCustomerId,
                ReturnUrl = request.ReturnUrl
            };

            var session = await sessionService.CreateAsync(sessionOptions);

            _logger.LogInformation("Created portal session {SessionId} for user {UserId}", session.Id, userId);

            return Ok(new CreatePortalSessionResponse
            {
                SessionUrl = session.Url
            });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating portal session");
            return BadRequest(new { Error = "Payment processing error", Details = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating portal session");
            return StatusCode(500, new { Error = "Internal server error" });
        }
    }

    /// <summary>
    /// Webhook endpoint for Stripe events
    /// </summary>
    /// <returns>200 OK if webhook processed successfully</returns>
    [HttpPost("webhook")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signatureHeader = Request.Headers["Stripe-Signature"].ToString();

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                signatureHeader,
                _stripeOptions.WebhookSecret
            );

            _logger.LogInformation("Processing webhook event {EventId} of type {EventType}", stripeEvent.Id, stripeEvent.Type);

            // Check idempotency
            if (await _webhookEventRepository.IsEventProcessedAsync(stripeEvent.Id))
            {
                _logger.LogInformation("Event {EventId} already processed, skipping", stripeEvent.Id);
                return Ok();
            }

            // Record event
            await _webhookEventRepository.RecordEventAsync(stripeEvent.Id, stripeEvent.Type);

            // Process event based on type
            // For MVP, we'll just log the events and mark them as processed
            // Full implementation will be in Phase 2
            _logger.LogInformation("Webhook event {EventId} type {EventType} received", stripeEvent.Id, stripeEvent.Type);

            // Mark as processed
            await _webhookEventRepository.MarkEventAsProcessedAsync(stripeEvent.Id);

            return Ok();
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook signature verification failed");
            return BadRequest(new { Error = "Invalid signature" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            return StatusCode(500);
        }
    }

    /// <summary>
    /// Gets the Stripe publishable key for client-side integration
    /// </summary>
    /// <returns>Publishable key</returns>
    [HttpGet("config")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(StripeConfigResponse), StatusCodes.Status200OK)]
    public IActionResult GetConfig()
    {
        return Ok(new StripeConfigResponse
        {
            PublishableKey = _stripeOptions.PublishableKey
        });
    }
}

/// <summary>
/// Request to create a checkout session
/// </summary>
public class CreateCheckoutSessionRequest
{
    public required string PriceId { get; init; }
    public required string SuccessUrl { get; init; }
    public required string CancelUrl { get; init; }
}

/// <summary>
/// Response with checkout session details
/// </summary>
public class CreateCheckoutSessionResponse
{
    public required string SessionId { get; init; }
    public required string SessionUrl { get; init; }
}

/// <summary>
/// Request to create a portal session
/// </summary>
public class CreatePortalSessionRequest
{
    public required string ReturnUrl { get; init; }
}

/// <summary>
/// Response with portal session details
/// </summary>
public class CreatePortalSessionResponse
{
    public required string SessionUrl { get; init; }
}

/// <summary>
/// Response with Stripe configuration
/// </summary>
public class StripeConfigResponse
{
    public required string PublishableKey { get; init; }
}
