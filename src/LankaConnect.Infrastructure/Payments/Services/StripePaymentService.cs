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
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = request.Currency.ToLower(),  // stripe expects lowercase
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
        return (long)(amount * 100);
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
