# Logging and Error Handling - Implementation Plan
## Phase 6A.52: Silent Failure Prevention System

**Date**: 2025-12-27
**Related Documents**:
- [LOGGING_ERROR_HANDLING_AUDIT.md](./LOGGING_ERROR_HANDLING_AUDIT.md)
- [PHASE_6A_MASTER_INDEX.md](./PHASE_6A_MASTER_INDEX.md)

---

## Overview

This plan implements comprehensive logging to eliminate "silent failures" in the paid event registration flow, where operations succeed at the database level but downstream actions (emails, notifications) fail with NO error logs.

## Critical Finding

**Root Cause**: PaymentCompletedEventHandler executes but provides ZERO visibility into internal operations. The handler logs entry/exit but not intermediate steps (template rendering, email creation, ticket generation), creating a "black box" where failures occur silently.

---

## Phase 1: Critical Fixes (P0) - Must Complete Today

### 1.1 PaymentCompletedEventHandler - Comprehensive Logging

**File**: `src/LankaConnect.Application/Events/EventHandlers/PaymentCompletedEventHandler.cs`

**Issue**: Handler has NO logging for critical internal operations:
- Template rendering (lines 188-197)
- Email message construction (lines 200-218)
- Email sending (line 221)
- Ticket generation intermediate steps (lines 153-183)

**Changes Required**: Add 15+ log statements at critical checkpoints

#### Change 1.1.1: Add correlation logging at handler entry

**Location**: After line 49 (handler invoked log)

```csharp
// BEFORE (line 47-49)
_logger.LogInformation(
    "[Phase 6A.24] ✅ PaymentCompletedEventHandler INVOKED - Event {EventId}, Registration {RegistrationId}, Amount {Amount}, Email {Email}",
    domainEvent.EventId, domainEvent.RegistrationId, domainEvent.AmountPaid, domainEvent.ContactEmail);

// AFTER - Add structured logging scope
_logger.LogInformation(
    "[Phase 6A.24] ✅ PaymentCompletedEventHandler INVOKED - Event {EventId}, Registration {RegistrationId}, Amount {Amount}, Email {Email}",
    domainEvent.EventId, domainEvent.RegistrationId, domainEvent.AmountPaid, domainEvent.ContactEmail);

using var logScope = _logger.BeginScope(new Dictionary<string, object>
{
    ["Handler"] = "PaymentCompletedEventHandler",
    ["EventId"] = domainEvent.EventId,
    ["RegistrationId"] = domainEvent.RegistrationId,
    ["PaymentIntentId"] = domainEvent.PaymentIntentId
});
```

#### Change 1.1.2: Add ticket generation logging

**Location**: Lines 152-183 (ticket generation section)

```csharp
// BEFORE (line 152)
var ticketResult = await _ticketService.GenerateTicketAsync(
    registration.Id,
    @event.Id,
    cancellationToken);

// AFTER - Add before/after logging
_logger.LogInformation(
    "[PaymentTicket-1] Starting ticket generation for Registration {RegistrationId}, Event {EventId}",
    registration.Id, @event.Id);

var ticketResult = await _ticketService.GenerateTicketAsync(
    registration.Id,
    @event.Id,
    cancellationToken);

if (ticketResult.IsSuccess)
{
    _logger.LogInformation(
        "[PaymentTicket-2] Ticket generated successfully: TicketCode={TicketCode}, TicketId={TicketId}",
        ticketResult.Value.TicketCode, ticketResult.Value.TicketId);

    parameters["HasTicket"] = true;
    parameters["TicketCode"] = ticketResult.Value.TicketCode;
    parameters["TicketExpiryDate"] = @event.EndDate.AddDays(1).ToString("MMMM dd, yyyy");

    // Get PDF bytes for email attachment
    _logger.LogInformation(
        "[PaymentTicket-3] Retrieving ticket PDF for TicketId {TicketId}",
        ticketResult.Value.TicketId);

    var pdfResult = await _ticketService.GetTicketPdfAsync(ticketResult.Value.TicketId, cancellationToken);

    if (pdfResult.IsSuccess)
    {
        pdfAttachment = pdfResult.Value;
        _logger.LogInformation(
            "[PaymentTicket-4] Ticket PDF retrieved successfully, size: {Size} bytes",
            pdfAttachment.Length);
    }
    else
    {
        _logger.LogError(
            "[PaymentTicket-ERROR-1] Failed to retrieve ticket PDF for TicketId {TicketId}: {Errors}. Continuing without PDF attachment.",
            ticketResult.Value.TicketId, string.Join(", ", pdfResult.Errors));
    }
}
else
{
    _logger.LogError(
        "[PaymentTicket-ERROR-2] Failed to generate ticket for Registration {RegistrationId}: {Errors}. Email will be sent without ticket.",
        registration.Id, string.Join(", ", ticketResult.Errors));
    parameters["HasTicket"] = false;
}
```

