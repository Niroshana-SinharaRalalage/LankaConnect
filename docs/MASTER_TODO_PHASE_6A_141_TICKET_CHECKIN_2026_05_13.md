# Phase 6A.141 — Paid-Event Ticket Check-in / QR Scanner

**Date opened:** 2026-05-13
**Branch:** `feat/phase-6a-141-ticket-checkin` off `main`
**Commits so far:** `dd8648df` (foundation: signed payload VO + HMAC signature service, 27 tests GREEN)
**Status:** 🔧 In progress — Phase A + B foundation shipped; Phase C blocked pending **corrections** from independent Plan-agent review below.

## Goal in one sentence

Make the QR code on paid-event tickets actually scannable at the gate by an organizer's phone, so two people can't walk in with one screenshot, forged QRs are rejected before the DB is touched, and every scan attempt (accepted or rejected) is in an audit table for dispute resolution.

## Decisions locked-in by product owner (2026-05-13)

| # | Decision | Locked |
|---|---|---|
| D1 | **One QR per registration** (not per attendee). Scanner shows party breakdown. | ✅ |
| D2 | **HMAC-SHA256 signed payload** (format `v1.body.sig`). Not JWT, not scan-URL. | ✅ |
| D3 | **Lazy-migrate legacy unsigned QRs** during grace window; accept both at decode. | ✅ |
| D4 | **Reuse existing organizer/co-organizer auth** for gate staff. No new role. | ✅ |
| D5 | **HTTP 200 with `result:"rejected"` body** for business rejections; HTTP 4xx only for protocol/auth failures. | ✅ |
| D6 | **Show tier on scanner, don't enforce per-gate** (no VIP-only doors). | ✅ |
| D7 | **No offline queue** — clear "no network" UX. Correctness > convenience. | ✅ |
| D8 | **Phase renumbering** — old 6A.141 (orphan backfill / auth trust-boundary / rate limit) → 6A.142. Scanner gets 6A.141. | ✅ |

## Independent architect review findings (Plan agent, 2026-05-13)

These corrections **must be incorporated** before Phase C / F implementation. Severities:

### 🔴 Must-fix-before-Phase-C

| # | Finding | Action |
|---|---|---|
| F1 | **Race-safe UPDATE pattern doesn't work via EF change tracker.** `IRepository.Update(ticket)` emits `UPDATE … WHERE Id = @id` only — no `validated_at IS NULL` predicate, so two parallel scans both succeed silently. | Add `ITicketRepository.TryMarkScannedAsync(id, now, ct)` using EF Core 7+ `ExecuteUpdateAsync` with `.Where(t => t.Id == id && t.ValidatedAt == null)`. Returns affected-row count; handler treats `0` as race-loser and reloads to classify. |
| F2 | **Audit log atomicity.** `ExecuteUpdateAsync` bypasses change tracker → audit-log insert is in a separate transaction → on partial failure, ticket is marked scanned but audit row missing. | Wrap the mark-scanned + audit-write in explicit `BeginTransactionAsync` / `CommitTransactionAsync`. Reject-attempt audit writes (forgery, malformed) happen OUTSIDE the transaction (no state change to atomically tie to). |
| F3 | **`client_ip` collection from `X-Forwarded-For`.** Naive `HttpContext.Connection.RemoteIpAddress` returns Azure Front Door's internal IP → useless for forensics. | Check existing controllers (Stripe webhook, audit log) for the project's X-Forwarded-For helper; reuse. |
| F4 | **`Ticket.GenerateQrCodeData` refactor location.** Plan said "factory or in TicketService — pick" but didn't. | **Pick `TicketService`.** Change `Ticket.Create` signature to accept `qrCodeData` as a constructor parameter; delete the private `GenerateQrCodeData`. `TicketService` builds the signed payload via `TicketSignedPayload.CreateV1(...).EncodeWithSignature(_signatureService.Sign(...))` and passes the string in. Avoids dragging Application-layer service interface into Domain. |

### 🔴 Must-fix-before-Phase-F

| # | Finding | Action |
|---|---|---|
| F5 | **HMAC secret rotation gap.** `HmacTicketSignatureService` holds ONE secret. Rotation = container restart = ALL in-flight signed v1 QRs become invalid mid-event. Catastrophic on event day. | Upgrade to dual-key verify, single-key sign: read `Tickets:QrSigningKey` (current, used for both sign + verify) + optional `Tickets:QrSigningKeyPrevious` (verify-only fallback). Standard JWT-style key-rollover pattern. Update tests + add `verified-with-previous` flag for audit log. |
| F6 | **Phase J pre-flight: provision `TICKET-QR-SIGNING-KEY` in PROD Key Vault BEFORE the API deploy.** If the secret is missing at startup, `HmacTicketSignatureService` throws `InvalidOperationException` on first DI resolution → API container won't start. | Add explicit step in Phase I/J runbook: `az keyvault secret set --vault-name lankaconnect-kv-{env} --name TICKET-QR-SIGNING-KEY --value $(openssl rand -base64 32)` BEFORE firing the deploy workflow. |

