using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Events;

public partial class Event : BaseEntity
{
    private readonly List<Registration> _registrations = new();
    private readonly List<EventImage> _images = new(); // Epic 2 Phase 2: Event images support
    private readonly List<EventVideo> _videos = new(); // Epic 2 Phase 2: Event videos support
    private readonly List<WaitingListEntry> _waitingList = new(); // Epic 2: Waiting List support
    private readonly List<EventPass> _passes = new(); // Event passes/tickets support
    private readonly List<SignUpList> _signUpLists = new(); // Sign-up lists for volunteers/items
    private readonly List<EventBadge> _badges = new(); // Phase 6A.25: Event badges for promotional overlays
    private readonly List<Guid> _emailGroupIds = new(); // Phase 6A.32: Email group references for event invitations
    private readonly List<Domain.Communications.Entities.EmailGroup> _emailGroupEntities = new(); // Phase 6A.32: Shadow navigation for EF Core
    private readonly List<EventOrganizerContact> _organizerContacts = new(); // Multiple organizer contacts

    private const int MAX_IMAGES = 10; // Maximum images per event
    private const int MAX_BADGES = 3; // Maximum badges per event
    private const int MAX_VIDEOS = 3; // Maximum videos per event
    private const int MAX_ORGANIZER_CONTACTS = 10; // Phase 6A.132: Maximum organizer contacts per event

