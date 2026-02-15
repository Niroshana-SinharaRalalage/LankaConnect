# Form Update Timeout Issue - User Action Guide

**Issue**: "Error - timeout of 30000ms exceeded" when trying to update a form response

**Date**: 2026-02-13

---

## Quick Diagnostic Steps (Do These First)

### Step 1: Run Diagnostic Script

Open PowerShell in the project directory and run:

```powershell
.\scripts\diagnose_form_update_timeout.ps1
```

This script will:
- ✅ Check if backend deployment is up-to-date
- ✅ Search logs for form update operations
- ✅ Identify if requests are reaching the backend
- ✅ Detect hanging operations
- ✅ Provide specific next steps

**Time**: 2-3 minutes

---

### Step 2: User Browser Testing

While the script runs backend diagnostics, perform these browser tests:

#### A. Hard Refresh (Clear Frontend Cache)

1. Open the form update page
2. Press **Ctrl+Shift+R** (Windows) or **Cmd+Shift+R** (Mac)
3. Try updating the form again

**Why**: Frontend might be serving old JavaScript code from cache

---

#### B. Inspect Network Request

1. Open DevTools (**F12**)
2. Go to **Network** tab
3. Try updating the form
4. Find the PUT request (should be `/api/events/{id}/forms/{formId}/responses/{responseId}`)

**Screenshot the following**:
- Request **Payload** (click on the request → Payload tab)
  - ✅ Verify it shows `Answers` (capital A), not `answers` (lowercase)
- Request **Timing** (click on the request → Timing tab)
  - ✅ Check how long before timeout (should be exactly 30.00s)
- Response **Status**
  - What error code? 408? 504? Or no response at all?

**Example Screenshot to Take**:
```
PUT /api/events/.../forms/.../responses/...
Status: (failed) timeout of 30000ms exceeded
Payload:
{
  "Answers": [  ← VERIFY CAPITAL 'A'
    {
      "questionId": "...",
      "textValue": "..."
    }
  ]
}
Timing: 30.00s
```

---

#### C. Check Browser Console

1. Open DevTools (**F12**)
2. Go to **Console** tab
3. Try updating the form
4. Look for:
   - Red error messages
   - CORS errors (Access-Control-Allow-Origin)
   - JavaScript exceptions

**Screenshot any red errors**

---

## Most Likely Scenarios & Solutions

Based on diagnostic script results, you'll see one of these:

---

### Scenario 1: Backend Deployment Outdated

**Symptoms**:
- Script shows: "Latest revision is from BEFORE the fix"
- No logs showing recent UpdateFormResponse operations

**Solution**:
```bash
# Redeploy backend to staging
git push origin develop
# Wait for GitHub Actions to complete
```

**Time**: 5-10 minutes

---

### Scenario 2: Frontend Cache Issue

**Symptoms**:
- Script shows backend is up-to-date
- No requests in backend logs when user tries to update
- Browser Network tab shows request going to old URL or old payload format

**Solution**:
1. Hard refresh: **Ctrl+Shift+R**
2. Clear browser cache:
   - Chrome: Settings → Privacy → Clear browsing data → Cached images and files
   - Firefox: Settings → Privacy → Clear Data → Cached Web Content
3. Try updating form again

**Time**: 1 minute

---

### Scenario 3: Backend Timeout (Most Common)

**Symptoms**:
- Script shows: "HANGING OPERATIONS: X (started but never completed)"
- Backend logs show "UpdateFormResponse START" but no "COMPLETE"
- Request in browser times out at exactly 30.00 seconds

**Root Cause**: Backend is processing the request but takes >30 seconds

**Quick Fix** (5 minutes):

Edit file: `web/src/infrastructure/api/repositories/events.repository.ts`

Find line ~1355 (in `updateFormResponse` method):
```typescript
// BEFORE
await apiClient.put(url, request);

// AFTER
await apiClient.put(url, request, { timeout: 60000 }); // 60 seconds
```

