# Master TODO — Phase 7C.3c Publication / Newsletter / Refund Location Decomposition

**Created**: 2026-04-24
**Owner**: next session (follow-up to Phase 7C.2b Chunk 2c)
**Trigger**: 2026-04-23 inbox-smoke revealed 4 templates outside the original
  Phase 7C.2b scope that still render event location as a single flat
  `"Street, City"` line instead of the decomposed Venue Name + Address +
  optional Secondary Location block. Source of truth: staging probe
  `scripts/probe_phase7c3_gap.py`.

**Architect call (Claude Opus 4.7, 2026-04-23)**: treat as a NEW chunk rather
than a Chunk 3 expansion — these templates ship via different handlers
(`EventPublishedEventHandler`, newsletter dispatcher, refund pipeline) and
the test matrix stays bounded. Master TODO scope grows from 16 → 20 templates.

---

## Scope (4 templates, out-of-original-scope)

| # | Template | Current body | Code-side ready? | Handler(s) |
|---|---|---|---|---|
| 17 | `template-event-details-publication` | ❌ flat `{{EventLocation}}` | ❓ needs audit | likely `EventDetailsPublishedEventHandler` |
| 18 | `template-new-event-publication` | ❌ flat `{{EventLocation}}` | ❓ needs audit | likely `EventPublishedEventHandler` |
| 19 | `template-newsletter-notification` | ❌ flat `{{EventLocation}}` | ❓ needs audit | newsletter dispatcher (check `NewsletterDispatchService` / `Communications` module) |
| 20 | `template-refund-requested` | ❌ flat `{{EventLocation}}` | ❓ needs audit | check `Payments` / `Refunds` module |

---

## Acceptance criteria (per template)

Same as Chunk 2a / 2b — for each template:
1. Identify the params class that feeds it (or create one if it currently uses a loose dictionary).
2. Confirm it has `LocationDetails` + `WithLocationDetails(...)` — add if missing.
3. Confirm `ToDictionary()` uses `LocationEmailDictionaryWriter.WriteTo(dict,
   LocationDetails ?? LocationEmailProjection.FromLegacyScalar(EventLocation))`.
4. Update every handler that constructs that params class to call
   `WithLocationDetails(@event.ProjectEmailLocation())`.
5. Migration that REPLACEs `{{EventLocation}}` with
   `EmailLocationBlockHtml.DecomposedBlock`, chunk-scoped backup
   `communications.email_templates_backup_phase7c3c`, 5 `RAISE EXCEPTION`
   invariants per template (`ROW_COUNT = 1`, legacy token gone, `{{LocationName}}`
   present, body-integrity anchor survives, `length(body) ≥ 50000`).
6. Anchor token selection — probe staging body first
   (`scripts/probe_phase7c3a_greetings.py`-style), do NOT assume the params
   class dictionary key matches the body's actual greeting.

---

## Tasks

- [ ] 3c.0 Probe: enumerate the tokens in each of the 4 template bodies to
      identify the right anchor and confirm `{{EventLocation}}` is the only
      flat legacy token (no `{{Location}}` variant hiding).
- [ ] 3c.1 Audit the 4 handlers / params classes: confirm `LocationDetails`
      plumbing exists or add it (mirrors Chunk 2a pattern).
- [ ] 3c.2 RED: transformation-logic unit tests mirroring
      `Phase7C3aDecomposeLocationTests` — REPLACE semantics + idempotence +
      anchor selection per template.
- [ ] 3c.3 GREEN: `dotnet ef migrations add Phase7C3c_DecomposeLocationInPublicationAndRefundTemplates`
      + fill Up()/Down() + backup table `_phase7c3c` + per-template invariants.
- [ ] 3c.4 Full build + suite + commit + push.
- [ ] 3c.5 Staging DB probe — 4 templates `has_decomposed=True` + byte-delta
      math matches (+713 per `{{EventLocation}}` instance).
- [ ] 3c.6 Live inbox smoke — (a) publish an event (new-event-publication +
      event-details-publication), (b) request a refund (refund-requested),
      (c) trigger newsletter notification.
- [ ] 3c.7 Close out: update `PROGRESS_TRACKER.md` +
      `STREAMLINED_ACTION_PLAN.md` + primary Phase 7C.2b master TODO to
      reflect 20-template total.

---

## Cross-chunk discipline (inherited)

1. **No regex on email HTML body** — literal `REPLACE(html_template,
   '{{EventLocation}}', :DecomposedBlock)` only. MEMORY
   `feedback_regex_on_email_html.md`.
2. **Chunk-scoped backup table** `_phase7c3c` — never shadow `_phase7c2` /
   `_phase7c2b` / `_phase7c3a`. MEMORY recovery-scoping rule.
3. **EF Core migration with `.Designer.cs`** — always via `dotnet ef
   migrations add`. MEMORY `NEVER Hand-Create EF Core Migration Files`.
4. **Anchor token derived from staging body probe** — MEMORY
   `feedback_template_body_is_authoritative.md`. The Chunk 2b first-deploy
   failure taught us params-class ToDictionary() keys are not a reliable
   proxy for what the template author actually interpolated.

---

## Out of scope

- The Phase 7C.3d CI gate (static probe of every `new *EmailParams` call
  site + matching `WithLocationDetails` call). Tracked separately.
- Chunk 3 (form-response, 3 templates — `template-form-response-*`).
  Independent chunk — different handlers, different user surface.
