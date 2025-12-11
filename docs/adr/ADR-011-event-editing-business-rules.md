# ADR-011: Event Editing Business Rules

**Status:** Proposed
**Date:** 2025-11-30
**Decision Makers:** System Architecture Team
**Stakeholders:** Event Organizers, Event Attendees, Product Team

---

## Context and Problem Statement

The current implementation restricts event editing to only Draft status events (`UpdateEventCommandHandler.cs` line 29-30). However, event organizers need the ability to update published events for legitimate reasons (typo fixes, description updates, capacity increases, etc.). This creates a tension between:

1. **Data Integrity**: Preventing disruptive changes to events people have already registered for
2. **Flexibility**: Allowing organizers to manage their events effectively
3. **User Trust**: Maintaining consistency for attendees who have committed to attend

**Current Behavior:**
- Backend: Returns 400 error "Only draft events can be updated" for any non-draft event
- Frontend: Shows "Edit Event" button for all events regardless of status
- Result: UI/UX mismatch and organizer frustration

**Event Lifecycle:**
```
Draft → Published → Active → Completed
   ↓         ↓
UnderReview  ↓
   ↓         ↓
Published  Postponed/Cancelled
```

---

## Decision Drivers

### Business Requirements
- Event organizers must manage their events throughout the lifecycle
- Minor corrections (typos, description clarifications) should be possible after publishing
- Attendees expect stability in core event details (date, location, pricing)
- Community trust depends on consistent event information

### Industry Analysis
Competitive analysis of major event platforms:

| Platform | Draft Editing | Published Editing | Restrictions |
|----------|--------------|-------------------|--------------|
| **Eventbrite** | Full editing | Limited editing | Cannot change: Dates (if tickets sold), Pricing (for sold tickets), Location (major changes blocked) |
| **Meetup** | Full editing | Full editing with notifications | Sends update notifications to attendees for significant changes |
| **Facebook Events** | Full editing | Full editing | Automatically notifies attendees of changes |
| **Luma** | Full editing | Limited editing | Restricts date/time changes after RSVPs |

**Industry Standard:** Allow limited editing of published events with attendee notifications for significant changes.

### Technical Constraints
- Clean Architecture: Changes should be domain-driven with clear business rules
- DDD Approach: Event aggregate should enforce invariants
- Existing registration data must remain valid
- Domain events must notify attendees of changes

### Risk Factors
- **High Risk Changes**: Date, time, location, pricing, capacity reduction
- **Medium Risk Changes**: Category, ticket type changes
- **Low Risk Changes**: Description, images, minor details
- **Critical Invariant**: Cannot reduce capacity below current registrations (already enforced)

---

## Considered Options

### Option 1: Strict Draft-Only Editing (Current Implementation)

**Description:** Maintain status quo - only Draft events can be edited.

**Pros:**
- Simple implementation (already exists)
- No risk of disrupting attendee plans
- Clear boundaries - organizers know when editing is locked

**Cons:**
- Industry non-standard approach
- Forces organizers to cancel/republish for minor typos
- Poor user experience for legitimate use cases
- Creates duplicate events with attendee confusion
- No mechanism to fix mistakes after publishing

**Risk Assessment:** Low technical risk, HIGH business/UX risk

---

### Option 2: Full Editing with Notification System

**Description:** Allow full editing of any field, but send notifications to registered attendees for significant changes.

**Pros:**
- Maximum flexibility for organizers
- Matches industry leaders (Meetup, Facebook)
- Empowers organizers to manage events dynamically
- Clear communication with attendees

**Cons:**
- Requires notification infrastructure (email, push, in-app)
- Complex logic to determine "significant" vs "minor" changes
- Higher development effort (Epic-level feature)
- Risk of attendee confusion if over-used
- Potential attendee trust issues

**Risk Assessment:** Medium technical risk, Medium business risk

---

### Option 3: Status-Based Edit Permissions (RECOMMENDED)

**Description:** Different editing capabilities based on event status, with field-level restrictions.

**Edit Matrix:**

