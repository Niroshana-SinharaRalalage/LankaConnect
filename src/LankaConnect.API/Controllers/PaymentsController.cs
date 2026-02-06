using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using LankaConnect.Domain.Payments;
using LankaConnect.Infrastructure.Payments.Configuration;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Services;
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
    private readonly IRegistrationAdditionRepository _additionRepository;
    private readonly IRegistrationPaymentRepository _paymentRepository;
    private readonly IRevenueCalculatorService _revenueCalculatorService;
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
        IRegistrationAdditionRepository additionRepository,
        IRegistrationPaymentRepository paymentRepository,
        IRevenueCalculatorService revenueCalculatorService,
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
        _additionRepository = additionRepository;
        _paymentRepository = paymentRepository;
        _revenueCalculatorService = revenueCalculatorService;
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

            // [AddOnlyAttendees] Check if this is an addition payment
            if (session.Metadata.TryGetValue("payment_type", out var paymentType) && paymentType == "addition")
            {
                _logger.LogInformation(
                    "[AddOnlyAttendees] [Webhook-Route] Routing to addition handler - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                    correlationId, session.Id);
                await HandleAdditionCheckoutCompletedAsync(session, stripeEvent, correlationId);
                return;
            }

            // Extract metadata (regular registration payment)
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

            // Phase 6A.81: Log registration state BEFORE payment completion
            _logger.LogInformation(
                "[Phase 6A.81] [Webhook-State] Before CompletePayment - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, CurrentStatus: {Status}, CurrentPaymentStatus: {PaymentStatus}, CheckoutExpiresAt: {ExpiresAt}",
                correlationId, registrationId, registration.Status, registration.PaymentStatus, registration.CheckoutSessionExpiresAt?.ToString("o") ?? "null");

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

            // Phase 6A.81: Log registration state AFTER payment completion (Preliminary → Confirmed)
            _logger.LogInformation(
                "[Phase 6A.81] [Webhook-State] After CompletePayment - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, NewStatus: {Status}, NewPaymentStatus: {PaymentStatus}, Transition: Preliminary→Confirmed",
                correlationId, registrationId, registration.Status, registration.PaymentStatus);

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
    /// [AddOnlyAttendees] Handles checkout.session.completed for addition payments.
    /// Completes the RegistrationAddition, merges attendees into registration,
    /// and creates a RegistrationPayment record.
    /// </summary>
    private async Task HandleAdditionCheckoutCompletedAsync(Session session, Stripe.Event stripeEvent, Guid correlationId)
    {
        try
        {
            _logger.LogInformation(
                "[AddOnlyAttendees] [Webhook-Addition-1] Processing addition payment - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                correlationId, session.Id);

            // Extract metadata
            if (!session.Metadata.TryGetValue("registration_addition_id", out var additionIdStr) ||
                !Guid.TryParse(additionIdStr, out var additionId))
            {
                _logger.LogWarning(
                    "[AddOnlyAttendees] [Webhook-Addition-ERROR] Missing registration_addition_id - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                    correlationId, session.Id);
                return;
            }

            if (!session.Metadata.TryGetValue("registration_id", out var registrationIdStr) ||
                !Guid.TryParse(registrationIdStr, out var registrationId))
            {
                _logger.LogWarning(
                    "[AddOnlyAttendees] [Webhook-Addition-ERROR] Missing registration_id - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                    correlationId, session.Id);
                return;
            }

            if (!session.Metadata.TryGetValue("event_id", out var eventIdStr) ||
                !Guid.TryParse(eventIdStr, out var eventId))
            {
                _logger.LogWarning(
                    "[AddOnlyAttendees] [Webhook-Addition-ERROR] Missing event_id - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                    correlationId, session.Id);
                return;
            }

            _logger.LogInformation(
                "[AddOnlyAttendees] [Webhook-Addition-2] Metadata extracted - CorrelationId: {CorrelationId}, AdditionId: {AdditionId}, RegistrationId: {RegistrationId}, EventId: {EventId}",
                correlationId, additionId, registrationId, eventId);

            // Load the RegistrationAddition (with tracking)
            var addition = await _additionRepository.GetByIdAsync(additionId);
            if (addition == null)
            {
                _logger.LogError(
                    "[AddOnlyAttendees] [Webhook-Addition-ERROR] RegistrationAddition not found - CorrelationId: {CorrelationId}, AdditionId: {AdditionId}",
                    correlationId, additionId);
                return;
            }

            _logger.LogInformation(
                "[AddOnlyAttendees] [Webhook-Addition-3] Addition loaded - CorrelationId: {CorrelationId}, AdditionId: {AdditionId}, Status: {Status}, NewAttendeesCount: {Count}",
                correlationId, additionId, addition.Status, addition.NewAttendees.Count);

            // Verify the addition is still pending
            if (addition.Status != RegistrationAdditionStatus.Pending)
            {
                _logger.LogWarning(
                    "[AddOnlyAttendees] [Webhook-Addition-WARN] Addition not in Pending status - CorrelationId: {CorrelationId}, AdditionId: {AdditionId}, CurrentStatus: {Status}",
                    correlationId, additionId, addition.Status);
                return;
            }

            // Load the registration (with tracking)
            var registration = await _registrationRepository.GetByIdAsync(registrationId);
            if (registration == null)
            {
                _logger.LogError(
                    "[AddOnlyAttendees] [Webhook-Addition-ERROR] Registration not found - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}",
                    correlationId, registrationId);
                return;
            }

            _logger.LogInformation(
                "[AddOnlyAttendees] [Webhook-Addition-4] Registration loaded - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, CurrentAttendees: {Count}, PaymentStatus: {PaymentStatus}",
                correlationId, registrationId, registration.Attendees.Count, registration.PaymentStatus);

            // Complete payment on the addition
            var paymentIntentId = session.PaymentIntentId ?? session.Id;
            var completePaymentResult = addition.CompletePayment(paymentIntentId);
            if (completePaymentResult.IsFailure)
            {
                _logger.LogError(
                    "[AddOnlyAttendees] [Webhook-Addition-ERROR] CompletePayment failed - CorrelationId: {CorrelationId}, AdditionId: {AdditionId}, Error: {Error}",
                    correlationId, additionId, completePaymentResult.Error);
                return;
            }

            _logger.LogInformation(
                "[AddOnlyAttendees] [Webhook-Addition-5] Payment completed on addition - CorrelationId: {CorrelationId}, AdditionId: {AdditionId}, PaymentIntentId: {PaymentIntentId}",
                correlationId, additionId, paymentIntentId);

            // Get the event to determine max attendees per registration
            var @event = await _eventRepository.GetByIdAsync(eventId);
            var maxAttendeesPerRegistration = @event?.MaxAttendeesPerRegistration ?? 10;

            // Create RegistrationPayment record for audit trail (needed for AddAttendees)
            var paymentResult = RegistrationPayment.CreateAddition(
                registrationId,
                paymentIntentId,
                addition.AdditionalAmount,
                additionId,
                PaymentStatus.Completed);

            if (paymentResult.IsFailure)
            {
                _logger.LogError(
                    "[AddOnlyAttendees] [Webhook-Addition-ERROR] Failed to create payment record - CorrelationId: {CorrelationId}, Error: {Error}",
                    correlationId, paymentResult.Error);
                addition.MarkAsFailed();
                _additionRepository.Update(addition);
                await _unitOfWork.CommitAsync();
                return;
            }

            var payment = paymentResult.Value;
            await _paymentRepository.AddAsync(payment);

            _logger.LogInformation(
                "[AddOnlyAttendees] [Webhook-Addition-6] Payment record created - CorrelationId: {CorrelationId}, PaymentId: {PaymentId}, Amount: {Amount}",
                correlationId, payment.Id, addition.AdditionalAmount.Amount);

            // Calculate new total price (previous total + additional amount)
            var previousTotal = registration.TotalPrice?.Amount ?? 0m;
            var newTotalAmount = previousTotal + addition.AdditionalAmount.Amount;
            var newTotalPriceResult = Domain.Shared.ValueObjects.Money.Create(newTotalAmount, addition.AdditionalAmount.Currency);

            if (newTotalPriceResult.IsFailure)
            {
                _logger.LogError(
                    "[AddOnlyAttendees] [Webhook-Addition-ERROR] Failed to create new total price - CorrelationId: {CorrelationId}, Error: {Error}",
                    correlationId, newTotalPriceResult.Error);
                addition.MarkAsFailed();
                _additionRepository.Update(addition);
                await _unitOfWork.CommitAsync();
                return;
            }

            // Merge attendees into the registration
            var addAttendeesResult = registration.AddAttendees(
                addition.NewAttendees,
                newTotalPriceResult.Value,
                payment,
                additionId,
                maxAttendeesPerRegistration);

            if (addAttendeesResult.IsFailure)
            {
                _logger.LogError(
                    "[AddOnlyAttendees] [Webhook-Addition-ERROR] AddAttendees failed - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, Error: {Error}",
                    correlationId, registrationId, addAttendeesResult.Error);
                // Mark addition as failed
                addition.MarkAsFailed();
                _additionRepository.Update(addition);
                await _unitOfWork.CommitAsync();
                return;
            }

            _logger.LogInformation(
                "[AddOnlyAttendees] [Webhook-Addition-7] Attendees merged - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, NewAttendeeCount: {NewCount}, TotalAttendees: {TotalCount}",
                correlationId, registrationId, addition.NewAttendees.Count, registration.Attendees.Count);

            // Phase 6A.X FIX: Recalculate revenue breakdown for cumulative total
            // Bug: Breakdown columns (StripeFeeAmount, PlatformCommissionAmount, OrganizerPayoutAmount)
            // were not updated after add-attendees, causing incorrect payout display on Attendees page
            try
            {
                var eventForBreakdown = await _eventRepository.GetByIdAsync(eventId);
                if (eventForBreakdown?.Location != null)
                {
                    _logger.LogInformation(
                        "[AddOnlyAttendees] [Webhook-Addition-7b] Recalculating revenue breakdown for cumulative total - CorrelationId: {CorrelationId}, NewTotalPrice: {TotalPrice}",
                        correlationId, newTotalPriceResult.Value.Amount);

                    var breakdownResult = await _revenueCalculatorService.CalculateBreakdownAsync(
                        newTotalPriceResult.Value,
                        eventForBreakdown.Location,
                        CancellationToken.None);

                    if (breakdownResult.IsSuccess)
                    {
                        registration.SetRevenueBreakdown(breakdownResult.Value);
                        _logger.LogInformation(
                            "[AddOnlyAttendees] [Webhook-Addition-7c] Revenue breakdown updated - CorrelationId: {CorrelationId}, GrossRevenue: {Gross}, StripeFee: {StripeFee}, PlatformCommission: {Commission}, OrganizerPayout: {Payout}",
                            correlationId,
                            newTotalPriceResult.Value.Amount,
                            breakdownResult.Value.StripeFeeAmount.Amount,
                            breakdownResult.Value.PlatformCommission.Amount,
                            breakdownResult.Value.OrganizerPayout.Amount);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "[AddOnlyAttendees] [Webhook-Addition-WARN] Revenue breakdown calculation failed - CorrelationId: {CorrelationId}, Error: {Error}",
                            correlationId, breakdownResult.Error);
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "[AddOnlyAttendees] [Webhook-Addition-WARN] Event or location not found for breakdown calculation - CorrelationId: {CorrelationId}, EventId: {EventId}",
                        correlationId, eventId);
                }
            }
            catch (Exception breakdownEx)
            {
                // Don't fail the whole operation if breakdown calculation fails
                _logger.LogError(breakdownEx,
                    "[AddOnlyAttendees] [Webhook-Addition-ERROR] Exception during revenue breakdown calculation - CorrelationId: {CorrelationId}",
                    correlationId);
            }

            // Mark addition as merged
            var mergeResult = addition.MarkAsMerged();
            if (mergeResult.IsFailure)
            {
                _logger.LogWarning(
                    "[AddOnlyAttendees] [Webhook-Addition-WARN] MarkAsMerged failed but continuing - CorrelationId: {CorrelationId}, AdditionId: {AdditionId}, Error: {Error}",
                    correlationId, additionId, mergeResult.Error);
            }

            // Save all changes and dispatch domain events
            _additionRepository.Update(addition);
            _registrationRepository.Update(registration);

            _logger.LogInformation(
                "[AddOnlyAttendees] [Webhook-Addition-8] Before CommitAsync - CorrelationId: {CorrelationId}, AdditionId: {AdditionId}, RegistrationDomainEvents: {Count}",
                correlationId, additionId, registration.DomainEvents.Count);

            await _unitOfWork.CommitAsync();

            _logger.LogInformation(
                "[AddOnlyAttendees] [Webhook-Addition-SUCCESS] Addition completed successfully - CorrelationId: {CorrelationId}, EventId: {EventId}, RegistrationId: {RegistrationId}, AdditionId: {AdditionId}, PaymentIntentId: {PaymentIntentId}, NewAttendees: {NewAttendees}, TotalAttendees: {TotalAttendees}",
                correlationId, eventId, registrationId, additionId, paymentIntentId, addition.NewAttendees.Count, registration.Attendees.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[AddOnlyAttendees] [Webhook-Addition-ERROR] Error handling addition checkout - CorrelationId: {CorrelationId}, Type: {ExceptionType}, Message: {Message}",
                correlationId, ex.GetType().FullName, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Phase 6A.81: Handles checkout.session.expired webhook to mark abandoned registrations
    /// This is part of the Three-State Registration Lifecycle to prevent payment bypass.
    /// When Stripe checkout expires (24h), mark registration as Abandoned to allow retry.
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

            // Extract metadata
            if (!session.Metadata.TryGetValue("registration_id", out var registrationIdStr) ||
                !Guid.TryParse(registrationIdStr, out var registrationId))
            {
                _logger.LogWarning(
                    "[Phase 6A.81] [Webhook-Expired-WARN] Missing registration_id in metadata - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                    correlationId, session.Id);
                return;
            }

            if (!session.Metadata.TryGetValue("event_id", out var eventIdStr) ||
                !Guid.TryParse(eventIdStr, out var eventId))
            {
                _logger.LogWarning(
                    "[Phase 6A.81] [Webhook-Expired-WARN] Missing event_id in metadata - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
                    correlationId, session.Id);
                return;
            }

            _logger.LogInformation(
                "[Phase 6A.81] [Webhook-Expired-2] Metadata extracted - CorrelationId: {CorrelationId}, EventId: {EventId}, RegistrationId: {RegistrationId}",
                correlationId, eventId, registrationId);

            // Load registration
            var registration = await _registrationRepository.GetByIdAsync(registrationId);
            if (registration == null)
            {
                _logger.LogWarning(
                    "[Phase 6A.81] [Webhook-Expired-WARN] Registration not found (might already be processed) - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}",
                    correlationId, registrationId);
                return;
            }

            _logger.LogInformation(
                "[Phase 6A.81] [Webhook-Expired-3] Registration loaded - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, CurrentStatus: {Status}, CurrentPaymentStatus: {PaymentStatus}",
                correlationId, registrationId, registration.Status, registration.PaymentStatus);

            // Verify registration belongs to the expected event (security check)
            if (registration.EventId != eventId)
            {
                _logger.LogError(
                    "[Phase 6A.81] [Webhook-Expired-ERROR] Event mismatch - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, ActualEventId: {ActualEventId}, ExpectedEventId: {ExpectedEventId}",
                    correlationId, registrationId, registration.EventId, eventId);
                return;
            }

            // Mark as abandoned (Preliminary → Abandoned)
            var abandonResult = registration.MarkAbandoned();

            if (abandonResult.IsFailure)
            {
                // This is expected if registration was already completed or abandoned
                _logger.LogWarning(
                    "[Phase 6A.81] [Webhook-Expired-INFO] MarkAbandoned failed (expected if already processed) - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, CurrentStatus: {Status}, Error: {Error}",
                    correlationId, registrationId, registration.Status, abandonResult.Error);
                return;
            }

            _logger.LogInformation(
                "[Phase 6A.81] [Webhook-Expired-4] After MarkAbandoned - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, NewStatus: {Status}, NewPaymentStatus: {PaymentStatus}, Transition: Preliminary→Abandoned",
                correlationId, registrationId, registration.Status, registration.PaymentStatus);

            // Save changes
            _registrationRepository.Update(registration);
            await _unitOfWork.CommitAsync();

            _logger.LogInformation(
                "[Phase 6A.81] [Webhook-Expired-SUCCESS] Checkout session expired, registration marked as Abandoned - CorrelationId: {CorrelationId}, EventId: {EventId}, RegistrationId: {RegistrationId}, AbandonedAt: {AbandonedAt}",
                correlationId, eventId, registrationId, registration.AbandonedAt?.ToString("o") ?? "null");
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
    /// Phase 6A.91: Handles charge.refunded webhook to complete refund workflow.
    /// Transitions registration from RefundRequested to Refunded state.
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

            // Phase 6A.X FIX: Try to find registration via metadata first, then fallback to PaymentIntentId
            Registration? registration = null;

            // Method 1: Extract registration_id from refund metadata (we store it when creating refund)
            if (latestRefund.Metadata != null &&
                latestRefund.Metadata.TryGetValue("registration_id", out var registrationIdStr) &&
                Guid.TryParse(registrationIdStr, out var registrationId))
            {
                _logger.LogInformation(
                    "[Phase 6A.91] [Webhook-Refund-3a] Metadata lookup - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}",
                    correlationId, registrationId);

                registration = await _registrationRepository.GetByIdAsync(registrationId);
            }

            // Method 2 (FALLBACK): Use PaymentIntentId from charge if metadata is missing
            if (registration == null && !string.IsNullOrEmpty(charge.PaymentIntentId))
            {
                _logger.LogInformation(
                    "[Phase 6A.91] [Webhook-Refund-3b] Fallback to PaymentIntentId lookup - CorrelationId: {CorrelationId}, PaymentIntentId: {PaymentIntentId}",
                    correlationId, charge.PaymentIntentId);

                registration = await _registrationRepository.GetByPaymentIntentIdAsync(charge.PaymentIntentId);
            }

            // If still not found, we cannot process the refund
            if (registration == null)
            {
                _logger.LogWarning(
                    "[Phase 6A.91] [Webhook-Refund-WARN] Registration not found by metadata or PaymentIntentId - CorrelationId: {CorrelationId}, ChargeId: {ChargeId}, PaymentIntentId: {PaymentIntentId}",
                    correlationId, charge.Id, charge.PaymentIntentId);
                return;
            }

            _logger.LogInformation(
                "[Phase 6A.91] [Webhook-Refund-4] Registration loaded - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, CurrentStatus: {Status}, CurrentPaymentStatus: {PaymentStatus}",
                correlationId, registration.Id, registration.Status, registration.PaymentStatus);

            // Complete refund (RefundRequested → Refunded)
            var completeRefundResult = registration.CompleteRefund(latestRefund.Id);

            if (completeRefundResult.IsFailure)
            {
                // This may be expected if refund was already processed (idempotency)
                _logger.LogWarning(
                    "[Phase 6A.91] [Webhook-Refund-INFO] CompleteRefund failed (may be already processed) - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, CurrentStatus: {Status}, Error: {Error}",
                    correlationId, registration.Id, registration.Status, completeRefundResult.Error);
                return;
            }

            _logger.LogInformation(
                "[Phase 6A.91] [Webhook-Refund-5] After CompleteRefund - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, NewStatus: {Status}, NewPaymentStatus: {PaymentStatus}, StripeRefundId: {RefundId}, Transition: RefundRequested→Refunded",
                correlationId, registration.Id, registration.Status, registration.PaymentStatus, latestRefund.Id);

            // Phase 6A.92 FIX: Log domain events to diagnose email dispatch issue
            _logger.LogInformation(
                "[Phase 6A.92] [Webhook-Refund-6] Domain events after CompleteRefund - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, DomainEvents.Count: {Count}, EventTypes: [{EventTypes}]",
                correlationId, registration.Id, registration.DomainEvents.Count, string.Join(", ", registration.DomainEvents.Select(e => e.GetType().Name)));

            // Save changes and dispatch RefundCompletedEvent
            _registrationRepository.Update(registration);

            _logger.LogInformation(
                "[Phase 6A.92] [Webhook-Refund-7] Before CommitAsync - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, DomainEvents.Count: {Count}",
                correlationId, registration.Id, registration.DomainEvents.Count);

            await _unitOfWork.CommitAsync();

            _logger.LogInformation(
                "[Phase 6A.92] [Webhook-Refund-8] After CommitAsync - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, DomainEvents.Count: {Count} (should be cleared after dispatch)",
                correlationId, registration.Id, registration.DomainEvents.Count);

            _logger.LogInformation(
                "[Phase 6A.91] [Webhook-Refund-SUCCESS] Refund completed successfully - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, StripeRefundId: {RefundId}, RefundCompletedAt: {RefundCompletedAt}",
                correlationId, registration.Id, latestRefund.Id, registration.RefundCompletedAt?.ToString("o") ?? "null");
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
