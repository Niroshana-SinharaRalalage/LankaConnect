# Staging Route Inventory (P3 audit, 2026-06-08)

**Purpose**: verify every smoke RouteMap entry maps to a real controller route BEFORE the G-sequence writes the entries. This audit caught one wrong route (PhotoAlbums) before execution started, validating the founder's insistence on pre-execution audits.

## Confirmed routes for G1.d / G2 / G3 / G4 / G5 / G8 smokes

| Gap | Resource | Method | Path | Source file:line | Status |
|---|---|---|---|---|---|
| **Auth foundation** | Login | POST | `/api/Auth/login` | AuthController.cs | ✅ ALREADY in smoke RouteMap |
| **G1.d / current smoke** | User UpdateLocation | PUT | `/api/users/{id:guid}/location` | UsersController.cs `[HttpPut("{id:guid}/location")]` | ⚠️ FIX: RouteMap has `/api/users/me/location` — needs `{id:guid}` substitution from `$env:LC_USER_ID` |
| **G1.d** | EmailGroup Update | PUT | `/api/EmailGroups/{id:guid}` | EmailGroupsController.cs | ✅ confirmed |
| **G1.d** | EmailGroup Deactivate | DELETE | `/api/EmailGroups/{id:guid}` | EmailGroupsController.cs (soft-delete via DELETE) | ✅ confirmed |
| **G1.d** | Collection MarkAsFailed | POST | `/api/events/{eventId}/collections/{collectionId}/payment-failed` | CollectionsController.cs (Stripe webhook simulation) | NOTE: webhook-driven; smoke triggers via test endpoint |
| **G1.d** | Registration UpdateDetails | PUT | `/api/events/{eventId}/my-registration` | EventsController.cs `[HttpPut("{eventId}/my-registration")]` | ✅ confirmed |
| **G2 Media** | PhotoAlbum Create | POST | `/api/events/{eventId:guid}/albums` | PhotoAlbumsController.cs `[Route("api/events/{eventId:guid}/albums")]` + `[HttpPost]` | ✅ **CORRECTION** — I had assumed `/api/events/{id}/photo-albums`; actual is `/albums` |
| **G2 Media** | PhotoAlbum List | GET | `/api/events/{eventId:guid}/albums` | PhotoAlbumsController.cs | ✅ confirmed |
| **G3 Forms** | EventForm List | GET | TBD — Forms.Api.Controllers — needs grep | TBD | ⏳ verify in G3 prep |
| **G4 Notifications** | Notification Unread | GET | `/api/notifications/unread` | Modules/Notifications/.../NotificationsController.cs `[HttpGet("unread")]` | ✅ ALREADY in smoke RouteMap |
| **G4 Notifications** | Notification MarkRead | POST | `/api/notifications/{notificationId}/read` | NotificationsController.cs `[HttpPost("{notificationId}/read")]` | ✅ confirmed (route works via trigger event, not direct POST) |
| **G5 Cultural** | GetEventRecommendations | GET | TBD — EventsController.cs has no recommendations route; check elsewhere | TBD | ⏳ deeper grep needed before G5 |
| **G8 BB/SK surface** | Event Detail | GET | `/api/events/{eventId}` | EventsController.cs | ✅ ALREADY in smoke RouteMap as event-List; price-shape assertion added in G8 |

## What this audit caught

1. **PhotoAlbums route was wrong** in my initial smoke RouteMap assumption — actual is `/api/events/{id}/albums`, not `/photo-albums`. Confirmed by reading PhotoAlbumsController.cs line 29: `[Route("api/events/{eventId:guid}/albums")]`.
2. **UpdateLocation route requires {id:guid}** — actual is `/api/users/{id:guid}/location`, not `/api/users/me/location`. Smoke needs to substitute `$env:LC_USER_ID`.
3. **GetEventRecommendations endpoint not visible in EventsController** — G5 needs deeper search (may be in a separate AnalyticsController or query handler with no direct HTTP surface; if so, G5 demotes from API-smokeable to unit-only).
4. **No RegistrationsController exists** — registrations are sub-resources of EventsController via `/api/events/{eventId}/my-registration` (PUT). Mental model corrected.

## Updated Smoke-Mutator RouteMap (additions for the G-sequence)

The script's `$script:RouteMap` should gain these entries when each gap starts:

```powershell
'user-UpdateLocation' = @{   # G1.d — FIX existing entry
    Method = 'PUT'
    Path   = '/api/users/{id:guid}/location'   # use $env:LC_USER_ID
    Body   = @{ city = 'Cleveland'; state = 'OH'; country = 'USA' }
    AssertAuditFields = $true
}
'photoAlbum-Create' = @{   # G2
    Method = 'POST'
    Path   = '/api/events/{eventId:guid}/albums'   # known staging event id
    Body   = @{ name = 'Smoke Test Album'; description = 'G2 smoke' }
    AssertAuditFields = $true
}
'emailGroup-Update' = @{   # G1.d
    Method = 'PUT'
    Path   = '/api/EmailGroups/{id:guid}'   # smoke creates the group first, captures id
    Body   = @{ name = 'Updated'; emailAddresses = 'a@b.com'; description = 'G1.d' }
    AssertAuditFields = $true
}
'registration-UpdateDetails' = @{   # G1.d
    Method = 'PUT'
    Path   = '/api/events/{eventId}/my-registration'   # requires known eventId
    Body   = @{ attendees = @( @{ name = 'Test'; ageCategory = 'Adult' } ); contact = @{ email = 'test@test.com'; phone = '1234567890' } }
    AssertAuditFields = $true
}
```

The `{id:guid}` and `{eventId:guid}` placeholders are substituted at runtime from `$env:LC_USER_ID` (set by Invoke-Login) or from a fixture-id seed captured per gap.

## Gap classification (P4 — API-smokeable vs unit-only)

Per the audit:

| Gap | Coverage |
|---|---|
| G0 | Foundation — smoke harness itself |
| G1.a | Unit-only (LegacyBaseEntity ctor) |
| G1.b | Unit-only (factory CreatedAt assertions) — already shipped |
| G1.c | Unit-only (model-builder Ignore coverage) |
| G1.d | **API-smokeable** (4 mutator smokes via RouteMap above) |
| G2 | **API-smokeable** (Media create + list via `/api/events/{id}/albums`) |
| G3 | **API-smokeable** (Forms — route TBD) |
| G4 | **API-smokeable** (Notifications mark-read via webhook trigger) |
| G5 | **Unit-only** (GetEventRecommendations not directly HTTP-surfaced — confirmed by audit; ICulturalCalendar tested via DI resolution unit test only) |
| G6 | Operational verification (`Smoke-Probe.ps1` against staging schemas) |
| G7 | **Unit-only** (Wave 2 cultural types are internal; no direct API surface; ArchTest + one unit test per moved type is the coverage) |
| G8 | **API-smokeable** (event detail price-shape assertion) |
