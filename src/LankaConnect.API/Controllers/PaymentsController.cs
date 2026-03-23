using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using LankaConnect.Domain.Payments;
using LankaConnect.Infrastructure.Payments.Configuration;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Events;
using LankaConnect.Application.Events.Services;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Payments controller for Stripe integration
/// Phase 6A.4: Stripe Payment Integration - MVP
/// Session 23 (Phase 2B): Extended for event ticket payment webhooks
/// Phase 0: Refactored — webhook handler logic extracted into injectable services
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
    private readonly IRegistrationWebhookHandler _registrationWebhookHandler;
    private readonly IAdditionWebhookHandler _additionWebhookHandler;
    private readonly IDonationWebhookHandler _donationWebhookHandler;
    private readonly ICollectionWebhookHandler _collectionWebhookHandler;
    private readonly ISponsorWebhookHandler _sponsorWebhookHandler;
    private readonly IAddOnPurchaseWebhookHandler _addOnPurchaseWebhookHandler;
    private readonly StripeOptions _stripeOptions;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IStripeClient stripeClient,
        IStripeCustomerRepository customerRepository,
        IStripeWebhookEventRepository webhookEventRepository,
        IUserRepository userRepository,
        IRegistrationWebhookHandler registrationWebhookHandler,
        IAdditionWebhookHandler additionWebhookHandler,
        IDonationWebhookHandler donationWebhookHandler,
        ICollectionWebhookHandler collectionWebhookHandler,
        ISponsorWebhookHandler sponsorWebhookHandler,
        IAddOnPurchaseWebhookHandler addOnPurchaseWebhookHandler,
        IOptions<StripeOptions> stripeOptions,
        ILogger<PaymentsController> logger)
    {
        _stripeClient = stripeClient;
        _customerRepository = customerRepository;
        _webhookEventRepository = webhookEventRepository;
        _userRepository = userRepository;
        _registrationWebhookHandler = registrationWebhookHandler;
        _additionWebhookHandler = additionWebhookHandler;
        _donationWebhookHandler = donationWebhookHandler;
        _collectionWebhookHandler = collectionWebhookHandler;
        _sponsorWebhookHandler = sponsorWebhookHandler;
        _addOnPurchaseWebhookHandler = addOnPurchaseWebhookHandler;
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

        // Phase 6A.136: Reject oversized webhook bodies before reading into memory.
        // Stripe webhooks are typically < 10KB. This prevents DoS via large payloads.
        const long maxWebhookBodySize = 65536; // 64KB
        if (HttpContext.Request.ContentLength > maxWebhookBodySize)
        {
            _logger.LogWarning(
                "[Phase 6A.136] Webhook body too large: ContentLength={ContentLength}, MaxAllowed={MaxAllowed}",
                HttpContext.Request.ContentLength, maxWebhookBodySize);
            return BadRequest("Webhook body too large");
        }

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

            // Check idempotency - skip only if event was fully processed
            if (await _webhookEventRepository.IsEventProcessedAsync(stripeEvent.Id))
            {
                _logger.LogInformation("Event {EventId} already processed, skipping", stripeEvent.Id);
                return Ok();
            }

            // Record event only if it doesn't exist yet (prevents duplicate INSERT on retries)
            if (!await _webhookEventRepository.EventExistsAsync(stripeEvent.Id))
            {
                await _webhookEventRepository.RecordEventAsync(stripeEvent.Id, stripeEvent.Type);
            }
            else
            {
                _logger.LogInformation("Event {EventId} exists but not processed, reprocessing", stripeEvent.Id);
            }

            // Session 23 (Phase 2B): Process event based on type
            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    await HandleCheckoutSessionCompletedAsync(stripeEvent);
                    break;

                // Phase 6A.81: Handle expired checkout sessions
                case "checkout.session.expired":
                    await HandleCheckoutSessionExpiredAsync(stripeEvent);
                    break;

                // Phase 6A.91: Handle refund completion
                case "charge.refunded":
                    await HandleChargeRefundedAsync(stripeEvent);
                    break;

                // Phase 6A.136 Issue #3: Log payment failures for observability.
                // The checkout session remains active — user can retry payment.
                // Registration.FailPayment() is NOT called here because the session may still succeed.
                case "payment_intent.payment_failed":
                    HandlePaymentIntentFailed(stripeEvent);
                    break;

                default:
                    _logger.LogInformation("Unhandled webhook event type {EventType}, skipping", stripeEvent.Type);
                    break;
            }

            // Mark as processed
            _logger.LogInformation(
                "[Phase 6A.X] [Webhook-PRE-MARK] About to mark event as processed - EventId: {EventId}",
                stripeEvent.Id);

            try
            {
                await _webhookEventRepository.MarkEventAsProcessedAsync(stripeEvent.Id);
                _logger.LogInformation(
                    "[Phase 6A.X] [Webhook-POST-MARK] Successfully marked event as processed - EventId: {EventId}",
                    stripeEvent.Id);
            }
            catch (Exception markEx)
            {
                _logger.LogError(markEx,
                    "[Phase 6A.X] [Webhook-MARK-FAILED] Failed to mark event as processed - EventId: {EventId}, Error: {Error}",
                    stripeEvent.Id, markEx.Message);
                // Don't rethrow - the main processing succeeded, just the mark failed
                // This prevents returning 500 when the payment actually processed successfully
            }

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
    /// Session 23 (Phase 2B): Handles checkout.session.completed webhook for event ticket payments.
    /// Phase 0: Thin dispatcher — routes to the appropriate webhook handler based on payment_type metadata.
    /// </summary>
    private async Task HandleCheckoutSessionCompletedAsync(Stripe.Event stripeEvent)
    {
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

            // Extract primitives from Stripe objects for handler dispatch
            var metadata = session.Metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                ?? new Dictionary<string, string>();
            var paymentIntentId = session.PaymentIntentId ?? session.Id;

            // Route based on payment_type metadata
            if (metadata.TryGetValue("payment_type", out var paymentType))
            {
                switch (paymentType)
                {
                    case "addition":
                        _logger.LogInformation(
                            "[AddOnlyAttendees] [Webhook-Route] Routing to addition handler - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                            correlationId, session.Id);
                        await _additionWebhookHandler.HandleCheckoutCompletedAsync(
                            session.Id, paymentIntentId, metadata, correlationId);
                        return;

                    case "donation":
                        _logger.LogInformation(
                            "[Donation] [Webhook-Route] Routing to donation handler - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                            correlationId, session.Id);
                        await _donationWebhookHandler.HandleCheckoutCompletedAsync(
                            session.Id, paymentIntentId, metadata, correlationId);
                        return;

                    case "collection":
                        _logger.LogInformation(
                            "[Collection] [Webhook-Route] Routing to collection handler - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                            correlationId, session.Id);
                        await _collectionWebhookHandler.HandleCheckoutCompletedAsync(
                            session.Id, paymentIntentId, metadata, correlationId);
                        return;

                    case "sponsor":
                        _logger.LogInformation(
                            "[Sponsor] [Webhook-Route] Routing to sponsor handler - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                            correlationId, session.Id);
                        await _sponsorWebhookHandler.HandleCheckoutCompletedAsync(
                            session.Id, paymentIntentId, metadata, correlationId);
                        return;

                    case "add_on_purchase":
                        _logger.LogInformation(
                            "[AddOnPurchase] [Webhook-Route] Routing to add-on purchase handler - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                            correlationId, session.Id);
                        await _addOnPurchaseWebhookHandler.HandleCheckoutCompletedAsync(
                            session.Id, paymentIntentId, metadata, correlationId);
                        return;
                }
            }

            // Default: registration payment
            await _registrationWebhookHandler.HandleCheckoutCompletedAsync(
                session.Id, session.PaymentStatus, paymentIntentId, metadata, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling checkout.session.completed webhook - Type: {ExceptionType}, Message: {Message}, InnerException: {InnerException}",
                ex.GetType().FullName, ex.Message, ex.InnerException?.Message ?? "None");
            throw; // Re-throw to trigger outer catch block with HTTP 500
        }
    }

    /// <summary>
    /// Phase 6A.81: Handles checkout.session.expired webhook to mark abandoned registrations/donations.
    /// Phase 0: Thin dispatcher — routes to the appropriate webhook handler based on payment_type metadata.
    /// </summary>
    private async Task HandleCheckoutSessionExpiredAsync(Stripe.Event stripeEvent)
    {
        var correlationId = Guid.NewGuid();

        try
        {
            var session = stripeEvent.Data.Object as Session;
            if (session == null)
            {
                _logger.LogWarning(
                    "[Phase 6A.81] [Webhook-Expired-ERROR] Checkout session data is null - CorrelationId: {CorrelationId}, EventId: {EventId}",
                    correlationId, stripeEvent.Id);
                return;
            }

            _logger.LogInformation(
                "[Phase 6A.81] [Webhook-Expired-1] Processing checkout.session.expired - CorrelationId: {CorrelationId}, SessionId: {SessionId}, StripeEventId: {StripeEventId}",
                correlationId, session.Id, stripeEvent.Id);

            // Extract primitives from Stripe objects for handler dispatch
            var metadata = session.Metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                ?? new Dictionary<string, string>();

            // Route based on payment_type metadata
            if (metadata.TryGetValue("payment_type", out var paymentType))
            {
                switch (paymentType)
                {
                    case "donation":
                        await _donationWebhookHandler.HandleCheckoutExpiredAsync(
                            session.Id, metadata, correlationId);
                        return;

                    case "collection":
                        await _collectionWebhookHandler.HandleCheckoutExpiredAsync(
                            session.Id, metadata, correlationId);
                        return;

                    case "sponsor":
                        await _sponsorWebhookHandler.HandleCheckoutExpiredAsync(
                            session.Id, metadata, correlationId);
                        return;

                    case "add_on_purchase":
                        await _addOnPurchaseWebhookHandler.HandleCheckoutExpiredAsync(
                            session.Id, metadata, correlationId);
                        return;

                    // Phase 6A.136 Issue #2: Handle addition session expiry
                    case "addition":
                        _logger.LogInformation(
                            "[Phase 6A.136] [Webhook-Expired-Route] Routing to addition expiry handler - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                            correlationId, session.Id);
                        await _additionWebhookHandler.HandleCheckoutExpiredAsync(
                            session.Id, metadata, correlationId);
                        return;
                }
            }

            // Default: registration payment expiry
            await _registrationWebhookHandler.HandleCheckoutExpiredAsync(
                session.Id, metadata, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 6A.81] [Webhook-Expired-ERROR] Error handling checkout.session.expired webhook - CorrelationId: {CorrelationId}, Type: {ExceptionType}, Message: {Message}",
                correlationId, ex.GetType().FullName, ex.Message);
            throw; // Re-throw to trigger outer catch block with HTTP 500
        }
    }

    /// <summary>
    /// Phase 6A.136 Issue #3: Logs payment_intent.payment_failed for observability.
    /// The checkout session remains open for retry — we do NOT call FailPayment() here.
    /// If the session ultimately expires, checkout.session.expired will handle cleanup.
    /// </summary>
    private void HandlePaymentIntentFailed(Stripe.Event stripeEvent)
    {
        try
        {
            var paymentIntent = stripeEvent.Data.Object as Stripe.PaymentIntent;
            if (paymentIntent == null)
            {
                _logger.LogWarning("[Phase 6A.136] payment_intent.payment_failed: PaymentIntent data is null");
                return;
            }

            var metadata = paymentIntent.Metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                ?? new Dictionary<string, string>();
            metadata.TryGetValue("payment_type", out var paymentType);
            metadata.TryGetValue("registration_id", out var registrationId);

            _logger.LogWarning(
                "[Phase 6A.136] [Payment-Failed] PaymentIntent failed - PaymentIntentId: {PaymentIntentId}, " +
                "PaymentType: {PaymentType}, RegistrationId: {RegistrationId}, " +
                "LastError: {LastError}, FailureCode: {FailureCode}",
                paymentIntent.Id,
                paymentType ?? "registration",
                registrationId ?? "unknown",
                paymentIntent.LastPaymentError?.Message ?? "none",
                paymentIntent.LastPaymentError?.Code ?? "none");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Phase 6A.136] Error handling payment_intent.payment_failed webhook");
        }
    }

    /// <summary>
    /// Phase 6A.91: Handles charge.refunded webhook to complete refund workflow.
    /// Phase 0: Extracts refund info from Stripe objects and delegates to RegistrationWebhookHandler.
    /// </summary>
    private async Task HandleChargeRefundedAsync(Stripe.Event stripeEvent)
    {
        var correlationId = Guid.NewGuid();

        try
        {
            var charge = stripeEvent.Data.Object as Charge;
            if (charge == null)
            {
                _logger.LogWarning(
                    "[Phase 6A.91] [Webhook-Refund-ERROR] Charge data is null - CorrelationId: {CorrelationId}, EventId: {EventId}",
                    correlationId, stripeEvent.Id);
                return;
            }

            _logger.LogInformation(
                "[Phase 6A.91] [Webhook-Refund-1] Processing charge.refunded - CorrelationId: {CorrelationId}, ChargeId: {ChargeId}, StripeEventId: {StripeEventId}, AmountRefunded: {AmountRefunded}",
                correlationId, charge.Id, stripeEvent.Id, charge.AmountRefunded);

            // Get the latest refund - try webhook payload first, then fetch from Stripe API
            Refund? latestRefund = charge.Refunds?.Data?.FirstOrDefault();

            // Phase 6A.X FIX: Webhook payload may not include Refunds collection - fetch from Stripe API
            if (latestRefund == null)
            {
                _logger.LogInformation(
                    "[Phase 6A.91] [Webhook-Refund-1b] Refunds not in webhook payload, fetching from Stripe API - CorrelationId: {CorrelationId}, ChargeId: {ChargeId}",
                    correlationId, charge.Id);

                try
                {
                    var refundService = new RefundService(_stripeClient);
                    var refunds = await refundService.ListAsync(new RefundListOptions { Charge = charge.Id, Limit = 1 });
                    latestRefund = refunds.Data?.FirstOrDefault();
                }
                catch (StripeException ex)
                {
                    _logger.LogWarning(ex,
                        "[Phase 6A.91] [Webhook-Refund-WARN] Failed to fetch refunds from Stripe API - CorrelationId: {CorrelationId}, ChargeId: {ChargeId}",
                        correlationId, charge.Id);
                }
            }

            if (latestRefund == null)
            {
                _logger.LogWarning(
                    "[Phase 6A.91] [Webhook-Refund-WARN] No refunds found on charge - CorrelationId: {CorrelationId}, ChargeId: {ChargeId}",
                    correlationId, charge.Id);
                return;
            }

            _logger.LogInformation(
                "[Phase 6A.91] [Webhook-Refund-2] Refund found - CorrelationId: {CorrelationId}, RefundId: {RefundId}, RefundStatus: {Status}, RefundAmount: {Amount}",
                correlationId, latestRefund.Id, latestRefund.Status, latestRefund.Amount);

            // Extract primitives and delegate to handler (keeps Stripe SDK dependency out of Application layer)
            var refundMetadata = latestRefund.Metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // Phase 6A.136 Issue #1: Route charge.refunded by payment_type metadata.
            // Previously ALL refunds went to RegistrationWebhookHandler regardless of payment type.
            // Check refund metadata first, then charge metadata for payment_type.
            var chargeMetadata = charge.Metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            string? refundPaymentType = null;
            refundMetadata?.TryGetValue("refund_type", out refundPaymentType);
            if (string.IsNullOrEmpty(refundPaymentType))
            {
                chargeMetadata?.TryGetValue("payment_type", out refundPaymentType);
            }

            if (!string.IsNullOrEmpty(refundPaymentType))
            {
                _logger.LogInformation(
                    "[Phase 6A.136] [Webhook-Refund-Route] Routing charge.refunded by type - CorrelationId: {CorrelationId}, PaymentType: {PaymentType}",
                    correlationId, refundPaymentType);

                switch (refundPaymentType)
                {
                    case "add_on_cancellation":
                    case "add_on_purchase":
                        // Add-on refunds are handled inline by AddOnRefundService (marks entity as refunded).
                        // The webhook arriving here is expected — log and acknowledge.
                        _logger.LogInformation(
                            "[Phase 6A.136] [Webhook-Refund-AddOn] Add-on refund acknowledged - CorrelationId: {CorrelationId}, ChargeId: {ChargeId}, RefundId: {RefundId}",
                            correlationId, charge.Id, latestRefund.Id);
                        return;

                    case "donation":
                    case "collection":
                    case "sponsor":
                        // Phase 6A.136: Log non-registration refunds. Domain state update for these
                        // payment types is a Phase E enhancement (dedicated refund services).
                        _logger.LogWarning(
                            "[Phase 6A.136] [Webhook-Refund-NonReg] Non-registration refund received but no handler yet - CorrelationId: {CorrelationId}, PaymentType: {PaymentType}, ChargeId: {ChargeId}, RefundId: {RefundId}",
                            correlationId, refundPaymentType, charge.Id, latestRefund.Id);
                        return;
                }
            }

            // Default: registration payment refund
            await _registrationWebhookHandler.HandleChargeRefundedAsync(
                charge.Id,
                charge.PaymentIntentId,
                latestRefund.Id,
                charge.AmountRefunded,
                refundMetadata,
                correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 6A.91] [Webhook-Refund-ERROR] Error handling charge.refunded webhook - CorrelationId: {CorrelationId}, Type: {ExceptionType}, Message: {Message}",
                correlationId, ex.GetType().FullName, ex.Message);
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
