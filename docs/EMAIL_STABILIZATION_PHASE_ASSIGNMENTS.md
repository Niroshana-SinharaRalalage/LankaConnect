# Email System Stabilization - Phase Assignments
**Date**: 2026-01-12
**Reference**: [EMAIL_SYSTEM_STABILIZATION_PLAN.md](./EMAIL_SYSTEM_STABILIZATION_PLAN.md)

---

## Phase Number Assignments

### Critical Fixes (P0 - Week 1)

| Phase | Feature | Duration | Phase Number | Document |
|-------|---------|----------|--------------|----------|
| Phase 1 | URL Centralization (P0-BLOCKING) | 4-6 hours | **6A.70** | [Stabilization Plan Phase 1](./EMAIL_SYSTEM_STABILIZATION_PLAN.md#phase-1-foundation---url-centralization-p0) |
| Phase 2 | Event Reminder System Fix (P0-BROKEN) | 6-8 hours | **6A.71** | [Stabilization Plan Phase 2](./EMAIL_SYSTEM_STABILIZATION_PLAN.md#phase-2-event-reminder-system-fix-p0) |
| Phase 3 | Event Cancellation Recipient Fix (P0-INCOMPLETE) | 4-5 hours | **6A.72** | [Stabilization Plan Phase 3](./EMAIL_SYSTEM_STABILIZATION_PLAN.md#phase-3-event-cancellation-recipient-fix-p0) |

### High Priority (P1 - Week 2)

| Phase | Feature | Duration | Phase Number | Document |
|-------|---------|----------|--------------|----------|
| Phase 4 | Email Template Constants | 2 hours | **6A.73** | [Stabilization Plan Phase 4](./EMAIL_SYSTEM_STABILIZATION_PLAN.md#phase-4-email-template-constants-p2) |
| Phase 6 | Signup Commitment Emails | 4 hours | **6A.60** | [EMAIL_SYSTEM_REMAINING_WORK_PLAN.md](./EMAIL_SYSTEM_REMAINING_WORK_PLAN.md#phase-6a60-signup-commitment-emails) |

### Medium Priority (P2 - Week 3-4)

| Phase | Feature | Duration | Phase Number | Document |
|-------|---------|----------|--------------|----------|
| Phase 5 | Centralized Email Orchestration | 2 days | **6A.74** | [Stabilization Plan Phase 5](./EMAIL_SYSTEM_STABILIZATION_PLAN.md#phase-5-centralized-email-orchestration-p2) |
| Phase 7 | Manual Event Email Sending | 17 hours | **6A.61** | [EMAIL_SYSTEM_REMAINING_WORK_PLAN.md](./EMAIL_SYSTEM_REMAINING_WORK_PLAN.md#phase-6a61-manual-event-email-sending) |

### Low Priority (P3 - Ongoing)

| Phase | Feature | Duration | Phase Number | Document |
|-------|---------|----------|--------------|----------|
| Phase 8 | Observability & Monitoring | 1 day | **6A.75** | [Stabilization Plan Phase 8](./EMAIL_SYSTEM_STABILIZATION_PLAN.md#phase-8-observability--monitoring-p3) |

---

## Bug Fixes (Existing Work)

| Phase | Feature | Duration | Phase Number | Document |
|-------|---------|----------|--------------|----------|
| Bug Fix | Registration Cancellation Email Template Name | 15 minutes | **6A.62** | [EMAIL_SYSTEM_REMAINING_WORK_PLAN.md](./EMAIL_SYSTEM_REMAINING_WORK_PLAN.md#phase-6a62-fix-registration-cancellation-email-urgent-bug-fix) |
| Part of Phase 3 | Event Cancellation Email Template | 1 hour | **6A.63** | [EMAIL_SYSTEM_REMAINING_WORK_PLAN.md](./EMAIL_SYSTEM_REMAINING_WORK_PLAN.md#phase-6a63-event-cancellation-email---complete-recipient-consolidation) |

**Note**: 6A.62 and 6A.63 are integrated into Phase 3 (6A.72) of the stabilization plan.

---

## Implementation Order (Recommended)

### Week 1: Critical Fixes
**Days 1-2**: Phase 1 (6A.70) - URL Centralization
- Unblocks staging deployments
- Creates foundation for all other phases
- Low risk, high impact

**Days 2-3**: Phase 2 (6A.71) - Event Reminder System Fix
- Restores critical automated notifications
- Database migration required
- Medium risk, critical impact

**Days 3-4**: Phase 3 (6A.72) - Event Cancellation Recipient Fix
- Ensures all stakeholders receive cancellation notifications
- Includes 6A.62 (bug fix) and 6A.63 (template creation)
- Medium risk, high impact

**Day 4**: Phase 4 (6A.73) - Email Template Constants
- Quick win, improves code quality
- Zero risk, immediate benefit
- Sets up for Phase 5

### Week 2: High Priority Features
**Days 5-6**: Phase 6 (6A.60) - Signup Commitment Emails
- Missing email flow
- No database changes
- Low risk, completes user story

**Days 6-7**: Begin Phase 5 (6A.74) - Email Orchestration
- Major refactor, requires confidence from phases 1-4
- Continue into Week 3 if needed

### Week 3-4: Medium Priority
**Days 8-9**: Complete Phase 5 (6A.74) - Email Orchestration
- Centralize all email logic
- Medium risk, high maintainability benefit

**Days 10-12**: Phase 7 (6A.61) - Manual Event Email Sending
- Complex feature, final major addition
- Organizer communication tool
- High risk, high business value

### Ongoing: Observability
**Continuous**: Phase 8 (6A.75) - Observability & Monitoring
- Can be implemented incrementally
- Correlation IDs in each phase
- Metrics and alerts as system matures

---

## Phase Number Verification

**Verified Against**: [PHASE_6A_MASTER_INDEX.md](./PHASE_6A_MASTER_INDEX.md)

**Last Used Phase**: 6A.69 (Sign-Up List CSV Export)

**New Assignments** (Checked - No Conflicts):
- ✅ 6A.70: URL Centralization (Phase 1)
- ✅ 6A.71: Event Reminder System Fix (Phase 2)
- ✅ 6A.72: Event Cancellation Recipient Fix (Phase 3)
- ✅ 6A.73: Email Template Constants (Phase 4)
- ✅ 6A.74: Centralized Email Orchestration (Phase 5)
- ✅ 6A.75: Observability & Monitoring (Phase 8)

**Existing Assignments** (Already Reserved):
- ✅ 6A.60: Signup Commitment Emails (Phase 6)
- ✅ 6A.61: Manual Event Email Sending (Phase 7)
- ✅ 6A.62: Fix Registration Cancellation Email (Bug fix)
- ✅ 6A.63: Event Cancellation Email Template (Integrated into Phase 3)

**Next Available Phase**: 6A.76

---

## Master Index Update Required

After completion of each phase, update [PHASE_6A_MASTER_INDEX.md](./PHASE_6A_MASTER_INDEX.md) with:

```markdown
| 6A.70 | URL Centralization | ✅ Complete | [PHASE_6A70_URL_CENTRALIZATION_SUMMARY.md](./PHASE_6A70_URL_CENTRALIZATION_SUMMARY.md) | 2026-01-XX |
| 6A.71 | Event Reminder System Fix | ✅ Complete | [PHASE_6A71_EVENT_REMINDER_FIX_SUMMARY.md](./PHASE_6A71_EVENT_REMINDER_FIX_SUMMARY.md) | 2026-01-XX |
| 6A.72 | Event Cancellation Recipient Fix | ✅ Complete | [PHASE_6A72_EVENT_CANCELLATION_FIX_SUMMARY.md](./PHASE_6A72_EVENT_CANCELLATION_FIX_SUMMARY.md) | 2026-01-XX |
| 6A.73 | Email Template Constants | ✅ Complete | [PHASE_6A73_TEMPLATE_CONSTANTS_SUMMARY.md](./PHASE_6A73_TEMPLATE_CONSTANTS_SUMMARY.md) | 2026-01-XX |
| 6A.74 | Centralized Email Orchestration | ✅ Complete | [PHASE_6A74_EMAIL_ORCHESTRATION_SUMMARY.md](./PHASE_6A74_EMAIL_ORCHESTRATION_SUMMARY.md) | 2026-01-XX |
| 6A.75 | Observability & Monitoring | ✅ Complete | [PHASE_6A75_EMAIL_OBSERVABILITY_SUMMARY.md](./PHASE_6A75_EMAIL_OBSERVABILITY_SUMMARY.md) | 2026-01-XX |
```

---

## Cross-References

**Related Documents**:
- [EMAIL_SYSTEM_STABILIZATION_PLAN.md](./EMAIL_SYSTEM_STABILIZATION_PLAN.md) - Detailed implementation plan
- [EMAIL_SYSTEM_COMPREHENSIVE_ANALYSIS.md](./EMAIL_SYSTEM_COMPREHENSIVE_ANALYSIS.md) - Current state analysis
- [EMAIL_SYSTEM_REMAINING_WORK_PLAN.md](./EMAIL_SYSTEM_REMAINING_WORK_PLAN.md) - Phase 6A.60, 6A.61 details
- [PHASE_6A_MASTER_INDEX.md](./PHASE_6A_MASTER_INDEX.md) - Phase number registry
- [PROGRESS_TRACKER.md](./PROGRESS_TRACKER.md) - Session history
- [STREAMLINED_ACTION_PLAN.md](./STREAMLINED_ACTION_PLAN.md) - Action items
- [TASK_SYNCHRONIZATION_STRATEGY.md](./TASK_SYNCHRONIZATION_STRATEGY.md) - Documentation protocol

**Primary Tracking Documents** (Must Stay in Sync):
1. PROGRESS_TRACKER.md
2. STREAMLINED_ACTION_PLAN.md
3. TASK_SYNCHRONIZATION_STRATEGY.md

---

## Success Criteria Per Phase

### Phase 1 (6A.70): URL Centralization
- [ ] Zero hardcoded production URLs in codebase
- [ ] Email URLs dynamically generated from configuration
- [ ] Staging deploys send staging URLs in emails
- [ ] All 7 handlers refactored to use `IEmailUrlHelper`
- [ ] 15 unit tests pass
- [ ] Build succeeds (0 errors, 0 warnings)

### Phase 2 (6A.71): Event Reminder System Fix
- [ ] Hangfire job registered and running hourly
- [ ] 3-tier reminders (7 days, 2 days, 1 day) sending correctly
- [ ] No duplicate reminders sent (tracking prevents)
- [ ] `event_reminders_sent` table created
- [ ] Email URLs use environment-specific configuration
- [ ] 15 unit tests + 5 integration tests pass

### Phase 3 (6A.72): Event Cancellation Recipient Fix
- [ ] Event cancellation emails reach ALL stakeholders (registrations + email groups + newsletter)
- [ ] Zero emails sent to cancelled events with zero recipients (graceful exit)
- [ ] Template-based emails (no inline HTML)
- [ ] `event-cancelled-notification` template created
- [ ] Comprehensive logging with recipient breakdowns
- [ ] 5 test scenarios pass (including critical bug fix tests)

### Phase 4 (6A.73): Email Template Constants
- [ ] Zero magic strings for template names in handlers
- [ ] Compile-time safety (typos cause compilation errors)
- [ ] All 15 handlers updated to use constants
- [ ] Easier refactoring (rename template = update one constant)

### Phase 5 (6A.74): Email Orchestration
- [ ] All email logic centralized in orchestration service
- [ ] Event handlers simplified (5-10 lines each)
- [ ] 90%+ test coverage on orchestration service
- [ ] Consistent logging format across all emails
- [ ] 50 unit tests + 10 integration tests pass

### Phase 6 (6A.60): Signup Commitment Emails
- [ ] Confirmation emails sent after signup commitments
- [ ] Domain event raised on commitment
- [ ] Template uses existing `signup-commitment-confirmation`
- [ ] 16 tests pass (4 domain + 10 handler + 2 integration)

### Phase 7 (6A.61): Manual Event Email Sending
- [ ] Organizers can send custom emails to registrants
- [ ] Recipient filtering works (All, PaidOnly, FreeOnly, Specific)
- [ ] Email preview available before sending
- [ ] Batch processing handles 1000+ recipients
- [ ] Idempotency prevents duplicate sends
- [ ] Audit trail with per-recipient delivery status
- [ ] 30+ tests pass (domain + application + integration)

### Phase 8 (6A.75): Observability & Monitoring
- [ ] Correlation IDs propagate across email flows
- [ ] Email delivery metrics tracked
- [ ] Alerting rules configured (delivery rate < 95%)
- [ ] Dashboard queries operational
- [ ] Logs searchable by correlation ID

---

## Deployment Verification Checklist

After each phase deployment to staging:

- [ ] Build succeeds (0 errors, 0 warnings)
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] Database migration applied (if applicable)
- [ ] Health check endpoint responds (200 OK)
- [ ] Application logs show successful startup
- [ ] Phase-specific functionality tested
- [ ] Logs checked for phase-specific markers
- [ ] Email sending verified
- [ ] No critical errors in logs
- [ ] Performance metrics acceptable
- [ ] Monitor for 24-48 hours before production

After each phase deployment to production:

- [ ] Staging verification complete
- [ ] Business sign-off received
- [ ] Rollback plan documented
- [ ] Deployment during low-traffic hours
- [ ] Smoke tests pass
- [ ] Health check responds
- [ ] Monitor logs for 48 hours
- [ ] Email delivery rates normal
- [ ] User feedback monitored

---

## Phase Completion Checklist

For each completed phase:

- [ ] Create phase summary document (e.g., `PHASE_6A70_URL_CENTRALIZATION_SUMMARY.md`)
- [ ] Update [PHASE_6A_MASTER_INDEX.md](./PHASE_6A_MASTER_INDEX.md) with status and document link
- [ ] Update [PROGRESS_TRACKER.md](./PROGRESS_TRACKER.md) with completion details
- [ ] Update [STREAMLINED_ACTION_PLAN.md](./STREAMLINED_ACTION_PLAN.md) with status
- [ ] Update [TASK_SYNCHRONIZATION_STRATEGY.md](./TASK_SYNCHRONIZATION_STRATEGY.md) if needed
- [ ] Verify build status (0 errors, 0 warnings)
- [ ] All tests pass (unit + integration)
- [ ] Production deployment verified
- [ ] Monitor logs for 48 hours post-deployment

---

**Document Owner**: System Architect
**Last Updated**: 2026-01-12
**Status**: Ready for Implementation

**END OF PHASE ASSIGNMENTS**