    public EventTitle Title { get; private set; }
    public EventDescription Description { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public Guid OrganizerId { get; private set; }
    public int Capacity { get; private set; }
    public EventStatus Status { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTime? PublishedAt { get; private set; } // Phase 6A.46: Track when event was published for "New" label calculation
    public EventLocation? Location { get; private set; } // Epic 2 Phase 1: Event location support
    public EventSecondaryLocation? SecondaryLocation { get; private set; } // Phase 7C.1: Optional secondary location (parking lot / secondary venue)

    /// <summary>
    /// Phase 6A.97: IANA timezone identifier for event's local time display.
    /// Derived from event location (US state) when event is created/updated.
    /// Examples: "America/New_York" (Eastern), "America/Chicago" (Central), "America/Los_Angeles" (Pacific)
    /// Used for consistent date/time display in emails and frontend.
    /// </summary>
    public string? TimeZoneId { get; private set; }

    public EventCategory Category { get; private set; } // Epic 2 Phase 2: Event category classification
    public Money? TicketPrice { get; private set; } // Epic 2 Phase 2: Ticket pricing support (legacy - single price)
    public TicketPricing? Pricing { get; private set; } // Session 21: Dual ticket pricing (adult/child) with age limit
    public RevenueBreakdown? RevenueBreakdown { get; private set; } // Phase 6A.X: Detailed revenue breakdown for paid events

    /// <summary>
    /// Phase 6A.86: Explicit flag indicating whether this event is free
    /// Source of truth for free/paid determination (eliminates NULL pricing ambiguity)
    /// Benefits:
    /// - Unambiguous intent (NULL pricing no longer means "free")
    /// - Security: Prevents payment bypass vulnerabilities
    /// - Simplicity: Single boolean instead of complex price checking
    ///
    /// Phase 8X note: superseded as the source of truth by <see cref="PaymentMode"/>
    /// but kept as a real entity property (Option B per architect verdict — no
    /// builder.Ignore, no shadow property, Phase 6A.123 lesson). The two are kept
    /// in lockstep by <c>SyncLegacyIsFree()</c> called from every PaymentMode
    /// mutation (Slice 8X.3). To be dropped in Phase 8Y once all reports / exports
    /// migrate to read <c>PaymentMode == Free</c> directly.
    /// </summary>
    public bool IsFreeEvent { get; private set; }

    /// <summary>
    /// Phase 8X — Source of truth for free / paid / external-payment determination.
    /// Default <see cref="EventPaymentMode.Free"/> matches the DB-level smallint
    /// DEFAULT 0 added by the Phase 8X.2 migration so legacy rows materialise
    /// correctly (Phase 6A.123 lesson — never rely on app-side defaults for NOT
    /// NULL columns). Mutations go through <c>SetExternalPayment</c> /
    /// <c>SetPaymentMode</c> domain methods (Slice 8X.3).
    /// </summary>
    public EventPaymentMode PaymentMode { get; private set; } = EventPaymentMode.Free;

    /// <summary>
    /// Phase 8X — External registration details (URL + optional vendor name +
    /// optional instructions) for <see cref="EventPaymentMode.ExternalPaid"/>
    /// events. Always null for <see cref="EventPaymentMode.Free"/> /
    /// <see cref="EventPaymentMode.OnPlatformPaid"/>. Set via
    /// <c>SetExternalPayment</c>; cleared automatically when payment mode
    /// transitions away from ExternalPaid.
    /// </summary>
    public ExternalRegistration? ExternalRegistration { get; private set; }

    /// <summary>
    /// Issue #51: Maximum number of attendees allowed per single registration
    /// Configurable by event organizer via /events/{id}/manage page
    /// Default: 10 (backward compatibility)
    /// System Maximum: 50 (safety net to prevent abuse)
    /// Cannot exceed event capacity
    /// </summary>
    public int MaxAttendeesPerRegistration { get; private set; } = 10;

    // System-wide maximum for safety (prevents one registration from booking entire large event)
    public const int SYSTEM_MAX_ATTENDEES_PER_REGISTRATION = 50;

    // Event Organizer Contact Details: Optional contact information for event inquiries
    public bool PublishOrganizerContact { get; private set; }
    public IReadOnlyList<EventOrganizerContact> OrganizerContacts => _organizerContacts.AsReadOnly();

    // Backward-compat computed properties for email handlers (delegate to primary contact)
    public string? OrganizerContactName => GetPrimaryContact()?.ContactName;
    public string? OrganizerContactPhone => GetPrimaryContact()?.ContactPhone;
    public string? OrganizerContactEmail => GetPrimaryContact()?.ContactEmail;

    // Donation Configuration: Optional donation settings for the event
    public DonationConfiguration? DonationConfig { get; private set; }

    // Collection Configuration: Optional event fund / fundraising settings
    public CollectionConfiguration? CollectionConfig { get; private set; }

    // Sponsor Configuration: Optional sponsorship settings (money + item)
    public SponsorConfiguration? SponsorConfig { get; private set; }

    // Add-on Configuration: Optional purchasable items settings
    public AddOnConfiguration? AddOnConfig { get; private set; }

    public IReadOnlyList<Registration> Registrations => _registrations.AsReadOnly();
    public IReadOnlyList<EventImage> Images => _images.AsReadOnly(); // Epic 2 Phase 2: Read-only image collection
    public IReadOnlyList<EventVideo> Videos => _videos.AsReadOnly(); // Epic 2 Phase 2: Read-only video collection
    public IReadOnlyList<WaitingListEntry> WaitingList => _waitingList.AsReadOnly(); // Epic 2: Read-only waiting list collection
    public IReadOnlyList<EventPass> Passes => _passes.AsReadOnly(); // Read-only pass collection
    public IReadOnlyList<SignUpList> SignUpLists => _signUpLists.AsReadOnly(); // Read-only sign-up lists collection
    public IReadOnlyList<EventBadge> Badges => _badges.AsReadOnly(); // Phase 6A.25: Read-only badge collection
    // Phase 6A.33 FIX: EmailGroupIds exposes domain's _emailGroupIds list (source of truth for business logic)
    // Infrastructure layer syncs _emailGroupEntities shadow navigation from _emailGroupIds for persistence
    // Pattern mirrors User.PreferredMetroAreaIds per ADR-009
    public IReadOnlyList<Guid> EmailGroupIds => _emailGroupIds.AsReadOnly();

    // Session 21: Updated to support both legacy Quantity and new multi-attendee format
    public int CurrentRegistrations => _registrations
        .Where(r => r.Status == RegistrationStatus.Confirmed)
        .Sum(r => r.GetAttendeeCount());

    // Phase 6A.136C: Reserved capacity includes Preliminary (awaiting payment) to prevent overbooking.
    // Use this for capacity checks instead of CurrentRegistrations.
    public int ReservedCapacity => _registrations
        .Where(r => r.Status == RegistrationStatus.Confirmed || r.Status == RegistrationStatus.Preliminary)
        .Sum(r => r.GetAttendeeCount());

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
        // Ensure dates are always stored as UTC for PostgreSQL compatibility
        StartDate = startDate.Kind == DateTimeKind.Utc ? startDate : DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
        EndDate = endDate.Kind == DateTimeKind.Utc ? endDate : DateTime.SpecifyKind(endDate, DateTimeKind.Utc);
        OrganizerId = organizerId;
        Capacity = capacity;
        Status = EventStatus.Draft;
        Location = location;
        Category = category;
        TicketPrice = ticketPrice;
        // Phase 6A.86: Set IsFreeEvent flag based on ticket price
        // Only explicit $0 price means free event
        // NULL ticket price defaults to PAID for security (Phase 6A.81 security fix)
        IsFreeEvent = ticketPrice != null && ticketPrice.IsZero;
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
        PublishedAt = DateTime.UtcNow; // Phase 6A.46: Track publish timestamp for "New" label
        MarkAsUpdated();

        // Raise domain event
        RaiseDomainEvent(new EventPublishedEvent(Id, DateTime.UtcNow, OrganizerId));

        return Result.Success();
    }

    /// <summary>
    /// Phase 6A.41: Unpublishes a published event, returning it to Draft status.
    /// Allows organizers to make corrections after premature publication.
    /// Business Rules:
    /// - Only Published events can be unpublished
    /// - Cannot unpublish Active, Cancelled, Postponed, or Completed events
    /// - Events with registrations CAN be unpublished (organizer's decision)
    /// </summary>
    public Result Unpublish()
    {
        if (Status != EventStatus.Published)
            return Result.Failure("Only published events can be unpublished");

        Status = EventStatus.Draft;
        PublishedAt = null; // Phase 6A.46: Clear publish timestamp when unpublishing
        MarkAsUpdated();

        // Raise domain event (for potential notification/logging)
        RaiseDomainEvent(new EventUnpublishedEvent(Id, DateTime.UtcNow));

        return Result.Success();
    }

    public Result Cancel(string reason)
    {
        // Phase 6A.59: Allow cancelling Draft events too (organizer changes mind before publishing)
        if (Status != EventStatus.Published && Status != EventStatus.Draft)
            return Result.Failure("Only published or draft events can be cancelled");

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

        if (StartDate <= DateTime.UtcNow)
            return Result.Failure("Cannot register for an event that has already started");

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

    public Result RegisterAnonymous(AttendeeInfo attendeeInfo, int quantity)
    {
        if (Status != EventStatus.Published)
            return Result.Failure("Cannot register for unpublished event");

        if (StartDate <= DateTime.UtcNow)
            return Result.Failure("Cannot register for an event that has already started");

        if (attendeeInfo == null)
            return Result.Failure("Attendee information is required");

        if (quantity <= 0)
            return Result.Failure("Quantity must be greater than 0");

        // Phase 6A.XXX FIX: Check for duplicate email across ALL registrations (both anonymous and authenticated)
        // Previously had zero duplicate detection - found by system-architect
#pragma warning disable CS0618
        var existingByEmail = _registrations.FirstOrDefault(r =>
            r.Status != RegistrationStatus.Cancelled &&
            r.Status != RegistrationStatus.Refunded &&
            r.Status != RegistrationStatus.RefundRequested &&
            r.Status != RegistrationStatus.Preliminary &&
            r.Status != RegistrationStatus.Abandoned &&
            r.Status != RegistrationStatus.Pending &&
            ((r.Contact != null && r.Contact.Email.Equals(attendeeInfo.Email.Value, StringComparison.OrdinalIgnoreCase)) ||
             (r.AttendeeInfo != null && r.AttendeeInfo.Email != null &&
              r.AttendeeInfo.Email.Value.Equals(attendeeInfo.Email.Value, StringComparison.OrdinalIgnoreCase))));
#pragma warning restore CS0618

        if (existingByEmail != null)
            return Result.Failure("This email is already registered for this event. Each email can only register once.");

        if (!HasCapacityFor(quantity))
            return Result.Failure("Event is at full capacity");

        var registrationResult = Registration.CreateAnonymous(Id, attendeeInfo, quantity);
        if (registrationResult.IsFailure)
            return Result.Failure(registrationResult.Errors);

        _registrations.Add(registrationResult.Value);
        MarkAsUpdated();

        // Raise domain event for anonymous registration
        RaiseDomainEvent(new AnonymousRegistrationConfirmedEvent(Id, attendeeInfo.Email.Value, quantity, DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Session 21: Registers multiple attendees with detailed information (name + age for each)
    /// Supports both anonymous and authenticated user registration
    /// </summary>
    /// <param name="userId">Optional user ID (null for anonymous registration)</param>
    /// <param name="attendees">List of attendees with names and ages</param>
    /// <param name="contact">Shared contact information for all attendees</param>
    /// <returns>Result indicating success or failure</returns>
    public Result RegisterWithAttendees(
        Guid? userId,
        IEnumerable<AttendeeDetails> attendees,
        RegistrationContact contact)
    {
        if (Status != EventStatus.Published)
            return Result.Failure("Cannot register for unpublished event");

        if (StartDate <= DateTime.UtcNow)
            return Result.Failure("Cannot register for an event that has already started");

        // Phase 7E.3a: Defensive mode guard — RegisterWithAttendees is the Mode-A path. Calling
        // it on a B-mode event would create a Registration row that contradicts the event's
        // RegistrationMode (Attendees populated AND Mode says HeadCount*). Reject early with
        // a clear redirect to the head-count flow.
        if (RegistrationMode != RegistrationMode.DetailedAttendees)
        {
            if (RegistrationMode == RegistrationMode.NoRegistration)
                return Result.Failure(
                    "Registration is not required for this event. Standalone donations / sponsors / " +
                    "add-on purchases / collections are still accepted via their own endpoints.");

            return Result.Failure(
                $"This event uses {RegistrationMode} registration. " +
                "Use the head-count RSVP path (LeadAttendeeName + HeadCount payload) instead of Attendees.");
        }

        if (attendees == null || !attendees.Any())
            return Result.Failure("At least one attendee is required");

        var attendeeList = attendees.ToList();

        if (contact == null)
            return Result.Failure("Contact information is required");

        // Phase 6A.45 FIX: Check for duplicate registration
        // Phase 6A.XXX FIX: Cross-path duplicate detection - check BOTH UserId AND email
        // Previously, authenticated path only checked UserId and anonymous path only checked email,
        // allowing the same email to register via both paths for the same event.
        if (userId.HasValue)
        {
            // Check 1: Authenticated user - check by UserId
#pragma warning disable CS0618 // Type or member is obsolete (Pending deprecated but supported for backward compatibility)
            var existingByUserId = _registrations.FirstOrDefault(r =>
                r.UserId == userId &&
                r.Status != RegistrationStatus.Cancelled &&
                r.Status != RegistrationStatus.Refunded &&
                r.Status != RegistrationStatus.RefundRequested &&
                r.Status != RegistrationStatus.Preliminary &&
                r.Status != RegistrationStatus.Abandoned &&
                r.Status != RegistrationStatus.Pending);
#pragma warning restore CS0618

            if (existingByUserId != null)
                return Result.Failure("You are already registered for this event. To change your registration details, please cancel your current registration first.");

            // Check 2: Cross-path - check by email to catch registrations from the anonymous path
#pragma warning disable CS0618
            var existingByEmail = _registrations.FirstOrDefault(r =>
                r.Contact != null &&
                r.Contact.Email.Equals(contact.Email, StringComparison.OrdinalIgnoreCase) &&
                r.Status != RegistrationStatus.Cancelled &&
                r.Status != RegistrationStatus.Refunded &&
                r.Status != RegistrationStatus.RefundRequested &&
                r.Status != RegistrationStatus.Preliminary &&
                r.Status != RegistrationStatus.Abandoned &&
                r.Status != RegistrationStatus.Pending);
#pragma warning restore CS0618

            if (existingByEmail != null)
                return Result.Failure("This email is already registered for this event. Each email can only register once.");
        }
        else
        {
            // Anonymous user - check by email across ALL registrations (both anonymous and authenticated)
#pragma warning disable CS0618
            var existingByEmail = _registrations.FirstOrDefault(r =>
                ((r.Contact != null && r.Contact.Email.Equals(contact.Email, StringComparison.OrdinalIgnoreCase)) ||
                 (r.AttendeeInfo != null && r.AttendeeInfo.Email != null &&
                  r.AttendeeInfo.Email.Value.Equals(contact.Email, StringComparison.OrdinalIgnoreCase))) &&
                r.Status != RegistrationStatus.Cancelled &&
                r.Status != RegistrationStatus.Refunded &&
                r.Status != RegistrationStatus.RefundRequested &&
                r.Status != RegistrationStatus.Preliminary &&
                r.Status != RegistrationStatus.Abandoned &&
                r.Status != RegistrationStatus.Pending);
#pragma warning restore CS0618

            if (existingByEmail != null)
                return Result.Failure("This email is already registered for this event. Each email can only register once.");
        }

        // Phase 8: Tier-aware capacity and pricing
        Money? totalPrice;
        if (TicketingMode == Enums.TicketingMode.Tiered)
        {
            // Tiered mode: per-tier capacity check
            var tierCapacityResult = HasTieredCapacityFor(attendeeList);
            if (tierCapacityResult.IsFailure)
                return Result.Failure(tierCapacityResult.Errors);

            // Tiered mode: per-attendee tier pricing
            var tieredPriceResult = CalculateTieredPriceForAttendees(attendeeList);
            if (tieredPriceResult.IsFailure)
                return Result.Failure(tieredPriceResult.Errors);

            totalPrice = tieredPriceResult.Value;

            // Reserve tier capacity
            foreach (var tierGroup in attendeeList.Where(a => a.TicketTierId != null).GroupBy(a => a.TicketTierId!.Value))
            {
                var tier = _ticketTiers.First(t => t.Id == tierGroup.Key);
                var reserveResult = tier.Reserve(tierGroup.Count());
                if (reserveResult.IsFailure)
                    return Result.Failure(reserveResult.Errors);
            }
        }
        else
        {
            // SingleTier mode: existing capacity check
            if (!HasCapacityFor(attendeeList.Count))
                return Result.Failure("Event does not have enough capacity for all attendees");

            // SingleTier mode: existing pricing
            var priceResult = CalculatePriceForAttendees(attendeeList);
            if (priceResult.IsFailure)
                return Result.Failure(priceResult.Errors);

            totalPrice = priceResult.Value;
        }

        // Session 23: Determine if this is a paid event (has pricing and not free)
        bool isPaidEvent = !IsFree();

        // Create registration with all attendees
        // Issue #51: Pass event's configured max attendees per registration
        var registrationResult = Registration.CreateWithAttendees(
            Id,
            userId,
            attendeeList,
            contact,
            totalPrice!,
            isPaidEvent,
            MaxAttendeesPerRegistration);

        if (registrationResult.IsFailure)
            return Result.Failure(registrationResult.Errors);

        _registrations.Add(registrationResult.Value);
        MarkAsUpdated();

        // Phase 6A.81: CRITICAL - Only raise confirmation events for Confirmed registrations
        // For Preliminary registrations (paid events waiting for payment), domain events will be
        // raised by the CompletePayment() method after webhook confirms payment
        var registration = registrationResult.Value;
        if (registration.Status == RegistrationStatus.Confirmed)
        {
            // Raise appropriate domain event (triggers confirmation email)
            if (userId.HasValue)
            {
                // Authenticated user registration - FREE event or immediate confirmation
                RaiseDomainEvent(new RegistrationConfirmedEvent(Id, userId.Value, attendeeList.Count, DateTime.UtcNow));
            }
            else
            {
                // Anonymous registration - FREE event or immediate confirmation
                RaiseDomainEvent(new AnonymousRegistrationConfirmedEvent(Id, contact.Email, attendeeList.Count, DateTime.UtcNow));
            }
        }
        // Phase 6A.81: For Preliminary registrations, NO domain event raised here
        // Email will be sent after payment completes via PaymentCompletedEvent

        return Result.Success();
    }

    public Result CancelRegistration(Guid userId)
    {
        // Debug logging
        Console.WriteLine($"[CancelRegistration] EventId: {Id}, UserId: {userId}");
        Console.WriteLine($"[CancelRegistration] Total registrations count: {_registrations.Count}");
        foreach (var reg in _registrations)
        {
            Console.WriteLine($"[CancelRegistration] Registration: UserId={reg.UserId}, Status={reg.Status}, Match={reg.UserId == userId}");
        }

        // Find any active registration (not already cancelled or refunded)
        var registration = _registrations.FirstOrDefault(r =>
            r.UserId == userId &&
            r.Status != RegistrationStatus.Cancelled &&
            r.Status != RegistrationStatus.Refunded);

        Console.WriteLine($"[CancelRegistration] Found registration: {registration != null}");

        if (registration == null)
        {
            Console.WriteLine($"[CancelRegistration] FAILURE: User is not registered for this event");
            return Result.Failure("User is not registered for this event");
        }

        registration.Cancel();
        MarkAsUpdated();

        // Raise domain event
        RaiseDomainEvent(new RegistrationCancelledEvent(Id, userId, DateTime.UtcNow));

        // Epic 2: Check if spot available for waiting list
        if (!IsAtCapacity() && _waitingList.Any())
        {
            var nextInLine = _waitingList.OrderBy(w => w.Position).First();
            RaiseDomainEvent(new WaitingListSpotAvailableDomainEvent(Id, nextInLine.UserId, DateTime.UtcNow));
        }

        return Result.Success();
    }

    /// <summary>
    /// Phase 6A.62 Fix: Raises RegistrationCancelledEvent without requiring _registrations to be loaded.
    /// This allows the CancelRsvpCommandHandler to bypass EF Core navigation property issues
    /// while still triggering email notifications.
    /// </summary>
    public void RaiseRegistrationCancelledEvent(Guid userId)
    {
        RaiseDomainEvent(new RegistrationCancelledEvent(Id, userId, DateTime.UtcNow));
        MarkAsUpdated();
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

    /// <summary>
    /// Phase 6A.14: Updates registration details (attendees and contact information) for a user
    /// Business Rules:
    /// - User must have an active registration for this event
    /// - If adding attendees, event must have capacity
    /// - Delegates to Registration.UpdateDetails() for validation of payment/status rules
    /// </summary>
    /// <param name="userId">The user whose registration to update</param>
    /// <param name="newAttendees">Updated list of attendees</param>
    /// <param name="newContact">Updated contact information</param>
    /// <returns>Result indicating success or failure</returns>
    public Result UpdateRegistrationDetails(
        Guid userId,
        IEnumerable<AttendeeDetails> newAttendees,
        RegistrationContact newContact)
    {
        if (userId == Guid.Empty)
            return Result.Failure("User ID is required");

        // Phase 6A.81: Find the user's active registration (Confirmed, Preliminary, or legacy Pending)
#pragma warning disable CS0618 // Type or member is obsolete (Pending deprecated but supported for backward compatibility)
        var registration = _registrations.FirstOrDefault(r =>
            r.UserId == userId &&
            (r.Status == RegistrationStatus.Confirmed ||
             r.Status == RegistrationStatus.Preliminary ||
             r.Status == RegistrationStatus.Pending));  // Support legacy Pending
#pragma warning restore CS0618

        if (registration == null)
            return Result.Failure("User does not have an active registration for this event");

        var attendeeList = newAttendees?.ToList() ?? new List<AttendeeDetails>();

        // Check capacity if adding more attendees
        var currentCount = registration.GetAttendeeCount();
        var newCount = attendeeList.Count;

        if (newCount > currentCount)
        {
            var additionalNeeded = newCount - currentCount;
            if (!HasCapacityFor(additionalNeeded))
            {
                return Result.Failure(
                    $"Event does not have enough capacity for additional attendees. " +
                    $"Available: {Capacity - CurrentRegistrations + currentCount}, Requested: {newCount}");
            }
        }

        // Delegate to Registration for business rule validation and update
        var updateResult = registration.UpdateDetails(attendeeList, newContact);
        if (updateResult.IsFailure)
            return updateResult;

        MarkAsUpdated();

        // Raise domain event
        RaiseDomainEvent(new RegistrationDetailsUpdatedEvent(
            Id,
            registration.Id,
            userId,
            attendeeList.Count,
            DateTime.UtcNow));

        return Result.Success();
    }

    // Phase 6A.XXX FIX: Check all active statuses, not just Confirmed
    // Previously only checked Confirmed, missing CheckedIn, Attended, and Waitlisted
#pragma warning disable CS0618 // Type or member is obsolete (Pending deprecated but supported for backward compatibility)
    public bool IsUserRegistered(Guid userId)
    {
        return _registrations.Any(r => r.UserId == userId
            && r.Status != RegistrationStatus.Cancelled
            && r.Status != RegistrationStatus.Refunded
            && r.Status != RegistrationStatus.RefundRequested
            && r.Status != RegistrationStatus.Preliminary
            && r.Status != RegistrationStatus.Abandoned
            && r.Status != RegistrationStatus.Pending);
    }
#pragma warning restore CS0618

    public bool HasCapacityFor(int quantity)
    {
        // Phase 6A.136C: Use ReservedCapacity (includes Preliminary) to prevent overbooking
        return ReservedCapacity + quantity <= Capacity;
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
        PublishedAt = DateTime.UtcNow; // Phase 6A.46: Track publish timestamp when approved
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

        // Issue #51: Auto-adjust MaxAttendeesPerRegistration if it exceeds new capacity
        if (MaxAttendeesPerRegistration > newCapacity)
        {
            MaxAttendeesPerRegistration = newCapacity;
        }

        MarkAsUpdated();

        // Raise domain event
        RaiseDomainEvent(new EventCapacityUpdatedEvent(Id, previousCapacity, newCapacity, DateTime.UtcNow));

        return Result.Success();
    }

    #region MaxAttendeesPerRegistration Management (Issue #51)

    /// <summary>
    /// Updates the maximum number of attendees allowed per single registration
    /// Issue #51: Configurable by event organizer
    /// </summary>
    /// <param name="maxAttendees">New maximum (1 to min(eventCapacity, 50))</param>
    /// <returns>Result indicating success or failure</returns>
    public Result UpdateMaxAttendeesPerRegistration(int maxAttendees)
    {
        // Validation: Must be at least 1
        if (maxAttendees < 1)
            return Result.Failure("Maximum attendees per registration must be at least 1");

        // Validation: Cannot exceed system maximum (safety net)
        if (maxAttendees > SYSTEM_MAX_ATTENDEES_PER_REGISTRATION)
            return Result.Failure($"Maximum attendees per registration cannot exceed {SYSTEM_MAX_ATTENDEES_PER_REGISTRATION}");

        // Validation: Cannot exceed event capacity
        if (maxAttendees > Capacity)
            return Result.Failure($"Maximum attendees per registration cannot exceed event capacity ({Capacity})");

        MaxAttendeesPerRegistration = maxAttendees;
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Gets the effective maximum attendees for a new registration
    /// Takes into account configured limit, event capacity, and spots left
    /// </summary>
    /// <returns>Maximum attendees allowed for a new registration</returns>
    public int GetEffectiveMaxAttendeesPerRegistration()
    {
        var spotsLeft = Capacity - CurrentRegistrations;
        return Math.Min(MaxAttendeesPerRegistration, spotsLeft);
    }

    #endregion

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
    /// Phase 6A.97: Sets the event's timezone for consistent date/time display.
    /// Should be called when event location is set/updated.
    /// </summary>
    /// <param name="timeZoneId">IANA timezone ID (e.g., "America/New_York")</param>
    public Result SetTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return Result.Failure("Timezone ID cannot be null or empty");

        // Validate the timezone ID is valid
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return Result.Failure($"Invalid timezone ID: {timeZoneId}");
        }

        TimeZoneId = timeZoneId;
        MarkAsUpdated();

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

    /// <summary>
    /// Phase 7C.1: Sets the optional secondary location (parking lot or secondary venue).
    /// Replaces any existing secondary location.
    /// </summary>
    public Result SetSecondaryLocation(EventSecondaryLocation secondaryLocation)
    {
        if (secondaryLocation == null)
            return Result.Failure("Secondary location cannot be null");

        SecondaryLocation = secondaryLocation;
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Phase 7C.1: Clears the secondary location. Idempotent — succeeds even if already null.
    /// </summary>
    public Result ClearSecondaryLocation()
    {
        if (SecondaryLocation == null)
            return Result.Success();

        SecondaryLocation = null;
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Phase 7C.1: Checks whether a secondary location is set on this event.
    /// </summary>
    public bool HasSecondaryLocation() => SecondaryLocation != null;

    #endregion

    #region Pricing Management (Epic 2 Phase 2 + Session 21)

    /// <summary>
    /// Checks if event is free
    /// Phase 6A.86: Returns explicit IsFreeEvent flag (source of truth)
    /// No longer computes from pricing amounts (eliminates NULL ambiguity)
    /// </summary>
    public bool IsFree() => IsFreeEvent;

    /// <summary>
    /// Phase 6A.86: Marks event as free
    /// Sets IsFreeEvent flag and optionally sets $0 pricing for display
    /// </summary>
    public Result SetAsFreeEvent()
    {
        IsFreeEvent = true;

        // Optional: Set explicit $0 pricing for display/reporting purposes
        var zeroPrice = Money.Create(0m, Shared.Enums.Currency.USD);
        if (zeroPrice.IsSuccess)
        {
            TicketPrice = zeroPrice.Value;
        }

        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Phase 6A.86: Sets single ticket pricing and updates IsFreeEvent flag
    /// Replaces legacy pricing setter with flag management
    /// </summary>
    /// <param name="ticketPrice">Ticket price (use Money.Zero for free events)</param>
    public Result SetPricing(Money? ticketPrice)
    {
        if (ticketPrice == null)
            return Result.Failure("Ticket price cannot be null. Use SetAsFreeEvent() for free events.");

        TicketPrice = ticketPrice;
        IsFreeEvent = ticketPrice.IsZero;

        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Sets dual pricing for event (adult + optional child pricing)
    /// Session 21: Dual Ticket Pricing feature
    /// Phase 6A.86: Now updates IsFreeEvent flag based on pricing
    /// </summary>
    /// <param name="pricing">Ticket pricing configuration with adult and optional child prices</param>
    public Result SetDualPricing(TicketPricing pricing)
    {
        if (pricing == null)
            return Result.Failure("Pricing cannot be null");

        Pricing = pricing;

        // Phase 6A.86: Update IsFreeEvent flag based on pricing type
        if (pricing.Type == PricingType.GroupTiered)
        {
            // For group-tiered pricing, check if all tiers are free
            IsFreeEvent = pricing.GroupTiers.All(tier => tier.PricePerPerson.IsZero);
        }
        else
        {
            // For single/dual pricing, check adult price
            IsFreeEvent = pricing.AdultPrice.IsZero;
        }

        // Session 33: Create a NEW Money instance for backward compatibility
        // CRITICAL: Do NOT share the same Money object reference between TicketPrice and Pricing.AdultPrice
        // EF Core cannot track the same owned entity instance in two different navigations
        if (pricing.AdultPrice != null)
        {
            var copyResult = Money.Create(pricing.AdultPrice.Amount, pricing.AdultPrice.Currency);
            if (copyResult.IsSuccess)
            {
                TicketPrice = copyResult.Value;
            }
        }

        MarkAsUpdated();

        // Raise domain event
        RaiseDomainEvent(new EventPricingUpdatedEvent(Id, pricing, DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Phase 6D: Sets group-based tiered pricing for the event
    /// Phase 6A.86: Now updates IsFreeEvent flag based on tier pricing
    /// </summary>
    /// <param name="pricing">Ticket pricing configuration with group tiers</param>
    public Result SetGroupPricing(TicketPricing? pricing)
    {
        if (pricing == null)
            return Result.Failure("Pricing cannot be null");

        if (pricing.Type != PricingType.GroupTiered)
            return Result.Failure("Only GroupTiered pricing type is allowed for SetGroupPricing");

        Pricing = pricing;

        // Phase 6A.86: Check if all tiers are free (all prices are zero)
        IsFreeEvent = pricing.GroupTiers.All(tier => tier.PricePerPerson.IsZero);

        // Set TicketPrice to null for group pricing (not applicable)
        TicketPrice = null;

        MarkAsUpdated();

        // Raise domain event
        RaiseDomainEvent(new EventPricingUpdatedEvent(Id, pricing, DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Phase 6A.X: Sets the revenue breakdown for this event
    /// Should be called after pricing is set and location is known
    /// </summary>
    public Result SetRevenueBreakdown(RevenueBreakdown breakdown)
    {
        if (breakdown == null)
            return Result.Failure("Revenue breakdown cannot be null");

        RevenueBreakdown = breakdown;
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Calculates total price for a list of attendees
    /// Session 21: Multi-attendee registration with age-based pricing
    /// Phase 6D: Updated to support GroupTiered pricing based on attendee count
    /// </summary>
    /// <param name="attendees">List of attendees with ages</param>
    /// <returns>Total price for all attendees</returns>
    public Result<Money> CalculatePriceForAttendees(IEnumerable<AttendeeDetails> attendees)
    {
        if (attendees == null || !attendees.Any())
            return Result<Money>.Failure("At least one attendee is required");

        // Free event
        if (IsFree())
        {
            var freePrice = Pricing?.AdultPrice ?? TicketPrice ?? Money.Create(0, Shared.Enums.Currency.USD).Value;
            return Result<Money>.Success(freePrice);
        }

        // Phase 6A.86 + Pricing-Guard Fix 2026-05-04: Paid events MUST have pricing
        // configured in one of three valid shapes (legacy Pricing, legacy TicketPrice,
        // OR Tiered with at least one active tier). The pre-fix legacy-only check
        // rejected paid+Tiered events created without redundant SetDualPricing
        // (architect-reviewed root cause; see EventPaidPricingGuardTests).
        if (!IsFreeEvent && !HasPaidPricingConfigured())
        {
            throw new InvalidOperationException(
                "This event is marked as paid but has no pricing configured. " +
                "Add ticket tiers, set a ticket price, or mark the event as free before accepting registrations.");
        }

        // Calculate based on pricing configuration
        if (Pricing != null)
        {
            // Phase 6D: Group tiered pricing - calculate based on total attendee count
            if (Pricing.Type == PricingType.GroupTiered)
            {
                var attendeeCount = attendees.Count();
                var groupPriceResult = Pricing.CalculateGroupPrice(attendeeCount);

                if (groupPriceResult.IsFailure)
                    return Result<Money>.Failure(groupPriceResult.Error);

                return Result<Money>.Success(groupPriceResult.Value);
            }

            // Dual pricing (AgeDual): Calculate for each attendee based on age category
            // Single pricing: Also uses CalculateForCategory (returns AdultPrice for all)
            Money? totalPrice = null;

            foreach (var attendee in attendees)
            {
                var attendeePrice = Pricing.CalculateForCategory(attendee.AgeCategory);

                if (totalPrice == null)
                {
                    totalPrice = attendeePrice;
                }
                else
                {
                    var addResult = totalPrice.Add(attendeePrice);
                    if (addResult.IsFailure)
                        return Result<Money>.Failure(addResult.Errors);

                    totalPrice = addResult.Value;
                }
            }

            return Result<Money>.Success(totalPrice!);
        }

        // Legacy single pricing: Multiply ticket price by quantity
        if (TicketPrice != null)
        {
            var multiplyResult = TicketPrice.Multiply(attendees.Count());
            if (multiplyResult.IsFailure)
                return Result<Money>.Failure(multiplyResult.Errors);

            return Result<Money>.Success(multiplyResult.Value);
        }

        return Result<Money>.Failure("Event pricing is not configured");
    }

    #endregion

    #region Image Management (Epic 2 Phase 2)

    /// <summary>
    /// Adds an image to the event gallery
    /// Enforces maximum image limit invariant
    /// </summary>
    public Result<EventImage> AddImage(string imageUrl, string blobName)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return Result<EventImage>.Failure("Image URL cannot be empty");

        if (string.IsNullOrWhiteSpace(blobName))
            return Result<EventImage>.Failure("Blob name cannot be empty");

        // Invariant: Max images limit
        if (_images.Count >= MAX_IMAGES)
            return Result<EventImage>.Failure($"Event cannot have more than {MAX_IMAGES} images");

        // Calculate next display order (sequential from 1)
        var displayOrder = _images.Any() ? _images.Max(i => i.DisplayOrder) + 1 : 1;

        // Create and add image
        var eventImage = EventImage.Create(Id, imageUrl, blobName, displayOrder);
        _images.Add(eventImage);
        MarkAsUpdated();

        // Raise domain event
        RaiseDomainEvent(new ImageAddedToEventDomainEvent(Id, eventImage.Id, imageUrl));

        return Result<EventImage>.Success(eventImage);
    }

    /// <summary>
    /// Removes an image from the event gallery
    /// Automatically resequences remaining images
    /// </summary>
    public Result RemoveImage(Guid imageId)
    {
        var image = _images.FirstOrDefault(i => i.Id == imageId);
        if (image == null)
            return Result.Failure($"Image with ID {imageId} not found in this event");

        var blobName = image.BlobName;
        _images.Remove(image);

        // Resequence display orders after removal
        ResequenceDisplayOrders();
        MarkAsUpdated();

        // Raise domain event (includes BlobName for cleanup in handler)
        RaiseDomainEvent(new ImageRemovedFromEventDomainEvent(Id, imageId, blobName));

        return Result.Success();
    }

    /// <summary>
    /// Replaces an existing image with a new one, maintaining the same ID and display order
    /// Used for updating image content while preserving gallery position
    /// </summary>
    public Result<EventImage> ReplaceImage(Guid imageId, string newImageUrl, string newBlobName)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(newImageUrl))
            return Result<EventImage>.Failure("Image URL cannot be empty");

        if (string.IsNullOrWhiteSpace(newBlobName))
            return Result<EventImage>.Failure("Blob name cannot be empty");

        // Find existing image
        var existingImage = _images.FirstOrDefault(i => i.Id == imageId);
        if (existingImage == null)
            return Result<EventImage>.Failure($"Image not found with ID {imageId}");

        // Store old blob name for cleanup
        var oldBlobName = existingImage.BlobName;

        // Remove old image
        _images.Remove(existingImage);

        // Create new image with same ID and display order
        var newImage = EventImage.CreateWithId(
            existingImage.Id,
            Id,
            newImageUrl,
            newBlobName,
            existingImage.DisplayOrder
        );

        // Add new image
        _images.Add(newImage);
        MarkAsUpdated();

        // Raise domain event with old blob name for cleanup
        RaiseDomainEvent(new ImageReplacedInEventDomainEvent(Id, imageId, oldBlobName, newImageUrl));

        return Result<EventImage>.Success(newImage);
    }

    /// <summary>
    /// Reorders event images by providing new display orders
    /// Enforces sequential ordering starting from 1
    /// </summary>
    public Result ReorderImages(Dictionary<Guid, int> newOrders)
    {
        if (newOrders == null || newOrders.Count == 0)
            return Result.Failure("New display orders cannot be empty");

        // Invariant: All image IDs must belong to this event
        var invalidIds = newOrders.Keys.Except(_images.Select(i => i.Id)).ToList();
        if (invalidIds.Any())
            return Result.Failure("Cannot reorder images that don't belong to this event");

        // Invariant: Must provide orders for ALL images
        if (newOrders.Count != _images.Count)
            return Result.Failure("Must provide display orders for all images");

        // Invariant: Display orders must be sequential starting from 1
        var orders = newOrders.Values.OrderBy(o => o).ToList();
        if (orders.Count != orders.Distinct().Count())
            return Result.Failure("Display orders must be unique");

        if (orders.First() != 1 || orders.Last() != orders.Count)
            return Result.Failure("Display orders must be sequential starting from 1");

        // Apply new display orders
        foreach (var kvp in newOrders)
        {
            var image = _images.First(i => i.Id == kvp.Key);
            image.UpdateDisplayOrder(kvp.Value);
        }

        MarkAsUpdated();

        // Raise domain event
        RaiseDomainEvent(new ImagesReorderedDomainEvent(Id));

        return Result.Success();
    }

    /// <summary>
    /// Sets the specified image as the primary/main thumbnail for the event
    /// Automatically unmarks any previously primary image
    /// Phase 6A.13: Primary image selection feature
    /// </summary>
    public Result SetPrimaryImage(Guid imageId)
    {
        // Invariant: Image must belong to this event
        var image = _images.FirstOrDefault(i => i.Id == imageId);
        if (image == null)
            return Result.Failure($"Image with ID {imageId} not found in this event");

        // Check if image is already primary
        if (image.IsPrimary)
            return Result.Success(); // Already primary, no action needed

        // Business rule: Only one image can be primary
        // Find and unmark the currently primary image (if any)
        var currentPrimary = _images.FirstOrDefault(i => i.IsPrimary);
        if (currentPrimary != null)
        {
            currentPrimary.UnmarkAsPrimary();
        }

        // Mark the selected image as primary
        image.SetAsPrimary();
        MarkAsUpdated();

        // Raise domain event
        RaiseDomainEvent(new PrimaryImageSetDomainEvent(Id, imageId));

        return Result.Success();
    }

    /// <summary>
    /// Unmarks the currently primary image (if any) without setting a new primary
    /// Used for two-phase primary image switching to avoid unique constraint violations
    /// Phase 6A.13: Primary image selection feature - fix for EF Core update ordering
    /// </summary>
    public Result UnmarkCurrentPrimaryImage()
    {
        var currentPrimary = _images.FirstOrDefault(i => i.IsPrimary);
        if (currentPrimary != null)
        {
            currentPrimary.UnmarkAsPrimary();
            MarkAsUpdated();
        }
        // Success even if no primary exists - it's a valid state
        return Result.Success();
    }

    /// <summary>
    /// Marks the specified image as primary (assumes no other image is currently primary)
    /// Used for two-phase primary image switching to avoid unique constraint violations
    /// Phase 6A.13: Primary image selection feature - fix for EF Core update ordering
    /// </summary>
    public Result MarkImageAsPrimary(Guid imageId)
    {
        // Invariant: Image must belong to this event
        var image = _images.FirstOrDefault(i => i.Id == imageId);
        if (image == null)
            return Result.Failure($"Image with ID {imageId} not found in this event");

        // Check if image is already primary
        if (image.IsPrimary)
            return Result.Success(); // Already primary, no action needed

        // Mark the selected image as primary
        image.SetAsPrimary();
        MarkAsUpdated();

        // Raise domain event
        RaiseDomainEvent(new PrimaryImageSetDomainEvent(Id, imageId));

        return Result.Success();
    }

    #endregion

    #region Video Management

    /// <summary>
    /// Adds a video to the event's video gallery
    /// Videos are displayed in the order they are added (DisplayOrder)
    /// Maximum 3 videos per event
    /// </summary>
    public Result<EventVideo> AddVideo(
        string videoUrl,
        string blobName,
        string thumbnailUrl,
        string thumbnailBlobName,
        TimeSpan? duration,
        string format,
        long fileSizeBytes)
    {
        if (string.IsNullOrWhiteSpace(videoUrl))
            return Result<EventVideo>.Failure("Video URL cannot be empty");

        if (string.IsNullOrWhiteSpace(blobName))
            return Result<EventVideo>.Failure("Blob name cannot be empty");

        if (string.IsNullOrWhiteSpace(thumbnailUrl))
            return Result<EventVideo>.Failure("Thumbnail URL cannot be empty");

        if (string.IsNullOrWhiteSpace(thumbnailBlobName))
            return Result<EventVideo>.Failure("Thumbnail blob name cannot be empty");

        if (string.IsNullOrWhiteSpace(format))
            return Result<EventVideo>.Failure("Format cannot be empty");

        // Invariant: Max videos limit
        if (_videos.Count >= MAX_VIDEOS)
            return Result<EventVideo>.Failure($"Event cannot have more than {MAX_VIDEOS} videos (maximum limit reached)");

        // Calculate next display order (sequential from 1)
        var displayOrder = _videos.Any() ? _videos.Max(v => v.DisplayOrder) + 1 : 1;

        // Create and add video
        var eventVideo = EventVideo.Create(
            Id,
            videoUrl,
            blobName,
            thumbnailUrl,
            thumbnailBlobName,
            duration,
            format,
            fileSizeBytes,
            displayOrder);

        _videos.Add(eventVideo);
        MarkAsUpdated();

        // Raise domain event
        RaiseDomainEvent(new VideoAddedToEventDomainEvent(Id, eventVideo.Id, videoUrl));

        return Result<EventVideo>.Success(eventVideo);
    }

    /// <summary>
    /// Removes a video from the event gallery
    /// Automatically resequences remaining videos
    /// Both video blob and thumbnail blob names are included in domain event for cleanup
    /// </summary>
    public Result RemoveVideo(Guid videoId)
    {
        var video = _videos.FirstOrDefault(v => v.Id == videoId);
        if (video == null)
            return Result.Failure($"Video with ID {videoId} not found in this event");

        var videoBlobName = video.BlobName;
        var thumbnailBlobName = video.ThumbnailBlobName;
        _videos.Remove(video);

        // Resequence display orders after removal
        ResequenceVideoDisplayOrders();
        MarkAsUpdated();

        // Raise domain event (includes both blob names for cleanup in handler)
        RaiseDomainEvent(new VideoRemovedFromEventDomainEvent(Id, videoId, videoBlobName, thumbnailBlobName));

        return Result.Success();
    }

    #endregion

    /// <summary>
    /// Resequences image display orders to be sequential from 1
    /// Called after image removal to eliminate gaps
    /// </summary>
    private void ResequenceDisplayOrders()
    {
        var sortedImages = _images.OrderBy(i => i.DisplayOrder).ToList();
        for (int i = 0; i < sortedImages.Count; i++)
        {
            sortedImages[i].UpdateDisplayOrder(i + 1);
        }
    }

    /// <summary>
    /// Resequences video display orders to be sequential from 1
    /// Called after video removal to eliminate gaps
    /// </summary>
    private void ResequenceVideoDisplayOrders()
    {
        var sortedVideos = _videos.OrderBy(v => v.DisplayOrder).ToList();
        for (int i = 0; i < sortedVideos.Count; i++)
        {
            sortedVideos[i].UpdateDisplayOrder(i + 1);
        }
    }

    /// <summary>
    /// Checks if event has any images
    /// </summary>
    public bool HasImages() => _images.Any();

    /// <summary>
    /// Gets the number of images in the event gallery
    /// </summary>
    public int ImageCount() => _images.Count;

    #region Waiting List Management (Epic 2)

    /// <summary>
    /// Adds a user to the waiting list when event is at capacity
    /// </summary>
    public Result AddToWaitingList(Guid userId)
    {
        if (userId == Guid.Empty)
            return Result.Failure("User ID is required");

        // Business Rule 1: Event must be at capacity
        if (!IsAtCapacity())
            return Result.Failure("Event still has available capacity");

        // Business Rule 2: User cannot be already registered
        if (IsUserRegistered(userId))
            return Result.Failure("User is already registered for this event");

        // Business Rule 3: User cannot join waiting list twice
        if (_waitingList.Any(w => w.UserId == userId))
            return Result.Failure("User is already on the waiting list");

        // Add to end of waiting list
        var position = _waitingList.Count + 1;
        var entry = WaitingListEntry.Create(userId, position);
        _waitingList.Add(entry);
        MarkAsUpdated();

        // Raise domain event
        RaiseDomainEvent(new UserAddedToWaitingListDomainEvent(Id, userId, position, DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Removes a user from the waiting list
    /// Automatically resequences remaining positions
    /// </summary>
    public Result RemoveFromWaitingList(Guid userId)
    {
        var entry = _waitingList.FirstOrDefault(w => w.UserId == userId);
        if (entry == null)
            return Result.Failure("User is not on the waiting list");

        _waitingList.Remove(entry);
        ResequenceWaitingList();
        MarkAsUpdated();

        // Raise domain event
        RaiseDomainEvent(new UserRemovedFromWaitingListDomainEvent(Id, userId, DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Promotes a user from waiting list to confirmed registration
    /// Called when user accepts the spot that became available
    /// </summary>
    public Result PromoteFromWaitingList(Guid userId)
    {
        if (userId == Guid.Empty)
            return Result.Failure("User ID is required");

        // Business Rule: Must have capacity
        if (!HasCapacityFor(1))
            return Result.Failure("No capacity available to promote user");

        // Find user on waiting list
        var entry = _waitingList.FirstOrDefault(w => w.UserId == userId);
        if (entry == null)
            return Result.Failure("User is not on the waiting list");

        // Register the user
        var registerResult = Register(userId, 1);
        if (registerResult.IsFailure)
            return registerResult;

        // Remove from waiting list
        _waitingList.Remove(entry);
        ResequenceWaitingList();
        MarkAsUpdated();

        // Raise domain event
        RaiseDomainEvent(new UserPromotedFromWaitingListDomainEvent(Id, userId, DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Gets the user's position in the waiting list
    /// Returns 0 if user is not on the waiting list
    /// </summary>
    public int GetWaitingListPosition(Guid userId)
    {
        var entry = _waitingList.FirstOrDefault(w => w.UserId == userId);
        return entry?.Position ?? 0;
    }

    /// <summary>
    /// Checks if event is at full capacity
    /// </summary>
    public bool IsAtCapacity()
    {
        // Phase 6A.136C: Use ReservedCapacity (includes Preliminary) to prevent overbooking
        return ReservedCapacity >= Capacity;
    }

    /// <summary>
    /// Resequences waiting list positions after removal
    /// Ensures sequential ordering from 1
    /// </summary>
    private void ResequenceWaitingList()
    {
        var sortedEntries = _waitingList.OrderBy(w => w.Position).ToList();
        for (int i = 0; i < sortedEntries.Count; i++)
        {
            sortedEntries[i].UpdatePosition(i + 1);
        }
    }

    #endregion

    #region Pass Management

    /// <summary>
    /// Adds a pass/ticket type to the event
    /// </summary>
    public Result AddPass(EventPass eventPass)
    {
        if (eventPass == null)
            return Result.Failure("Event pass cannot be null");

        // Check for duplicate pass names
        if (_passes.Any(p => p.Name.Value.Equals(eventPass.Name.Value, StringComparison.OrdinalIgnoreCase)))
            return Result.Failure($"A pass with the name '{eventPass.Name}' already exists");

        _passes.Add(eventPass);
        MarkAsUpdated();

        // Raise domain event
        RaiseDomainEvent(new PassAddedToEventDomainEvent(Id, eventPass.Id, eventPass.Name, DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Removes a pass from the event
    /// Cannot remove if there are existing reservations
    /// </summary>
    public Result RemovePass(Guid passId)
    {
        var pass = _passes.FirstOrDefault(p => p.Id == passId);
        if (pass == null)
            return Result.Failure($"Pass with ID {passId} not found");

        // Business rule: Cannot remove pass with existing reservations
        if (pass.ReservedQuantity > 0)
            return Result.Failure("Cannot remove pass with existing reservations");

        _passes.Remove(pass);
        MarkAsUpdated();

        // Raise domain event
        RaiseDomainEvent(new PassRemovedFromEventDomainEvent(Id, passId, DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Gets a pass by ID
    /// </summary>
    public EventPass? GetPass(Guid passId)
    {
        return _passes.FirstOrDefault(p => p.Id == passId);
    }

    /// <summary>
    /// Checks if event has any passes configured
    /// </summary>
    public bool HasPasses() => _passes.Any();

    #endregion

    #region Sign-Up List Management

    /// <summary>
    /// Adds a sign-up list to the event for volunteers/items
    /// Example: Food sign-up where users can commit to bringing dishes
    /// </summary>
    public Result AddSignUpList(SignUpList signUpList)
    {
        if (signUpList == null)
            return Result.Failure("Sign-up list cannot be null");

        // Phase 7D.1: Uniqueness keys on (Kind, Category) so organizers can run an
        // Items list and a Volunteers list that happen to share a category label.
        if (_signUpLists.Any(s => s.Kind == signUpList.Kind
                                  && s.Category.Equals(signUpList.Category, StringComparison.OrdinalIgnoreCase)))
            return Result.Failure($"A sign-up list with category '{signUpList.Category}' already exists");

        _signUpLists.Add(signUpList);
        MarkAsUpdated();

        // Raise domain event
        RaiseDomainEvent(new SignUpListAddedToEventDomainEvent(Id, signUpList.Id, signUpList.Category, DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Removes a sign-up list from the event
    /// Cannot remove if there are existing commitments
    /// </summary>
    public Result RemoveSignUpList(Guid signUpListId)
    {
        var signUpList = _signUpLists.FirstOrDefault(s => s.Id == signUpListId);
        if (signUpList == null)
            return Result.Failure($"Sign-up list with ID {signUpListId} not found");

        // Business rule: Cannot remove sign-up list with existing commitments
        if (signUpList.HasCommitments())
            return Result.Failure("Cannot remove sign-up list with existing commitments");

        _signUpLists.Remove(signUpList);
        MarkAsUpdated();

        // Raise domain event
        RaiseDomainEvent(new SignUpListRemovedFromEventDomainEvent(Id, signUpListId, DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Cancels all signup commitments for a specific user across all sign-up lists in this event
    /// Phase 6A.28: Called during registration cancellation when user opts to delete commitments
    /// Properly restores remaining_quantity for each affected SignUpItem via domain logic
    /// </summary>
    /// <param name="userId">The ID of the user whose commitments should be cancelled</param>
    /// <returns>Result indicating success or failure with error details</returns>
    public Result CancelAllUserCommitments(Guid userId)
    {
        if (userId == Guid.Empty)
            return Result.Failure("User ID is required");

        var cancelledCount = 0;
        var errors = new List<string>();

        // Phase 6A.28 Issue 4 Fix: Track Open items to delete (user-created items should be removed entirely)
        var itemsToRemove = new List<(SignUpList signUpList, Guid itemId)>();

        foreach (var signUpList in _signUpLists)
        {
            foreach (var item in signUpList.Items)
            {
                // Check if this item has a commitment from the user
                if (item.Commitments.Any(c => c.UserId == userId))
                {
                    // Use domain method which properly restores remaining_quantity
                    var result = item.CancelCommitment(userId);

                    if (result.IsSuccess)
                    {
                        cancelledCount++;

                        // Phase 6A.28 Issue 4 Fix: If this is an Open item created by this user, mark for deletion
                        // Open items are user-owned, so when the user cancels, both commitment AND item should be removed
                        // This makes Open items consistent with the "Cancel Sign Up" button behavior
                        if (item.CreatedByUserId.HasValue && item.CreatedByUserId.Value == userId)
                        {
                            itemsToRemove.Add((signUpList, item.Id));
                        }
                    }
                    else
                    {
                        // Log error but continue processing other commitments
                        errors.Add($"Failed to cancel commitment for item '{item.ItemDescription}': {result.Error}");
                    }
                }
            }
        }

        // Phase 6A.28 Issue 4 Fix: Remove user-created Open items (commitment already cancelled above)
        foreach (var (signUpList, itemId) in itemsToRemove)
        {
            var removeResult = signUpList.RemoveItem(itemId);
            if (removeResult.IsFailure)
            {
                errors.Add($"Failed to remove Open item: {removeResult.Error}");
            }
        }

        if (cancelledCount > 0)
        {
            MarkAsUpdated();
        }

        // Return success if at least one commitment was cancelled, even if some failed
        if (cancelledCount > 0)
        {
            return Result.Success();
        }

        // No commitments found or all failed
        return errors.Any()
            ? Result.Failure($"Failed to cancel commitments: {string.Join("; ", errors)}")
            : Result.Success();
    }

    /// <summary>
    /// Gets a sign-up list by ID
    /// </summary>
    public SignUpList? GetSignUpList(Guid signUpListId)
    {
        return _signUpLists.FirstOrDefault(s => s.Id == signUpListId);
    }

    /// <summary>
    /// Gets a sign-up list by category
    /// </summary>
    public SignUpList? GetSignUpListByCategory(string category)
    {
        return _signUpLists.FirstOrDefault(s => s.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if event has any sign-up lists
    /// </summary>
    public bool HasSignUpLists() => _signUpLists.Any();

    #endregion

    #region Badge Management (Phase 6A.25)

    /// <summary>
    /// Assigns a badge to this event
    /// Business rules:
    /// - Same badge cannot be assigned twice
    /// - Maximum 3 badges per event
    /// Phase 6A.28: Added duration support
    /// </summary>
    /// <param name="badgeId">Badge ID to assign</param>
    /// <param name="assignedByUserId">User performing the assignment</param>
    /// <param name="durationDays">Duration override in days (null = use badge default or never expire)</param>
    /// <param name="maxDurationDays">Maximum duration from badge's DefaultDurationDays (for validation)</param>
    public Result<EventBadge> AssignBadge(
        Guid badgeId,
        Guid assignedByUserId,
        int? durationDays = null,
        int? maxDurationDays = null)
    {
        if (badgeId == Guid.Empty)
            return Result<EventBadge>.Failure("Badge ID is required");

        if (assignedByUserId == Guid.Empty)
            return Result<EventBadge>.Failure("Assigner user ID is required");

        // Business Rule 1: Cannot assign same badge twice
        if (_badges.Any(b => b.BadgeId == badgeId))
            return Result<EventBadge>.Failure("This badge is already assigned to the event");

        // Business Rule 2: Maximum badges limit
        if (_badges.Count >= MAX_BADGES)
            return Result<EventBadge>.Failure($"Event cannot have more than {MAX_BADGES} badges");

        var eventBadgeResult = EventBadge.Create(Id, badgeId, assignedByUserId, durationDays, maxDurationDays);
        if (eventBadgeResult.IsFailure)
            return Result<EventBadge>.Failure(eventBadgeResult.Error);

        _badges.Add(eventBadgeResult.Value);
        MarkAsUpdated();

        return Result<EventBadge>.Success(eventBadgeResult.Value);
    }

    /// <summary>
    /// Removes a badge from this event
    /// </summary>
    public Result RemoveBadge(Guid badgeId)
    {
        var eventBadge = _badges.FirstOrDefault(b => b.BadgeId == badgeId);
        if (eventBadge == null)
            return Result.Failure($"Badge with ID {badgeId} is not assigned to this event");

        _badges.Remove(eventBadge);
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Checks if event has any badges assigned
    /// </summary>
    public bool HasBadges() => _badges.Any();

    /// <summary>
    /// Checks if a specific badge is assigned to this event
    /// </summary>
    public bool HasBadge(Guid badgeId) => _badges.Any(b => b.BadgeId == badgeId);

    /// <summary>
    /// Gets the number of badges assigned to this event
    /// </summary>
    public int BadgeCount() => _badges.Count;

    #endregion

    #region Email Group Management

    /// <summary>
    /// Assigns email groups to this event for invitation distribution
    /// Business Rules:
    /// - Email groups must belong to the event organizer
    /// - Duplicate assignments are prevented
    /// - Can assign multiple groups at once
    /// Phase 6A.32: Email Groups Integration
    /// NOTE: _emailGroupEntities shadow navigation synced by infrastructure layer per ADR-009
    /// </summary>
    public Result AssignEmailGroups(IEnumerable<Guid> emailGroupIds)
    {
        if (emailGroupIds == null)
            return Result.Failure("Email group IDs cannot be null");

        var groupList = emailGroupIds.Where(id => id != Guid.Empty).Distinct().ToList();

        if (!groupList.Any())
            return Result.Success(); // No groups to assign

        // Add groups that aren't already assigned to domain list
        foreach (var groupId in groupList)
        {
            if (!_emailGroupIds.Contains(groupId))
            {
                _emailGroupIds.Add(groupId);
            }
        }

        // NOTE: _emailGroupEntities shadow navigation updated by infrastructure layer
        // See EventRepository.AddAsync/UpdateAsync - uses EF Core Entry API per ADR-009

        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Replaces all email groups with a new set
    /// Primary method for updating email groups during event editing
    /// Phase 6A.32: Email Groups Integration
    /// NOTE: _emailGroupEntities shadow navigation synced by infrastructure layer per ADR-009
    /// </summary>
    public Result SetEmailGroups(IEnumerable<Guid> emailGroupIds)
    {
        if (emailGroupIds == null)
            return Result.Failure("Email group IDs cannot be null");

        var groupList = emailGroupIds.Where(id => id != Guid.Empty).Distinct().ToList();

        // Update domain collection (used by business logic)
        _emailGroupIds.Clear();
        _emailGroupIds.AddRange(groupList);

        // NOTE: _emailGroupEntities shadow navigation updated by infrastructure layer
        // See EventRepository.AddAsync/UpdateAsync - uses EF Core Entry API per ADR-009

        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Removes specific email groups from this event
    /// Phase 6A.32: Email Groups Integration
    /// NOTE: _emailGroupEntities shadow navigation synced by infrastructure layer per ADR-009
    /// </summary>
    public Result RemoveEmailGroups(IEnumerable<Guid> emailGroupIds)
    {
        if (emailGroupIds == null)
            return Result.Failure("Email group IDs cannot be null");

        var groupList = emailGroupIds.Where(id => id != Guid.Empty).ToList();

        if (!groupList.Any())
            return Result.Success(); // No groups to remove

        // Remove from domain list
        foreach (var groupId in groupList)
        {
            _emailGroupIds.Remove(groupId);
        }

        // NOTE: _emailGroupEntities shadow navigation updated by infrastructure layer
        // See EventRepository - uses EF Core Entry API per ADR-009

        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Removes all email groups from this event
    /// Phase 6A.32: Email Groups Integration
    /// NOTE: _emailGroupEntities shadow navigation synced by infrastructure layer per ADR-009
    /// </summary>
    public Result ClearEmailGroups()
    {
        if (_emailGroupIds.Any())
        {
            _emailGroupIds.Clear();

            // NOTE: _emailGroupEntities shadow navigation updated by infrastructure layer
            // See EventRepository - uses EF Core Entry API per ADR-009

            MarkAsUpdated();
        }

        return Result.Success();
    }

    /// <summary>
    /// Phase 6A.33 FIX: Sync domain's email group ID list from loaded EF Core entities
    /// This method is called ONLY by infrastructure layer after loading event from database
    /// Does NOT mark entity as updated or raise events - this is a hydration concern
    /// Per ADR-009: Domain maintains List&lt;Guid&gt; for business logic, EF Core has shadow navigation for persistence
    /// Pattern mirrors User.SyncPreferredMetroAreaIdsFromEntities
    /// </summary>
    /// <param name="emailGroupIds">IDs extracted from loaded shadow navigation entities</param>
    internal void SyncEmailGroupIdsFromEntities(IEnumerable<Guid> emailGroupIds)
    {
        _emailGroupIds.Clear();
        _emailGroupIds.AddRange(emailGroupIds);
        // NOTE: Do NOT call MarkAsUpdated() - this is a read operation, not a modification
        // NOTE: Do NOT raise domain events - this is infrastructure hydration, not business operation
    }

    /// <summary>
    /// Checks if this event has any email groups assigned
    /// Phase 6A.32: Email Groups Integration
    /// </summary>
    public bool HasEmailGroups() => _emailGroupIds.Any();

    /// <summary>
    /// Gets the count of email groups assigned to this event
    /// Phase 6A.32: Email Groups Integration
    /// </summary>
    public int EmailGroupCount() => _emailGroupIds.Count;

    #endregion

    #region Organizer Contact Management

    /// <summary>
    /// Sets organizer contacts for event inquiries (supports multiple contacts).
    /// Business Rules:
    /// - If publishContact is true, at least one contact is required
    /// - Each contact must have a name and at least one contact method (email or phone)
    /// - Email format is validated per contact
    /// - If publishContact is false, all contacts are cleared (privacy)
    /// - First contact is automatically set as primary if none specified
    /// </summary>
    public Result SetOrganizerContacts(
        bool publishContact,
        List<(string name, string? email, string? phone, Guid? linkedUserId, bool isPrimary)> contacts)
    {
        if (publishContact)
        {
            if (contacts == null || contacts.Count == 0)
                return Result.Failure("At least one organizer contact is required when publishing");

            if (contacts.Count > MAX_ORGANIZER_CONTACTS)
                return Result.Failure($"Maximum {MAX_ORGANIZER_CONTACTS} organizer contacts allowed");

            // Validate each contact — respect isPrimary as provided (zero primaries allowed)
            var newContacts = new List<EventOrganizerContact>();
            for (var i = 0; i < contacts.Count; i++)
            {
                var (name, email, phone, linkedUserId, requestedPrimary) = contacts[i];
                var isPrimary = requestedPrimary;
                var contactResult = EventOrganizerContact.Create(
                    Id, name, email, phone, isPrimary, sortOrder: i, linkedUserId: linkedUserId);

                if (contactResult.IsFailure)
                    return Result.Failure($"Contact {i + 1}: {contactResult.Error}");

                newContacts.Add(contactResult.Value);
            }

            // Replace all contacts
            _organizerContacts.Clear();
            _organizerContacts.AddRange(newContacts);
            PublishOrganizerContact = true;
        }
        else
        {
            // Privacy: Clear all contacts when unpublishing
            _organizerContacts.Clear();
            PublishOrganizerContact = false;
        }

        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Backward-compatible overload: sets organizer contacts without linkedUserId.
    /// </summary>
    public Result SetOrganizerContacts(
        bool publishContact,
        List<(string name, string? email, string? phone)> contacts)
    {
        var contactsWithDefaults = contacts
            .Select(c => (c.name, c.email, c.phone, (Guid?)null, false))
            .ToList();
        return SetOrganizerContacts(publishContact, contactsWithDefaults);
    }

    /// <summary>
    /// Gets the primary organizer contact (or first contact if none marked primary)
    /// </summary>
    public EventOrganizerContact? GetPrimaryContact() =>
        _organizerContacts.FirstOrDefault(c => c.IsPrimary)
        ?? _organizerContacts.FirstOrDefault();

    /// <summary>
    /// Checks if organizer contact information is published and available
    /// </summary>
    public bool HasOrganizerContact() =>
        PublishOrganizerContact && _organizerContacts.Any();

    /// <summary>
    /// Phase 6A.133: Determines if the given user is an organizer of this event.
    /// Returns true if the user is the primary organizer (OrganizerId)
    /// or a co-organizer (linked via OrganizerContacts.LinkedUserId).
    /// </summary>
    public bool IsOrganizer(Guid userId)
    {
        if (userId == Guid.Empty) return false;

        // Primary organizer (original event creator)
        if (OrganizerId == userId) return true;

        // Co-organizer (linked via organizer contacts)
        return _organizerContacts.Any(c => c.LinkedUserId.HasValue && c.LinkedUserId.Value == userId);
    }

    /// <summary>
    /// Phase 6A.133: Gets all user IDs that are organizers of this event.
    /// Includes the primary OrganizerId and all linked co-organizer user IDs.
    /// </summary>
    public IReadOnlyList<Guid> GetAllOrganizerUserIds()
    {
        var ids = new List<Guid> { OrganizerId };
        ids.AddRange(_organizerContacts
            .Where(c => c.LinkedUserId.HasValue)
            .Select(c => c.LinkedUserId!.Value)
            .Where(id => id != OrganizerId)); // Avoid duplicates if primary is also linked
        return ids.AsReadOnly();
    }

    /// <summary>
    /// Phase 6A.133: Links an organizer contact to a user account, granting co-organizer access.
    /// Business rules:
    /// - Contact must exist on this event
    /// - Cannot link to the primary organizer (they already have full access via OrganizerId)
    /// - User cannot be linked to multiple contacts on the same event
    /// </summary>
    public Result LinkOrganizerContactToUser(Guid contactId, Guid userId)
    {
        if (userId == Guid.Empty)
            return Result.Failure("User ID is required");

        if (contactId == Guid.Empty)
            return Result.Failure("Contact ID is required");

        // Cannot link the primary organizer — they already have access
        if (userId == OrganizerId)
            return Result.Failure("The primary organizer already has full access to this event");

        var contact = _organizerContacts.FirstOrDefault(c => c.Id == contactId);
        if (contact == null)
            return Result.Failure("Organizer contact not found");

        // Check if this user is already linked to another contact on this event
        var existingLink = _organizerContacts.FirstOrDefault(c => c.LinkedUserId == userId);
        if (existingLink != null && existingLink.Id != contactId)
            return Result.Failure("User is already linked to another organizer contact on this event");

        var linkResult = contact.LinkToUser(userId);
        if (linkResult.IsFailure)
            return linkResult;

        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Phase 6A.133: Removes the user link from an organizer contact, revoking co-organizer access.
    /// The contact information remains, but the user loses management access.
    /// </summary>
    public Result UnlinkOrganizerContact(Guid contactId)
    {
        if (contactId == Guid.Empty)
            return Result.Failure("Contact ID is required");

        var contact = _organizerContacts.FirstOrDefault(c => c.Id == contactId);
        if (contact == null)
            return Result.Failure("Organizer contact not found");

        var unlinkResult = contact.UnlinkUser();
        if (unlinkResult.IsFailure)
            return unlinkResult;

        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Phase 6A.133: Batch-links multiple organizer contacts to user accounts in one operation.
    /// Validates all links before applying any (all-or-nothing).
    /// </summary>
    public Result BatchLinkOrganizerContacts(IList<(Guid contactId, Guid userId)> links)
    {
        if (links == null || links.Count == 0)
            return Result.Success();

        // Pre-validate: check for duplicate user IDs in the batch
        var userIds = links.Select(l => l.userId).ToList();
        if (userIds.Distinct().Count() != userIds.Count)
            return Result.Failure("Batch contains duplicate user IDs — each user can only be linked once");

        // Pre-validate each link before applying any
        foreach (var (contactId, userId) in links)
        {
            if (userId == Guid.Empty)
                return Result.Failure("User ID is required");

            if (contactId == Guid.Empty)
                return Result.Failure("Contact ID is required");

            if (userId == OrganizerId)
                return Result.Failure("The primary organizer already has full access to this event");

            var contact = _organizerContacts.FirstOrDefault(c => c.Id == contactId);
            if (contact == null)
                return Result.Failure($"Organizer contact not found for contact ID {contactId}");

            // Check for conflicts with existing links (not in this batch)
            var existingLink = _organizerContacts.FirstOrDefault(c =>
                c.LinkedUserId == userId && !links.Any(l => l.contactId == c.Id));
            if (existingLink != null)
                return Result.Failure($"User is already linked to another organizer contact on this event");
        }

        // All validations passed — apply all links
        foreach (var (contactId, userId) in links)
        {
            var result = LinkOrganizerContactToUser(contactId, userId);
            if (result.IsFailure)
                return result; // Should not happen after pre-validation, but safety net
        }

        return Result.Success();
    }

    #endregion

    #region Donation Configuration

    /// <summary>
    /// Sets or updates the donation configuration for this event.
    /// Organizer uses this to enable/configure donations.
    /// </summary>
    public Result SetDonationConfiguration(DonationConfiguration config)
    {
        if (config == null)
            return Result.Failure("Donation configuration is required");

        DonationConfig = config;
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Disables donations for this event by setting config to disabled state.
    /// </summary>
    public Result DisableDonations()
    {
        DonationConfig = DonationConfiguration.Disabled();
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Whether donations are currently enabled for this event.
    /// Returns false when DonationConfig is null (safe default for existing events).
    /// </summary>
    public bool AreDonationsEnabled() => DonationConfig?.IsEnabled == true;

    /// <summary>
    /// Validates a donation amount against the event's donation configuration.
    /// </summary>
    public Result ValidateDonationAmount(decimal amount)
    {
        if (!AreDonationsEnabled())
            return Result.Failure("Donations are not enabled for this event");

        return DonationConfig!.ValidateAmount(amount);
    }

    #endregion

    #region Collection Configuration

    /// <summary>
    /// Sets or updates the collection (event fund) configuration for this event.
    /// </summary>
    public Result SetCollectionConfiguration(CollectionConfiguration config)
    {
        if (config == null)
            return Result.Failure("Collection configuration is required");

        CollectionConfig = config;
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Disables collections for this event.
    /// </summary>
    public Result DisableCollections()
    {
        CollectionConfig = CollectionConfiguration.Disabled();
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Whether collections are currently enabled for this event.
    /// </summary>
    public bool AreCollectionsEnabled() => CollectionConfig?.IsEnabled == true;

    /// <summary>
    /// Validates a collection contribution amount against the event's collection configuration.
    /// </summary>
    public Result ValidateCollectionAmount(decimal amount)
    {
        if (!AreCollectionsEnabled())
            return Result.Failure("Collections are not enabled for this event");

        return CollectionConfig!.ValidateAmount(amount);
    }

    #endregion

    #region Sponsor Configuration

    /// <summary>
    /// Sets or updates the sponsor configuration for this event.
    /// </summary>
    public Result SetSponsorConfiguration(SponsorConfiguration config)
    {
        if (config == null)
            return Result.Failure("Sponsor configuration is required");

        SponsorConfig = config;
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Disables sponsorships for this event.
    /// </summary>
    public Result DisableSponsors()
    {
        SponsorConfig = SponsorConfiguration.Disabled();
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Whether sponsorships are currently enabled for this event.
    /// </summary>
    public bool AreSponsorsEnabled() => SponsorConfig?.IsEnabled == true;

    #endregion

    #region Add-on Configuration

    /// <summary>
    /// Sets or updates the add-on configuration for this event.
    /// </summary>
    public Result SetAddOnConfiguration(AddOnConfiguration config)
    {
        if (config == null)
            return Result.Failure("Add-on configuration is required");

        AddOnConfig = config;
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Disables add-ons for this event.
    /// </summary>
    public Result DisableAddOns()
    {
        AddOnConfig = AddOnConfiguration.Disabled();
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Whether add-ons are currently enabled for this event.
    /// </summary>
    public bool AreAddOnsEnabled() => AddOnConfig?.IsEnabled == true;

    #endregion

    /// <summary>
    /// Pricing-guard helper (architect-approved 2026-05-04). A paid event is well-formed
    /// when at least one of three pricing shapes is configured:
    ///   1. Legacy single/dual pricing — <see cref="Pricing"/> populated via
    ///      <c>SetDualPricing</c> / <c>SetGroupPricing</c>.
    ///   2. Legacy fixed ticket price — <see cref="TicketPrice"/>.
    ///   3. Tiered ticketing with at least one active tier — <see cref="HasTicketTiers"/>
    ///      (each tier carries its own AdultPrice / ChildPrice, so the registration's
    ///      tier-counts axis is the pricing source).
    ///
    /// Used by <see cref="CalculatePriceForAttendees"/> and
    /// <c>CalculateHeadCountPrice</c> (Event.RegistrationMode.cs) so both Mode A and
    /// Mode B treat the same event-shape as valid. Pre-fix: those guards checked only
    /// the legacy shapes and rejected paid+Tiered events lacking redundant Pricing
    /// (latent bug — see EventPaidPricingGuardTests).
    /// </summary>
    private bool HasPaidPricingConfigured() =>
        Pricing != null
        || TicketPrice != null
        || HasTicketTiers;
}