#### Change 1.1.3: Add template rendering logging

**Location**: Lines 188-197 (template rendering section)

```csharp
// BEFORE (line 188)
var renderResult = await _emailTemplateService.RenderTemplateAsync(
    "ticket-confirmation",
    parameters,
    cancellationToken);

if (renderResult.IsFailure)
{
    _logger.LogError("Failed to render email template 'ticket-confirmation': {Error}", renderResult.Error);
    return;
}

// AFTER - Add comprehensive logging
_logger.LogInformation(
    "[PaymentEmail-1] Starting template rendering for 'ticket-confirmation'. ParameterCount: {Count}, HasTicket: {HasTicket}",
    parameters.Count, parameters.ContainsKey("HasTicket") ? parameters["HasTicket"] : "N/A");

_logger.LogDebug(
    "[PaymentEmail-DEBUG] Template parameters: {@Parameters}",
    parameters); // Use structured logging for full parameter dump

var renderResult = await _emailTemplateService.RenderTemplateAsync(
    "ticket-confirmation",
    parameters,
    cancellationToken);

if (renderResult.IsFailure)
{
    _logger.LogError(
        "[PaymentEmail-ERROR-1] CRITICAL: Failed to render email template 'ticket-confirmation' for Registration {RegistrationId}: {Error}. Email will NOT be sent.",
        domainEvent.RegistrationId, renderResult.Error);
    return;
}

_logger.LogInformation(
    "[PaymentEmail-2] Template rendered successfully. Subject: '{Subject}', HtmlBodyLength: {HtmlLength}, PlainTextLength: {PlainLength}",
    renderResult.Value.Subject,
    renderResult.Value.HtmlBody?.Length ?? 0,
    renderResult.Value.PlainTextBody?.Length ?? 0);
```

#### Change 1.1.4: Add email construction and sending logging

**Location**: Lines 200-233 (email construction and sending)

```csharp
// BEFORE (line 199)
var emailMessage = new EmailMessageDto
{
    ToEmail = recipientEmail,
    ToName = recipientName,
    Subject = renderResult.Value.Subject,
    HtmlBody = renderResult.Value.HtmlBody,
    PlainTextBody = renderResult.Value.PlainTextBody,
    Attachments = pdfAttachment != null
        ? new List<EmailAttachment>
        {
            new EmailAttachment
            {
                FileName = $"ticket-{ticketResult.Value?.TicketCode ?? "event"}.pdf",
                Content = pdfAttachment,
                ContentType = "application/pdf"
            }
        }
        : null
};

// Send email with attachment
var result = await _emailService.SendEmailAsync(emailMessage, cancellationToken);

// AFTER - Add comprehensive logging
_logger.LogInformation(
    "[PaymentEmail-3] Constructing email message for {RecipientEmail} ({RecipientName}). Subject: '{Subject}', HasAttachment: {HasAttachment}",
    recipientEmail, recipientName, renderResult.Value.Subject, pdfAttachment != null);

var emailMessage = new EmailMessageDto
{
    ToEmail = recipientEmail,
    ToName = recipientName,
    Subject = renderResult.Value.Subject,
    HtmlBody = renderResult.Value.HtmlBody,
    PlainTextBody = renderResult.Value.PlainTextBody,
    Attachments = pdfAttachment != null
        ? new List<EmailAttachment>
        {
            new EmailAttachment
            {
                FileName = $"ticket-{ticketResult.Value?.TicketCode ?? "event"}.pdf",
                Content = pdfAttachment,
                ContentType = "application/pdf"
            }
        }
        : null
};

if (pdfAttachment != null)
{
    _logger.LogInformation(
        "[PaymentEmail-4] Email includes PDF attachment: FileName='{FileName}', Size={Size} bytes",
        $"ticket-{ticketResult.Value?.TicketCode ?? "event"}.pdf", pdfAttachment.Length);
}

// Send email with attachment
_logger.LogInformation(
    "[PaymentEmail-5] Sending payment confirmation email to {RecipientEmail} for Registration {RegistrationId}",
    recipientEmail, domainEvent.RegistrationId);

var result = await _emailService.SendEmailAsync(emailMessage, cancellationToken);

_logger.LogInformation(
    "[PaymentEmail-6] Email send operation completed. IsSuccess: {IsSuccess}, IsFailure: {IsFailure}",
    result.IsSuccess, result.IsFailure);

if (result.IsFailure)
{
    _logger.LogError(
        "[PaymentEmail-ERROR-2] CRITICAL: Failed to send payment confirmation email to {Email} for Registration {RegistrationId}: {Errors}",
        recipientEmail, domainEvent.RegistrationId, string.Join(", ", result.Errors));
}
else
{
    _logger.LogInformation(
        "[PaymentEmail-SUCCESS] Payment confirmation email sent successfully to {Email} for Registration {RegistrationId} with {AttendeeCount} attendees, HasTicket: {HasTicket}",
        recipientEmail, domainEvent.RegistrationId, registration.Attendees.Count, parameters["HasTicket"]);
}
```

