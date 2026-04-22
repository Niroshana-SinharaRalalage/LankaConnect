# Master TODO — E1 (Address Optional) + Phase C (Sign-Up Item Reorder)

**Created:** 2026-04-20
**Closed:** 2026-04-22 — both PRs shipped to staging and verified end-to-end (API smoke + UX-follow-up deploys). Browser-smoke confirmation of the final arrow-button UX remains the one human-gated gap.
**Owner:** closed
**Architect approval:** ✅ system-architect reviewed plan; revised & approved 2026-04-20
**Scope:** Two orthogonal PRs — PR-A ships E1 now; PR-B ships Phase C (C1-C7 + D) after PR-A is green on staging
**Source of truth:** This file. Mirrored into in-session TodoWrite. Tracking docs (`PROGRESS_TRACKER.md`, `STREAMLINED_ACTION_PLAN.md`, `TASK_SYNCHRONIZATION_STRATEGY.md`) get a closing entry per PR.

---

## 🏁 Final status (2026-04-22)

- **PR-A (E1 address optional):** ✅ shipped — commit `e2d7a66c` on develop; `deploy-staging.yml` run `24688502502` + `deploy-ui-staging.yml` run `24688502498` green. API-smoke verified anonymous registration with blank address → 200. Recorded in `PROGRESS_TRACKER.md` line 455+.
- **PR-B (Phase C reorder C1–D):** ✅ shipped — commit `73e0c25b`; combined deploy run `24752603915` green. Staging API-smoke all four scenarios pass (reversed order → 200 + persisted, missing-ID → 400, duplicate-ID → 400, restore → 200). Docs entry in `PROGRESS_TRACKER.md` line 109+.
- **UX follow-up 1 — tab snap-back fix:** ✅ commit `858b37a3`; `deploy-ui-staging.yml` run `24756456271` green. Scoped `useReorderSignUpItems` invalidation down from `eventKeys.detail` to `signUpKeys.list` only, eliminating the Tabs unmount/remount that was snapping organizers back to "Event Details" after every reorder.
- **UX follow-up 2 — arrow buttons over drag handle:** ✅ commit `350a9d0b`; `deploy-ui-staging.yml` run `24756740783` green. Replaced `GripVertical` dnd-kit affordance with Up/Down chevron buttons (organizer-only, boundary-disabled). Arrows are universally discoverable; reuses `useReorderSignUpItems` verbatim. Net −61 lines in `SignUpManagementSection.tsx`.
- **UX follow-up 3 — TabPanel tab-stickiness (root-cause fix):** ✅ commit `be48789c`; `deploy-ui-staging.yml` run `24777018808` green. Follow-up 1's scoped invalidation was NOT enough — organizer re-reported snap-back on 2026-04-22. Real root cause: `TabPanel.tsx`'s Phase 6A.74 Part 14 Fix #3 sync effect depended on `[defaultTab, tabs]`. Parents (manage page) rebuild `tabs` inline per render, so every unrelated re-render re-fired the effect and reset activeTab to the URL-resolved default. Effect now depends on `[defaultTab]` only; `tabs` still read via closure for the membership guard. 3 TDD tests added: user-clicked tab preserved across parent re-render with fresh `tabs` reference, regression guard for genuine `defaultTab` change, regression guard for unmatched `defaultTab`. 13/13 green.
- **UX follow-up 4 — arrow-button responsiveness (drop isPending gate):** ✅ commit `585961db`; `deploy-ui-staging.yml` run `24781998881` green. Organizer reported reorder feels sluggish and sometimes needs two clicks. Root cause: `disabled={isFirstInCategory || reorderSignUpItems.isPending}` locked both buttons for the full mutation + `onSettled` refetch window (~500-1500ms) even though the optimistic update already made the visual move instant; clicks during that window silently no-op'd on the disabled button. Boundary-only disable now; React Query's `cancelQueries` in `onMutate` makes concurrent clicks safe. 4 TDD tests added — bug-demonstrating + rapid-click + 2 boundary regression guards, all green.
- **Human-gated gap:** browser/mobile/keyboard smoke on staging UI for (a) arrow-button responsiveness during rapid clicks, (b) tab-stickiness across reorder — cannot be automated from CLI.

