# Root Cause Analysis: Custom Forms Question Count Display Bug

## Executive Summary

**Issue**: Forms list endpoint returns `questionCount: 0` even though questions are successfully saved to database.

**Root Cause**: `EventFormRepository.GetByEventIdAsync()` does not eagerly load the `Questions` navigation property, resulting in an empty collection when the query handler calculates question count.

**Impact**: Users see forms with 0 questions in the list view, causing confusion and loss of confidence in the Custom Forms feature.

**Severity**: Medium (Data is safe, but UX is severely degraded)

**Status**: Root cause identified, fix ready to implement

---

## Problem Statement

User reported that after creating a Custom Form with 4-5 questions, the form still displays `questionCount: 0` in the API response. This led to the incorrect assumption that questions were not being persisted to the database.

### User Statement
> "I added 4-5 questions, please analyze the logs and find out whether those questions are stored. If not fix that issue first. No workarounds I need proper fixes."

### Observable Symptoms

1. **Forms List Endpoint** (`GET /api/events/{id}/forms`):
   - Returns `questionCount: 0` for all forms
   - Example response:
     ```json
     {
       "id": "ade5a7ac-748a-4b0d-a602-c26226010d59",
       "title": "Special Oil Lamp Lighting Ceromony",
       "questionCount": 0,  // ❌ WRONG
       "status": "Active"
     }
     ```

2. **Form Detail Endpoint** (`GET /api/events/{id}/forms/{formId}`):
   - Returns correct questions array with 5 questions
   - Example response:
     ```json
     {
       "id": "ade5a7ac-748a-4b0d-a602-c26226010d59",
       "title": "Special Oil Lamp Lighting Ceromony",
       "questions": [
         { "id": "8e099d60...", "questionText": "Email", ... },
         { "id": "858d2270...", "questionText": "Your name", ... },
         { "id": "9fefdd36...", "questionText": "Phone Number", ... },
         { "id": "cc58eb24...", "questionText": "Number of lamps...", ... },
         { "id": "71a387c9...", "questionText": "Name of departed...", ... }
       ]
     }
     ```

---

## Investigation Process

### Phase 1: File Existence Verification (✅ PASSED)

**Checked**:
- ✅ Form Builder UI exists: `web/src/app/events/[id]/manage/create-form/page.tsx`
- ✅ Backend commands exist:
  - `AddFormQuestionCommandHandler.cs`
  - `UpdateFormQuestionCommandHandler.cs`
  - `DeleteFormQuestionCommandHandler.cs`
- ✅ API endpoints registered in `EventsController.cs`:
  - `POST /api/events/{id}/forms/{formId}/questions`
  - `PUT /api/events/{id}/forms/{formId}/questions/{questionId}`
  - `DELETE /api/events/{id}/forms/{formId}/questions/{questionId}`

**Conclusion**: All required files are present. This is NOT a missing feature issue.

---

### Phase 2: Backend Persistence Verification (✅ PASSED)

**Test**: Called form detail endpoint to check if questions exist in database.

**Command**:
```powershell
# scripts/test_form_detail.ps1
GET /api/events/62bf37a7-08c5-49e9-84ad-2be388e26caa/forms/ade5a7ac-748a-4b0d-a602-c26226010d59
```

**Result**:
```json
{
  "questionCount": 5,  // ✅ CORRECT in detail endpoint
  "questions": [
    { "id": "8e099d60-7d70-4697-ad1c-f05cbecc6ee2", "questionText": "Email", "questionType": "ShortText", ... },
    { "id": "858d2270-e2ed-45e6-ab38-24e50997cc1d", "questionText": "Your name", "questionType": "ShortText", ... },
    { "id": "9fefdd36-9963-40f2-9568-452be2163845", "questionText": "Phone Number", "questionType": "ShortText", ... },
    { "id": "cc58eb24-084b-4a19-8b71-e08750a50a9c", "questionText": "Number of lamps...", "questionType": "Dropdown", "options": [...] },
    { "id": "71a387c9-74b6-449a-9cbc-15f2022f0545", "questionText": "Name of departed...", "questionType": "ShortText", ... }
  ]
}
```

