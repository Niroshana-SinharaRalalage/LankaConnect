# Comprehensive Logging and Error Handling Audit
## Paid Event Registration Flow - End-to-End Analysis

**Date**: 2025-12-27
**Phase**: Phase 6A.51 Follow-up
**Context**: Silent failures where operations succeed at database level but fail to trigger downstream actions (emails, notifications) with NO error logs

---

## Executive Summary

### Critical Finding
**PaymentCompletedEventHandler is executing but has ZERO observable logging of its internal operations.** The handler logs entry (line 47-49) and exceptions (line 236-241), but provides NO visibility into:
- Template rendering success/failure
- Email creation success/failure
- Ticket generation intermediate steps
- Email sending intermediate steps

This creates a "black box" where the handler executes, encounters an internal failure, and exits silently via the catch-all exception handler without providing diagnostic information.

### Root Cause Analysis
The current issue is NOT with domain event dispatch (Phase 6A.51 confirmed MediatR publishes successfully). The problem is **insufficient internal logging within PaymentCompletedEventHandler** that makes it impossible to diagnose where the handler fails.

---

## 1. Stripe Webhook Reception

### File: `PaymentsController.cs`

#### Current State: **GOOD**

**Logging Coverage**:
- ✅ Line 231-235: Webhook endpoint entry with request details
- ✅ Line 240-242: Webhook body received with length and signature check
- ✅ Line 253: Event construction successful
- ✅ Line 256-259: Idempotency check
- ✅ Line 263: Event recorded
- ✅ Line 278: Unhandled event types
- ✅ Line 283: Mark as processed
- ✅ Line 289: Signature verification failed
- ✅ Line 294-295: Generic webhook error with full context

**Exception Handling**: **GOOD**
```csharp
try
{
    // Webhook processing
}
catch (StripeException ex)
{
    _logger.LogError(ex, "Stripe webhook signature verification failed");
    return BadRequest(new { Error = "Invalid signature" });
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error processing webhook - Type: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}",
        ex.GetType().FullName, ex.Message, ex.StackTrace);
    return StatusCode(500);
}
```

#### Issues Found: **MINOR**

**Issue 1: Success path logging gap**
- **Location**: Line 285
- **Problem**: No log after successful event processing
- **Impact**: P2 (nice to have)
- **Recommendation**: Add success log before `return Ok()`

```csharp
// BEFORE (line 283-285)
await _webhookEventRepository.MarkEventAsProcessedAsync(stripeEvent.Id);
return Ok();

// AFTER
await _webhookEventRepository.MarkEventAsProcessedAsync(stripeEvent.Id);
_logger.LogInformation("Webhook event {EventId} processed successfully", stripeEvent.Id);
return Ok();
```

---

## 2. HandleCheckoutSessionCompletedAsync

### File: `PaymentsController.cs` (Lines 303-427)

#### Current State: **EXCELLENT**

**Logging Coverage**:
- ✅ Line 314-317: Processing checkout session with payment status
- ✅ Line 322: Non-paid session skip
- ✅ Line 330: Missing registration_id metadata
- ✅ Line 337: Missing event_id metadata
- ✅ Line 341-344: Completing payment for specific Event/Registration
- ✅ Line 352-354: Registration not found
- ✅ Line 360-364: Registration security check failed
- ✅ Line 369-372: Domain events BEFORE CompletePayment()
- ✅ Line 388-392: Domain events AFTER CompletePayment()
- ✅ Line 402-405: BEFORE CommitAsync
- ✅ Line 411-414: AFTER CommitAsync
- ✅ Line 416-419: Success message
- ✅ Line 423-425: Exception handling with full context

**Exception Handling**: **EXCELLENT**
```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Error handling checkout.session.completed webhook - Type: {ExceptionType}, Message: {Message}, InnerException: {InnerException}",
        ex.GetType().FullName, ex.Message, ex.InnerException?.Message ?? "None");
    throw; // Re-throw to trigger outer catch block with HTTP 500
}
```

#### Issues Found: **NONE**

This section has exemplary logging and error handling. It provides complete observability from webhook receipt to database commit.

---

## 3. AppDbContext.CommitAsync - Domain Event Dispatch

### File: `AppDbContext.cs` (Lines 312-431)

#### Current State: **EXCELLENT** (Phase 6A.50 Diagnostics)

