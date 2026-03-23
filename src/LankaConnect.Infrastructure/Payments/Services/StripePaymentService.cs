using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Payments;
using LankaConnect.Infrastructure.Payments.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace LankaConnect.Infrastructure.Payments.Services;

/// <summary>
/// Session 23 (Phase 2B): Stripe payment service implementation for event ticket purchases
/// Reuses existing Stripe infrastructure from Phase 6A.4
/// </summary>
public class StripePaymentService : IStripePaymentService
{
    private readonly IStripeClient _stripeClient;
    private readonly IStripeCustomerRepository _customerRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly StripeOptions _stripeOptions;
    private readonly ILogger<StripePaymentService> _logger;

    public StripePaymentService(
        IStripeClient stripeClient,
        IStripeCustomerRepository customerRepository,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IOptions<StripeOptions> stripeOptions,
        ILogger<StripePaymentService> logger)
    {
        _stripeClient = stripeClient;
        _customerRepository = customerRepository;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _stripeOptions = stripeOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Session 23: Creates a Stripe Checkout session for event ticket purchase
    /// Returns the checkout session URL for frontend redirect
    /// </summary>
    public async Task<Result<string>> CreateEventCheckoutSessionAsync(
        CreateEventCheckoutSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Creating event checkout session for Event {EventId}, Registration {RegistrationId}",
                request.EventId,
                request.RegistrationId);

            // Phase 6A.44: Handle both authenticated and anonymous registrations
            string? stripeCustomerId = null;
            bool isAnonymous = request.Metadata?.ContainsKey("anonymous") == true;

            if (!isAnonymous)
            {
                // Get or create Stripe customer for authenticated user
                if (request.Metadata == null || !request.Metadata.TryGetValue("user_id", out var userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                {
                    return Result<string>.Failure("Invalid or missing user_id in request metadata");
                }

                stripeCustomerId = await GetOrCreateStripeCustomerAsync(userId, cancellationToken);

                if (stripeCustomerId == null)
                {
                    return Result<string>.Failure("Failed to create or retrieve Stripe customer");
                }
            }
            else
            {
                // Phase 6A.44: For anonymous users, optionally provide email for receipt
                // Stripe will create a guest checkout without requiring a customer record
                _logger.LogInformation("Creating anonymous checkout session for Event {EventId}", request.EventId);
            }

            // Create checkout session for one-time payment (not subscription)
            var sessionService = new SessionService(_stripeClient);
            var sessionOptions = new SessionCreateOptions
            {
                Customer = stripeCustomerId, // null for anonymous users
                PaymentMethodTypes = new List<string> { "card" },
                Mode = "payment",  // One-time payment (not subscription)
                // Phase 6A.44: For anonymous users, provide email for receipt
                CustomerEmail = isAnonymous && request.Metadata?.TryGetValue("email", out var email) == true ? email : null,
                // C1 Guard: Support multi-line-item checkout (combined ticket + donation).
                // Null/empty LineItems = legacy single-item behavior (backward compatible).
                LineItems = request.LineItems?.Count > 0
                    ? request.LineItems.Select(li => new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = li.Currency.ToLower(),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = li.Name,
                                Description = li.Description,
                                Metadata = new Dictionary<string, string>
                                {
                                    ["event_id"] = request.EventId.ToString(),
                                    ["registration_id"] = request.RegistrationId.ToString()
                                }
                            },
                            UnitAmount = ConvertToStripeAmount(li.Amount, li.Currency)
                        },
                        Quantity = li.Quantity
                    }).ToList()
                    : new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                Currency = request.Currency.ToLower(),
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = $"Event Registration: {request.EventTitle}",
                                    Description = $"Registration for {request.EventTitle}",
                                    Metadata = new Dictionary<string, string>
                                    {
                                        ["event_id"] = request.EventId.ToString(),
                                        ["registration_id"] = request.RegistrationId.ToString()
                                    }
                                },
                                UnitAmount = ConvertToStripeAmount(request.Amount, request.Currency)
                            },
                            Quantity = 1
                        }
                    },
                // Phase 6A.44: Append registrationId to success URL for anonymous users
                SuccessUrl = AppendRegistrationIdToUrl(request.SuccessUrl, request.RegistrationId),
                CancelUrl = request.CancelUrl,
                Metadata = request.Metadata ?? new Dictionary<string, string>(),
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    Metadata = request.Metadata ?? new Dictionary<string, string>()
                }
            };

            var session = await sessionService.CreateAsync(sessionOptions, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Created checkout session {SessionId} for Event {EventId}, Registration {RegistrationId}",
                session.Id,
                request.EventId,
                request.RegistrationId);

            // Return the checkout session URL (not ID) for frontend redirect
            return Result<string>.Success(session.Url);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating event checkout session for Event {EventId}", request.EventId);
            return Result<string>.Failure($"Payment processing error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating event checkout session for Event {EventId}", request.EventId);
            return Result<string>.Failure("Failed to create payment session");
        }
    }

    /// <summary>
    /// Phase 6A.81 Part 3: Retrieves the checkout URL from an existing Stripe Checkout Session.
    /// Used to display payment link in UI and email without creating duplicate sessions.
    /// Architect decision: Retrieve from Stripe at query time (not store in DB) for security.
    /// User concern: Payment link should be invalid after payment completes (Stripe handles this automatically).
    /// </summary>
    public async Task<Result<string>> GetCheckoutSessionUrlAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[Phase 6A.81-Part3] Retrieving Stripe checkout URL - SessionId={SessionId}",
                sessionId);

            if (string.IsNullOrWhiteSpace(sessionId))
            {
                _logger.LogWarning(
                    "[Phase 6A.81-Part3] Cannot retrieve checkout URL: SessionId is empty");
                return Result<string>.Failure("Session ID cannot be empty");
            }

            // Phase 6A.81 Part 4 FIX: Handle legacy data where full URL was stored instead of session ID
            // Bug origin: CreateEventCheckoutSessionAsync returned session.Url (full URL) instead of session.Id
            // This URL was then stored in StripeCheckoutSessionId field (misnamed)
            // Fix: Detect if input is already a checkout URL and return it directly
            if (sessionId.StartsWith("https://checkout.stripe.com", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation(
                    "[Phase 6A.81-Part4] Detected legacy checkout URL in StripeCheckoutSessionId field - returning directly. " +
                    "UrlLength={UrlLength}",
                    sessionId.Length);
                return Result<string>.Success(sessionId);
            }

            var sessionService = new SessionService(_stripeClient);
            var session = await sessionService.GetAsync(sessionId, null, null, cancellationToken);

            if (session == null)
            {
                _logger.LogError(
                    "[Phase 6A.81-Part3] Stripe session not found - SessionId={SessionId}",
                    sessionId);
                return Result<string>.Failure("Checkout session not found");
            }

            // Check session status - Stripe auto-invalidates URLs after 24h or successful payment
            if (session.Status == "expired")
            {
                _logger.LogWarning(
                    "[Phase 6A.81-Part3] Stripe session expired - SessionId={SessionId}, ExpiresAt={ExpiresAt}",
                    sessionId, session.ExpiresAt);
                return Result<string>.Failure("Checkout session has expired (24 hours)");
            }

            if (session.Status == "complete")
            {
                _logger.LogInformation(
                    "[Phase 6A.81-Part3] Stripe session already completed - SessionId={SessionId}",
                    sessionId);
                return Result<string>.Failure("Checkout session already completed. Payment link cannot be reused.");
            }

            if (string.IsNullOrWhiteSpace(session.Url))
            {
                _logger.LogError(
                    "[Phase 6A.81-Part3] Stripe session has no URL - SessionId={SessionId}, Status={Status}",
                    sessionId, session.Status);
                return Result<string>.Failure("Checkout session URL not available");
            }

            _logger.LogInformation(
                "[Phase 6A.81-Part3] Checkout URL retrieved successfully - SessionId={SessionId}, Status={Status}, UrlLength={UrlLength}",
                sessionId, session.Status, session.Url.Length);

            return Result<string>.Success(session.Url);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex,
                "[Phase 6A.81-Part3] Stripe error retrieving checkout URL - SessionId={SessionId}, StripeCode={Code}, Error={Error}",
                sessionId, ex.StripeError?.Code, ex.Message);
            return Result<string>.Failure($"Stripe error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 6A.81-Part3] Unexpected error retrieving checkout URL - SessionId={SessionId}",
                sessionId);
            return Result<string>.Failure("Failed to retrieve checkout URL");
        }
    }

    /// <summary>
    /// Phase 6A.91: Creates a Stripe refund for a completed payment.
    /// This is called when a user requests a refund for a paid event registration.
    ///
    /// Stripe Refund Process:
    /// 1. API call is synchronous - refund object returned immediately
    /// 2. Status is usually "succeeded" for immediate refunds
    /// 3. charge.refunded webhook will be sent for confirmation
    /// 4. Customer sees refund on statement in 5-10 business days
    ///
    /// Idempotency: Uses RegistrationId as idempotency key to prevent duplicate refunds.
    /// </summary>
    public async Task<Result<StripeRefundResult>> CreateRefundAsync(
        CreateRefundRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Phase 6A.94: Enhanced pre-API-call logging
        _logger.LogInformation(
            "[Phase 6A.94] CreateRefundAsync START - PaymentIntentId={PaymentIntentId}, RegistrationId={RegistrationId}, " +
            "AmountInCents={AmountInCents}, Reason={Reason}, MetadataKeys={MetadataKeys}",
            request.PaymentIntentId ?? "NULL",
            request.RegistrationId,
            request.AmountInCents,
            request.Reason ?? "NULL",
            request.Metadata != null ? string.Join(",", request.Metadata.Keys) : "NONE");

        try
        {
            if (string.IsNullOrWhiteSpace(request.PaymentIntentId))
            {
                _logger.LogWarning(
                    "[Phase 6A.94] VALIDATION FAILED - PaymentIntentId is empty/null, RegistrationId={RegistrationId}",
                    request.RegistrationId);
                return Result<StripeRefundResult>.Failure("Payment intent ID is required for refund");
            }

            var refundService = new RefundService(_stripeClient);

            // Phase 6A.95: Map internal reason to valid Stripe reason
            // Stripe only accepts: "duplicate", "fraudulent", or "requested_by_customer"
            // Store original reason in metadata for our records
            var stripeReason = MapToStripeReason(request.Reason);

            _logger.LogInformation(
                "[Phase 6A.95] Mapping refund reason - OriginalReason={OriginalReason}, StripeReason={StripeReason}",
                request.Reason ?? "NULL", stripeReason);

            // Build refund options
            var refundOptions = new RefundCreateOptions
            {
                PaymentIntent = request.PaymentIntentId,
                Reason = stripeReason,
                Metadata = request.Metadata ?? new Dictionary<string, string>()
            };

            // Add registration ID and original reason to metadata for tracking
            refundOptions.Metadata["registration_id"] = request.RegistrationId.ToString();
            refundOptions.Metadata["refund_source"] = "lankaconnect";
            refundOptions.Metadata["original_reason"] = request.Reason ?? "not_specified";

            // If partial refund amount specified, include it
            if (request.AmountInCents.HasValue && request.AmountInCents.Value > 0)
            {
                refundOptions.Amount = request.AmountInCents.Value;
            }

            // Phase 6A.135: Use PaymentIntentId as idempotency key to prevent duplicate refunds.
            // Previously used RegistrationId which caused ALL add-on refunds (RegistrationId=Guid.Empty)
            // to share the same idempotency key, silently deduplicating at the Stripe API level.
            var requestOptions = new RequestOptions
            {
                IdempotencyKey = $"refund_{request.PaymentIntentId}"
            };

            // Phase 6A.94: Log just before Stripe API call
            _logger.LogInformation(
                "[Phase 6A.94] CALLING Stripe RefundService.CreateAsync - PaymentIntentId={PaymentIntentId}, " +
                "IdempotencyKey={IdempotencyKey}, Amount={Amount}",
                request.PaymentIntentId, requestOptions.IdempotencyKey, refundOptions.Amount);

            // Create the refund
            var refund = await refundService.CreateAsync(refundOptions, requestOptions, cancellationToken);

            stopwatch.Stop();

            // Phase 6A.94: Enhanced success logging with all response details
            _logger.LogInformation(
                "[Phase 6A.94] Stripe API SUCCESS - RefundId={RefundId}, Status={Status}, AmountRefunded={Amount}, " +
                "Currency={Currency}, PaymentIntentId={PaymentIntentId}, Created={Created}, Duration={ElapsedMs}ms",
                refund.Id, refund.Status, refund.Amount, refund.Currency,
                request.PaymentIntentId, refund.Created, stopwatch.ElapsedMilliseconds);

            var result = new StripeRefundResult
            {
                RefundId = refund.Id,
                Status = refund.Status,
                AmountRefunded = refund.Amount,
                Currency = refund.Currency,
                CreatedAt = refund.Created
            };

            return Result<StripeRefundResult>.Success(result);
        }
        catch (StripeException ex)
        {
            stopwatch.Stop();

            // Phase 6A.94: Enhanced Stripe error logging with full error details
            _logger.LogError(ex,
                "[Phase 6A.94] STRIPE API ERROR - PaymentIntentId={PaymentIntentId}, RegistrationId={RegistrationId}, " +
                "StripeErrorCode={Code}, StripeErrorType={Type}, StripeErrorParam={Param}, " +
                "StripeErrorDeclineCode={DeclineCode}, HttpStatus={HttpStatus}, Message={Message}, Duration={ElapsedMs}ms",
                request.PaymentIntentId,
                request.RegistrationId,
                ex.StripeError?.Code ?? "NULL",
                ex.StripeError?.Type ?? "NULL",
                ex.StripeError?.Param ?? "NULL",
                ex.StripeError?.DeclineCode ?? "NULL",
                ex.HttpStatusCode,
                ex.Message,
                stopwatch.ElapsedMilliseconds);

            // Handle specific error cases with clear messaging
            if (ex.StripeError?.Code == "charge_already_refunded")
            {
                _logger.LogWarning(
                    "[Phase 6A.94] Charge already refunded - PaymentIntentId={PaymentIntentId}. This is expected if refund was previously processed.",
                    request.PaymentIntentId);
                return Result<StripeRefundResult>.Failure("This payment has already been refunded.");
            }

            if (ex.StripeError?.Code == "insufficient_funds")
            {
                _logger.LogWarning(
                    "[Phase 6A.94] Insufficient Stripe balance for refund - PaymentIntentId={PaymentIntentId}",
                    request.PaymentIntentId);
                return Result<StripeRefundResult>.Failure(
                    "Refund cannot be processed due to insufficient Stripe balance. Please contact support.");
            }

            if (ex.StripeError?.Code == "charge_disputed")
            {
                _logger.LogWarning(
                    "[Phase 6A.94] Charge is disputed, cannot refund - PaymentIntentId={PaymentIntentId}",
                    request.PaymentIntentId);
                return Result<StripeRefundResult>.Failure(
                    "Cannot refund a disputed charge. Please contact support.");
            }

            return Result<StripeRefundResult>.Failure($"Stripe refund failed: {ex.StripeError?.Code ?? "unknown"} - {ex.Message}");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "[Phase 6A.94] UNEXPECTED ERROR in CreateRefundAsync - PaymentIntentId={PaymentIntentId}, " +
                "RegistrationId={RegistrationId}, ExceptionType={ExceptionType}, Duration={ElapsedMs}ms",
                request.PaymentIntentId,
                request.RegistrationId,
                ex.GetType().Name,
                stopwatch.ElapsedMilliseconds);

            return Result<StripeRefundResult>.Failure($"Failed to process refund request: {ex.Message}");
        }
    }

    /// <summary>
    /// Phase 6A.95: Maps internal refund reasons to valid Stripe refund reasons.
    /// Stripe only accepts: "duplicate", "fraudulent", or "requested_by_customer".
    /// </summary>
    private static string MapToStripeReason(string? internalReason)
    {
        // Stripe API only accepts these three values for refund reason
        // See: https://docs.stripe.com/api/refunds/create#create_refund-reason
        return internalReason?.ToLowerInvariant() switch
        {
            "duplicate" => "duplicate",
            "fraudulent" => "fraudulent",
            "requested_by_customer" => "requested_by_customer",
            // Map all other reasons to "requested_by_customer" as the default
            // This covers: event_cancelled, user_cancellation, organizer_refund, etc.
            _ => "requested_by_customer"
        };
    }

    /// <summary>
    /// Gets or creates a Stripe customer for the given user
    /// Reuses existing customer management logic from Phase 6A.4
    /// </summary>
    private async Task<string?> GetOrCreateStripeCustomerAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            // Check if customer already exists
            var existingCustomerId = await _customerRepository.GetStripeCustomerIdByUserIdAsync(userId, cancellationToken);
            if (!string.IsNullOrEmpty(existingCustomerId))
            {
                _logger.LogInformation("Found existing Stripe customer {CustomerId} for user {UserId}", existingCustomerId, userId);
                return existingCustomerId;
            }

            // Get user details to create new customer
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found when creating Stripe customer", userId);
                return null;
            }

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

            var customer = await customerService.CreateAsync(customerOptions, cancellationToken: cancellationToken);

            // Save to database
            await _customerRepository.SaveStripeCustomerAsync(
                userId,
                customer.Id,
                user.Email.Value,
                user.FullName,
                customer.Created,
                cancellationToken);

            _logger.LogInformation("Created new Stripe customer {CustomerId} for user {UserId}", customer.Id, userId);
            return customer.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting or creating Stripe customer for user {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    /// Converts decimal amount to Stripe's integer format (cents for USD, paisa for LKR)
    /// Stripe requires amounts in the smallest currency unit
    /// </summary>
    private long ConvertToStripeAmount(decimal amount, string currency)
    {
        // For zero-decimal currencies (e.g., JPY), don't multiply by 100
        // For now, we only support USD and LKR which are both 2-decimal currencies
        // Phase 6A.136: Use Math.Round to prevent fractional cent truncation.
        // (long) cast truncates: (long)(19.994999 * 100) = 1999 instead of 2000.
        return (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Add-Only Attendees Feature: Creates a Stripe Checkout session for adding attendees to an existing registration.
    /// Returns both the session ID (for tracking) and checkout URL (for redirect).
    /// </summary>
    public async Task<Result<AdditionCheckoutResult>> CreateAdditionCheckoutSessionAsync(
        CreateAdditionCheckoutSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[AddOnlyAttendees] Creating addition checkout session - RegistrationId={RegistrationId}, AdditionId={AdditionId}, Amount={Amount} {Currency}",
                request.RegistrationId,
                request.RegistrationAdditionId,
                request.Amount,
                request.Currency);

            // Handle authenticated vs anonymous users
            string? stripeCustomerId = null;
            bool isAnonymous = !request.UserId.HasValue;

            if (!isAnonymous)
            {
                stripeCustomerId = await GetOrCreateStripeCustomerAsync(request.UserId!.Value, cancellationToken);
                if (stripeCustomerId == null)
                {
                    _logger.LogWarning(
                        "[AddOnlyAttendees] Failed to get/create Stripe customer for user {UserId}",
                        request.UserId);
                    // Continue without customer - will create guest checkout
                }
            }

            // Create checkout session
            var sessionService = new SessionService(_stripeClient);
            var sessionOptions = new SessionCreateOptions
            {
                Customer = stripeCustomerId,
                PaymentMethodTypes = new List<string> { "card" },
                Mode = "payment",
                CustomerEmail = isAnonymous && !string.IsNullOrEmpty(request.ContactEmail)
                    ? request.ContactEmail
                    : null,
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = request.Currency.ToLower(),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Additional Attendees: {request.EventTitle}",
                                Description = $"Add {request.NewAttendeesCount} attendee(s) to your registration",
                                Metadata = new Dictionary<string, string>
                                {
                                    ["event_id"] = request.EventId.ToString(),
                                    ["registration_id"] = request.RegistrationId.ToString(),
                                    ["registration_addition_id"] = request.RegistrationAdditionId.ToString(),
                                    ["payment_type"] = "addition" // Key differentiator for webhook
                                }
                            },
                            UnitAmount = ConvertToStripeAmount(request.Amount, request.Currency)
                        },
                        Quantity = 1
                    }
                },
                SuccessUrl = AppendRegistrationIdToUrl(request.SuccessUrl, request.RegistrationId),
                CancelUrl = request.CancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    ["event_id"] = request.EventId.ToString(),
                    ["registration_id"] = request.RegistrationId.ToString(),
                    ["registration_addition_id"] = request.RegistrationAdditionId.ToString(),
                    ["payment_type"] = "addition" // Key differentiator for webhook
                },
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        ["event_id"] = request.EventId.ToString(),
                        ["registration_id"] = request.RegistrationId.ToString(),
                        ["registration_addition_id"] = request.RegistrationAdditionId.ToString(),
                        ["payment_type"] = "addition" // Key differentiator for webhook
                    }
                },
                // Session expires after 24 hours
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            // Add any additional metadata from request
            if (request.Metadata != null)
            {
                foreach (var kvp in request.Metadata)
                {
                    sessionOptions.Metadata[kvp.Key] = kvp.Value;
                    sessionOptions.PaymentIntentData.Metadata[kvp.Key] = kvp.Value;
                }
            }

            var session = await sessionService.CreateAsync(sessionOptions, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "[AddOnlyAttendees] Created checkout session - SessionId={SessionId}, RegistrationId={RegistrationId}, AdditionId={AdditionId}, ExpiresAt={ExpiresAt}",
                session.Id,
                request.RegistrationId,
                request.RegistrationAdditionId,
                session.ExpiresAt);

            return Result<AdditionCheckoutResult>.Success(new AdditionCheckoutResult
            {
                SessionId = session.Id,
                CheckoutUrl = session.Url,
                ExpiresAt = session.ExpiresAt
            });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex,
                "[AddOnlyAttendees] Stripe error creating addition checkout - RegistrationId={RegistrationId}, AdditionId={AdditionId}, Error={Error}",
                request.RegistrationId,
                request.RegistrationAdditionId,
                ex.Message);
            return Result<AdditionCheckoutResult>.Failure($"Payment processing error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[AddOnlyAttendees] Error creating addition checkout - RegistrationId={RegistrationId}, AdditionId={AdditionId}",
                request.RegistrationId,
                request.RegistrationAdditionId);
            return Result<AdditionCheckoutResult>.Failure("Failed to create payment session");
        }
    }

    /// <summary>
    /// Donation Feature: Creates a Stripe Checkout session for a standalone donation.
    /// Follows the CreateAdditionCheckoutSessionAsync pattern.
    /// </summary>
    public async Task<Result<DonationCheckoutResult>> CreateDonationCheckoutSessionAsync(
        CreateDonationCheckoutSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[Donation] Creating donation checkout session - EventId={EventId}, DonationId={DonationId}, Amount={Amount} {Currency}",
                request.EventId,
                request.DonationId,
                request.Amount,
                request.Currency);

            var sessionService = new SessionService(_stripeClient);
            var sessionOptions = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                Mode = "payment",
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = request.Currency.ToLower(),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Donation: {request.EventTitle}",
                                Description = "Voluntary donation to event",
                                Metadata = new Dictionary<string, string>
                                {
                                    ["event_id"] = request.EventId.ToString(),
                                    ["donation_id"] = request.DonationId.ToString(),
                                    ["payment_type"] = "donation"
                                }
                            },
                            UnitAmount = ConvertToStripeAmount(request.Amount, request.Currency)
                        },
                        Quantity = 1
                    }
                },
                SuccessUrl = request.SuccessUrl,
                CancelUrl = request.CancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    ["event_id"] = request.EventId.ToString(),
                    ["donation_id"] = request.DonationId.ToString(),
                    ["payment_type"] = "donation"
                },
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        ["event_id"] = request.EventId.ToString(),
                        ["donation_id"] = request.DonationId.ToString(),
                        ["payment_type"] = "donation"
                    }
                },
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            // Add any additional metadata from request
            if (request.Metadata != null)
            {
                foreach (var kvp in request.Metadata)
                {
                    sessionOptions.Metadata[kvp.Key] = kvp.Value;
                    sessionOptions.PaymentIntentData.Metadata[kvp.Key] = kvp.Value;
                }
            }

            var session = await sessionService.CreateAsync(sessionOptions, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "[Donation] Created checkout session - SessionId={SessionId}, DonationId={DonationId}, ExpiresAt={ExpiresAt}",
                session.Id,
                request.DonationId,
                session.ExpiresAt);

            return Result<DonationCheckoutResult>.Success(new DonationCheckoutResult
            {
                SessionId = session.Id,
                CheckoutUrl = session.Url,
                ExpiresAt = session.ExpiresAt
            });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex,
                "[Donation] Stripe error creating donation checkout - DonationId={DonationId}, Error={Error}",
                request.DonationId,
                ex.Message);
            return Result<DonationCheckoutResult>.Failure($"Payment processing error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Donation] Error creating donation checkout - DonationId={DonationId}",
                request.DonationId);
            return Result<DonationCheckoutResult>.Failure("Failed to create donation payment session");
        }
    }

    /// <summary>
    /// Creates a Stripe Checkout session for an event fund collection contribution.
    /// </summary>
    public async Task<Result<CollectionCheckoutResult>> CreateCollectionCheckoutSessionAsync(
        CreateCollectionCheckoutSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[Collection] Creating collection checkout session - EventId={EventId}, CollectionId={CollectionId}, Amount={Amount} {Currency}",
                request.EventId, request.CollectionId, request.Amount, request.Currency);

            var sessionService = new SessionService(_stripeClient);
            var sessionOptions = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                Mode = "payment",
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = request.Currency.ToLower(),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Contribution: {request.EventTitle}",
                                Description = "Event fund contribution",
                                Metadata = new Dictionary<string, string>
                                {
                                    ["event_id"] = request.EventId.ToString(),
                                    ["collection_id"] = request.CollectionId.ToString(),
                                    ["payment_type"] = "collection"
                                }
                            },
                            UnitAmount = ConvertToStripeAmount(request.Amount, request.Currency)
                        },
                        Quantity = 1
                    }
                },
                SuccessUrl = request.SuccessUrl,
                CancelUrl = request.CancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    ["event_id"] = request.EventId.ToString(),
                    ["collection_id"] = request.CollectionId.ToString(),
                    ["payment_type"] = "collection"
                },
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        ["event_id"] = request.EventId.ToString(),
                        ["collection_id"] = request.CollectionId.ToString(),
                        ["payment_type"] = "collection"
                    }
                },
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            if (request.Metadata != null)
            {
                foreach (var kvp in request.Metadata)
                {
                    sessionOptions.Metadata[kvp.Key] = kvp.Value;
                    sessionOptions.PaymentIntentData.Metadata[kvp.Key] = kvp.Value;
                }
            }

            var session = await sessionService.CreateAsync(sessionOptions, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "[Collection] Created checkout session - SessionId={SessionId}, CollectionId={CollectionId}, ExpiresAt={ExpiresAt}",
                session.Id, request.CollectionId, session.ExpiresAt);

            return Result<CollectionCheckoutResult>.Success(new CollectionCheckoutResult
            {
                SessionId = session.Id,
                CheckoutUrl = session.Url,
                ExpiresAt = session.ExpiresAt
            });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex,
                "[Collection] Stripe error creating collection checkout - CollectionId={CollectionId}, Error={Error}",
                request.CollectionId, ex.Message);
            return Result<CollectionCheckoutResult>.Failure($"Payment processing error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Collection] Error creating collection checkout - CollectionId={CollectionId}",
                request.CollectionId);
            return Result<CollectionCheckoutResult>.Failure("Failed to create collection payment session");
        }
    }

    /// <summary>
    /// Creates a Stripe Checkout session for a money sponsorship.
    /// </summary>
    public async Task<Result<SponsorCheckoutResult>> CreateSponsorCheckoutSessionAsync(
        CreateSponsorCheckoutSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[Sponsor] Creating sponsor checkout session - EventId={EventId}, SponsorId={SponsorId}, Amount={Amount} {Currency}",
                request.EventId, request.SponsorId, request.Amount, request.Currency);

            var productName = string.IsNullOrWhiteSpace(request.SponsorOrganization)
                ? $"Sponsorship: {request.EventTitle}"
                : $"Sponsorship ({request.SponsorOrganization}): {request.EventTitle}";

            var sessionService = new SessionService(_stripeClient);
            var sessionOptions = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                Mode = "payment",
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = request.Currency.ToLower(),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = productName,
                                Description = "Event sponsorship contribution",
                                Metadata = new Dictionary<string, string>
                                {
                                    ["event_id"] = request.EventId.ToString(),
                                    ["sponsor_id"] = request.SponsorId.ToString(),
                                    ["payment_type"] = "sponsor"
                                }
                            },
                            UnitAmount = ConvertToStripeAmount(request.Amount, request.Currency)
                        },
                        Quantity = 1
                    }
                },
                SuccessUrl = request.SuccessUrl,
                CancelUrl = request.CancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    ["event_id"] = request.EventId.ToString(),
                    ["sponsor_id"] = request.SponsorId.ToString(),
                    ["payment_type"] = "sponsor"
                },
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        ["event_id"] = request.EventId.ToString(),
                        ["sponsor_id"] = request.SponsorId.ToString(),
                        ["payment_type"] = "sponsor"
                    }
                },
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            if (request.Metadata != null)
            {
                foreach (var kvp in request.Metadata)
                {
                    sessionOptions.Metadata[kvp.Key] = kvp.Value;
                    sessionOptions.PaymentIntentData.Metadata[kvp.Key] = kvp.Value;
                }
            }

            var session = await sessionService.CreateAsync(sessionOptions, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "[Sponsor] Created checkout session - SessionId={SessionId}, SponsorId={SponsorId}, ExpiresAt={ExpiresAt}",
                session.Id, request.SponsorId, session.ExpiresAt);

            return Result<SponsorCheckoutResult>.Success(new SponsorCheckoutResult
            {
                SessionId = session.Id,
                CheckoutUrl = session.Url,
                ExpiresAt = session.ExpiresAt
            });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex,
                "[Sponsor] Stripe error creating sponsor checkout - SponsorId={SponsorId}, Error={Error}",
                request.SponsorId, ex.Message);
            return Result<SponsorCheckoutResult>.Failure($"Payment processing error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Sponsor] Error creating sponsor checkout - SponsorId={SponsorId}",
                request.SponsorId);
            return Result<SponsorCheckoutResult>.Failure("Failed to create sponsor payment session");
        }
    }

    /// <summary>
    /// Creates a Stripe Checkout session for an add-on purchase.
    /// UnitPrice is already snapshotted on the AddOnPurchase entity (M3: price snapshot at checkout creation).
    /// </summary>
    public async Task<Result<AddOnPurchaseCheckoutResult>> CreateAddOnPurchaseCheckoutSessionAsync(
        CreateAddOnPurchaseCheckoutSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[AddOnPurchase] Creating add-on purchase checkout session - EventId={EventId}, PurchaseId={PurchaseId}, AddOn={AddOnName}, Qty={Quantity}, UnitPrice={UnitPrice}, Total={TotalAmount} {Currency}",
                request.EventId, request.AddOnPurchaseId, request.AddOnName,
                request.Quantity, request.UnitPrice, request.TotalAmount, request.Currency);

            var sessionService = new SessionService(_stripeClient);
            var sessionOptions = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                Mode = "payment",
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = request.Currency.ToLower(),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"{request.AddOnName} - {request.EventTitle}",
                                Description = $"Add-on purchase ({request.Quantity}x)",
                                Metadata = new Dictionary<string, string>
                                {
                                    ["event_id"] = request.EventId.ToString(),
                                    ["add_on_purchase_id"] = request.AddOnPurchaseId.ToString(),
                                    ["add_on_definition_id"] = request.AddOnDefinitionId.ToString(),
                                    ["payment_type"] = "add_on_purchase"
                                }
                            },
                            UnitAmount = ConvertToStripeAmount(request.UnitPrice, request.Currency)
                        },
                        Quantity = request.Quantity
                    }
                },
                SuccessUrl = request.SuccessUrl,
                CancelUrl = request.CancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    ["event_id"] = request.EventId.ToString(),
                    ["add_on_purchase_id"] = request.AddOnPurchaseId.ToString(),
                    ["add_on_definition_id"] = request.AddOnDefinitionId.ToString(),
                    ["payment_type"] = "add_on_purchase"
                },
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        ["event_id"] = request.EventId.ToString(),
                        ["add_on_purchase_id"] = request.AddOnPurchaseId.ToString(),
                        ["add_on_definition_id"] = request.AddOnDefinitionId.ToString(),
                        ["payment_type"] = "add_on_purchase"
                    }
                },
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            if (request.Metadata != null)
            {
                foreach (var kvp in request.Metadata)
                {
                    sessionOptions.Metadata[kvp.Key] = kvp.Value;
                    sessionOptions.PaymentIntentData.Metadata[kvp.Key] = kvp.Value;
                }
            }

            var session = await sessionService.CreateAsync(sessionOptions, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "[AddOnPurchase] Created checkout session - SessionId={SessionId}, PurchaseId={PurchaseId}, ExpiresAt={ExpiresAt}",
                session.Id, request.AddOnPurchaseId, session.ExpiresAt);

            return Result<AddOnPurchaseCheckoutResult>.Success(new AddOnPurchaseCheckoutResult
            {
                SessionId = session.Id,
                CheckoutUrl = session.Url,
                ExpiresAt = session.ExpiresAt
            });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex,
                "[AddOnPurchase] Stripe error creating add-on purchase checkout - PurchaseId={PurchaseId}, Error={Error}",
                request.AddOnPurchaseId, ex.Message);
            return Result<AddOnPurchaseCheckoutResult>.Failure($"Payment processing error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[AddOnPurchase] Error creating add-on purchase checkout - PurchaseId={PurchaseId}",
                request.AddOnPurchaseId);
            return Result<AddOnPurchaseCheckoutResult>.Failure("Failed to create add-on purchase payment session");
        }
    }

    /// <summary>
    /// Creates a Stripe Checkout session with multiple add-on line items (cart purchase).
    /// Each line item maps to a distinct AddOnDefinition with its own quantity and price.
    /// All paid purchases share the same checkout session ID.
    /// </summary>
    public async Task<Result<AddOnPurchaseCheckoutResult>> CreateAddOnCartCheckoutSessionAsync(
        CreateAddOnCartCheckoutSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[AddOnCart] Creating cart checkout session - EventId={EventId}, LineItemCount={LineItemCount}",
                request.EventId, request.Items.Count);

            var lineItems = request.Items.Select(item => new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = item.Currency.ToLower(),
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = $"{item.Name} - {request.EventTitle}",
                        Description = !string.IsNullOrEmpty(item.Description)
                            ? item.Description
                            : $"Add-on purchase ({item.Quantity}x)",
                        Metadata = new Dictionary<string, string>
                        {
                            ["event_id"] = request.EventId.ToString(),
                            ["add_on_purchase_id"] = item.AddOnPurchaseId.ToString(),
                            ["add_on_definition_id"] = item.AddOnDefinitionId.ToString(),
                            ["payment_type"] = "add_on_purchase"
                        }
                    },
                    UnitAmount = ConvertToStripeAmount(item.UnitPrice, item.Currency)
                },
                Quantity = item.Quantity
            }).ToList();

            var sessionService = new SessionService(_stripeClient);
            var sessionOptions = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                Mode = "payment",
                LineItems = lineItems,
                SuccessUrl = request.SuccessUrl,
                CancelUrl = request.CancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    ["event_id"] = request.EventId.ToString(),
                    ["payment_type"] = "add_on_purchase",
                    ["cart_item_count"] = request.Items.Count.ToString(),
                    ["purchase_ids"] = string.Join(",", request.Items.Select(i => i.AddOnPurchaseId))
                },
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        ["event_id"] = request.EventId.ToString(),
                        ["payment_type"] = "add_on_purchase",
                        ["cart_item_count"] = request.Items.Count.ToString(),
                        ["purchase_ids"] = string.Join(",", request.Items.Select(i => i.AddOnPurchaseId))
                    }
                },
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            if (request.Metadata != null)
            {
                foreach (var kvp in request.Metadata)
                {
                    sessionOptions.Metadata[kvp.Key] = kvp.Value;
                    sessionOptions.PaymentIntentData.Metadata[kvp.Key] = kvp.Value;
                }
            }

            var session = await sessionService.CreateAsync(sessionOptions, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "[AddOnCart] Created cart checkout session - SessionId={SessionId}, EventId={EventId}, LineItems={LineItemCount}, ExpiresAt={ExpiresAt}",
                session.Id, request.EventId, request.Items.Count, session.ExpiresAt);

            return Result<AddOnPurchaseCheckoutResult>.Success(new AddOnPurchaseCheckoutResult
            {
                SessionId = session.Id,
                CheckoutUrl = session.Url,
                ExpiresAt = session.ExpiresAt
            });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex,
                "[AddOnCart] Stripe error creating cart checkout - EventId={EventId}, Error={Error}",
                request.EventId, ex.Message);
            return Result<AddOnPurchaseCheckoutResult>.Failure($"Payment processing error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[AddOnCart] Error creating cart checkout - EventId={EventId}",
                request.EventId);
            return Result<AddOnPurchaseCheckoutResult>.Failure("Failed to create cart payment session");
        }
    }

    #region Not Implemented Methods (Cultural Intelligence - Future)

    // These methods are part of IStripePaymentService for Cultural Intelligence billing
    // They are not needed for event ticket payments, so we'll throw NotImplementedException
    // These will be implemented when we add the Cultural Intelligence subscription features

    public Task<Result> CreateSubscriptionAsync(CreateStripeSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Cultural Intelligence subscriptions not yet implemented");
    }

    public Task<Result> CreateEnterpriseSubscriptionAsync(CreateEnterpriseSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Enterprise subscriptions not yet implemented");
    }

    public Task<Result> ChargeUsageAsync(ChargeUsageRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Usage-based charging not yet implemented");
    }

    public Task<Result> CreatePartnerPayoutAsync(CreatePartnerPayoutRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Partner payouts not yet implemented");
    }

    public Task<Result> CancelSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Subscription cancellation not yet implemented");
    }

    public Task<Result> UpdateSubscriptionAsync(UpdateSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Subscription updates not yet implemented");
    }

    public Task<Result<StripeWebhookEvent>> ProcessWebhookAsync(string payload, string signature, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Webhook processing is handled by PaymentsController for now");
    }

    #endregion

    /// <summary>
    /// Phase 6A.44: Appends registrationId to success URL for anonymous user checkout
    /// This allows the payment success page to fetch registration details without userId
    /// </summary>
    private string AppendRegistrationIdToUrl(string url, Guid registrationId)
    {
        if (string.IsNullOrEmpty(url))
            return url;

        var separator = url.Contains('?') ? "&" : "?";
        return $"{url}{separator}registrationId={registrationId}";
    }
}
