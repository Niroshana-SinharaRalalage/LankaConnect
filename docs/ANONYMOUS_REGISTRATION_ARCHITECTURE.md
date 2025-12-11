# Anonymous Event Registration - Architectural Design Document

**Date:** 2025-12-01
**Status:** Design Proposal
**Architecture Pattern:** DDD with Clean Architecture
**Target:** LankaConnect Event Management System

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Current State Analysis](#2-current-state-analysis)
3. [Requirements Analysis](#3-requirements-analysis)
4. [Architectural Decision Records](#4-architectural-decision-records)
5. [Domain Model Design](#5-domain-model-design)
6. [Database Schema Design](#6-database-schema-design)
7. [API Contract Design](#7-api-contract-design)
8. [Migration Strategy](#8-migration-strategy)
9. [Implementation Roadmap](#9-implementation-roadmap)
10. [Risk Analysis & Mitigation](#10-risk-analysis--mitigation)

---

## 1. Executive Summary

### 1.1 Problem Statement

The current registration system requires authenticated users with valid UserId (Guid), preventing anonymous users from registering for events. This creates a barrier to entry and reduces event participation.

### 1.2 Proposed Solution

Implement a **hybrid registration model** using nullable UserId with attendee information value objects, maintaining the existing Registration entity while supporting both authenticated and anonymous users.

### 1.3 Key Architectural Decisions

| Decision | Rationale |
|----------|-----------|
| **Option A: Nullable UserId + AttendeeInfo Value Object** | Best balance of domain integrity, simplicity, and backward compatibility |
| Single Registration table with optional UserId | Maintains aggregate cohesion, simplifies queries |
| AttendeeInfo value object embedded as JSONB | PostgreSQL native support, flexible schema, easy querying |
| Sign-up list registration after event registration | Clear user flow, enforces event commitment first |

---

## 2. Current State Analysis

### 2.1 Existing Domain Model

```csharp
// Current Registration Entity
public class Registration : BaseEntity
{
    public Guid EventId { get; private set; }
    public Guid UserId { get; private set; }  // ❌ Currently required (non-nullable)
    public int Quantity { get; private set; }
    public RegistrationStatus Status { get; private set; }

    // Registration lifecycle: Pending → Confirmed → CheckedIn → Completed
}
```

### 2.2 Current Constraints

1. **UserId is required** - Prevents anonymous registrations
2. **No attendee information storage** - Relies on User aggregate for details
3. **Event.Register() requires userId** - Validation prevents Guid.Empty
4. **Database configuration enforces UserId** - FK constraint, unique index

### 2.3 Current Event Registration Flow

```
User authenticates → Event.Register(userId) → Registration.Create(eventId, userId, quantity)
                                             → Validation: userId != Guid.Empty
                                             → Add to _registrations collection
```

---

## 3. Requirements Analysis

### 3.1 Functional Requirements

#### FR-1: Anonymous Registration
**Priority:** P0 (Critical)

Anonymous users must be able to register by providing:
- Name (required, string, 2-100 characters)
- Age (required, integer, 1-150)
- Address (required, string, 10-500 characters)
- Email (required, valid email format)
- Phone (required, valid phone format)

#### FR-2: Authenticated User Registration
**Priority:** P0 (Critical)

Authenticated users:
- Auto-fill details from User profile
- Register with single click (no form required)
- UserId links to existing User aggregate

#### FR-3: UI Changes
**Priority:** P1 (High)

- Remove "Manage Sign-ups" button from event detail page
- Show event registration first, then sign-up lists

#### FR-4: Sign-up List Access
**Priority:** P2 (Medium)

- Users can register for sign-up lists AFTER event registration
- Sign-up lists available on separate page/section
- Anonymous users can participate in sign-ups

### 3.2 Non-Functional Requirements

#### NFR-1: Data Integrity
- Anonymous registrations must not corrupt existing data
- Email uniqueness per event (prevent duplicate anonymous registrations)
- Graceful handling of partial data

#### NFR-2: Performance
- No performance degradation for existing authenticated registrations
- Efficient queries for mixed registration types
- Index optimization for email-based lookups

#### NFR-3: Security
- Email validation and sanitization
- Rate limiting for anonymous registrations
- CAPTCHA for spam prevention

#### NFR-4: Privacy
- Anonymous data stored separately from authenticated users
- GDPR compliance for anonymous data retention
- Clear data ownership boundaries

---

## 4. Architectural Decision Records

### ADR-001: Domain Model Approach

**Status:** RECOMMENDED ✅

**Decision:** Use nullable UserId with AttendeeInfo value object (Option A)

**Context:**
We need to support both authenticated and anonymous registrations within the same Registration entity while maintaining DDD principles.

**Options Considered:**

#### Option A: Nullable UserId + AttendeeInfo Value Object ✅ RECOMMENDED

```csharp
public class Registration : BaseEntity
{
    public Guid EventId { get; private set; }
    public Guid? UserId { get; private set; }  // ✅ Nullable for anonymous
    public AttendeeInfo? AttendeeInfo { get; private set; }  // For anonymous users
    public int Quantity { get; private set; }
    public RegistrationStatus Status { get; private set; }

    // Business rule: Either UserId OR AttendeeInfo must be provided
    public bool IsAnonymous => UserId == null;
}

public record AttendeeInfo(
    string Name,
    int Age,
    string Address,
    Email Email,
    PhoneNumber Phone
)
{
    public static Result<AttendeeInfo> Create(string name, int age,
        string address, string email, string phone)
    {
        // Validation logic
    }
}
```

**Advantages:**
- ✅ Single source of truth for registrations
- ✅ Maintains aggregate cohesion within Event
- ✅ Simple queries: `WHERE EventId = @eventId`
- ✅ Backward compatible with existing code
- ✅ Clear domain invariant: `UserId XOR AttendeeInfo`
- ✅ Leverages PostgreSQL JSONB for AttendeeInfo

**Disadvantages:**
- ⚠️ Nullable UserId breaks some existing assumptions
- ⚠️ Requires migration to add nullable column
- ⚠️ Mixed data structure (relational + JSONB)

---

#### Option B: Separate AnonymousRegistration Entity ❌ NOT RECOMMENDED

```csharp
public class Registration : BaseEntity  // For authenticated users
{
    public Guid EventId { get; private set; }
    public Guid UserId { get; private set; }  // Non-nullable
    public int Quantity { get; private set; }
    public RegistrationStatus Status { get; private set; }
}

public class AnonymousRegistration : BaseEntity  // For anonymous users
{
    public Guid EventId { get; private set; }
    public AttendeeInfo AttendeeInfo { get; private set; }
    public int Quantity { get; private set; }
    public RegistrationStatus Status { get; private set; }
}
```

**Advantages:**
- ✅ Clear separation of concerns
- ✅ No nullable UserId
- ✅ Independent evolution of each type

**Disadvantages:**
- ❌ Duplicate logic for status management
- ❌ Complex queries: `UNION` of two tables
- ❌ Event aggregate must manage two collections
- ❌ Capacity calculations require joining tables
- ❌ More code duplication
- ❌ Violates DRY principle

---

#### Option C: Inheritance Hierarchy ❌ NOT RECOMMENDED

```csharp
public abstract class RegistrationBase : BaseEntity
{
    public Guid EventId { get; private set; }
    public int Quantity { get; private set; }
    public RegistrationStatus Status { get; private set; }
}

public class AuthenticatedRegistration : RegistrationBase
{
    public Guid UserId { get; private set; }
}

public class AnonymousRegistration : RegistrationBase
{
    public AttendeeInfo AttendeeInfo { get; private set; }
}
```

**Advantages:**
- ✅ Polymorphic behavior
- ✅ Type safety at compile time

**Disadvantages:**
- ❌ EF Core TPH/TPT complexity
- ❌ Discriminator column required
- ❌ Performance overhead for polymorphic queries
- ❌ Aggregate must manage polymorphic collections
- ❌ Over-engineering for simple use case

---

**Decision Rationale:**

Option A (Nullable UserId + AttendeeInfo) is selected because:

1. **Domain Integrity**: Single Registration concept in domain model
2. **Simplicity**: One entity, one table, straightforward queries
3. **PostgreSQL Strengths**: JSONB perfectly suited for AttendeeInfo
4. **Backward Compatibility**: Minimal changes to Event aggregate
5. **Query Performance**: Single table queries, no JOINs/UNIONs
6. **Maintainability**: Less code duplication, single status management

---

### ADR-002: Data Storage Strategy

**Status:** RECOMMENDED ✅

**Decision:** Store AttendeeInfo as JSONB column in Registration table

**Context:**
Anonymous attendee information is semi-structured and may evolve over time (e.g., adding emergency contact, dietary restrictions).

**Options Considered:**

#### Option A: JSONB Column ✅ RECOMMENDED

```sql
CREATE TABLE events.registrations (
    id UUID PRIMARY KEY,
    event_id UUID NOT NULL,
    user_id UUID NULL,  -- Nullable for anonymous
    attendee_info JSONB NULL,  -- Null for authenticated users
    quantity INTEGER NOT NULL,
    status VARCHAR(20) NOT NULL,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP NULL,

    -- Business rule: Either user_id OR attendee_info must be set
    CONSTRAINT chk_registration_identity
        CHECK ((user_id IS NOT NULL AND attendee_info IS NULL) OR
               (user_id IS NULL AND attendee_info IS NOT NULL))
);
```

**Advantages:**
- ✅ PostgreSQL native JSONB support with GIN indexing
- ✅ Flexible schema evolution (add fields without migration)
- ✅ Efficient querying: `attendee_info->>'email'`
- ✅ No additional tables
- ✅ Atomic updates

**Disadvantages:**
- ⚠️ JSONB structure not enforced at DB level
- ⚠️ Application-level validation required

---

#### Option B: Separate AnonymousAttendee Table ❌ NOT RECOMMENDED

```sql
CREATE TABLE events.anonymous_attendees (
    id UUID PRIMARY KEY,
    registration_id UUID REFERENCES registrations(id),
    name VARCHAR(100) NOT NULL,
    age INTEGER NOT NULL,
    address TEXT NOT NULL,
    email VARCHAR(255) NOT NULL,
    phone VARCHAR(20) NOT NULL
);
```

**Advantages:**
- ✅ Strongly typed columns
- ✅ Database-level constraints

**Disadvantages:**
- ❌ Requires JOIN for every query
- ❌ Additional table maintenance
- ❌ More complex schema
- ❌ Schema migrations for field changes

---

**Decision Rationale:**

JSONB column is selected because:
1. PostgreSQL JSONB is production-proven for semi-structured data
2. Single table keeps aggregate cohesive
3. Flexible for future enhancements (custom fields per event)
4. Excellent query performance with GIN indexes
5. Simpler application code (no ORM mapping for separate table)

---

### ADR-003: Sign-up List Access Flow

**Status:** RECOMMENDED ✅

**Decision:** Require event registration before sign-up list access

**Context:**
Sign-up lists (volunteer commitments, food items) are secondary to event attendance.

**Flow:**

```
1. User views Event Details page
   ↓
2. User registers for event (anonymous or authenticated)
   ↓
3. Registration confirmed
   ↓
4. User can now access Sign-up Lists for this event
   ↓
5. User commits to sign-up items
```

**Business Rules:**
- Event registration is prerequisite for sign-up list access
- Anonymous users can participate in sign-ups using same AttendeeInfo
- Sign-up commitments linked to Registration.Id (not UserId)

**Advantages:**
- ✅ Clear user journey
- ✅ Ensures commitment to event attendance
- ✅ Simplifies authorization (check Registration.Id)
- ✅ Prevents orphaned sign-ups

---

### ADR-004: Email Uniqueness Constraint

**Status:** RECOMMENDED ✅

**Decision:** Enforce email uniqueness per event for anonymous registrations

**Rationale:**
Prevent duplicate anonymous registrations from same person using different names.

```sql
-- Partial unique index (PostgreSQL feature)
CREATE UNIQUE INDEX idx_registrations_event_email_unique
ON events.registrations (event_id, (attendee_info->>'email'))
WHERE user_id IS NULL;  -- Only for anonymous registrations
```

**Benefits:**
- Prevents duplicate anonymous registrations
- Allows same email for authenticated users (linked to UserId)
- Efficient partial index (only anonymous records)

---

## 5. Domain Model Design

### 5.1 Updated Registration Entity

```csharp
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Events;

public class Registration : BaseEntity
{
    public Guid EventId { get; private set; }

    /// <summary>
    /// User ID for authenticated registrations.
    /// Null for anonymous registrations.
    /// </summary>
    public Guid? UserId { get; private set; }

    /// <summary>
    /// Attendee information for anonymous registrations.
    /// Null for authenticated registrations.
    /// </summary>
    public AttendeeInfo? AttendeeInfo { get; private set; }

    public int Quantity { get; private set; }
    public RegistrationStatus Status { get; private set; }

    /// <summary>
    /// Indicates if this is an anonymous registration
    /// </summary>
    public bool IsAnonymous => UserId == null;

    /// <summary>
    /// Gets the attendee email (from User or AttendeeInfo)
    /// </summary>
    public Email GetAttendeeEmail()
    {
        if (AttendeeInfo != null)
            return AttendeeInfo.Email;

        // For authenticated users, email would come from User aggregate
        throw new InvalidOperationException(
            "Email not available for authenticated registration in this context");
    }

    // EF Core constructor
    private Registration() { }

    // Private constructor for authenticated users
    private Registration(Guid eventId, Guid userId, int quantity)
    {
        EventId = eventId;
        UserId = userId;
        AttendeeInfo = null;  // Explicit null for clarity
        Quantity = quantity;
        Status = RegistrationStatus.Confirmed;
    }

    // Private constructor for anonymous users
    private Registration(Guid eventId, AttendeeInfo attendeeInfo, int quantity)
    {
        EventId = eventId;
        UserId = null;  // Explicit null for clarity
        AttendeeInfo = attendeeInfo;
        Quantity = quantity;
        Status = RegistrationStatus.Confirmed;
    }

    /// <summary>
    /// Creates an authenticated user registration
    /// </summary>
    public static Result<Registration> CreateAuthenticated(
        Guid eventId, Guid userId, int quantity)
    {
        if (eventId == Guid.Empty)
            return Result<Registration>.Failure("Event ID is required");

        if (userId == Guid.Empty)
            return Result<Registration>.Failure("User ID is required");

        if (quantity <= 0)
            return Result<Registration>.Failure("Quantity must be greater than 0");

        var registration = new Registration(eventId, userId, quantity);
        return Result<Registration>.Success(registration);
    }

    /// <summary>
    /// Creates an anonymous user registration
    /// </summary>
    public static Result<Registration> CreateAnonymous(
        Guid eventId, AttendeeInfo attendeeInfo, int quantity)
    {
        if (eventId == Guid.Empty)
            return Result<Registration>.Failure("Event ID is required");

        if (attendeeInfo == null)
            return Result<Registration>.Failure("Attendee information is required");

        if (quantity <= 0)
            return Result<Registration>.Failure("Quantity must be greater than 0");

        var registration = new Registration(eventId, attendeeInfo, quantity);
        return Result<Registration>.Success(registration);
    }

    // Existing methods remain unchanged
    public void Cancel() { /* existing implementation */ }
    public void Confirm() { /* existing implementation */ }
    public Result CheckIn() { /* existing implementation */ }
    public Result CompleteAttendance() { /* existing implementation */ }
    public Result MoveTo(RegistrationStatus newStatus) { /* existing implementation */ }
    internal void UpdateQuantity(int newQuantity) { /* existing implementation */ }
    private static bool IsValidTransition(RegistrationStatus from, RegistrationStatus to)
        { /* existing implementation */ }
}
```

---

### 5.2 AttendeeInfo Value Object

```csharp
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;
using System.Text.RegularExpressions;

namespace LankaConnect.Domain.Events.ValueObjects;

/// <summary>
/// Value object representing anonymous attendee information.
/// Immutable by design following DDD principles.
/// </summary>
public record AttendeeInfo
{
    private const int MIN_NAME_LENGTH = 2;
    private const int MAX_NAME_LENGTH = 100;
    private const int MIN_AGE = 1;
    private const int MAX_AGE = 150;
    private const int MIN_ADDRESS_LENGTH = 10;
    private const int MAX_ADDRESS_LENGTH = 500;

    public string Name { get; init; }
    public int Age { get; init; }
    public string Address { get; init; }
    public Email Email { get; init; }
    public PhoneNumber Phone { get; init; }

    // Private constructor for record
    private AttendeeInfo(string name, int age, string address, Email email, PhoneNumber phone)
    {
        Name = name;
        Age = age;
        Address = address;
        Email = email;
        Phone = phone;
    }

    /// <summary>
    /// Factory method with comprehensive validation
    /// </summary>
    public static Result<AttendeeInfo> Create(
        string name,
        int age,
        string address,
        string emailString,
        string phoneString)
    {
        // Validate name
        if (string.IsNullOrWhiteSpace(name))
            return Result<AttendeeInfo>.Failure("Name is required");

        var trimmedName = name.Trim();
        if (trimmedName.Length < MIN_NAME_LENGTH)
            return Result<AttendeeInfo>.Failure(
                $"Name must be at least {MIN_NAME_LENGTH} characters");

        if (trimmedName.Length > MAX_NAME_LENGTH)
            return Result<AttendeeInfo>.Failure(
                $"Name cannot exceed {MAX_NAME_LENGTH} characters");

        // Validate age
        if (age < MIN_AGE || age > MAX_AGE)
            return Result<AttendeeInfo>.Failure(
                $"Age must be between {MIN_AGE} and {MAX_AGE}");

        // Validate address
        if (string.IsNullOrWhiteSpace(address))
            return Result<AttendeeInfo>.Failure("Address is required");

        var trimmedAddress = address.Trim();
        if (trimmedAddress.Length < MIN_ADDRESS_LENGTH)
            return Result<AttendeeInfo>.Failure(
                $"Address must be at least {MIN_ADDRESS_LENGTH} characters");

        if (trimmedAddress.Length > MAX_ADDRESS_LENGTH)
            return Result<AttendeeInfo>.Failure(
                $"Address cannot exceed {MAX_ADDRESS_LENGTH} characters");

        // Validate email using existing Email value object
        var emailResult = Email.Create(emailString);
        if (emailResult.IsFailure)
            return Result<AttendeeInfo>.Failure(emailResult.Errors);

        // Validate phone using existing PhoneNumber value object
        var phoneResult = PhoneNumber.Create(phoneString);
        if (phoneResult.IsFailure)
            return Result<AttendeeInfo>.Failure(phoneResult.Errors);

        var attendeeInfo = new AttendeeInfo(
            trimmedName,
            age,
            trimmedAddress,
            emailResult.Value,
            phoneResult.Value);

        return Result<AttendeeInfo>.Success(attendeeInfo);
    }

    /// <summary>
    /// Creates AttendeeInfo from authenticated user profile
    /// Used when authenticated users want to register
    /// </summary>
    public static Result<AttendeeInfo> CreateFromUserProfile(
        string firstName,
        string lastName,
        int? dateOfBirth,  // Calculate age from DOB
        string address,
        string email,
        string phone)
    {
        var fullName = $"{firstName?.Trim()} {lastName?.Trim()}".Trim();

        // Calculate age from date of birth if available
        var age = CalculateAge(dateOfBirth);
        if (age == null)
            return Result<AttendeeInfo>.Failure(
                "Date of birth is required to calculate age");

        return Create(fullName, age.Value, address, email, phone);
    }

    private static int? CalculateAge(int? birthYear)
    {
        if (birthYear == null) return null;
        return DateTime.UtcNow.Year - birthYear.Value;
    }

    /// <summary>
    /// Value object equality based on all properties
    /// </summary>
    public virtual bool Equals(AttendeeInfo? other)
    {
        if (other is null) return false;
        return Name == other.Name &&
               Age == other.Age &&
               Address == other.Address &&
               Email.Equals(other.Email) &&
               Phone.Equals(other.Phone);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Age, Address, Email, Phone);
    }
}
```

---

### 5.3 Updated Event Aggregate

```csharp
namespace LankaConnect.Domain.Events;

public class Event : BaseEntity
{
    // ... existing fields ...

    /// <summary>
    /// Registers an authenticated user for the event
    /// </summary>
    public Result RegisterAuthenticated(Guid userId, int quantity)
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

        var registrationResult = Registration.CreateAuthenticated(Id, userId, quantity);
        if (registrationResult.IsFailure)
            return Result.Failure(registrationResult.Errors);

        _registrations.Add(registrationResult.Value);
        MarkAsUpdated();

        RaiseDomainEvent(new RegistrationConfirmedEvent(
            Id, userId, quantity, DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Registers an anonymous user for the event
    /// </summary>
    public Result RegisterAnonymous(AttendeeInfo attendeeInfo, int quantity)
    {
        if (Status != EventStatus.Published)
            return Result.Failure("Cannot register for unpublished event");

        if (attendeeInfo == null)
            return Result.Failure("Attendee information is required");

        if (quantity <= 0)
            return Result.Failure("Quantity must be greater than 0");

        // Check if email already registered for this event (prevent duplicates)
        if (IsEmailRegistered(attendeeInfo.Email))
            return Result.Failure(
                "This email is already registered for this event");

        if (!HasCapacityFor(quantity))
            return Result.Failure("Event is at full capacity");

        var registrationResult = Registration.CreateAnonymous(
            Id, attendeeInfo, quantity);
        if (registrationResult.IsFailure)
            return Result.Failure(registrationResult.Errors);

        _registrations.Add(registrationResult.Value);
        MarkAsUpdated();

        RaiseDomainEvent(new AnonymousRegistrationConfirmedEvent(
            Id, attendeeInfo.Email, quantity, DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Checks if an email is already registered (for anonymous registrations)
    /// </summary>
    private bool IsEmailRegistered(Email email)
    {
        return _registrations.Any(r =>
            r.IsAnonymous &&
            r.AttendeeInfo != null &&
            r.AttendeeInfo.Email.Equals(email) &&
            r.Status == RegistrationStatus.Confirmed);
    }

    /// <summary>
    /// Existing method - checks authenticated user registration
    /// </summary>
    public bool IsUserRegistered(Guid userId)
    {
        return _registrations.Any(r =>
            r.UserId == userId &&
            r.Status == RegistrationStatus.Confirmed);
    }

    // ... existing methods remain unchanged ...
}
```

---

### 5.4 New Domain Events

```csharp
namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Raised when an anonymous user registers for an event
/// </summary>
public record AnonymousRegistrationConfirmedEvent(
    Guid EventId,
    Email AttendeeEmail,
    int Quantity,
    DateTime OccurredOn
) : IDomainEvent;

/// <summary>
/// Raised when an anonymous registration is cancelled
/// </summary>
public record AnonymousRegistrationCancelledEvent(
    Guid EventId,
    Email AttendeeEmail,
    DateTime OccurredOn
) : IDomainEvent;
```

---

## 6. Database Schema Design

### 6.1 Updated Registration Table Schema

```sql
-- Drop existing table (development only - use migration for production)
-- DROP TABLE IF EXISTS events.registrations CASCADE;

-- Updated registrations table with anonymous support
CREATE TABLE events.registrations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_id UUID NOT NULL,

    -- Nullable for anonymous registrations
    user_id UUID NULL,

    -- JSONB column for anonymous attendee information
    -- Structure: { "name": "...", "age": 30, "address": "...",
    --              "email": "...", "phone": "..." }
    attendee_info JSONB NULL,

    quantity INTEGER NOT NULL DEFAULT 1,
    status VARCHAR(20) NOT NULL DEFAULT 'Confirmed',

    -- Audit fields
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NULL,

    -- Business rule constraint: Either user_id OR attendee_info must be set
    CONSTRAINT chk_registration_identity
        CHECK (
            (user_id IS NOT NULL AND attendee_info IS NULL) OR
            (user_id IS NULL AND attendee_info IS NOT NULL)
        ),

    -- Ensure valid attendee_info structure when present
    CONSTRAINT chk_attendee_info_structure
        CHECK (
            attendee_info IS NULL OR (
                attendee_info ? 'name' AND
                attendee_info ? 'age' AND
                attendee_info ? 'address' AND
                attendee_info ? 'email' AND
                attendee_info ? 'phone'
            )
        )
);

-- Indexes for performance
CREATE INDEX idx_registrations_event_id
    ON events.registrations(event_id);

CREATE INDEX idx_registrations_user_id
    ON events.registrations(user_id)
    WHERE user_id IS NOT NULL;

-- Unique constraint: one registration per user per event (authenticated)
CREATE UNIQUE INDEX idx_registrations_event_user_unique
    ON events.registrations(event_id, user_id)
    WHERE user_id IS NOT NULL;

-- Unique constraint: one registration per email per event (anonymous)
CREATE UNIQUE INDEX idx_registrations_event_email_unique
    ON events.registrations(event_id, (attendee_info->>'email'))
    WHERE user_id IS NULL;

-- GIN index for JSONB queries (fast lookup by email, name, etc.)
CREATE INDEX idx_registrations_attendee_info_gin
    ON events.registrations USING GIN (attendee_info)
    WHERE user_id IS NULL;

-- Index for status queries
CREATE INDEX idx_registrations_user_status
    ON events.registrations(user_id, status)
    WHERE user_id IS NOT NULL;

CREATE INDEX idx_registrations_email_status
    ON events.registrations((attendee_info->>'email'), status)
    WHERE user_id IS NULL;

-- Comments for documentation
COMMENT ON TABLE events.registrations IS
    'Event registrations supporting both authenticated and anonymous users';

COMMENT ON COLUMN events.registrations.user_id IS
    'Foreign key to users table. Null for anonymous registrations.';

COMMENT ON COLUMN events.registrations.attendee_info IS
    'JSONB containing anonymous attendee information (name, age, address, email, phone). Null for authenticated users.';

COMMENT ON CONSTRAINT chk_registration_identity ON events.registrations IS
    'Business rule: Registration must have either user_id (authenticated) or attendee_info (anonymous), but not both.';
```

---

### 6.2 Sample JSONB Structure

```json
{
  "name": "John Doe",
  "age": 35,
  "address": "123 Main Street, Apartment 4B, New York, NY 10001",
  "email": "john.doe@example.com",
  "phone": "+1-555-123-4567"
}
```

---

### 6.3 Query Examples

```sql
-- Get all registrations for an event (both types)
SELECT
    id,
    event_id,
    user_id,
    attendee_info->>'name' AS attendee_name,
    attendee_info->>'email' AS attendee_email,
    quantity,
    status
FROM events.registrations
WHERE event_id = '123e4567-e89b-12d3-a456-426614174000';

-- Get anonymous registrations only
SELECT
    id,
    attendee_info->>'name' AS name,
    (attendee_info->>'age')::INTEGER AS age,
    attendee_info->>'email' AS email,
    status
FROM events.registrations
WHERE user_id IS NULL
  AND event_id = '123e4567-e89b-12d3-a456-426614174000';

-- Check if email already registered (anonymous)
SELECT EXISTS (
    SELECT 1
    FROM events.registrations
    WHERE event_id = '123e4567-e89b-12d3-a456-426614174000'
      AND user_id IS NULL
      AND attendee_info->>'email' = 'john.doe@example.com'
      AND status = 'Confirmed'
);

-- Count registrations by type
SELECT
    CASE
        WHEN user_id IS NOT NULL THEN 'Authenticated'
        ELSE 'Anonymous'
    END AS registration_type,
    COUNT(*) AS count,
    SUM(quantity) AS total_attendees
FROM events.registrations
WHERE event_id = '123e4567-e89b-12d3-a456-426614174000'
GROUP BY registration_type;
```

---

## 7. API Contract Design

### 7.1 Request/Response DTOs

```csharp
namespace LankaConnect.Application.Events.Commands;

/// <summary>
/// Command to register authenticated user for event
/// </summary>
public record RegisterForEventCommand(
    Guid EventId,
    int Quantity
);

/// <summary>
/// Command to register anonymous user for event
/// </summary>
public record RegisterAnonymouslyForEventCommand(
    Guid EventId,
    string Name,
    int Age,
    string Address,
    string Email,
    string Phone,
    int Quantity
);

/// <summary>
/// Response DTO for registration
/// </summary>
public record RegistrationResponse(
    Guid RegistrationId,
    Guid EventId,
    bool IsAnonymous,
    int Quantity,
    string Status,
    DateTime RegisteredAt
);
```

---

### 7.2 API Endpoints

```csharp
// EventsController.cs

/// <summary>
/// Registers authenticated user for an event
/// Requires: Bearer token authentication
/// </summary>
[HttpPost("api/events/{eventId}/register")]
[Authorize]
public async Task<IActionResult> RegisterForEvent(
    Guid eventId,
    [FromBody] RegisterForEventCommand command)
{
    // Get userId from authenticated claims
    var userId = User.GetUserId();

    var result = await _mediator.Send(
        new RegisterAuthenticatedUserCommand(eventId, userId, command.Quantity));

    if (result.IsFailure)
        return BadRequest(new { errors = result.Errors });

    return Ok(new RegistrationResponse(
        RegistrationId: result.Value.Id,
        EventId: eventId,
        IsAnonymous: false,
        Quantity: command.Quantity,
        Status: "Confirmed",
        RegisteredAt: DateTime.UtcNow
    ));
}

/// <summary>
/// Registers anonymous user for an event
/// No authentication required
/// Rate limited: 5 requests per IP per minute
/// </summary>
[HttpPost("api/events/{eventId}/register-anonymous")]
[AllowAnonymous]
[EnableRateLimiting("anonymous-registration")]
public async Task<IActionResult> RegisterAnonymously(
    Guid eventId,
    [FromBody] RegisterAnonymouslyForEventCommand command)
{
    // Validate CAPTCHA (not shown here)
    // if (!await _captchaService.ValidateAsync(command.CaptchaToken))
    //     return BadRequest(new { error = "CAPTCHA validation failed" });

    var result = await _mediator.Send(command);

    if (result.IsFailure)
        return BadRequest(new { errors = result.Errors });

    return Ok(new RegistrationResponse(
        RegistrationId: result.Value.Id,
        EventId: eventId,
        IsAnonymous: true,
        Quantity: command.Quantity,
        Status: "Confirmed",
        RegisteredAt: DateTime.UtcNow
    ));
}

/// <summary>
/// Gets registration details (for confirmation email, etc.)
/// </summary>
[HttpGet("api/registrations/{registrationId}")]
[AllowAnonymous]  // Allow anonymous access with registration ID
public async Task<IActionResult> GetRegistration(Guid registrationId)
{
    var result = await _mediator.Send(
        new GetRegistrationQuery(registrationId));

    if (result.IsFailure)
        return NotFound();

    return Ok(result.Value);
}
```

---

### 7.3 Application Service Handlers

```csharp
namespace LankaConnect.Application.Events.CommandHandlers;

/// <summary>
/// Handles anonymous user registration
/// </summary>
public class RegisterAnonymouslyForEventHandler
    : IRequestHandler<RegisterAnonymouslyForEventCommand, Result<Registration>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result<Registration>> Handle(
        RegisterAnonymouslyForEventCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Load event aggregate
        var @event = await _eventRepository.GetByIdAsync(
            request.EventId, cancellationToken);

        if (@event == null)
            return Result<Registration>.Failure("Event not found");

        // 2. Create AttendeeInfo value object
        var attendeeInfoResult = AttendeeInfo.Create(
            request.Name,
            request.Age,
            request.Address,
            request.Email,
            request.Phone
        );

        if (attendeeInfoResult.IsFailure)
            return Result<Registration>.Failure(attendeeInfoResult.Errors);

        // 3. Register anonymous user via Event aggregate
        var registerResult = @event.RegisterAnonymous(
            attendeeInfoResult.Value,
            request.Quantity);

        if (registerResult.IsFailure)
            return Result<Registration>.Failure(registerResult.Errors);

        // 4. Persist changes
        await _eventRepository.UpdateAsync(@event, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 5. Send confirmation email (async, non-blocking)
        var registration = @event.Registrations
            .First(r => r.AttendeeInfo?.Email.Value == request.Email);

        _ = _emailService.SendRegistrationConfirmationAsync(
            registration.Id,
            request.Email,
            @event.Title.Value);

        return Result<Registration>.Success(registration);
    }
}
```

---

## 8. Migration Strategy

### 8.1 Database Migration Steps

```csharp
// EF Core Migration: AddAnonymousRegistrationSupport

public partial class AddAnonymousRegistrationSupport : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Step 1: Make UserId nullable
        migrationBuilder.AlterColumn<Guid>(
            name: "UserId",
            table: "Registrations",
            type: "uuid",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uuid");

        // Step 2: Add AttendeeInfo JSONB column
        migrationBuilder.AddColumn<string>(
            name: "AttendeeInfo",
            table: "Registrations",
            type: "jsonb",
            nullable: true);

        // Step 3: Add check constraint for identity
        migrationBuilder.Sql(@"
            ALTER TABLE ""Registrations""
            ADD CONSTRAINT chk_registration_identity
            CHECK (
                (""UserId"" IS NOT NULL AND ""AttendeeInfo"" IS NULL) OR
                (""UserId"" IS NULL AND ""AttendeeInfo"" IS NOT NULL)
            );
        ");

        // Step 4: Add check constraint for AttendeeInfo structure
        migrationBuilder.Sql(@"
            ALTER TABLE ""Registrations""
            ADD CONSTRAINT chk_attendee_info_structure
            CHECK (
                ""AttendeeInfo"" IS NULL OR (
                    ""AttendeeInfo"" ? 'name' AND
                    ""AttendeeInfo"" ? 'age' AND
                    ""AttendeeInfo"" ? 'address' AND
                    ""AttendeeInfo"" ? 'email' AND
                    ""AttendeeInfo"" ? 'phone'
                )
            );
        ");

        // Step 5: Drop existing unique index (UserId + EventId)
        migrationBuilder.DropIndex(
            name: "ix_registrations_event_user_unique",
            table: "Registrations");

        // Step 6: Create partial unique index for authenticated users
        migrationBuilder.Sql(@"
            CREATE UNIQUE INDEX ix_registrations_event_user_unique
            ON ""Registrations""(""EventId"", ""UserId"")
            WHERE ""UserId"" IS NOT NULL;
        ");

        // Step 7: Create partial unique index for anonymous users (by email)
        migrationBuilder.Sql(@"
            CREATE UNIQUE INDEX ix_registrations_event_email_unique
            ON ""Registrations""(""EventId"", (""AttendeeInfo""->>'email'))
            WHERE ""UserId"" IS NULL;
        ");

        // Step 8: Create GIN index for JSONB queries
        migrationBuilder.Sql(@"
            CREATE INDEX idx_registrations_attendee_info_gin
            ON ""Registrations"" USING GIN (""AttendeeInfo"")
            WHERE ""UserId"" IS NULL;
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Rollback: Remove anonymous support

        // Step 1: Drop GIN index
        migrationBuilder.Sql(
            @"DROP INDEX IF EXISTS idx_registrations_attendee_info_gin;");

        // Step 2: Drop email unique index
        migrationBuilder.Sql(
            @"DROP INDEX IF EXISTS ix_registrations_event_email_unique;");

        // Step 3: Drop partial unique index
        migrationBuilder.Sql(
            @"DROP INDEX IF EXISTS ix_registrations_event_user_unique;");

        // Step 4: Drop check constraints
        migrationBuilder.Sql(
            @"ALTER TABLE ""Registrations"" DROP CONSTRAINT IF EXISTS chk_attendee_info_structure;");
        migrationBuilder.Sql(
            @"ALTER TABLE ""Registrations"" DROP CONSTRAINT IF EXISTS chk_registration_identity;");

        // Step 5: Remove AttendeeInfo column
        migrationBuilder.DropColumn(
            name: "AttendeeInfo",
            table: "Registrations");

        // Step 6: Make UserId required again
        migrationBuilder.AlterColumn<Guid>(
            name: "UserId",
            table: "Registrations",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        // Step 7: Recreate original unique index
        migrationBuilder.CreateIndex(
            name: "ix_registrations_event_user_unique",
            table: "Registrations",
            columns: new[] { "EventId", "UserId" },
            unique: true);
    }
}
```

---

### 8.2 Code Migration Steps

**Phase 1: Add Support (Backward Compatible)**
1. Add `AttendeeInfo` value object
2. Update `Registration` entity with nullable `UserId`
3. Add `RegisterAnonymous()` method to Event aggregate
4. Keep existing `Register()` method as `RegisterAuthenticated()`
5. Update EF Core configuration
6. Run database migration

**Phase 2: Update Callers**
1. Update application layer to use new methods
2. Update API controllers
3. Update frontend to support anonymous registration
4. Add CAPTCHA for anonymous registrations

**Phase 3: Remove Old Methods (Optional)**
1. Mark `Register(userId)` as `[Obsolete]`
2. Replace all usages with `RegisterAuthenticated()`
3. Remove deprecated method in next major version

---

### 8.3 Data Migration (If Needed)

If you have existing test data that needs conversion:

```sql
-- Example: Convert existing test registrations to use new schema
-- (Only if you have test data you want to preserve)

-- Verify all existing registrations have UserId
SELECT COUNT(*)
FROM events.registrations
WHERE user_id IS NULL;
-- Should return 0

-- Verify AttendeeInfo is NULL for all existing
SELECT COUNT(*)
FROM events.registrations
WHERE attendee_info IS NOT NULL;
-- Should return 0
```

---

## 9. Implementation Roadmap

### 9.1 Sprint 1: Foundation (Week 1)

**Backend Tasks:**
- [x] Design review and approval
- [ ] Create `AttendeeInfo` value object with validation
- [ ] Update `Registration` entity with nullable UserId
- [ ] Add `RegisterAnonymous()` to Event aggregate
- [ ] Write unit tests for AttendeeInfo validation
- [ ] Write unit tests for Event.RegisterAnonymous()

**Database Tasks:**
- [ ] Create EF Core migration
- [ ] Test migration up/down locally
- [ ] Add JSONB query tests

**Deliverables:**
- AttendeeInfo value object (100% test coverage)
- Updated Registration entity (100% test coverage)
- Updated Event aggregate (100% test coverage)
- Database migration scripts

---

### 9.2 Sprint 2: Application Layer (Week 2)

**Application Layer:**
- [ ] Create `RegisterAnonymouslyForEventCommand`
- [ ] Create command handler with validation
- [ ] Update repository for JSONB querying
- [ ] Add domain event handlers
- [ ] Email notification templates

**API Layer:**
- [ ] Add `/register-anonymous` endpoint
- [ ] Rate limiting configuration
- [ ] CAPTCHA integration (Google reCAPTCHA v3)
- [ ] API documentation (Swagger)

**Testing:**
- [ ] Integration tests for anonymous registration
- [ ] API tests with Postman/REST Client
- [ ] Load testing for rate limiting

---

### 9.3 Sprint 3: Frontend & UI (Week 3)

**Web Frontend (Next.js):**
- [ ] Anonymous registration form component
- [ ] Form validation (client-side + server-side)
- [ ] CAPTCHA integration
- [ ] Success/error handling
- [ ] Email confirmation display

**UI/UX:**
- [ ] Remove "Manage Sign-ups" button from event detail
- [ ] Show sign-up lists after registration
- [ ] Anonymous user flow (register → confirm → sign-ups)
- [ ] Responsive design for mobile

**Testing:**
- [ ] E2E tests with Playwright
- [ ] Accessibility testing (WCAG 2.1 AA)
- [ ] Cross-browser testing

---

### 9.4 Sprint 4: Testing & Deployment (Week 4)

**Quality Assurance:**
- [ ] Full regression testing
- [ ] Performance testing (load, stress)
- [ ] Security testing (SQL injection, XSS)
- [ ] GDPR compliance review

**Deployment:**
- [ ] Deploy to staging environment
- [ ] Smoke testing in staging
- [ ] Production deployment
- [ ] Monitor logs and metrics

**Documentation:**
- [ ] Update API documentation
- [ ] Update user guide
- [ ] Internal technical documentation

---

## 10. Risk Analysis & Mitigation

### 10.1 Technical Risks

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| **Migration fails in production** | High | Low | Comprehensive testing, rollback plan, canary deployment |
| **JSONB query performance issues** | Medium | Low | GIN indexes, query optimization, caching |
| **Nullable UserId breaks existing code** | High | Medium | Thorough unit tests, integration tests, code review |
| **Email uniqueness constraint violations** | Medium | Low | Partial unique index, clear error messages |
| **CAPTCHA bypass by bots** | Medium | Medium | Rate limiting, IP blocking, honeypot fields |

---

### 10.2 Business Risks

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| **Spam registrations** | High | High | CAPTCHA, rate limiting, email verification |
| **Fake attendee information** | Medium | High | Email verification required, phone validation optional |
| **GDPR compliance issues** | High | Low | Data retention policy, right to erasure, consent tracking |
| **Anonymous users don't convert to authenticated** | Low | High | Clear value proposition for account creation |

---

### 10.3 Mitigation Strategies

#### Spam Prevention
```csharp
// Rate limiting configuration
services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("anonymous-registration", opt =>
    {
        opt.PermitLimit = 5;  // 5 registrations
        opt.Window = TimeSpan.FromMinutes(1);  // per minute
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 2;
    });
});
```

#### Email Verification (Future Enhancement)
```csharp
// Send verification email before confirming registration
public async Task<Result> SendVerificationEmailAsync(
    Registration registration)
{
    var token = GenerateVerificationToken();
    var verificationLink = $"https://lankaconnect.com/verify/{token}";

    await _emailService.SendAsync(
        registration.AttendeeInfo!.Email,
        "Verify Your Registration",
        $"Click here to verify: {verificationLink}");

    // Keep registration in "Pending" status until verified
    registration.MoveTo(RegistrationStatus.Pending);
}
```

#### GDPR Compliance
```csharp
// Data retention policy: Delete anonymous registrations after event
public class DeleteExpiredAnonymousRegistrationsJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-90);  // 90 days retention

        var expiredRegistrations = await _repository
            .GetExpiredAnonymousRegistrationsAsync(cutoffDate);

        foreach (var registration in expiredRegistrations)
        {
            await _repository.DeleteAsync(registration.Id);
        }
    }
}
```

---

## 11. Success Metrics

### 11.1 Technical Metrics
- **Test Coverage:** ≥ 90% for new code
- **API Response Time:** < 500ms for registration endpoint
- **Database Query Performance:** < 100ms for registration queries
- **Migration Success Rate:** 100% (no data loss)

### 11.2 Business Metrics
- **Anonymous Registration Rate:** Target 30% of total registrations
- **Conversion to Authenticated:** Target 15% within 30 days
- **Email Verification Rate:** Target 80% within 24 hours
- **Spam Rate:** < 5% of anonymous registrations

---

## 12. Future Enhancements

### Phase 2 Considerations

1. **Social Authentication for Anonymous Users**
   - Allow "Sign in with Google" after anonymous registration
   - Automatically convert anonymous → authenticated

2. **Attendee Profiles**
   - Store anonymous attendee history across events
   - Pre-fill forms for returning anonymous users (cookie-based)

3. **Advanced Spam Detection**
   - Machine learning model for fake data detection
   - IP reputation checking
   - Behavioral analysis

4. **Multi-Event Registration**
   - Register for multiple events at once
   - Bulk registration discounts

5. **Guest Management**
   - Primary attendee + guest details
   - Guest-specific fields (dietary restrictions, accessibility needs)

---

## Appendix A: Alternative Approaches Considered

### A.1 Separate Tables (Rejected)

```sql
CREATE TABLE events.authenticated_registrations (
    id UUID PRIMARY KEY,
    event_id UUID NOT NULL,
    user_id UUID NOT NULL,
    ...
);

CREATE TABLE events.anonymous_registrations (
    id UUID PRIMARY KEY,
    event_id UUID NOT NULL,
    name VARCHAR(100) NOT NULL,
    age INTEGER NOT NULL,
    ...
);
```

**Rejected because:**
- Complex capacity calculations require UNION queries
- Duplicate status management logic
- Aggregate boundary violations

---

### A.2 EF Core TPH Inheritance (Rejected)

```csharp
public abstract class RegistrationBase { }
public class AuthenticatedRegistration : RegistrationBase { }
public class AnonymousRegistration : RegistrationBase { }
```

**Rejected because:**
- Discriminator column overhead
- Polymorphic query complexity
- Over-engineering for simple use case

---

## Appendix B: Testing Scenarios

### B.1 Unit Test Cases

```csharp
[Fact]
public void AttendeeInfo_Create_WithValidData_ShouldSucceed()
{
    var result = AttendeeInfo.Create(
        "John Doe", 35, "123 Main St",
        "john@example.com", "+1-555-1234");

    Assert.True(result.IsSuccess);
    Assert.Equal("John Doe", result.Value.Name);
}

[Fact]
public void AttendeeInfo_Create_WithInvalidEmail_ShouldFail()
{
    var result = AttendeeInfo.Create(
        "John Doe", 35, "123 Main St",
        "invalid-email", "+1-555-1234");

    Assert.True(result.IsFailure);
    Assert.Contains("email", result.Errors.First().ToLower());
}

[Fact]
public void Event_RegisterAnonymous_WithValidData_ShouldSucceed()
{
    // Arrange
    var @event = CreatePublishedEvent();
    var attendeeInfo = CreateValidAttendeeInfo();

    // Act
    var result = @event.RegisterAnonymous(attendeeInfo, 1);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(1, @event.Registrations.Count);
    Assert.True(@event.Registrations.First().IsAnonymous);
}

[Fact]
public void Event_RegisterAnonymous_WithDuplicateEmail_ShouldFail()
{
    // Arrange
    var @event = CreatePublishedEvent();
    var attendeeInfo1 = CreateValidAttendeeInfo("john@example.com");
    var attendeeInfo2 = CreateValidAttendeeInfo("john@example.com");

    // Act
    @event.RegisterAnonymous(attendeeInfo1, 1);
    var result = @event.RegisterAnonymous(attendeeInfo2, 1);

    // Assert
    Assert.True(result.IsFailure);
    Assert.Contains("already registered", result.Errors.First());
}
```

---

## Appendix C: Database Performance Benchmarks

```sql
-- Test JSONB query performance
EXPLAIN ANALYZE
SELECT *
FROM events.registrations
WHERE event_id = '123e4567-e89b-12d3-a456-426614174000'
  AND attendee_info->>'email' = 'john@example.com';

-- Expected: Index Scan using idx_registrations_attendee_info_gin
-- Execution time: < 5ms
```

---

## Conclusion

This architectural design provides a robust, scalable solution for anonymous event registration while maintaining domain integrity and backward compatibility. The hybrid approach using nullable UserId with JSONB-stored AttendeeInfo leverages PostgreSQL's strengths and keeps the domain model clean and cohesive.

**Next Steps:**
1. Stakeholder review and approval
2. Begin Sprint 1 implementation
3. Continuous monitoring of success metrics
4. Iterate based on user feedback

---

**Document Version:** 1.0
**Last Updated:** 2025-12-01
**Approved By:** [Pending Review]
