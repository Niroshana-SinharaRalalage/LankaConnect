# Implementation Quick Start Guide
## Email Parameter Standardization Architecture

**For Developers**: Step-by-step guide to implement strongly-typed email parameters

---

## OVERVIEW

This guide shows how to migrate from manual Dictionary construction to strongly-typed email parameters in 3 simple steps.

**Before**:
```csharp
// ❌ Manual, error-prone, no compile-time safety
var parameters = new Dictionary<string, object>
{
    { "UserName", user.Name },
    { "EventTitle", @event.Title.Value },
    // Missing TicketCode? No compiler warning!
    // Typo in parameter name? Only found in production!
};
```

**After**:
```csharp
// ✅ Type-safe, compile-time validated, IntelliSense support
var emailParams = new EventReminderEmailParams
{
    User = UserEmailParams.From(user),
    Event = EventEmailParams.From(@event, _urlHelper),
    Organizer = OrganizerEmailParams.From(@event)!,
    ReminderTimeframe = "24 hours",
    TicketCode = ticket.TicketCode,
    // Missing required property? ❌ Build fails!
    // Typo? ❌ Compiler error!
};
```

---

## STEP 1: CREATE BASE PARAMETER CONTRACTS (One-Time Setup)

These are reusable across ALL email handlers.

### 1.1 Create `IEmailParameters` Interface

**File**: `src/LankaConnect.Application/Common/Email/Parameters/IEmailParameters.cs`

```csharp
namespace LankaConnect.Application.Common.Email.Parameters;

/// <summary>
/// Base interface for all email parameter objects.
/// Enables automatic conversion to Dictionary for template rendering.
/// </summary>
public interface IEmailParameters
{
    /// <summary>
    /// Converts this parameter object to a dictionary for Handlebars template rendering.
    /// </summary>
    Dictionary<string, object> ToDictionary();
}
```

### 1.2 Create `UserEmailParams` (Reusable)

**File**: `src/LankaConnect.Application/Common/Email/Parameters/UserEmailParams.cs`

```csharp
namespace LankaConnect.Application.Common.Email.Parameters;

/// <summary>
/// Common user-related email parameters.
/// Used across all email templates requiring user information.
/// </summary>
public record UserEmailParams : IEmailParameters
{
    public required string UserName { get; init; }
    public string? Email { get; init; }

    /// <summary>
    /// Factory method: Creates UserEmailParams from User entity.
    /// </summary>
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
```

### 1.3 Create `EventEmailParams` (Reusable)

**File**: `src/LankaConnect.Application/Common/Email/Parameters/EventEmailParams.cs`

```csharp
namespace LankaConnect.Application.Common.Email.Parameters;

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
        // Legacy: EventDateTime for backward compatibility
        { "EventDateTime", $"{EventStartDate} at {EventStartTime}" }
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
```

### 1.4 Create `OrganizerEmailParams` (Reusable)

**File**: `src/LankaConnect.Application/Common/Email/Parameters/OrganizerEmailParams.cs`

```csharp
namespace LankaConnect.Application.Common.Email.Parameters;

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

        // OLD parameter names (backward compatibility - Phase 6B will remove these)
        { "OrganizerContactName", OrganizerName },
        { "OrganizerContactEmail", OrganizerEmail },
        { "OrganizerContactPhone", OrganizerPhone ?? "" },

        // Flags for conditional rendering
        { "HasOrganizerContact", true }
    };
}
```

### 1.5 Create `EmailParameterConverter` Utility

**File**: `src/LankaConnect.Application/Common/Email/EmailParameterConverter.cs`

```csharp
namespace LankaConnect.Application.Common.Email;

/// <summary>
/// Converts strongly-typed email parameter objects to Dictionary for template rendering.
/// </summary>
public static class EmailParameterConverter
{
    public static Dictionary<string, object> ToDictionary(IEmailParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var dict = parameters.ToDictionary();

        // Validation: Replace null values with empty string (Handlebars-safe)
        foreach (var key in dict.Keys.ToList())
        {
            dict[key] ??= "";
        }

        return dict;
    }
}
```

---