**Logging Coverage**:
- ✅ Line 314: CommitAsync START
- ✅ Line 318-330: Tracked entities BEFORE DetectChanges
- ✅ Line 353-366: Tracked entities AFTER DetectChanges
- ✅ Line 374-377: Domain events collected with types
- ✅ Line 381-384: Domain events to dispatch
- ✅ Line 389: SaveChangesAsync completed
- ✅ Line 394: Dispatching domain events
- ✅ Line 399: About to dispatch specific event
- ✅ Line 406: Publishing notification
- ✅ Line 408: Successfully dispatched event
- ✅ Line 412: Failed to create notification
- ✅ Line 422: All events dispatched
- ✅ Line 426: No domain events (potential issue indicator)
- ✅ Line 429: CommitAsync COMPLETE

**Exception Handling**: **NEEDS IMPROVEMENT**

**Issue 1: MediatR.Publish() not wrapped in try-catch**
- **Location**: Line 407
- **Severity**: **P0 (CRITICAL)**
- **Problem**: If `_publisher.Publish()` throws, the exception bubbles up and transaction may be in inconsistent state
- **Impact**: Silent handler failures with no diagnostic logs

```csharp
// BEFORE (lines 396-414)
foreach (var domainEvent in domainEvents)
{
    var eventType = domainEvent.GetType();
    _logger.LogInformation("[DIAG-17] About to dispatch domain event: {EventType}", eventType.Name);

    var notificationType = typeof(DomainEventNotification<>).MakeGenericType(eventType);
    var notification = Activator.CreateInstance(notificationType, domainEvent);

    if (notification != null)
    {
        _logger.LogInformation("[DIAG-18] Publishing notification for: {EventType}", eventType.Name);
        await _publisher.Publish(notification, cancellationToken); // ⚠️ NOT WRAPPED
        _logger.LogInformation("[Phase 6A.24] Successfully dispatched domain event: {EventType}", eventType.Name);
    }
}

// AFTER
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
        // Log but continue - don't fail entire transaction if one handler fails
        _logger.LogError(ex,
            "[CRITICAL] Domain event handler failed for {EventType}. Event: {@DomainEvent}. Exception: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}",
            eventType.Name, domainEvent, ex.GetType().FullName, ex.Message, ex.StackTrace);

        // DECISION: Continue with other events vs. throw
        // Current: Continue (fail-silent for individual handlers)
        // Alternative: throw; (fail-fast, rollback transaction)
    }
}
```

**Issue 2: Handler registration verification**
- **Location**: N/A (system-level issue)
- **Severity**: P1
- **Problem**: No way to verify if handler is registered with MediatR at runtime
- **Recommendation**: Add diagnostic endpoint or startup validation

```csharp
// Add to DependencyInjection.cs or startup diagnostics
public static IServiceCollection AddApplication(this IServiceCollection services)
{
    var assembly = Assembly.GetExecutingAssembly();

    services.AddMediatR(config =>
    {
        config.RegisterServicesFromAssembly(assembly);
    });

    // NEW: Log all registered handlers at startup
    var handlerTypes = assembly.GetTypes()
        .Where(t => t.GetInterfaces().Any(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(INotificationHandler<>)))
        .ToList();

    // This would be logged at startup
    foreach (var handlerType in handlerTypes)
    {
        Console.WriteLine($"Registered handler: {handlerType.FullName}");
    }

    return services;
}
```

---

## 4. PaymentCompletedEventHandler - THE CRITICAL ISSUE

### File: `PaymentCompletedEventHandler.cs`

#### Current State: **POOR - BLACK BOX**

**Logging Coverage Analysis**:
- ✅ Line 47-49: Handler INVOKED (confirms execution)
- ❌ **NO logging for template rendering** (lines 188-197)
- ❌ **NO logging for email message construction** (lines 200-218)
- ❌ **NO logging for email send attempt** (line 221)
- ❌ **Limited logging for ticket generation** (only if successful, line 161)
- ✅ Line 224-227: Email send failure
- ✅ Line 230-233: Email send success
- ✅ Line 236-241: Generic exception handler (BUT lacks detail)

#### Critical Gaps

