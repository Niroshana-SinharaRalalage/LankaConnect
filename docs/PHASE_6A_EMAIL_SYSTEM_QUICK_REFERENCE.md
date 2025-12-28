# Phase 6A Email System - Quick Reference Guide
**Created**: 2025-12-27
**For**: Fast session startup and continuity

---

## What's Been Done (Phase 6A.54 - Complete)

✅ **4 Email Templates Created** with professional HTML layout:
1. `member-email-verification` - Email verification for new signups
2. `signup-commitment-confirmation` - User commits to bringing item to event
3. `registration-cancellation` - User cancels event registration
4. `organizer-custom-message` - Organizer sends custom email to attendees

✅ **Branding**: All templates use orange/rose gradient (#fb923c → #f43f5e)
✅ **Documentation**: EMAIL_TEMPLATE_VARIABLES.md updated with all variables
✅ **Database**: Templates seeded in communications.email_templates table

---

## What Needs to Be Done (5 Phases Remaining)

### NEXT: Phase 6A.57 - Event Reminder Improvements (3-4 hours) - USER URGENT

**Current Problem**:
- Ugly plain text HTML in reminders
- Only sends 1 reminder (24 hours before)

**User Wants**:
- Professional HTML matching other templates
- Send 3 reminders: 7 days, 2 days, 1 day before event

**Implementation**:
1. Create `event-reminder` template (orange/rose gradient)
2. Update EventReminderJob.cs to send 3 reminder types
3. Use time windows: 167-169h (7d), 47-49h (2d), 23-25h (1d)
4. Replace inline HTML with templated emails
5. Add comprehensive logging [Phase 6A.57]

**Files to Modify**:
- Migration: `YYYYMMDD_Phase6A57_AddEventReminderTemplate.cs`
- Job: `src/LankaConnect.Application/Events/BackgroundJobs/EventReminderJob.cs`
- Constants: `src/LankaConnect.Infrastructure/Email/Configuration/EmailTemplateNames.cs`
- Tests: `tests/LankaConnect.Application.Tests/Events/BackgroundJobs/EventReminderJobTests.cs`

---

### Phase 6A.51 - Signup Commitment Emails (3-4 hours)

**Template**: ✅ Already created
**Backend**: ❌ Not started

**Tasks**:
1. Create domain event: `SignupCommitmentConfirmedEvent`
2. Create event handler: `SignupCommitmentConfirmedEventHandler`
3. Trigger from SignUpItem entity when user commits
4. Tests (unit + integration)

---

### Phase 6A.52 - Registration Cancellation Emails (3-4 hours)

**Template**: ✅ Already created
**Backend**: ❌ Not started

**Tasks**:
1. Create domain event: `RegistrationCancelledEvent` (include PaymentStatus)
2. Create event handler: `RegistrationCancelledEventHandler`
3. Update Registration.Cancel() to raise event
4. Tests (unit + integration)

---

### Phase 6A.53 - Member Email Verification (7-9 hours)

**Template**: ✅ Already created
**Backend**: ❌ Not started (COMPLEX - security critical)

**Tasks**:
1. Database migration (3 columns + 2 indexes)
2. GUID token generation (NOT Base64)
3. Token expiry (24 hours)
4. Rate limiting (3 emails/hour/user)
5. Resend with 1-hour cooldown
6. Frontend verification page
7. Tests (unit + integration + staging)

---

### Phase 6A.50 - Manual Organizer Emails (11-13 hours)

**Template**: ✅ Already created
**Backend**: ❌ Not started (MOST COMPLEX)

**Tasks**:
1. Install HtmlSanitizer NuGet
2. Create command + handler
3. HTML sanitization
4. Rate limiting (5 emails/event/day)
5. Recipient resolution (All/Checked-In/Pending)
6. Repository methods (GetOrganizerEmailCountTodayAsync, GetEmailRecipientsAsync)
7. Frontend SendEmailModal with rich text editor
8. Tests (unit + integration + staging)

---

## Implementation Order (WHY)

1. **6A.57 First** - User urgent request, quick win, high visibility
2. **6A.51 & 6A.52** - Simple domain events, build momentum
3. **6A.53** - Security-critical, needs focused attention
4. **6A.50 Last** - Most complex, save for when patterns established

**Total Time**: 28-34 hours over 3 weeks

---

## Key Architectural Decisions

### Decision 1: Event Reminder Template
✅ **Single template** with {{DaysUntilEvent}} variable (NOT 3 separate templates)
- Easier to maintain
- Consistent branding
- DRY principle

### Decision 2: Event Reminder Scheduling
✅ **Multiple time windows** (NOT database tracking)
- 167-169h window = 7-day reminder
- 47-49h window = 2-day reminder
- 23-25h window = 1-day reminder
- No schema changes
- Uses existing Hangfire hourly job

### Decision 3: Email Attachments
✅ **No PDF attachments** in reminders (just links)
- Faster email processing
- Lower SendGrid costs
- Users already have ticket from confirmation email

---

## Critical Requirements (ALL PHASES)

### Security
- Email injection prevention (sanitize subjects, HTML)
- GUID tokens (NOT Base64)
- Rate limiting (per user, per event)
- Fail-silent pattern (email failures don't block operations)

### Quality
- Test coverage ≥90%
- Build: 0 errors, 0 warnings
- Logging with phase numbers: [Phase 6A.XX]
- Documentation updated with code

---

## Session Startup Checklist

Starting a new session? Follow these steps:

1. [ ] Read [PHASE_6A_EMAIL_SYSTEM_MASTER_PLAN.md](./PHASE_6A_EMAIL_SYSTEM_MASTER_PLAN.md)
2. [ ] Check [PHASE_6A_MASTER_INDEX.md](./PHASE_6A_MASTER_INDEX.md) for phase numbers
3. [ ] Review [PROGRESS_TRACKER.md](./PROGRESS_TRACKER.md) for last session status
4. [ ] Check this Quick Reference for current phase
5. [ ] Run `git status` to see uncommitted work
6. [ ] Check TodoWrite list for sub-tasks

---

## Files to Track

### Documentation (UPDATE AFTER EACH PHASE)
- `docs/PHASE_6A_EMAIL_SYSTEM_MASTER_PLAN.md` - Master plan
- `docs/PHASE_6A_MASTER_INDEX.md` - Phase registry
- `docs/PROGRESS_TRACKER.md` - Session log
- `docs/STREAMLINED_ACTION_PLAN.md` - Action items
- `docs/EMAIL_TEMPLATE_VARIABLES.md` - Template variables

### Code (MODIFIED DURING IMPLEMENTATION)
- `src/LankaConnect.Application/Events/BackgroundJobs/EventReminderJob.cs`
- `src/LankaConnect.Infrastructure/Email/Configuration/EmailTemplateNames.cs`
- `src/LankaConnect.Infrastructure/Data/Migrations/YYYYMMDD_Phase6A[X]_*.cs`
- `tests/LankaConnect.Application.Tests/Events/BackgroundJobs/EventReminderJobTests.cs`

---

## Common Patterns (REUSE ACROSS PHASES)

### Domain Event Pattern
```csharp
public sealed class [Feature]Event : IDomainEvent
{
    public Guid Id { get; }
    // ... properties
    public DateTimeOffset OccurredOn { get; }
}
```

### Event Handler Pattern
```csharp
public class [Feature]EventHandler : INotificationHandler<DomainEventNotification<[Feature]Event>>
{
    public async Task Handle(notification, cancellationToken)
    {
        try
        {
            var parameters = new Dictionary<string, object> { ... };
            var result = await _emailService.SendTemplatedEmailAsync(
                "template-name", toEmail, parameters, cancellationToken);

            if (result.IsFailure)
                _logger.LogError("Failed: {Errors}", string.Join(", ", result.Errors));
        }
        catch (Exception ex)
        {
            // FAIL-SILENT: Log but don't throw
            _logger.LogError(ex, "Error handling event");
        }
    }
}
```

### Migration Pattern
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.InsertData(
        table: "email_templates",
        schema: "communications",
        columns: new[] { "id", "name", "subject", "body_html", "body_text", ... },
        values: new object[] { Guid.NewGuid(), "template-name", "{{Subject}}", @"<html>...</html>", "Plain text...", ... }
    );
}
```

---

## Quick Commands

```bash
# Check git status
git status

# Run build
dotnet build

# Run tests
dotnet test

# Create migration
dotnet ef migrations add Phase6A[X]_[Description] --project src/LankaConnect.Infrastructure --startup-project src/LankaConnect.API --context AppDbContext

# Apply migration (local)
dotnet ef database update --project src/LankaConnect.Infrastructure --startup-project src/LankaConnect.API --context AppDbContext

# Commit work
git add .
git commit -m "feat(phase-6a[X]): [Description]"
git push origin develop
```

---

## Success Metrics

After each phase:
- [ ] Build: 0 errors, 0 warnings
- [ ] Tests: All passing, ≥90% coverage
- [ ] Email sent successfully in staging
- [ ] Documentation updated
- [ ] Phase summary created
- [ ] PHASE_6A_MASTER_INDEX.md updated
- [ ] PROGRESS_TRACKER.md updated

---

**Last Updated**: 2025-12-27
**Next Phase**: 6A.57 - Event Reminder Improvements
**Estimated Time**: 3-4 hours