| Status | Editable Fields | Restricted Fields | Invariants |
|--------|----------------|-------------------|------------|
| **Draft** | All fields | None | Standard validations |
| **UnderReview** | None (locked) | All | Awaiting admin approval |
| **Published** | Description, Images, Videos, Capacity (increase only), Tags | Title, Category, StartDate, EndDate, Location, Pricing | Cannot reduce capacity below registrations |
| **Active** | Description, Images, Videos | All core fields | Event is in progress |
| **Postponed** | StartDate, EndDate, Description | Capacity, Pricing, Location (major changes) | Requires republishing |
| **Cancelled** | None | All | Final state |
| **Completed** | None | All | Historical record |
| **Archived** | None | All | Read-only |

**Business Rules:**

1. **Draft Events:**
   - Full editing allowed
   - No attendee notifications needed (no registrations yet)
   - Command: `UpdateEventCommand` (existing)

2. **Published Events:**
   - **Allowed Changes:**
     - Description (clarifications, additional details)
     - Event images and videos
     - Capacity increases (never decreases)
     - Category (with warning in UI)
   - **Prohibited Changes:**
     - Title (brand/marketing consistency)
     - Start/End dates (attendee scheduling commitments)
     - Location (attendee travel plans)
     - Ticket pricing (financial commitments)
   - Command: `UpdatePublishedEventCommand` (new)

3. **Active Events:**
   - Only cosmetic updates (description, media)
   - No structural changes
   - Command: Reuse `UpdatePublishedEventCommand` with stricter validation

4. **Postponed Events:**
   - Allow date rescheduling only
   - Requires transition back to Published status
   - Command: `ReschedulePostponedEventCommand` (future)

5. **Terminal States (Cancelled, Completed, Archived):**
   - Read-only
   - No editing allowed
   - Maintains historical accuracy

**Pros:**
- Balanced approach between flexibility and stability
- Clear business rules aligned with event lifecycle
- Industry-standard restrictions
- Incremental implementation (Phase-based)
- Protects attendee trust while enabling organizers
- Low risk for existing registrations

**Cons:**
- More complex than Option 1
- Requires domain model updates
- Multiple command handlers needed
- UI must enforce field-level restrictions

**Risk Assessment:** Low-Medium technical risk, LOW business risk

---

### Option 4: Notification-Based Approval for Critical Changes

**Description:** Allow editing with admin approval for critical fields (dates, location, pricing).

**Pros:**
- Maximum safety for attendees
- Quality control for significant changes
- Audit trail for changes

**Cons:**
- Introduces admin bottleneck
- Slower organizer workflow
- Requires admin notification system
- Higher operational cost

**Risk Assessment:** Medium technical risk, Medium operational risk

---

## Decision Outcome

**Chosen Option:** **Option 3 - Status-Based Edit Permissions**

### Rationale

1. **Industry Alignment:** Matches standard practices (Eventbrite, Luma model)
2. **User Experience:** Balances organizer flexibility with attendee trust
3. **Risk Management:** Protects critical commitments (dates, location, pricing) while allowing non-disruptive updates
4. **Incremental Implementation:** Can be implemented in phases without major refactoring
5. **Clean Architecture Fit:** Aligns with DDD - status is part of aggregate state, rules are domain invariants
6. **Low Business Risk:** Prevents the "typo cancel-republish spiral" while protecting attendee commitments

### Implementation Approach

#### Phase 1: Published Event Limited Editing (Immediate - Current Session)

**Scope:**
- Create `UpdatePublishedEventCommand` and handler
- Allow editing: Description, Capacity (increase only), Category, Images
- Restrict: Title, Dates, Location, Pricing
- Update UI to show/hide fields based on status
- Add validation messages for restricted fields

**Deliverables:**
1. New command handler: `UpdatePublishedEventCommandHandler.cs`
2. Domain method: `Event.UpdatePublishedDetails()`
3. UI updates: `EventEditForm.tsx` - conditional field rendering
4. Integration tests for edit scenarios
5. Update API endpoint validation

#### Phase 2: Active Event Editing (Future)

**Scope:**
- Restrict Active events to cosmetic updates only
- Description and media only

#### Phase 3: Postponed Event Rescheduling (Future)

**Scope:**
- Allow date changes for Postponed events
- Workflow to republish after rescheduling

#### Phase 4: Notification System (Epic - Future)