#### Change 1.1.5: Improve exception handler

**Location**: Lines 236-242 (exception handler)

```csharp
// BEFORE
catch (Exception ex)
{
    // Fail-silent pattern: Log error but don't throw to prevent transaction rollback
    _logger.LogError(ex,
        "Error handling PaymentCompletedEvent for Event {EventId}, Registration {RegistrationId}",
        domainEvent.EventId, domainEvent.RegistrationId);
}

// AFTER - Add comprehensive diagnostic information
catch (Exception ex)
{
    _logger.LogError(ex,
        "[CRITICAL] PaymentCompletedEventHandler FAILED for Event {EventId}, Registration {RegistrationId}. " +
        "ExceptionType: {ExceptionType}, Message: {Message}, InnerException: {InnerException}. " +
        "Event Details: ContactEmail={ContactEmail}, AmountPaid={AmountPaid}, PaymentIntentId={PaymentIntentId}, PaymentDate={PaymentDate}. " +
        "StackTrace: {StackTrace}",
        domainEvent.EventId, domainEvent.RegistrationId,
        ex.GetType().FullName, ex.Message, ex.InnerException?.Message ?? "None",
        domainEvent.ContactEmail, domainEvent.AmountPaid, domainEvent.PaymentIntentId, domainEvent.PaymentCompletedAt,
        ex.StackTrace);

    // Log inner exception details if present
    if (ex.InnerException != null)
    {
        _logger.LogError(
            "[CRITICAL-INNER] Inner exception details: Type={Type}, Message={Message}, StackTrace={StackTrace}",
            ex.InnerException.GetType().FullName, ex.InnerException.Message, ex.InnerException.StackTrace);
    }
}
```

**Estimated Time**: 30-45 minutes
**Testing**: Deploy to staging, trigger payment, verify all logs appear

---

### 1.2 AppDbContext - Wrap MediatR.Publish in Try-Catch

**File**: `src/LankaConnect.Infrastructure/Data/AppDbContext.cs`

**Issue**: `_publisher.Publish()` (line 407) not wrapped in try-catch, causing handler exceptions to bubble up and potentially leave transaction in inconsistent state.

**Location**: Lines 396-414 (domain event dispatch loop)

