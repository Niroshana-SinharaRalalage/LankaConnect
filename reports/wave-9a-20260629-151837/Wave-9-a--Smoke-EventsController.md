# Smoke Report: Wave 9.a: Smoke-EventsController

**Status**: [FAIL]
**Started**: 2026-06-29T19:18:41Z UTC
**Finished**: 2026-06-29T19:19:16Z UTC
**Duration**: 35.34 sec

## Summary

| Metric | Count |
|---|---|
| Total | 34 |
| Passed | 22 |
| Failed | 5 |
| Skipped | 7 |
| Pass rate | 64.71% |

## Per-Section Results

| Section | Pass | Fail | Skip |
|---|---|---|---|
| admin | 0 | 0 | 1 |
| analytics | 0 | 0 | 1 |
| attendees | 0 | 1 | 0 |
| cancel | 0 | 2 | 0 |
| crud-read | 4 | 0 | 0 |
| crud-write | 4 | 1 | 1 |
| email-groups | 1 | 0 | 1 |
| list-and-filter | 3 | 0 | 0 |
| my-registrations | 1 | 0 | 0 |
| organizer-contacts | 1 | 0 | 1 |
| paid-event | 2 | 0 | 1 |
| rsvp | 5 | 1 | 0 |
| ticketing | 1 | 0 | 1 |

## Failures

- **crud-write :: fetch created event** (GET /api/Events/{id})
  - Assertion 'Assert-AuditFieldsFresh' failed: expected=updatedAt present actual=missing updatedAt context=newly-created event audit fields
- **rsvp :: dispatch log: RegistrationConfirmedEvent dispatched** ((container logs))
  - Assertion 'Assert-DomainEventDispatched' failed: expected=log line containing 'dispatched ... Registration' actual=no matching log lines found (event was not dispatched OR log tail too short) context=W5.3 EventRepository must dispatch RegistrationConfirmedEvent through Wave3-followup.B-widened filter
- **cancel :: cancel my registration** (DELETE /api/Events/{id}/my-registration)
  - Expected 200/204, got 405
- **cancel :: post-cancel: registration removed from event** (GET /api/Events/{id})
  - Expected count=0 OR status!=Confirmed; got count=1 status=Confirmed
- **attendees :: attendees list for canonical event** (GET /api/Events/{paidFixture}/attendees)
  - Assertion 'Assert-HttpStatus' failed: expected=200 actual=403 context= url=GET https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/5fbcea92-bd5b-486f-9eab-1c4ee0146307/attendees bodyPreview=<empty> err=HTTP 403

## Skipped (documented)

- **crud-write :: delete event** (DELETE /api/Events/{id}) - destructive; run with -IncludeDestructive
- **paid-event :: Stripe checkout** (POST /api/Events/{id}/checkout) - state-dependent (Stripe); run with -IncludePaymentFlows
- **ticketing :: ticket-tier CRUD operations** (POST/PATCH /api/Events/{id}/ticket-tiers) - expansion deferred to Wave 9.c full ticketing coverage
- **organizer-contacts :: organizer contacts CRUD** (POST/PATCH/DELETE /api/Events/{id}/organizer-contacts) - expansion deferred to follow-up
- **email-groups :: email-group CRUD on events** (POST/DELETE /api/Events/{id}/email-groups) - expansion deferred to follow-up
- **analytics :: event analytics endpoints** (GET /api/Events/{id}/analytics/*) - EventAnalyticsRepository + EventViewRecordRepository remain in legacy Infrastructure (interfaces in LankaConnect.Domain.Analytics); not part of W5.3 surface
- **admin :: global admin endpoints (skipped - 403 inverted assertion)** (POST /api/Events/admin/*) - test user is EventOrganizer not global admin; inverted-403 assertions deferred to dedicated admin smoke (Wave 9.b)