**Gap 1: Template Rendering (Lines 188-197) - ZERO LOGGING**
```csharp
// CURRENT CODE - NO LOGGING
var renderResult = await _emailTemplateService.RenderTemplateAsync(
    "ticket-confirmation",
    parameters,
    cancellationToken);

if (renderResult.IsFailure)
{
    _logger.LogError("Failed to render email template 'ticket-confirmation': {Error}", renderResult.Error);
    return;
}

// SHOULD BE
_logger.LogInformation(
    "[PaymentEmail-1] About to render template 'ticket-confirmation' for Registration {RegistrationId}, Event {EventId}",
    domainEvent.RegistrationId, domainEvent.EventId);

var renderResult = await _emailTemplateService.RenderTemplateAsync(
    "ticket-confirmation",
    parameters,
    cancellationToken);

if (renderResult.IsFailure)
{
    _logger.LogError(
        "[PaymentEmail-ERROR-1] Failed to render email template 'ticket-confirmation' for Registration {RegistrationId}: {Error}. Parameters: {@Parameters}",
        domainEvent.RegistrationId, renderResult.Error, parameters);
    return;
}

_logger.LogInformation(
    "[PaymentEmail-2] Successfully rendered template 'ticket-confirmation'. Subject: '{Subject}', HtmlBodyLength: {HtmlLength}, PlainTextLength: {PlainLength}",
    renderResult.Value.Subject, renderResult.Value.HtmlBody?.Length ?? 0, renderResult.Value.PlainTextBody?.Length ?? 0);
```

**Gap 2: Email Message Construction (Lines 200-218) - ZERO LOGGING**
```csharp
// CURRENT CODE - NO LOGGING
var emailMessage = new EmailMessageDto
{
    ToEmail = recipientEmail,
    ToName = recipientName,
    Subject = renderResult.Value.Subject,
    HtmlBody = renderResult.Value.HtmlBody,
    PlainTextBody = renderResult.Value.PlainTextBody,
    Attachments = pdfAttachment != null ? new List<EmailAttachment> { ... } : null
};

// SHOULD BE
_logger.LogInformation(
    "[PaymentEmail-3] Building email message for {RecipientEmail} ({RecipientName}). HasAttachment: {HasAttachment}",
    recipientEmail, recipientName, pdfAttachment != null);

var emailMessage = new EmailMessageDto
{
    ToEmail = recipientEmail,
    ToName = recipientName,
    Subject = renderResult.Value.Subject,
    HtmlBody = renderResult.Value.HtmlBody,
    PlainTextBody = renderResult.Value.PlainTextBody,
    Attachments = pdfAttachment != null ? new List<EmailAttachment>
    {
        new EmailAttachment
        {
            FileName = $"ticket-{ticketResult.Value?.TicketCode ?? "event"}.pdf",
            Content = pdfAttachment,
            ContentType = "application/pdf"
        }
    } : null
};

if (pdfAttachment != null)
{
    _logger.LogInformation(
        "[PaymentEmail-4] Email has PDF attachment: FileName='{FileName}', Size={Size} bytes",
        $"ticket-{ticketResult.Value?.TicketCode ?? "event"}.pdf", pdfAttachment.Length);
}
```

**Gap 3: Email Sending (Line 221) - ZERO PRE-SEND LOGGING**
```csharp
// CURRENT CODE
var result = await _emailService.SendEmailAsync(emailMessage, cancellationToken);

// SHOULD BE
_logger.LogInformation(
    "[PaymentEmail-5] About to send payment confirmation email to {RecipientEmail} for Registration {RegistrationId}",
    recipientEmail, domainEvent.RegistrationId);

var result = await _emailService.SendEmailAsync(emailMessage, cancellationToken);

_logger.LogInformation(
    "[PaymentEmail-6] Email send result: Success={Success}, Error={Error}",
    result.IsSuccess, result.IsFailure ? result.Error : "N/A");
```

**Gap 4: Ticket Generation Internal Steps (Lines 153-183)**
```csharp
// CURRENT CODE - Only logs if successful
if (ticketResult.IsSuccess)
{
    _logger.LogInformation("Ticket generated successfully: {TicketCode}", ticketResult.Value.TicketCode);
    // ... PDF retrieval
}
else
{
    _logger.LogWarning("Failed to generate ticket: {Error}", string.Join(", ", ticketResult.Errors));
}

// SHOULD BE
_logger.LogInformation(
    "[PaymentTicket-1] About to generate ticket for Registration {RegistrationId}, Event {EventId}",
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

    _logger.LogInformation(
        "[PaymentTicket-3] About to retrieve ticket PDF for TicketId {TicketId}",
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
            "[PaymentTicket-ERROR-1] Failed to retrieve ticket PDF for TicketId {TicketId}: {Error}",
            ticketResult.Value.TicketId, string.Join(", ", pdfResult.Errors));
    }
}
else
{
    _logger.LogError(
        "[PaymentTicket-ERROR-2] Failed to generate ticket for Registration {RegistrationId}: {Error}",
        registration.Id, string.Join(", ", ticketResult.Errors));
    parameters["HasTicket"] = false;
}
```

