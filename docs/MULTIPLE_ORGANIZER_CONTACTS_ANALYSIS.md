# Multiple Organizer Contacts — Design Analysis Document

**Date:** 2026-03-02
**Author:** System Architect (AI-Assisted)
**Status:** Analysis Complete — Awaiting Review
**Scope:** Refactor from single organizer contact (scalar columns) to multiple organizer contacts (child entity table)

---

## Table of Contents

1. [Problem Statement](#1-problem-statement)
2. [Current Design (Before Changes)](#2-current-design-before-changes)
3. [New Design (In-Progress Changes)](#3-new-design-in-progress-changes)
4. [Database Migration Strategy](#4-database-migration-strategy)
5. [API Contract Changes](#5-api-contract-changes)
6. [Frontend Changes](#6-frontend-changes)
7. [Test Coverage](#7-test-coverage)
8. [Backward Compatibility](#8-backward-compatibility)
9. [Issues & Risks Identified](#9-issues--risks-identified)
10. [Files Changed Summary](#10-files-changed-summary)
11. [Recommendations](#11-recommendations)

---

## 1. Problem Statement

Events currently support only **one** organizer contact (name, email, phone) stored as scalar columns on the `events` table. Real-world events often have **multiple** organizers — e.g., a primary organizer and co-organizers from different teams. Users need the ability to publish multiple contact persons so attendees can reach the right person.

**User-facing behavior requested:**
- Add/remove multiple organizer contacts per event
- Each contact has: name, email (optional), phone (optional)
- First contact is automatically the "primary" contact
- Backward compatibility: existing email handlers still access `OrganizerContactName/Email/Phone`

---

## 2. Current Design (Before Changes)

### 2.1 Domain Entity (`Event.cs`)

Three scalar properties stored directly on the `Event` aggregate:

```csharp
public bool PublishOrganizerContact { get; private set; }
public string? OrganizerContactName { get; private set; }
public string? OrganizerContactPhone { get; private set; }
public string? OrganizerContactEmail { get; private set; }
```

Single domain method for setting contact details:

```csharp
public Result SetOrganizerContactDetails(
    bool publishContact,
    string? contactName,
    string? contactPhone,
    string? contactEmail)
```

### 2.2 Database Schema (Before)

Three nullable columns on the `events.events` table:

| Column | Type | Nullable |
|--------|------|----------|
| `publish_organizer_contact` | `boolean` | NOT NULL, default `false` |
| `organizer_contact_name` | `varchar(200)` | YES |
| `organizer_contact_email` | `varchar(255)` | YES |
| `organizer_contact_phone` | `varchar(20)` | YES |

### 2.3 Limitations

- **Single contact only** — no way to list multiple organizers
- **No ordering/priority** — single contact is implicitly the primary
- **No extensibility** — adding fields like `role`, `linkedUserId` requires new columns on the events table
- **Denormalized** — contact data embedded in the events table

---

## 3. New Design (In-Progress Changes)

### 3.1 New Domain Entity: `EventOrganizerContact`

**File:** `src/LankaConnect.Domain/Events/Entities/EventOrganizerContact.cs`

A proper DDD child entity under the `Event` aggregate:

```csharp
public class EventOrganizerContact : BaseEntity
{
    public Guid    EventId      { get; private set; }
    public string  ContactName  { get; private set; } = string.Empty;
    public string? ContactEmail { get; private set; }
    public string? ContactPhone { get; private set; }
    public bool    IsPrimary    { get; private set; }
    public Guid?   LinkedUserId { get; private set; }  // Future: co-organizer linking
    public int     SortOrder    { get; private set; }
}
```

**Validation rules in `Create()` factory:**
- `eventId` must not be `Guid.Empty`
- `contactName` required, max 200 characters
- At least one of `contactEmail` or `contactPhone` must be provided
- Email validated with RFC 5322 regex, stored as `.Trim().ToLowerInvariant()`
- Phone max 20 chars, stored as `.Trim()`

### 3.2 Modified `Event` Aggregate

The scalar properties are replaced with a collection:

```csharp
// Backing field
private readonly List<EventOrganizerContact> _organizerContacts = new();

// Public read-only collection
public IReadOnlyList<EventOrganizerContact> OrganizerContacts => _organizerContacts.AsReadOnly();

// Backward-compat computed properties (delegate to primary contact)
public string? OrganizerContactName  => GetPrimaryContact()?.ContactName;
public string? OrganizerContactPhone => GetPrimaryContact()?.ContactPhone;
public string? OrganizerContactEmail => GetPrimaryContact()?.ContactEmail;
```

New domain method:

```csharp
public Result SetOrganizerContacts(
    bool publishContact,
    List<(string name, string? email, string? phone)> contacts)
```

Behavior:
- When `publishContact = true`: validates all contacts, assigns `IsPrimary = true` to index 0, assigns `SortOrder` by index
- When `publishContact = false`: clears all contacts (privacy)
- Uses `Clear()` + `AddRange()` pattern (safe for EF Core relational tracking, unlike JSONB)

### 3.3 New Database Schema

**New table:** `events.event_organizer_contacts`

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `id` | `uuid` | NOT NULL | PK, ValueGeneratedNever |
| `event_id` | `uuid` | NOT NULL | FK → `events.events.id`, CASCADE DELETE |
| `contact_name` | `varchar(200)` | NOT NULL | |
| `contact_email` | `varchar(255)` | YES | |
| `contact_phone` | `varchar(20)` | YES | |
| `is_primary` | `boolean` | NOT NULL | Default `false` |
| `linked_user_id` | `uuid` | YES | Reserved for future use |
| `sort_order` | `integer` | NOT NULL | Default `0` |
| `created_at` | `timestamp with time zone` | NOT NULL | Default `NOW()` |
| `updated_at` | `timestamp with time zone` | YES | |

**Index:** `ix_event_organizer_contacts_event_id` on `event_id`

**Dropped columns** from `events.events`: `organizer_contact_name`, `organizer_contact_email`, `organizer_contact_phone`

**Retained column** on `events.events`: `publish_organizer_contact` (boolean flag)

### 3.4 EF Core Configuration

**`EventConfiguration.cs` changes:**
```csharp
// Computed props ignored (not DB columns)
builder.Ignore(e => e.OrganizerContactName);
builder.Ignore(e => e.OrganizerContactPhone);
builder.Ignore(e => e.OrganizerContactEmail);

// One-to-many relationship
builder.HasMany(e => e.OrganizerContacts)
    .WithOne()
    .HasForeignKey(c => c.EventId)
    .OnDelete(DeleteBehavior.Cascade);

builder.Navigation(e => e.OrganizerContacts)
    .UsePropertyAccessMode(PropertyAccessMode.Field);
```

**Repository (`EventRepository.cs`):**
```csharp
// Added to GetByIdAsync only (not listing queries)
.Include(e => e.OrganizerContacts)
```

### 3.5 Relationship Diagram

```
┌──────────────────────────────┐
│       events.events          │
├──────────────────────────────┤
│  id (PK)                     │
│  title                       │
│  ...                         │
│  publish_organizer_contact   │  ← boolean flag stays here
│  ...                         │
│  ❌ organizer_contact_name   │  ← DROPPED
│  ❌ organizer_contact_email  │  ← DROPPED
│  ❌ organizer_contact_phone  │  ← DROPPED
└──────────────┬───────────────┘
               │ 1
               │
               │ *
┌──────────────┴───────────────┐
│ events.event_organizer_contacts │
├──────────────────────────────┤
│  id (PK)                     │
│  event_id (FK)               │  ← CASCADE DELETE
│  contact_name                │
│  contact_email               │
│  contact_phone               │
│  is_primary                  │
│  linked_user_id              │  ← future use
│  sort_order                  │
│  created_at                  │
│  updated_at                  │
└──────────────────────────────┘
```

---

## 4. Database Migration Strategy

**Migration:** `20260301000842_AddEventOrganizerContactsTable`

### Up() Steps (in order):

1. **CREATE TABLE** `events.event_organizer_contacts` with all columns and FK constraint
2. **DATA MIGRATION** — existing single contacts copied to new table:
   ```sql
   INSERT INTO events.event_organizer_contacts (id, event_id, contact_name, contact_email, contact_phone, is_primary, sort_order, created_at)
   SELECT gen_random_uuid(), id, organizer_contact_name, organizer_contact_email, organizer_contact_phone, true, 0, NOW()
   FROM events.events
   WHERE organizer_contact_name IS NOT NULL AND TRIM(organizer_contact_name) <> ''
   ```
3. **DROP COLUMN** `organizer_contact_email` from `events.events`
4. **DROP COLUMN** `organizer_contact_name` from `events.events`
5. **DROP COLUMN** `organizer_contact_phone` from `events.events`
6. **CREATE INDEX** `ix_event_organizer_contacts_event_id`

### Down() Steps:

1. Drop `event_organizer_contacts` table
2. Re-add the three scalar columns to `events.events`

> **Note:** The Down() does NOT migrate data back from the contacts table to the scalar columns. This is a one-way data migration.

---

## 5. API Contract Changes

### 5.1 Request Models

**Shared request record** (used by Create, Update, and dedicated UpdateOrganizerContact):

```csharp
public record OrganizerContactRequest(
    string ContactName,
    string? ContactEmail = null,
    string? ContactPhone = null,
    bool IsPrimary = false);  // Note: ignored by handlers, primary is position-based
```

**CreateEventCommand** parameters:
```csharp
bool? PublishOrganizerContact = false,
List<OrganizerContactRequest>? OrganizerContacts = null
```

**UpdateEventCommand** parameters:
```csharp
bool? PublishOrganizerContact = null,   // null = don't modify
List<OrganizerContactRequest>? OrganizerContacts = null
```

**UpdateEventOrganizerContactCommand** (dedicated endpoint):
```csharp
Guid EventId,
bool PublishOrganizerContact,           // required
List<OrganizerContactRequest> Contacts  // required
```

### 5.2 Response Model

```csharp
public record OrganizerContactDto {
    public Guid    Id           { get; init; }
    public string  ContactName  { get; init; } = string.Empty;
    public string? ContactEmail { get; init; }
    public string? ContactPhone { get; init; }
    public bool    IsPrimary    { get; init; }
    public int     SortOrder    { get; init; }
}
```

**EventDto** now carries:
```csharp
public bool PublishOrganizerContact { get; init; }
public IReadOnlyList<OrganizerContactDto> OrganizerContacts { get; init; } = Array.Empty<OrganizerContactDto>();
```

### 5.3 Handler Behavior Summary

| Handler | Guard Condition | Behavior |
|---------|----------------|----------|
| `CreateEventCommandHandler` | `PublishOrganizerContact == true && OrganizerContacts.Any()` | Sets contacts at creation time |
| `UpdateEventCommandHandler` | `PublishOrganizerContact.HasValue` | Replaces all contacts (or clears if `false`) |
| `UpdateEventOrganizerContactCommandHandler` | Always executes | Dedicated endpoint, replaces all contacts |

---

## 6. Frontend Changes

### 6.1 TypeScript Types (`events.types.ts`)

```typescript
export interface OrganizerContactDto {
  id: string;
  contactName: string;
  contactEmail?: string | null;
  contactPhone?: string | null;
  isPrimary: boolean;
  sortOrder: number;
}

export interface OrganizerContactRequest {
  contactName: string;
  contactEmail?: string | null;
  contactPhone?: string | null;
  isPrimary?: boolean;
}
```

### 6.2 Validation Schema (`event.schemas.ts`)

- `organizerContacts`: array of objects, optional, defaults to `[]`
- Each item: `contactName` (required, max 200), `contactEmail` (optional, email format), `contactPhone` (optional, max 20), `isPrimary` (boolean, default false)
- Cross-field refinement: when `publishOrganizerContact = true`, at least one contact required, each must have name + (email OR phone)

### 6.3 Form Components

**Both `EventCreationForm.tsx` and `EventEditForm.tsx`:**
- Use `useFieldArray` from react-hook-form for dynamic contact list
- Auto-populate first contact from user profile when checkbox first enabled
- "Add Another Contact" button appends empty contact
- "Remove" button on all contacts except index 0 (primary)
- At submission: `isPrimary` is overridden to `idx === 0` (always index-based)

### 6.4 Display Components

**`EventDetailsTab.tsx`** (organizer management view):
- Grid layout (140px label column)
- Shows "Primary" badge on primary contacts
- Email/phone as clickable `mailto:` / `tel:` links

**`page.tsx`** (public event page):
- `CollapsibleSection` wrapper, collapsed by default
- Flex layout
- Same "Primary" badge and clickable links

---

## 7. Test Coverage

### 7.1 Domain Tests (`EventOrganizerContactDetailsTests.cs`) — 35 tests

| Category | Tests | Coverage |
|----------|-------|----------|
| SetOrganizerContacts success cases | 5 | Single contact, multiple contacts, email-only, phone-only, unpublish clears all |
| SetOrganizerContacts validation | 6 | Empty list, blank name, no contact method, invalid email, invalid email theory (5 patterns) |
| Valid email format theory | 5 | Standard email formats including Azure ACS |
| Backward-compat properties | 2 | Primary contact delegation, null when empty |
| GetPrimaryContact | 2 | Returns primary, null when empty |
| HasOrganizerContact | 3 | True when published, false when unpublished, false when never set |
| Update scenarios | 2 | Replace all, switch contact method |
| Entity direct tests | 6 | Create, Update, Guid.Empty, empty name, no method, name > 200 chars |

### 7.2 Handler Tests (`UpdateEventOrganizerContactCommandHandlerTests.cs`) — 10 tests

| Test | Scenario |
|------|----------|
| Single contact success | All fields populated |
| Multiple contacts success | Two contacts, primary verification |
| Email-only contact | Phone is null |
| Phone-only contact | Email is null |
| Unpublish clears all | `PublishOrganizerContact = false` |
| Event not found | Returns failure, no commit |
| Empty name validation | Returns domain error |
| No contact method | Returns domain error |
| Invalid email | Returns domain error |
| Replace existing contacts | 1 → 2 contacts replacement |

### 7.3 Integration with Other Tests (`CancelEventCommandHandlerTests.cs`)

- `CreateTestEvent` helper updated to use `SetOrganizerContacts` (new API)
- Paid event cancellation still requires published organizer contact
- Three tests verify: paid without contact (fail), paid with contact (pass), free without contact (pass)

---

## 8. Backward Compatibility

### 8.1 Domain Layer

The `Event` aggregate maintains backward-compatible computed properties:

```csharp
public string? OrganizerContactName  => GetPrimaryContact()?.ContactName;
public string? OrganizerContactPhone => GetPrimaryContact()?.ContactPhone;
public string? OrganizerContactEmail => GetPrimaryContact()?.ContactEmail;
```

These are `builder.Ignore()`d in EF Core — they are never read from or written to the database. They always delegate to the primary contact in the collection.

**Impact:** Any existing code that reads `event.OrganizerContactName` (e.g., email handlers, cancellation logic) will automatically get the primary contact's name without code changes.

### 8.2 Database Layer

The migration copies existing single contacts into the new table before dropping the old columns. No data loss occurs.

### 8.3 API Layer

The `EventDto` response shape changes from:
```json
{
  "publishOrganizerContact": true,
  "organizerContactName": "John",
  "organizerContactEmail": "john@example.com",
  "organizerContactPhone": "555-1234"
}
```

To:
```json
{
  "publishOrganizerContact": true,
  "organizerContacts": [
    {
      "id": "...",
      "contactName": "John",
      "contactEmail": "john@example.com",
      "contactPhone": "555-1234",
      "isPrimary": true,
      "sortOrder": 0
    }
  ]
}
```

> **Breaking change:** The old flat fields (`organizerContactName`, `organizerContactEmail`, `organizerContactPhone`) are removed from the API response. Any external consumer relying on those fields will break.

---

## 9. Issues & Risks Identified

### 9.1 Critical Issues

| # | Issue | Severity | Details |
|---|-------|----------|---------|
| 1 | **No max contact count** | Medium | No UI or schema limit on contacts. A user could theoretically add hundreds. Should enforce a reasonable max (e.g., 10). |
| 2 | **`IsPrimary` on request is dead code** | Low | `OrganizerContactRequest.IsPrimary` exists but is never forwarded to the domain method. Primary is always index 0. Consider removing to avoid confusion. |
| 3 | **Migration Down() loses data** | Medium | The `Down()` method re-adds scalar columns but does NOT copy data back from the child table. Rollback will lose contact data. |
| 4 | **Two display components with different layouts** | Low | `EventDetailsTab` uses grid layout; `page.tsx` uses flex layout. Not a shared component — future styling changes need updating in both places. |
| 5 | **Contacts collapsed by default on public page** | Low | `CollapsibleSection defaultOpen={false}` means attendees might miss contact info. Consider defaulting to open. |

### 9.2 Potential Risks

| # | Risk | Mitigation |
|---|------|------------|
| 1 | **EF Core change tracking with `Clear()` + `AddRange()`** | This is a relational child entity (not JSONB), so standard EF Core change tracking handles additions/deletions correctly. The Memory.md JSONB issue (Phase 6A.129) does NOT apply here. Verified: the entity is loaded with `trackChanges: true`. |
| 2 | **`OrganizerContacts` not loaded in listing queries** | By design — `GetAllAsync`, `SearchAsync` etc. do not `.Include(e => e.OrganizerContacts)`. This is correct for performance. Only `GetByIdAsync` loads contacts. |
| 3 | **Breaking API response change** | The old flat fields are removed. If any mobile app or external integration uses them, it will break. Mitigated by the fact that LankaConnect is still in staging. |
| 4 | **Email handlers depending on `OrganizerContactName`** | Mitigated by backward-compat computed properties that delegate to `GetPrimaryContact()`. No email handler code changes needed. |

### 9.3 Missing Pieces (Future Work)

| Feature | Status | Notes |
|---------|--------|-------|
| `LinkedUserId` support | Column exists, not wired | Future: link contacts to registered LankaConnect users for co-organizer management |
| User can designate primary | Not implemented | Currently always index 0. UI has no mechanism to choose a different primary. |
| Contact role/title | Not implemented | E.g., "Event Coordinator", "Ticket Support" |
| Per-contact visibility | Not implemented | Currently all-or-nothing via `PublishOrganizerContact` flag |

---

## 10. Files Changed Summary

### New Files (5)

| File | Purpose |
|------|---------|
| `src/LankaConnect.Domain/Events/Entities/EventOrganizerContact.cs` | Child entity for organizer contacts |
| `src/LankaConnect.Application/Events/Common/OrganizerContactDto.cs` | Response DTO |
| `src/LankaConnect.Infrastructure/Data/Configurations/EventOrganizerContactConfiguration.cs` | EF Core table config |
| `src/LankaConnect.Infrastructure/Data/Migrations/20260301000842_AddEventOrganizerContactsTable.cs` | Migration |
| `src/LankaConnect.Infrastructure/Data/Migrations/20260301000842_AddEventOrganizerContactsTable.Designer.cs` | Migration designer |

### Modified Files — Backend (11)

| File | Change |
|------|--------|
| `Event.cs` | Scalar props → collection + computed props + new domain methods |
| `EventConfiguration.cs` | Ignore computed props, add HasMany relationship |
| `EventRepository.cs` | Add `.Include(e => e.OrganizerContacts)` |
| `AppDbContext.cs` | Register `EventOrganizerContactConfiguration` |
| `AppDbContextModelSnapshot.cs` | Updated snapshot with new entity |
| `EventMappingProfile.cs` | Add mapping for `EventOrganizerContact` → `OrganizerContactDto` |
| `EventDto.cs` | Replace flat fields with `OrganizerContacts` collection |
| `CreateEventCommand.cs` | Add `OrganizerContacts` parameter |
| `CreateEventCommandHandler.cs` | Call `SetOrganizerContacts` |
| `UpdateEventCommand.cs` | Add `OrganizerContacts` parameter |
| `UpdateEventCommandHandler.cs` | Call `SetOrganizerContacts` |
| `UpdateEventOrganizerContactCommand.cs` | Refactored to use `OrganizerContactRequest` list |
| `UpdateEventOrganizerContactCommandHandler.cs` | Refactored for multiple contacts |

### Modified Files — Frontend (5)

| File | Change |
|------|--------|
| `events.types.ts` | New `OrganizerContactDto` and `OrganizerContactRequest` interfaces |
| `event.schemas.ts` | Array-based validation for `organizerContacts` |
| `EventCreationForm.tsx` | `useFieldArray` for dynamic contact list |
| `EventEditForm.tsx` | `useFieldArray` for dynamic contact list |
| `EventDetailsTab.tsx` | Render multiple contacts with "Primary" badge |
| `page.tsx` (event details) | Render multiple contacts in `CollapsibleSection` |

### Modified Test Files (3)

| File | Change |
|------|--------|
| `UpdateEventOrganizerContactCommandHandlerTests.cs` | Updated for multi-contact API |
| `CancelEventCommandHandlerTests.cs` | Helper uses `SetOrganizerContacts` |
| `EventOrganizerContactDetailsTests.cs` | 35 domain tests for new entity + methods |

---

## 11. Recommendations

### Before Merging

1. **Add a max contact limit** — Enforce max 10 contacts in both domain validation (`SetOrganizerContacts`) and frontend schema. Prevents abuse and keeps the UI manageable.

2. **Remove or document `IsPrimary` on `OrganizerContactRequest`** — It's confusing dead code. Either remove it from the request model (since primary is always index-based) or implement proper primary designation in the future.

3. **Verify all email handlers** — While backward-compat computed properties should work, do a grep for `OrganizerContactName`, `OrganizerContactEmail`, `OrganizerContactPhone` across the codebase to confirm no code path requires these to be database columns.

4. **Consider keeping old fields in API response** — For a transition period, include both `organizerContactName` (from primary) and `organizerContacts[]` in the `EventDto` to avoid breaking any consumers. Mark the old fields as `[Obsolete]`.

### After Merging

5. **Test data migration** — Verify the `INSERT...SELECT` migration correctly copies existing contacts from the staging database.

6. **Update email templates** — If any email templates reference organizer contact directly (not via domain properties), they may need updates.

7. **Monitor EF Core SQL** — Verify that `SetOrganizerContacts` generates correct `DELETE` + `INSERT` SQL for the child entities (not silent no-ops).

---

**This document is an analysis only. No code changes have been made as part of this document.**