## STEP 2: CREATE TEMPLATE-SPECIFIC PARAMETER CLASS

For each email template, create a class that composes base parameters + template-specific fields.

### Example: EventReminderEmailParams

**File**: `src/LankaConnect.Application/Events/Email/EventReminderEmailParams.cs`

```csharp
namespace LankaConnect.Application.Events.Email;

using LankaConnect.Application.Common.Email.Parameters;

/// <summary>
/// Parameters for template-event-reminder email.
///
/// Template Database Name: template-event-reminder
/// Required Parameters: UserName, EventTitle, EventStartDate, EventStartTime, EventLocation,
///                      OrganizerContactName, OrganizerContactEmail, OrganizerContactPhone,
///                      TicketCode, TicketExpiryDate, ReminderTimeframe, AttendeeName, ContactEmail
/// Optional Parameters: EventDescription, ReminderMessage
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
```

**Pattern**:
1. Compose reusable base parameters (User, Event, Organizer)
2. Add template-specific fields
3. Merge all dictionaries in `ToDictionary()`

---

## STEP 3: UPDATE HANDLER TO USE STRONGLY-TYPED PARAMETERS

### Before (Manual Dictionary)

```csharp
public async Task ExecuteAsync(Guid eventId, CancellationToken cancellationToken = default)
{
    var @event = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
    var registration = await _registrationRepository.GetByEventAsync(eventId, cancellationToken);

    // ❌ OLD APPROACH: Manual Dictionary
    var parameters = new Dictionary<string, object>
    {
        { "UserName", registration.User.Name },
        { "EventTitle", @event.Title.Value },
        { "EventStartDate", @event.StartDate.ToString("MMMM dd, yyyy") },
        { "EventStartTime", @event.StartDate.ToString("h:mm tt") },
        { "OrganizerName", @event.OrganizerContactName },  // Wrong parameter name!
        { "OrganizerEmail", @event.OrganizerContactEmail },
        // Missing TicketCode, TicketExpiryDate, ContactEmail!
    };

    await _emailService.SendTemplatedEmailAsync(
        EmailTemplateNames.EventReminder,
        registration.User.Email,
        parameters,
        cancellationToken);
}
```

### After (Strongly-Typed)

```csharp
public async Task ExecuteAsync(Guid eventId, CancellationToken cancellationToken = default)
{
    var @event = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
    var registration = await _registrationRepository.GetByEventAsync(eventId, cancellationToken);

    // Fetch ticket if exists
    var ticket = registration.TicketId.HasValue
        ? await _ticketRepository.GetByIdAsync(registration.TicketId.Value, cancellationToken)
        : null;

    // ✅ NEW APPROACH: Strongly-typed parameter object
    var emailParams = new EventReminderEmailParams
    {
        // Reuse common parameters
        User = UserEmailParams.From(registration.User),
        Event = EventEmailParams.From(@event, _emailUrlHelper),
        Organizer = OrganizerEmailParams.From(@event)!,

        // Template-specific parameters
        ReminderTimeframe = "24 hours",
        AttendeeName = registration.User.FirstName,
        ContactEmail = registration.Contact?.Email ?? registration.User.Email,
        TicketCode = ticket?.TicketCode,
        TicketExpiryDate = ticket?.ExpiryDate.ToString("MMMM dd, yyyy"),
        ReminderMessage = null  // Optional
    };

    // Render template with strongly-typed parameters
    var result = await _emailTemplateService.RenderTemplateAsync(
        EmailTemplateNames.EventReminder,
        emailParams,
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

**Key Changes**:
1. ✅ Build `EventReminderEmailParams` instead of Dictionary
2. ✅ Use `.From()` factory methods for common parameters (code reuse!)
3. ✅ Call `RenderTemplateAsync<TParams>()` overload (strongly-typed)
4. ✅ Compiler enforces all required properties are set

---

## STEP 4: UPDATE EMAIL TEMPLATE SERVICE (One-Time)

Add strongly-typed overload to `IEmailTemplateService`.

### Update Interface

**File**: `src/LankaConnect.Application/Common/Interfaces/IEmailTemplateService.cs`

```csharp
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
```

### Update Implementation

**File**: `src/LankaConnect.Infrastructure/Email/Services/RazorEmailTemplateService.cs`

```csharp
public class RazorEmailTemplateService : IEmailTemplateService
{
    // NEW: Strongly-typed method
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