**Gap 5: Exception Handler Lacks Detail (Lines 236-242)**
```csharp
// CURRENT CODE - Generic catch-all
catch (Exception ex)
{
    // Fail-silent pattern: Log error but don't throw to prevent transaction rollback
    _logger.LogError(ex,
        "Error handling PaymentCompletedEvent for Event {EventId}, Registration {RegistrationId}",
        domainEvent.EventId, domainEvent.RegistrationId);
}

// SHOULD BE - More diagnostic information
catch (Exception ex)
{
    _logger.LogError(ex,
        "[CRITICAL] PaymentCompletedEventHandler failed for Event {EventId}, Registration {RegistrationId}. " +
        "ExceptionType: {ExceptionType}, Message: {Message}, InnerException: {InnerException}, StackTrace: {StackTrace}. " +
        "Event Details: ContactEmail={ContactEmail}, AmountPaid={AmountPaid}, PaymentIntentId={PaymentIntentId}",
        domainEvent.EventId, domainEvent.RegistrationId,
        ex.GetType().FullName, ex.Message, ex.InnerException?.Message ?? "None", ex.StackTrace,
        domainEvent.ContactEmail, domainEvent.AmountPaid, domainEvent.PaymentIntentId);
}
```

---

## 5. AzureEmailService - Template Rendering & Email Sending

### File: `AzureEmailService.cs`

#### Current State: **GOOD** with minor gaps

**RenderTemplateAsync (Lines 622-670)**:
- ✅ Line 629-631: Template rendering called
- ✅ Line 637: Template not found
- ✅ Line 643: Template not active
- ✅ Line 652-654: Template rendered successfully
- ✅ Line 667: Render failure
- ❌ **Missing: Parameter validation logging**
- ❌ **Missing: Template content length logging**

**SendEmailAsync (Lines 56-108)**:
- ✅ Line 60-61: Attempting to send
- ✅ Line 67: Validation failed
- ✅ Line 89-94: Send failed - mark as failed
- ✅ Line 98-99: Send succeeded - mark as sent
- ✅ Line 101: Success log
- ✅ Line 106: Generic failure
- ❌ **Missing: Pre-validation logging of email details**

**SendViaAzureAsync (Lines 447-545)**:
- ✅ Line 451: Azure client not configured
- ✅ Line 459: Sender address not configured
- ✅ Line 514: Sending via Azure
- ✅ Line 521-525: Success
- ✅ Line 529-531: Failed status
- ✅ Line 534-538: RequestFailedException
- ✅ Line 542: Generic exception
- ❌ **Missing: Attachment details logging**

**CreateDomainEmailMessage (Lines 383-445)**:
- ✅ Line 388-389: Creating domain email with FROM/TO
- ✅ Line 394: FromEmail validation failed
- ✅ Line 401: ToEmail validation failed
- ✅ Line 405: About to create EmailSubject
- ✅ Line 409: EmailSubject validation failed
- ❌ **Missing: Success confirmation after domain entity created**

#### Recommendations

**Add to RenderTemplateAsync**:
```csharp
// After line 634 (template found)
_logger.LogInformation(
    "[EmailTemplate-1] Template '{TemplateName}' loaded from database. IsActive: {IsActive}, HasHtml: {HasHtml}, HasText: {HasText}",
    templateName, template.IsActive, !string.IsNullOrEmpty(template.HtmlTemplate), !string.IsNullOrEmpty(template.TextTemplate));

// After line 650 (rendering complete)
_logger.LogInformation(
    "[EmailTemplate-2] Template rendered. SubjectLength: {SubjectLen}, HtmlLength: {HtmlLen}, TextLength: {TextLen}, ParameterCount: {ParamCount}",
    subject.Length, htmlBody.Length, textBody.Length, parameters.Count);
```

**Add to SendEmailAsync**:
```csharp
// At start of method (line 59)
_logger.LogInformation(
    "[EmailSend-1] SendEmailAsync called. ToEmail: {ToEmail}, Subject: '{Subject}', Provider: {Provider}, HasAttachments: {HasAttachments}",
    emailMessage.ToEmail, emailMessage.Subject, _emailSettings.Provider, emailMessage.Attachments?.Any() ?? false);
```