### 🟡 Should-address-before-Phase-F

| # | Finding | Action |
|---|---|---|
| F7 | **Legacy QR + freshness check.** Legacy payload has no `iat`. If a future freshness-window check is added, must explicitly skip for legacy. | Add comment in `TicketSignedPayload.TryParseLegacy` warning that `IssuedAtUnixSeconds == 0` for legacy and downstream callers must guard. |
| F8 | **Non-primary co-organizer scope on scan endpoint not tested.** | Add to UAT checklist: have a co-organizer (non-primary, linked via OrganizerContacts) attempt to scan; must succeed. |
| F9 | **Smoke matrix gaps.** Expand the 6 scenarios to ~12 to cover every reason code. | See § Smoke matrix below — `expired`, `invalidated`, `ticket_not_found` via manual entry, `concurrent_double_scan`, `co_organizer`, `malformed_payload` added. |
| F10 | **`rejection_reason varchar(64)` too tight.** Codes fit but future codes might not. | Bump column to `varchar(128)`. Cheap. |
| F11 | **`scan_result` enum missing `'unmarked'`** (admin override path). | Add `'unmarked'` to the enum / allowed values. |
| F12 | **No need for `ValidatedByUserId` column on `Ticket`.** Audit log already captures `scanner_user_id`. | Audit log only. Skip the column. |
| F13 | **Result DTO must render breakdown based on `Ticket.TicketCategory`, not blindly on `Registration.Attendees`.** Locks in future `CreateTiered` compat. | When `TicketCategory == Individual`, return just that one attendee in the breakdown. When `Standard`, return full registration. Cheap to get right now. |

### 🟡 Should-address-before-Phase-I

| # | Finding | Action |
|---|---|---|
| F14 | **Camera-denied panel.** Scanner UI plan mentions network-loss yellow panel but not camera-denied. | Add equivalent yellow panel for camera-permission-denied / no-camera-available. Make manual-entry modal prominent. |
| F15 | **Continuous-scan cooldown keyed on `lastScannedCode`, not just time.** Phone-displayed QRs that re-render can re-trigger on the same code within 2s. | Use `lastScannedCode == currentCode` AND `lastScanAt + 2s > now` as the debounce condition. |

### 🟢 Nice-to-have / out-of-scope-for-6A.141

| # | Finding | Action |
|---|---|---|
| F16 | Code-split scanner route so `html5-qrcode` (~80KB) doesn't bloat main bundle. | Next.js dynamic import. |
| F17 | Real-time "X of Y checked in" dashboard for organizer. | Phase 6A.143 candidate. Audit log makes this a trivial query. |
| F18 | Dedicated `ScannerRole`-scoped token (event-day only, no edit rights) for hired gate staff who aren't organizers. | Phase 6A.144 candidate. Out of scope today — workaround is co-organizer add. |

## Phase task checklist

### ✅ Phase A — TicketSignedPayload value object (DONE — `dd8648df`)

- [x] Failing tests written first (RED)
- [x] `TicketSignedPayload` VO with `CreateV1` + `EncodeWithSignature` + `TryParse` (v1 + legacy)
- [x] 19 tests GREEN

### ✅ Phase B — HMAC signature service (DONE — `dd8648df`)

- [x] `ITicketSignatureService` interface in Application
- [x] `HmacTicketSignatureService` impl in Infrastructure (HMAC-SHA256, constant-time compare, fail-fast)
- [x] DI registration (Singleton)
- [x] 8 tests GREEN
- [ ] **F5: dual-key verify, single-key sign upgrade** (move to before Phase F)

### Phase C — Ticket entity update & `TicketService` signed-payload generation

