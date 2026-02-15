# Deep Root Cause Analysis - Form Update Timeout (Phase 6A.111)

**Date**: 2026-02-13
**Status**: Active Investigation
**Issue**: Form update still times out after initial fixes (case mismatch + nullable request body)

---

## Executive Summary

**Most Likely Root Cause**: Backend database query or transaction deadlock causing processing time > 30 seconds.

**Confidence Level**: High (75%)

**Evidence**:
1. ✅ Fixes deployed successfully (commit 71feed37)
2. ✅ GET works (form loads with existing data)
3. ✅ User authenticated (logged in)
4. ❌ PUT times out at exactly 30 seconds
5. Frontend timeout: `api-client.ts` line 42: `timeout: config?.timeout || 30000`

**Hypothesis**: Backend IS receiving the request but CANNOT complete processing within 30 seconds. Either:
- Database query is slow (full table scan, missing index)
- Transaction deadlock or lock contention
- Event handler processing (email sending) blocking the response
- EF Core query inefficiency

---

## Phase 1: Verify Deployment Reality

### Questions to Answer
1. Is the frontend using NEW code or is browser cache serving OLD code?
2. Is the backend container running NEW image or OLD image?
3. Are Azure logs showing the NEW logging we added?

### Diagnostic Commands

```bash
# 1. Check Azure Container App revision history
az containerapp revision list \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query "[].{name:name,active:properties.active,trafficWeight:properties.trafficWeight,createdTime:properties.createdTime,replicas:properties.replicas}" \
  -o table

# Expected: Latest revision from 2026-02-13 00:29:18Z (commit 71feed37) should be ACTIVE with 100% traffic

# 2. Check recent logs for UpdateFormResponse
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 500 \
  --follow

# Look for:
# - "UpdateFormResponse START: ResponseId=..."
# - "UpdateFormResponse COMPLETE: ResponseId=..." (this is missing if timeout)
# - "UpdateFormResponse FAILED: ..." (validation error)
# - Any exceptions or errors

# 3. Check specific logs for timeout pattern
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 1000 \
  | grep -i "UpdateFormResponse"

# 4. Check if any errors occurred in the last hour
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 1000 \
  | grep -i "error\|exception\|timeout"
```

### User Actions Required

**Step 1: Hard refresh frontend** (to bypass cache)
1. Open DevTools (F12)
2. Hard refresh: `Ctrl+Shift+R` (Windows) or `Cmd+Shift+R` (Mac)
3. Go to Network tab
4. Try updating the form
5. Inspect the PUT request:
   - Request payload → Does it show `Answers` (capital A) or `answers` (lowercase)?
   - Response → What is the HTTP status? 408? 504? Or no response at all?
   - Timing → How long before timeout? Should be exactly 30.00 seconds

**Step 2: Check browser console**
1. Open Console tab
2. Look for the request log: `🚀 API Request: PUT ...`
3. Check the request payload structure
4. Look for any JavaScript errors

**Step 3: Send screenshot**
- Screenshot of Network tab showing the failing PUT request
- Screenshot of request payload
- Screenshot of Console logs

---

## Phase 2: Identify Where the Timeout Occurs

### Layer Analysis

| Layer | Symptom | Diagnostic |
|-------|---------|-----------|
| **Frontend (30s timeout)** | Browser Network tab shows request pending for exactly 30s, then error | Frontend timing out before backend responds |
| **Azure Ingress (240s timeout)** | Backend logs show request START but no COMPLETE within 240s | Azure proxy timing out |
| **Backend Processing** | Backend logs show START and COMPLETE in >30s but <240s | Backend slow but functional |
| **Database** | Backend logs show START, then hang indefinitely | Database query deadlock or slow query |

### Backend Code Analysis

**File**: `src/LankaConnect.Application/Events/Commands/UpdateFormResponse/UpdateFormResponseCommandHandler.cs`

**Critical Operations** (in order):
1. Line 47: `GetByIdWithAnswersAsync()` - Loads response with answers (JOIN query)
2. Line 114: `GetByIdWithQuestionsAsync()` - Loads form with questions (JOIN query)
3. Lines 135-201: Loop through answers, updating each (N queries if not batched)
4. Line 204: `_unitOfWork.CommitAsync()` - Transaction commit
5. **Domain Events**: `FormResponseUpdated` event triggers email sending (ASYNC)

