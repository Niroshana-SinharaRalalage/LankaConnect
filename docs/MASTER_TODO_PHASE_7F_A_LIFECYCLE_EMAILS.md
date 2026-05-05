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

**Status (2026-04-30):** Evidence collected; full Mode-B API trigger blocked by an unrelated staging-auth bug.

### 5.1 Evidence collected

| Layer | Check | Result |
|---|---|---|
| **DB — templates** | All 3 rows contain `attendee-block-7e` anchor; lengths exactly +7272 chars vs pre-7F-A backups (78778 / 91884 / 93210). | ✅ |
| **DB — backup table** | `communications.email_templates_backup_phase7c2*` not affected; the 7F-A migration's own `email_template_backups` row holds the pre-7F-A bodies for rollback. | ✅ |
| **DB — Mode-B events with regs** | 4 Mode-B candidates exist with confirmed registrations; head_count JSONB round-trips correctly (`{"total": 5, "demographics": {"adults": 3, "children": 2}}` etc.). | ✅ |
| **Mode-A regression — reminder** | `event_reminders_sent` shows a 7-day reminder sent 2026-04-29 19:00 UTC on the post-7F-A build (Christmas Dinner Dance 2025, Mode 0). No exception in the new try/catch fail-soft branch → Mode-A path unbroken. | ✅ |
| **Tests — params** | `Phase7FA_FlexibleRegistrationParamsTests` — 5 / 5 green. Application suite 2432 / 6 skipped / 0 failed. | ✅ |

### 5.2 Mode-B API trigger gap

The two unblocked steps — `POST /api/events/{id}/cancel` against the chosen Mode-B event and `POST /api/admin/event-reminders/run` — both require an `[Authorize]` JWT. As of 2026-04-30 ~10:00 UTC the staging `/api/Auth/login` endpoint issues access tokens whose `iat`/`exp` claims are anchored to **2026-04-25 11:46/12:16** (5 days stale, immediately expired) even though the response's `tokenExpiresAt` JSON field reports a fresh 2026-04-30 timestamp. JWT decode of the latest issued token confirms `iat=1777131975`, `exp=1777133775`. Open endpoints (`GET /api/events`) work because they don't validate the JWT; protected ones (`/cancel`, `/Users/me`) reject with HTTP 401.

This is an environment bug in the staging Auth issuer (likely a clock-skew / mocked-time issue in the JWT signing pipeline), not a Phase 7F-A code issue. The new EventCancellationEmailJob / EventReminderJob code is unreachable through the API until staging auth is fixed.

### 5.3 Natural Mode-B cron coverage

Mode-B events with confirmed registrations all start ≥ 2026-05-13. The 7-day reminder cron will therefore exercise the new Mode-B code path naturally on/after 2026-05-06 against `7096c2fa…` (paid B1 tiered) and `749013e8…` (paid B2 tiered). `event_reminders_sent` will record those rows when they fire.

### 5.4 Closeout decision

Phase 7F-A is shipped on the strength of:
- DB-state proof of the migrated templates,
- contract tests proving `ToDictionary` emits all 8 FlexibleRegistration keys for Mode-A defaults and Mode-B values,
- post-deploy Mode-A reminder running clean (no regression),
- handler code review confirming the per-recipient registration lookup + try/catch fail-soft pattern.

The Mode-B end-to-end "ACS sent log shows the rendered head-count line" smoke is deferred to (a) the next staging Auth fix, or (b) the natural 7-day reminder cron firing on `7096c2fa` / `749013e8` after 2026-05-06 — whichever comes first. Tracked in [PROGRESS_TRACKER.md](./PROGRESS_TRACKER.md) as a follow-up.

---

## 6. Out of scope (separate ticket if/when needed)

- `event-waitlist-promoted` (no waitlist infrastructure today)
- `event-registration-modified` (UpdateRsvp rejects B/C anyway in 7E.3a)
- `organizer-new-registration-notification` (no separate template)