- [ ] (RED) Write failing tests covering: `Ticket.Create` accepts `qrCodeData` parameter; `TicketService.GenerateTicketAsync` produces a v1 signed payload using `ITicketSignatureService`; legacy decoder still parses pre-141 tickets.
- [ ] **F4:** Refactor `Ticket.Create` to accept `qrCodeData` as a parameter; delete private `GenerateQrCodeData`. Keep public static `DecodeQrCodeData` (used elsewhere? grep first).
- [ ] Update `TicketService.GenerateTicketAsync` to inject `ITicketSignatureService` and build the v1 signed payload before calling `Ticket.Create`.
- [ ] Update `Ticket.CreateTiered` similarly (even though it's dead code, keep it consistent for future activation per F13).
- [ ] Run domain + infrastructure tests → GREEN.

### Phase D — Audit log table + entity + EF migration

- [ ] (RED) Write failing tests for `TicketScanLog` domain entity (factory methods for accepted / rejected / unmarked variants).
- [ ] `TicketScanLog` entity in `LankaConnect.Domain.Events.Entities`.
- [ ] EF config in `LankaConnect.Infrastructure.Data.Configurations.TicketScanLogConfiguration`.
- [ ] EF migration `Phase6A141_AddTicketScanLog` creating `events.ticket_scan_log`:
  - `id uuid PK`
  - `ticket_id uuid NULL FK` (soft reference — invalid-signature attempts have no ticket)
  - `event_id uuid NOT NULL`
  - `ticket_code varchar(20) NULL`
  - `scanner_user_id uuid NOT NULL FK → users.id`
  - `scanner_name varchar(200)` (denormalized snapshot)
  - `scan_result varchar(32) NOT NULL` — `'accepted'` | `'rejected'` | `'unmarked'` per **F11**
  - `rejection_reason varchar(128) NULL` per **F10**
  - `entry_method varchar(16) NOT NULL` — `'qr'` | `'qr_legacy'` | `'manual'` | `'admin_unmark'`
  - `client_ip inet NULL`
  - `user_agent varchar(500) NULL`
  - `created_at timestamptz NOT NULL DEFAULT now()`
- [ ] Indices: `IX_ticket_scan_log_ticket_id`, `IX_ticket_scan_log_event_id_created_at`, `IX_ticket_scan_log_scanner_user_id_created_at`.
- [ ] **CRITICAL: verify `[Migration("...")]` attribute is present in `.Designer.cs`** (per project memory rule — EF Core discovers migrations only via this attribute).
- [ ] `ITicketScanLogRepository` + impl + DI.
- [ ] Run `dotnet ef database update` against local staging-copy DB to verify Up + Down both succeed.

### Phase E — Application command + race-safe UPDATE + atomic audit

- [ ] (RED) Write 11 failing tests covering 8 reason codes + 3 happy paths.
- [ ] **F1:** `ITicketRepository.TryMarkScannedAsync(id, now, ct)` using `ExecuteUpdateAsync(.Where(t => t.Id == id && t.ValidatedAt == null)…)` → returns `int` row count.
- [ ] `ScanTicketCommand(EventId, QrPayload, ScannerUserId, ClientIp, UserAgent)` + `ScanTicketByCodeCommand(...)` for manual entry.
- [ ] Handler logic:
  1. Parse via `TicketSignedPayload.TryParse` → `malformed_payload` if null.
  2. If v1 → verify HMAC via `ITicketSignatureService.Verify` (now dual-key per F5) → `invalid_signature` on fail; log audit row OUTSIDE transaction per **F2**.
  3. If legacy → flag `entry_method=qr_legacy`; proceed.
  4. Look up ticket by code → `ticket_not_found`.
  5. Compare event IDs → `wrong_event` (include ticket's actual event title in response for staff context).
  6. **F2:** `BeginTransactionAsync`. `TryMarkScannedAsync` → row count `0` = reload, classify into `already_scanned` / `expired` / `invalidated`. Row count `1` = build accepted result.
  7. Insert audit-log row (in same transaction).
  8. `CommitTransactionAsync`.
- [ ] **F13:** Result DTO renders breakdown from `Ticket.TicketCategory`-aware projection.
- [ ] All 11 tests GREEN.

### Phase F — API endpoints + integration tests

- [ ] (RED) Write 8 integration tests (real DB via WebApplicationFactory).
- [ ] `POST /api/Events/{eventId}/tickets/scan` — `[Authorize]` + `IsCurrentUserOrganizer` check. Body `{ qrPayload: "..." }`.
- [ ] `POST /api/Events/{eventId}/tickets/scan-by-code` — manual-entry fallback. Body `{ ticketCode: "LC-2026-..." }`.
- [ ] `POST /api/Events/{eventId}/tickets/{ticketCode}/unmark-scanned` — admin override. Body `{ reason: "..." }`. Writes audit row with `scan_result='unmarked'`.
- [ ] **F3:** Extract client IP via X-Forwarded-For helper (reuse existing project pattern — grep first).
- [ ] All HTTP-200 with `result:"accepted"|"rejected"|"unmarked"` body; HTTP 4xx only for protocol/auth.
- [ ] 8 integration tests GREEN.

### Phase G — Scanner UI (web)

- [ ] (RED) 7 frontend tests covering: page render for organizer, redirect non-organizer, POST on decode, accepted panel, rejected panel, network-loss yellow panel, manual-entry modal.
- [ ] Install `html5-qrcode` dependency.
- [ ] New page `web/src/app/events/[id]/manage/scan/page.tsx`.
- [ ] Auth-on-mount: GET event → redirect with toast if `!isCurrentUserOrganizer`.
- [ ] Continuous-scan with **F15** cooldown (lastScannedCode + 2s).
- [ ] Big green / red / yellow panels with attendee name, tier, party breakdown.
- [ ] Audio + vibrate (gated behind settings toggle).
- [ ] **F14:** Camera-denied yellow panel + prominent manual-entry CTA.
- [ ] Manual-entry modal — uses `/scan-by-code` endpoint.
- [ ] **F16:** Next.js dynamic import for `html5-qrcode` to code-split.

### Phase H — Build, full test sweep, tracking docs, commit

- [ ] `dotnet build` clean.
- [ ] `npx tsc --noEmit` clean.
- [ ] Full domain + application + infrastructure + integration + web test suites all GREEN.
- [ ] Update `docs/PROGRESS_TRACKER.md`, `docs/STREAMLINED_ACTION_PLAN.md`, `docs/TASK_SYNCHRONIZATION_STRATEGY.md` with shipping status.
- [ ] Per-file `git add` (no `-A`).
- [ ] Commit per architect convention; push to `feat/phase-6a-141-ticket-checkin`.

### Phase I — Staging deploy + smoke + UAT

- [ ] **F6 pre-flight:** `az keyvault secret set --vault-name lankaconnect-kv-staging --name TICKET-QR-SIGNING-KEY --value $(openssl rand -base64 32)`.
- [ ] `gh workflow run deploy-staging.yml --ref feat/phase-6a-141-ticket-checkin`.
- [ ] `gh run watch` until backend deploy GREEN.
- [ ] Verify active Container App revision = my SHA (post-race-guard from 6A.140 lesson).
- [ ] **Verify DB migration applied:** query `events.ticket_scan_log` table exists, indices created.
- [ ] Run smoke matrix (see below).
- [ ] `gh workflow run deploy-ui-staging.yml --ref feat/phase-6a-141-ticket-checkin`.
- [ ] Verify UI deploy GREEN.
- [ ] Hand UAT checklist to product owner (see below).

### Phase J — Production deploy

- [ ] **F6 pre-flight:** `az keyvault secret set --vault-name lankaconnect-kv-prod --name TICKET-QR-SIGNING-KEY --value $(openssl rand -base64 32)` — **different key from staging**.
- [ ] Open release-branch PR: `feat/phase-6a-141-ticket-checkin` → next release branch (`Production_05_15_2026` per architect rec).
- [ ] Merge to release branch.
- [ ] `gh workflow run "Deploy to Azure Production" --ref Production_05_15_2026`.
- [ ] Watch + verify prod revision = release-branch HEAD.
- [ ] `gh workflow run "Deploy UI to Azure Production" --ref Production_05_15_2026`.
- [ ] Prod smoke (subset of staging smoke against prod API URL).
- [ ] Operator browser UAT on prod.

## Smoke matrix (≥12 cells — must run before flipping staging-verified)

Authenticated token via the project's standard login curl (`niroshhh@gmail.com` test member account):

| # | Cell | Endpoint | Expected `result` | Expected `reason` |
|---|---|---|---|---|
| S1 | Happy path: valid signed QR scanned by organizer | `POST /scan` | `accepted` | — |
| S2 | Replay: scan S1's QR a second time | `POST /scan` | `rejected` | `already_scanned` |
| S3 | Forgery: tamper one byte in the signature segment | `POST /scan` | `rejected` | `invalid_signature` |
| S4 | Wrong event: send a valid signed QR from Event A to Event B's `/scan` | `POST /scan` | `rejected` | `wrong_event` |
| S5 | Legacy QR (pre-141 ticket): scan unsigned base64 payload | `POST /scan` | `accepted` (with `entry_method=qr_legacy` in audit) | — |
| S6 | Network loss UX: scanner UI shows yellow panel | (frontend) | n/a | n/a |
| S7 | **Expired ticket** (event ended >24h ago) | `POST /scan` | `rejected` | `expired` |
| S8 | **Invalidated ticket** (`IsValid=false` from a refund) | `POST /scan` | `rejected` | `invalidated` |
| S9 | **Ticket-not-found** via manual entry (typo'd code) | `POST /scan-by-code` | `rejected` | `ticket_not_found` |
| S10 | **Concurrent double-scan** (two HTTP calls within 100ms for same valid ticket) | `POST /scan` ×2 | one `accepted`, one `rejected` with `already_scanned` | — |
| S11 | **Co-organizer (non-primary) scope** — non-primary co-organizer hits `/scan` | `POST /scan` | `accepted` | — |
| S12 | **Malformed payload** — gibberish `qrPayload` string | `POST /scan` | `rejected` | `malformed_payload` |
| S13 | **Admin unmark** — admin reverses a wrongly-scanned ticket | `POST /unmark-scanned` | `unmarked` (audit row written with method=`admin_unmark`) | — |

After each scan, verify a row exists in `events.ticket_scan_log` with the expected `scan_result` + `rejection_reason` + `entry_method`.

## Operator UAT checklist (browser, staging)

Hand to product owner only after smoke matrix is fully GREEN.

- [ ] U1 — Open the manage page of a paid event you organize; click "Scan Tickets" CTA; scanner page loads with camera viewfinder.
- [ ] U2 — Scan your own valid ticket: large green panel appears with your name, tier, "scanned at HH:MM:SS".
- [ ] U3 — Scan the same QR again: large red panel "Already scanned at HH:MM:SS".
- [ ] U4 — Toggle audio + vibrate settings; verify both work.
- [ ] U5 — Tap "Enter ticket code manually"; type a valid ticket code; succeeds.
- [ ] U6 — Tap "Enter ticket code manually"; type `LC-2026-NOPE99` (invalid); large red panel "Ticket not found".
- [ ] U7 — **F8 co-organizer scope:** add a second account as co-organizer to your event; have THAT user open the scan page; verify they can scan.
- [ ] U8 — Deny camera permission in browser settings; reload page; verify yellow camera-denied panel + manual-entry CTA prominent.
- [ ] U9 — Disable wifi briefly; scan a QR; verify yellow "no network" panel (NOT green/red).
- [ ] U10 — Confirmation: PDF tickets generated AFTER deploy show a v1 signed QR (base64-decode the QR — should start with `v1.`); PDFs generated BEFORE deploy show legacy format (no `v1.` prefix); both scan successfully.

## Rollback paths

- **Code revert:** single `git revert <release-sha>` on the release branch + redeploy.
- **QR generation rollback only (keep scan endpoint live):** feature flag `TicketSignatureV1Enabled=false` falls back `Ticket.Create` to legacy QR generation; existing signed QRs still scan via legacy decoder.
- **HMAC key leaked:** generate new secret + set as `TICKET-QR-SIGNING-KEY`; set OLD secret as `TICKET-QR-SIGNING-KEY-PREVIOUS` (per **F5**) for grace; container restart; eventually drop the previous secret.
- **Wrongly-scanned ticket:** organizer hits admin-unmark endpoint with a reason; audit row records the override.

## Memory rules in force

- `feedback_post_deploy_api_test.md` — curl every changed endpoint after deploy; smoke matrix is the proof.
- `feedback_consult_architect.md` — done (Plan agent independent review, 2026-05-13, findings above).
- `feedback_no_false_attribution.md` — GitHub audit fields = whoever owns the gh token; don't accuse user of programmatic actions.
- `feedback_stop_asking_execute.md` — user already authorized "TDD red-first now"; no more approval gates until shipped.
- `feedback_plain_language.md` — every user-facing report uses plain English, not engineer jargon.

## Current status (as of doc creation)

- **Phase A + B:** DONE (commit `dd8648df`), 27 tests GREEN
- **Phase C blocked on:** F1 (race-safe UPDATE pattern), F2 (audit log atomicity), F4 (`Ticket.Create` refactor location), and incorporating F5 dual-key into the signature service before Phase F
- **Branch:** `feat/phase-6a-141-ticket-checkin` pushed
- **No PR open yet:** PR opens at Phase H when staging-verified

## Sign-off log

| Event | Date | Notes |
|---|---|---|
| Phase plan drafted (architect agent — `claude` general) | 2026-05-13 | 16 sections, 7 PO decisions surfaced |
| Product owner approval | 2026-05-13 | All 7 decisions YES |
| Phase A + B shipped | 2026-05-13 | Commit `dd8648df` |
| **Independent architect review (Plan agent)** | **2026-05-13** | **18 findings — 4 must-fix-before-C, 2 must-fix-before-F, 9 should-address, 3 nice-to-have** |
| Phase C-G implementation | TBD | |
| Staging deploy | TBD | |
| Operator UAT | TBD | |
| Prod deploy | TBD | |
| SHIPPED-TO-PROD | TBD | |
