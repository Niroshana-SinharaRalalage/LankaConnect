# Phase 6A.87: Hybrid Email System - Complete Implementation Plan

**Start Date**: 2026-01-27
**Target Completion**: 2026-03-14 (7 weeks)
**Status**: Week 2 Complete, Week 3 Starting

---

## Overview

| Week | Focus | Status |
|------|-------|--------|
| Week 1 | Foundation Infrastructure | ✅ COMPLETE |
| Week 2 | Pilot Handler Migration | ✅ COMPLETE |
| Week 3 | Email Tracking Dashboard | 🔄 IN PROGRESS |
| Week 4 | High Priority Migrations (Part 1) | 🔴 PENDING |
| Week 5 | High Priority Migrations (Part 2) | 🔴 PENDING |
| Week 6 | Medium Priority Migrations | 🔴 PENDING |
| Week 7 | Production Rollout & Cleanup | 🔴 PENDING |

---

## WEEK 1: FOUNDATION INFRASTRUCTURE ✅ COMPLETE

**Dates**: 2026-01-27 to 2026-01-27 (Completed in 1 day)

### Day 1 (2026-01-27) ✅
| Task | Tests | Status |
|------|-------|--------|
| Create LankaConnect.Shared project | - | ✅ |
| IEmailParameters interface | 10 | ✅ |
| EmailFeatureFlags configuration | 15 | ✅ |
| IEmailLogger interface | 8 | ✅ |
| IEmailMetrics interface | 12 | ✅ |
| **Subtotal** | **45** | ✅ |

### Day 2 (2026-01-27) ✅
| Task | Tests | Status |
|------|-------|--------|
| UserEmailParams base class | 7 | ✅ |
| EventEmailParams base class | 7 | ✅ |
| OrganizerEmailParams base class | 7 | ✅ |
| **Subtotal** | **21** | ✅ |

### Day 3 (2026-01-27) ✅
| Task | Tests | Status |
|------|-------|--------|
| ITypedEmailService interface | 5 | ✅ |
| TypedEmailServiceAdapter | 10 | ✅ |
| **Subtotal** | **15** | ✅ |

### Day 4 (2026-01-27) ✅
| Task | Tests | Status |
|------|-------|--------|
| EmailServiceBridgeAdapter | - | ✅ |
| DI registration extensions | - | ✅ |
| DefaultEmailLogger implementation | - | ✅ |
| DefaultEmailMetrics implementation | - | ✅ |

### Day 5 (2026-01-27) ✅
| Task | Tests | Status |
|------|-------|--------|
| Test coverage verification | - | ✅ |
| Documentation | - | ✅ |
| Deploy to staging | - | ✅ |

**Week 1 Total Tests**: 81 ✅

---

## WEEK 2: PILOT HANDLER MIGRATION ✅ COMPLETE

**Dates**: 2026-01-28

### Day 1 (2026-01-28) ✅
| Task | Tests | Status |
|------|-------|--------|
| EventReminderEmailParams class | 28 | ✅ |
| Migrate EventReminderJob to ITypedEmailService | - | ✅ |
| Update EventReminderJobTests | - | ✅ |
| Configure feature flag (EventReminderJob: true) | - | ✅ |
| Deploy to staging | - | ✅ |
| Test with real email | - | ✅ |

### Day 2 (2026-01-28) ✅
| Task | Tests | Status |
|------|-------|--------|
| Fix HasTicket parameter bug | 1 | ✅ |
| Create EMAIL_TEMPLATE_PARAMETER_MANIFEST.md | - | ✅ |
| Deploy fix to staging | - | ✅ |

**Week 2 Total Tests**: 29 ✅

---

## WEEK 3: EMAIL TRACKING DASHBOARD 🔄 IN PROGRESS

**Dates**: 2026-01-28 to 2026-02-03

### Day 1 (2026-01-28) ✅
| Task | Tests | Status |
|------|-------|--------|
| Enhance IEmailMetrics with dashboard methods | 9 | ✅ |
| Create EmailMetricsController | - | ✅ |
| GET /api/admin/email-metrics/summary | - | ✅ |
| GET /api/admin/email-metrics/by-template | - | ✅ |
| GET /api/admin/email-metrics/failures | - | ✅ |
| GET /api/admin/email-metrics/validation-failures | - | ✅ |
| GET /api/admin/email-metrics/migration-progress | - | ✅ |
| GET /api/admin/email-metrics/by-template/{name} | - | ✅ |
| POST /api/admin/email-metrics/reset | - | ✅ |

### Day 2 (2026-01-29)
| Task | Tests | Status |
|------|-------|--------|
| Deploy to staging | - | 🔴 |
| Test all API endpoints with curl | - | 🔴 |
| Verify metrics collection works | - | 🔴 |

### Day 3-4 (2026-01-30 to 2026-01-31)
| Task | Tests | Status |
|------|-------|--------|
| Database table: email_send_logs | - | 🔴 |
| EF Core migration | - | 🔴 |
| Persist metrics to database | 5 | 🔴 |
| Historical data queries | 5 | 🔴 |