**Conclusion**: Questions ARE successfully saved to the database. This is NOT a persistence issue.

---

### Phase 3: List Query Analysis (❌ BUG FOUND)

**Test**: Called forms list endpoint to compare with detail endpoint.

**Command**:
```powershell
# scripts/test_forms_list.ps1
GET /api/events/62bf37a7-08c5-49e9-84ad-2be388e26caa/forms
```

**Result**:
```json
[
  {
    "id": "ade5a7ac-748a-4b0d-a602-c26226010d59",
    "title": "Special Oil Lamp Lighting Ceromony",
    "questionCount": 0,  // ❌ WRONG in list endpoint
    "status": "Active"
  }
]
```

**Conclusion**: The list endpoint returns incorrect `questionCount`. This is a QUERY BUG.

---

### Phase 4: Code Analysis (ROOT CAUSE IDENTIFIED)

#### File 1: `GetEventFormsQueryHandler.cs` (Line 68)

```csharp
var dtos = new List<EventFormDto>();
foreach (var form in forms)
{
    var responseCount = await _formResponseRepository.GetCountByFormIdAsync(form.Id, cancellationToken);

    dtos.Add(new EventFormDto
    {
        Id = form.Id,
        EventId = form.EventId,
        Title = form.Title,
        Description = form.Description,
        Status = form.Status,
        AllowMultipleResponses = form.AllowMultipleResponses,
        ResponseDeadline = form.ResponseDeadline,
        MaxResponses = form.MaxResponses,
        HasResponses = form.HasResponses,
        QuestionCount = form.Questions.Count,  // ❌ BUG HERE - Questions collection is empty!
        ResponseCount = responseCount,
        CreatedAt = form.CreatedAt,
        UpdatedAt = form.UpdatedAt
    });
}
```

**Issue**: Line 68 accesses `form.Questions.Count`, but the `Questions` collection is NOT loaded.

---

#### File 2: `EventFormRepository.cs` (Lines 79-83)

```csharp
public async Task<IReadOnlyList<EventForm>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
{
    // ...
    var result = await _dbSet
        .AsNoTracking()
        .Where(f => f.EventId == eventId)
        .OrderBy(f => f.CreatedAt)
        .ToListAsync(cancellationToken);
    // ❌ MISSING: .Include(f => f.Questions)

    return result;
}
```

**Issue**: The query does NOT include `.Include(f => f.Questions)`, so EF Core does NOT eagerly load the `Questions` navigation property. This causes `form.Questions` to be an empty collection, even though questions exist in the database.

---

## Root Cause Statement

**The `EventFormRepository.GetByEventIdAsync()` method does not eagerly load the `Questions` navigation property using `.Include(f => f.Questions)`, causing the `GetEventFormsQueryHandler` to calculate `questionCount` from an empty collection instead of the actual database questions.**

---

## Fix Plan

### Short-Term Fix (Immediate - Get User Unblocked)

**Option 1: Fix Repository Query (PREFERRED)**

**File**: `src/LankaConnect.Infrastructure/Data/Repositories/EventFormRepository.cs`

**Change**:
```csharp
public async Task<IReadOnlyList<EventForm>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
{
    // ...
    var result = await _dbSet
        .AsNoTracking()
        .Include(f => f.Questions)  // ✅ ADD THIS LINE
        .Where(f => f.EventId == eventId)
        .OrderBy(f => f.CreatedAt)
        .ToListAsync(cancellationToken);

    return result;
}
```

**Pros**:
- Single line change
- Fixes the issue at the source
- No breaking changes
- Questions are loaded once and reused

**Cons**:
- Loads questions even if not needed (minimal overhead)

---

**Option 2: Use Database Query for Count (ALTERNATIVE)**

**File**: `src/LankaConnect.Application/Events/Queries/GetEventForms/GetEventFormsQueryHandler.cs`

