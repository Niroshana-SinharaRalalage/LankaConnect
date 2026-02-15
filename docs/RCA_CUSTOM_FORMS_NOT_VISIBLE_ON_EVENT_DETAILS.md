# Root Cause Analysis: Custom Forms Not Visible on Event Details Page

**Date:** 2026-02-12
**Phase:** 7.3 - Custom Forms Feature
**Reporter:** User
**Severity:** Medium (Feature appears broken to users)
**Status:** Root Cause Identified

---

## 1. Problem Statement

User created a Custom Form successfully via organizer interface but cannot see it displayed on the event details page. The form section is expected to appear below the "Sign-Up Lists" section, but it is completely invisible to attendees.

---

## 2. Evidence Collected

### 2.1 API Response Analysis

**API Call:**
```bash
GET https://lankaconnect-api-staging.../api/events/62bf37a7-08c5-49e9-84ad-2be388e26caa/forms
```

**API Response (HTTP 200):**
```json
[
  {
    "id": "ade5a7ac-748a-4b0d-a602-c26226010d59",
    "eventId": "62bf37a7-08c5-49e9-84ad-2be388e26caa",
    "title": "Special Oil Lamp Lighting Ceromony",
    "description": "Saturday March 14 2026, 5PM...",
    "status": "Active",
    "allowMultipleResponses": false,
    "responseDeadline": "2026-02-28T19:41:00Z",
    "maxResponses": null,
    "hasResponses": false,
    "questionCount": 0,           // ❌ CRITICAL: Zero questions!
    "responseCount": 0,
    "createdAt": "2026-02-12T19:45:34.662932Z",
    "updatedAt": "2026-02-12T19:45:42.999781Z"
  }
]
```

**Key Finding:** The form exists and is `Active`, but has **0 questions** (`questionCount: 0`).

### 2.2 Frontend Filtering Logic

**File:** `web/src/app/events/[id]/page.tsx` (lines 199-203)

```typescript
// Phase 7.3: Fetch custom forms for this event
const { data: eventForms, isLoading: isLoadingForms } = useEventForms(id);

// Filter to show only Active forms to attendees
const activeForms = eventForms?.filter(form => form.status === EventFormStatus.Active) || [];
```

**Rendering Condition (line 1578):**
```typescript
{!isLoadingForms && activeForms.length > 0 && (
  <div className="mt-8">
    <Card>
      <CardHeader>
        <CardTitle>Custom Forms</CardTitle>
      </CardHeader>
      ...
    </Card>
  </div>
)}
```

**Analysis:**
- ✅ Form has `status: "Active"` → Passes filter
- ✅ `activeForms.length > 0` → Array has 1 form
- ❌ **BUT**: The form has no questions to display

### 2.3 User Workflow Analysis

**What User Did:**
1. Navigated to `/events/[id]/manage`
2. Clicked "Create Custom Form" button
3. Filled out form metadata (title, description, deadline, settings)
4. **CRITICAL:** Published form WITHOUT adding any questions
5. Expected form to appear on event details page
6. **RESULT:** Nothing visible

**What User Expected:**
> "Signup Forms should come next to Sign-up Lists as a different tab"

**What User Saw:**
- Only "Sign-Up Lists" section visible
- No "Custom Forms" section appearing
- No error message or indication why form isn't showing

---

## 3. Root Cause Analysis

### 3.1 Primary Root Cause

**Category:** Missing Business Rule Validation

**Root Cause:**
The system allows users to **publish a form with 0 questions**, creating a technically valid but functionally useless form. The frontend correctly filters for `Active` forms, but there is **no additional filtering** to exclude forms with zero questions.

### 3.2 Contributing Factors

#### Factor 1: No Frontend Validation Before Publishing
**File:** `web/src/app/events/[id]/manage/create-form/page.tsx`

The form builder allows users to publish forms without checking if questions exist.

#### Factor 2: No Backend Validation on Publish
**Expected Behavior:** Backend should reject `PublishEventForm` command if `form.Questions.Count == 0`

**Current Behavior:** Backend allows publishing empty forms

#### Factor 3: No User Feedback
**Issue:** When a form with 0 questions is filtered out, there is no message explaining why the form isn't visible:
- No "No forms available" placeholder when `activeForms.length == 0`
- No warning during form creation if user tries to publish without questions
- No visual indicator in organizer dashboard that form is invisible to attendees

---

## 4. Impact Assessment

### 4.1 Severity: Medium