            _logger.LogInformation(
                "Rendering template {TemplateName} with strongly-typed parameters {ParamsType}",
                templateName, typeof(TParams).Name);

            // Delegate to existing dictionary-based implementation
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

    // EXISTING: Dictionary-based method (unchanged)
    public async Task<Result<RenderedEmailTemplate>> RenderTemplateAsync(
        string templateName,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        // Existing implementation (no changes needed)
        // ...
    }
}
```

---

## TESTING CHECKLIST

### Unit Tests (Per Parameter Class)

```csharp
public class EventReminderEmailParamsTests
{
    [Fact]
    public void ToDictionary_ShouldIncludeAllRequiredParameters()
    {
        // Arrange
        var user = UserTestDataBuilder.CreateUser();
        var @event = EventTestDataBuilder.CreateEvent();
        var emailParams = new EventReminderEmailParams
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
        var dict = emailParams.ToDictionary();

        // Assert
        dict.Should().ContainKey("UserName");
        dict.Should().ContainKey("EventTitle");
        dict.Should().ContainKey("OrganizerContactName");
        dict.Should().ContainKey("TicketCode");
        dict.Should().ContainKey("ReminderTimeframe");
    }

    [Fact]
    public void From_WithNullOrganizer_ShouldHandleGracefully()
    {
        var @event = EventTestDataBuilder.CreateEventWithoutOrganizer();
        var organizer = OrganizerEmailParams.From(@event);
        organizer.Should().BeNull();
    }
}
```

### Integration Tests (Per Handler)

```csharp
[Fact]
public async Task EventReminderJob_ShouldRenderEmailWithoutLiteralParameters()
{
    // Arrange
    var @event = await CreateTestEventAsync();
    var registration = await RegisterUserForEventAsync(@event.Id);

    // Act
    await _job.ExecuteAsync(@event.Id, CancellationToken.None);

    // Assert
    var sentEmail = await GetLastSentEmailAsync();
    sentEmail.BodyHtml.Should().NotContain("{{");  // No literal Handlebars
    sentEmail.BodyHtml.Should().Contain(registration.User.FirstName);
    sentEmail.BodyHtml.Should().Contain(@event.OrganizerContactName);
    sentEmail.BodyHtml.Should().Contain("ABC123");  // TicketCode rendered
}
```

### Staging Validation

```bash
# 1. Deploy to staging
git push origin develop

# 2. Trigger email scenario
curl -X POST "https://lankaconnect-api-staging.../api/test/trigger-event-reminder" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"eventId": "..."}'

# 3. Check MailHog for rendered email
curl "http://mailhog-staging:8025/api/v2/messages" | jq '.items[0].Content.Body'

# 4. Verify no literal {{}} in output
curl "http://mailhog-staging:8025/api/v2/messages" | \
  jq '.items[0].Content.Body' | \
  grep -q "{{" && echo "❌ FAILED" || echo "✅ PASSED"
```

---

## MIGRATION CHECKLIST (Per Handler)

- [ ] **Step 1**: Create template-specific parameter class (e.g., `EventReminderEmailParams.cs`)
- [ ] **Step 2**: Write unit tests for parameter class
- [ ] **Step 3**: Update handler to build strongly-typed parameter object
- [ ] **Step 4**: Update handler to call `RenderTemplateAsync<TParams>()`
- [ ] **Step 5**: Update handler unit tests
- [ ] **Step 6**: Run all tests locally (`dotnet test`)
- [ ] **Step 7**: Commit with message `fix(phase-6a): Migrate [HandlerName] to strongly-typed email parameters`
- [ ] **Step 8**: Deploy to staging
- [ ] **Step 9**: Test email rendering in staging (MailHog)
- [ ] **Step 10**: Verify no literal `{{}}` in rendered email
- [ ] **Step 11**: Deploy to production
- [ ] **Step 12**: Monitor logs for 1 hour
- [ ] **Step 13**: Mark handler as ✅ COMPLETE

---

## COMMON PATTERNS

### Pattern 1: Optional Organizer Contact

```csharp
// Use null-conditional operator
Organizer = OrganizerEmailParams.From(@event) ??
            new OrganizerEmailParams {
                OrganizerName = "Event Organizer",
                OrganizerEmail = "support@lankaconnect.com",
                OrganizerPhone = null
            }