**Scope:**
- Email notifications for capacity increases
- In-app notifications for description changes
- Audit log for event modifications

---

## Architectural Design

### Domain Model Changes

```csharp
// Event.cs - New domain methods

/// <summary>
/// Updates fields allowed for published events
/// Enforces status-based editing rules
/// </summary>
public Result UpdatePublishedDetails(
    EventDescription? description = null,
    int? newCapacity = null,
    EventCategory? category = null)
{
    if (Status != EventStatus.Published && Status != EventStatus.Active)
        return Result.Failure("Only published or active events can be updated with this method");

    // Description update (low risk)
    if (description != null)
    {
        Description = description;
    }

    // Capacity increase only (medium risk - must increase, never decrease)
    if (newCapacity.HasValue)
    {
        if (newCapacity.Value < Capacity)
            return Result.Failure("Cannot reduce capacity for published events");

        if (newCapacity.Value < CurrentRegistrations)
            return Result.Failure("Capacity cannot be less than current registrations");

        var previousCapacity = Capacity;
        Capacity = newCapacity.Value;

        // Raise domain event for notification system (future)
        RaiseDomainEvent(new EventCapacityIncreasedEvent(Id, previousCapacity, newCapacity.Value, DateTime.UtcNow));
    }

    // Category update (medium risk - allowed but logged)
    if (category.HasValue && category.Value != Category)
    {
        var previousCategory = Category;
        Category = category.Value;

        // Raise domain event for audit trail
        RaiseDomainEvent(new EventCategoryChangedEvent(Id, previousCategory, category.Value, DateTime.UtcNow));
    }

    MarkAsUpdated();
    return Result.Success();
}

/// <summary>
/// Checks if a field is editable based on current status
/// </summary>
public bool IsFieldEditable(string fieldName)
{
    return Status switch
    {
        EventStatus.Draft => true, // All fields editable
        EventStatus.Published or EventStatus.Active => fieldName switch
        {
            nameof(Description) => true,
            nameof(Capacity) => true, // Only increases
            nameof(Category) => true,
            _ => false
        },
        _ => false // No editing for other statuses
    };
}
```

### Application Layer Changes

```csharp
// UpdatePublishedEventCommand.cs
public class UpdatePublishedEventCommand : ICommand
{
    public Guid EventId { get; init; }
    public string? Description { get; init; }
    public int? Capacity { get; init; }
    public EventCategory? Category { get; init; }
    // Explicitly omit: Title, Dates, Location, Pricing
}

// UpdatePublishedEventCommandHandler.cs
public class UpdatePublishedEventCommandHandler : ICommandHandler<UpdatePublishedEventCommand>
{
    public async Task<Result> Handle(UpdatePublishedEventCommand request, CancellationToken cancellationToken)
    {
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result.Failure("Event not found");

        // Verify status allows editing
        if (@event.Status != EventStatus.Published && @event.Status != EventStatus.Active)
            return Result.Failure("Only published or active events can be updated");

        // Build value objects
        EventDescription? description = null;
        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            var descResult = EventDescription.Create(request.Description);
            if (descResult.IsFailure)
                return Result.Failure(descResult.Error);
            description = descResult.Value;
        }

        // Call domain method
        var updateResult = @event.UpdatePublishedDetails(
            description,
            request.Capacity,
            request.Category
        );

        if (updateResult.IsFailure)
            return updateResult;

        await _unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
```

### UI Changes

```typescript
// EventEditForm.tsx - Conditional field rendering

const isPublished = event.status === EventStatus.Published ||
                    event.status === EventStatus.Active;
const isDraft = event.status === EventStatus.Draft;

// Render logic
{isDraft && (
  <Input
    label="Event Title"
    value={formData.title}
    onChange={(e) => setFormData({...formData, title: e.target.value})}
  />
)}

{isPublished && (
  <div className="p-4 bg-gray-100 rounded">
    <label className="text-sm font-semibold text-gray-700">Event Title</label>
    <p className="text-gray-900">{event.title}</p>
    <p className="text-xs text-gray-500 mt-1">
      Title cannot be changed after publishing
    </p>
  </div>
)}

// Capacity with increase-only validation
{isPublished && (
  <Input
    label="Event Capacity"
    type="number"
    value={formData.capacity}
    min={event.currentRegistrations}
    onChange={(e) => setFormData({...formData, capacity: parseInt(e.target.value)})}
    helpText={`Cannot be less than current registrations (${event.currentRegistrations}). Current: ${event.capacity}`}
  />
)}
```

