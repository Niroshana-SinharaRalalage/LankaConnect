# Critical Issues and Recommendations - Phase 6A.X
## Date: 2026-01-26

---

## 🚨 CRITICAL ISSUE #1: Ticket Generation Rollback on Email Failure

### Problem
**Tickets are NOT persisted to the database when email sending fails**, even though ticket generation succeeds. This violates the principle of separation of concerns.

### Root Cause
**File**: `src/LankaConnect.Infrastructure/Services/Tickets/TicketService.cs`
**Line**: 172

```csharp
// Save ticket
await _ticketRepository.AddAsync(ticket, cancellationToken);  // Line 172
// ⚠️ NO CommitAsync() call here!
```

The ticket is added to EF Core's change tracker but **never committed to the database**. When email sending fails later:
1. ResendAttendeeConfirmationCommandHandler returns failure
2. Any outer transaction wrapper rolls back uncommitted changes
3. Ticket disappears from change tracker
4. **Database remains empty**

### Evidence
```
Test Results:
- ✅ GenerateTicketAsync() called successfully
- ✅ Ticket entity created in memory
- ❌ Email sending failed (Azure suppression)
- ❌ Handler returned 400 Bad Request
- ❌ Database query: 0 tickets found
```

### Impact
- **HIGH**: Tickets cannot be generated via resend endpoint
- **BLOCKER**: Feature cannot be fully tested or demonstrated
- **ARCHITECTURAL**: Violates single responsibility principle

---

## 🔧 RECOMMENDED FIX #1

### Solution: Commit Ticket Immediately After Generation

**File to Modify**: `src/LankaConnect.Infrastructure/Services/Tickets/TicketService.cs`

**Add after line 172:**

```csharp
// Save ticket
await _ticketRepository.AddAsync(ticket, cancellationToken);

// CRITICAL: Commit immediately to persist ticket independently of email sending
await _unitOfWork.CommitAsync(cancellationToken);  // ← ADD THIS LINE

_logger.LogInformation("Successfully generated ticket {TicketCode} for Registration {RegistrationId}",
    ticket.TicketCode, registrationId);
```

### Why This Works
1. **Separation of Concerns**: Ticket persistence is independent of email sending
2. **Resilience**: Even if email fails, ticket is saved
3. **Idempotency**: Subsequent calls will find existing ticket (line 58-69)
4. **No Breaking Changes**: Maintains existing API contract

### Alternative: Use Separate Transaction Scope
If CommitAsync causes issues:

```csharp
// Wrap ticket generation in its own transaction
using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
try
{
    await _ticketRepository.AddAsync(ticket, cancellationToken);
    await _context.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
}
catch
{
    await transaction.RollbackAsync(cancellationToken);
    throw;
}
```

---

## ⚠️ CRITICAL ISSUE #2: Azure Communication Services Email Suppression

### Problem
The test email address `niroshhh@gmail.com` is on Azure Communication Services' **suppression list**, preventing ANY emails from being sent to this address.

### Error Details
```
ErrorCode: EmailDroppedAllRecipientsSuppressed
Status: 200 (OK)
Message: Message dropped because all recipients were suppressed
```

### Root Causes (Likely)
1. **Hard Bounces**: Previous emails bounced back permanently
2. **Spam Reports**: Recipient marked emails as spam
3. **Unsubscribe**: Explicit unsubscribe request
4. **Too Many Emails**: Excessive testing/sending volume

### Impact
- **MEDIUM**: Cannot test email sending features
- **BLOCKER**: Cannot demonstrate end-to-end flow
- **PRODUCTION RISK**: May affect other legitimate email addresses

---

## 🔧 RECOMMENDED FIX #2A: Check Azure Suppression List

### Steps to Investigate

#### Option 1: Azure Portal
1. Navigate to Azure Portal → Communication Services
2. Select your Communication Service resource
3. Go to **Suppression Lists** (under Settings)
4. Search for `niroshhh@gmail.com`
5. Check suppression reason and date
6. **Remove from list** if appropriate for testing

#### Option 2: Azure CLI
```bash
# List suppression entries
az communication email suppression-list show \
  --email-service-name <your-service> \
  --resource-group lankaconnect-staging

# Remove specific address (if supported)
az communication email suppression-list remove \
  --email-service-name <your-service> \
  --resource-group lankaconnect-staging \
  --email "niroshhh@gmail.com"
```

#### Option 3: REST API
```bash
GET https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/lankaconnect-staging/providers/Microsoft.Communication/emailServices/{emailServiceName}/domains/{domainName}/suppressionLists
```

---

## 🔧 RECOMMENDED FIX #2B: Use Alternative Test Email

### Quick Workaround (Immediate)
Use a different email address for testing:

```python
# In test scripts
LOGIN_EMAIL = "test.lankaconnect+1@gmail.com"  # Use Gmail + addressing
# or
LOGIN_EMAIL = "organizer@lankaconnect-test.com"  # Use custom domain
```

### Gmail + Addressing Trick
Gmail ignores everything after `+`, so these all go to the same inbox:
- `niroshhh+test1@gmail.com`
- `niroshhh+test2@gmail.com`
- `niroshhh+staging@gmail.com`

But Azure Communication Services treats them as different addresses, avoiding suppression.

---

## 🔧 RECOMMENDED FIX #2C: Configure Email Service Properly

### Best Practices for Production

#### 1. Implement Proper Unsubscribe Links
Ensure all emails have unsubscribe links that update suppression list correctly.

#### 2. Monitor Bounce Rates
Set up alerts for high bounce rates:
- Hard bounces > 5%
- Soft bounces > 10%

#### 3. Warm Up Email Domain
For new domains/senders:
- Start with low volume (< 100/day)
- Gradually increase over 2-4 weeks
- Build sender reputation

