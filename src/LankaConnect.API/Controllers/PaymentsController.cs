using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using LankaConnect.Domain.Payments;
using LankaConnect.Infrastructure.Payments.Configuration;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Common;
using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Payments controller for Stripe integration
/// Phase 6A.4: Stripe Payment Integration - MVP
/// Session 23 (Phase 2B): Extended for event ticket payment webhooks
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
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly StripeOptions _stripeOptions;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IStripeClient stripeClient,
        IStripeCustomerRepository customerRepository,
        IStripeWebhookEventRepository webhookEventRepository,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        IUnitOfWork unitOfWork,
        IOptions<StripeOptions> stripeOptions,
        ILogger<PaymentsController> logger)
    {
        _stripeClient = stripeClient;
        _customerRepository = customerRepository;
        _webhookEventRepository = webhookEventRepository;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _unitOfWork = unitOfWork;
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
        // CRITICAL: Log that we've reached the webhook endpoint
        _logger.LogInformation("Webhook endpoint reached - Method: {Method}, Path: {Path}, ContentType: {ContentType}, ContentLength: {ContentLength}",
            HttpContext.Request.Method,
            HttpContext.Request.Path,
            HttpContext.Request.ContentType,
            HttpContext.Request.ContentLength);

        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signatureHeader = Request.Headers["Stripe-Signature"].ToString();

        _logger.LogInformation("Webhook body received - Length: {Length}, HasSignature: {HasSignature}",
            json?.Length ?? 0,
            !string.IsNullOrEmpty(signatureHeader));

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                signatureHeader,
                _stripeOptions.WebhookSecret,
                throwOnApiVersionMismatch: false
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

            // Session 23 (Phase 2B): Process event based on type
            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    await HandleCheckoutSessionCompletedAsync(stripeEvent);
                    break;

                // Future: Add more event types as needed
                // case "payment_intent.payment_failed":
                //     await HandlePaymentFailedAsync(stripeEvent);
                //     break;

                default:
                    _logger.LogInformation("Unhandled webhook event type {EventType}, skipping", stripeEvent.Type);
                    break;
            }

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
            _logger.LogError(ex, "Error processing webhook - Type: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}",
                ex.GetType().FullName, ex.Message, ex.StackTrace);
            return StatusCode(500);
        }
    }

    /// <summary>
    /// Session 23 (Phase 2B): Handles checkout.session.completed webhook for event ticket payments
    /// </summary>
    private async Task HandleCheckoutSessionCompletedAsync(Stripe.Event stripeEvent)
    {
        // Phase 6A.52: Generate correlation ID for end-to-end tracing
        var correlationId = Guid.NewGuid();

        try
        {
            var session = stripeEvent.Data.Object as Session;
            if (session == null)
            {
                _logger.LogWarning(
                    "[Phase 6A.52] [Webhook-ERROR] Checkout session data is null - CorrelationId: {CorrelationId}, EventId: {EventId}",
                    correlationId, stripeEvent.Id);
                return;
            }

            _logger.LogInformation(
                "[Phase 6A.52] [Webhook-1] Processing checkout.session.completed - CorrelationId: {CorrelationId}, SessionId: {SessionId}, PaymentStatus: {PaymentStatus}, StripeEventId: {StripeEventId}",
                correlationId, session.Id, session.PaymentStatus, stripeEvent.Id);

            // Only process successful payments
            if (session.PaymentStatus != "paid")
            {
                _logger.LogWarning(
                    "[Phase 6A.52] [Webhook-WARN] Payment not completed - CorrelationId: {CorrelationId}, SessionId: {SessionId}, Status: {Status}",
                    correlationId, session.Id, session.PaymentStatus);
                return;
            }

            // Extract metadata
            if (!session.Metadata.TryGetValue("registration_id", out var registrationIdStr) ||
                !Guid.TryParse(registrationIdStr, out var registrationId))
            {
                _logger.LogWarning(
                    "[Phase 6A.52] [Webhook-ERROR] Missing registration_id - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                    correlationId, session.Id);
                return;
            }

            if (!session.Metadata.TryGetValue("event_id", out var eventIdStr) ||
                !Guid.TryParse(eventIdStr, out var eventId))
            {
                _logger.LogWarning(
                    "[Phase 6A.52] [Webhook-ERROR] Missing event_id - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                    correlationId, session.Id);
                return;
            }

            _logger.LogInformation(
                "[Phase 6A.52] [Webhook-2] Metadata extracted - CorrelationId: {CorrelationId}, EventId: {EventId}, RegistrationId: {RegistrationId}",
                correlationId, eventId, registrationId);

            // Phase 6A.52: Log before loading registration
            _logger.LogInformation(
                "[Phase 6A.52] [Webhook-3] Loading registration - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}",
                correlationId, registrationId);

            // Phase 6A.49 FIX: Load Registration DIRECTLY with tracking enabled
            var registration = await _registrationRepository.GetByIdAsync(registrationId);
            if (registration == null)
            {
                _logger.LogError(
                    "[Phase 6A.52] [Webhook-ERROR] Registration not found - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, SessionId: {SessionId}",
                    correlationId, registrationId, session.Id);
                return;
            }

            _logger.LogInformation(
                "[Phase 6A.52] [Webhook-4] Registration loaded - CorrelationId: {CorrelationId}, PaymentStatus: {PaymentStatus}, CurrentStripePaymentIntentId: {StripePaymentIntentId}",
                correlationId, registration.PaymentStatus, registration.StripePaymentIntentId);

            // Verify registration belongs to the expected event (security check)
            if (registration.EventId != eventId)
            {
                _logger.LogError(
                    "[Phase 6A.52] [Webhook-ERROR] Event mismatch - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, ActualEventId: {ActualEventId}, ExpectedEventId: {ExpectedEventId}",
                    correlationId, registrationId, registration.EventId, eventId);
                return;
            }

            // Phase 6A.52: Log domain events BEFORE CompletePayment (with correlation ID)
            _logger.LogInformation(
                "[Phase 6A.52] [Webhook-5] Before CompletePayment - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, DomainEvents.Count: {Count}",
                correlationId, registrationId, registration.DomainEvents.Count);

            // Complete payment on registration domain entity
            var paymentIntentId = session.PaymentIntentId ?? session.Id;
            var completeResult = registration.CompletePayment(paymentIntentId);

            if (completeResult.IsFailure)
            {
                _logger.LogError(
                    "[Phase 6A.52] [Webhook-ERROR] CompletePayment failed - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, Error: {Error}",
                    correlationId, registrationId, completeResult.Error);
                return;
            }

            // Phase 6A.52: Log domain events AFTER CompletePayment
            _logger.LogInformation(
                "[Phase 6A.52] [Webhook-6] After CompletePayment - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, DomainEvents.Count: {Count}, EventTypes: [{EventTypes}]",
                correlationId, registrationId, registration.DomainEvents.Count, string.Join(", ", registration.DomainEvents.Select(e => e.GetType().Name)));

            // Phase 6A.51 FIX: Restore Update() call (critical for domain event dispatch)
            _registrationRepository.Update(registration);

            // Phase 6A.52: Log BEFORE CommitAsync
            _logger.LogInformation(
                "[Phase 6A.52] [Webhook-7] Before CommitAsync - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, DomainEvents: {Count}",
                correlationId, registrationId, registration.DomainEvents.Count);

            // Save changes and dispatch domain events
            await _unitOfWork.CommitAsync();

            // Phase 6A.52: Log AFTER CommitAsync
            _logger.LogInformation(
                "[Phase 6A.52] [Webhook-8] After CommitAsync - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, DomainEvents: {Count} (should be cleared)",
                correlationId, registrationId, registration.DomainEvents.Count);

            _logger.LogInformation(
                "[Phase 6A.52] [Webhook-SUCCESS] Payment completed successfully - CorrelationId: {CorrelationId}, EventId: {EventId}, RegistrationId: {RegistrationId}, PaymentIntentId: {PaymentIntentId}",
                correlationId, eventId, registrationId, paymentIntentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling checkout.session.completed webhook - Type: {ExceptionType}, Message: {Message}, InnerException: {InnerException}",
                ex.GetType().FullName, ex.Message, ex.InnerException?.Message ?? "None");
            throw; // Re-throw to trigger outer catch block with HTTP 500
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
