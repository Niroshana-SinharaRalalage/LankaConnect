# Master TODO — E1 (Address Optional) + Phase C (Sign-Up Item Reorder)

**Created:** 2026-04-20
**Owner:** current session
**Architect approval:** ✅ system-architect reviewed plan; revised & approved 2026-04-20
**Scope:** Two orthogonal PRs — PR-A ships E1 now; PR-B ships Phase C (C1-C7 + D) after PR-A is green on staging
**Source of truth:** This file. Mirrored into in-session TodoWrite. Tracking docs (`PROGRESS_TRACKER.md`, `STREAMLINED_ACTION_PLAN.md`, `TASK_SYNCHRONIZATION_STRATEGY.md`) get a closing entry per PR.

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
- [ ] Add `PUT /api/events/{eventId:guid}/signups/{signupId:guid}/items/reorder` in `src/LankaConnect.API/Controllers/EventsController.cs` (~ line 1990, after `RemoveSignUpItem`)
- [ ] Add `ReorderSignUpItemsRequest(IReadOnlyList<Guid> OrderedItemIds)` record (~ line 3487)
- [ ] `[Authorize]`, `HandleResult` → 200 OK
- [ ] `[ProducesResponseType]` 200/400/401/404 matching siblings
- [ ] One `Logger.LogInformation` at entry
- [ ] No new tests (handler-only convention — confirmed by architect)
- [ ] `dotnet build` clean

### C5 — Read path ordering (30 min)
Per architect revision: add `ThenBy(ItemDescription)` as stable secondary sort.
- [ ] Add `int DisplayOrder` property to `ISignUpItemDto` interface (Phase 6A.124 rule — interface-level properties required for System.Text.Json)
- [ ] Add `DisplayOrder` to `QuantityBasedItemDto` + `SlotBasedItemDto`
- [ ] In `GetEventSignUpListsQueryHandler`: `OrderBy(i => i.DisplayOrder).ThenBy(i => i.ItemDescription)`
- [ ] TDD: add one query-handler test asserting ordered output **if sibling tests exist**; else honor handler-only convention (check `tests/LankaConnect.Application.Tests/Events/Queries/GetEventSignUpLists/`)
- [ ] **Local JSON-shape verification (MANDATORY per architect — Phase 6A.124 precedent):** `dotnet run` locally → hit GET `/api/events/{id}/signups` → grep `displayOrder` in JSON response → confirm present and correctly ordered

### C6 — Drag-drop UI (2-3h)
- [ ] **Reuse-check first (MANDATORY):** `grep -r "Sortable\|DragHandle\|Reorder\|DndContext\|@dnd-kit" web/src/presentation/components/`. Document findings. If reusable pattern exists, use it.
- [ ] Add `reorderSignUpItems(eventId, signupId, orderedItemIds)` to `web/src/infrastructure/api/repositories/events.repository.ts`
- [ ] Add `displayOrder: number` to TS `ISignUpItemDto` in `web/src/infrastructure/api/types/events.types.ts`
- [ ] Wrap sign-up item list (organizer view ONLY) with `DndContext` + `SortableContext` + `PointerSensor` + `KeyboardSensor` (a11y-first per @dnd-kit)
- [ ] Loading state during save
- [ ] Refetch on 400 (stale-set race)
- [ ] Mobile touch verified (PointerSensor covers)
- [ ] Keyboard: Tab → Space → Arrows → Space

### C7 — Commit
- [ ] Single commit: `feat(signups): Phase C — drag-drop reorder of sign-up items`
- [ ] Includes all C1-C6 work + docs updates

### Phase D — Deploy + verify

**Pre-push gates (architect-mandated):**
- [ ] `dotnet ef migrations has-pending-model-changes` returns **false** (investigate current "has pending changes" alert — likely just Phase C drift that's already captured in migration; confirm before push)
- [ ] Full `dotnet test` solution suite green
- [ ] `dotnet build` clean, 0 errors

**Deploy sequence:**
1. [ ] Push backend → `deploy-staging.yml`
2. [ ] Wait for workflow green
3. [ ] **BEFORE pushing frontend: three-query migration verification gate (MANDATORY — Phase 6A.117/122/129 precedent):**
   ```sql
   -- 1. History row present
   SELECT * FROM events."__EFMigrationsHistory"
     WHERE migration_id = '20260420040155_AddSignUpItemDisplayOrder';
   -- 2. Column exists with expected shape
   SELECT column_name, data_type, column_default, is_nullable
     FROM information_schema.columns
     WHERE table_schema='events' AND table_name='sign_up_items' AND column_name='display_order';
   -- 3. Backfill correct — DisplayOrder unique within each list
   SELECT sign_up_list_id, COUNT(DISTINCT display_order), COUNT(*)
     FROM events.sign_up_items
     GROUP BY sign_up_list_id
     HAVING COUNT(DISTINCT display_order) != COUNT(*);
   -- Expect zero rows from query 3.
   ```
4. [ ] API smoke:
   ```bash
   # GET baseline
   curl -sH "Authorization: Bearer $TOKEN" \
     "https://.../api/events/{id}/signups" | jq '.[0].items[] | {id, displayOrder, itemDescription}'
   # PUT reorder (reverse IDs) → expect 200
   curl -sX PUT "https://.../api/events/{id}/signups/{sid}/items/reorder" \
     -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
     -d '{"orderedItemIds":["<reversed GUIDs>"]}'
   # GET again → assert order persisted
   # Negative: PUT with tampered set (missing/extra/unknown ID) → expect 400
   ```
5. [ ] Push frontend → `deploy-ui-staging.yml`
6. [ ] Wait for workflow green
7. [ ] Browser smoke:
   - [ ] Drag item in organizer sign-up list; refresh; order persists
   - [ ] Keyboard: Tab to drag handle → Space → Arrows → Space
   - [ ] Mobile (touch) works
8. [ ] Azure container logs clean
9. [ ] Update `PROGRESS_TRACKER.md`, `STREAMLINED_ACTION_PLAN.md`, `TASK_SYNCHRONIZATION_STRATEGY.md` with closing entry per `TASK_SYNCHRONIZATION_STRATEGY.md` format. Commit.

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
