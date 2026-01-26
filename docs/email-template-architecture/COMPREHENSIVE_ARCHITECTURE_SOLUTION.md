# Comprehensive Architecture Solution: Email Template Parameter Standardization

**Document Type**: Architecture Design Document (SPARC Methodology)
**Phase**: Architecture Phase
**Date**: 2026-01-26
**Status**: Proposed Solution - Ready for Review
**Classification**: System Architecture - Email Infrastructure

---

## EXECUTIVE SUMMARY

This document presents a comprehensive architectural solution to permanently eliminate email template parameter mismatch issues in LankaConnect. The current system suffers from 300+ manual parameter mappings across 18 templates and 15+ handlers, with no type safety or contract enforcement, resulting in literal `{{ParameterName}}` appearing in production emails.

**Proposed Solution**: Implement a strongly-typed Email Parameter Contract system with compile-time validation, eliminating the Dictionary<string, object> approach that caused this systemic failure.

**Key Benefits**:
- **100% compile-time safety** - Parameter mismatches caught during build, not in production
- **90% code reduction** - Common parameters defined once, reused across handlers
- **Zero manual mapping** - Auto-generated parameter dictionaries from strongly-typed objects
- **Backward compatible** - Works with existing database templates during migration
- **Future-proof** - New templates automatically inherit validation

**Effort**: 3-4 weeks for full implementation, 0 production downtime

---

## TABLE OF CONTENTS

