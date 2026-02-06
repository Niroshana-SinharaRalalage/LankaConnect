using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.DomainEvents;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Communications.Entities;

/// <summary>
/// Newsletter Subscriber Aggregate Root
/// Encapsulates newsletter subscription business rules and workflow
/// Phase 6A.64: Updated to support many-to-many relationship with MetroAreas via junction table
/// </summary>
public class NewsletterSubscriber : BaseEntity, IAggregateRoot
{
    public Email Email { get; private set; } = null!;

    // Phase 6A.64: Collection of metro area IDs (replaces single MetroAreaId)
    // EF Core will map this to the newsletter_subscriber_metro_areas junction table
    private readonly List<Guid> _metroAreaIds = new();
    public IReadOnlyList<Guid> MetroAreaIds => _metroAreaIds.AsReadOnly();

    public bool ReceiveAllLocations { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsConfirmed { get; private set; }

    public string? ConfirmationToken { get; private set; }
    public DateTime? ConfirmationSentAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }

    public string? UnsubscribeToken { get; private set; }
    public DateTime? UnsubscribedAt { get; private set; }

    // IAggregateRoot implementation
    public byte[] Version { get; private set; } = Array.Empty<byte>();

    public void SetVersion(byte[] version)
    {
        Version = version;
    }

    // EF Core constructor
    private NewsletterSubscriber() { }

    /// <summary>
    /// Validates the current state of the aggregate
    /// Phase 6A.64: Updated to validate collection of metro area IDs
    /// </summary>
    public ValidationResult ValidateState()
    {
        var errors = new List<string>();

        if (Email == null)
            errors.Add("Email is required");

        if (!ReceiveAllLocations && !_metroAreaIds.Any())
            errors.Add("Must specify at least one metro area or choose to receive all locations");

        if (string.IsNullOrWhiteSpace(UnsubscribeToken))
            errors.Add("Unsubscribe token is required");

        return errors.Any()
            ? ValidationResult.Invalid(errors)
            : ValidationResult.Valid();
    }

    /// <summary>
    /// Checks if the aggregate is in a valid state
    /// </summary>
    public bool IsValid()
    {
        return ValidateState().IsValid;
    }

    /// <summary>
    /// Factory method to create a new newsletter subscription
    /// Phase 6A.64: Updated to accept collection of metro area IDs
    /// </summary>
    public static Result<NewsletterSubscriber> Create(
        Email email,
        IEnumerable<Guid> metroAreaIds,
        bool receiveAllLocations)
    {
        var metroAreaIdsList = metroAreaIds?.ToList() ?? new List<Guid>();

        // Business rule: Must have at least one metro area OR receive all locations
        if (!receiveAllLocations && !metroAreaIdsList.Any())
        {
            return Result<NewsletterSubscriber>.Failure(
                "Must specify at least one metro area or choose to receive all locations");
        }

        var subscriber = new NewsletterSubscriber
        {
            Email = email,
            ReceiveAllLocations = receiveAllLocations,
            IsActive = true,
            IsConfirmed = false,
            ConfirmationToken = GenerateToken(),
            ConfirmationSentAt = DateTime.UtcNow,
            UnsubscribeToken = GenerateToken()
        };

        // Add metro area IDs if not receiving all locations
        if (!receiveAllLocations)
        {
            subscriber._metroAreaIds.AddRange(metroAreaIdsList);
        }

        // Raise domain event
        subscriber.RaiseDomainEvent(new NewsletterSubscriptionCreatedEvent(
            subscriber.Id,
            subscriber.Email.Value,
            subscriber.MetroAreaIds.FirstOrDefault(),  // Backward compatibility: use first for event
            subscriber.ReceiveAllLocations
        ));

        return Result<NewsletterSubscriber>.Success(subscriber);
    }

    /// <summary>
    /// Confirm subscription with token
    /// </summary>
    public Result Confirm(string confirmationToken)
    {
        if (IsConfirmed)
        {
            return Result.Failure("Subscription is already confirmed");
        }

        if (string.IsNullOrWhiteSpace(ConfirmationToken))
        {
            return Result.Failure("No confirmation token available");
        }

        if (ConfirmationToken != confirmationToken)
        {
            return Result.Failure("Invalid confirmation token");
        }

        IsConfirmed = true;
        ConfirmedAt = DateTime.UtcNow;
        ConfirmationToken = null;
        MarkAsUpdated();

        RaiseDomainEvent(new NewsletterSubscriptionConfirmedEvent(
            Id,
            Email.Value,
            MetroAreaIds.FirstOrDefault()  // Backward compatibility: use first for event
        ));

        return Result.Success();
    }

    /// <summary>
    /// Resend confirmation email with new token
    /// </summary>
    public Result ResendConfirmation()
    {
        if (IsConfirmed)
        {
            return Result.Failure("Subscription is already confirmed");
        }

        ConfirmationToken = GenerateToken();
        ConfirmationSentAt = DateTime.UtcNow;
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Unsubscribe from newsletter
    /// </summary>
    public Result Unsubscribe(string unsubscribeToken)
    {
        if (string.IsNullOrWhiteSpace(UnsubscribeToken))
        {
            return Result.Failure("No unsubscribe token available");
        }

        if (UnsubscribeToken != unsubscribeToken)
        {
            return Result.Failure("Invalid unsubscribe token");
        }

        IsActive = false;
        UnsubscribedAt = DateTime.UtcNow;
        MarkAsUpdated();

        RaiseDomainEvent(new NewsletterSubscriptionCancelledEvent(
            Id,
            Email.Value,
            MetroAreaIds.FirstOrDefault()  // Backward compatibility: use first for event
        ));

        return Result.Success();
    }

    /// <summary>
    /// Generate a secure random token
    /// </summary>
    private static string GenerateToken()
    {
        return Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
    }
}