**Potential Bottlenecks**:
1. ❌ `GetByIdWithAnswersAsync()` - EF Core query might do N+1 queries
2. ❌ `GetByIdWithQuestionsAsync()` - Another JOIN query
3. ❌ Answer update loop (lines 137-201) - Could be slow if many questions
4. ❌ `CommitAsync()` - Transaction commit triggers domain events
5. ❌ **Email sending** - If `FormResponseUpdatedEmailHandler` runs SYNCHRONOUSLY, it blocks the response

### Email Handler Investigation

**File**: `src/LankaConnect.Application/Events/EventHandlers/FormResponseUpdatedEmailHandler.cs`

**Critical Question**: Is this handler blocking the HTTP response?

```bash
# Search for how email handlers are registered
grep -r "FormResponseUpdatedEmailHandler" c:/Work/LankaConnect/src --include="*.cs"

# Check if it's async or sync
grep -A 20 "class FormResponseUpdatedEmailHandler" c:/Work/LankaConnect/src/LankaConnect.Application/Events/EventHandlers/FormResponseUpdatedEmailHandler.cs
```

---

## Phase 3: Hypothesis Generation

### Hypothesis 1: Frontend Timeout Too Short (60% probability)

**Evidence**:
- Frontend timeout: 30 seconds (line 42 of api-client.ts)
- Backend might be processing correctly but takes 35-40 seconds
- Video upload timeout is 300s, suggesting some operations need longer

**Test**:
1. Check Azure logs - does backend show "UpdateFormResponse COMPLETE" AFTER frontend times out?
2. If YES → Frontend timeout is too short
3. If NO → Backend is hanging

**Fix**: Increase frontend timeout for form update operations:
```typescript
// In events.repository.ts, updateFormResponse method
await apiClient.put(url, request, { timeout: 60000 }); // 60s instead of default 30s
```

---

### Hypothesis 2: Database Query Performance (75% probability) ⭐ MOST LIKELY

**Evidence**:
- `GetByIdWithAnswersAsync()` loads FormResponse + Answers (JOIN)
- `GetByIdWithQuestionsAsync()` loads EventForm + Questions + Options (JOIN JOIN)
- Forms can have 20+ questions with 10+ options each
- Answers table might have missing indexes

**Symptoms in Azure logs**:
- "UpdateFormResponse START" exists
- No "UpdateFormResponse COMPLETE" (hanging indefinitely)
- OR "UpdateFormResponse COMPLETE" shows duration > 30s

**Root Causes**:
1. Missing index on `FormAnswers.FormResponseId`
2. Missing index on `FormQuestions.EventFormId`
3. EF Core generating inefficient SQL (N+1 queries)
4. Database lock contention (another transaction holding lock)

**Diagnostic SQL**:
```sql
-- Check for missing indexes
SELECT
    tablename,
    indexname,
    indexdef
FROM pg_indexes
WHERE tablename IN ('form_responses', 'form_answers', 'event_forms', 'form_questions')
ORDER BY tablename, indexname;

-- Check for slow queries
SELECT
    query,
    calls,
    total_exec_time,
    mean_exec_time,
    max_exec_time
FROM pg_stat_statements
WHERE query LIKE '%form_responses%' OR query LIKE '%form_answers%'
ORDER BY mean_exec_time DESC
LIMIT 10;

-- Check for active locks
SELECT
    pid,
    state,
    query,
    wait_event_type,
    wait_event
FROM pg_stat_activity
WHERE state != 'idle'
AND query LIKE '%form%';
```

**Fix Options**:
1. Add indexes to FormAnswers, FormQuestions tables
2. Optimize EF Core queries (use `.Include()` instead of separate queries)
3. Add query hints for PostgreSQL

---

### Hypothesis 3: Email Handler Blocking Response (40% probability)

**Evidence**:
- `FormResponseUpdatedEmailHandler` sends email when form response is updated
- If handler is registered as SYNCHRONOUS, it blocks `CommitAsync()`
- Email sending can take 2-5 seconds (Azure Communication Services)
- If handler fails (Azure 429 rate limit), retry logic adds delays

**Test**:
```bash
# Check if email handler is async
grep -A 30 "class FormResponseUpdatedEmailHandler" c:/Work/LankaConnect/src/LankaConnect.Application/Events/EventHandlers/FormResponseUpdatedEmailHandler.cs

# Check DI registration (sync vs async)
grep -B 5 -A 5 "FormResponseUpdatedEmailHandler" c:/Work/LankaConnect/src/LankaConnect.API/Program.cs
```

**Fix**:
1. Make email handler fully async (use Hangfire background job)
2. OR disable email sending for form updates temporarily

