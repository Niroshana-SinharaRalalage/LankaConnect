using LankaConnect.Domain.Common;

namespace LankaConnect.Products.LankaEvents.Domain.ValueObjects;

/// <summary>
/// Value object representing add-on configuration for an event.
/// Stored as JSONB on the Event aggregate.
///
/// IMPORTANT (C5 Guard): This VO uses flat primitive types only — no nested Money
/// value objects — to avoid EF Core OwnsOne(ToJson) nested entity issues.
/// </summary>
public class AddOnConfiguration : ValueObject
{
    public const int MAX_MESSAGE_LENGTH = 500;

    /// <summary>
    /// Whether add-ons are enabled for this event.
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Whether add-ons are available during the registration flow
    /// (bundled with ticket purchase as additional Stripe line items).
    /// </summary>
    public bool AvailableDuringRegistration { get; private set; }

    /// <summary>
    /// Whether add-ons are available for standalone purchase
    /// (from the event details page, independent of registration).
    /// </summary>
    public bool AvailableStandalone { get; private set; }

    /// <summary>
    /// Optional message from the organizer displayed on the add-on selection area.
    /// </summary>
    public string? AddOnMessage { get; private set; }

    // EF Core constructor
    private AddOnConfiguration()
    {
    }

    private AddOnConfiguration(
        bool isEnabled,
        bool availableDuringRegistration,
        bool availableStandalone,
        string? addOnMessage)
    {
        IsEnabled = isEnabled;
        AvailableDuringRegistration = availableDuringRegistration;
        AvailableStandalone = availableStandalone;
        AddOnMessage = addOnMessage;
    }

    /// <summary>
    /// Creates an add-on configuration with validation.
    /// </summary>
    public static Result<AddOnConfiguration> Create(
        bool isEnabled,
        bool availableDuringRegistration,
        bool availableStandalone,
        string? addOnMessage)
    {
        if (!isEnabled)
            return Result<AddOnConfiguration>.Success(Disabled());

        // Must be available in at least one context
        if (!availableDuringRegistration && !availableStandalone)
            return Result<AddOnConfiguration>.Failure(
                "Add-ons must be available in at least one context (during registration or standalone)");

        // Validate message length
        if (addOnMessage != null && addOnMessage.Length > MAX_MESSAGE_LENGTH)
            return Result<AddOnConfiguration>.Failure(
                $"Add-on message cannot exceed {MAX_MESSAGE_LENGTH} characters");

        return Result<AddOnConfiguration>.Success(new AddOnConfiguration(
            true,
            availableDuringRegistration,
            availableStandalone,
            addOnMessage?.Trim()));
    }

    /// <summary>
    /// Creates a disabled (default) add-on configuration.
    /// </summary>
    public static AddOnConfiguration Disabled()
    {
        return new AddOnConfiguration(false, false, false, null);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return IsEnabled;
        yield return AvailableDuringRegistration;
        yield return AvailableStandalone;
        yield return AddOnMessage ?? string.Empty;
    }
}