**Affected Users:**
- Event organizers who create forms
- Attendees who should see forms but don't

**Impact:**
- **Data Loss Risk:** Low (form data is saved, just not visible)
- **User Trust:** Medium (users think feature is broken)
- **Workaround Available:** Yes (add questions to the form)
- **Business Impact:** Medium (blocks Custom Forms feature adoption)

### 4.2 Scope

**Environments Affected:**
- ✅ Staging (confirmed)
- ✅ Production (assumed - same code deployed)

**Feature Affected:**
- Phase 7.3: Custom Forms (Survey/Form Sign-Up Type)

---

## 5. Fix Plan

### 5.1 Immediate Workaround (User-Facing)

**Action:** Notify user to add questions to the form

**Steps:**
1. Navigate to `/events/[id]/manage`
2. Click on the form title to edit
3. Add at least 1 question
4. Save the form
5. Form will now be visible on event details page

### 5.2 Short-Term Fix (Backend Validation)

**Goal:** Prevent publishing forms with 0 questions

**Changes Required:**

#### A. Update `PublishEventFormCommandHandler.cs`

**File:** `src/LankaConnect.Application/Events/Commands/PublishEventForm/PublishEventFormCommandHandler.cs`

**Add Validation:**
```csharp
public async Task<Result> Handle(PublishEventFormCommand request, CancellationToken cancellationToken)
{
    // ... existing code to fetch form ...

    // ✅ ADD: Validate form has questions
    if (form.Questions.Count == 0)
    {
        _logger.LogWarning(
            "Cannot publish form {FormId} with zero questions - EventId={EventId}",
            request.FormId, request.EventId);

        return Result.Failure("Cannot publish a form without any questions. Please add at least one question.");
    }

    // ... existing publish logic ...
}
```

#### B. Update Frontend Form Builder

**File:** `web/src/app/events/[id]/manage/create-form/page.tsx`

**Add Pre-Publish Validation:**
```typescript
const handlePublish = async () => {
  if (questions.length === 0) {
    toast.error('Cannot publish a form without questions. Please add at least one question.');
    return;
  }

  // ... existing publish logic ...
};
```

### 5.3 Medium-Term Fix (UI Improvements)

**Goal:** Provide better feedback to users

#### A. Show Empty State on Event Details Page

**File:** `web/src/app/events/[id]/page.tsx`

**Change Rendering Logic:**
```typescript
{/* Phase 7.3: Custom Forms Section */}
{!isLoadingForms && (
  <div className="mt-8">
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <svg className="h-5 w-5 text-orange-600">...</svg>
          Custom Forms
        </CardTitle>
      </CardHeader>
      <CardContent>
        {activeForms.length === 0 ? (
          <p className="text-sm text-gray-500">
            No forms available for this event yet.
          </p>
        ) : (
          activeForms.map((form) => (
            // ... existing form card rendering ...
          ))
        )}
      </CardContent>
    </Card>
  </div>
)}
```

**Rationale:** Always show the section, even if no forms exist, so users understand the feature is present.

#### B. Add Warning in Organizer Dashboard

**File:** `web/src/app/events/[id]/manage/page.tsx`

**Show Warning Icon for Forms with 0 Questions:**
```typescript
{form.questionCount === 0 && form.status === 'Active' && (
  <Badge variant="warning" className="ml-2">
    No Questions - Not Visible to Attendees
  </Badge>
)}
```

### 5.4 Long-Term Fix (Architecture Improvement)

**Goal:** Enforce invariants at domain level

**Domain Rule:** "A form cannot be Active without at least one question"

**Implementation:**
1. Add domain validation in `EventForm` aggregate:
   ```csharp
   public void Publish()
   {
       if (Questions.Count == 0)
       {
           throw new DomainException("Cannot publish form without questions");
       }
       Status = EventFormStatus.Active;
   }
   ```

2. Update `EventFormStatus` state machine to prevent invalid transitions

---

## 6. Testing Checklist

**Before Deployment:**

### Backend Tests
- [ ] Unit test: `PublishEventForm` fails if `Questions.Count == 0`
- [ ] Unit test: `PublishEventForm` succeeds if `Questions.Count >= 1`
- [ ] Integration test: API returns 400 when trying to publish empty form

### Frontend Tests
- [ ] Unit test: Publish button shows error if no questions added
- [ ] E2E test: Create form → Add question → Publish → Verify visible on event page
- [ ] E2E test: Create form → Publish without questions → Verify error message