---

### Hypothesis 4: Azure Proxy/Ingress Timeout (20% probability)

**Evidence**:
- Azure Container Apps ingress default timeout: 240 seconds
- If backend responds in 31 seconds, frontend times out at 30s
- But request actually succeeded on backend

**Test**:
1. Check Azure logs for "UpdateFormResponse COMPLETE" at timestamp AFTER frontend timeout
2. Check database - was the response actually updated despite timeout error?

**Fix**:
- Check Azure Container App ingress settings
- Verify no custom timeout configurations

---

## Phase 4: Diagnostic Execution Plan

### Step 1: User Verification (Frontend)

**User performs**:
1. Hard refresh (Ctrl+Shift+R)
2. Open DevTools → Network tab
3. Try updating form response
4. Screenshot:
   - Request payload (verify `Answers` with capital A)
   - Request timing (verify 30.00s timeout)
   - Response status

**Expected if frontend cache issue**:
- Still shows `answers` (lowercase)

**Expected if NEW code**:
- Shows `Answers` (capital A)

---

### Step 2: Backend Log Analysis (Azure CLI)

**Engineer performs**:
```bash
# Real-time logs
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 200 \
  --follow

# While logs are streaming, ask user to try updating form
# Watch for:
# 1. "UpdateFormResponse START" - request received
# 2. "UpdateFormResponse COMPLETE" - request finished
# 3. Duration in log message - how long did it take?
```

**Interpretation**:

| Log Pattern | Root Cause | Next Action |
|-------------|-----------|-------------|
| No "START" log | Request not reaching backend | Check frontend deployment, CORS, network |
| "START" but no "COMPLETE" | Backend hanging | Check database locks, query performance |
| "START" + "COMPLETE" in <30s | Frontend cache issue | Hard refresh, clear cache |
| "START" + "COMPLETE" in >30s | Frontend timeout too short | Increase timeout |
| "START" + Exception | Backend error | Check exception details |

---

### Step 3: Database Query Analysis

**If backend logs show hanging**, run:

```sql
-- Check active locks (run while form update is in progress)
SELECT
    pid,
    usename,
    application_name,
    state,
    query,
    wait_event_type,
    wait_event,
    query_start,
    state_change
FROM pg_stat_activity
WHERE state != 'idle'
ORDER BY query_start;

-- Check table sizes (large tables = slow queries)
SELECT
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size,
    n_live_tup AS row_count
FROM pg_stat_user_tables
WHERE tablename LIKE '%form%'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;

-- Check missing indexes
SELECT
    schemaname,
    tablename,
    attname,
    n_distinct,
    correlation
FROM pg_stats
WHERE tablename IN ('form_responses', 'form_answers')
ORDER BY tablename, attname;
```

---

### Step 4: Email Handler Check

```bash
# Check if email handler is blocking
cd c:/Work/LankaConnect
grep -A 30 "class FormResponseUpdatedEmailHandler" src/LankaConnect.Application/Events/EventHandlers/FormResponseUpdatedEmailHandler.cs

# Expected: Should use INotificationHandler<FormResponseUpdatedEvent>
# Expected: Should be async (Task Handle(...))
```

**If handler is synchronous**:
- This is likely the bottleneck
- Email sending (2-5s) + Azure retries (2s, 4s, 8s) = 20+ seconds
- Fix: Make handler async using Hangfire

---

## Phase 5: Immediate Fixes (Conditional)

### Fix 1: Frontend Timeout (If backend logs show >30s but <60s)

**File**: `web/src/infrastructure/api/repositories/events.repository.ts`

**Line 1355**:
```typescript
// BEFORE
await apiClient.put(url, request);

// AFTER
await apiClient.put(url, request, { timeout: 60000 }); // 60 seconds
```

---

### Fix 2: Database Indexes (If slow queries detected)

**Create migration**:
```bash
cd c:/Work/LankaConnect
dotnet ef migrations add Phase6A111_AddFormResponseIndexes --project src/LankaConnect.Infrastructure
```

