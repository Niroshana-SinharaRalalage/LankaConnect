# Smoke Report: Wave 9.a: Smoke-EventsController

**Status**: [PASS-WITH-SKIPS]
**Started**: 2026-06-29T19:22:16Z UTC
**Finished**: 2026-06-29T19:23:03Z UTC
**Duration**: 46.93 sec

## Summary

| Metric | Count |
|---|---|
| Total | 34 |
| Passed | 26 |
| Failed | 0 |
| Skipped | 8 |
| Pass rate | 76.47% |

## Per-Section Results

| Section | Pass | Fail | Skip |
|---|---|---|---|
| admin | 0 | 0 | 1 |
| analytics | 0 | 0 | 1 |
| attendees | 1 | 0 | 0 |
| cancel | 2 | 0 | 0 |
| crud-read | 4 | 0 | 0 |
| crud-write | 5 | 0 | 1 |
| email-groups | 1 | 0 | 1 |
| list-and-filter | 3 | 0 | 0 |
| my-registrations | 1 | 0 | 0 |
| organizer-contacts | 1 | 0 | 1 |
| paid-event | 2 | 0 | 1 |
| rsvp | 5 | 0 | 1 |
| ticketing | 1 | 0 | 1 |

## Skipped (documented)

- **crud-write :: delete event** (DELETE /api/Events/{id}) - destructive; run with -IncludeDestructive
- **rsvp :: dispatch log assertion (log tail too short)** ((container logs)) - Staging logs roll fast (WhatsApp diag spam); count-incremented + Confirmed status above is canonical W5.3 proof
- **paid-event :: Stripe checkout** (POST /api/Events/{id}/checkout) - state-dependent (Stripe); run with -IncludePaymentFlows
- **ticketing :: ticket-tier CRUD operations** (POST/PATCH /api/Events/{id}/ticket-tiers) - expansion deferred to Wave 9.c full ticketing coverage
- **organizer-contacts :: organizer contacts CRUD** (POST/PATCH/DELETE /api/Events/{id}/organizer-contacts) - expansion deferred to follow-up
- **email-groups :: email-group CRUD on events** (POST/DELETE /api/Events/{id}/email-groups) - expansion deferred to follow-up
- **analytics :: event analytics endpoints** (GET /api/Events/{id}/analytics/*) - EventAnalyticsRepository + EventViewRecordRepository remain in legacy Infrastructure (interfaces in LankaConnect.Domain.Analytics); not part of W5.3 surface
- **admin :: global admin endpoints (skipped - 403 inverted assertion)** (POST /api/Events/admin/*) - test user is EventOrganizer not global admin; inverted-403 assertions deferred to dedicated admin smoke (Wave 9.b)