Save, commit, push:
```bash
git add web/src/infrastructure/api/repositories/events.repository.ts
git commit -m "fix(forms): Increase form update timeout to 60s (Phase 6A.111)"
git push origin develop
```

Wait for frontend deployment (Vercel), then test again.

---

**Durable Fix** (30 minutes):

The backend is slow due to missing database indexes. Create migration:

```bash
dotnet ef migrations add Phase6A111_AddFormResponseIndexes --project src/LankaConnect.Infrastructure
```

Edit the generated migration file:
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Speed up GetByIdWithAnswersAsync
    migrationBuilder.CreateIndex(
        name: "IX_FormAnswers_FormResponseId",
        schema: "events",
        table: "FormAnswers",
        column: "FormResponseId");

    // Speed up GetByIdWithQuestionsAsync
    migrationBuilder.CreateIndex(
        name: "IX_FormQuestions_EventFormId",
        schema: "events",
        table: "FormQuestions",
        column: "EventFormId");

    // Speed up question option lookups
    migrationBuilder.CreateIndex(
        name: "IX_QuestionOptions_FormQuestionId_SortOrder",
        schema: "events",
        table: "QuestionOptions",
        columns: new[] { "FormQuestionId", "SortOrder" });
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropIndex(
        name: "IX_FormAnswers_FormResponseId",
        schema: "events",
        table: "FormAnswers");

    migrationBuilder.DropIndex(
        name: "IX_FormQuestions_EventFormId",
        schema: "events",
        table: "FormQuestions");

    migrationBuilder.DropIndex(
        name: "IX_QuestionOptions_FormQuestionId_SortOrder",
        schema: "events",
        table: "QuestionOptions");
}
```

Commit and deploy:
```bash
git add .
git commit -m "perf(forms): Add database indexes for form response operations (Phase 6A.111)"
git push origin develop
```

**Why this works**: Database queries will use indexes instead of full table scans, reducing query time from 30s to <1s.

---

### Scenario 4: Network/CORS Issue

**Symptoms**:
- Script shows: "No requests reaching backend"
- Browser console shows CORS errors
- Network tab shows request fails immediately (not 30s timeout)

**Solution**:
1. Check browser console for exact error message
2. Verify API URL is correct: `https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io`
3. Check if user is on localhost (cross-origin issue)

**Time**: 5 minutes

---

## After Fix: Verification Steps

1. ✅ Form update completes without timeout
2. ✅ Success message appears: "Response updated successfully!"
3. ✅ Browser Network tab shows request completed in <10 seconds
4. ✅ Backend logs show "UpdateFormResponse COMPLETE" with duration <10s

---

## Need More Help?

If none of the above solutions work, provide the following information:

1. **Diagnostic script output** (copy entire PowerShell output)
2. **Browser screenshots**:
   - Network tab showing the failing PUT request
   - Request payload (verify `Answers` capitalization)
   - Console tab showing any errors
3. **User environment**:
   - Browser (Chrome, Firefox, Edge?)
   - OS (Windows, Mac, Linux?)
   - Internet connection (WiFi, VPN, corporate network?)

---

## Technical Details

For engineers investigating this issue, see the full Root Cause Analysis document:
- **File**: `docs/RCA_PHASE_6A111_FORM_UPDATE_TIMEOUT_DEEP_INVESTIGATION.md`
- **Sections**:
  - Phase 1: Deployment verification
  - Phase 2: Layer analysis (frontend, proxy, backend, database)
  - Phase 3: Hypothesis generation
  - Phase 4: Diagnostic execution plan
  - Phase 5: Conditional fixes

---

## Summary

**Most Likely Issue**: Backend timeout due to slow database queries

**Quick Fix**: Increase frontend timeout to 60 seconds

**Durable Fix**: Add database indexes

**Diagnostic Tool**: `scripts/diagnose_form_update_timeout.ps1`

**Total Time**: 5-30 minutes depending on root cause