```csharp
// BEFORE
foreach (var domainEvent in domainEvents)
{
    var eventType = domainEvent.GetType();
    _logger.LogInformation("[DIAG-17] About to dispatch domain event: {EventType}", eventType.Name);

    var notificationType = typeof(DomainEventNotification<>).MakeGenericType(eventType);
    var notification = Activator.CreateInstance(notificationType, domainEvent);

    if (notification != null)
    {
        _logger.LogInformation("[DIAG-18] Publishing notification for: {EventType}", eventType.Name);
        await _publisher.Publish(notification, cancellationToken);
        _logger.LogInformation("[Phase 6A.24] Successfully dispatched domain event: {EventType}", eventType.Name);
    }
    else
    {
        _logger.LogWarning("[Phase 6A.24] Failed to create notification for domain event: {EventType}", eventType.Name);
    }
}

// AFTER - Add try-catch around handler execution
foreach (var domainEvent in domainEvents)
{
    var eventType = domainEvent.GetType();
    _logger.LogInformation("[DIAG-17] About to dispatch domain event: {EventType}", eventType.Name);

    try
    {
        var notificationType = typeof(DomainEventNotification<>).MakeGenericType(eventType);
        var notification = Activator.CreateInstance(notificationType, domainEvent);

        if (notification != null)
        {
            _logger.LogInformation("[DIAG-18] Publishing notification for: {EventType}", eventType.Name);
            await _publisher.Publish(notification, cancellationToken);
            _logger.LogInformation("[Phase 6A.24] Successfully dispatched domain event: {EventType}", eventType.Name);
        }
        else
        {
            _logger.LogWarning("[Phase 6A.24] Failed to create notification for domain event: {EventType}", eventType.Name);
        }
    }
    catch (Exception ex)
    {
        // CRITICAL: Handler failed - log but continue with other events
        // This prevents one handler failure from blocking other handlers
        _logger.LogError(ex,
            "[CRITICAL] Domain event handler FAILED for {EventType}. " +
            "ExceptionType: {ExceptionType}, Message: {Message}, InnerException: {InnerException}, StackTrace: {StackTrace}. " +
            "Event Details: {@DomainEvent}",
            eventType.Name,
            ex.GetType().FullName, ex.Message, ex.InnerException?.Message ?? "None", ex.StackTrace,
            domainEvent);

        // DECISION POINT: Continue vs. Throw
        // Current: Continue (allows partial success - some handlers may succeed)
        // Alternative: throw; (fail-fast - rollback entire transaction)
        // For payment flow: Continue is safer (payment already processed, better to have partial email failure than rollback payment)
    }
}
```

**Estimated Time**: 15 minutes
**Testing**: Inject failure into handler, verify logs capture exception and other handlers continue

---

### 1.3 PaymentsController - Add Correlation ID

**File**: `src/LankaConnect.API/Controllers/PaymentsController.cs`

**Issue**: No correlation ID to trace single payment through entire system.

**Location**: Lines 228-298 (Webhook method)

```csharp
// BEFORE (line 228)
[HttpPost("webhook")]
[AllowAnonymous]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> Webhook()
{
    // CRITICAL: Log that we've reached the webhook endpoint
    _logger.LogInformation("Webhook endpoint reached - Method: {Method}, Path: {Path}, ContentType: {ContentType}, ContentLength: {ContentLength}",
        HttpContext.Request.Method,
        HttpContext.Request.Path,
        HttpContext.Request.ContentType,
        HttpContext.Request.ContentLength);

    var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
    var signatureHeader = Request.Headers["Stripe-Signature"].ToString();

// AFTER - Add correlation ID at entry
[HttpPost("webhook")]
[AllowAnonymous]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> Webhook()
{
    // Generate correlation ID for end-to-end tracing
    var correlationId = Guid.NewGuid();

    // CRITICAL: Log that we've reached the webhook endpoint WITH correlation ID
    _logger.LogInformation(
        "[Webhook-Entry] Webhook endpoint reached - CorrelationId: {CorrelationId}, Method: {Method}, Path: {Path}, ContentType: {ContentType}, ContentLength: {ContentLength}",
        correlationId,
        HttpContext.Request.Method,
        HttpContext.Request.Path,
        HttpContext.Request.ContentType,
        HttpContext.Request.ContentLength);

    var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
    var signatureHeader = Request.Headers["Stripe-Signature"].ToString();

    // Continue with existing code...
    // Add correlationId to all subsequent logs in this method
```

**Then update HandleCheckoutSessionCompletedAsync signature and calls**:

```csharp
// BEFORE (line 266-269)
case "checkout.session.completed":
    await HandleCheckoutSessionCompletedAsync(stripeEvent);
    break;

// AFTER
case "checkout.session.completed":
    await HandleCheckoutSessionCompletedAsync(stripeEvent, correlationId);
    break;

// Update method signature (line 303)
// BEFORE
private async Task HandleCheckoutSessionCompletedAsync(Stripe.Event stripeEvent)

// AFTER
private async Task HandleCheckoutSessionCompletedAsync(Stripe.Event stripeEvent, Guid correlationId)
{
    // Add correlation ID to logging scope
    using var scope = _logger.BeginScope(new Dictionary<string, object>
    {
        ["CorrelationId"] = correlationId,
        ["StripeEventId"] = stripeEvent.Id
    });

    try
    {
        var session = stripeEvent.Data.Object as Session;
        // ... rest of method
```

**Estimated Time**: 20 minutes
**Testing**: Verify correlation ID appears in all webhook-related logs

---

## Phase 2: Critical Diagnostics (P1) - This Week

### 2.1 Handler Registration Diagnostics Endpoint

**File**: `src/LankaConnect.API/Controllers/DiagnosticsController.cs` (NEW FILE)

**Purpose**: Verify MediatR handler registration at runtime

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using System.Reflection;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Phase 6A.52: Diagnostics endpoints for runtime verification
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator")] // Only admins can access diagnostics
public class DiagnosticsController : ControllerBase
{
    private readonly ILogger<DiagnosticsController> _logger;

    public DiagnosticsController(ILogger<DiagnosticsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Lists all registered MediatR notification handlers
    /// </summary>
    [HttpGet("handlers")]
    [ProducesResponseType(typeof(List<HandlerInfo>), StatusCodes.Status200OK)]
    public IActionResult GetRegisteredHandlers()
    {
        var applicationAssembly = typeof(Application.DependencyInjection).Assembly;

        var handlers = applicationAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(INotificationHandler<>)))
            .Select(t => new HandlerInfo
            {
                HandlerType = t.FullName ?? t.Name,
                EventTypes = t.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
                    .Select(i => i.GetGenericArguments()[0].FullName ?? i.GetGenericArguments()[0].Name)
                    .ToList(),
                AssemblyName = t.Assembly.GetName().Name ?? "Unknown"
            })
            .OrderBy(h => h.HandlerType)
            .ToList();

        _logger.LogInformation("Diagnostics: Found {Count} registered handlers", handlers.Count);