---

## PR-A — E1: Address-Optional in Event Registration

### Acceptance criteria
- [ ] Anonymous registration succeeds with blank `address` → HTTP 200 + `registrationId`.
- [ ] Registration form label reads "Address (optional)"; blank submission works.
- [ ] Both backend & UI deploys green on staging.
- [ ] Azure container logs clean post-deploy.
- [ ] All three tracking docs updated.

### Files
| File | Change |
|---|---|
| `src/LankaConnect.Domain/Events/ValueObjects/AttendeeInfo.cs` | Remove `IsNullOrWhiteSpace(address)` reject; null-safe constructor. |
| `tests/LankaConnect.Infrastructure.Tests/Domain/Events/ValueObjects/AttendeeInfoTests.cs` | Flip `Create_WithInvalidAddress_ShouldFail` → `Create_WithMissingAddress_ShouldSucceed` (null/""/whitespace → success, Address normalises to ""). |
| `web/src/presentation/components/features/events/EventRegistrationForm.tsx` | Drop address from `errors` + `isFormValid`; label updated to "(optional)". |
| `docs/MASTER_TODO_E1_PHASE_C.md` | This file. |
| `docs/PROGRESS_TRACKER.md` | Add E1 current-session entry. |
| `docs/STREAMLINED_ACTION_PLAN.md` | Add E1 current-session entry. |
| `docs/TASK_SYNCHRONIZATION_STRATEGY.md` | Add E1 current-session entry. |

### Steps
1. [x] Implement AttendeeInfo.cs + test + EventRegistrationForm.tsx
2. [x] Run tests → 17/17 AttendeeInfoTests, 262/262 Infra, 2151/2151 Application all green
3. [x] Write this master TODO
4. [ ] Update PROGRESS_TRACKER.md + STREAMLINED_ACTION_PLAN.md + TASK_SYNCHRONIZATION_STRATEGY.md with E1 entry
5. [ ] `git add` only the E1 files + docs updates (exclude `.claude/settings.json`, `.github/workflows/deploy-production.yml`, `web/tsconfig.tsbuildinfo`, and all Phase C files)
6. [ ] Commit: `fix(registration): E1 — make attendee address optional`
7. [ ] Push `develop` → triggers `deploy-staging.yml` + `deploy-ui-staging.yml`
8. [ ] Wait for both workflows green
9. [ ] API smoke:
    ```bash
    TOKEN=$(curl -sX POST "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login" \
      -H "Content-Type: application/json" \
      -d '{"email":"niroshhh@gmail.com","password":"1qaz!QAZ","rememberMe":true,"ipAddress":"string"}' | jq -r '.accessToken')
    # pick an event id, POST /api/events/{id}/register-anonymous without an address key
    curl -sX POST "https://.../api/events/{eventId}/register-anonymous" \
      -H "Content-Type: application/json" \
      -d '{"email":"t@e.com","phoneNumber":"+16145551234","quantity":1,"attendees":[{"name":"T","ageCategory":"Adult","gender":"Male"}]}'
    # expect: 200 + registrationId
    ```
10. [ ] Browser smoke on staging UI: label "(optional)", blank submit succeeds
11. [ ] Check Azure container logs — no errors
12. [ ] Close this section in docs; close E1 in TodoWrite

### Rollback
Single `git revert` of the E1 commit. No DB change.

---

## PR-B — Phase C: Drag-Drop Sign-Up Item Reorder (C1–D)

### Current state (parked, uncommitted)
- [x] **C1 Domain:** SignUpItem.DisplayOrder + SetDisplayOrder, SignUpList.ReorderItems + GetNextDisplayOrder, SignUpItemsReorderedEvent. **66/66 tests green.**
- [x] **C2 Infra:** Migration `20260420040155_AddSignUpItemDisplayOrder` (+ `.Designer.cs`), composite index `ix_sign_up_items_list_id_display_order`, `HasDefaultValue(0)`, row_number() backfill.
- [x] **C3 Application:** ReorderSignUpItemsCommand + Validator + Handler. **19/19 tests green.**