### Day 5 (2026-02-01)
| Task | Tests | Status |
|------|-------|--------|
| Deploy dashboard API to staging | - | 🔴 |
| Test all endpoints | - | 🔴 |
| Documentation | - | 🔴 |

**Week 3 Estimated Tests**: 30

---

## WEEK 4: HIGH PRIORITY MIGRATIONS (Part 1)

**Dates**: 2026-02-03 to 2026-02-07

### Day 1-2: PaymentCompletedEventHandler
| Task | Tests | Status |
|------|-------|--------|
| TicketConfirmationEmailParams class | ~25 | 🔴 |
| Migrate PaymentCompletedEventHandler | - | 🔴 |
| Enable feature flag | - | 🔴 |

### Day 3-4: RegistrationConfirmedEventHandler
| Task | Tests | Status |
|------|-------|--------|
| FreeEventRegistrationEmailParams class | ~20 | 🔴 |
| Migrate RegistrationConfirmedEventHandler | - | 🔴 |
| Enable feature flag | - | 🔴 |

### Day 5: Testing & Deploy
| Task | Tests | Status |
|------|-------|--------|
| Integration testing | - | 🔴 |
| Deploy to staging | - | 🔴 |
| Test with real emails | - | 🔴 |

**Week 4 Estimated Tests**: 45

---

## WEEK 5: HIGH PRIORITY MIGRATIONS (Part 2)

**Dates**: 2026-02-10 to 2026-02-14

### Day 1-2: MemberVerificationRequestedEventHandler
| Task | Tests | Status |
|------|-------|--------|
| EmailVerificationEmailParams class | ~15 | 🔴 |
| Migrate MemberVerificationRequestedEventHandler | - | 🔴 |

### Day 3-4: Password Reset Handlers
| Task | Tests | Status |
|------|-------|--------|
| PasswordResetEmailParams class | ~15 | 🔴 |
| Migrate PasswordResetRequestedEventHandler | - | 🔴 |
| Migrate PasswordChangedEventHandler | - | 🔴 |

### Day 5: Testing & Deploy
| Task | Tests | Status |
|------|-------|--------|
| Integration testing | - | 🔴 |
| Deploy to staging | - | 🔴 |

**Week 5 Estimated Tests**: 30

---

## WEEK 6: MEDIUM PRIORITY MIGRATIONS

**Dates**: 2026-02-17 to 2026-02-21

### Templates to Migrate:
| Template | Handler | Tests |
|----------|---------|-------|
| Signup commitment confirmation | SignupCommitmentConfirmedEventHandler | ~10 |
| Signup commitment update | SignupCommitmentUpdatedEventHandler | ~10 |
| Signup commitment cancellation | SignupCommitmentCancelledEventHandler | ~10 |
| Registration cancellation | RegistrationCancelledEventHandler | ~10 |
| Event published | EventPublishedEventHandler | ~10 |
| Event cancellation | EventCancelledEventHandler | ~10 |

**Week 6 Estimated Tests**: 60

---

## WEEK 7: PRODUCTION ROLLOUT & CLEANUP

**Dates**: 2026-02-24 to 2026-02-28

### Day 1-2: Remaining Migrations
| Template | Handler | Status |
|----------|---------|--------|
| Newsletter | NewsletterEmailJob | 🔴 |
| Newsletter subscription | SubscribeToNewsletterCommandHandler | 🔴 |
| Event details | EventNotificationEmailJob | 🔴 |
| Welcome | EmailVerifiedEventHandler | 🔴 |
| Organizer approval | OrganizerRoleApprovedEventHandler | 🔴 |

### Day 3: Global Rollout
| Task | Status |
|------|--------|
| Set UseTypedParameters = true globally | 🔴 |
| Monitor metrics dashboard | 🔴 |
| Verify all handlers working | 🔴 |

### Day 4-5: Cleanup
| Task | Status |
|------|--------|
| Remove legacy Dictionary code paths (optional) | 🔴 |
| Update documentation | 🔴 |
| Performance review | 🔴 |

---

## Summary

| Week | Focus | Tests | Status |
|------|-------|-------|--------|
| Week 1 | Foundation | 81 | ✅ COMPLETE |
| Week 2 | Pilot (EventReminder) | 29 | ✅ COMPLETE |
| Week 3 | Dashboard API | ~30 | 🔄 IN PROGRESS |
| Week 4 | High Priority (Part 1) | ~45 | 🔴 PENDING |
| Week 5 | High Priority (Part 2) | ~30 | 🔴 PENDING |
| Week 6 | Medium Priority | ~60 | 🔴 PENDING |
| Week 7 | Rollout & Cleanup | ~20 | 🔴 PENDING |
| **TOTAL** | | **~295** | |

---

## Current Progress

- **Tests Written**: 110 (81 + 29)
- **Templates Migrated**: 1/19 (5%)
- **Handlers Migrated**: 1/~15 (7%)
- **Dashboard**: 0% (Starting now)

---

**Last Updated**: 2026-01-28
