using System.Text.RegularExpressions;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain.Entities;

/// <summary>
/// Represents an organizer contact for an event.
/// Supports multiple contacts per event with primary contact designation.
/// Future: linked_user_id enables co-organizer management.
/// </summary>
public class EventOrganizerContact : LegacyBaseEntity
{
    /// <summary>
    /// The event this contact belongs to
    /// </summary>
    public Guid EventId { get; private set; }

    /// <summary>
    /// Display name of the organizer contact
    /// </summary>
    public string ContactName { get; private set; } = string.Empty;

    /// <summary>
    /// Email address (optional if phone provided)
    /// </summary>
    public string? ContactEmail { get; private set; }

    /// <summary>
    /// Phone number (optional if email provided)
    /// </summary>
    public string? ContactPhone { get; private set; }

    /// <summary>
    /// Whether this is the primary contact for the event
    /// </summary>
    public bool IsPrimary { get; private set; }

    /// <summary>
    /// Future: Link to a user account for co-organizer management
    /// </summary>
    public Guid? LinkedUserId { get; private set; }

    /// <summary>
    /// Display order for multiple contacts
    /// </summary>
    public int SortOrder { get; private set; }

    // EF Core constructor
    private EventOrganizerContact()
    {
    }

    private EventOrganizerContact(
        Guid eventId,
        string contactName,
        string? contactEmail,
        string? contactPhone,
        bool isPrimary,
        int sortOrder)
    {
        EventId = eventId;
        ContactName = contactName.Trim();
        ContactEmail = contactEmail?.Trim().ToLowerInvariant();
        ContactPhone = contactPhone?.Trim();
        IsPrimary = isPrimary;
        SortOrder = sortOrder;
    }

    /// <summary>
    /// Factory method to create a new organizer contact with validation
    /// </summary>
    public static Result<EventOrganizerContact> Create(
        Guid eventId,
        string contactName,
        string? contactEmail,
        string? contactPhone,
        bool isPrimary = false,
        int sortOrder = 0,
        Guid? linkedUserId = null)
    {
        if (eventId == Guid.Empty)
            return Result<EventOrganizerContact>.Failure("Event ID is required");

        if (string.IsNullOrWhiteSpace(contactName))
            return Result<EventOrganizerContact>.Failure("Contact name is required");

        if (contactName.Trim().Length > 200)
            return Result<EventOrganizerContact>.Failure("Contact name must be less than 200 characters");

        var hasEmail = !string.IsNullOrWhiteSpace(contactEmail);
        var hasPhone = !string.IsNullOrWhiteSpace(contactPhone);

        if (!hasEmail && !hasPhone)
            return Result<EventOrganizerContact>.Failure("At least one contact method (email or phone) is required");

        if (hasEmail && !IsValidEmail(contactEmail!))
            return Result<EventOrganizerContact>.Failure("Invalid email format");

        if (hasPhone && contactPhone!.Trim().Length > 20)
            return Result<EventOrganizerContact>.Failure("Phone number must be less than 20 characters");

        var contact = new EventOrganizerContact(
            eventId, contactName, contactEmail, contactPhone, isPrimary, sortOrder);

        // Pre-link to a registered user if provided
        if (linkedUserId.HasValue && linkedUserId.Value != Guid.Empty)
        {
            contact.LinkedUserId = linkedUserId.Value;
        }

        return Result<EventOrganizerContact>.Success(contact);
    }

    /// <summary>
    /// Update contact details
    /// </summary>
    public Result Update(string contactName, string? contactEmail, string? contactPhone)
    {
        if (string.IsNullOrWhiteSpace(contactName))
            return Result.Failure("Contact name is required");

        if (contactName.Trim().Length > 200)
            return Result.Failure("Contact name must be less than 200 characters");

        var hasEmail = !string.IsNullOrWhiteSpace(contactEmail);
        var hasPhone = !string.IsNullOrWhiteSpace(contactPhone);

        if (!hasEmail && !hasPhone)
            return Result.Failure("At least one contact method (email or phone) is required");

        if (hasEmail && !IsValidEmail(contactEmail!))
            return Result.Failure("Invalid email format");

        if (hasPhone && contactPhone!.Trim().Length > 20)
            return Result.Failure("Phone number must be less than 20 characters");

        ContactName = contactName.Trim();
        ContactEmail = hasEmail ? contactEmail!.Trim().ToLowerInvariant() : null;
        ContactPhone = hasPhone ? contactPhone!.Trim() : null;

        return Result.Success();
    }

    internal void SetAsPrimary()
    {
        IsPrimary = true;
    }

    internal void UnmarkAsPrimary()
    {
        IsPrimary = false;
    }

    internal void UpdateSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
    }

    /// <summary>
    /// Links this organizer contact to a registered user account.
    /// Once linked, the user gains co-organizer access to the event.
    /// </summary>
    internal Result LinkToUser(Guid userId)
    {
        if (userId == Guid.Empty)
            return Result.Failure("User ID is required");

        if (LinkedUserId.HasValue && LinkedUserId.Value == userId)
            return Result.Success(); // Already linked to this user, idempotent

        if (LinkedUserId.HasValue)
            return Result.Failure("This contact is already linked to a different user. Unlink first.");

        LinkedUserId = userId;
        return Result.Success();
    }

    /// <summary>
    /// Removes the user account link from this organizer contact.
    /// The contact information remains, but the user loses co-organizer access.
    /// </summary>
    internal Result UnlinkUser()
    {
        if (!LinkedUserId.HasValue)
            return Result.Success(); // Already unlinked, idempotent

        LinkedUserId = null;
        return Result.Success();
    }

    /// <summary>
    /// Validates email format using RFC 5322 compliant regex
    /// Same pattern as used in Event.cs domain validation
    /// </summary>
    internal static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var emailRegex = new Regex(
            @"^[a-zA-Z0-9][a-zA-Z0-9._%+-]*@[a-zA-Z0-9][a-zA-Z0-9.-]*\.[a-zA-Z]{2,}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        return emailRegex.IsMatch(email.Trim());
    }
}