**Add to CreateDomainEmailMessage**:
```csharp
// After line 437 (successful save)
_logger.LogInformation(
    "[EmailDomain-1] Domain email message created and saved. EmailId: {EmailId}, Status: {Status}",
    domainEmail.Id, domainEmail.Status);
```

---

## 6. TicketService - Ticket Generation

### File: `TicketService.cs`

#### Current State: **GOOD** with minor gaps

**GenerateTicketAsync (Lines 47-191)**:
- ✅ Line 54-55: Generating ticket
- ✅ Line 61: Ticket already exists
- ✅ Line 75: Event not found
- ✅ Line 82-84: Registration not found in event, loading directly
- ✅ Line 89: Registration not found
- ✅ Line 153-154: PDF generation failed
- ✅ Line 174-175: Ticket generated successfully
- ✅ Line 187-188: Exception with full context
- ❌ **Missing: QR code generation logging**
- ❌ **Missing: PDF upload logging is internal only (line 364)**

#### Recommendations

```csharp
// Add after line 120 (QR code generation)
_logger.LogInformation(
    "[Ticket-1] QR code generated for ticket {TicketCode}. QrDataLength: {DataLength}",
    ticket.TicketCode, qrCodeBase64.Length);

// Add after line 148 (before PDF generation)
_logger.LogInformation(
    "[Ticket-2] About to generate PDF for ticket {TicketCode}. EventTitle: '{EventTitle}', AttendeeCount: {Count}",
    ticket.TicketCode, @event.Title.Value, registration.GetAttendeeCount());

// Make line 364 more visible
_logger.LogInformation(
    "[Ticket-3] Uploaded PDF for ticket {TicketCode} to blob storage. URL: {BlobUrl}",
    ticketCode, blobClient.Uri.ToString());
```

---

## 7. Overall Architecture Issues

### Issue 1: No Correlation ID System
**Severity**: P0
**Impact**: Cannot trace a single payment through the entire system

**Recommendation**:
```csharp
// Add to PaymentsController webhook
var correlationId = Guid.NewGuid();
using var scope = _logger.BeginScope(new Dictionary<string, object>
{
    ["CorrelationId"] = correlationId,
    ["StripeEventId"] = stripeEvent.Id,
    ["RegistrationId"] = registrationId,
    ["EventId"] = eventId
});

// All subsequent logs will include correlation ID
_logger.LogInformation("Processing checkout session");
```

### Issue 2: No End-to-End Transaction Logging
**Severity**: P1
**Impact**: Cannot confirm complete flow execution

**Recommendation**:
Create a `PaymentFlowTracker` service that logs checkpoints:
```csharp
public interface IPaymentFlowTracker
{
    Task LogCheckpoint(Guid correlationId, string checkpoint, Dictionary<string, object> data);
}

// Checkpoints:
// 1. webhook_received
// 2. payment_completed_domain_event
// 3. handler_started
// 4. template_rendered
// 5. ticket_generated
// 6. email_sent
// 7. flow_complete
```

### Issue 3: MediatR Handler Registration Verification
**Severity**: P1
**Impact**: No way to verify handler is registered

**Recommendation**:
```csharp
// Add diagnostic endpoint
[HttpGet("diagnostics/handlers")]
public IActionResult GetRegisteredHandlers()
{
    var handlers = typeof(PaymentCompletedEventHandler).Assembly
        .GetTypes()
        .Where(t => t.GetInterfaces().Any(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(INotificationHandler<>)))
        .Select(t => new
        {
            HandlerType = t.FullName,
            EventTypes = t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
                .Select(i => i.GetGenericArguments()[0].FullName)
        });

    return Ok(handlers);
}
```

---

## Summary of Issues by Priority

### P0 - Critical (Silent Failures)
1. **PaymentCompletedEventHandler** - No internal operation logging (template, email, ticket steps)
2. **AppDbContext.CommitAsync** - MediatR.Publish() not wrapped in try-catch
3. **No Correlation ID system** - Cannot trace single payment through system

### P1 - High (Hard to Debug)
1. **PaymentCompletedEventHandler** - Generic exception handler lacks detail
2. **No handler registration verification** - Cannot confirm handler is registered
3. **No end-to-end transaction logging** - Cannot confirm complete flow

### P2 - Medium (Nice to Have)
1. **AzureEmailService** - Missing parameter validation logging
2. **TicketService** - Missing QR code and PDF upload visibility
3. **PaymentsController** - Missing success path logging

