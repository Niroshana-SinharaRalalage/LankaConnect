# Master TODO — Phase 7F sub-feature A: Mode-B head-count rendering on lifecycle emails

**Status**: ✅ **SHIPPED + STAGING-VERIFIED** (2026-04-30).
**Backend deploy**: `25145447580` `conclusion=success`.
**DB verification**: all 3 templates contain `attendee-block-7e` anchor; lengths grew exactly +7272 chars (78778, 91884, 93210). Backup table has all 3 pre-7F-A bodies for rollback.
**Architect review iteration**: 1 (5 edits applied; scope tightened from 6 templates to 3 actually-existing templates).
**Commits**: `1e7678f3` (Slice 1: handler + params), `fcde946a` (Slice 2: template HTML migration).
**Backend deploy**: in flight as of commit push.

---

## 1. Scope correction during pre-condition checks

Architect plan §6.2 inventory listed 6 lifecycle templates needing Mode-B updates. Code reality: only **3 exist in production today**. The other 3 (`event-waitlist-promoted`, `event-registration-modified`, `organizer-new-registration-notification`) are aspirational placeholders — no email-params class, no handler, no DB row. They'll ship when/if those features are built (separate work, not Phase 7F-A).

**3 templates updated in this slice**:
1. `template-event-cancellation-notifications` (organiser-cancels-event broadcast — `EventCancellationEmailJob`)
2. `template-event-reminder` (cron — `EventReminderJob`)
3. `template-attendees-added-confirmation` (`AttendeesAddedEventHandler`)

---

## 2. Architect-required pre-conditions (all clean)

| Item | Status |
|---|---|
| **#3 Mode C silent** | ✅ Both `EventCancellationEmailJob` (line 122) and `EventReminderJob` (line 145) iterate `event.Registrations`. Mode C events have 0 registrations → loops execute 0 times → templates never rendered for Mode C. Naturally silent, no explicit guard added. |
| **#6 Pin DB row names** | ✅ `psycopg2`-probed staging on 2026-04-30 — confirmed all 3 rows exist with sizes 84612 / 85938 / 71506 chars. |
| **#7 LeadAttendeeName at waitlist-promotion** | ✅ N/A — waitlist email infrastructure doesn't exist (deferred). |

---

## 3. Slice 1 — handler + params (commit `1e7678f3`)

**Changed (3 files in Shared, 3 files in Application)**:
- `EventCancellationEmailParams`, `EventReminderEmailParams`, `AttendeesAddedEmailParams` — added Phase 7F-A region with 7 Flexible* booleans/strings + `LeadAttendeeName`. `ToDictionary` always emits the 8 keys (booleans true AND false, never omitted) per architect rule.
- `EventCancellationEmailJob` — per-recipient registration lookup (`user.Id → confirmedRegistrations row`) feeds `HeadCountEmailFormatter.Compute`. Non-registration recipients (sign-up users, newsletter subscribers) leave Flexible* fields at defaults — Mode-A block falls through. Try/catch fail-soft.
- `EventReminderJob` — both reminder-send branches (lines ~221 + ~437) call the formatter. Try/catch fail-soft.
- `AttendeesAddedEventHandler` — already has `registration` in scope (line 92). Try/catch fail-soft.

**Tests**: 5 new (`Phase7FA_FlexibleRegistrationParamsTests`) — assert each params class's `ToDictionary` emits all 8 Flexible keys for Mode-A defaults AND when explicitly set to Mode-B values. Application suite **2432 / 6 skipped / 0 failed**.

---

## 4. Slice 2 — template HTML migration (commit `fcde946a`)

**Method**:
1. `psycopg2`-probed staging at 2026-04-30 to capture authoritative bodies (per memory `feedback_template_body_is_authoritative` — never trust migration history).
2. Located `{{#if HasOrganizerContact}}` anchor in each (positions 58509 / 65496 / 51080).
3. Inserted the Phase 7E.4 chunk 1 Mode-B card snippet (7271 chars; anchor-wrapped with `<!-- attendee-block-7e --> ... <!-- /attendee-block-7e -->`) immediately before the `HasOrganizerContact` block.
4. Saved 3 v2 bodies as embedded resources in `Resources/Phase7F_A/*.html`.

**Migration**: `Phase7F_A_FlexibleRegistrationLifecycleTemplates` (scaffolded via `dotnet ef migrations add`).
- Up: defensive `CREATE IF NOT EXISTS` on `communications.email_template_backups` (created in 7E.4 chunk 1; defensive for fresh DBs), per-template backup INSERT + parameterised UPDATE with v2 HTML loaded via `Phase7FATemplates.LoadHtml` (embedded resource — MEMORY 6A.129b: never `File.ReadAllText` in migrations).
- Down: restore each template body from the backup row. Idempotent.

---

## 5. End-to-end staging API smoke (post-deploy)

Pending. Plan:

1. Get fresh token.
2. Use the existing free-Mode-B2 staging event (`16eeb15c-…`) OR the paid B2 event (`749013e8-…`).
3. **Cancellation broadcast**: cancel the event via `POST /api/events/{id}/cancel` → fetch the ACS sent log → assert the email body contains the Mode-B head-count line ("Lead: X · Total: 5 · 2 adults · 1 child").
4. **Reminder cron**: trigger via `POST /api/admin/event-reminders/run` (if endpoint exists) → fetch ACS log → assert head-count line present.
5. **Attendees-added**: this fires only on Mode A today; expect no Mode-B emission yet. Verify Mode-A regression baseline unchanged.

DB verification via `psycopg2`:
```python
SELECT name, length(html_template), position('attendee-block-7e' in html_template) > 0 AS has_block
FROM communications.email_templates
WHERE name IN (
    'template-event-cancellation-notifications',
    'template-event-reminder',
    'template-attendees-added-confirmation'
);
```
Expected: 3 rows, all `has_block = true`, lengths increased by ~7272.

---

## 6. Out of scope (separate ticket if/when needed)

- `event-waitlist-promoted` (no waitlist infrastructure today)
- `event-registration-modified` (UpdateRsvp rejects B/C anyway in 7E.3a)
- `organizer-new-registration-notification` (no separate template)