---

## Consequences

### Positive

1. **Improved UX:** Organizers can fix typos and update descriptions without republishing
2. **Industry Standard:** Aligns with Eventbrite and Luma editing models
3. **Attendee Trust:** Core commitments (dates, location, pricing) remain stable
4. **Flexibility:** Capacity increases allow events to scale up based on demand
5. **Clean Architecture:** Domain-driven rules with clear invariants
6. **Incremental Rollout:** Can implement in phases without breaking existing functionality

### Negative

1. **Increased Complexity:** Multiple command handlers and validation rules
2. **UI Complexity:** Conditional rendering based on status
3. **Testing Overhead:** More test scenarios for different status states
4. **Future Dependency:** Full value requires notification system (Phase 4)

### Neutral

1. **Migration:** No data migration needed - uses existing event structure
2. **Performance:** No significant performance impact
3. **Backward Compatibility:** Existing `UpdateEventCommand` remains for draft events

---

## Validation and Testing

### Test Scenarios

```csharp
// Domain Tests
[Fact]
public void UpdatePublishedDetails_WhenPublished_AllowsDescriptionUpdate()
{
    // Arrange
    var @event = CreatePublishedEvent();
    var newDescription = EventDescription.Create("Updated description").Value;

    // Act
    var result = @event.UpdatePublishedDetails(description: newDescription);

    // Assert
    result.IsSuccess.Should().BeTrue();
    @event.Description.Should().Be(newDescription);
}

[Fact]
public void UpdatePublishedDetails_WhenPublished_PreventsCapacityDecrease()
{
    // Arrange
    var @event = CreatePublishedEventWithRegistrations(capacity: 100, registered: 50);

    // Act
    var result = @event.UpdatePublishedDetails(newCapacity: 80);

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Should().Contain("Cannot reduce capacity");
}

[Fact]
public void UpdatePublishedDetails_WhenDraft_ReturnsFailure()
{
    // Arrange
    var @event = CreateDraftEvent();

    // Act
    var result = @event.UpdatePublishedDetails(newCapacity: 200);

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Should().Contain("Only published or active events");
}

// Integration Tests
[Fact]
public async Task UpdatePublishedEvent_ValidRequest_ReturnsSuccess()
{
    // Arrange
    var eventId = await CreatePublishedEvent();
    var command = new UpdatePublishedEventCommand
    {
        EventId = eventId,
        Description = "Updated description",
        Capacity = 200
    };

    // Act
    var result = await _mediator.Send(command);

    // Assert
    result.IsSuccess.Should().BeTrue();
}
```

### Manual Testing Checklist

- [ ] Draft event: All fields editable
- [ ] Published event: Description editable
- [ ] Published event: Capacity increase works
- [ ] Published event: Capacity decrease blocked
- [ ] Published event: Title field read-only
- [ ] Published event: Date fields read-only
- [ ] Published event: Location fields read-only
- [ ] Published event: Pricing fields read-only
- [ ] Active event: Limited fields editable
- [ ] Cancelled event: No editing allowed
- [ ] UI shows appropriate error messages
- [ ] API returns correct validation errors

---

## Compliance and Security

### Security Considerations

1. **Authorization:** Only event organizer can edit their events (existing middleware)
2. **Input Validation:** All inputs validated via value objects (existing pattern)
3. **Audit Trail:** Domain events log all changes (existing infrastructure)
4. **Data Integrity:** Invariants prevent invalid state transitions

### Compliance

1. **GDPR:** No PII changes in this feature
2. **Audit Requirements:** Domain events provide complete audit trail
3. **Business Rules:** All changes respect event lifecycle state machine

---

## References