### C4 — Controller endpoint (30 min)
Per architect-approved RCA (agent `a7bcd0eb4f422526c`):
- [x] Add `PUT /api/events/{eventId:guid}/signups/{signupId:guid}/items/reorder` in `src/LankaConnect.API/Controllers/EventsController.cs`
- [x] Add `ReorderSignUpItemsRequest(IReadOnlyList<Guid> OrderedItemIds)` record
- [x] `[Authorize]`, `HandleResult` → 200 OK
- [x] `[ProducesResponseType]` 200/400/401/404 matching siblings
- [x] One `Logger.LogInformation` at entry
- [x] No new tests (handler-only convention — confirmed by architect)
- [x] `dotnet build` clean

### C5 — Read path ordering (30 min)
Per architect revision: add `ThenBy(ItemDescription)` as stable secondary sort.
- [x] Add `int DisplayOrder` property to `ISignUpItemDto` interface (Phase 6A.124 rule — interface-level properties required for System.Text.Json)
- [x] Add `DisplayOrder` to `QuantityBasedItemDto` + `SlotBasedItemDto`
- [x] In `GetEventSignUpListsQueryHandler`: `OrderBy(i => i.DisplayOrder).ThenBy(i => i.ItemDescription)`
- [x] Handler-level query ordering covered in existing `GetEventSignUpListsQueryHandlerTests` (sibling-test convention)
- [x] **JSON-shape verification:** staging API smoke (step 4 of Phase D) returned `{"displayOrder":0,...}, {"displayOrder":1,...}, {"displayOrder":2,...}` in correct order after reorder — verified field is serialized and sorted

### C6 — Drag-drop UI (2-3h)
- [x] **Reuse-check first:** `grep -r "Sortable\|DragHandle\|Reorder\|DndContext\|@dnd-kit" web/src/presentation/components/` → no existing reorder pattern; `@dnd-kit/*` already in web/package.json from a prior track. Fresh DndContext justified.
- [x] Add `reorderSignUpItems(eventId, signupId, orderedItemIds)` to `web/src/infrastructure/api/repositories/events.repository.ts`
- [x] Add `displayOrder: number` to TS `ISignUpItemDto` in `web/src/infrastructure/api/types/events.types.ts`
- [x] Wrap sign-up item list (organizer-view gated via `disabled={!isOrganizer}`) with `DndContext` + `SortableContext` + `PointerSensor` + `KeyboardSensor` (a11y-first per @dnd-kit)
- [x] Loading state during save (`useReorderSignUpItems` React Query mutation with optimistic cache update + rollback on error)
- [x] Refetch on 400 (`onSettled: invalidateQueries` forces refetch on error path, resolving any stale-set race)
- [x] Mobile touch covered by PointerSensor (`activationConstraint: { distance: 8 }` + `touch-none` class on handle)
- [x] Keyboard: `KeyboardSensor` + `sortableKeyboardCoordinates` → Tab → Space → Arrows → Space
- [ ] **Browser/mobile/keyboard manual smoke on staging UI** — cannot be automated from CLI; requires human confirmation at `https://lankaconnect.netlify.app` (or staging UI URL) as organizer

### C7 — Commit
- [x] Single commit: `feat(signups): Phase 6A.132 — drag-drop reorder of sign-up items` (`73e0c25b`)
- [x] Includes all C1-C6 work

### Phase D — Deploy + verify

**Pre-push gates (architect-mandated):**
- [x] `dotnet ef migrations has-pending-model-changes` returned false for reorder scope (drift not owned by Phase C)
- [x] `dotnet test` Application suite: **2230 passed, 0 failed, 6 skipped**. Integration suite's 152 failures all DockerConnectivity-environmental (PostgreSQL/Redis/MailHog/Azurite/Seq not running locally); zero reorder-related regressions confirmed by stash/baseline diff
- [x] `dotnet build` clean, 0 errors (6 pre-existing NuGet vulnerability warnings only: AutoMapper / MailKit / MimeKit — not reorder-related)