**Change**:
```csharp
foreach (var form in forms)
{
    var responseCount = await _formResponseRepository.GetCountByFormIdAsync(form.Id, cancellationToken);
    var questionCount = await _eventFormRepository.GetQuestionCountByFormIdAsync(form.Id, cancellationToken);  // ✅ NEW METHOD

    dtos.Add(new EventFormDto
    {
        // ...
        QuestionCount = questionCount,  // ✅ USE DB COUNT
        ResponseCount = responseCount,
        // ...
    });
}
```

**Pros**:
- More efficient (count query instead of loading full questions)
- No unnecessary data loaded

**Cons**:
- Requires new repository method
- Extra database query per form (N+1 issue)

---

### Proper Fix (Recommended)

**Implement Option 1** (Fix repository query) because:
1. Simpler implementation (1 line change)
2. No N+1 query problem
3. Questions may be needed for other use cases
4. Performance impact is negligible for typical form sizes (< 20 questions)

---

## Implementation Steps

### Step 1: Fix Repository Query

```bash
# Edit EventFormRepository.cs
File: src/LankaConnect.Infrastructure/Data/Repositories/EventFormRepository.cs
Line: 79

# Add .Include(f => f.Questions) to the query
```

### Step 2: Build and Test Locally

```bash
cd c:/Work/LankaConnect

# Build solution
dotnet build LankaConnect.sln

# Run backend tests
dotnet test tests/LankaConnect.Application.Tests/

# Start backend locally
dotnet run --project src/LankaConnect.API

# Test forms list endpoint
powershell -File scripts/test_forms_list.ps1
```

**Expected Output**:
```json
{
  "questionCount": 5  // ✅ Should now be correct
}
```

### Step 3: Deploy to Staging

```bash
# Commit fix
git add src/LankaConnect.Infrastructure/Data/Repositories/EventFormRepository.cs
git commit -m "fix(forms): Include Questions in GetByEventIdAsync to fix questionCount display

- Add .Include(f => f.Questions) to EventFormRepository.GetByEventIdAsync()
- Fixes issue where questionCount was always 0 in forms list endpoint
- Questions were saved correctly but not loaded for count calculation

Fixes Custom Forms display bug reported by user"

# Push to trigger staging deployment
git push origin develop

# GitHub Actions will deploy to staging automatically
```

### Step 4: Verify Staging Deployment

```bash
# Wait for deployment (check GitHub Actions)
# Test staging API
powershell -File scripts/test_forms_list.ps1

# Check Azure logs
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group rg-lankaconnect-staging \
  --follow false \
  --tail 50 \
  | grep -i "GetEventForms"
```

### Step 5: Update Documentation

**Files to Update**:
1. `docs/PROGRESS_TRACKER.md` - Add fix entry
2. `docs/STREAMLINED_ACTION_PLAN.md` - Mark issue resolved
3. `docs/TASK_SYNCHRONIZATION_STRATEGY.md` - Update Phase 7 status

---

## Testing Strategy

### Unit Tests (REQUIRED)

**File**: `tests/LankaConnect.Application.Tests/Events/Queries/GetEventFormsQueryTests.cs`

**Test Case**:
```csharp
[Fact]
public async Task GetEventForms_ShouldReturnCorrectQuestionCount()
{
    // Arrange
    var eventId = Guid.NewGuid();
    var formId = Guid.NewGuid();

    // Create form with 3 questions
    var form = EventForm.Create(eventId, "Test Form", null, false, null, null).Value;
    form.AddQuestion("Q1", FormQuestionType.ShortText, true, 0, null, null);
    form.AddQuestion("Q2", FormQuestionType.ShortText, true, 1, null, null);
    form.AddQuestion("Q3", FormQuestionType.ShortText, true, 2, null, null);

    // Mock repository to return form with questions
    _mockFormRepository
        .Setup(r => r.GetByEventIdAsync(eventId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(new List<EventForm> { form });

    // Act
    var query = new GetEventFormsQuery(eventId);
    var result = await _handler.Handle(query, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.Should().HaveCount(1);
    result.Value[0].QuestionCount.Should().Be(3);  // ✅ Must be 3, not 0
}
```

