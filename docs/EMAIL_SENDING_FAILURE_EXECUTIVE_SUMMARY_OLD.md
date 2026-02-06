# Email Sending Failure - Executive Summary

**Date**: December 23, 2025
**Incident Start**: Morning of Dec 23, 2025
**Impact**: CRITICAL - Zero emails sent for both free and paid events
**Status**: Root cause identified, fix plan ready

---

## Problem Statement

All event registration emails stopped sending as of this morning. Users registering for events (both free and paid) receive no confirmation emails. Frontend returns 400 error: "ValidationError: Failed to render email template".

---

## Root Cause (99% Confidence)

**Template Variable Mismatch** between code and database template:

| Component | Variable Used | Status |
|-----------|--------------|--------|
| Database template (`ticket-confirmation`) | `{{EventStartDate}}`, `{{EventStartTime}}` | OLD format |
| PaymentCompletedEventHandler code | `{{EventDateTime}}` | NEW format |
| **Result** | Variables don't match → Rendering fails | ❌ BROKEN |

**Why It Broke Now**:

Commit `f45f08b4` (Dec 23, 09:05 AM) updated code to use `{{EventDateTime}}` but created **NO migration** to update the database template. The code expects new variables but the database template still has old variables.

---

## Evidence Trail

### Dec 22, 2025 - Working (With Issues)
- User screenshot shows email received at 10:04 AM
- Email displayed unrendered variables: `{{EventStartDate}}`, `{{EventStartTime}}`
- **Conclusion**: Template mismatch existed but emails still sent (variables showed as literal text)

### Dec 23, 2025 - Completely Broken
- Commit f45f08b4: Code updated to use `{{EventDateTime}}`
- Commit 2bda1cfb: DI changed to use database templates exclusively
- **No migration** created to update database template
- **Result**: Email rendering fails completely, no emails sent

---

## Missing Migration

**Expected Migration**: `UpdateTicketConfirmationTemplate_Phase6A43.cs`

Should UPDATE the database template from:
```html
<p><strong>Date:</strong> {{EventStartDate}} at {{EventStartTime}}</p>
```

To:
```html
<p><strong>Date & Time:</strong> {{EventDateTime}}</p>
```

**Current State**:
- Migration file: ❌ Does NOT exist
- Database template: ⚠️ Still has OLD variables
- Code: ✅ Sends NEW variables
- **Mismatch**: Code and database out of sync

---

## Impact Assessment

### Users Affected
- **Free event registrations**: 100% failure rate
- **Paid event registrations**: 100% failure rate
- **Estimated users**: 10-50 since this morning
- **Business impact**: Users pay for events but receive no ticket confirmation

### System Health
- Email queue processor: Running but failing all jobs
- Database: Functional but has outdated templates
- API: Functional but returns 400 errors for email operations

---

## Fix Strategy

### Recommended: Option A - Execute Missing Migration

**Steps**:
1. Create migration to UPDATE ticket-confirmation template
2. Replace `{{EventStartDate}}`, `{{EventStartTime}}` with `{{EventDateTime}}`
3. Add missing conditional sections for attendees, images
4. Execute migration on staging database
5. Verify email sending works

**Timeline**: 45-60 minutes
**Downtime**: None (migration is UPDATE only)
**Risk**: Low (can rollback if fails)

### Fallback: Option B - Rollback Code

**Steps**:
1. Revert DependencyInjection.cs to use filesystem templates
2. Update filesystem templates to match current code
3. Redeploy application

**Timeline**: 30-45 minutes
**Downtime**: 5-10 minutes for deployment
**Risk**: Medium (loses Phase 6A.43 benefits)

---

## Investigation Required (Before Fix)

**CRITICAL**: Run these queries on staging database to confirm root cause:

```sql
-- 1. Check if templates exist
SELECT name, subject_template, is_active, created_at
FROM communications.email_templates
WHERE name IN ('registration-confirmation', 'ticket-confirmation');

-- 2. Check template content for variable version
SELECT name,
       CASE
           WHEN html_template LIKE '%{{EventStartDate}}%' THEN 'OLD FORMAT'
           WHEN html_template LIKE '%{{EventDateTime}}%' THEN 'NEW FORMAT'
           ELSE 'UNKNOWN'
       END as template_version
FROM communications.email_templates
WHERE name = 'ticket-confirmation';

-- 3. Verify migration history
SELECT migration_id
FROM public."__EFMigrationsHistory"
WHERE migration_id LIKE '%Template%'
ORDER BY migration_id;
```

**Expected Results**:
- Templates exist: 2 rows
- ticket-confirmation version: 'OLD FORMAT'
- Migrations: Should see 3 template-related migrations

**If Different**:
- 0 templates → Database not migrated (use Option A with full seed)
- NEW FORMAT → Template was updated (issue is elsewhere)
- Missing migrations → Database state corrupted