---

## Recommended Implementation Order

### Phase 1: Immediate Fixes (P0 - Today)
1. Add comprehensive logging to `PaymentCompletedEventHandler.Handle()`
   - Template rendering (before/after)
   - Email construction
   - Email sending (before/after)
   - Ticket generation steps
2. Wrap `_publisher.Publish()` in try-catch in `AppDbContext.CommitAsync()`
3. Add correlation ID to webhook processing

### Phase 2: Critical Diagnostics (P1 - This Week)
1. Improve exception handler in `PaymentCompletedEventHandler`
2. Add handler registration diagnostics endpoint
3. Create payment flow checkpoint logging

### Phase 3: Enhanced Observability (P2 - Next Sprint)
1. Add parameter validation logging to `AzureEmailService`
2. Enhance `TicketService` logging
3. Add success path logging to all controllers

---

## Code Diff Summary

### Files Requiring Changes:
1. `PaymentCompletedEventHandler.cs` - **40 new log statements** (Lines 152-235)
2. `AppDbContext.cs` - **Try-catch wrapper** (Lines 396-423)
3. `PaymentsController.cs` - **Correlation ID + 3 logs** (Lines 229-285)
4. `AzureEmailService.cs` - **6 new log statements** (Lines 59, 634, 650, 437)
5. `TicketService.cs` - **3 new log statements** (Lines 120, 148, 364)

### Estimated Impact:
- **Logging Overhead**: ~5-10ms per payment (negligible)
- **Log Volume**: ~50 log entries per payment (manageable with log levels)
- **Developer Time**: 2-4 hours for all P0 fixes
- **Testing Time**: 1-2 hours for end-to-end validation

---

## Success Metrics

After implementing these fixes, you should be able to:

1. ✅ See EXACTLY where PaymentCompletedEventHandler fails (or if it executes at all)
2. ✅ Trace a single payment from webhook → email sent with correlation ID
3. ✅ Never see "silent failures" - every error logged with full context
4. ✅ Diagnose issues from logs alone without database queries
5. ✅ Identify which step in the flow failed within 30 seconds of looking at logs

---

## Example: Complete Log Trace (After Fixes)

```
[INFO] Webhook endpoint reached - StripeEventId: evt_123, CorrelationId: abc-def-123
[INFO] Processing checkout.session.completed - SessionId: cs_test_456
[INFO] [Phase 6A.50] About to call CommitAsync - Registration abc-123 has 1 domain events
[INFO] [DIAG-17] About to dispatch domain event: PaymentCompletedEvent
[INFO] [DIAG-18] Publishing notification for: PaymentCompletedEvent
[INFO] [Phase 6A.24] ✅ PaymentCompletedEventHandler INVOKED - Event abc, Registration xyz
[INFO] [PaymentTicket-1] About to generate ticket for Registration xyz
[INFO] [Ticket-1] QR code generated for ticket TKT-123456
[INFO] [Ticket-2] About to generate PDF for ticket TKT-123456
[INFO] [Ticket-3] Uploaded PDF to blob storage
[INFO] [PaymentTicket-2] Ticket generated successfully: TicketCode=TKT-123456
[INFO] [PaymentTicket-3] About to retrieve ticket PDF
[INFO] [PaymentTicket-4] Ticket PDF retrieved successfully, size: 45678 bytes
[INFO] [PaymentEmail-1] About to render template 'ticket-confirmation'
[INFO] [EmailTemplate-1] Template loaded from database. IsActive: true
[INFO] [EmailTemplate-2] Template rendered. SubjectLength: 45, HtmlLength: 3456
[INFO] [PaymentEmail-2] Successfully rendered template
[INFO] [PaymentEmail-3] Building email message for user@example.com
[INFO] [PaymentEmail-4] Email has PDF attachment: Size=45678 bytes
[INFO] [PaymentEmail-5] About to send payment confirmation email
[INFO] [EmailSend-1] SendEmailAsync called. Provider: Azure
[INFO] [EmailDomain-1] Domain email message created and saved
[INFO] Sending email via Azure Communication Services
[INFO] Azure email sent successfully
[INFO] [PaymentEmail-6] Email send result: Success=true
[INFO] Payment confirmation email sent successfully
[INFO] [Phase 6A.24] Successfully dispatched domain event: PaymentCompletedEvent
```

**With this log trace, you can identify EXACTLY where any failure occurs.**