**Migration content**:
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Index for GetByIdWithAnswersAsync
    migrationBuilder.CreateIndex(
        name: "IX_FormAnswers_FormResponseId",
        schema: "events",
        table: "FormAnswers",
        column: "FormResponseId");

    // Index for GetByIdWithQuestionsAsync
    migrationBuilder.CreateIndex(
        name: "IX_FormQuestions_EventFormId",
        schema: "events",
        table: "FormQuestions",
        column: "EventFormId");

    // Composite index for question options
    migrationBuilder.CreateIndex(
        name: "IX_QuestionOptions_FormQuestionId_SortOrder",
        schema: "events",
        table: "QuestionOptions",
        columns: new[] { "FormQuestionId", "SortOrder" });
}
```

---

### Fix 3: Email Handler Async (If handler is blocking)

**File**: `src/LankaConnect.Application/Events/EventHandlers/FormResponseUpdatedEmailHandler.cs`

**Convert to Hangfire background job**:
```csharp
public async Task Handle(FormResponseUpdatedEvent notification, CancellationToken cancellationToken)
{
    // Instead of sending email synchronously, queue it
    BackgroundJob.Enqueue<IEmailService>(x =>
        x.SendFormResponseUpdatedEmailAsync(
            notification.FormId,
            notification.ResponseId,
            CancellationToken.None));

    await Task.CompletedTask;
}
```

---

## Phase 6: Monitoring & Verification

### After Fix Applied

1. **Check Azure logs** for successful completion
2. **Test form update** in staging
3. **Measure duration** - should be <10 seconds
4. **Verify email** is still sent (if async)

### Success Criteria

- ✅ Form update completes in <10 seconds
- ✅ No timeout errors in browser
- ✅ Backend logs show "UpdateFormResponse COMPLETE" with duration <10s
- ✅ Email still sent (if applicable)
- ✅ Database response updated correctly

---

## Next Steps

**Immediate** (User):
1. Hard refresh browser (Ctrl+Shift+R)
2. Try updating form
3. Send screenshot of Network tab (request payload + timing)

**Immediate** (Engineer):
1. Run Azure CLI commands to check logs
2. Look for "UpdateFormResponse START/COMPLETE" pattern
3. Measure duration

**Conditional** (Based on findings):
- If frontend cache → Clear cache, redeploy frontend
- If backend timeout → Increase frontend timeout to 60s
- If database slow → Add indexes, optimize queries
- If email blocking → Convert to Hangfire background job

---

## Risk Assessment

| Hypothesis | Probability | Impact | Effort to Fix |
|------------|------------|--------|---------------|
| Frontend timeout too short | 60% | Low | 5 min |
| Database query slow | 75% | High | 30 min |
| Email handler blocking | 40% | Medium | 15 min |
| Azure proxy timeout | 20% | Low | 10 min |

**Recommended Approach**: Multi-pronged attack
1. First: Check Azure logs (5 min diagnostic)
2. Second: Increase frontend timeout to 60s (quick win)
3. Third: Add database indexes (durable fix)
4. Fourth: Convert email handler to async (best practice)

---

## Appendix A: Code References

### Frontend Request Flow
```
page.tsx (line 164-170)
  → updateMutation.mutateAsync()
    → useUpdateFormResponse hook
      → eventsRepository.updateFormResponse() (line 1344-1356)
        → apiClient.put(url, request)
          → axios.put() with 30s timeout (api-client.ts line 42)
```

### Backend Processing Flow
```
PUT /api/events/{id}/forms/{formId}/responses/{responseId}
  → FormResponseController.UpdateFormResponse()
    → Mediator.Send(UpdateFormResponseCommand)
      → UpdateFormResponseCommandHandler.Handle() (lines 30-222)
        1. GetByIdWithAnswersAsync() - Load response
        2. Auth check (lines 61-102)
        3. GetByIdWithQuestionsAsync() - Load form
        4. Deadline check (lines 125-132)
        5. Update answers loop (lines 137-201)
        6. _unitOfWork.CommitAsync() - Transaction + events
          → FormResponseUpdatedEvent published
            → FormResponseUpdatedEmailHandler.Handle()
              → Send email (BLOCKING?)
```

### Database Schema
```
events.EventForms (id, eventId, title, description, status, deadlines)
  ↓ 1:N
events.FormQuestions (id, eventFormId, questionText, questionType, sortOrder)
  ↓ 1:N
events.QuestionOptions (id, formQuestionId, text, sortOrder)

events.FormResponses (id, eventFormId, respondentName, respondentEmail, userId, accessTokenHash, submittedAt)
  ↓ 1:N
events.FormAnswers (id, formResponseId, formQuestionId, questionText, textValue, booleanValue, selectedOptionIds, selectedOptionTextSnapshots)
```

---

## Document Status

- **Created**: 2026-02-13
- **Last Updated**: 2026-02-13
- **Status**: Investigation in progress
- **Next Review**: After user provides diagnostic results
