# Architecture Decision Record: Dual Ticket Pricing with Multiple Attendees

**Status**: Proposed
**Date**: 2025-12-01
**Deciders**: System Architecture Designer
**Context**: LankaConnect Event Registration System

## Executive Summary

This ADR defines the architectural design for implementing dual ticket pricing (Adult/Child) with multiple attendees per registration while maintaining Clean Architecture principles, DDD best practices, backward compatibility, and PostgreSQL JSONB efficiency.

---

## Table of Contents

1. [Context and Problem Statement](#1-context-and-problem-statement)
2. [Design Decision](#2-design-decision)
3. [Entity Structure](#3-entity-structure)
4. [Value Object Design](#4-value-object-design)
5. [Domain Services](#5-domain-services)
6. [Migration Strategy](#6-migration-strategy)
7. [Consequences](#7-consequences)
8. [Implementation Roadmap](#8-implementation-roadmap)

---

## 1. Context and Problem Statement

### Current State

**Event Aggregate**:
- Single `TicketPrice` (Money value object with Amount + Currency)
- Registration with `Quantity` (int) - just a count
- `AttendeeInfo` (JSONB) for anonymous registrations

**Requirements**:
1. Adult vs Child pricing with configurable age limit
2. Multiple attendees per registration (family of 4)
3. Each attendee needs Name + Age
4. Shared contact info (Email, Phone, Address) for all attendees
5. Automatic price calculation based on ages
6. Member profile pre-fill for primary attendee

### Key Constraints

1. **Clean Architecture**: Domain entities must not depend on infrastructure
2. **DDD**: Rich domain models with invariant enforcement
3. **PostgreSQL**: Leverage JSONB for flexible schema evolution
4. **Backward Compatibility**: Existing single-ticket registrations must work
5. **EF Core**: Must map cleanly to database

---

## 2. Design Decision

### Option Analysis

| Option | Pros | Cons | Decision |
|--------|------|------|----------|
| **A: JSONB Array in AttendeeInfo** | - Simple migration<br>- No schema change<br>- Flexible | - Weak typing<br>- Poor queryability | ❌ Rejected |
| **B: Separate Attendee Entity Table** | - Strong typing<br>- Easy queries<br>- Relational integrity | - Complex migration<br>- Performance overhead<br>- Over-engineering | ❌ Rejected |
| **C: Enhanced Value Objects in JSONB** | - Clean domain model<br>- Type safety<br>- Flexible evolution<br>- PostgreSQL optimized | - Slight query complexity for reporting | ✅ **SELECTED** |

### Why Option C (Enhanced Value Objects)?

1. **Domain Purity**: Value objects are immutable and self-validating
2. **PostgreSQL JSONB**: Indexed queries, GIN indexes, jsonb_array_elements
3. **Migration Safety**: Backward compatible with single attendee
4. **Performance**: No JOIN overhead, atomic read/write
5. **Clean Architecture**: No infrastructure concerns in domain layer

---

## 3. Entity Structure

### 3.1 Event Aggregate (Root)

```csharp
public class Event : BaseEntity
{
    // EXISTING - Keep for backward compatibility
    public Money? TicketPrice { get; private set; }

    // NEW - Dual pricing configuration
    public TicketPricing? Pricing { get; private set; } // null = free event

    // ... existing properties ...

    #region Pricing Configuration

    /// <summary>
    /// Configures dual ticket pricing (Adult + Child)
    /// Replaces single TicketPrice for events requiring age-based pricing
    /// </summary>
    public Result SetDualPricing(Money adultPrice, Money childPrice, int childAgeLimit)
    {
        if (adultPrice == null)
            return Result.Failure("Adult price is required");

        if (childPrice == null)
            return Result.Failure("Child price is required");

        if (childAgeLimit < 1 || childAgeLimit > 18)
            return Result.Failure("Child age limit must be between 1 and 18");

        if (adultPrice.Currency != childPrice.Currency)
            return Result.Failure("Adult and child prices must use the same currency");

        // Business rule: Child price cannot exceed adult price
        if (childPrice.IsGreaterThan(adultPrice))
            return Result.Failure("Child ticket price cannot exceed adult ticket price");

        var pricingResult = TicketPricing.CreateDual(adultPrice, childPrice, childAgeLimit);
        if (pricingResult.IsFailure)
            return Result.Failure(pricingResult.Errors);

        Pricing = pricingResult.Value;
        TicketPrice = null; // Clear old single-price for consistency
        MarkAsUpdated();

        RaiseDomainEvent(new EventPricingUpdatedEvent(Id, Pricing, DateTime.UtcNow));
        return Result.Success();
    }

    /// <summary>
    /// Configures single ticket pricing (backward compatible)
    /// </summary>
    public Result SetSinglePricing(Money price)
    {
        if (price == null)
            return Result.Failure("Price is required");

        var pricingResult = TicketPricing.CreateSingle(price);
        if (pricingResult.IsFailure)
            return Result.Failure(pricingResult.Errors);

        Pricing = pricingResult.Value;
        TicketPrice = price; // Keep for backward compatibility in queries
        MarkAsUpdated();

        RaiseDomainEvent(new EventPricingUpdatedEvent(Id, Pricing, DateTime.UtcNow));
        return Result.Success();
    }

    /// <summary>
    /// Sets event as free (no pricing)
    /// </summary>
    public Result SetFree()
    {
        Pricing = null;
        TicketPrice = null;
        MarkAsUpdated();

        RaiseDomainEvent(new EventPricingUpdatedEvent(Id, null, DateTime.UtcNow));
        return Result.Success();
    }

    /// <summary>
    /// Checks if event requires age information for pricing
    /// </summary>
    public bool RequiresAgeBasedPricing() => Pricing?.IsDualPricing ?? false;

    /// <summary>
    /// Checks if event is free (override existing method)
    /// </summary>
    public new bool IsFree() => Pricing == null;

    /// <summary>
    /// Calculates total price for multiple attendees
    /// Domain service method - pricing logic belongs to Event aggregate
    /// </summary>
    public Result<Money> CalculatePriceForAttendees(IEnumerable<AttendeeDetails> attendees)
    {
        if (attendees == null || !attendees.Any())
            return Result<Money>.Failure("At least one attendee is required");

        if (IsFree())
            return Result<Money>.Success(Money.Zero(Currency.USD)); // Default currency for free events

        if (Pricing == null)
            return Result<Money>.Failure("Event pricing not configured");

        return Pricing.CalculateTotal(attendees);
    }

    #endregion

    #region Multi-Attendee Registration

    /// <summary>
    /// Registers multiple attendees (authenticated user)
    /// Primary attendee from user profile, additional attendees provided manually
    /// </summary>
    public Result<Registration> RegisterWithAttendees(
        Guid userId,
        IEnumerable<AttendeeDetails> attendees,
        RegistrationContact contact)
    {
        if (Status != EventStatus.Published)
            return Result<Registration>.Failure("Cannot register for unpublished event");

        if (userId == Guid.Empty)
            return Result<Registration>.Failure("User ID is required");

        if (attendees == null || !attendees.Any())
            return Result<Registration>.Failure("At least one attendee is required");

        if (contact == null)
            return Result<Registration>.Failure("Contact information is required");

        if (IsUserRegistered(userId))
            return Result<Registration>.Failure("User is already registered for this event");

        var attendeeList = attendees.ToList();
        var totalQuantity = attendeeList.Count;

        if (!HasCapacityFor(totalQuantity))
            return Result<Registration>.Failure("Event is at full capacity");

        // Validate attendee age requirements if dual pricing
        if (RequiresAgeBasedPricing())
        {
            if (attendeeList.Any(a => a.Age < 1))
                return Result<Registration>.Failure("All attendees must have valid age when event uses age-based pricing");
        }

        // Calculate total price
        Result<Money> totalPriceResult;
        if (IsFree())
        {
            totalPriceResult = Result<Money>.Success(Money.Zero(Currency.USD));
        }
        else
        {
            totalPriceResult = CalculatePriceForAttendees(attendeeList);
            if (totalPriceResult.IsFailure)
                return Result<Registration>.Failure(totalPriceResult.Errors);
        }

        // Create multi-attendee registration
        var registrationResult = Registration.CreateWithAttendees(
            Id,
            userId,
            attendeeList,
            contact,
            totalPriceResult.Value);

        if (registrationResult.IsFailure)
            return registrationResult;

        _registrations.Add(registrationResult.Value);
        MarkAsUpdated();

        RaiseDomainEvent(new RegistrationConfirmedEvent(Id, userId, totalQuantity, DateTime.UtcNow));
        return registrationResult;
    }

    /// <summary>
    /// Registers multiple attendees (anonymous user)
    /// </summary>
    public Result<Registration> RegisterAnonymousWithAttendees(
        IEnumerable<AttendeeDetails> attendees,
        RegistrationContact contact)
    {
        if (Status != EventStatus.Published)
            return Result<Registration>.Failure("Cannot register for unpublished event");

        if (attendees == null || !attendees.Any())
            return Result<Registration>.Failure("At least one attendee is required");

        if (contact == null)
            return Result<Registration>.Failure("Contact information is required");

        var attendeeList = attendees.ToList();
        var totalQuantity = attendeeList.Count;

        if (!HasCapacityFor(totalQuantity))
            return Result<Registration>.Failure("Event is at full capacity");

        // Validate age requirements
        if (RequiresAgeBasedPricing())
        {
            if (attendeeList.Any(a => a.Age < 1))
                return Result<Registration>.Failure("All attendees must have valid age when event uses age-based pricing");
        }

        // Calculate total price
        Result<Money> totalPriceResult;
        if (IsFree())
        {
            totalPriceResult = Result<Money>.Success(Money.Zero(Currency.USD));
        }
        else
        {
            totalPriceResult = CalculatePriceForAttendees(attendeeList);
            if (totalPriceResult.IsFailure)
                return Result<Registration>.Failure(totalPriceResult.Errors);
        }

        var registrationResult = Registration.CreateAnonymousWithAttendees(
            Id,
            attendeeList,
            contact,
            totalPriceResult.Value);

        if (registrationResult.IsFailure)
            return registrationResult;

        _registrations.Add(registrationResult.Value);
        MarkAsUpdated();

        RaiseDomainEvent(new AnonymousRegistrationConfirmedEvent(Id, contact.Email.Value, totalQuantity, DateTime.UtcNow));
        return registrationResult;
    }

    #endregion
}
```

### 3.2 Registration Entity (Updated)

```csharp
public class Registration : BaseEntity
{
    // EXISTING - Keep for backward compatibility
    public Guid EventId { get; private set; }
    public Guid? UserId { get; private set; }
    public AttendeeInfo? AttendeeInfo { get; private set; } // DEPRECATED - Use Attendees instead
    public int Quantity { get; private set; } // DEPRECATED - Derived from Attendees.Count
    public RegistrationStatus Status { get; private set; }

    // NEW - Multi-attendee support
    private readonly List<AttendeeDetails> _attendees = new();
    public IReadOnlyList<AttendeeDetails> Attendees => _attendees.AsReadOnly();
    public RegistrationContact? Contact { get; private set; } // Shared contact info
    public Money? TotalPrice { get; private set; } // Calculated and stored

    // Computed property for compatibility
    public int ActualQuantity => _attendees.Any() ? _attendees.Count : Quantity;

    // EF Core constructor
    private Registration() { }

    // DEPRECATED - Keep for backward compatibility
    private Registration(Guid eventId, Guid userId, int quantity)
    {
        EventId = eventId;
        UserId = userId;
        AttendeeInfo = null;
        Quantity = quantity;
        Status = RegistrationStatus.Confirmed;
        // New fields null for legacy registrations
    }

    // DEPRECATED - Keep for backward compatibility
    private Registration(Guid eventId, AttendeeInfo attendeeInfo, int quantity)
    {
        EventId = eventId;
        UserId = null;
        AttendeeInfo = attendeeInfo;
        Quantity = quantity;
        Status = RegistrationStatus.Confirmed;
    }

    // NEW - Multi-attendee authenticated registration
    private Registration(
        Guid eventId,
        Guid userId,
        List<AttendeeDetails> attendees,
        RegistrationContact contact,
        Money totalPrice)
    {
        EventId = eventId;
        UserId = userId;
        AttendeeInfo = null; // New pattern doesn't use AttendeeInfo
        _attendees = attendees;
        Contact = contact;
        TotalPrice = totalPrice;
        Quantity = attendees.Count; // For backward compatibility
        Status = RegistrationStatus.Confirmed;
    }

    // NEW - Multi-attendee anonymous registration
    private Registration(
        Guid eventId,
        List<AttendeeDetails> attendees,
        RegistrationContact contact,
        Money totalPrice)
    {
        EventId = eventId;
        UserId = null;
        AttendeeInfo = null;
        _attendees = attendees;
        Contact = contact;
        TotalPrice = totalPrice;
        Quantity = attendees.Count;
        Status = RegistrationStatus.Confirmed;
    }

    #region Factory Methods

    // NEW - Multi-attendee authenticated
    public static Result<Registration> CreateWithAttendees(
        Guid eventId,
        Guid userId,
        IEnumerable<AttendeeDetails> attendees,
        RegistrationContact contact,
        Money totalPrice)
    {
        if (eventId == Guid.Empty)
            return Result<Registration>.Failure("Event ID is required");

        if (userId == Guid.Empty)
            return Result<Registration>.Failure("User ID is required");

        if (attendees == null || !attendees.Any())
            return Result<Registration>.Failure("At least one attendee is required");

        var attendeeList = attendees.ToList();

        // Business rule: Max 10 attendees per registration
        if (attendeeList.Count > 10)
            return Result<Registration>.Failure("Cannot register more than 10 attendees per registration");

        if (contact == null)
            return Result<Registration>.Failure("Contact information is required");

        if (totalPrice == null)
            return Result<Registration>.Failure("Total price is required");

        var registration = new Registration(eventId, userId, attendeeList, contact, totalPrice);
        return Result<Registration>.Success(registration);
    }

    // NEW - Multi-attendee anonymous
    public static Result<Registration> CreateAnonymousWithAttendees(
        Guid eventId,
        IEnumerable<AttendeeDetails> attendees,
        RegistrationContact contact,
        Money totalPrice)
    {
        if (eventId == Guid.Empty)
            return Result<Registration>.Failure("Event ID is required");

        if (attendees == null || !attendees.Any())
            return Result<Registration>.Failure("At least one attendee is required");

        var attendeeList = attendees.ToList();

        if (attendeeList.Count > 10)
            return Result<Registration>.Failure("Cannot register more than 10 attendees per registration");

        if (contact == null)
            return Result<Registration>.Failure("Contact information is required");

        if (totalPrice == null)
            return Result<Registration>.Failure("Total price is required");

        var registration = new Registration(eventId, attendeeList, contact, totalPrice);
        return Result<Registration>.Success(registration);
    }

    // EXISTING - Keep for backward compatibility
    public static Result<Registration> Create(Guid eventId, Guid userId, int quantity)
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

    // EXISTING - Keep for backward compatibility
    public static Result<Registration> CreateAnonymous(Guid eventId, AttendeeInfo attendeeInfo, int quantity)
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

    #endregion

    #region Queries

    /// <summary>
    /// Gets primary attendee (first in list or from AttendeeInfo for legacy)
    /// </summary>
    public string GetPrimaryAttendeeName()
    {
        if (_attendees.Any())
            return _attendees[0].Name;

        return AttendeeInfo?.Name ?? "Unknown";
    }

    /// <summary>
    /// Checks if registration uses new multi-attendee format
    /// </summary>
    public bool IsMultiAttendeeRegistration() => _attendees.Any();

    /// <summary>
    /// Gets contact email (from Contact or legacy AttendeeInfo)
    /// </summary>
    public string GetContactEmail()
    {
        if (Contact != null)
            return Contact.Email.Value;

        return AttendeeInfo?.Email.Value ?? string.Empty;
    }

    #endregion

    // ... existing methods (Cancel, Confirm, CheckIn, etc.) ...
}
```

---

## 4. Value Object Design

### 4.1 TicketPricing Value Object (NEW)

```csharp
namespace LankaConnect.Domain.Events.ValueObjects;

/// <summary>
/// Represents event ticket pricing configuration
/// Supports both single pricing and dual (adult/child) pricing
/// </summary>
public class TicketPricing : ValueObject
{
    public Money AdultPrice { get; }
    public Money? ChildPrice { get; } // null for single pricing
    public int? ChildAgeLimit { get; } // null for single pricing
    public bool IsDualPricing => ChildPrice != null;

    // Private constructor
    private TicketPricing(Money adultPrice, Money? childPrice, int? childAgeLimit)
    {
        AdultPrice = adultPrice;
        ChildPrice = childPrice;
        ChildAgeLimit = childAgeLimit;
    }

    // EF Core constructor
    private TicketPricing()
    {
        AdultPrice = null!;
    }

    /// <summary>
    /// Creates dual pricing (adult + child)
    /// </summary>
    public static Result<TicketPricing> CreateDual(Money adultPrice, Money childPrice, int childAgeLimit)
    {
        if (adultPrice == null)
            return Result<TicketPricing>.Failure("Adult price is required");

        if (childPrice == null)
            return Result<TicketPricing>.Failure("Child price is required");

        if (childAgeLimit < 1 || childAgeLimit > 18)
            return Result<TicketPricing>.Failure("Child age limit must be between 1 and 18");

        if (adultPrice.Currency != childPrice.Currency)
            return Result<TicketPricing>.Failure("Prices must use the same currency");

        if (childPrice.IsGreaterThan(adultPrice))
            return Result<TicketPricing>.Failure("Child price cannot exceed adult price");

        return Result<TicketPricing>.Success(
            new TicketPricing(adultPrice, childPrice, childAgeLimit));
    }

    /// <summary>
    /// Creates single pricing (backward compatible)
    /// </summary>
    public static Result<TicketPricing> CreateSingle(Money price)
    {
        if (price == null)
            return Result<TicketPricing>.Failure("Price is required");

        return Result<TicketPricing>.Success(
            new TicketPricing(price, null, null));
    }

    /// <summary>
    /// Calculates price for a single attendee based on age
    /// </summary>
    public Result<Money> CalculateForAttendee(int age)
    {
        if (age < 1)
            return Result<Money>.Failure("Age must be at least 1");

        if (!IsDualPricing)
            return Result<Money>.Success(AdultPrice);

        // Dual pricing logic
        if (age <= ChildAgeLimit!.Value)
            return Result<Money>.Success(ChildPrice!);

        return Result<Money>.Success(AdultPrice);
    }

    /// <summary>
    /// Calculates total price for multiple attendees
    /// Domain logic: Sum of individual attendee prices based on age
    /// </summary>
    public Result<Money> CalculateTotal(IEnumerable<AttendeeDetails> attendees)
    {
        if (attendees == null || !attendees.Any())
            return Result<Money>.Failure("At least one attendee is required");

        var attendeeList = attendees.ToList();
        var currency = AdultPrice.Currency;

        // Start with zero
        var total = Money.Zero(currency);

        foreach (var attendee in attendeeList)
        {
            var priceResult = CalculateForAttendee(attendee.Age);
            if (priceResult.IsFailure)
                return priceResult;

            var addResult = total.Add(priceResult.Value);
            if (addResult.IsFailure)
                return Result<Money>.Failure(addResult.Error);

            total = addResult.Value;
        }

        return Result<Money>.Success(total);
    }

    /// <summary>
    /// Gets pricing breakdown for display (e.g., "Adults $50, Children $25 (Under 12)")
    /// </summary>
    public string GetPricingDescription()
    {
        if (!IsDualPricing)
            return AdultPrice.ToString();

        return $"Adults {AdultPrice}, Children {ChildPrice} (Under {ChildAgeLimit})";
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return AdultPrice;
        if (ChildPrice != null) yield return ChildPrice;
        if (ChildAgeLimit.HasValue) yield return ChildAgeLimit.Value;
    }
}
```

### 4.2 AttendeeDetails Value Object (NEW)

```csharp
namespace LankaConnect.Domain.Events.ValueObjects;

/// <summary>
/// Represents individual attendee details (Name + Age)
/// Lightweight compared to AttendeeInfo (no contact info)
/// </summary>
public class AttendeeDetails : ValueObject
{
    public string Name { get; }
    public int Age { get; }

    private AttendeeDetails(string name, int age)
    {
        Name = name;
        Age = age;
    }

    // EF Core constructor
    private AttendeeDetails()
    {
        Name = string.Empty;
    }

    public static Result<AttendeeDetails> Create(string name, int age)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<AttendeeDetails>.Failure("Name is required");

        if (name.Length > 100)
            return Result<AttendeeDetails>.Failure("Name cannot exceed 100 characters");

        if (age < 1 || age > 150)
            return Result<AttendeeDetails>.Failure("Age must be between 1 and 150");

        return Result<AttendeeDetails>.Success(
            new AttendeeDetails(name.Trim(), age));
    }

    /// <summary>
    /// Checks if attendee qualifies as child based on age limit
    /// </summary>
    public bool IsChild(int childAgeLimit) => Age <= childAgeLimit;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
        yield return Age;
    }

    public override string ToString() => $"{Name} (Age {Age})";
}
```

### 4.3 RegistrationContact Value Object (NEW)

```csharp
namespace LankaConnect.Domain.Events.ValueObjects;

/// <summary>
/// Shared contact information for all attendees in a registration
/// Replaces individual contact fields in AttendeeInfo
/// </summary>
public class RegistrationContact : ValueObject
{
    public Email Email { get; }
    public PhoneNumber PhoneNumber { get; }
    public string Address { get; }

    private RegistrationContact(Email email, PhoneNumber phoneNumber, string address)
    {
        Email = email;
        PhoneNumber = phoneNumber;
        Address = address;
    }

    // EF Core constructor
    private RegistrationContact()
    {
        Email = null!;
        PhoneNumber = null!;
        Address = string.Empty;
    }

    public static Result<RegistrationContact> Create(
        string email,
        string phoneNumber,
        string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return Result<RegistrationContact>.Failure("Address is required");

        var emailResult = Email.Create(email);
        if (!emailResult.IsSuccess)
            return Result<RegistrationContact>.Failure($"Invalid email: {emailResult.Error}");

        var phoneResult = PhoneNumber.Create(phoneNumber);
        if (!phoneResult.IsSuccess)
            return Result<RegistrationContact>.Failure($"Invalid phone: {phoneResult.Error}");

        return Result<RegistrationContact>.Success(
            new RegistrationContact(emailResult.Value, phoneResult.Value, address.Trim()));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Email;
        yield return PhoneNumber;
        yield return Address;
    }
}
```

### 4.4 AttendeeInfo (DEPRECATED - Keep for Backward Compatibility)

```csharp
// NO CHANGES - Keep existing implementation
// Mark as deprecated in documentation
// Used only for legacy registrations

[Obsolete("Use AttendeeDetails + RegistrationContact for new registrations")]
public class AttendeeInfo : ValueObject
{
    // ... existing implementation unchanged ...
}
```

---

## 5. Domain Services

### 5.1 RegistrationPricingService (Optional - Can be Event method)

**Decision**: Pricing logic lives in **Event aggregate** as `CalculatePriceForAttendees()` method, NOT a separate domain service. This follows DDD principle that behavior should be close to data.

**Rationale**:
- Event owns pricing configuration (TicketPricing)
- Event enforces pricing invariants
- Registration is created with calculated price (stored, not recomputed)

---

## 6. Migration Strategy

### 6.1 Database Schema Changes

```sql
-- 1. Add new columns to events table (nullable for backward compatibility)
ALTER TABLE events
ADD COLUMN pricing JSONB;  -- Stores TicketPricing value object

-- 2. Add new columns to registrations table
ALTER TABLE registrations
ADD COLUMN attendees JSONB,  -- Array of AttendeeDetails
ADD COLUMN contact JSONB,    -- RegistrationContact value object
ADD COLUMN total_price_amount DECIMAL(18,2),
ADD COLUMN total_price_currency VARCHAR(3);

-- 3. Create GIN index for attendee queries (PostgreSQL JSONB)
CREATE INDEX idx_registrations_attendees_gin ON registrations USING GIN (attendees);

-- 4. Migrate existing data (backward compatible)
UPDATE events
SET pricing = jsonb_build_object(
    'AdultPrice', jsonb_build_object(
        'Amount', ticket_price_amount,
        'Currency', ticket_price_currency
    ),
    'ChildPrice', NULL,
    'ChildAgeLimit', NULL,
    'IsDualPricing', false
)
WHERE ticket_price_amount IS NOT NULL;

-- 5. For existing registrations with AttendeeInfo, leave attendees NULL
-- New code will check: if attendees.Any() use new format, else use legacy
```

### 6.2 EF Core Configuration

```csharp
// EventConfiguration.cs
public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        // ... existing configuration ...

        // Configure TicketPricing as owned entity (JSONB)
        builder.OwnsOne(e => e.Pricing, pricingBuilder =>
        {
            pricingBuilder.ToJson("pricing");

            // Configure nested Money value objects
            pricingBuilder.OwnsOne(p => p.AdultPrice, adultBuilder =>
            {
                adultBuilder.Property(m => m.Amount).HasColumnName("adult_price_amount");
                adultBuilder.Property(m => m.Currency).HasColumnName("adult_price_currency")
                    .HasConversion<string>();
            });

            pricingBuilder.OwnsOne(p => p.ChildPrice, childBuilder =>
            {
                childBuilder.Property(m => m.Amount).HasColumnName("child_price_amount");
                childBuilder.Property(m => m.Currency).HasColumnName("child_price_currency")
                    .HasConversion<string>();
            });

            pricingBuilder.Property(p => p.ChildAgeLimit)
                .HasColumnName("child_age_limit");
        });
    }
}

// RegistrationConfiguration.cs
public class RegistrationConfiguration : IEntityTypeConfiguration<Registration>
{
    public void Configure(EntityTypeBuilder<Registration> builder)
    {
        // ... existing configuration ...

        // Configure Attendees as owned collection (JSONB array)
        builder.OwnsMany(r => r.Attendees, attendeeBuilder =>
        {
            attendeeBuilder.ToJson("attendees");
            attendeeBuilder.Property(a => a.Name).HasColumnName("name");
            attendeeBuilder.Property(a => a.Age).HasColumnName("age");
        });

        // Configure Contact as owned entity (JSONB)
        builder.OwnsOne(r => r.Contact, contactBuilder =>
        {
            contactBuilder.ToJson("contact");

            contactBuilder.OwnsOne(c => c.Email, emailBuilder =>
            {
                emailBuilder.Property(e => e.Value).HasColumnName("email");
            });

            contactBuilder.OwnsOne(c => c.PhoneNumber, phoneBuilder =>
            {
                phoneBuilder.Property(p => p.Value).HasColumnName("phone_number");
            });

            contactBuilder.Property(c => c.Address).HasColumnName("address");
        });

        // Configure TotalPrice as owned entity
        builder.OwnsOne(r => r.TotalPrice, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount)
                .HasColumnName("total_price_amount")
                .HasColumnType("decimal(18,2)");

            moneyBuilder.Property(m => m.Currency)
                .HasColumnName("total_price_currency")
                .HasConversion<string>()
                .HasMaxLength(3);
        });
    }
}
```

### 6.3 Migration Code

```csharp
// C:\Work\LankaConnect\src\LankaConnect.Infrastructure\Migrations\[timestamp]_AddDualPricingMultiAttendee.cs

public partial class AddDualPricingMultiAttendee : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add new columns to events
        migrationBuilder.AddColumn<string>(
            name: "pricing",
            table: "events",
            type: "jsonb",
            nullable: true);

        // Add new columns to registrations
        migrationBuilder.AddColumn<string>(
            name: "attendees",
            table: "registrations",
            type: "jsonb",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "contact",
            table: "registrations",
            type: "jsonb",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "total_price_amount",
            table: "registrations",
            type: "decimal(18,2)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "total_price_currency",
            table: "registrations",
            type: "varchar(3)",
            nullable: true);

        // Create GIN index for attendees JSONB queries
        migrationBuilder.Sql(
            "CREATE INDEX idx_registrations_attendees_gin ON registrations USING GIN (attendees);");

        // Migrate existing single pricing to new format
        migrationBuilder.Sql(@"
            UPDATE events
            SET pricing = jsonb_build_object(
                'AdultPrice', jsonb_build_object(
                    'Amount', ticket_price_amount,
                    'Currency', ticket_price_currency
                ),
                'ChildPrice', NULL,
                'ChildAgeLimit', NULL,
                'IsDualPricing', false
            )
            WHERE ticket_price_amount IS NOT NULL;
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "pricing", table: "events");
        migrationBuilder.DropColumn(name: "attendees", table: "registrations");
        migrationBuilder.DropColumn(name: "contact", table: "registrations");
        migrationBuilder.DropColumn(name: "total_price_amount", table: "registrations");
        migrationBuilder.DropColumn(name: "total_price_currency", table: "registrations");

        migrationBuilder.Sql("DROP INDEX IF EXISTS idx_registrations_attendees_gin;");
    }
}
```

### 6.4 Data Migration Strategy

**Phase 1: Deploy Schema (Non-Breaking)**
- Add new columns as nullable
- Keep existing columns (ticket_price_amount, attendee_info, quantity)
- Application reads from both old and new formats

**Phase 2: Dual Write (Transition Period)**
- New registrations use new format (attendees + contact)
- Old registrations remain unchanged
- Query layer handles both formats

**Phase 3: Backfill (Optional)**
```csharp
// Application service to migrate old registrations to new format
public async Task MigrateLegacyRegistrations()
{
    var legacyRegistrations = await _context.Registrations
        .Where(r => r.Attendees == null || !r.Attendees.Any())
        .Where(r => r.AttendeeInfo != null)
        .ToListAsync();

    foreach (var reg in legacyRegistrations)
    {
        // Convert AttendeeInfo to AttendeeDetails + Contact
        var attendee = AttendeeDetails.Create(
            reg.AttendeeInfo.Name,
            reg.AttendeeInfo.Age).Value;

        var contact = RegistrationContact.Create(
            reg.AttendeeInfo.Email.Value,
            reg.AttendeeInfo.PhoneNumber.Value,
            reg.AttendeeInfo.Address).Value;

        // Update using domain method (not direct property access)
        // Would need to add migration method to Registration entity
    }

    await _context.SaveChangesAsync();
}
```

**Phase 4: Cleanup (Future)**
- After all registrations migrated
- Drop old columns (ticket_price_amount, attendee_info)
- Make new columns non-nullable

---

## 7. Consequences

### 7.1 Positive Consequences

1. **Type Safety**: Value objects provide compile-time guarantees
2. **Domain Clarity**: Pricing logic is explicit in TicketPricing
3. **Flexibility**: JSONB allows schema evolution without migrations
4. **Performance**: No JOINs required, GIN indexes for fast queries
5. **Backward Compatibility**: Existing code continues to work
6. **Clean Architecture**: Domain doesn't depend on EF Core
7. **Testability**: Value objects are easy to unit test

### 7.2 Negative Consequences

1. **Query Complexity**: Reporting queries need to handle JSONB
2. **Migration Period**: Dual format handling during transition
3. **Learning Curve**: Team needs to understand JSONB queries

### 7.3 Migration Risks

| Risk | Mitigation |
|------|------------|
| Data loss during migration | Nullable columns, preserve old data |
| Query performance degradation | GIN indexes on JSONB columns |
| Application compatibility | Feature flags for gradual rollout |
| Rollback complexity | Keep old columns during transition |

---

## 8. Implementation Roadmap

### Phase 1: Value Objects & Domain Logic (Week 1)

**Files to Create**:
```
C:\Work\LankaConnect\src\LankaConnect.Domain\Events\ValueObjects\TicketPricing.cs
C:\Work\LankaConnect\src\LankaConnect.Domain\Events\ValueObjects\AttendeeDetails.cs
C:\Work\LankaConnect\src\LankaConnect.Domain\Events\ValueObjects\RegistrationContact.cs
C:\Work\LankaConnect\src\LankaConnect.Domain\Events\DomainEvents\EventPricingUpdatedEvent.cs
```

**Files to Modify**:
```
C:\Work\LankaConnect\src\LankaConnect.Domain\Events\Event.cs
C:\Work\LankaConnect\src\LankaConnect.Domain\Events\Registration.cs
```

**Tests to Create**:
```
C:\Work\LankaConnect\tests\LankaConnect.Application.Tests\Events\Domain\TicketPricingTests.cs
C:\Work\LankaConnect\tests\LankaConnect.Application.Tests\Events\Domain\AttendeeDetailsTests.cs
C:\Work\LankaConnect\tests\LankaConnect.Application.Tests\Events\Domain\RegistrationContactTests.cs
C:\Work\LankaConnect\tests\LankaConnect.Application.Tests\Events\Domain\MultiAttendeeRegistrationTests.cs
```

**Deliverables**:
- [ ] TicketPricing value object with unit tests
- [ ] AttendeeDetails value object with unit tests
- [ ] RegistrationContact value object with unit tests
- [ ] Event.SetDualPricing() method
- [ ] Event.RegisterWithAttendees() method
- [ ] Registration.CreateWithAttendees() factory
- [ ] 90% test coverage for new domain logic

### Phase 2: Infrastructure & Database (Week 2)

**Files to Create**:
```
C:\Work\LankaConnect\src\LankaConnect.Infrastructure\Migrations\[timestamp]_AddDualPricingMultiAttendee.cs
C:\Work\LankaConnect\src\LankaConnect.Infrastructure\Migrations\[timestamp]_MigrateLegacyPricingData.cs
```

**Files to Modify**:
```
C:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Configurations\EventConfiguration.cs
C:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Configurations\RegistrationConfiguration.cs
```

**Deliverables**:
- [ ] EF Core configurations for JSONB columns
- [ ] Database migration scripts
- [ ] GIN indexes for JSONB queries
- [ ] Data migration for existing events
- [ ] Integration tests with PostgreSQL

### Phase 3: Application Layer (Week 3)

**Files to Create**:
```
C:\Work\LankaConnect\src\LankaConnect.Application\Events\Commands\RegisterWithAttendees\RegisterWithAttendeesCommand.cs
C:\Work\LankaConnect\src\LankaConnect\Application\Events\Commands\RegisterWithAttendees\RegisterWithAttendeesCommandHandler.cs
C:\Work\LankaConnect\src\LankaConnect\Application\Events\Commands\SetDualPricing\SetDualPricingCommand.cs
C:\Work\LankaConnect\src\LankaConnect\Application\Events\Commands\SetDualPricing\SetDualPricingCommandHandler.cs
C:\Work\LankaConnect\src\LankaConnect.Application\Events\DTOs\AttendeeDto.cs
C:\Work\LankaConnect\src\LankaConnect.Application\Events\DTOs\RegistrationContactDto.cs
```

**Deliverables**:
- [ ] CQRS commands for multi-attendee registration
- [ ] CQRS commands for dual pricing setup
- [ ] DTOs for API layer
- [ ] FluentValidation validators
- [ ] Application service tests

### Phase 4: API & Frontend (Week 4)

**Files to Modify**:
```
C:\Work\LankaConnect\web\src\infrastructure\api\repositories\event-repository.ts
C:\Work\LankaConnect\web\src\presentation\components\features\events\EventRegistrationForm.tsx
C:\Work\LankaConnect\web\src\presentation\components\features\events\EventPricingForm.tsx
```

**Deliverables**:
- [ ] API endpoints for multi-attendee registration
- [ ] Frontend form for adding multiple attendees
- [ ] Price calculation display (live update)
- [ ] Event creation form with dual pricing
- [ ] E2E tests for registration flow

---

## Domain Method Signatures Summary

### Event Aggregate

```csharp
// Pricing Configuration
Result SetDualPricing(Money adultPrice, Money childPrice, int childAgeLimit)
Result SetSinglePricing(Money price)
Result SetFree()
bool RequiresAgeBasedPricing()
bool IsFree()
Result<Money> CalculatePriceForAttendees(IEnumerable<AttendeeDetails> attendees)

// Multi-Attendee Registration
Result<Registration> RegisterWithAttendees(Guid userId, IEnumerable<AttendeeDetails> attendees, RegistrationContact contact)
Result<Registration> RegisterAnonymousWithAttendees(IEnumerable<AttendeeDetails> attendees, RegistrationContact contact)
```

### Registration Entity

```csharp
// Factory Methods
static Result<Registration> CreateWithAttendees(Guid eventId, Guid userId, IEnumerable<AttendeeDetails> attendees, RegistrationContact contact, Money totalPrice)
static Result<Registration> CreateAnonymousWithAttendees(Guid eventId, IEnumerable<AttendeeDetails> attendees, RegistrationContact contact, Money totalPrice)

// Queries
string GetPrimaryAttendeeName()
bool IsMultiAttendeeRegistration()
string GetContactEmail()
int ActualQuantity { get; }
```

### TicketPricing Value Object

```csharp
static Result<TicketPricing> CreateDual(Money adultPrice, Money childPrice, int childAgeLimit)
static Result<TicketPricing> CreateSingle(Money price)
Result<Money> CalculateForAttendee(int age)
Result<Money> CalculateTotal(IEnumerable<AttendeeDetails> attendees)
string GetPricingDescription()
```

### AttendeeDetails Value Object

```csharp
static Result<AttendeeDetails> Create(string name, int age)
bool IsChild(int childAgeLimit)
```

### RegistrationContact Value Object

```csharp
static Result<RegistrationContact> Create(string email, string phoneNumber, string address)
```

---

## Domain Invariants

### Event Aggregate

1. Child age limit must be between 1-18
2. Child price cannot exceed adult price
3. Dual pricing requires same currency for both prices
4. Free events have no pricing configured
5. Max 10 attendees per registration
6. Total capacity includes all attendees across all registrations

### TicketPricing Value Object

1. Adult price is always required
2. Child price and age limit are both present or both absent (cohesion)
3. Currencies must match for addition operations

### Registration Entity

1. At least 1 attendee required (min)
2. Maximum 10 attendees per registration (max)
3. Either UserId OR (Attendees + Contact), not both null
4. Quantity = Attendees.Count for new registrations
5. TotalPrice must be calculated by Event aggregate (not user input)

---

## Backward Compatibility Matrix

| Scenario | Old Format | New Format | Handled By |
|----------|-----------|------------|------------|
| Free Event | TicketPrice = null | Pricing = null | Both work |
| Single Price Event | TicketPrice = Money | Pricing.IsDualPricing = false | Read from both |
| Dual Price Event | N/A | Pricing.IsDualPricing = true | New only |
| Single Attendee (Auth) | UserId + Quantity | UserId + Attendees[1] | Both work |
| Single Attendee (Anon) | AttendeeInfo + Quantity | Attendees[1] + Contact | Both work |
| Multiple Attendees | N/A | Attendees[n] + Contact | New only |

---

## Database Schema Evolution

### Before (Current)

```sql
events:
  id UUID PRIMARY KEY
  ticket_price_amount DECIMAL(18,2)
  ticket_price_currency VARCHAR(3)

registrations:
  id UUID PRIMARY KEY
  event_id UUID
  user_id UUID NULL
  attendee_info JSONB NULL
  quantity INT
```

### After (New)

```sql
events:
  id UUID PRIMARY KEY
  ticket_price_amount DECIMAL(18,2)  -- DEPRECATED, keep for queries
  ticket_price_currency VARCHAR(3)   -- DEPRECATED
  pricing JSONB                      -- NEW: TicketPricing value object

registrations:
  id UUID PRIMARY KEY
  event_id UUID
  user_id UUID NULL
  attendee_info JSONB NULL           -- DEPRECATED, keep for legacy
  quantity INT                       -- DEPRECATED, use attendees.length
  attendees JSONB                    -- NEW: Array of AttendeeDetails
  contact JSONB                      -- NEW: RegistrationContact
  total_price_amount DECIMAL(18,2)   -- NEW: Calculated price
  total_price_currency VARCHAR(3)    -- NEW
```

---

## Example JSONB Structure

### Event Pricing (JSONB)

```json
{
  "AdultPrice": {
    "Amount": 50.00,
    "Currency": "USD"
  },
  "ChildPrice": {
    "Amount": 25.00,
    "Currency": "USD"
  },
  "ChildAgeLimit": 12,
  "IsDualPricing": true
}
```

### Registration Attendees (JSONB Array)

```json
[
  { "Name": "John Doe", "Age": 35 },
  { "Name": "Jane Doe", "Age": 32 },
  { "Name": "Tommy Doe", "Age": 8 },
  { "Name": "Sally Doe", "Age": 5 }
]
```

### Registration Contact (JSONB)

```json
{
  "Email": { "Value": "john.doe@example.com" },
  "PhoneNumber": { "Value": "+1-555-0123" },
  "Address": "123 Main St, Anytown, USA"
}
```

---

## PostgreSQL Query Examples

### Find all registrations with children under 10

```sql
SELECT r.id, r.attendees
FROM registrations r
CROSS JOIN LATERAL jsonb_array_elements(r.attendees) AS attendee
WHERE (attendee->>'Age')::int < 10;
```

### Calculate total revenue from dual-priced events

```sql
SELECT
  e.id,
  e.title,
  SUM(r.total_price_amount) AS total_revenue
FROM events e
JOIN registrations r ON r.event_id = e.id
WHERE e.pricing->>'IsDualPricing' = 'true'
  AND r.status = 'Confirmed'
GROUP BY e.id, e.title;
```

### Count adults vs children per event

```sql
SELECT
  e.id,
  e.title,
  COUNT(*) FILTER (
    WHERE (attendee->>'Age')::int > (e.pricing->>'ChildAgeLimit')::int
  ) AS adults,
  COUNT(*) FILTER (
    WHERE (attendee->>'Age')::int <= (e.pricing->>'ChildAgeLimit')::int
  ) AS children
FROM events e
JOIN registrations r ON r.event_id = e.id
CROSS JOIN LATERAL jsonb_array_elements(r.attendees) AS attendee
WHERE e.pricing->>'IsDualPricing' = 'true'
GROUP BY e.id, e.title;
```

---

## Conclusion

This architecture provides a clean, type-safe, and backward-compatible solution for dual ticket pricing with multiple attendees while adhering to Clean Architecture and DDD principles. The use of value objects in JSONB columns leverages PostgreSQL's strengths while maintaining domain model purity.

**Next Steps**: Review this ADR with the team, then proceed to Phase 1 implementation (Value Objects & Domain Logic).

---

## References

- Clean Architecture by Robert C. Martin
- Domain-Driven Design by Eric Evans
- PostgreSQL JSONB Best Practices: https://www.postgresql.org/docs/current/datatype-json.html
- EF Core Value Objects: https://learn.microsoft.com/en-us/ef/core/modeling/owned-entities
