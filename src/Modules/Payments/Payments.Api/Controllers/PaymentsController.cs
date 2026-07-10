using LankaConnect.Modules.Identity.Contracts; // W4.7.d.3
using LankaConnect.Modules.Payments.Domain.Repositories; // IStripe* interfaces remain in Payments.Domain
using LankaConnect.Products.LankaEvents.Contracts.LegacyPromotions; // W4.4.d.2: 3 repo interfaces moved here
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using LankaConnect.Modules.Payments.Infrastructure.Configuration;
using LankaConnect.Modules.Identity.Domain.Entities;
using LankaConnect.Modules.Identity.Domain.Repositories;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Modules.Identity.Domain.Events;
using LankaConnect.Products.LankaEvents.Domain;
namespace LankaConnect.Modules.Payments.Api.Controllers;

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
    private readonly IIdentityQueries _identityQueries;
    private readonly IRegistrationWebhookHandler _registrationWebhookHandler;
    private readonly IAdditionWebhookHandler _additionWebhookHandler;
    private readonly IDonationWebhookHandler _donationWebhookHandler;
    private readonly ICollectionWebhookHandler _collectionWebhookHandler;
    private readonly ISponsorWebhookHandler _sponsorWebhookHandler;
    private readonly IAddOnPurchaseWebhookHandler _addOnPurchaseWebhookHandler;
    // Phase 6A.157 — sibling to ISponsorWebhookHandler for packaged sponsorships.
    private readonly IPackageSponsorWebhookHandler _packageSponsorWebhookHandler;
    // Phase 6A.148.W5.5.D4 — workflow-line lookup for iterate-all-refunds router. Without
    // this the dispatcher uses charge.Refunds.Data.FirstOrDefault() and silently mis-routes
    // when a single PI carries multiple refunds of different types (Bug 1: ticket+sponsor
    // on shared PI with operator-UAT registration 8df17ec1 stuck Cancelled).
    private readonly LankaConnect.Products.LankaEvents.Contracts.LegacyPromotions.IRefundRequestRepository _refundRequestRepository;
    private readonly StripeOptions _stripeOptions;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IStripeClient stripeClient,
        IStripeCustomerRepository customerRepository,
        IStripeWebhookEventRepository webhookEventRepository,
        IIdentityQueries identityQueries,
        IRegistrationWebhookHandler registrationWebhookHandler,
        IAdditionWebhookHandler additionWebhookHandler,
        IDonationWebhookHandler donationWebhookHandler,
        ICollectionWebhookHandler collectionWebhookHandler,
        ISponsorWebhookHandler sponsorWebhookHandler,
        IAddOnPurchaseWebhookHandler addOnPurchaseWebhookHandler,
        IPackageSponsorWebhookHandler packageSponsorWebhookHandler,
        LankaConnect.Products.LankaEvents.Contracts.LegacyPromotions.IRefundRequestRepository refundRequestRepository,
        IOptions<StripeOptions> stripeOptions,
        ILogger<PaymentsController> logger)
    {
        _stripeClient = stripeClient;
        _customerRepository = customerRepository;
        _webhookEventRepository = webhookEventRepository;
        _identityQueries = identityQueries;
        _registrationWebhookHandler = registrationWebhookHandler;
        _additionWebhookHandler = additionWebhookHandler;
        _donationWebhookHandler = donationWebhookHandler;
        _collectionWebhookHandler = collectionWebhookHandler;
        _sponsorWebhookHandler = sponsorWebhookHandler;
        _addOnPurchaseWebhookHandler = addOnPurchaseWebhookHandler;
        _packageSponsorWebhookHandler = packageSponsorWebhookHandler;
        _refundRequestRepository = refundRequestRepository;
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
            var user = await _identityQueries.GetContactInfoAsync(userId);
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
                    Email = user.Email,
                    Name = user.DisplayName,
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
                    user.Email,
                    user.DisplayName,
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

                    // Phase 6A.157 — packaged sponsorship purchase. Routes to
                    // the new handler which calls Sponsor.CompletePackagePayment
                    // (raises PackageSponsorCompletedEvent → forked email handler).
                    case "package_sponsor":
                        _logger.LogInformation(
                            "[PackageSponsor] [Webhook-Route] Routing to package sponsor handler - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                            correlationId, session.Id);
                        await _packageSponsorWebhookHandler.HandleCheckoutCompletedAsync(
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

                    // Phase 6A.157 — package sponsor expiry restores reserved stock
                    case "package_sponsor":
                        await _packageSponsorWebhookHandler.HandleCheckoutExpiredAsync(
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
    /// Phase 0: Extracts refund info from Stripe objects and delegates to typed handlers.
    ///
    /// Phase 6A.148.W5.5.D4 — REWRITTEN to iterate ALL refunds on the charge and route each
    /// independently. The prior implementation used <c>charge.Refunds.Data.FirstOrDefault()</c>
    /// which silently mis-routed when a single PaymentIntent carried multiple refunds of
    /// different types (e.g., a bundled-at-registration sponsorship + the registration's
    /// own ticket portion share one PI; W5.D2 per-line dispatcher creates two distinct
    /// Stripe refunds; both <c>charge.refunded</c> webhooks picked the most-recent refund
    /// via <c>FirstOrDefault</c> and routed BOTH events to the sponsor handler — the ticket
    /// refund's registration handler was NEVER invoked, leaving the registration stuck in
    /// <c>Cancelled</c> with no completion email). Operator UAT proof: registration
    /// <c>8df17ec1-42b5-41ed-808c-d66914e5699d</c> on event ad8903c4 on 2026-05-22.
    ///
    /// Per-refund routing precedence:
    ///   1. Workflow-line lookup via <see cref="LankaConnect.Products.LankaEvents.Contracts.LegacyPromotions.IRefundRequestRepository.GetWorkflowLineByStripeRefundIdAsync"/>
    ///      (authoritative for refunds dispatched through the 6A.148 approval workflow).
    ///   2. Metadata-based switch on <c>refund_type</c> / <c>payment_type</c> (legacy fallback
    ///      for pre-6A.148 direct-Stripe refunds, donation, etc.).
    ///
    /// Idempotency is guaranteed downstream — every typed handler refuses duplicate state
    /// transitions, and <c>RefundLineDispatcher</c>'s W5.D1 Stripe IdempotencyKey ensures
    /// no double-charging.
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

            // W5.5.D4 — iterate ALL refunds on the charge. Fallback to Stripe API list when
            // the webhook payload omits the refunds collection. Limit=100 captures
            // practical max refunds per charge (operator UAT scenarios are <=10).
            var refundsOnCharge = charge.Refunds?.Data?.ToList() ?? new List<Refund>();
            if (refundsOnCharge.Count == 0)
            {
                _logger.LogInformation(
                    "[Phase 6A.91] [Webhook-Refund-1b] Refunds not in webhook payload, fetching from Stripe API - CorrelationId: {CorrelationId}, ChargeId: {ChargeId}",
                    correlationId, charge.Id);

                try
                {
                    var refundService = new RefundService(_stripeClient);
                    var refunds = await refundService.ListAsync(
                        new RefundListOptions { Charge = charge.Id, Limit = 100 });
                    refundsOnCharge = refunds.Data?.ToList() ?? new List<Refund>();
                }
                catch (StripeException ex)
                {
                    _logger.LogWarning(ex,
                        "[Phase 6A.91] [Webhook-Refund-WARN] Failed to fetch refunds from Stripe API - CorrelationId: {CorrelationId}, ChargeId: {ChargeId}",
                        correlationId, charge.Id);
                }
            }

            if (refundsOnCharge.Count == 0)
            {
                _logger.LogWarning(
                    "[Phase 6A.91] [Webhook-Refund-WARN] No refunds found on charge - CorrelationId: {CorrelationId}, ChargeId: {ChargeId}",
                    correlationId, charge.Id);
                return;
            }

            _logger.LogInformation(
                "[Phase 6A.148.W5.5.D4] [Webhook-Refund-IterateAll] Iterating {RefundCount} refund(s) on charge - CorrelationId: {CorrelationId}, ChargeId: {ChargeId}",
                refundsOnCharge.Count, correlationId, charge.Id);

            var chargeMetadata = charge.Metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // Route each refund independently. Failures on one refund must not stop the
            // others (downstream handlers are idempotent so a partial-success retry is
            // safe). Each per-refund try/catch isolates failures.
            foreach (var refund in refundsOnCharge)
            {
                if (!string.Equals(refund.Status, "succeeded", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation(
                        "[Phase 6A.148.W5.5.D4] [Webhook-Refund-SkipNonSucceeded] Skipping refund - CorrelationId: {CorrelationId}, RefundId: {RefundId}, Status: {Status}",
                        correlationId, refund.Id, refund.Status);
                    continue;
                }

                try
                {
                    await RouteSingleRefundAsync(charge, refund, chargeMetadata, correlationId);
                }
                catch (Exception ex)
                {
                    // Isolate per-refund failure; continue with remaining refunds.
                    _logger.LogError(ex,
                        "[Phase 6A.148.W5.5.D4] [Webhook-Refund-PerRefundEx] Routing exception for refund (continuing with others) - CorrelationId: {CorrelationId}, RefundId: {RefundId}",
                        correlationId, refund.Id);
                }
            }
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
    /// Phase 6A.148.W5.5.D4 — route ONE refund to the correct typed handler.
    ///
    /// Decision flow:
    ///   1. Workflow-line lookup via <c>GetWorkflowLineByStripeRefundIdAsync</c>. If hit,
    ///      dispatch by <c>line.Type</c> — authoritative for 6A.148 workflow refunds.
    ///   2. Metadata-based switch on <c>refund_type</c> (refund metadata) or
    ///      <c>payment_type</c> (charge metadata). Preserves legacy direct-Stripe refunds.
    ///   3. Final fallback: <c>RegistrationWebhookHandler</c> (the pre-6A.136 default).
    /// </summary>
    private async Task RouteSingleRefundAsync(
        Charge charge,
        Refund refund,
        Dictionary<string, string>? chargeMetadata,
        Guid correlationId)
    {
        var refundMetadata = refund.Metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        _logger.LogInformation(
            "[Phase 6A.91] [Webhook-Refund-2] Routing refund - CorrelationId: {CorrelationId}, RefundId: {RefundId}, RefundAmount: {Amount}",
            correlationId, refund.Id, refund.Amount);

        // ① Workflow-line lookup (authoritative for 6A.148 refunds). Type-agnostic —
        //    the line itself knows whether it's a Ticket/AddOn/Sponsor/Collection refund.
        var workflowLine = await _refundRequestRepository
            .GetWorkflowLineByStripeRefundIdAsync(refund.Id);
        if (workflowLine != null)
        {
            _logger.LogInformation(
                "[Phase 6A.148.W5.5.D4] [Webhook-Refund-Route-Workflow] Refund matched to workflow line - CorrelationId: {CorrelationId}, RefundId: {RefundId}, LineId: {LineId}, Type: {Type}, RrId: {RrId}",
                correlationId, refund.Id, workflowLine.Id, workflowLine.Type, workflowLine.RefundRequestId);

            switch (workflowLine.Type)
            {
                case LankaConnect.Products.LankaEvents.Domain.Enums.RefundLineItemType.AddOn:
                    await _addOnPurchaseWebhookHandler.HandleChargeRefundedAsync(
                        charge.PaymentIntentId, refund.Id, correlationId);
                    return;

                case LankaConnect.Products.LankaEvents.Domain.Enums.RefundLineItemType.Sponsor:
                    await _sponsorWebhookHandler.HandleChargeRefundedAsync(
                        charge.PaymentIntentId, refund.Id, correlationId);
                    return;

                case LankaConnect.Products.LankaEvents.Domain.Enums.RefundLineItemType.Collection:
                    await _collectionWebhookHandler.HandleChargeRefundedAsync(
                        charge.PaymentIntentId, refund.Id, correlationId);
                    return;

                case LankaConnect.Products.LankaEvents.Domain.Enums.RefundLineItemType.Ticket:
                    await _registrationWebhookHandler.HandleChargeRefundedAsync(
                        charge.Id, charge.PaymentIntentId, refund.Id,
                        refund.Amount, refundMetadata, correlationId);
                    return;
            }
        }

        // ② Metadata-based switch (legacy direct-Stripe refunds / non-workflow paths).
        string? refundPaymentType = null;
        refundMetadata?.TryGetValue("refund_type", out refundPaymentType);
        if (string.IsNullOrEmpty(refundPaymentType))
        {
            chargeMetadata?.TryGetValue("payment_type", out refundPaymentType);
        }

        if (!string.IsNullOrEmpty(refundPaymentType))
        {
            _logger.LogInformation(
                "[Phase 6A.136] [Webhook-Refund-Route-Legacy] Routing by metadata (no workflow line matched) - CorrelationId: {CorrelationId}, RefundId: {RefundId}, PaymentType: {PaymentType}",
                correlationId, refund.Id, refundPaymentType);

            switch (refundPaymentType)
            {
                case "add_on_cancellation":
                case "add_on_purchase":
                    await _addOnPurchaseWebhookHandler.HandleChargeRefundedAsync(
                        charge.PaymentIntentId, refund.Id, correlationId);
                    return;
                case "donation":
                    await _donationWebhookHandler.HandleChargeRefundedAsync(
                        charge.PaymentIntentId, refund.Id, correlationId);
                    return;
                case "collection":
                    await _collectionWebhookHandler.HandleChargeRefundedAsync(
                        charge.PaymentIntentId, refund.Id, correlationId);
                    return;
                case "sponsor":
                    await _sponsorWebhookHandler.HandleChargeRefundedAsync(
                        charge.PaymentIntentId, refund.Id, correlationId);
                    return;
            }
        }
        else
        {
            _logger.LogWarning(
                "[Phase 6A.148.W4.D14] [Webhook-Refund-Default-Route] Refund falling through to default Registration handler — no workflow line match, no refund_type metadata, no charge payment_type. CorrelationId: {CorrelationId}, ChargeId: {ChargeId}, RefundId: {RefundId}, RefundMetaKeys: [{RefundKeys}], ChargeMetaKeys: [{ChargeKeys}]",
                correlationId, charge.Id, refund.Id,
                refundMetadata == null ? "" : string.Join(",", refundMetadata.Keys),
                chargeMetadata == null ? "" : string.Join(",", chargeMetadata.Keys));
        }

        // ③ Final fallback: registration handler (pre-6A.136 default).
        await _registrationWebhookHandler.HandleChargeRefundedAsync(
            charge.Id, charge.PaymentIntentId, refund.Id,
            refund.Amount, refundMetadata, correlationId);
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