```

### Pattern 2: Conditional Ticket Parameters

```csharp
var ticket = registration.TicketId.HasValue
    ? await _ticketRepository.GetByIdAsync(registration.TicketId.Value, cancellationToken)
    : null;

var emailParams = new EventReminderEmailParams
{
    // ...
    TicketCode = ticket?.TicketCode,
    TicketExpiryDate = ticket?.ExpiryDate.ToString("MMMM dd, yyyy")
};
```

### Pattern 3: Attendee List Formatting

```csharp
public static string FormatAttendeeDetailsHtml(IEnumerable<Attendee> attendees)
{
    if (!attendees.Any()) return "";

    var sb = new StringBuilder();
    foreach (var attendee in attendees)
    {
        sb.AppendLine($"<p style=\"margin: 8px 0; font-size: 16px;\">{attendee.Name}</p>");
    }
    return sb.ToString().TrimEnd();
}

// Usage
Attendees = FormatAttendeeDetailsHtml(registration.Attendees),
HasAttendeeDetails = registration.Attendees.Any()
```

---

## TROUBLESHOOTING

### Error: "The following required properties are not set"

```
CS7036: There is no argument given that corresponds to the required
parameter 'TicketCode' of 'EventReminderEmailParams'
```

**Solution**: Add the missing property to your parameter object:

```csharp
var emailParams = new EventReminderEmailParams
{
    // ... other properties
    TicketCode = ticket?.TicketCode ?? ""  // ✅ Add this
};
```

### Error: "Cannot convert from 'null' to 'OrganizerEmailParams'"

```csharp
Organizer = OrganizerEmailParams.From(@event)  // ❌ Returns null if no organizer
```

**Solution**: Use null-forgiving operator or provide default:

```csharp
// Option 1: Null-forgiving (if you know organizer exists)
Organizer = OrganizerEmailParams.From(@event)!

// Option 2: Provide default
Organizer = OrganizerEmailParams.From(@event) ??
            new OrganizerEmailParams {
                OrganizerName = "Event Organizer",
                OrganizerEmail = "support@lankaconnect.com"
            }
```

### Error: "Literal {{}} still appearing in emails"

**Diagnosis**: Parameter name in template doesn't match dictionary key

**Solution**: Check `ToDictionary()` includes BOTH old and new parameter names:

```csharp
public Dictionary<string, object> ToDictionary() => new()
{
    { "OrganizerName", OrganizerName },          // New name
    { "OrganizerContactName", OrganizerName },   // Old name (backward compat)
};
```

---

## NEXT STEPS

1. **Read**: [COMPREHENSIVE_ARCHITECTURE_SOLUTION.md](./COMPREHENSIVE_ARCHITECTURE_SOLUTION.md) for full design
2. **Review**: [ROOT_CAUSE_ANALYSIS_EXECUTIVE_SUMMARY.md](./ROOT_CAUSE_ANALYSIS_EXECUTIVE_SUMMARY.md) for context
3. **Implement**: Follow this quick start guide for each handler migration
4. **Track**: Update [PROGRESS_TRACKER.md](../PROGRESS_TRACKER.md) as handlers are completed

---

**Quick Reference**:
- **Base contracts**: `UserEmailParams`, `EventEmailParams`, `OrganizerEmailParams`
- **Template-specific**: One class per email template (18 total)
- **Handler pattern**: Build params → RenderTemplateAsync<TParams> → SendEmailAsync
- **Testing**: Unit tests (parameter classes) + Integration tests (handlers)

**Questions?** See [COMPREHENSIVE_ARCHITECTURE_SOLUTION.md](./COMPREHENSIVE_ARCHITECTURE_SOLUTION.md) Section 6 (Validation & Testing)