### Manual Testing
- [ ] Create form with 0 questions → Publish → Verify error shown
- [ ] Create form with 1 question → Publish → Verify visible on event details page
- [ ] Organizer dashboard shows warning for forms with 0 questions

---

## 7. Deployment Plan

### 7.1 Phase 1: Backend Validation (Critical)
1. Add validation to `PublishEventFormCommandHandler`
2. Write unit tests
3. Build and test locally
4. Deploy to staging
5. Test with actual form creation workflow
6. Deploy to production

### 7.2 Phase 2: Frontend Validation (High Priority)
1. Add pre-publish check in form builder
2. Add warning badges in organizer dashboard
3. Deploy to staging
4. User acceptance testing
5. Deploy to production

### 7.3 Phase 3: UI Improvements (Medium Priority)
1. Show empty state on event details page
2. Add better error messages
3. Deploy to staging
4. User feedback
5. Deploy to production

---

## 8. Prevention Strategies

### 8.1 Process Improvements

**For Future Features:**
1. ✅ **Define business rules explicitly** in specification phase
   - "A form MUST have at least 1 question to be published"

2. ✅ **Add validation at multiple layers:**
   - Domain: Enforce invariants in aggregates
   - Application: Validate commands before execution
   - API: Validate requests
   - UI: Pre-validate user actions

3. ✅ **Design empty states proactively:**
   - Always consider "what if there's no data?" scenarios
   - Show helpful messages instead of hiding UI sections

### 8.2 Code Review Checklist

**For Domain Features:**
- [ ] Are all business rules enforced in the domain layer?
- [ ] Are invalid state transitions prevented?
- [ ] Do aggregates validate their own invariants?

**For UI Features:**
- [ ] Are empty states designed and implemented?
- [ ] Do user actions have immediate validation feedback?
- [ ] Are error messages actionable and helpful?

### 8.3 Testing Standards

**Domain Tests:**
- Must test all state transitions
- Must test all invariant violations

**E2E Tests:**
- Must test "happy path" AND "edge cases"
- Must test forms with 0, 1, and multiple items

---

## 9. Related Issues

**Similar Past Issues:**
- None documented (first occurrence)

**Related Features:**
- Sign-Up Lists (has similar empty state handling)
- Event Registration (has validation before publishing)

---

## 10. Lessons Learned

### 10.1 What Went Well
✅ Clean Architecture separation made debugging easy
✅ API endpoint worked correctly (returned accurate data)
✅ React Query caching worked as expected
✅ Logging helped trace the issue

### 10.2 What Went Wrong
❌ No business rule validation for "minimum 1 question"
❌ No empty state UI designed
❌ No user feedback when form is invisible
❌ Missed this edge case during Phase 7.3 implementation

### 10.3 Action Items
1. **For Current Issue:**
   - [ ] Implement backend validation (Priority: High)
   - [ ] Add frontend pre-publish check (Priority: High)
   - [ ] Add empty state UI (Priority: Medium)
   - [ ] Write comprehensive tests (Priority: High)

2. **For Future:**
   - [ ] Add "business rules checklist" to SPARC Specification phase
   - [ ] Require explicit empty state designs in UI mockups
   - [ ] Add domain invariant validation to code review template

---

## 11. Sign-Off

**RCA Completed By:** Claude (SPARC Architecture Agent)
**Date:** 2026-02-12
**Review Status:** Pending User Approval

**Recommended Actions:**
1. Implement Short-Term Fix (Backend + Frontend Validation) immediately
2. Schedule Medium-Term Fix (UI Improvements) for next sprint
3. Add Long-Term Fix (Domain Validation) to technical debt backlog

---

## Appendix: Technical Details

### A. API Endpoint Details
- **URL:** `GET /api/events/{id}/forms`
- **Authorization:** AllowAnonymous
- **Response Type:** `List<EventFormDto>`
- **Handler:** `GetEventFormsQueryHandler`

### B. Files Modified (Proposed Fix)
1. `src/LankaConnect.Application/Events/Commands/PublishEventForm/PublishEventFormCommandHandler.cs`
2. `web/src/app/events/[id]/manage/create-form/page.tsx`
3. `web/src/app/events/[id]/page.tsx`
4. `web/src/app/events/[id]/manage/page.tsx`

### C. Test Coverage Requirements
- **Backend:** 90%+ coverage for `PublishEventForm` command
- **Frontend:** Unit tests for publish validation
- **E2E:** Full form creation workflow with edge cases

---

**End of RCA**