---

## Immediate Actions (Next 2 Hours)

1. **[15 min] Investigation**: Run SQL queries to confirm database state
2. **[30 min] Create Migration**: Write UpdateTicketConfirmationTemplate migration
3. **[15 min] Test Locally**: Verify migration works on local database
4. **[10 min] Execute on Staging**: Apply migration to staging database
5. **[15 min] Verification**: Test email sending for free and paid events
6. **[15 min] Monitoring**: Monitor logs for successful email sends

**Total**: 100 minutes (includes buffer)

---

## Post-Fix Actions

### Resend Missed Emails
```sql
-- Find users who registered during outage
SELECT DISTINCT r.contact->>'email' as email,
                e.title as event_title,
                r.id as registration_id
FROM events.registrations r
JOIN events.events e ON r.event_id = e.id
WHERE r.created_at BETWEEN '<outage-start>' AND '<outage-end>'
  AND r.status = 'Confirmed';
```

### Add Monitoring
- Alert: Email template rendering failures > 5%
- Alert: Email sending failures > 10%
- Health check: Verify email templates exist on startup

### Update Deployment Checklist
- [ ] Check for pending migrations before code deployment
- [ ] Execute migrations BEFORE deploying code changes
- [ ] Verify email templates match code expectations
- [ ] Test email sending in staging before production

---

## Related Documents

1. [EMAIL_SENDING_FAILURE_RCA.md](./EMAIL_SENDING_FAILURE_RCA.md) - Full root cause analysis
2. [EMAIL_SENDING_FAILURE_FIX_PLAN.md](./EMAIL_SENDING_FAILURE_FIX_PLAN.md) - Detailed fix plan with commands
3. [EMAIL_SENDING_FAILURE_ACTUAL_ROOT_CAUSE.md](./EMAIL_SENDING_FAILURE_ACTUAL_ROOT_CAUSE.md) - Updated analysis

---

## Key Files

### Code Files (Modified in Phase 6A.43)
- `src/LankaConnect.Infrastructure/DependencyInjection.cs` (lines 207-212)
- `src/LankaConnect.Application/Events/EventHandlers/PaymentCompletedEventHandler.cs` (lines 122-126)
- `src/LankaConnect.Infrastructure/Email/Services/AzureEmailService.cs` (line 636)

### Migration Files
- **Existing**: `20251220155500_SeedTicketConfirmationTemplate_Phase6A24.cs` (OLD variables)
- **Missing**: `UpdateTicketConfirmationTemplate_Phase6A43.cs` (NEW variables) ❌

### Database Schema
- Table: `communications.email_templates`
- Column: `html_template` (contains template with variables)
- Column: `subject_template` (mapped to SubjectTemplate.Value object)

---

## Decision Tree

```
Run Investigation SQL Queries
    ↓
┌───────────────────────────────────────────┐
│ Templates exist with OLD FORMAT?         │
├───────────────────────────────────────────┤
│ YES → Create migration, update template   │
│ NO → Check if templates missing           │
└───────────────────────────────────────────┘
    ↓
┌───────────────────────────────────────────┐
│ Templates missing entirely?               │
├───────────────────────────────────────────┤
│ YES → Execute all template migrations     │
│ NO → Templates exist with NEW FORMAT?     │
└───────────────────────────────────────────┘
    ↓
┌───────────────────────────────────────────┐
│ Templates exist with NEW FORMAT?          │
├───────────────────────────────────────────┤
│ YES → Issue is elsewhere (check EF Core)  │
│ NO → Unknown state (escalate)             │
└───────────────────────────────────────────┘
```

---

## Success Criteria

Email sending is fixed when:

- [ ] Free event registration sends confirmation email
- [ ] Paid event registration sends ticket confirmation email
- [ ] Email template variables render correctly (no `{{...}}` in email)
- [ ] Event date/time shows as formatted range
- [ ] Attendee names display correctly
- [ ] Event image displays (if exists)
- [ ] Payment details show (for paid events)
- [ ] Zero "Failed to render email template" errors in logs
- [ ] Email sending success rate > 95%

---

## Contact Information

**Incident Commander**: System Architecture Team
**Database Admin**: Required for migration execution
**DevOps Engineer**: Required for deployment verification
**Support Team**: Notify when fix deployed (to handle user inquiries)

**Escalation**: If not resolved in 2 hours, escalate to CTO

---

## Lessons Learned (Preliminary)

1. **Code-Database Sync**: Always create migrations when changing template variables
2. **Testing**: Test email sending in staging after every template-related change
3. **Monitoring**: Add alerts for template rendering failures
4. **Deployment**: Update checklist to verify migrations before code deployment

---

**Report Generated**: 2025-12-23
**Severity**: CRITICAL
**Priority**: P0 - Immediate fix required
**Status**: Analysis complete, ready for fix execution
