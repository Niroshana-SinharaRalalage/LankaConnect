using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Events;

public class Event : BaseEntity
{
    private readonly List<Registration> _registrations = new();

    public EventTitle Title { get; private set; }
    public EventDescription Description { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public Guid OrganizerId { get; private set; }
    public int Capacity { get; private set; }
    public EventStatus Status { get; private set; }
    public string? CancellationReason { get; private set; }
    public EventLocation? Location { get; private set; } // Epic 2 Phase 1: Event location support
    public EventCategory Category { get; private set; } // Epic 2 Phase 2: Event category classification
    public Money? TicketPrice { get; private set; } // Epic 2 Phase 2: Ticket pricing support

    public IReadOnlyList<Registration> Registrations => _registrations.AsReadOnly();
    public int CurrentRegistrations => _registrations
        .Where(r => r.Status == RegistrationStatus.Confirmed)
        .Sum(r => r.Quantity);

    // EF Core constructor
    private Event() 
    {
        Title = null!;
        Description = null!;
    }

    private Event(EventTitle title, EventDescription description, DateTime startDate, DateTime endDate,
        Guid organizerId, int capacity, EventLocation? location = null, EventCategory category = EventCategory.Community, Money? ticketPrice = null)
    {
        Title = title;
        Description = description;
        StartDate = startDate;
        EndDate = endDate;
        OrganizerId = organizerId;
        Capacity = capacity;
        Status = EventStatus.Draft;
        Location = location;
        Category = category;
        TicketPrice = ticketPrice;
    }

    public static Result<Event> Create(EventTitle title, EventDescription description, DateTime startDate,
        DateTime endDate, Guid organizerId, int capacity, EventLocation? location = null,
        EventCategory category = EventCategory.Community, Money? ticketPrice = null)
    {
        if (title == null)
            return Result<Event>.Failure("Title is required");

        if (description == null)
            return Result<Event>.Failure("Description is required");

        if (organizerId == Guid.Empty)
            return Result<Event>.Failure("Organizer ID is required");

        if (startDate <= DateTime.UtcNow)
            return Result<Event>.Failure("Start date cannot be in the past");

        if (endDate <= startDate)
            return Result<Event>.Failure("End date must be after start date");

        if (capacity <= 0)
            return Result<Event>.Failure("Capacity must be greater than 0");

        var @event = new Event(title, description, startDate, endDate, organizerId, capacity, location, category, ticketPrice);
        return Result<Event>.Success(@event);
    }

    /// <summary>
    /// Creates a default event for testing and scoring scenarios
    /// </summary>
    public static Event CreateDefault()
    {
        return new Event(
            EventTitle.Create("Default Event").Value,
            EventDescription.Create("Default event for scoring calculations").Value,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            Guid.NewGuid(),
            100
        );
    }

    public Result Publish()
    {
        if (Status == EventStatus.Published)
            return Result.Failure("Event is already published");

        if (Status != EventStatus.Draft)
            return Result.Failure("Only draft events can be published");

        Status = EventStatus.Published;
        MarkAsUpdated();
        
        // Raise domain event
        RaiseDomainEvent(new EventPublishedEvent(Id, DateTime.UtcNow, OrganizerId));
        
        return Result.Success();
    }

    public Result Cancel(string reason)
    {
        if (Status != EventStatus.Published)
            return Result.Failure("Only published events can be cancelled");

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure("Cancellation reason is required");

        Status = EventStatus.Cancelled;
        CancellationReason = reason.Trim();
        MarkAsUpdated();
        
        // Raise domain event
        RaiseDomainEvent(new EventCancelledEvent(Id, reason.Trim(), DateTime.UtcNow));
        
        return Result.Success();
    }

    public Result Register(Guid userId, int quantity)
    {
        if (Status != EventStatus.Published)
            return Result.Failure("Cannot register for unpublished event");

        if (userId == Guid.Empty)
            return Result.Failure("User ID is required");

        if (quantity <= 0)
            return Result.Failure("Quantity must be greater than 0");

        if (IsUserRegistered(userId))
            return Result.Failure("User is already registered for this event");

        if (!HasCapacityFor(quantity))
            return Result.Failure("Event is at full capacity");

        var registrationResult = Registration.Create(Id, userId, quantity);
        if (registrationResult.IsFailure)
            return Result.Failure(registrationResult.Errors);

        _registrations.Add(registrationResult.Value);
        MarkAsUpdated();
        
        // Raise domain event
        RaiseDomainEvent(new RegistrationConfirmedEvent(Id, userId, quantity, DateTime.UtcNow));
        
        return Result.Success();
    }

    public Result CancelRegistration(Guid userId)
    {
        var registration = _registrations.FirstOrDefault(r => r.UserId == userId && r.Status == RegistrationStatus.Confirmed);

        if (registration == null)
            return Result.Failure("User is not registered for this event");

        registration.Cancel();
        MarkAsUpdated();

        // Raise domain event
        RaiseDomainEvent(new RegistrationCancelledEvent(Id, userId, DateTime.UtcNow));

        return Result.Success();
    }

    public Result UpdateRegistration(Guid userId, int newQuantity)
    {
        if (userId == Guid.Empty)
            return Result.Failure("User ID is required");

        if (newQuantity <= 0)
            return Result.Failure("Quantity must be greater than 0");

        // Find existing registration
        var registration = _registrations.FirstOrDefault(r => r.UserId == userId && r.Status == RegistrationStatus.Confirmed);

        if (registration == null)
            return Result.Failure("User is not registered for this event");

        var previousQuantity = registration.Quantity;
        var quantityDelta = newQuantity - previousQuantity;

        // If increasing quantity, check capacity
        if (quantityDelta > 0)
        {
            if (!HasCapacityFor(quantityDelta))
                return Result.Failure("Insufficient capacity to increase registration quantity");
        }

        // Update the registration
        registration.UpdateQuantity(newQuantity);
        MarkAsUpdated();

        // Raise domain event
        RaiseDomainEvent(new RegistrationQuantityUpdatedEvent(Id, userId, previousQuantity, newQuantity, DateTime.UtcNow));

        return Result.Success();
    }

    public bool IsUserRegistered(Guid userId)
    {
        return _registrations.Any(r => r.UserId == userId && r.Status == RegistrationStatus.Confirmed);
    }

    public bool HasCapacityFor(int quantity)
    {
        return CurrentRegistrations + quantity <= Capacity;
    }

    public void Complete()
    {
        if (Status == EventStatus.Published && DateTime.UtcNow > EndDate)
        {
            Status = EventStatus.Completed;
            MarkAsUpdated();
        }
    }

    public Result ActivateEvent()
    {
        if (Status != EventStatus.Published)
            return Result.Failure("Only published events can be activated");

        if (DateTime.UtcNow < StartDate)
            return Result.Failure("Event cannot be activated before start date");

        Status = EventStatus.Active;
        MarkAsUpdated();
        
        // Raise domain event
        RaiseDomainEvent(new EventActivatedEvent(Id, DateTime.UtcNow));
        
        return Result.Success();
    }

    public Result Postpone(string reason)
    {
        if (Status != EventStatus.Published)
            return Result.Failure("Only published events can be postponed");

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure("Postponement reason is required");

        Status = EventStatus.Postponed;
        CancellationReason = reason.Trim(); // Reuse cancellation reason field for postponement
        MarkAsUpdated();
        
        // Raise domain event
        RaiseDomainEvent(new EventPostponedEvent(Id, reason.Trim(), DateTime.UtcNow));
        
        return Result.Success();
    }

    public Result Archive()
    {
        if (Status != EventStatus.Completed)
            return Result.Failure("Only completed events can be archived");

        Status = EventStatus.Archived;
        MarkAsUpdated();
        
        // Raise domain event
        RaiseDomainEvent(new EventArchivedEvent(Id, DateTime.UtcNow));
        
        return Result.Success();
    }

    public Result SubmitForReview()
    {
        if (Status != EventStatus.Draft)
            return Result.Failure("Only draft events can be submitted for review");

        Status = EventStatus.UnderReview;
        MarkAsUpdated();

        // Raise domain event
        RaiseDomainEvent(new EventSubmittedForReviewEvent(Id, DateTime.UtcNow, true)); // Cultural events require approval

        return Result.Success();
    }

    public Result Approve(Guid approvedByAdminId)
    {
        if (Status != EventStatus.UnderReview)
            return Result.Failure("Only events under review can be approved");

        if (approvedByAdminId == Guid.Empty)
            return Result.Failure("Admin ID is required");

        Status = EventStatus.Published;
        MarkAsUpdated();

        // Raise domain event
        RaiseDomainEvent(new EventApprovedEvent(Id, approvedByAdminId, DateTime.UtcNow));

        return Result.Success();
    }

    public Result Reject(Guid rejectedByAdminId, string reason)
    {
        if (Status != EventStatus.UnderReview)
            return Result.Failure("Only events under review can be rejected");

        if (rejectedByAdminId == Guid.Empty)
            return Result.Failure("Admin ID is required");

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure("Rejection reason is required");

        Status = EventStatus.Draft; // Return to draft for organizer to modify
        MarkAsUpdated();

        // Raise domain event
        RaiseDomainEvent(new EventRejectedEvent(Id, rejectedByAdminId, reason, DateTime.UtcNow));

        return Result.Success();
    }

    public Result UpdateCapacity(int newCapacity)
    {
        if (newCapacity <= 0)
            return Result.Failure("Capacity must be greater than 0");

        if (newCapacity < CurrentRegistrations)
            return Result.Failure("Cannot reduce capacity below current registrations");

        var previousCapacity = Capacity;
        Capacity = newCapacity;
        MarkAsUpdated();
        
        // Raise domain event
        RaiseDomainEvent(new EventCapacityUpdatedEvent(Id, previousCapacity, newCapacity, DateTime.UtcNow));
        
        return Result.Success();
    }

    public Result HasSchedulingConflict(Event otherEvent)
    {
        if (otherEvent == null)
            return Result.Failure("Cannot check conflict with null event");

        // Check for date overlap
        bool hasDateOverlap = StartDate.Date == otherEvent.StartDate.Date ||
                             EndDate.Date == otherEvent.EndDate.Date ||
                             (StartDate <= otherEvent.StartDate && EndDate >= otherEvent.StartDate) ||
                             (StartDate <= otherEvent.EndDate && EndDate >= otherEvent.EndDate);

        return hasDateOverlap ? Result.Success() : Result.Failure("No scheduling conflict");
    }

    #region Location Management (Epic 2 Phase 1)

    /// <summary>
    /// Sets the event location (address + optional coordinates)
    /// </summary>
    public Result SetLocation(EventLocation location)
    {
        if (location == null)
            return Result.Failure("Location cannot be null");

        Location = location;
        MarkAsUpdated();

        // Raise domain event
        RaiseDomainEvent(new EventLocationUpdatedEvent(Id, location, DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Removes the event location (e.g., converting to virtual event)
    /// </summary>
    public Result RemoveLocation()
    {
        if (Location == null)
            return Result.Failure("Event does not have a location set");

        Location = null;
        MarkAsUpdated();

        // Raise domain event
        RaiseDomainEvent(new EventLocationRemovedEvent(Id, DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Checks if event has a physical location set
    /// </summary>
    public bool HasLocation() => Location != null;

    #endregion

    #region Pricing Management (Epic 2 Phase 2)

    /// <summary>
    /// Checks if event is free (no ticket price or zero ticket price)
    /// </summary>
    public bool IsFree() => TicketPrice == null || TicketPrice.IsZero;

    #endregion
}