#### 4. Use Engagement-Based Sending
Only send to engaged users:
- Opened email in last 90 days
- Clicked link in last 180 days
- Registered/attended in last 6 months

#### 5. Separate Transactional vs Marketing
- **Transactional**: Registration confirmations, tickets (high priority)
- **Marketing**: Newsletters, promotions (lower priority)
- Use different sender addresses/domains

---

## 📋 PRIORITY ACTION PLAN

### 🔴 CRITICAL (Do Immediately)

1. **Fix Ticket Generation Rollback** (1 hour)
   - Add `CommitAsync()` after ticket creation
   - Test with failing email scenario
   - Verify ticket persists to database

2. **Investigate Azure Suppression List** (30 minutes)
   - Check Azure Portal for suppression details
   - Document suppression reason
   - Remove test email if appropriate

3. **Test with Alternative Email** (15 minutes)
   - Use `niroshhh+staging@gmail.com`
   - Verify end-to-end flow works
   - Confirm ticket + email both succeed

### 🟡 HIGH (Do Today)

4. **Update ResendAttendeeConfirmationCommandHandler** (Optional)
   - Add try-catch around email sending
   - Log email failures but don't fail entire operation
   - Return success if ticket generated, even if email fails

5. **Document Email Suppression Policy** (30 minutes)
   - Create production email guidelines
   - Document monitoring procedures
   - Define suppression list management process

### 🟢 MEDIUM (Do This Week)

6. **Implement Email Retry Logic** (2 hours)
   - Queue failed emails for retry
   - Exponential backoff
   - Max 3 attempts

7. **Add Health Check for Email Service** (1 hour)
   - Test email sending on startup
   - Alert if service unavailable
   - Fallback to queue-only mode

---

## 📊 VERIFICATION CHECKLIST

### After Fix #1 (Ticket Generation)
- [ ] Ticket created when email succeeds
- [ ] Ticket created when email fails
- [ ] Ticket NOT duplicated on retry
- [ ] Database query returns ticket
- [ ] Attendees API shows ticket data
- [ ] QR code displayed correctly

### After Fix #2 (Email Suppression)
- [ ] Email sent successfully to test address
- [ ] Email received in inbox
- [ ] Ticket PDF attached correctly
- [ ] Email content renders properly
- [ ] Unsubscribe link works
- [ ] Tracking pixels fire (if implemented)

---

## 📂 FILES TO MODIFY

### Backend Fix #1
1. `src/LankaConnect.Infrastructure/Services/Tickets/TicketService.cs` (Line 172 - add CommitAsync)
2. `tests/LankaConnect.Infrastructure.Tests/Services/TicketServiceTests.cs` (Add test for failure scenario)

### Backend Fix #2 (Optional Enhancement)
3. `src/LankaConnect.Application/Events/Commands/ResendAttendeeConfirmation/ResendAttendeeConfirmationCommandHandler.cs` (Wrap email sending in try-catch)

### Documentation
4. `docs/EMAIL_SERVICE_GUIDELINES.md` (NEW - production guidelines)
5. `docs/PROGRESS_TRACKER.md` (Update with findings)
6. `docs/STREAMLINED_ACTION_PLAN.md` (Update status)

---

## 🎯 EXPECTED OUTCOMES

### After Both Fixes

#### Successful Scenario
```
User clicks "Resend Confirmation"
→ Backend: Ticket generated → Committed to DB → Email sent → Success
→ Frontend: Success message shown
→ Database: Ticket row exists
→ User: Receives email with PDF
```

#### Email Failure Scenario (With Fix)
```
User clicks "Resend Confirmation"
→ Backend: Ticket generated → Committed to DB → Email fails → Success
→ Frontend: "Confirmation prepared, email may be delayed" message
→ Database: Ticket row exists
→ User: Can view ticket in QR code modal
→ Background: Email queued for retry
```

#### Current Behavior (Without Fix)
```
User clicks "Resend Confirmation"
→ Backend: Ticket generated → Email fails → Rollback → Failure
→ Frontend: Error message shown
→ Database: NO ticket row
→ User: Frustrated, tries again, same result
```

---

## 💡 LESSONS LEARNED

### 1. Always Commit Critical Data Separately
Tickets are valuable business data and should be persisted in their own transaction, independent of side effects like email sending.

### 2. Test Failure Scenarios
We tested the success path but not the failure path. Always test:
- Network failures
- External service failures
- Partial failures
- Rollback behavior

### 3. Separate Read from Write Operations
Ticket generation (write) should not be in the same transaction as email sending (side effect).

### 4. Monitor External Service Dependencies
Azure Communication Services has quotas, limits, and suppression lists. These should be monitored and alerting configured.

---

## 📞 STAKEHOLDER COMMUNICATION

### For Product Owner
> "We've successfully deployed the resend confirmation feature to staging. However, testing revealed two issues: tickets aren't persisting when emails fail (architectural fix needed), and our test email is suppressed by Azure (operational issue). Both have clear solutions with 1-hour fixes. Feature will be fully functional after fixes are applied."

### For QA Team
> "Resend confirmation endpoint is deployed and functional. To test, use an email address OTHER than niroshhh@gmail.com (it's suppressed). After we apply the ticket persistence fix, you'll be able to verify tickets are created even when emails fail."

### For DevOps Team
> "Please investigate Azure Communication Services suppression list for niroshhh@gmail.com. We need to either remove it or use alternative test emails. Also, let's set up alerts for suppression list growth and bounce rates."

---

**Status**: DOCUMENTED - Awaiting implementation approval
**Owner**: Niroshana Sinharage
**Date**: 2026-01-26
**Priority**: CRITICAL (Blocks feature completion)