        return Ok(new
        {
            TotalHandlers = handlers.Count,
            Handlers = handlers,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Verifies specific handler is registered
    /// </summary>
    [HttpGet("handlers/verify/{handlerName}")]
    [ProducesResponseType(typeof(HandlerVerificationResult), StatusCodes.Status200OK)]
    public IActionResult VerifyHandler(string handlerName)
    {
        var applicationAssembly = typeof(Application.DependencyInjection).Assembly;

        var handler = applicationAssembly.GetTypes()
            .FirstOrDefault(t => t.Name.Equals(handlerName, StringComparison.OrdinalIgnoreCase) ||
                                t.FullName?.Equals(handlerName, StringComparison.OrdinalIgnoreCase) == true);

        if (handler == null)
        {
            return Ok(new HandlerVerificationResult
            {
                IsRegistered = false,
                HandlerName = handlerName,
                Message = "Handler type not found in assembly"
            });
        }

        var isNotificationHandler = handler.GetInterfaces().Any(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(INotificationHandler<>));

        return Ok(new HandlerVerificationResult
        {
            IsRegistered = isNotificationHandler,
            HandlerName = handler.FullName ?? handler.Name,
            EventTypes = isNotificationHandler
                ? handler.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
                    .Select(i => i.GetGenericArguments()[0].FullName ?? i.GetGenericArguments()[0].Name)
                    .ToList()
                : new List<string>(),
            Message = isNotificationHandler
                ? "Handler is registered and implements INotificationHandler"
                : "Handler found but does not implement INotificationHandler"
        });
    }
}

public record HandlerInfo
{
    public required string HandlerType { get; init; }
    public required List<string> EventTypes { get; init; }
    public required string AssemblyName { get; init; }
}

public record HandlerVerificationResult
{
    public required bool IsRegistered { get; init; }
    public required string HandlerName { get; init; }
    public List<string> EventTypes { get; init; } = new();
    public required string Message { get; init; }
}
```

**Testing**:
```bash
# List all handlers
curl -H "Authorization: Bearer {admin-token}" https://localhost:7001/api/diagnostics/handlers

# Verify specific handler
curl -H "Authorization: Bearer {admin-token}" https://localhost:7001/api/diagnostics/handlers/verify/PaymentCompletedEventHandler
```

**Estimated Time**: 45 minutes

---

### 2.2 Payment Flow Checkpoint Tracking

**File**: `src/LankaConnect.Application/Common/Interfaces/IPaymentFlowTracker.cs` (NEW FILE)

```csharp
namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Phase 6A.52: Tracks payment flow checkpoints for end-to-end observability
/// </summary>
public interface IPaymentFlowTracker
{
    /// <summary>
    /// Logs a checkpoint in the payment flow
    /// </summary>
    Task LogCheckpointAsync(
        Guid correlationId,
        string checkpoint,
        Dictionary<string, object> metadata,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all checkpoints for a correlation ID
    /// </summary>
    Task<List<PaymentFlowCheckpoint>> GetCheckpointsAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default);
}

public record PaymentFlowCheckpoint
{
    public required Guid CorrelationId { get; init; }
    public required string Checkpoint { get; init; }
    public required DateTime Timestamp { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Standard checkpoint names for payment flow
/// </summary>
public static class PaymentFlowCheckpoints
{
    public const string WebhookReceived = "webhook_received";
    public const string PaymentDomainEventCreated = "payment_domain_event_created";
    public const string HandlerStarted = "handler_started";
    public const string TicketGenerationStarted = "ticket_generation_started";
    public const string TicketGenerated = "ticket_generated";
    public const string TemplateRenderingStarted = "template_rendering_started";
    public const string TemplateRendered = "template_rendered";
    public const string EmailConstructed = "email_constructed";
    public const string EmailSendStarted = "email_send_started";
    public const string EmailSent = "email_sent";
    public const string FlowComplete = "flow_complete";
    public const string FlowFailed = "flow_failed";
}
```

**File**: `src/LankaConnect.Infrastructure/Services/PaymentFlowTracker.cs` (NEW FILE)

```csharp
using LankaConnect.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Infrastructure.Services;

/// <summary>
/// Phase 6A.52: In-memory implementation of payment flow tracker (can be upgraded to database-backed)
/// </summary>
public class PaymentFlowTracker : IPaymentFlowTracker
{
    private readonly ILogger<PaymentFlowTracker> _logger;
    private readonly Dictionary<Guid, List<PaymentFlowCheckpoint>> _checkpoints = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public PaymentFlowTracker(ILogger<PaymentFlowTracker> logger)
    {
        _logger = logger;
    }

    public async Task LogCheckpointAsync(
        Guid correlationId,
        string checkpoint,
        Dictionary<string, object> metadata,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!_checkpoints.ContainsKey(correlationId))
            {
                _checkpoints[correlationId] = new List<PaymentFlowCheckpoint>();
            }

            var checkpointRecord = new PaymentFlowCheckpoint
            {
                CorrelationId = correlationId,
                Checkpoint = checkpoint,
                Timestamp = DateTime.UtcNow,
                Metadata = metadata
            };

            _checkpoints[correlationId].Add(checkpointRecord);

            _logger.LogInformation(
                "[FlowTracker] Checkpoint logged: CorrelationId={CorrelationId}, Checkpoint={Checkpoint}, Metadata={@Metadata}",
                correlationId, checkpoint, metadata);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<PaymentFlowCheckpoint>> GetCheckpointsAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return _checkpoints.TryGetValue(correlationId, out var checkpoints)
                ? checkpoints.ToList()
                : new List<PaymentFlowCheckpoint>();
        }
        finally
        {
            _lock.Release();
        }
    }
}
```

**Register in DI** (`src/LankaConnect.Infrastructure/DependencyInjection.cs`):

```csharp
// Add to ConfigureInfrastructureServices method
services.AddSingleton<IPaymentFlowTracker, PaymentFlowTracker>();
```

**Estimated Time**: 1 hour

---

## Phase 3: Enhanced Observability (P2) - Next Sprint

### 3.1 AzureEmailService Enhanced Logging

**File**: `src/LankaConnect.Infrastructure/Email/Services/AzureEmailService.cs`

**Changes**:
1. Add parameter validation logging in `RenderTemplateAsync` (line 634)
2. Add pre-validation logging in `SendEmailAsync` (line 59)
3. Add success confirmation in `CreateDomainEmailMessage` (line 437)

**Estimated Time**: 30 minutes

---

### 3.2 TicketService Enhanced Logging

**File**: `src/LankaConnect.Infrastructure/Services/Tickets/TicketService.cs`

**Changes**:
1. Add QR code generation logging (line 120)
2. Add PDF generation start logging (line 148)
3. Make PDF upload logging more prominent (line 364)

**Estimated Time**: 20 minutes

---

## Testing Plan

### Unit Tests
- Add tests for new logging paths in PaymentCompletedEventHandler
- Verify correlation ID propagation
- Test handler registration diagnostics endpoint

### Integration Tests
1. **Happy Path**: Paid event registration → payment → email sent
2. **Template Failure**: Simulate template not found → verify error logged
3. **Email Failure**: Simulate email service down → verify error logged
4. **Ticket Failure**: Simulate ticket generation failure → verify email sent without ticket
5. **Handler Exception**: Inject exception in handler → verify caught and logged

### Staging Validation
1. Deploy Phase 1 changes to staging
2. Process test payment
3. Verify all expected log entries appear:
   ```
   [Webhook-Entry] Webhook endpoint reached - CorrelationId: ...
   [PaymentTicket-1] Starting ticket generation
   [PaymentTicket-4] Ticket PDF retrieved successfully
   [PaymentEmail-1] Starting template rendering
   [PaymentEmail-2] Template rendered successfully
   [PaymentEmail-5] Sending payment confirmation email
   [PaymentEmail-SUCCESS] Payment confirmation email sent successfully
   ```
4. Simulate failure scenarios and verify error logs

---

## Success Criteria

After implementing Phase 1 (P0 fixes), we must be able to:

1. ✅ See EXACTLY where PaymentCompletedEventHandler fails (or if it executes at all)
2. ✅ Trace a single payment from webhook → email sent with correlation ID
3. ✅ Never see "silent failures" - every error logged with full context
4. ✅ Diagnose issues from logs alone without database queries
5. ✅ Identify which step failed within 30 seconds of looking at logs

---

## Rollout Plan

### Day 1 (Today)
- [ ] Implement Phase 1.1: PaymentCompletedEventHandler logging (45 min)
- [ ] Implement Phase 1.2: AppDbContext try-catch wrapper (15 min)
- [ ] Implement Phase 1.3: Correlation ID (20 min)
- [ ] Unit test changes (30 min)
- [ ] Deploy to staging (15 min)
- [ ] Staging validation (30 min)
- **Total**: ~2.5 hours

### Day 2
- [ ] Monitor staging for 24 hours
- [ ] Review logs from real payment flows
- [ ] Fix any issues found
- [ ] Deploy to production with monitoring

### Week 1
- [ ] Implement Phase 2.1: Handler diagnostics endpoint (45 min)
- [ ] Implement Phase 2.2: Payment flow tracker (1 hour)
- [ ] Deploy and validate

### Sprint End
- [ ] Implement Phase 3: Enhanced observability
- [ ] Final documentation update
- [ ] Mark Phase 6A.52 complete

---

## Rollback Plan

If issues occur after deployment:

1. **Logs consuming too much storage**: Adjust log levels (change Information → Debug for verbose logs)
2. **Performance impact**: Remove structured logging (@Parameters) from hot paths
3. **Breaking change**: Correlation ID parameter is additive, backward compatible
4. **Handler try-catch issues**: Remove try-catch, allow exceptions to bubble (revert to original behavior)

---

## Related Phases

- **Phase 6A.50**: Added diagnostic logging for domain event dispatch
- **Phase 6A.51**: Fixed domain event dispatch (restored Update() call)
- **Phase 6A.52**: This phase - comprehensive logging and error handling
- **Phase 6A.53**: (Future) Implement payment flow dashboard using checkpoint data

---

## Appendix: Log Level Guidelines

Use appropriate log levels:

- **LogTrace**: Very detailed (disabled in production)
- **LogDebug**: Diagnostic details (enabled for troubleshooting)
- **LogInformation**: Normal flow checkpoints ✅ **Use this for Phase 1**
- **LogWarning**: Recoverable issues (ticket generation failed but email sent)
- **LogError**: Failures requiring attention ✅ **Use this for all errors**
- **LogCritical**: System-level failures (database down, email service unavailable)