### Industry Research
- [Eventbrite Help: Editing Published Events](https://www.eventbrite.com/help/en-us/articles/646207/how-do-i-edit-my-event/)
- [Meetup Help: Edit Event Details](https://help.meetup.com/hc/en-us/articles/360002882311-Editing-event-details)
- [Luma Knowledge Base: Event Management](https://lu.ma/help)

### Related ADRs
- [ADR-001: Clean Architecture Implementation](./ADR-001-clean-architecture.md)
- [ADR-003: Domain-Driven Design](./ADR-003-domain-driven-design.md)
- [ADR-007: Database Migration Safety](./ADR-007-database-migration-safety.md)

### Related Documentation
- [Event Domain Model](../architecture/Event-Domain-Model.md)
- [Event Lifecycle State Machine](../architecture/Event-Lifecycle.md)
- [Master Requirements Specification](../Master%20Requirements%20Specification.md)

---

## Implementation Roadmap

### Phase 1: Core Published Event Editing (Current Session)
**Timeline:** Immediate
**Effort:** 4-6 hours
**Priority:** HIGH

**Tasks:**
1. Create `UpdatePublishedEventCommand` and handler
2. Add `Event.UpdatePublishedDetails()` domain method
3. Add domain events for capacity/category changes
4. Update UI to conditionally render fields
5. Write unit and integration tests
6. Update API documentation

### Phase 2: Active Event Editing
**Timeline:** Future sprint
**Effort:** 2-3 hours
**Priority:** MEDIUM

**Tasks:**
1. Add Active status validation to handler
2. Further restrict editable fields
3. Update UI for Active events
4. Add tests

### Phase 3: Postponed Event Rescheduling
**Timeline:** Future epic
**Effort:** 8-12 hours
**Priority:** LOW

**Tasks:**
1. Create `ReschedulePostponedEventCommand`
2. Implement workflow for republishing
3. Add UI for rescheduling
4. Consider notification system integration

### Phase 4: Attendee Notification System
**Timeline:** Future epic
**Effort:** 20-40 hours
**Priority:** MEDIUM (long-term)

**Tasks:**
1. Design notification architecture
2. Implement email notification service
3. Create notification templates
4. Add in-app notifications
5. User preferences for notifications
6. Integration with event change domain events

---

## Approval and Sign-Off

**Architect Recommendation:** ✅ APPROVED (Option 3)
**Product Owner:** [Pending]
**Technical Lead:** [Pending]
**Security Review:** [Pending]

---

## Appendix A: Field-Level Edit Matrix

| Field | Draft | Published | Active | Postponed | Cancelled | Completed | Archived |
|-------|-------|-----------|--------|-----------|-----------|-----------|----------|
| Title | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Description | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| StartDate | ✅ | ❌ | ❌ | ✅ (reschedule) | ❌ | ❌ | ❌ |
| EndDate | ✅ | ❌ | ❌ | ✅ (reschedule) | ❌ | ❌ | ❌ |
| Location | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Capacity | ✅ | ✅ (increase only) | ⚠️ (increase only) | ❌ | ❌ | ❌ | ❌ |
| Category | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| TicketPrice | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Images | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| Videos | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |

**Legend:**
- ✅ = Fully editable
- ⚠️ = Partially editable (with restrictions)
- ❌ = Not editable (read-only)

---

## Appendix B: Domain Event Specifications

### New Domain Events

```csharp
// EventCapacityIncreasedEvent.cs
public record EventCapacityIncreasedEvent(
    Guid EventId,
    int PreviousCapacity,
    int NewCapacity,
    DateTime OccurredAt
) : IDomainEvent;

// EventCategoryChangedEvent.cs
public record EventCategoryChangedEvent(
    Guid EventId,
    EventCategory PreviousCategory,
    EventCategory NewCategory,
    DateTime OccurredAt
) : IDomainEvent;

// EventDescriptionUpdatedEvent.cs
public record EventDescriptionUpdatedEvent(
    Guid EventId,
    DateTime OccurredAt
) : IDomainEvent;
```

### Event Handler Responsibilities (Future)

```csharp
// EventCapacityIncreasedEventHandler.cs
// - Send email to attendees: "Good news! More spots available"
// - Log audit entry
// - Update analytics

// EventCategoryChangedEventHandler.cs
// - Log audit entry for category change
// - Update search index
// - Notify admin of category change (monitoring)
```

---

**Document Version:** 1.0
**Last Updated:** 2025-11-30
**Next Review:** After Phase 1 implementation