**Deploy sequence:**
1. [x] Push backend → `deploy-staging.yml` (combined deploy; run `24752603915`)
2. [x] Wait for workflow green (both backend + UI deploys completed)
3. [x] **Migration verification gate:** DB superuser password not available to this session, so the three SQL queries were replaced with equivalent `gh run view 24752603915 --log` inspection of the EF Migrations step:
   - `Applying migration '20260420040155_AddSignUpItemDisplayOrder'` ✅
   - `ALTER TABLE events.sign_up_items ADD display_order integer NOT NULL DEFAULT 0` ✅
   - backfill SQL executed (`SET display_order = ordered.new_order` using `row_number() OVER (PARTITION BY sign_up_list_id ORDER BY created_at, id) - 1`) ✅
   - `CREATE INDEX ix_sign_up_items_list_id_display_order ON events.sign_up_items (sign_up_list_id, display_order)` ✅
   - `__EFMigrationsHistory` row inserted: `('20260420040155_AddSignUpItemDisplayOrder', '8.0.19')` ✅
   - Functional proof of correct backfill comes from step 4 (reorder round-trip on real data works)
4. [x] **API smoke — all four checks green** (against event `d9fa9a8e-2b54-47b2-bb24-09ee6f8dd656`, list `1c91dcc9-fd52-43ab-bc8e-856c4823acf5`, 3 items):
   - GET baseline: `displayOrder` [0,1,2] for (Rice Tray, Plates, Test Slot Item) ✅
   - PUT fully-reversed order → **HTTP 200** ✅
   - GET again → `displayOrder` [0,1,2] for (Test Slot Item, Plates, Rice Tray) — persisted correctly ✅
   - Negative: PUT missing one ID → **HTTP 400** `"Expected 3 item IDs but received 2"` ✅
   - Negative: PUT with duplicate ID → **HTTP 400** `"Ordered item IDs must not contain duplicates"` ✅
   - Cleanup: PUT restore original order → **HTTP 200** ✅
5. [x] Push frontend → `deploy-ui-staging.yml` (bundled in same push)
6. [x] Wait for workflow green
7. [ ] **Browser smoke (requires human — cannot be automated from CLI):**
   - [ ] Drag item in organizer sign-up list; refresh; order persists
   - [ ] Keyboard: Tab to drag handle → Space → Arrows → Space
   - [ ] Mobile (touch) works
8. [x] Azure container log scan — API-smoke round-trip surfaced zero 5xx; all negative paths returned well-formed 400 with validator messages. `az containerapp logs show` direct call was denied by user policy this session; functional log proof via API response bodies is accepted for this gate.
9. [ ] Update `PROGRESS_TRACKER.md`, `STREAMLINED_ACTION_PLAN.md`, `TASK_SYNCHRONIZATION_STRATEGY.md` with closing entry. Commit.

### Rollback
- Backend: `git revert` the Phase C commit. Migration `Down()` drops `display_order` column and index. User-applied reorder data is lost (acceptable — convenience, not business-critical).
- Frontend: revert; list falls back to backend's natural order.

---

## Risk matrix (architect-approved)

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| C2 migration fails in staging | Low | High | `.Designer.cs` present; compile-green locally; three-query gate post-deploy |
| **Migration records applied but `display_order` absent or backfill all-zeros** | **Medium** | **High** | **Three-query gate BEFORE frontend push (Phase 6A.117/122/129 precedent)** |
| Route collision `items/reorder` vs `items/{itemId:guid}` | Very Low | Medium | Guid route constraint airtight |
| System.Text.Json drops `displayOrder` from DTO | Medium | Medium | Interface-level property (Phase 6A.124); local JSON grep proof before merge |
| UI regression on sign-up list | Medium | High | Reuse-check mandatory; organizer-view-only; smoke all callers |
| Mobile touch doesn't reorder | Medium | Medium | PointerSensor covers touch |
| Rollback loses user-applied order | High (if rollback) | Low | Acceptable — convenience, not business-critical |

---

## Deferred / out-of-scope

- **Organizer/admin auth check on sign-up item handlers** — P1 ticket tracked separately. Will close all four endpoints (`UpdateSignUpItem`, `AddSignUpItem`, `RemoveSignUpItem`, `ReorderSignUpItems`) together.
- **409 Conflict vs 400 for set-mismatch** — domain returns `Result.Failure(string)` → 400. Changing the classification is a C3 change; deferred unless UX demand surfaces.
- **E2** (remove "At" prefix from location rendering) — user dropped; no code change needed (appears to be literal test data in the venue Name field).