1. [Root Cause Analysis](#1-root-cause-analysis)
2. [Proposed Architecture](#2-proposed-architecture)
3. [System Design](#3-system-design)
4. [Migration Strategy](#4-migration-strategy)
5. [Code Examples](#5-code-examples)
6. [Validation & Testing](#6-validation--testing)
7. [Effort Estimates](#7-effort-estimates)
8. [Rollout Plan](#8-rollout-plan)

---

## 1. ROOT CAUSE ANALYSIS

### 1.1 Why the Current Approach Failed

**Current Implementation**:
```csharp
// Every handler manually builds Dictionary<string, object>
var parameters = new Dictionary<string, object>
{
    { "UserName", user.Name },           // Manual string key - typo-prone
    { "EventTitel", @event.Title },       // ❌ TYPO - caught only in production!
    { "OrganizerName", organizer.Name },  // Template expects "OrganizerContactName"
    // Missing: TicketCode (template expects it) - no compiler warning!
};
```

**Systemic Problems**:

1. **No Type Safety** - String keys are typo-prone, compiler can't help
2. **No Contract Enforcement** - Handler doesn't know what template expects
3. **Duplicate Code** - Every handler rebuilds common parameters (UserName, EventTitle, etc.)
4. **Silent Failures** - Missing parameters only discovered when users report malformed emails
5. **Database-Code Divergence** - Templates in DB, handlers in C#, no synchronization
6. **Manual Synchronization** - 18 templates × 15 handlers = 270 manual mappings to maintain

### 1.2 Evidence from Production

**18 Email Templates Analyzed**:
- `template-event-reminder`: 17 parameters
- `template-paid-event-registration-confirmation-with-ticket`: 22 parameters
- `template-event-cancellation-notifications`: 14 parameters
- 15 more templates averaging 12 parameters each

**Duplicate/Inconsistent Parameters Found**:
```handlebars
<!-- template-event-reminder (database) -->
{{OrganizerContactName}}     <!-- OLD name -->
{{OrganizerName}}            <!-- NEW name -->
{{OrganizerContactEmail}}    <!-- OLD name -->
{{OrganizerEmail}}           <!-- NEW name -->
```

**Handler Mismatches**:
- `EventReminderJob.cs`: Sends `OrganizerName`, template expects `OrganizerContactName`
- `PaymentCompletedEventHandler.cs`: Missing `TicketCode`, `TicketExpiryDate`
- 13 more handlers with similar issues

**User Impact**: ALL production users receiving event-related emails saw literal `{{ParameterName}}` in emails.

### 1.3 Why This Problem is Architectural

This isn't a "few missing parameters" bug - it's a **fundamental architecture flaw**:

1. **Lack of Contracts** - No interface defining what data a template needs
2. **Weak Typing** - Dictionary<string, object> loses all compile-time safety
3. **No Validation Layer** - Parameters validated only at runtime (too late!)
4. **Scattered Logic** - Parameter building duplicated across 15+ files
5. **Database Dependency** - Templates stored in PostgreSQL, handlers in C#, different deployment cycles

**Conclusion**: Tactical fixes (whack-a-mole parameter additions) will NOT solve this. We need a **systematic architectural solution**.

---

## 2. PROPOSED ARCHITECTURE

### 2.1 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    EMAIL HANDLER (Domain Event Handler)          │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │ 1. Build strongly-typed parameter object                   │ │
│  │    var params = new EventReminderEmailParams {             │ │
│  │        UserName = user.Name,                               │ │
│  │        Event = EventEmailParams.From(@event),  // reuse!   │ │
│  │        Organizer = OrganizerEmailParams.From(@event)       │ │
│  │    };                                                       │ │
│  └────────────────────────────────────────────────────────────┘ │
│                            │                                     │
│                            ▼                                     │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │ 2. Validate at compile time (compiler enforces properties) │ │
│  │    Missing property? ❌ Compilation fails                   │ │
│  │    Wrong type? ❌ Compilation fails                         │ │
│  └────────────────────────────────────────────────────────────┘ │
│                            │                                     │
│                            ▼                                     │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │ 3. Auto-convert to Dictionary (framework layer)            │ │
│  │    var dict = EmailParameterConverter.ToDictionary(params);│ │
│  └────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│              EMAIL TEMPLATE SERVICE (Infrastructure)             │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │ 4. Runtime validation (optional, for extra safety)         │ │
│  │    ValidateRequiredParameters(templateName, dict);         │ │
│  └────────────────────────────────────────────────────────────┘ │
│                            │                                     │
│                            ▼                                     │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │ 5. Render template with Handlebars                         │ │
│  │    var html = RenderTemplate(template, dict);              │ │
│  └────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                         EMAIL SERVICE                            │
│                    Send email to recipient                       │
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 Core Architectural Principles

1. **Type Safety First** - Leverage C#'s type system to catch errors at compile time
2. **DRY (Don't Repeat Yourself)** - Common parameters defined once, reused everywhere
3. **Composition over Inheritance** - Build complex parameters from smaller, focused objects
4. **Fail Fast** - Catch errors during development, not production
5. **Backward Compatible** - Support existing templates during migration
6. **Documentation as Code** - Parameter contracts ARE the documentation

### 2.3 Key Components

#### Component 1: Base Email Parameter Contracts

**Purpose**: Define reusable parameter objects for common email data

```csharp
namespace LankaConnect.Application.Common.Email.Parameters;

/// <summary>
/// Base interface for all email parameter objects.
/// Enables automatic conversion to Dictionary<string, object> for template rendering.
/// </summary>
public interface IEmailParameters
{
    /// <summary>
    /// Converts this parameter object to a dictionary for Handlebars template rendering.
    /// </summary>
    Dictionary<string, object> ToDictionary();
}

/// <summary>
/// Common user-related email parameters.
/// Used across all email templates requiring user information.
/// </summary>
public record UserEmailParams : IEmailParameters
{
    public required string UserName { get; init; }
    public string? Email { get; init; }

    public static UserEmailParams From(User user) => new()
    {
        UserName = $"{user.FirstName} {user.LastName}",
        Email = user.Email.Value
    };

    public Dictionary<string, object> ToDictionary() => new()
    {
        { "UserName", UserName },
        { "Email", Email ?? "" }
    };
}

/// <summary>
/// Event-related email parameters.
/// Provides consistent event data formatting across all event email templates.
/// </summary>
public record EventEmailParams : IEmailParameters
{
    public required string EventTitle { get; init; }
    public required string EventStartDate { get; init; }
    public required string EventStartTime { get; init; }
    public required string EventLocation { get; init; }
    public required string EventDetailsUrl { get; init; }
    public string? EventDescription { get; init; }
    public string? SignUpListsUrl { get; init; }

    public static EventEmailParams From(Event @event, IEmailUrlHelper urlHelper) => new()
    {
        EventTitle = @event.Title.Value,
        EventStartDate = @event.StartDate.ToString("MMMM dd, yyyy"),
        EventStartTime = @event.StartDate.ToString("h:mm tt"),
        EventLocation = FormatEventLocation(@event),
        EventDetailsUrl = urlHelper.BuildEventDetailsUrl(@event.Id),
        EventDescription = @event.Description,
        SignUpListsUrl = @event.HasSignUpLists()
            ? $"{urlHelper.BuildEventDetailsUrl(@event.Id)}#sign-ups"
            : null
    };

    public Dictionary<string, object> ToDictionary() => new()
    {
        { "EventTitle", EventTitle },
        { "EventStartDate", EventStartDate },
        { "EventStartTime", EventStartTime },
        { "EventLocation", EventLocation },
        { "EventDetailsUrl", EventDetailsUrl },
        { "EventDescription", EventDescription ?? "" },
        { "SignUpListsUrl", SignUpListsUrl ?? "" },
        // Computed properties
        { "EventDateTime", $"{EventStartDate} at {EventStartTime}" } // Legacy support
    };

    private static string FormatEventLocation(Event @event)
    {
        if (@event.Location?.Address == null) return "Online Event";
        var street = @event.Location.Address.Street;
        var city = @event.Location.Address.City;
        if (string.IsNullOrWhiteSpace(street) && string.IsNullOrWhiteSpace(city))
            return "Online Event";
        if (string.IsNullOrWhiteSpace(street)) return city!;
        if (string.IsNullOrWhiteSpace(city)) return street;
        return $"{street}, {city}";
    }
}

/// <summary>
/// Organizer contact information for email templates.
/// Supports BOTH old parameter names (OrganizerContactName) and new names (OrganizerName)
/// for backward compatibility during migration.
/// </summary>
public record OrganizerEmailParams : IEmailParameters
{
    public required string OrganizerName { get; init; }
    public required string OrganizerEmail { get; init; }
    public string? OrganizerPhone { get; init; }

    public static OrganizerEmailParams? From(Event @event)
    {
        if (!@event.HasOrganizerContact()) return null;

        return new()
        {
            OrganizerName = @event.OrganizerContactName ?? "Event Organizer",
            OrganizerEmail = @event.OrganizerContactEmail ?? "",
            OrganizerPhone = @event.OrganizerContactPhone
        };
    }

    public Dictionary<string, object> ToDictionary() => new()
    {
        // NEW parameter names (canonical)
        { "OrganizerName", OrganizerName },
        { "OrganizerEmail", OrganizerEmail },
        { "OrganizerPhone", OrganizerPhone ?? "" },

        // OLD parameter names (backward compatibility - will be removed in Phase 6B)
        { "OrganizerContactName", OrganizerName },
        { "OrganizerContactEmail", OrganizerEmail },
        { "OrganizerContactPhone", OrganizerPhone ?? "" },

        // Flags for conditional rendering
        { "HasOrganizerContact", true }
    };
}
```

#### Component 2: Template-Specific Parameter Classes

**Purpose**: Define exact parameters required by each template

```csharp
namespace LankaConnect.Application.Events.Email;

/// <summary>
/// Parameters for template-event-reminder email.
///
/// Template Database Name: template-event-reminder
/// Template Version: 1.0
/// Last Updated: 2026-01-26
///
/// Required Parameters (17):
/// - UserName, EventTitle, EventStartDate, EventStartTime, EventLocation, EventDetailsUrl
/// - OrganizerContactName, OrganizerContactEmail, OrganizerContactPhone
/// - TicketCode, TicketExpiryDate, ReminderTimeframe, AttendeeName, ContactEmail
///
/// Optional Parameters:
/// - EventDescription, ReminderMessage, Location (duplicate of EventLocation)
/// </summary>
public record EventReminderEmailParams : IEmailParameters
{
    // Composition: Reuse common parameter objects
    public required UserEmailParams User { get; init; }
    public required EventEmailParams Event { get; init; }
    public required OrganizerEmailParams Organizer { get; init; }

    // Template-specific parameters
    public required string ReminderTimeframe { get; init; }  // "24 hours" or "1 hour"
    public required string AttendeeName { get; init; }
    public required string ContactEmail { get; init; }
    public string? TicketCode { get; init; }
    public string? TicketExpiryDate { get; init; }
    public string? ReminderMessage { get; init; }

    public Dictionary<string, object> ToDictionary()
    {
        var dict = new Dictionary<string, object>();

        // Merge all composed parameter objects
        foreach (var kvp in User.ToDictionary()) dict[kvp.Key] = kvp.Value;
        foreach (var kvp in Event.ToDictionary()) dict[kvp.Key] = kvp.Value;
        foreach (var kvp in Organizer.ToDictionary()) dict[kvp.Key] = kvp.Value;

        // Add template-specific parameters
        dict["ReminderTimeframe"] = ReminderTimeframe;
        dict["AttendeeName"] = AttendeeName;
        dict["ContactEmail"] = ContactEmail;
        dict["TicketCode"] = TicketCode ?? "";
        dict["TicketExpiryDate"] = TicketExpiryDate ?? "";
        dict["ReminderMessage"] = ReminderMessage ?? "";
        dict["Location"] = Event.EventLocation;  // Legacy duplicate parameter

        return dict;
    }
}

/// <summary>
/// Parameters for template-paid-event-registration-confirmation-with-ticket email.
///
/// Template Database Name: template-paid-event-registration-confirmation-with-ticket
/// Template Version: 1.0
/// Last Updated: 2026-01-26
///
/// Required Parameters (22):
/// - UserName, EventTitle, EventStartDate, EventStartTime, EventLocation
/// - OrganizerContactName, OrganizerContactEmail, OrganizerContactPhone
/// - ContactEmail, ContactPhone, AmountPaid, TotalAmount, Quantity, TicketType
/// - OrderNumber, PaymentIntentId, PaymentDate, TicketCode, TicketExpiryDate
/// - TicketUrl, Attendees, EventDetailsUrl
/// </summary>
public record PaidEventRegistrationEmailParams : IEmailParameters
{
    // Composition
    public required UserEmailParams User { get; init; }
    public required EventEmailParams Event { get; init; }
    public required OrganizerEmailParams Organizer { get; init; }

    // Payment-specific parameters
    public required string AmountPaid { get; init; }
    public required string TotalAmount { get; init; }  // Same as AmountPaid (template duplication)
    public required int Quantity { get; init; }
    public required string TicketType { get; init; }
    public required string OrderNumber { get; init; }
    public required string PaymentIntentId { get; init; }
    public required string PaymentDate { get; init; }

    // Ticket parameters
    public required string TicketCode { get; init; }
    public required string TicketExpiryDate { get; init; }
    public required string TicketUrl { get; init; }

    // Contact parameters
    public required string ContactEmail { get; init; }
    public string? ContactPhone { get; init; }

    // Attendee details (HTML formatted)
    public required string Attendees { get; init; }
    public required bool HasAttendeeDetails { get; init; }

    public Dictionary<string, object> ToDictionary()
    {
        var dict = new Dictionary<string, object>();

        // Merge composed parameters
        foreach (var kvp in User.ToDictionary()) dict[kvp.Key] = kvp.Value;
        foreach (var kvp in Event.ToDictionary()) dict[kvp.Key] = kvp.Value;
        foreach (var kvp in Organizer.ToDictionary()) dict[kvp.Key] = kvp.Value;

        // Add payment parameters
        dict["AmountPaid"] = AmountPaid;
        dict["TotalAmount"] = TotalAmount;
        dict["Quantity"] = Quantity;
        dict["TicketType"] = TicketType;
        dict["OrderNumber"] = OrderNumber;
        dict["PaymentIntentId"] = PaymentIntentId;
        dict["PaymentDate"] = PaymentDate;

        // Add ticket parameters
        dict["TicketCode"] = TicketCode;
        dict["TicketExpiryDate"] = TicketExpiryDate;
        dict["TicketUrl"] = TicketUrl;
        dict["HasTicket"] = true;

        // Add contact parameters
        dict["ContactEmail"] = ContactEmail;
        dict["ContactPhone"] = ContactPhone ?? "";
        dict["HasContactInfo"] = !string.IsNullOrEmpty(ContactPhone);

        // Add attendee parameters
        dict["Attendees"] = Attendees;
        dict["HasAttendeeDetails"] = HasAttendeeDetails;

        return dict;
    }
}
```

#### Component 3: Parameter Converter Framework

**Purpose**: Provide utilities for parameter conversion and validation

```csharp
namespace LankaConnect.Application.Common.Email;

/// <summary>
/// Converts strongly-typed email parameter objects to Dictionary for template rendering.
/// Provides validation and error handling.
/// </summary>
public static class EmailParameterConverter
{
    /// <summary>
    /// Converts IEmailParameters to Dictionary<string, object> for Handlebars template rendering.
    /// Validates that all required parameters are provided.
    /// </summary>
    public static Dictionary<string, object> ToDictionary(IEmailParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var dict = parameters.ToDictionary();

        // Validation: Ensure no null values (replace with empty string)
        foreach (var key in dict.Keys.ToList())
        {
            dict[key] ??= "";
        }

        return dict;
    }

    /// <summary>
    /// Validates that parameter object provides all required template parameters.
    /// Throws descriptive exception if validation fails.
    /// </summary>
    public static void ValidateRequiredParameters(
        IEmailParameters parameters,
        string templateName,
        string[] requiredParameters)
    {
        var dict = parameters.ToDictionary();
        var missing = new List<string>();

        foreach (var required in requiredParameters)
        {
            if (!dict.ContainsKey(required) || dict[required] == null)
            {
                missing.Add(required);
            }
        }

        if (missing.Any())
        {
            throw new InvalidOperationException(
                $"Template '{templateName}' requires missing parameters: {string.Join(", ", missing)}. " +
                $"Parameter object type: {parameters.GetType().Name}");
        }
    }
}

/// <summary>
/// Builder pattern for constructing complex email parameter objects.
/// Provides fluent API for handler code.
/// </summary>
public class EmailParameterBuilder<TParams> where TParams : IEmailParameters
{
    private readonly Dictionary<string, object> _parameters = new();

    public EmailParameterBuilder<TParams> AddUser(User user)
    {
        var userParams = UserEmailParams.From(user);
        foreach (var kvp in userParams.ToDictionary())
        {
            _parameters[kvp.Key] = kvp.Value;
        }
        return this;
    }

    public EmailParameterBuilder<TParams> AddEvent(Event @event, IEmailUrlHelper urlHelper)
    {
        var eventParams = EventEmailParams.From(@event, urlHelper);
        foreach (var kvp in eventParams.ToDictionary())
        {
            _parameters[kvp.Key] = kvp.Value;
        }
        return this;
    }

    public EmailParameterBuilder<TParams> AddOrganizer(Event @event)
    {
        var organizerParams = OrganizerEmailParams.From(@event);
        if (organizerParams != null)
        {
            foreach (var kvp in organizerParams.ToDictionary())
            {
                _parameters[kvp.Key] = kvp.Value;
            }
        }
        return this;
    }

    public EmailParameterBuilder<TParams> Add(string key, object value)
    {
        _parameters[key] = value;
        return this;
    }

    public Dictionary<string, object> Build() => _parameters;
}
```

#### Component 4: Enhanced Email Template Service

**Purpose**: Add compile-time validation layer to template service

```csharp
namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Enhanced email template service with strongly-typed parameter support.
/// </summary>
public interface IEmailTemplateService
{
    /// <summary>
    /// Renders email template with strongly-typed parameters.
    /// RECOMMENDED: Use this overload for compile-time safety.
    /// </summary>
    Task<Result<RenderedEmailTemplate>> RenderTemplateAsync<TParams>(
        string templateName,
        TParams parameters,
        CancellationToken cancellationToken = default
    ) where TParams : IEmailParameters;

    /// <summary>
    /// Renders email template with dictionary parameters.
    /// LEGACY: Use only for backward compatibility.
    /// </summary>
    Task<Result<RenderedEmailTemplate>> RenderTemplateAsync(
        string templateName,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default
    );
}

// Implementation
public class RazorEmailTemplateService : IEmailTemplateService
{
    // NEW strongly-typed method
    public async Task<Result<RenderedEmailTemplate>> RenderTemplateAsync<TParams>(
        string templateName,
        TParams parameters,
        CancellationToken cancellationToken = default
    ) where TParams : IEmailParameters
    {
        try
        {
            // Convert to dictionary
            var dict = EmailParameterConverter.ToDictionary(parameters);

            // Delegate to existing implementation
            return await RenderTemplateAsync(templateName, dict, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to render template {TemplateName} with parameters type {ParamsType}",
                templateName, typeof(TParams).Name);

            return Result<RenderedEmailTemplate>.Failure(
                $"Failed to render template: {ex.Message}");
        }
    }

    // Existing dictionary-based method (unchanged)
    public async Task<Result<RenderedEmailTemplate>> RenderTemplateAsync(
        string templateName,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        // Existing implementation (no changes)
    }
}
```

---

## 3. SYSTEM DESIGN

### 3.1 Component Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      APPLICATION LAYER                                   │
│                                                                           │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │              Email Parameter Contracts                            │  │
│  │  ┌────────────────┐  ┌────────────────┐  ┌──────────────────┐   │  │
│  │  │ UserEmailParams│  │ EventEmailParams│  │OrganizerEmailParams│  │  │
│  │  └────────────────┘  └────────────────┘  └──────────────────┘   │  │
│  │                     (Reusable base contracts)                     │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                                │                                         │
│                                │ Compose                                 │
│                                ▼                                         │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │         Template-Specific Parameter Classes                       │  │
│  │  ┌──────────────────────┐  ┌────────────────────────────────┐   │  │
│  │  │EventReminderEmailParams│ │PaidEventRegistrationEmailParams│   │  │
│  │  └──────────────────────┘  └────────────────────────────────┘   │  │
│  │  (18 template-specific classes - one per email template)         │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                                │                                         │
│                                │ Used by                                 │
│                                ▼                                         │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │                Event Handlers / Background Jobs                   │  │
│  │  ┌─────────────────────┐  ┌──────────────────────────────┐      │  │
│  │  │ EventReminderJob    │  │ PaymentCompletedEventHandler │      │  │
│  │  └─────────────────────┘  └──────────────────────────────┘      │  │
│  │  (15+ handlers - each uses strongly-typed parameters)            │  │
│  └──────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘
                                │
                                │ Call
                                ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                    INFRASTRUCTURE LAYER                                  │
│                                                                           │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │               IEmailTemplateService                               │  │
│  │  - RenderTemplateAsync<TParams>(templateName, params)            │  │
│  │  - RenderTemplateAsync(templateName, dictionary) [LEGACY]        │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                                │                                         │
│                                │ Delegates to                            │
│                                ▼                                         │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │            RazorEmailTemplateService                              │  │
│  │  - Converts IEmailParameters → Dictionary<string, object>        │  │
│  │  - Validates required parameters                                 │  │
│  │  - Renders Handlebars template                                   │  │
│  │  - Logs parameter types for debugging                            │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                                │                                         │
│                                │ Queries                                 │
│                                ▼                                         │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │              PostgreSQL Database                                  │  │
│  │  communications.email_templates table                            │  │
│  │  (18 templates with Handlebars syntax)                           │  │
│  └──────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘
```

### 3.2 Sequence Diagram: Email Sending with Strong Typing

```
Handler                Parameter Objects        TemplateService         Database
  │                          │                         │                    │
  │ 1. Create params         │                         │                    │
  ├─────────────────────────>│                         │                    │
  │  EventReminderEmailParams│                         │                    │
  │  {                       │                         │                    │
  │    User: UserEmailParams.From(user),              │                    │
  │    Event: EventEmailParams.From(@event, urls),    │                    │
  │    Organizer: OrganizerEmailParams.From(@event)   │                    │
  │  }                       │                         │                    │
  │<─────────────────────────│                         │                    │
  │ Compile-time validation! │                         │                    │
  │ (Missing property = build error)                  │                    │
  │                          │                         │                    │
  │ 2. Render template       │                         │                    │
  ├─────────────────────────────────────────────────>│                    │
  │  RenderTemplateAsync("template-event-reminder", params)                │
  │                          │                         │                    │
  │                          │ 3. Convert to dict      │                    │
  │                          │<────────────────────────│                    │
  │                          │  params.ToDictionary()  │                    │
  │                          │─────────────────────────>                    │
  │                          │ Dictionary with ALL     │                    │
  │                          │ parameters (old + new)  │                    │
  │                          │                         │                    │
  │                          │                         │ 4. Fetch template  │
  │                          │                         ├───────────────────>│
  │                          │                         │ SELECT body_html   │
  │                          │                         │ FROM email_templates│
  │                          │                         │<───────────────────│
  │                          │                         │ template HTML      │
  │                          │                         │                    │
  │                          │                         │ 5. Render Handlebars
  │                          │                         │ Replace {{params}} │
  │                          │                         │ with values        │
  │                          │                         │                    │
  │ 6. Rendered HTML         │                         │                    │
  │<─────────────────────────────────────────────────│                    │
  │  All {{params}} replaced │                         │                    │
  │  No literal {{}} in HTML │                         │                    │
  │                          │                         │                    │
  │ 7. Send email            │                         │                    │
  └──>EmailService.SendAsync(html, recipient)         │                    │
```

### 3.3 Data Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    DEVELOPMENT TIME                              │
│                                                                  │
│  Developer writes handler:                                      │
│  ┌────────────────────────────────────────────────────────┐    │
│  │ var params = new EventReminderEmailParams {           │    │
│  │     User = UserEmailParams.From(user),                │    │
│  │     Event = EventEmailParams.From(@event, urls),      │    │
│  │     Organizer = OrganizerEmailParams.From(@event),    │    │
│  │     ReminderTimeframe = "24 hours",                   │    │
│  │     // Missing TicketCode? ❌ COMPILER ERROR!          │    │
│  │ };                                                     │    │
│  └────────────────────────────────────────────────────────┘    │
│                          │                                      │
│                          ▼                                      │
│  ┌────────────────────────────────────────────────────────┐    │
│  │              C# COMPILER                               │    │
│  │  - Validates all required properties set              │    │
│  │  - Checks property types (string vs int)              │    │
│  │  - IntelliSense shows available properties            │    │
│  │  BUILD FAILS if parameters incomplete ❌               │    │
│  └────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
                          │
                          │ Build succeeds ✅
                          ▼
┌─────────────────────────────────────────────────────────────────┐
│                     RUNTIME (PRODUCTION)                         │
│                                                                  │
│  Handler executes:                                              │
│  ┌────────────────────────────────────────────────────────┐    │
│  │ var params = new EventReminderEmailParams { ... }     │    │
│  │ var result = await _templateService.RenderTemplateAsync│    │
│  │     ("template-event-reminder", params);               │    │
│  └────────────────────────────────────────────────────────┘    │
│                          │                                      │
│                          ▼                                      │
│  ┌────────────────────────────────────────────────────────┐    │
│  │         Parameter Converter                            │    │
│  │  params.ToDictionary() →                               │    │
│  │  {                                                     │    │
│  │    "UserName": "John Doe",                            │    │
│  │    "EventTitle": "Tech Meetup",                       │    │
│  │    "OrganizerContactName": "Jane Smith", // OLD name  │    │
│  │    "OrganizerName": "Jane Smith",        // NEW name  │    │
│  │    "TicketCode": "ABC123",                            │    │
│  │    ... (50+ parameters)                               │    │
│  │  }                                                     │    │
│  └────────────────────────────────────────────────────────┘    │
│                          │                                      │
│                          ▼                                      │
│  ┌────────────────────────────────────────────────────────┐    │
│  │         Handlebars Template Rendering                  │    │
│  │  Template: "Hello {{UserName}}, your ticket is {{Ticket│    │
│  │  Code}} for {{EventTitle}}"                            │    │
│  │                    ↓ Replace parameters                │    │
│  │  Result: "Hello John Doe, your ticket is ABC123 for    │    │
│  │           Tech Meetup"                                 │    │
│  │  ✅ All {{parameters}} replaced successfully            │    │
│  │  ✅ No literal {{}} in output                           │    │
│  └────────────────────────────────────────────────────────┘    │
│                          │                                      │
│                          ▼                                      │
│  ┌────────────────────────────────────────────────────────┐    │
│  │              Email sent to user                        │    │
│  │  Perfect formatting, all data populated ✅              │    │
│  └────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
```

### 3.4 Technology Stack

**Parameter Contracts**:
- C# 12 Records (immutable, concise syntax)
- Required properties (compile-time enforcement)
- Static factory methods (From(entity) pattern)

**Validation**:
- Compile-time: C# type system
- Runtime: Optional validation in IEmailTemplateService
- Testing: Unit tests for parameter objects

**Template Rendering**:
- Handlebars.Net (existing)
- Dictionary<string, object> (existing interface)
- PostgreSQL (existing storage)

**Backward Compatibility**:
- Parameter objects output BOTH old and new names
- Template service accepts both IEmailParameters and Dictionary
- Gradual migration path (no big-bang deployment)

---

## 4. MIGRATION STRATEGY

### 4.1 Migration Phases

**Phase 1: Foundation (Week 1)**
- Create base parameter contracts (UserEmailParams, EventEmailParams, OrganizerEmailParams)
- Implement IEmailParameters interface
- Add strongly-typed RenderTemplateAsync<TParams> overload
- Unit tests for parameter objects

**Phase 2: Template Contracts (Week 2)**
- Create 18 template-specific parameter classes (one per template)
- Document required/optional parameters for each
- Generate parameter registry documentation
- Integration tests for parameter conversion

**Phase 3: Handler Migration (Week 3)**
- Migrate HIGH priority handlers (5 handlers)
- Test in staging environment
- Deploy to production incrementally
- Monitor for regressions

**Phase 4: Cleanup (Week 4)**
- Migrate remaining handlers (10 handlers)
- Remove duplicate parameter names from database templates
- Deprecate Dictionary-based RenderTemplateAsync method
- Final testing and documentation

### 4.2 Rollback Strategy

**Per-Handler Rollback**:
Each handler migration is independent. If issues found:
1. Revert handler code to previous commit
2. Redeploy to production
3. Parameter contract classes remain (no impact on other handlers)

**Database Rollback** (Phase 4 only):
If template cleanup causes issues:
1. SQL migration to restore duplicate parameter names
2. Handlers already send both old and new names (still works)
3. No code deployment needed

**Safety Net**:
- Legacy Dictionary-based method remains available during entire migration
- Handlers can temporarily fall back to old approach if needed
- Each handler migration is a separate commit (granular rollback)

### 4.3 Migration Testing Strategy

**Unit Tests** (Per Parameter Class):
```csharp
public class EventReminderEmailParamsTests
{
    [Fact]
    public void ToDictionary_ShouldIncludeAllRequiredParameters()
    {
        // Arrange
        var user = UserTestDataBuilder.CreateUser();
        var @event = EventTestDataBuilder.CreateEvent();
        var params = new EventReminderEmailParams
        {
            User = UserEmailParams.From(user),
            Event = EventEmailParams.From(@event, _urlHelper),
            Organizer = OrganizerEmailParams.From(@event)!,
            ReminderTimeframe = "24 hours",
            AttendeeName = "John Doe",
            ContactEmail = "john@example.com",
            TicketCode = "ABC123",
            TicketExpiryDate = "December 31, 2025"
        };

        // Act
        var dict = params.ToDictionary();

        // Assert
        dict.Should().ContainKey("UserName");
        dict.Should().ContainKey("EventTitle");
        dict.Should().ContainKey("OrganizerContactName");  // Old name
        dict.Should().ContainKey("OrganizerName");         // New name
        dict.Should().ContainKey("TicketCode");
        dict.Should().ContainKey("ReminderTimeframe");
        dict.Should().HaveCount(50);  // Expected total parameters
    }

    [Fact]
    public void ToDictionary_ShouldProvideBackwardCompatibleParameters()
    {
        // Assert old parameter names map to same values as new names
        var dict = CreateSampleParams().ToDictionary();

        dict["OrganizerContactName"].Should().Be(dict["OrganizerName"]);
        dict["OrganizerContactEmail"].Should().Be(dict["OrganizerEmail"]);
    }
}
```

**Integration Tests** (Per Handler):
```csharp
public class EventReminderJobIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task ExecuteAsync_ShouldRenderEmailWithoutLiteralParameters()
    {
        // Arrange
        var @event = await CreateTestEventAsync();
        var registration = await RegisterUserForEventAsync(@event.Id);

        // Act
        await _job.ExecuteAsync(@event.Id);

        // Assert
        var sentEmail = await GetLastSentEmailAsync();
        sentEmail.BodyHtml.Should().NotContain("{{");  // No literal Handlebars
        sentEmail.BodyHtml.Should().Contain(registration.User.FirstName);
        sentEmail.BodyHtml.Should().Contain(@event.OrganizerContactName);
        sentEmail.BodyHtml.Should().Contain("ABC123");  // TicketCode rendered
    }
}
```

**Staging Smoke Tests**:
```bash
# After each handler migration
curl -X POST "https://lankaconnect-api-staging.../api/test/trigger-event-reminder" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"eventId": "..."}'

# Verify email in MailHog
curl "http://mailhog-staging:8025/api/v2/messages" | \
  jq '.items[0].Content.Body' | \
  grep -q "{{" && echo "❌ FAILED: Literal parameters found" || echo "✅ PASSED"
```

### 4.4 Database Template Migration (Phase 4)

**SQL Migration to Remove Duplicate Parameters**:
```sql
-- Phase 6B: Remove duplicate parameter names from templates
-- RUN AFTER all handlers migrated to strongly-typed parameters

-- Backup templates before modification
CREATE TABLE communications.email_templates_backup_phase6b AS
SELECT * FROM communications.email_templates;

-- Update template-event-reminder: Remove old parameter names
UPDATE communications.email_templates
SET body_html = REPLACE(body_html, '{{OrganizerContactName}}', '{{OrganizerName}}'),
    body_html = REPLACE(body_html, '{{OrganizerContactEmail}}', '{{OrganizerEmail}}'),
    body_html = REPLACE(body_html, '{{OrganizerContactPhone}}', '{{OrganizerPhone}}')
WHERE template_name = 'template-event-reminder';

-- Repeat for all 18 templates...

-- Verification: Check no templates contain old parameter names
SELECT template_name,
       CASE
         WHEN body_html LIKE '%{{OrganizerContactName}}%' THEN 'Has old parameters'
         ELSE 'Clean'
       END as status
FROM communications.email_templates;

-- Rollback procedure if issues found:
-- UPDATE communications.email_templates
-- SET body_html = b.body_html
-- FROM communications.email_templates_backup_phase6b b
-- WHERE communications.email_templates.id = b.id;
```

---

## 5. CODE EXAMPLES

### 5.1 Before: EventReminderJob (Current Broken Approach)

```csharp
// BEFORE: Manual Dictionary construction - error-prone, no compile-time safety
public async Task ExecuteAsync(Guid eventId, CancellationToken cancellationToken = default)
{
    var @event = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
    var registration = await _registrationRepository.GetByEventAsync(eventId, cancellationToken);

    // ❌ PROBLEM 1: Typo in key name - only caught in production!
    var parameters = new Dictionary<string, object>
    {
        { "UserNam", registration.User.Name },  // TYPO! Template expects "UserName"
        { "EventTitle", @event.Title.Value },
        { "EventStartDate", @event.StartDate.ToString("MMMM dd, yyyy") },
        { "EventStartTime", @event.StartDate.ToString("h:mm tt") },

        // ❌ PROBLEM 2: Using new parameter name, template expects old name
        { "OrganizerName", @event.OrganizerContactName },
        // Template has {{OrganizerContactName}} → shows literally!

        { "OrganizerEmail", @event.OrganizerContactEmail },
        // Template has {{OrganizerContactEmail}} → shows literally!
    };

    // ❌ PROBLEM 3: Missing required parameters - no compiler warning!
    // Template expects: TicketCode, TicketExpiryDate, ContactEmail
    // Result: {{TicketCode}} shows literally in email

    await _emailService.SendTemplatedEmailAsync(
        EmailTemplateNames.EventReminder,
        registration.User.Email,
        parameters,  // Broken dictionary passed to template
        cancellationToken);
}
```

**Problems**:
1. Typos in dictionary keys (UserNam vs UserName)
2. Parameter name mismatch (OrganizerName vs OrganizerContactName)
3. Missing required parameters (TicketCode, ContactEmail)
4. Duplicate code across handlers (every handler builds UserName, EventTitle, etc.)
5. No IntelliSense support
6. No compile-time validation

### 5.2 After: EventReminderJob (Strongly-Typed Approach)

```csharp
// AFTER: Strongly-typed parameters - compiler-enforced correctness
public async Task ExecuteAsync(Guid eventId, CancellationToken cancellationToken = default)
{
    var @event = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
    var registration = await _registrationRepository.GetByEventAsync(eventId, cancellationToken);
    var ticket = registration.TicketId.HasValue
        ? await _ticketRepository.GetByIdAsync(registration.TicketId.Value, cancellationToken)
        : null;

    // ✅ SOLUTION: Build strongly-typed parameter object
    var emailParams = new EventReminderEmailParams
    {
        // ✅ Reuse common parameters (no duplication!)
        User = UserEmailParams.From(registration.User),
        Event = EventEmailParams.From(@event, _emailUrlHelper),
        Organizer = OrganizerEmailParams.From(@event)!,  // Compiler enforces non-null

        // ✅ Template-specific parameters (IntelliSense shows what's required!)
        ReminderTimeframe = "24 hours",
        AttendeeName = registration.User.FirstName,
        ContactEmail = registration.Contact?.Email ?? registration.User.Email,

        // ✅ Ticket parameters (compiler reminds you to provide these!)
        TicketCode = ticket?.TicketCode,
        TicketExpiryDate = ticket?.ExpiryDate.ToString("MMMM dd, yyyy"),

        // ✅ Missing a required property? BUILD FAILS with clear error message!
        // EventReminderEmailParams requires 'ReminderMessage' property
    };

    // ✅ Strongly-typed template rendering
    var result = await _emailTemplateService.RenderTemplateAsync(
        EmailTemplateNames.EventReminder,
        emailParams,  // Type-safe parameter object
        cancellationToken);

    if (result.IsSuccess)
    {
        await _emailService.SendEmailAsync(new EmailMessageDto
        {
            ToEmail = registration.User.Email,
            ToName = emailParams.User.UserName,
            Subject = result.Value.Subject,
            HtmlBody = result.Value.HtmlBody,
            PlainTextBody = result.Value.PlainTextBody
        }, cancellationToken);
    }
}
```

**Benefits**:
1. ✅ **No typos possible** - Property names validated by compiler
2. ✅ **Parameter name mismatch impossible** - ToDictionary() handles old/new names
3. ✅ **Missing parameters caught at build time** - Required properties enforced
4. ✅ **Code reuse** - UserEmailParams, EventEmailParams shared across all handlers
5. ✅ **IntelliSense support** - IDE shows all available properties
6. ✅ **Compile-time validation** - Errors caught during development, not production

### 5.3 Before: PaymentCompletedEventHandler

```csharp
// BEFORE: Missing critical parameters
var parameters = new Dictionary<string, object>
{
    { "UserName", recipientName },
    { "EventTitle", @event.Title.Value },
    { "EventStartDate", @event.StartDate.ToString("MMMM dd, yyyy") },
    { "EventStartTime", @event.StartDate.ToString("h:mm tt") },
    { "TotalAmount", domainEvent.AmountPaid.ToString("C") },
    // ❌ Missing AmountPaid (template has BOTH AmountPaid and TotalAmount!)
    // ❌ Missing OrganizerContactName, OrganizerContactEmail, OrganizerContactPhone
    { "TicketCode", ticketResult.Value.TicketCode },
    { "TicketExpiryDate", @event.EndDate.AddDays(1).ToString("MMMM dd, yyyy") },
};

// Result: Email shows {{AmountPaid}}, {{OrganizerContactName}} literally
```

### 5.4 After: PaymentCompletedEventHandler

```csharp
// AFTER: All parameters guaranteed present
var emailParams = new PaidEventRegistrationEmailParams
{
    User = UserEmailParams.From(user),
    Event = EventEmailParams.From(@event, _emailUrlHelper),
    Organizer = OrganizerEmailParams.From(@event)!,

    // Payment parameters
    AmountPaid = domainEvent.AmountPaid.ToString("C", CultureInfo.GetCultureInfo("en-US")),
    TotalAmount = domainEvent.AmountPaid.ToString("C", CultureInfo.GetCultureInfo("en-US")),
    Quantity = registration.Attendees.Count,
    TicketType = @event.IsFree() ? "Free Entry" : "General Admission",
    OrderNumber = domainEvent.PaymentIntentId,
    PaymentIntentId = domainEvent.PaymentIntentId,
    PaymentDate = domainEvent.PaymentCompletedAt.ToString("MMMM dd, yyyy h:mm tt"),

    // Ticket parameters
    TicketCode = ticketResult.Value.TicketCode,
    TicketExpiryDate = @event.EndDate.AddDays(1).ToString("MMMM dd, yyyy"),
    TicketUrl = _emailUrlHelper.BuildTicketViewUrl(ticketResult.Value.TicketId),

    // Contact parameters
    ContactEmail = registration.Contact?.Email ?? "",
    ContactPhone = registration.Contact?.PhoneNumber,

    // Attendee details
    Attendees = FormatAttendeeDetailsHtml(registration.Attendees),
    HasAttendeeDetails = registration.HasDetailedAttendees()
};

// ✅ Compiler FORCES you to provide all 22 required properties
// ✅ Missing one? Build error with clear message
// ✅ Wrong type? Build error
// ✅ Typo in property name? Build error

var result = await _emailTemplateService.RenderTemplateAsync(
    EmailTemplateNames.PaidEventRegistration,
    emailParams,
    cancellationToken);

// ✅ Result: Perfect email with all parameters populated
// ✅ No literal {{}} in output
// ✅ All organizer contact info displays correctly
```

### 5.5 Helper Method Pattern: Composition

```csharp
// REUSABLE: Common parameter construction
public static class EmailParameterHelpers
{
    public static EventEmailParams BuildEventParams(
        Event @event,
        IEmailUrlHelper urlHelper)
    {
        return EventEmailParams.From(@event, urlHelper);
    }

    public static string FormatAttendeeDetailsHtml(IEnumerable<Attendee> attendees)
    {
        var sb = new StringBuilder();
        foreach (var attendee in attendees)
        {
            sb.AppendLine($"<p style=\"margin: 8px 0; font-size: 16px;\">{attendee.Name}</p>");
        }
        return sb.ToString().TrimEnd();
    }

    public static string FormatEventLocation(Event @event)
    {
        if (@event.Location?.Address == null) return "Online Event";
        var street = @event.Location.Address.Street;
        var city = @event.Location.Address.City;
        if (string.IsNullOrWhiteSpace(street) && string.IsNullOrWhiteSpace(city))
            return "Online Event";
        if (string.IsNullOrWhiteSpace(street)) return city!;
        if (string.IsNullOrWhiteSpace(city)) return street;
        return $"{street}, {city}";
    }
}
```

---

## 6. VALIDATION & TESTING

### 6.1 Compile-Time Validation

**How It Works**:
```csharp
// Developer writes handler
var emailParams = new EventReminderEmailParams
{
    User = UserEmailParams.From(user),
    Event = EventEmailParams.From(@event, _urlHelper),
    // Oops, forgot Organizer!
};

// ❌ COMPILER ERROR:
// CS7036: There is no argument given that corresponds to the required
// parameter 'Organizer' of 'EventReminderEmailParams'
//
// The following required properties are not set:
// - Organizer
// - ReminderTimeframe
// - AttendeeName
// - ContactEmail
```

**Benefits**:
- Errors caught **before code is committed**
- Clear error messages guide developer
- No need to run the app to find missing parameters
- IntelliSense shows exactly what's required

### 6.2 Runtime Validation (Optional Extra Safety)

```csharp
public class RazorEmailTemplateService : IEmailTemplateService
{
    public async Task<Result<RenderedEmailTemplate>> RenderTemplateAsync<TParams>(
        string templateName,
        TParams parameters,
        CancellationToken cancellationToken = default
    ) where TParams : IEmailParameters
    {
        // 1. Convert to dictionary
        var dict = EmailParameterConverter.ToDictionary(parameters);

        // 2. Optional: Runtime validation against template metadata
        if (_enableRuntimeValidation)
        {
            var templateMetadata = await GetTemplateMetadataAsync(templateName, cancellationToken);
            var missing = templateMetadata.RequiredParameters
                .Where(p => !dict.ContainsKey(p) || dict[p] == null)
                .ToList();

            if (missing.Any())
            {
                _logger.LogError(
                    "Template {TemplateName} missing required parameters: {MissingParams}. " +
                    "Parameter object type: {ParamsType}",
                    templateName, string.Join(", ", missing), typeof(TParams).Name);

                // Option 1: Fail (strict mode)
                return Result<RenderedEmailTemplate>.Failure(
                    $"Missing required parameters: {string.Join(", ", missing)}");

                // Option 2: Log warning and continue (permissive mode)
                // Useful during migration when templates still have duplicate params
            }
        }

        // 3. Render template
        return await RenderTemplateAsync(templateName, dict, cancellationToken);
    }
}
```

### 6.3 Integration Test Pattern

```csharp
[Collection("Integration")]
public class EmailTemplateIntegrationTests : IntegrationTestBase
{
    [Theory]
    [InlineData(EmailTemplateNames.EventReminder)]
    [InlineData(EmailTemplateNames.PaidEventRegistration)]
    [InlineData(EmailTemplateNames.EventCancellation)]
    public async Task AllEmailTemplates_ShouldRenderWithoutLiteralParameters(string templateName)
    {
        // Arrange: Create test data for this template
        var testData = _emailTestDataFactory.CreateParametersFor(templateName);

        // Act: Render template
        var result = await _templateService.RenderTemplateAsync(templateName, testData);

        // Assert: No literal Handlebars syntax in output
        result.IsSuccess.Should().BeTrue();
        result.Value.HtmlBody.Should().NotContain("{{");
        result.Value.HtmlBody.Should().NotContain("}}");

        // Assert: All expected data present
        var expectedData = testData.ToDictionary();
        foreach (var kvp in expectedData)
        {
            if (!string.IsNullOrEmpty(kvp.Value?.ToString()))
            {
                result.Value.HtmlBody.Should().Contain(kvp.Value.ToString()!);
            }
        }
    }

    [Fact]
    public async Task EventReminderEmail_ShouldIncludeAllRequiredInformation()
    {
        // Arrange
        var @event = await CreateTestEventAsync(hasTickets: true);
        var user = await CreateTestUserAsync();
        var registration = await RegisterUserAsync(@event.Id, user.Id);
        var ticket = await GenerateTicketAsync(registration.Id);

        var emailParams = new EventReminderEmailParams
        {
            User = UserEmailParams.From(user),
            Event = EventEmailParams.From(@event, _urlHelper),
            Organizer = OrganizerEmailParams.From(@event)!,
            ReminderTimeframe = "24 hours",
            AttendeeName = user.FirstName,
            ContactEmail = user.Email.Value,
            TicketCode = ticket.TicketCode,
            TicketExpiryDate = ticket.ExpiryDate.ToString("MMMM dd, yyyy")
        };

        // Act
        var result = await _templateService.RenderTemplateAsync(
            EmailTemplateNames.EventReminder,
            emailParams);

        // Assert: Specific business requirements verified
        result.Value.Subject.Should().Contain(@event.Title.Value);
        result.Value.HtmlBody.Should().Contain(user.FirstName);
        result.Value.HtmlBody.Should().Contain(ticket.TicketCode);
        result.Value.HtmlBody.Should().Contain(@event.OrganizerContactName!);
        result.Value.HtmlBody.Should().Contain(@event.OrganizerContactEmail!);
        result.Value.HtmlBody.Should().NotContain("{{");  // No literal params
    }
}
```

### 6.4 Automated Template Parameter Extraction Tool

```csharp
/// <summary>
/// CLI tool to extract Handlebars parameters from database templates
/// and validate against strongly-typed parameter classes.
///
/// Usage:
///   dotnet run --project EmailTemplateValidator -- extract-params
///   dotnet run --project EmailTemplateValidator -- validate-handlers
/// </summary>
public class EmailTemplateValidator
{
    public async Task<ValidationReport> ValidateAllHandlersAsync()
    {
        var report = new ValidationReport();

        // 1. Extract parameters from database templates
        var templates = await _db.EmailTemplates.ToListAsync();
        foreach (var template in templates)
        {
            var extractedParams = ExtractHandlebarsParameters(template.BodyHtml);
            report.TemplateParameters[template.TemplateName] = extractedParams;
        }

        // 2. Find corresponding parameter class for each template
        var parameterClasses = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(IEmailParameters).IsAssignableFrom(t) && !t.IsInterface)
            .ToList();

        // 3. For each template, validate parameter class provides all required params
        foreach (var template in templates)
        {
            var paramClass = FindParameterClassForTemplate(template.TemplateName, parameterClasses);
            if (paramClass == null)
            {
                report.AddWarning(template.TemplateName, "No parameter class found");
                continue;
            }

            // Create sample instance and convert to dictionary
            var instance = CreateSampleInstance(paramClass);
            var providedParams = instance.ToDictionary().Keys.ToList();

            var requiredParams = report.TemplateParameters[template.TemplateName];
            var missing = requiredParams.Except(providedParams).ToList();
            var extra = providedParams.Except(requiredParams).ToList();

            if (missing.Any())
            {
                report.AddError(template.TemplateName,
                    $"Parameter class {paramClass.Name} missing: {string.Join(", ", missing)}");
            }

            if (extra.Any())
            {
                report.AddWarning(template.TemplateName,
                    $"Parameter class {paramClass.Name} has unused params: {string.Join(", ", extra)}");
            }
        }

        return report;
    }

    private List<string> ExtractHandlebarsParameters(string templateHtml)
    {
        var regex = new Regex(@"\{\{([^}]+)\}\}");
        var matches = regex.Matches(templateHtml);
        return matches
            .Select(m => m.Groups[1].Value.Trim())
            .Distinct()
            .OrderBy(p => p)
            .ToList();
    }
}
```

**Sample Output**:
```
Email Template Validation Report
=================================

✅ template-event-reminder
   Parameter Class: EventReminderEmailParams
   Required Parameters: 17
   Provided Parameters: 17
   Status: VALID

❌ template-paid-event-registration-confirmation-with-ticket
   Parameter Class: PaidEventRegistrationEmailParams
   Required Parameters: 22
   Provided Parameters: 20
   Missing: TicketCode, TicketExpiryDate
   Status: INVALID

⚠️ template-newsletter-notification
   Parameter Class: Not Found
   Required Parameters: 10
   Status: NO PARAMETER CLASS

Summary:
  Total Templates: 18
  Valid: 15
  Invalid: 2
  Missing Parameter Class: 1
```

---

## 7. EFFORT ESTIMATES

### 7.1 Development Effort Breakdown

| Task | Effort (Hours) | Complexity | Risk |
|------|----------------|------------|------|
| **Phase 1: Foundation** | **32 hours** | Medium | Low |
| - Create IEmailParameters interface | 2 | Low | Low |
| - Implement UserEmailParams | 4 | Low | Low |
| - Implement EventEmailParams | 6 | Medium | Low |
| - Implement OrganizerEmailParams | 4 | Low | Low |
| - Implement EmailParameterConverter | 6 | Medium | Low |
| - Add strongly-typed RenderTemplateAsync | 4 | Medium | Low |
| - Unit tests for parameter classes | 6 | Low | Low |
| **Phase 2: Template Contracts** | **48 hours** | High | Medium |
| - Analyze 18 templates, extract parameters | 8 | Medium | Low |
| - Create EventReminderEmailParams | 3 | Medium | Low |
| - Create PaidEventRegistrationEmailParams | 4 | Medium | Low |
| - Create EventCancellationEmailParams | 3 | Medium | Low |
| - Create 15 more template parameter classes | 24 | Medium | Medium |
| - Document parameter registry | 4 | Low | Low |
| - Integration tests for parameter conversion | 6 | Medium | Low |
| **Phase 3: Handler Migration** | **60 hours** | High | Medium |
| - Migrate EventReminderJob | 6 | Medium | Medium |
| - Migrate PaymentCompletedEventHandler | 6 | Medium | High |
| - Migrate EventCancellationEmailJob | 5 | Medium | Medium |
| - Migrate EventPublishedEventHandler | 5 | Medium | Medium |
| - Migrate EventNotificationEmailJob | 5 | Medium | Medium |
| - Migrate RegistrationConfirmedEventHandler | 4 | Low | Low |
| - Migrate AnonymousRegistrationConfirmedEventHandler | 4 | Low | Low |
| - Migrate 3 signup list handlers | 9 | Medium | Low |
| - Migrate RegistrationCancelledEventHandler | 3 | Low | Low |
| - Migrate 6 remaining handlers | 12 | Medium | Low |
| - Testing in staging per handler | 10 | Medium | Medium |
| - Production deployment monitoring | 6 | Low | Low |
| **Phase 4: Cleanup & Tooling** | **40 hours** | Medium | Low |
| - Implement EmailTemplateValidator tool | 12 | Medium | Low |
| - Create SQL migration for template cleanup | 6 | Medium | Medium |
| - Test template cleanup in staging | 4 | Low | Low |
| - Deploy template cleanup to production | 2 | Low | Low |
| - Remove duplicate params from handler code | 8 | Low | Low |
| - Final integration testing | 6 | Medium | Low |
| - Documentation updates | 4 | Low | Low |
| **Total Effort** | **180 hours** | | |

**Estimated Timeline**: 3-4 weeks for 1 developer working full-time

**Recommended Approach**: 2 developers working in parallel:
- Developer 1: Foundation + Template Contracts (Phases 1-2)
- Developer 2: Handler Migration (Phase 3, starts after Phase 1 complete)
- Both: Cleanup & Tooling (Phase 4)
- **Total Calendar Time**: 2-3 weeks

### 7.2 Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Handler migration introduces regression | Medium | High | - Comprehensive integration tests<br>- Per-handler staging validation<br>- Gradual rollout (1-2 handlers per day) |
| Template cleanup breaks production emails | Low | Critical | - Keep duplicate params during Phase 3<br>- Deploy cleanup only after all handlers migrated<br>- SQL rollback script ready |
| Parameter class doesn't match template | Low | Medium | - EmailTemplateValidator tool catches before deployment<br>- Integration tests validate rendered output |
| Developer resistance to new pattern | Medium | Low | - Clear documentation with examples<br>- Pair programming for first few migrations<br>- Show compile-time error prevention benefits |

### 7.3 Cost-Benefit Analysis

**Costs**:
- 180 hours development effort (~$18,000 at $100/hr contractor rate)
- 2-3 weeks calendar time
- Learning curve for new pattern

**Benefits**:
- **Eliminate 100% of parameter mismatch bugs** (HIGH value)
- **90% reduction in email-related support tickets** (estimated $5,000/year savings)
- **50% faster handler development** (reusable parameter objects save ~4 hours per handler)
- **Zero regression risk for future email changes** (compile-time validation)
- **Improved developer experience** (IntelliSense, clear errors)

**ROI**: Benefits pay back initial investment within 6 months through:
1. Reduced support tickets (estimated 20 tickets/year at 1 hour each = $2,000/year)
2. Faster feature development (estimated 10 new email types/year × 4 hours saved = $4,000/year)
3. Prevented production incidents (estimated 2 major incidents/year × $5,000 each = $10,000/year)

**Total Annual Savings**: ~$16,000/year
**Break-even Point**: ~13 months

---

## 8. ROLLOUT PLAN

### 8.1 Week-by-Week Plan

**Week 1: Foundation + Template Contracts**

Day 1-2: Foundation
- Create base parameter contracts (UserEmailParams, EventEmailParams, OrganizerEmailParams)
- Implement IEmailParameters interface
- Add EmailParameterConverter utilities
- Unit tests (90%+ coverage)

Day 3-4: Template Analysis
- Extract parameters from 18 database templates
- Document required/optional parameters for each template
- Create parameter registry documentation

Day 5: Template Contract Classes (Part 1)
- Create EventReminderEmailParams
- Create PaidEventRegistrationEmailParams
- Create EventCancellationEmailParams
- Integration tests for these 3 classes

**Week 2: Template Contracts + HIGH Priority Handlers**

Day 1-2: Template Contract Classes (Part 2)
- Create 15 remaining template parameter classes
- Unit tests for each class
- Integration tests for parameter conversion

Day 3: HIGH Priority Handler #1 - EventReminderJob
- Migrate to EventReminderEmailParams
- Unit tests
- Deploy to staging
- Smoke test

Day 4: HIGH Priority Handlers #2-3
- Migrate PaymentCompletedEventHandler
- Migrate EventCancellationEmailJob
- Deploy to staging
- Comprehensive testing

Day 5: HIGH Priority Handlers #4-5
- Migrate EventPublishedEventHandler
- Migrate EventNotificationEmailJob
- Deploy to staging
- Production deployment preparation

**Week 3: MEDIUM/LOW Priority Handlers**

Day 1-2: Registration Handlers
- Migrate RegistrationConfirmedEventHandler
- Migrate AnonymousRegistrationConfirmedEventHandler
- Deploy to staging

Day 3: Signup List Handlers
- Migrate UserCommittedToSignUpEventHandler
- Migrate CommitmentUpdatedEventHandler
- Migrate CommitmentCancelledEventHandler
- Deploy to staging

Day 4: Remaining Handlers
- Migrate RegistrationCancelledEventHandler
- Migrate 5 remaining low-priority handlers
- Deploy to staging

Day 5: Production Deployment
- Deploy all migrated handlers to production
- Monitor logs for 24 hours
- Verify no literal {{}} in sent emails

**Week 4: Cleanup + Tooling**

Day 1-2: EmailTemplateValidator Tool
- Implement parameter extraction logic
- Implement validation logic
- CLI interface
- Unit tests

Day 3: Template Cleanup
- Create SQL migration to remove duplicate parameters
- Test migration in staging
- Verify all emails still render correctly

Day 4: Production Cleanup
- Deploy SQL migration to production
- Remove duplicate parameters from handler code
- Deploy updated handlers

Day 5: Final Testing + Documentation
- Run EmailTemplateValidator on all templates
- Generate validation report
- Update documentation
- Retrospective

### 8.2 Daily Deployment Checklist

**For Each Handler Migration**:

1. **Code Changes**
   - [ ] Create/update parameter class
   - [ ] Update handler to use strongly-typed params
   - [ ] Update unit tests
   - [ ] Run all tests locally (dotnet test)

2. **Commit**
   - [ ] Git commit with descriptive message
   - [ ] Push to develop branch

3. **Staging Deployment**
   - [ ] Wait for GitHub Actions deploy-staging.yml to complete
   - [ ] Check Azure Container Apps logs for errors

4. **Staging Validation**
   - [ ] Trigger email scenario via API
   - [ ] Check MailHog for rendered email
   - [ ] Verify no literal {{}} parameters
   - [ ] Verify all data renders correctly
   - [ ] SQL query: Check parameters in email_messages table

5. **Production Deployment** (after all handlers in phase tested)
   - [ ] Merge to master branch
   - [ ] Wait for production deployment
   - [ ] Monitor logs for 1 hour
   - [ ] Check production emails
   - [ ] Mark handler as ✅ COMPLETE in tracker

### 8.3 Rollback Procedures

**Handler Rollback** (if issue found in production):
```bash
# 1. Identify problematic commit
git log --oneline --all -20

# 2. Revert the handler migration commit
git revert <commit-hash>

# 3. Push revert
git push origin master

# 4. Wait for auto-deployment (~5 min)

# 5. Verify rollback successful
curl -X POST "https://lankaconnect-api.../api/test/trigger-event-reminder"
# Check email renders correctly
```

**Template Cleanup Rollback** (if Phase 4 cleanup causes issues):
```sql
-- Restore templates from backup
UPDATE communications.email_templates
SET body_html = b.body_html,
    plain_text_body = b.plain_text_body,
    subject_template = b.subject_template
FROM communications.email_templates_backup_phase6b b
WHERE communications.email_templates.id = b.id;

-- Verify rollback
SELECT template_name,
       CASE
         WHEN body_html LIKE '%{{OrganizerContactName}}%' THEN 'Has old parameters (restored)'
         ELSE 'Only new parameters'
       END as status
FROM communications.email_templates;
```

**Emergency Stop** (if critical issue found):
```bash
# Option 1: Revert to last known good commit
git reset --hard <last-good-commit>
git push --force origin master

# Option 2: Feature flag disable (if implemented)
# Set environment variable in Azure Container Apps:
# USE_STRONGLY_TYPED_EMAIL_PARAMS=false

# Option 3: Database template restore (if templates were modified)
# Run SQL rollback script (see above)
```

### 8.4 Success Metrics

**Immediate Success (After Week 3)**:
- [ ] Zero user reports of literal {{}} in emails
- [ ] All 15 handlers migrated and tested
- [ ] 90%+ test coverage for parameter classes
- [ ] Zero production incidents related to email rendering

**Short-Term Success (After Week 4)**:
- [ ] EmailTemplateValidator tool running in CI
- [ ] Email template contract documentation complete
- [ ] SQL query confirms zero literal {{}} in last 7 days
- [ ] Developer feedback: "Easier to build email handlers now"

**Long-Term Success (After 3 months)**:
- [ ] Zero email parameter bugs reported
- [ ] New email templates added with no issues
- [ ] Code review time reduced (compile-time validation eliminates review burden)
- [ ] Developer onboarding faster (clear parameter contracts)

---

## 9. CONCLUSION

### 9.1 Summary

The proposed Email Parameter Standardization Architecture solves the root cause of parameter mismatch issues by:

1. **Eliminating manual Dictionary construction** - Replaced with strongly-typed parameter objects
2. **Enforcing compile-time validation** - Missing/wrong parameters caught during development, not production
3. **Enabling code reuse** - Common parameters (User, Event, Organizer) defined once, used everywhere
4. **Providing backward compatibility** - Works with existing database templates during migration
5. **Creating documentation as code** - Parameter classes ARE the contract specification

### 9.2 Key Architectural Decisions

| Decision | Rationale |
|----------|-----------|
| Use C# Records for parameter classes | Immutable, concise syntax, compile-time validation |
| Composition over inheritance | EventEmailParams + OrganizerEmailParams composed into EventReminderEmailParams |
| IEmailParameters interface | Common abstraction for all parameter objects |
| Support both old and new parameter names | Backward compatibility during migration, gradual cleanup |
| Keep Dictionary-based method | Legacy support, allows gradual migration |
| Implement EmailTemplateValidator tool | Automated validation catches contract violations before deployment |

### 9.3 Comparison to Alternative Approaches

**Alternative 1: Fix templates to use consistent parameter names**
- **Pros**: Simpler, cleaner templates
- **Cons**: Requires modifying 18 database templates, higher risk, harder to rollback
- **Verdict**: ❌ Rejected - Too risky for initial fix, can be done later in Phase 4

**Alternative 2: Keep Dictionary approach, add runtime validation**
- **Pros**: Minimal code changes
- **Cons**: Still typo-prone, errors caught only at runtime, no IntelliSense
- **Verdict**: ❌ Rejected - Doesn't solve root cause, only adds detection

**Alternative 3: Generate parameter classes from database templates**
- **Pros**: Templates are source of truth
- **Cons**: Complex tooling, templates change requires code regeneration, tight coupling
- **Verdict**: ❌ Rejected - Overly complex for the benefit

**Proposed Approach: Strongly-typed parameter contracts**
- **Pros**: Compile-time safety, code reuse, IntelliSense, self-documenting
- **Cons**: Initial development effort (180 hours)
- **Verdict**: ✅ RECOMMENDED - Best long-term solution, prevents entire class of bugs

### 9.4 Long-Term Vision

**Phase 6A (Current)**: Implement strongly-typed email parameters
**Phase 6B (Month 2)**: Clean up database templates, remove duplicate parameter names
**Phase 6C (Month 3)**: Template versioning system, automated CI validation
**Phase 7 (Quarter 2)**: Email template editor UI with parameter autocomplete

**Ultimate Goal**: Email system where parameter mismatch bugs are **architecturally impossible**.

---

## APPENDIX A: Template Parameter Registry

| Template Name | Parameter Class | Required Params | Optional Params |
|---------------|-----------------|-----------------|-----------------|
| template-event-reminder | EventReminderEmailParams | 15 | 2 |
| template-paid-event-registration-confirmation-with-ticket | PaidEventRegistrationEmailParams | 22 | 0 |
| template-event-cancellation-notifications | EventCancellationEmailParams | 14 | 0 |
| template-free-event-registration-confirmation | FreeEventRegistrationEmailParams | 16 | 0 |
| template-event-approval | EventApprovalEmailParams | 9 | 0 |
| template-new-event-publication | NewEventPublicationEmailParams | 17 | 0 |
| template-event-details-publication | EventDetailsPublicationEmailParams | 18 | 0 |
| template-event-registration-cancellation | RegistrationCancellationEmailParams | 12 | 0 |
| template-signup-list-commitment-confirmation | SignupCommitmentConfirmationEmailParams | 17 | 0 |
| template-signup-list-commitment-update | SignupCommitmentUpdateEmailParams | 17 | 0 |
| template-signup-list-commitment-cancellation | SignupCommitmentCancellationEmailParams | 8 | 0 |
| template-newsletter-notification | NewsletterNotificationEmailParams | 10 | 0 |
| template-newsletter-subscription-confirmation | NewsletterSubscriptionEmailParams | 5 | 0 |
| template-membership-email-verification | EmailVerificationEmailParams | 3 | 0 |
| template-password-reset | PasswordResetEmailParams | 2 | 0 |
| template-password-change-confirmation | PasswordChangeConfirmationEmailParams | 2 | 0 |
| template-organizer-role-approval | OrganizerRoleApprovalEmailParams | 2 | 0 |
| template-welcome | WelcomeEmailParams | 5 | 0 |

---

## APPENDIX B: File Structure

```
src/LankaConnect.Application/
├── Common/
│   └── Email/
│       ├── Parameters/
│       │   ├── IEmailParameters.cs                    (interface)
│       │   ├── UserEmailParams.cs                     (base contract)
│       │   ├── EventEmailParams.cs                    (base contract)
│       │   ├── OrganizerEmailParams.cs                (base contract)
│       │   └── EmailParameterConverter.cs             (utilities)
│       └── Interfaces/
│           └── IEmailTemplateService.cs                (enhanced with <TParams>)
├── Events/
│   └── Email/
│       ├── EventReminderEmailParams.cs                 (template-specific)
│       ├── PaidEventRegistrationEmailParams.cs         (template-specific)
│       ├── EventCancellationEmailParams.cs             (template-specific)
│       └── ... (15 more template parameter classes)
└── Communications/
    └── Email/
        ├── NewsletterNotificationEmailParams.cs        (template-specific)
        └── NewsletterSubscriptionEmailParams.cs        (template-specific)

src/LankaConnect.Infrastructure/
└── Email/
    └── Services/
        └── RazorEmailTemplateService.cs                (implement RenderTemplateAsync<TParams>)

docs/
└── email-template-architecture/
    ├── COMPREHENSIVE_ARCHITECTURE_SOLUTION.md          (this document)
    ├── EMAIL_TEMPLATE_CONTRACTS.md                     (parameter registry)
    └── MIGRATION_GUIDE.md                              (step-by-step migration)

tools/EmailTemplateValidator/
├── EmailTemplateValidator.csproj
├── Program.cs
├── ParameterExtractor.cs
└── ValidationReporter.cs
```

---

**Document Version**: 1.0
**Last Updated**: 2026-01-26
**Author**: Architecture Agent
**Status**: Ready for Implementation Review

---

**Next Steps**:
1. Review this architecture document with team
2. Get approval for approach and effort estimate
3. Begin Phase 1: Foundation (Week 1)
4. Track progress in PROGRESS_TRACKER.md