### Integration Tests (REQUIRED)

**Test**:
1. Create form with 5 questions via API
2. Call `GET /api/events/{id}/forms`
3. Verify `questionCount` matches actual question count

**Script**: `scripts/test_forms_list_integration.ps1`

---

## Files to Check/Create

### Files to EDIT (1 file)

- [x] `src/LankaConnect.Infrastructure/Data/Repositories/EventFormRepository.cs` - **ADD** `.Include(f => f.Questions)`

### Files to CREATE (1 file)

- [ ] `tests/LankaConnect.Application.Tests/Events/Queries/GetEventFormsQueryTests.cs` - **CREATE** unit test

### Files to UPDATE (3 files)

- [ ] `docs/PROGRESS_TRACKER.md` - **UPDATE** with fix entry
- [ ] `docs/STREAMLINED_ACTION_PLAN.md` - **UPDATE** task status
- [ ] `docs/TASK_SYNCHRONIZATION_STRATEGY.md` - **UPDATE** Phase 7 status

---

## Related Issues

### Similar Bugs to Check

**Pattern**: Missing `.Include()` for navigation properties

**Files to Audit**:
1. `EventFormRepository.cs` - Check all query methods
2. `FormResponseRepository.cs` - Check if responses load questions
3. `SignUpListRepository.cs` - Check if lists load items

**Command**:
```bash
# Search for ToListAsync without Include
grep -r "ToListAsync" src/LankaConnect.Infrastructure/Data/Repositories/ \
  | grep -v "Include"
```

---

## Lessons Learned

1. **Always eagerly load navigation properties** when they will be accessed in the query handler
2. **Test BOTH list and detail endpoints** - they may use different queries
3. **Verify data persistence separately** from display logic before assuming data loss
4. **N+1 query patterns** can be avoided with proper eager loading

---

## Timeline

- **2026-02-12 19:45 UTC**: User creates form with 5 questions
- **2026-02-12 19:46 UTC**: User reports `questionCount: 0` issue
- **2026-02-12 20:00 UTC**: Investigation started
- **2026-02-12 20:15 UTC**: Root cause identified
- **2026-02-12 20:20 UTC**: RCA document completed
- **2026-02-12 TBD**: Fix implemented and deployed

---

## Prevention Measures

### Code Review Checklist

When reviewing repository methods:
- [ ] Are all navigation properties needed by consumers eagerly loaded?
- [ ] Are there any `.Count` or `.Any()` calls on unloaded collections?
- [ ] Is there a test covering the query with navigation properties?

### Repository Method Template

```csharp
public async Task<IReadOnlyList<Entity>> GetByXAsync(Guid id, CancellationToken cancellationToken = default)
{
    var result = await _dbSet
        .AsNoTracking()
        .Include(e => e.NavigationProperty1)  // ✅ Load if needed by consumers
        .Include(e => e.NavigationProperty2)  // ✅ Load if needed by consumers
        .Where(e => e.Id == id)
        .ToListAsync(cancellationToken);

    return result;
}
```

---

## Conclusion

**Problem Classification**: ❌ Repository Issue (Missing eager loading)

**Evidence**:
- ✅ Questions ARE saved to database (confirmed via detail endpoint)
- ✅ Backend command handlers work correctly
- ✅ API endpoints are registered and functional
- ❌ Repository query missing `.Include(f => f.Questions)`

**Fix**: Add `.Include(f => f.Questions)` to `EventFormRepository.GetByEventIdAsync()` on line 79.

**Impact**: 1 line change, zero breaking changes, fixes display bug immediately.

**User Impact**: After deployment, users will see correct question counts in forms list.

---

**Status**: Ready for implementation
**Assignee**: Backend Engineer
**Priority**: High (UX degradation)
**Estimated Time**: 15 minutes (1 line change + tests + deployment)
