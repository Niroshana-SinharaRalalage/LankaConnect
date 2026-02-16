# COMPREHENSIVE ROOT CAUSE ANALYSIS - PHASE 6A.115 POST-DEPLOYMENT ISSUES

**Date**: 2026-02-15
**Analyzer**: Claude Sonnet 4.5
**Scope**: Analysis of 9 critical issues discovered during Phase 6A.115 staging verification
**Deployment Status**: Phase 6A.115 changes SUCCESSFULLY DEPLOYED (commit d2bc4bcb deployed at 2026-02-15T15:42:28Z)

---

## EXECUTIVE SUMMARY

**Overall Findings**: Analysis reveals **7 backend issues**, **2 UI issues**, **0 auth-specific issues** across 9 reported problems. **CRITICAL DISCOVERY**: Despite Phase 6A.115 code being successfully deployed to staging (verified via GitHub Actions run 22038432032), **the email handler changes are NOT producing expected output**, indicating either:
1. Migration Phase6A112 was NOT applied to staging database, OR
2. Email template parameters are still mismatched, OR
3. Email is being sent from cached/wrong template

**Severity Assessment**:
- **P0 (Critical)**: 4 issues - Email rendering broken (Issues #4, #5, #8, #9)
- **P1 (High)**: 3 issues - UX degradation (Issues #1, #2, #6)
- **P2 (Medium)**: 2 issues - Feature missing (Issues #3, #7)

**Recommended Approach**:
1. **Immediate (Today)**: Verify Phase6A112 migration applied to staging database
2. **High Priority (Tomorrow)**: Fix email template issues and cache invalidation
3. **Medium Priority (Next Week)**: Implement UI enhancements

---

## DEPLOYMENT VERIFICATION STATUS

### ✅ Backend Code Deployment: CONFIRMED

**Evidence**:
- Commit `d2bc4bcb` successfully deployed via GitHub Actions
- Workflow: `Deploy to Azure Staging` (Run ID: 22038432032)
- Status: SUCCESS
- Completed: 2026-02-15T15:42:28Z (8m 43s)
- Branch: `develop`

**Deployed Changes**:
1. ✅ `FormResponseUpdatedEmailHandler.cs` - Line 208 uses `string.Join("<br/>", summaryParts)`
2. ✅ `web/src/app/events/[id]/forms/[formId]/page.tsx` - Messages at bottom, scroll behavior updated

### ❌ Database Migration: UNKNOWN STATUS

**Critical Gap**: No confirmation that Phase6A112 migration was applied to staging database.

**Migration Details**:
- File: `20260214211455_Phase6A112_UpdateFormResponseEmailTemplatesWithProfessionalStyling.cs`
- Commit: `34a0ca70` (2026-02-15)
- Purpose: Update 3 form response email templates with professional styling
- Templates Modified:
  1. `template-form-response-confirmation`
  2. `template-form-response-update`
  3. `template-form-response-cancellation`

**Investigation Required**:
```sql
-- Check if migration was applied
SELECT * FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20260214211455_Phase6A112_UpdateFormResponseEmailTemplatesWithProfessionalStyling';

-- Check template content
SELECT name, subject_template, LEFT(html_template, 200) as html_preview
FROM communications.email_templates
WHERE name IN (
  'template-form-response-confirmation',
  'template-form-response-update',
  'template-form-response-cancellation'
);
```

---

## ISSUE-BY-ISSUE ANALYSIS

### Issue #1: First-Time Form Visitors See "Edit Your Response"

**Classification**: UI Issue (Frontend Logic)
**Severity**: P1 (High - Confusing UX)
**Status**: NOT FIXED

#### Root Cause

**Problem**: UI shows "Edit Your Response" button for users who haven't submitted a response yet.

**Evidence from Code** (`web/src/app/events/[id]/forms/[formId]/page.tsx`):
```typescript
// Line 376-397: Conditional rendering based on existingResponse
{existingResponse && (
  <div className="mt-4 p-4 bg-blue-50 border border-blue-200 rounded-lg">
    <div className="flex items-start justify-between">
      <p className="text-sm text-blue-800">
        <strong>Editing your response</strong>
        {form.responseDeadline && !isDeadlinePassed && (
          <span> - You can make changes until {new Date(form.responseDeadline).toLocaleString()}</span>
        )}
      </p>
```

**Actual Cause**: The component doesn't differentiate between:
1. First-time visitor (no response) → Should show "Fill Out Form"
2. Returning visitor (has response) → Should show "Editing your response"

**Current Behavior**:
- Page fetches `existingResponse` via `useMyFormResponse()` hook
- If response exists, shows editing message
- BUT: The issue is that the button/CTA doesn't change based on this state

**Where the Issue Occurs**:
- Submit button area (around line 400-415) always shows generic "Submit" text
- No conditional rendering like "Submit Response" vs "Update Response"

#### Fix Strategy

**Step 1**: Add conditional button text based on `existingResponse`:
```typescript
<Button type="submit" disabled={!canSubmit || isSubmitting}>
  {isSubmitting ? 'Submitting...' : existingResponse ? 'Update Response' : 'Submit Response'}
</Button>
```

**Step 2**: Update info message for first-time visitors:
```typescript
{!existingResponse && canSubmit && (
  <div className="mt-4 p-4 bg-blue-50 border border-blue-200 rounded-lg">
    <p className="text-sm text-blue-800">
      <strong>Fill out this form</strong> - You can edit your response anytime after submitting.
    </p>
  </div>
)}
```

#### Files to Modify
- `web/src/app/events/[id]/forms/[formId]/page.tsx` (lines 376-415)

#### Effort Estimate
- **Time**: 1 hour
- **Complexity**: Low
- **Testing**: Manual (verify in staging with new form + existing response)

#### Risk Level
- **Risk**: Low
- **Impact**: UI text change only, no business logic

#### Testing Strategy
1. Navigate to form as first-time visitor (no response)
   - Verify: Shows "Submit Response" button and "Fill out this form" message
2. Submit a response
3. Return to same form
   - Verify: Shows "Update Response" button and "Editing your response" message
4. Test with different user accounts and forms

---

### Issue #2: Success Page Doesn't Differentiate Member vs Non-Member Instructions

**Classification**: UI Issue (Frontend Logic)
**Severity**: P1 (High - Confusing UX)
**Status**: NOT FIXED

#### Root Cause

**Problem**: After form submission, success message shows generic edit instructions without distinguishing between logged-in members (can edit via UI) and anonymous users (must use token link).

**Evidence from Code** (`web/src/app/events/[id]/forms/[formId]/page.tsx`):
```typescript
// Lines 100-105: Submit success handler
onSuccess: (data) => {
  if (data.accessToken) {
    const storageKey = `form-response-${eventId}-${formId}`;
    localStorage.setItem(storageKey, data.accessToken);
  }

  setSuccessMessage('Response submitted successfully! You can edit your response until the deadline.');
  // ...
}
```

**Actual Cause**:
- Success message is hardcoded string
- Doesn't check if user is authenticated (`userId` from session)
- Doesn't provide token-based edit link for anonymous users
- Doesn't explain difference between member (login to edit) vs anonymous (use link)

**Current Behavior**:
- ALL users see: "Response submitted successfully! You can edit your response until the deadline."
- No mention of HOW to edit (login vs token link)
- Token stored in localStorage but not displayed to anonymous users

#### Fix Strategy

**Step 1**: Check authentication status:
```typescript
// Add at top of component
const { data: session } = useSession(); // or however auth is handled
const isAuthenticated = !!session?.user;
```

**Step 2**: Conditional success messages:
```typescript
onSuccess: (data) => {
  if (data.accessToken) {
    const storageKey = `form-response-${eventId}-${formId}`;
    localStorage.setItem(storageKey, data.accessToken);
  }

  if (isAuthenticated) {
    setSuccessMessage('Response submitted successfully! You can edit your response anytime by logging in and visiting this form again.');
  } else {
    const editUrl = `${window.location.origin}/events/${eventId}/forms/${formId}?token=${data.accessToken}`;
    setSuccessMessage(`Response submitted successfully! Save this link to edit your response later: ${editUrl} (Click to copy)`);
  }
  // ...
}
```

**Step 3**: Add copy-to-clipboard functionality for anonymous users:
```typescript
const [showCopySuccess, setShowCopySuccess] = useState(false);

const handleCopyEditLink = () => {
  const editUrl = `${window.location.origin}/events/${eventId}/forms/${formId}?token=${localStorage.getItem(`form-response-${eventId}-${formId}`)}`;
  navigator.clipboard.writeText(editUrl);
  setShowCopySuccess(true);
  setTimeout(() => setShowCopySuccess(false), 3000);
};
```

#### Files to Modify
- `web/src/app/events/[id]/forms/[formId]/page.tsx` (lines 100-110, 420-425)

#### Effort Estimate
- **Time**: 2 hours
- **Complexity**: Medium (requires auth state check + UI enhancement)
- **Testing**: Manual (test both authenticated and anonymous flows)

#### Risk Level
- **Risk**: Low-Medium
- **Impact**: UI logic change, depends on auth implementation

#### Testing Strategy
1. **Authenticated User Test**:
   - Login as member
   - Submit form response
   - Verify: Message says "login to edit"
2. **Anonymous User Test**:
   - Open form in incognito window (no login)
   - Submit form response
   - Verify: Message shows edit link with copy button
   - Copy link and open in new browser
   - Verify: Link works and loads existing response
3. **Token Security Test**:
   - Verify token is not logged or exposed in network tab

---

### Issue #3: Edit Link in Another Browser Returns 400 Error

**Classification**: Backend API Issue (Token Validation)
**Severity**: P0 (Critical - Feature Broken)
**Status**: NOT FIXED

#### Root Cause

**Problem**: Opening edit link with token in different browser returns HTTP 400 "Error: The requested operation is invalid."

**URL Pattern**: `/events/{eventId}/forms/{formId}?token={accessToken}`

**Evidence from Backend Code**:

**1. Frontend Token Handling** (`web/src/app/events/[id]/forms/[formId]/page.tsx`):
```typescript
// Lines 61-68: Extract token from URL query params
const searchParams = useSearchParams();
const accessTokenParam = searchParams?.get('token') || null;

// Lines 70-91: Fetch existing response with token
const { data: existingResponse, isLoading: isLoadingResponse } = useMyFormResponse(
  eventId,
  formId,
  accessTokenParam || undefined
);
```

**2. React Query Hook** (`web/src/presentation/hooks/useEventForms.ts`):
```typescript
// Lines 557-589: useMyFormResponse hook
export const useMyFormResponse = (
  eventId: string,
  formId: string,
  accessToken?: string
) => {
  const { getAuthHeaders } = useAuth();

  return useQuery({
    queryKey: formKeys.myResponse(eventId, formId, accessToken),
    queryFn: async () => {
      const headers = await getAuthHeaders();

      // Add token to headers if provided
      if (accessToken) {
        headers['X-Access-Token'] = accessToken;
      }

      const response = await fetch(
        `${API_BASE_URL}/events/${eventId}/forms/${formId}/my-response`,
        { headers }
      );

      if (!response.ok) {
        if (response.status === 404) return null;
        throw new Error('Failed to fetch response');
      }

      return response.json();
    },
    enabled: !!eventId && !!formId,
  });
};
```

**3. Backend Handler** (Need to check `GetFormResponseQuery.cs` and handler):
- Expected: Backend should validate `X-Access-Token` header
- Actual: Backend is returning 400 error instead of 401/403

**Hypotheses for 400 Error**:
1. **Token not being sent**: Frontend might not be passing token in headers correctly
2. **Backend validation failing**: `UpdateFormResponseCommandHandler.cs` might have different validation logic
3. **Token expired**: AccessToken might have expiration that's too short
4. **Token format mismatch**: Token encoding/decoding issue

**Investigation Required**:
```bash
# Check backend handler for token validation
grep -r "X-Access-Token" src/LankaConnect.API/
grep -r "AccessToken" src/LankaConnect.Application/Events/Commands/UpdateFormResponseCommand.cs
```

#### Fix Strategy

**Step 1: Verify Token Flow** (Investigation Phase)
1. Add browser console logging in frontend:
```typescript
console.log('Token from URL:', accessTokenParam);
console.log('Headers being sent:', headers);
```

2. Add backend logging in handler:
```csharp
_logger.LogInformation(
    "UpdateFormResponse: Received token from header: {HasToken}, UserId: {UserId}",
    Request.Headers.ContainsKey("X-Access-Token"),
    User?.Identity?.Name ?? "Anonymous"
);
```

3. Test with curl:
```bash
# Get token from submit response
TOKEN="<token_from_response>"

# Try to fetch with token
curl -X GET \
  "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events/{eventId}/forms/{formId}/my-response" \
  -H "X-Access-Token: $TOKEN" \
  -v
```

**Step 2: Fix Backend Validation** (Based on Investigation)

Likely issue: Backend handler expects authenticated user OR valid token, but validation logic is incorrect.

**Current Code** (Assumption - need to verify):
```csharp
// UpdateFormResponseCommandHandler.cs or GetFormResponseQueryHandler.cs
public async Task<FormResponseDto> Handle(GetFormResponseQuery request, CancellationToken cancellationToken)
{
    var userId = _currentUserService.UserId; // Returns null for anonymous

    if (userId == null)
    {
        throw new UnauthorizedAccessException(); // Returns 401, not 400
    }

    // Missing: Check for X-Access-Token header
}
```

**Fixed Code**:
```csharp
public async Task<FormResponseDto> Handle(GetFormResponseQuery request, CancellationToken cancellationToken)
{
    var userId = _currentUserService.UserId;
    var accessToken = _httpContextAccessor.HttpContext?.Request.Headers["X-Access-Token"].FirstOrDefault();

    // Allow EITHER authenticated user OR valid token
    if (userId == null && string.IsNullOrWhiteSpace(accessToken))
    {
        _logger.LogWarning("GetFormResponse: No userId and no access token provided");
        throw new UnauthorizedAccessException("Authentication required");
    }

    // If token provided, validate and get response by token
    if (!string.IsNullOrWhiteSpace(accessToken))
    {
        var response = await _formResponseRepository.GetByAccessTokenAsync(accessToken, cancellationToken);
        if (response == null)
        {
            _logger.LogWarning("GetFormResponse: Invalid access token: {Token}", accessToken);
            throw new UnauthorizedAccessException("Invalid access token");
        }
        return _mapper.Map<FormResponseDto>(response);
    }

    // Otherwise, get by userId
    var userResponse = await _formResponseRepository.GetByUserIdAndFormIdAsync(userId.Value, request.FormId, cancellationToken);
    if (userResponse == null)
    {
        return null; // 404
    }
    return _mapper.Map<FormResponseDto>(userResponse);
}
```

**Step 3: Add Repository Method** (If Missing)
```csharp
// IFormResponseRepository.cs
Task<FormResponse?> GetByAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default);

// FormResponseRepository.cs
public async Task<FormResponse?> GetByAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default)
{
    return await _context.FormResponses
        .Include(r => r.Answers)
        .FirstOrDefaultAsync(r => r.AccessToken == accessToken, cancellationToken);
}
```

#### Files to Modify
1. Backend:
   - `src/LankaConnect.Application/Events/Queries/GetFormResponseQuery.cs`
   - `src/LankaConnect.Application/Events/Queries/GetFormResponseQueryHandler.cs`
   - `src/LankaConnect.Application/Events/Commands/UpdateFormResponseCommandHandler.cs`
   - `src/LankaConnect.Domain/Events/Repositories/IFormResponseRepository.cs`
   - `src/LankaConnect.Infrastructure/Events/Repositories/FormResponseRepository.cs`

2. Frontend (if needed):
   - `web/src/presentation/hooks/useEventForms.ts` (verify token header is sent)

#### Effort Estimate
- **Investigation**: 2 hours
- **Backend Fix**: 4 hours
- **Testing**: 2 hours
- **Total**: 8 hours (1 day)

#### Risk Level
- **Risk**: High
- **Impact**: Affects anonymous user edit functionality, requires backend authentication logic changes

#### Testing Strategy
1. **Token Generation Test**:
   - Submit form as anonymous user
   - Capture token from response JSON
2. **Token Validation Test**:
   - Open edit URL with token in different browser (incognito)
   - Verify: Form loads with existing response data
3. **Security Test**:
   - Try to access other user's response with invalid token → Expect 401/403
   - Try expired token → Expect 401
4. **Authenticated User Test**:
   - Login and edit form without token
   - Verify: Works normally with userId-based auth

---

### Issue #4: Email Has Placeholder Parameters Not Replaced

**Classification**: Backend API Issue (Email Template Rendering)
**Severity**: P0 (Critical - Email Broken)
**Status**: NOT FIXED

#### Root Cause

**Problem**: Email shows raw Handlebars placeholders (`{{UserName}}`, `{{ContactEmail}}`, etc.) instead of actual values.

**Evidence from Screenshot**: Email displays:
```
Hi {{UserName}},
Contact: {{ContactEmail}}
```

**Expected Output**:
```
Hi Niroshana Sinharage,
Contact: niroshhh@gmail.com
```

**Root Cause Analysis**:

**Hypothesis 1: Migration Not Applied** (MOST LIKELY)
- Phase6A112 migration updates email templates
- If migration wasn't run on staging database, templates still have old structure
- Old templates might have different parameter names or missing parameters

**Hypothesis 2: Parameter Name Mismatch**
- `FormResponseEmailParams.cs` sends `userName` but template expects `{{UserName}}`
- Handlebars is case-sensitive

**Hypothesis 3: Wrong Template Being Used**
- Handler sends to `template-form-response-update`
- But Azure Email Service uses different template name

**Evidence from Code**:

**1. Email Handler** (`FormResponseUpdatedEmailHandler.cs`):
```csharp
// Lines 108-121: Email parameters creation
var emailParams = FormResponseEmailParams.CreateUpdate(
    userName: response.RespondentName ?? "User",
    userEmail: response.RespondentEmail,
    eventId: eventEntity.Id,
    eventTitle: eventEntity.Title.Value,
    formTitle: form.Title,
    responseSummary: responseSummary,
    editFormUrl: editUrl,
    eventStartDate: eventEntity.StartDate,
    timeZoneId: eventEntity.TimeZoneId,
    eventLocation: eventEntity.Location?.ToString() ?? "TBA",
    eventDetailsUrl: _emailUrlHelper.BuildEventDetailsUrl(eventEntity.Id),
    updatedAt: domainEvent.OccurredAt
);
```

**2. Email Parameters Class** (Need to check):
```bash
# Check FormResponseEmailParams.cs
grep -A30 "CreateUpdate" src/LankaConnect.Shared/Email/TypedEmailParams/FormResponseEmailParams.cs
```

**3. Migration SQL** (`20260214211455_Phase6A112_...cs`):
```csharp
// Lines 56-64: Updates template-form-response-update
migrationBuilder.Sql($@"
    UPDATE communications.email_templates
    SET
        subject_template = '{{{{EventTitle}}}} - Form Response Updated',
        html_template = '{EscapeSql(updateHtml)}',
        updated_at = NOW()
    WHERE name = 'template-form-response-update';
");
```

**Investigation Required**:
```sql
-- 1. Check if migration was applied
SELECT * FROM "__EFMigrationsHistory"
WHERE "MigrationId" LIKE '%Phase6A112%'
ORDER BY "MigrationId" DESC;

-- 2. Check current template content
SELECT
    name,
    subject_template,
    CASE
        WHEN html_template LIKE '%{{UserName}}%' THEN 'Has UserName placeholder'
        WHEN html_template LIKE '%{{userName}}%' THEN 'Has userName placeholder'
        ELSE 'No UserName placeholder found'
    END as user_name_check,
    LENGTH(html_template) as template_size,
    updated_at
FROM communications.email_templates
WHERE name = 'template-form-response-update';

-- 3. Extract first 500 chars of template to see structure
SELECT
    name,
    SUBSTRING(html_template FROM 1 FOR 500) as html_preview
FROM communications.email_templates
WHERE name = 'template-form-response-update';
```

#### Fix Strategy

**Step 1: Database Investigation** (CRITICAL - Do First)
1. Connect to staging database
2. Run queries above to check:
   - Is Phase6A112 migration applied?
   - What does current template content look like?
   - What parameter names does template use?

**Step 2: Apply Migration** (If Not Applied)
```bash
# Connect to staging Azure Container Apps
az containerapp exec \
  --name lankaconnect-api-staging \
  --resource-group <resource-group> \
  --command "/bin/bash"

# Inside container, run migrations
dotnet ef database update --project src/LankaConnect.Infrastructure
```

**Step 3: Verify Parameter Mapping** (If Migration Is Applied)

Check `FormResponseEmailParams.cs`:
```csharp
public class FormResponseEmailParams : IEmailParameters
{
    // ...

    public Dictionary<string, string> ToDictionary()
    {
        return new Dictionary<string, string>
        {
            { EmailTemplateContract.Common.UserName, UserName },  // Must match template
            { EmailTemplateContract.Common.UserEmail, UserEmail },
            { EmailTemplateContract.Event.EventTitle, EventTitle },
            { EmailTemplateContract.Event.FormTitle, FormTitle },
            // ... rest of parameters
        };
    }
}
```

Verify `EmailTemplateContract.cs` has correct constants:
```csharp
public static class Common
{
    public const string UserName = "UserName";  // Template uses {{UserName}}
    public const string UserEmail = "UserEmail";
    // ...
}
```

**Step 4: Test Email Rendering Locally** (If Issue Persists)
1. Add debug logging in `AzureEmailService.cs`:
```csharp
_logger.LogInformation(
    "SendEmail: Template={TemplateName}, Parameters={Parameters}",
    templateName,
    JsonSerializer.Serialize(parameters)
);
```

2. Trigger email send and check logs:
```bash
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group <resource-group> \
  --follow
```

#### Files to Modify

**If Migration Not Applied**:
- None (just run migration)

**If Parameter Mismatch**:
- `src/LankaConnect.Shared/Email/TypedEmailParams/FormResponseEmailParams.cs`
- `src/LankaConnect.Shared/Email/Contracts/EmailTemplateContract.cs`

**For Investigation**:
- `src/LankaConnect.Infrastructure/Email/AzureEmailService.cs` (add debug logging)

#### Effort Estimate
- **Investigation**: 1 hour
- **Migration Application**: 30 minutes
- **Parameter Fix** (if needed): 2 hours
- **Testing**: 1 hour
- **Total**: 4-5 hours

#### Risk Level
- **Risk**: Low (if just migration), Medium (if parameter fix needed)
- **Impact**: Critical - affects all form response emails

#### Testing Strategy
1. **Staging Database Check**:
   - Run SQL queries to verify migration status
2. **Local Email Test**:
   - Submit form response
   - Trigger update
   - Check email received
   - Verify: All placeholders replaced with real values
3. **Template Preview**:
   - Use email client HTML preview
   - Verify: Professional styling applied (gradient header/footer)
4. **Parameter Coverage Test**:
   - Test with all optional parameters (event image, organizer contact, etc.)
   - Verify: Conditional sections render correctly

---

### Issue #5: Response Text Still Not Readable (No Line Separation)

**Classification**: Backend API Issue (Email Handler Logic)
**Severity**: P0 (Critical - Email Readability)
**Status**: NOT FIXED (Despite Phase 6A.115 Fix)

#### Root Cause

**Problem**: Email shows responses in hard-to-read format without line breaks.

**Evidence from Screenshot**: Email displays:
```
Email: niroshanaks@gmail.com | Your name: Niroshana Sinharage! | Phone Number: 8609780124 | ...
```

**Expected Output** (Per Phase 6A.115 Fix):
```
Email: niroshanaks@gmail.com
Your name: Niroshana Sinharage!
Phone Number: 8609780124
```

**CRITICAL FINDING**: Phase 6A.115 fix was deployed (commit d2bc4bcb) but is NOT working.

**Evidence from Deployed Code** (`FormResponseUpdatedEmailHandler.cs`):
```csharp
// Lines 204-208: Phase 6A.115 fix
return $"<strong>{questionText}:</strong> {answerText}";

// Phase 6A.115 Issue #4: Use HTML line breaks instead of pipes for better email readability
var summary = string.Join("<br/>", summaryParts);
```

**Why Fix Isn't Working - Hypothesis**:

**Hypothesis 1: Email Template Escapes HTML** (MOST LIKELY)
- Template uses `{{ResponseSummary}}` which auto-escapes HTML
- Should use `{{{ResponseSummary}}}` (triple braces) to render raw HTML

**Hypothesis 2: Email Client Strips HTML**
- Some email clients strip `<br/>` tags
- Should use `<br>` (no slash) or double line breaks `\n\n`

**Hypothesis 3: Template Not Updated**
- Phase6A112 migration might have old template structure
- Template might wrap response summary in `<pre>` tag which ignores HTML

**Investigation Required**:

**1. Check Migration Template File**:
```bash
# Check HTML template content
cat Template_Correction/staging/template-form-response-update-modified.html | grep -A5 -B5 "ResponseSummary"
```

**2. Check Template in Database**:
```sql
SELECT
    name,
    SUBSTRING(html_template FROM POSITION('ResponseSummary' IN html_template) - 100 FOR 300) as context
FROM communications.email_templates
WHERE name = 'template-form-response-update'
    AND html_template LIKE '%ResponseSummary%';
```

**Expected Template Code**:
```html
<!-- WRONG: Auto-escapes HTML -->
<div class="response-summary">
  {{ResponseSummary}}
</div>

<!-- CORRECT: Renders raw HTML -->
<div class="response-summary">
  {{{ResponseSummary}}}
</div>
```

#### Fix Strategy

**Step 1: Verify Template Rendering** (Investigation)
1. Check template file content
2. Look for `{{ResponseSummary}}` vs `{{{ResponseSummary}}}`
3. Check if there's any HTML escaping happening

**Step 2: Update Migration Template** (If Issue Found)

**Option A: Fix Template File and Re-migrate**
```bash
# Edit template file
vi Template_Correction/staging/template-form-response-update-modified.html

# Change line containing ResponseSummary:
# FROM: {{ResponseSummary}}
# TO:   {{{ResponseSummary}}}

# Create new migration to update template
dotnet ef migrations add Phase6A116_FixFormResponseEmailHTMLRendering \
  --project src/LankaConnect.Infrastructure
```

**Option B: Hotfix with Direct SQL Update**
```sql
-- Staging database hotfix
UPDATE communications.email_templates
SET html_template = REPLACE(
    html_template,
    '{{ResponseSummary}}',
    '{{{ResponseSummary}}}'
)
WHERE name IN (
    'template-form-response-confirmation',
    'template-form-response-update',
    'template-form-response-cancellation'
)
AND html_template LIKE '%{{ResponseSummary}}%';
```

**Step 3: Alternative - Change Backend to Use Plain Text**

If HTML rendering is problematic, change backend to use plain text with actual line breaks:

```csharp
// FormResponseUpdatedEmailHandler.cs - BuildResponseSummary method
private string BuildResponseSummary(
    IReadOnlyList<FormAnswer> answers,
    IReadOnlyList<FormQuestion> questions,
    int maxQuestions = 5,
    int maxAnswerLength = 100)
{
    if (!answers.Any())
        return "No responses provided.";

    var questionMap = questions.ToDictionary(q => q.Id, q => q.QuestionText);
    var displayedAnswers = answers.Take(maxQuestions);

    var summaryParts = displayedAnswers.Select(answer =>
    {
        var questionText = questionMap.TryGetValue(answer.FormQuestionId, out var qText)
            ? qText
            : "Question";

        var answerText = answer.TextValue ??
                        string.Join(", ", answer.SelectedOptionTextSnapshots ?? new List<string>()) ??
                        answer.BooleanValue?.ToString() ?? "";

        if (answerText.Length > maxAnswerLength)
            answerText = $"{answerText.Substring(0, maxAnswerLength)}...";

        // Option 1: Use actual line breaks (will work in <pre> tag)
        return $"{questionText}: {answerText}";
    });

    // Use double line breaks for email rendering
    var summary = string.Join("\n\n", summaryParts);

    var remainingCount = answers.Count - maxQuestions;
    if (remainingCount > 0)
        summary += $"\n\n... and {remainingCount} more response{(remainingCount > 1 ? "s" : "")}";

    return summary;
}
```

And wrap in template with `<pre>` tag:
```html
<div class="response-summary" style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; white-space: pre-wrap;">
  {{{ResponseSummary}}}
</div>
```

#### Files to Modify

**Option A (Template Fix)**:
- `Template_Correction/staging/template-form-response-update-modified.html`
- `Template_Correction/staging/template-form-response-confirmation-modified.html`
- `Template_Correction/staging/template-form-response-cancellation-modified.html`
- New migration: `Phase6A116_FixFormResponseEmailHTMLRendering.cs`

**Option B (SQL Hotfix)**:
- None (direct database update)

**Option C (Backend Change)**:
- `src/LankaConnect.Application/Events/EventHandlers/FormResponseUpdatedEmailHandler.cs`
- Corresponding templates (update wrapper styling)

#### Effort Estimate
- **Investigation**: 1 hour
- **Template Fix**: 2 hours (re-migrate + test)
- **SQL Hotfix**: 30 minutes (faster but less traceable)
- **Backend Change**: 3 hours (safer long-term solution)
- **Total**: 3-6 hours (depends on approach)

#### Risk Level
- **Template Fix**: Low risk (updates template rendering only)
- **SQL Hotfix**: Medium risk (not tracked in migrations)
- **Backend Change**: Low risk (pure logic change)

#### Testing Strategy
1. **Local Template Test**:
   - Render template with test data
   - Use Handlebars CLI or online tester
   - Verify HTML renders correctly
2. **Email Send Test**:
   - Submit form response
   - Update response
   - Check email received
   - Verify: Each answer on separate line
3. **Email Client Test**:
   - Test in Gmail, Outlook, Apple Mail
   - Verify: Line breaks render correctly across clients
4. **Mobile Email Test**:
   - Open email on mobile device
   - Verify: Readable and properly formatted

---

### Issue #6: Updated Response Doesn't Show Changes When Returning

**Classification**: UI Issue (Frontend Cache)
**Severity**: P1 (High - User Confusion)
**Status**: NOT FIXED

#### Root Cause

**Problem**: After successful response update, navigating away and back to form shows OLD data instead of updated values.

**Evidence from User Report**:
1. User edits response
2. Receives success message + email
3. Navigates back to event details
4. Returns to form
5. Form shows old data (before update)

**Root Cause**: React Query cache invalidation not working correctly.

**Evidence from Code** (`web/src/presentation/hooks/useEventForms.ts`):
```typescript
// Lines 712-742: useUpdateFormResponse hook
export const useUpdateFormResponse = (options?: {
  onSuccess?: (data: FormResponseDto, variables: {
    eventId: string;
    formId: string;
    data: UpdateFormResponseDto;
    accessToken?: string;
  }) => void;
  onError?: (error: Error) => void;
}) => {
  const queryClient = useQueryClient();
  const { getAuthHeaders } = useAuth();

  return useMutation({
    mutationFn: async ({
      eventId,
      formId,
      data,
      accessToken,
    }: {
      eventId: string;
      formId: string;
      data: UpdateFormResponseDto;
      accessToken?: string;
    }) => {
      const headers = await getAuthHeaders();

      if (accessToken) {
        headers['X-Access-Token'] = accessToken;
      }

      const response = await fetch(
        `${API_BASE_URL}/events/${eventId}/forms/${formId}/my-response`,
        {
          method: 'PUT',
          headers,
          body: JSON.stringify(data),
        }
      );

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new Error(errorData.message || 'Failed to update response');
      }

      return response.json();
    },
    onSuccess: (data, variables) => {
      // Invalidate relevant queries to refetch fresh data
      queryClient.invalidateQueries({
        queryKey: formKeys.myResponse(variables.eventId, variables.formId, variables.accessToken),
      });
      queryClient.invalidateQueries({
        queryKey: formKeys.detail(variables.eventId, variables.formId),
      });
      queryClient.invalidateQueries({
        queryKey: formKeys.responsesList(variables.eventId, variables.formId),
      });
      queryClient.invalidateQueries({
        queryKey: formKeys.list(variables.eventId),
      });

      options?.onSuccess?.(data, variables);
    },
    onError: options?.onError,
  });
};
```

**Potential Issues**:

**Issue 1: Query Key Mismatch**
- Invalidation uses `formKeys.myResponse(eventId, formId, accessToken)`
- But when user returns, query might not use same `accessToken`
- If `accessToken` is in query key, invalidation won't match

**Issue 2: Soft Navigation Cache**
- Next.js client-side navigation might cache component state
- Need to force refetch on page mount

**Issue 3: localStorage Token Change**
- If update returns new token but localStorage isn't updated
- Subsequent fetch uses old token → gets old data

**Investigation Required**:
```typescript
// Check formKeys.ts - what does myResponse key include?
grep -A10 "myResponse" web/src/presentation/hooks/formKeys.ts

// Check how token is used in query key
```

#### Fix Strategy

**Step 1: Verify Query Key Structure**

Check `formKeys.ts`:
```typescript
export const formKeys = {
  all: ['forms'] as const,
  lists: () => [...formKeys.all, 'list'] as const,
  list: (eventId: string) => [...formKeys.lists(), eventId] as const,
  details: () => [...formKeys.all, 'detail'] as const,
  detail: (eventId: string, formId: string) =>
    [...formKeys.details(), eventId, formId] as const,
  myResponse: (eventId: string, formId: string, accessToken?: string) =>
    [...formKeys.all, 'myResponse', eventId, formId, accessToken] as const,
    // ^^^ Issue: accessToken in key means different token = different cache entry
};
```

**Step 2: Fix Query Key Structure**

**Option A: Remove Token from Query Key** (RECOMMENDED)
```typescript
myResponse: (eventId: string, formId: string) =>
  [...formKeys.all, 'myResponse', eventId, formId] as const,
  // Token is sent in headers, not needed in cache key
```

Then update invalidation:
```typescript
onSuccess: (data, variables) => {
  // Invalidate by eventId + formId only (matches all token variants)
  queryClient.invalidateQueries({
    queryKey: formKeys.myResponse(variables.eventId, variables.formId),
  });
  // ... rest of invalidations
};
```

**Option B: Invalidate All Variants**
```typescript
onSuccess: (data, variables) => {
  // Invalidate ALL myResponse queries for this form (regardless of token)
  queryClient.invalidateQueries({
    predicate: (query) => {
      const key = query.queryKey;
      return (
        key[0] === 'forms' &&
        key[1] === 'myResponse' &&
        key[2] === variables.eventId &&
        key[3] === variables.formId
      );
    },
  });
  // ... rest of invalidations
};
```

**Step 3: Force Refetch on Page Navigation**

Add refetch on window focus and mount:
```typescript
const { data: existingResponse, isLoading: isLoadingResponse } = useMyFormResponse(
  eventId,
  formId,
  accessTokenParam || undefined,
  {
    refetchOnMount: 'always',  // Always refetch when component mounts
    refetchOnWindowFocus: true, // Refetch when user returns to tab
    staleTime: 0, // Consider data stale immediately
  }
);
```

**Step 4: Update localStorage Token After Update**

In `page.tsx`:
```typescript
// Update mutation
const updateMutation = useUpdateFormResponse({
  onSuccess: (data) => {
    // Update token in localStorage if returned
    if (data.accessToken) {
      const storageKey = `form-response-${eventId}-${formId}`;
      localStorage.setItem(storageKey, data.accessToken);
    }

    setSuccessMessage('Response updated successfully!');
    setErrorMessage(null);
    setTimeout(() => window.scrollTo({ top: document.body.scrollHeight, behavior: 'smooth' }), 100);
  },
  // ...
});
```

#### Files to Modify
- `web/src/presentation/hooks/formKeys.ts` (query key structure)
- `web/src/presentation/hooks/useEventForms.ts` (invalidation logic)
- `web/src/app/events/[id]/forms/[formId]/page.tsx` (refetch options, localStorage update)

#### Effort Estimate
- **Investigation**: 1 hour
- **Query Key Fix**: 2 hours
- **Testing**: 2 hours
- **Total**: 5 hours

#### Risk Level
- **Risk**: Medium
- **Impact**: Changes cache invalidation logic, could affect other queries

#### Testing Strategy
1. **Update and Navigate Test**:
   - Submit initial response
   - Edit response (change values)
   - Save update
   - Navigate to event details page
   - Navigate back to form
   - Verify: Shows updated values (not old values)
2. **Multiple Update Test**:
   - Update response 3 times in a row
   - Each time navigate away and back
   - Verify: Always shows latest values
3. **Token Persistence Test**:
   - Update as anonymous user (with token)
   - Close browser
   - Reopen with saved token link
   - Verify: Shows latest values
4. **Cross-Browser Test**:
   - Update in Browser A
   - Open token link in Browser B
   - Verify: Browser B shows latest values

---

### Issue #7: Event Details Signup Form Tab Should Show Response Data

**Classification**: Feature Missing (UI Enhancement)
**Severity**: P2 (Medium - UX Improvement)
**Status**: NOT IMPLEMENTED

#### Root Cause

**Problem**: Event details page "Signup Forms" tab only shows "Edit Your Response" button without displaying actual response data.

**Evidence from User Report**:
- User on event details page, Signup Forms tab
- Shows "Edit Your Response" button
- Does NOT show response summary
- User must click button to see what they submitted

**Comparison**: Registration tab shows full registration details (ticket name, price, status, etc.) plus action buttons.

**Current Behavior**: Tab shows minimal info, requires extra click to view response.

**Expected Behavior**: Tab should show response summary (similar to registration tab) with inline view of submitted data.

**Evidence from Code**:

Need to check event details page structure:
```bash
# Find Signup Forms tab implementation
grep -A20 "Signup Form" web/src/app/events/[id]/page.tsx
```

**Missing Components**:
1. `FormResponseSummary.tsx` - Component to display response data inline
2. Query to fetch user's responses for all forms on event
3. Mapping of form IDs to response data

#### Fix Strategy

**Step 1: Create Response Summary Component**

Create `web/src/presentation/components/features/events/FormResponseSummary.tsx`:
```typescript
interface FormResponseSummaryProps {
  response: {
    formTitle: string;
    submittedAt: string;
    answers: Array<{
      questionText: string;
      answerText: string;
    }>;
  };
  onEdit: () => void;
  onDelete: () => void;
}

export const FormResponseSummary: React.FC<FormResponseSummaryProps> = ({
  response,
  onEdit,
  onDelete,
}) => {
  return (
    <Card className="mb-4">
      <CardHeader>
        <div className="flex items-start justify-between">
          <div>
            <CardTitle className="text-lg">{response.formTitle}</CardTitle>
            <p className="text-sm text-gray-600 mt-1">
              Submitted: {new Date(response.submittedAt).toLocaleString()}
            </p>
          </div>
          <div className="flex gap-2">
            <Button variant="outline" size="sm" onClick={onEdit}>
              <Edit2 className="w-4 h-4 mr-1" />
              Edit
            </Button>
            <Button variant="destructive" size="sm" onClick={onDelete}>
              <Trash2 className="w-4 h-4 mr-1" />
              Delete
            </Button>
          </div>
        </div>
      </CardHeader>
      <CardContent>
        <div className="space-y-2">
          {response.answers.slice(0, 5).map((answer, idx) => (
            <div key={idx} className="text-sm">
              <span className="font-medium text-gray-700">{answer.questionText}:</span>{' '}
              <span className="text-gray-900">{answer.answerText}</span>
            </div>
          ))}
          {response.answers.length > 5 && (
            <p className="text-sm text-gray-500 italic">
              ... and {response.answers.length - 5} more responses
            </p>
          )}
        </div>
      </CardContent>
    </Card>
  );
};
```

**Step 2: Add Hook to Fetch All User Responses for Event**

In `web/src/presentation/hooks/useEventForms.ts`:
```typescript
export const useMyEventFormResponses = (eventId: string) => {
  const { getAuthHeaders } = useAuth();

  return useQuery({
    queryKey: formKeys.myEventResponses(eventId),
    queryFn: async () => {
      const headers = await getAuthHeaders();

      const response = await fetch(
        `${API_BASE_URL}/events/${eventId}/forms/my-responses`,
        { headers }
      );

      if (!response.ok) {
        throw new Error('Failed to fetch responses');
      }

      return response.json() as Promise<FormResponseSummaryDto[]>;
    },
    enabled: !!eventId,
  });
};
```

**Step 3: Update Event Details Page**

In `web/src/app/events/[id]/page.tsx`:
```typescript
// Add query for user's form responses
const { data: myFormResponses } = useMyEventFormResponses(eventId);

// In Signup Forms tab rendering:
<TabsContent value="signup-forms">
  <div className="space-y-4">
    {event.forms.map((form) => {
      const myResponse = myFormResponses?.find(r => r.formId === form.id);

      if (myResponse) {
        return (
          <FormResponseSummary
            key={form.id}
            response={myResponse}
            onEdit={() => router.push(`/events/${eventId}/forms/${form.id}`)}
            onDelete={() => handleDeleteResponse(form.id)}
          />
        );
      }

      return (
        <Card key={form.id}>
          <CardHeader>
            <CardTitle>{form.title}</CardTitle>
            <p className="text-sm text-gray-600">{form.description}</p>
          </CardHeader>
          <CardContent>
            <Button onClick={() => router.push(`/events/${eventId}/forms/${form.id}`)}>
              Fill Out Form
            </Button>
          </CardContent>
        </Card>
      );
    })}
  </div>
</TabsContent>
```

**Step 4: Add Backend Endpoint** (If Not Exists)

Check if endpoint exists:
```bash
grep -r "my-responses" src/LankaConnect.API/
```

If not, create:
```csharp
// GetMyEventFormResponsesQuery.cs
public class GetMyEventFormResponsesQuery : IRequest<List<FormResponseSummaryDto>>
{
    public Guid EventId { get; set; }
}

// Handler
public async Task<List<FormResponseSummaryDto>> Handle(
    GetMyEventFormResponsesQuery request,
    CancellationToken cancellationToken)
{
    var userId = _currentUserService.UserId;
    if (userId == null)
        return new List<FormResponseSummaryDto>();

    var responses = await _formResponseRepository
        .GetByUserIdAndEventIdAsync(userId.Value, request.EventId, cancellationToken);

    return _mapper.Map<List<FormResponseSummaryDto>>(responses);
}

// Controller
[HttpGet("events/{eventId}/forms/my-responses")]
public async Task<ActionResult<List<FormResponseSummaryDto>>> GetMyEventFormResponses(
    [FromRoute] Guid eventId)
{
    var query = new GetMyEventFormResponsesQuery { EventId = eventId };
    var result = await _mediator.Send(query);
    return Ok(result);
}
```

#### Files to Modify

**Frontend**:
- `web/src/presentation/components/features/events/FormResponseSummary.tsx` (NEW)
- `web/src/presentation/hooks/useEventForms.ts` (add hook)
- `web/src/presentation/hooks/formKeys.ts` (add query key)
- `web/src/app/events/[id]/page.tsx` (update tab rendering)

**Backend** (if endpoint doesn't exist):
- `src/LankaConnect.Application/Events/Queries/GetMyEventFormResponsesQuery.cs` (NEW)
- `src/LankaConnect.Application/Events/Queries/GetMyEventFormResponsesQueryHandler.cs` (NEW)
- `src/LankaConnect.Application/DTOs/FormResponseSummaryDto.cs` (NEW)
- `src/LankaConnect.API/Controllers/EventFormsController.cs` (add endpoint)
- `src/LankaConnect.Domain/Events/Repositories/IFormResponseRepository.cs` (add method)
- `src/LankaConnect.Infrastructure/Events/Repositories/FormResponseRepository.cs` (implement method)

#### Effort Estimate
- **Frontend Component**: 3 hours
- **Backend Endpoint**: 4 hours (if not exists)
- **Integration**: 2 hours
- **Testing**: 2 hours
- **Total**: 11 hours (1.5 days)

#### Risk Level
- **Risk**: Low-Medium
- **Impact**: New feature, doesn't affect existing functionality

#### Testing Strategy
1. **No Response Test**:
   - View event with forms but no submissions
   - Verify: Shows "Fill Out Form" button
2. **Single Response Test**:
   - Submit response to one form
   - Navigate to event details
   - Verify: Shows response summary with data
3. **Multiple Response Test**:
   - Submit responses to multiple forms
   - Verify: All responses shown in tab
4. **Edit from Tab Test**:
   - Click "Edit" button on response summary
   - Verify: Navigates to form with existing data
5. **Delete from Tab Test**:
   - Click "Delete" button
   - Verify: Shows confirmation dialog
   - Confirm deletion
   - Verify: Response removed from tab

---

### Issue #8: "Edit Your Response" Email Button Goes to 404 Error

**Classification**: Backend API Issue (URL Generation)
**Severity**: P0 (Critical - Email Link Broken)
**Status**: NOT FIXED

#### Root Cause

**Problem**: Email's "Edit Your Response" button navigates to URL that returns 404 error.

**Evidence from Screenshot**: Clicking button shows "404 - Page Not Found"

**Expected Behavior**: Button should navigate to valid form page with token: `/events/{eventId}/forms/{formId}?token={accessToken}`

**Evidence from Code** (`FormResponseUpdatedEmailHandler.cs`):
```csharp
// Lines 104-105: Edit URL generation
var editUrl = $"{_emailUrlHelper.BuildEventDetailsUrl(eventEntity.Id).Replace("/details", "")}/events/{response.EventId}/forms/{domainEvent.FormId}";
```

**Analysis of URL Generation**:

**Assumption**: `BuildEventDetailsUrl()` returns something like:
- `https://lankaconnect-staging.com/events/{eventId}/details`

**After Replace**:
- `https://lankaconnect-staging.com/events/{eventId}`

**After Concatenation**:
- `https://lankaconnect-staging.com/events/{eventId}/events/{eventId}/forms/{formId}`
  - ^^^ DUPLICATE `/events/{eventId}` path!

**This creates invalid URL** → 404 error

**Correct URL Should Be**:
- `https://lankaconnect-staging.com/events/{eventId}/forms/{formId}?token={accessToken}`

**Additional Issue**: Token is NOT included in URL!

#### Fix Strategy

**Step 1: Verify URL Generation**

Add debug logging to see what URL is generated:
```csharp
_logger.LogInformation(
    "FormResponseUpdatedEmail: Generated edit URL: {EditUrl}, EventId: {EventId}, FormId: {FormId}",
    editUrl, response.EventId, domainEvent.FormId
);
```

**Step 2: Fix URL Generation Logic**

**Current Code** (Lines 104-105):
```csharp
var editUrl = $"{_emailUrlHelper.BuildEventDetailsUrl(eventEntity.Id).Replace("/details", "")}/events/{response.EventId}/forms/{domainEvent.FormId}";
```

**Fixed Code**:
```csharp
// Option 1: Use base URL helper method
var baseUrl = _emailUrlHelper.GetBaseUrl(); // e.g., https://lankaconnect-staging.com
var editUrl = $"{baseUrl}/events/{response.EventId}/forms/{domainEvent.FormId}";

// Option 2: Build from event details URL correctly
var eventDetailsUrl = _emailUrlHelper.BuildEventDetailsUrl(eventEntity.Id); // https://.../events/{id}/details
var baseEventUrl = eventDetailsUrl.Replace("/details", ""); // https://.../events/{id}
var editUrl = $"{baseEventUrl}/forms/{domainEvent.FormId}";

// Option 3: Use dedicated helper method (RECOMMENDED)
var editUrl = _emailUrlHelper.BuildFormEditUrl(eventEntity.Id, domainEvent.FormId);
```

**Step 3: Add Token to URL**

**CRITICAL**: Anonymous users need token in URL to edit response.

**Updated Code**:
```csharp
// Get access token from response entity
var accessToken = response.AccessToken;

// Build edit URL with token
var editUrl = _emailUrlHelper.BuildFormEditUrl(eventEntity.Id, domainEvent.FormId);
if (!string.IsNullOrWhiteSpace(accessToken))
{
    editUrl = $"{editUrl}?token={accessToken}";
}
```

**Step 4: Update EmailUrlHelper** (If Method Doesn't Exist)

In `src/LankaConnect.Shared/Email/Helpers/EmailUrlHelper.cs`:
```csharp
public interface IEmailUrlHelper
{
    string GetBaseUrl();
    string BuildEventDetailsUrl(Guid eventId);
    string BuildFormEditUrl(Guid eventId, Guid formId);
    // ... other methods
}

public class EmailUrlHelper : IEmailUrlHelper
{
    private readonly string _baseUrl;

    public EmailUrlHelper(IConfiguration configuration)
    {
        _baseUrl = configuration["App:WebUrl"] ?? "https://lankaconnect.com";
    }

    public string GetBaseUrl() => _baseUrl;

    public string BuildEventDetailsUrl(Guid eventId)
    {
        return $"{_baseUrl}/events/{eventId}/details";
    }

    public string BuildFormEditUrl(Guid eventId, Guid formId)
    {
        return $"{_baseUrl}/events/{eventId}/forms/{formId}";
    }
}
```

**Step 5: Update Email Parameters**

Make sure `FormResponseEmailParams` includes correct edit URL:
```csharp
var emailParams = FormResponseEmailParams.CreateUpdate(
    userName: response.RespondentName ?? "User",
    userEmail: response.RespondentEmail,
    eventId: eventEntity.Id,
    eventTitle: eventEntity.Title.Value,
    formTitle: form.Title,
    responseSummary: responseSummary,
    editFormUrl: editUrl, // Now includes token
    // ... rest of parameters
);
```

#### Files to Modify
- `src/LankaConnect.Application/Events/EventHandlers/FormResponseUpdatedEmailHandler.cs` (fix URL generation)
- `src/LankaConnect.Shared/Email/Helpers/EmailUrlHelper.cs` (add BuildFormEditUrl method)
- `src/LankaConnect.Shared/Email/Helpers/IEmailUrlHelper.cs` (add interface method)

#### Effort Estimate
- **Investigation**: 30 minutes
- **URL Helper Update**: 2 hours
- **Handler Update**: 1 hour
- **Testing**: 1 hour
- **Total**: 4.5 hours

#### Risk Level
- **Risk**: Low
- **Impact**: Email URL generation only

#### Testing Strategy
1. **Email URL Test**:
   - Submit form response
   - Update response
   - Check email received
   - Verify: "Edit Your Response" button URL is correct
2. **Click Link Test**:
   - Click "Edit Your Response" button in email
   - Verify: Opens form page (not 404)
   - Verify: Form loads with existing response data
3. **Token in URL Test**:
   - Check URL contains `?token=...` parameter
   - Copy URL to different browser
   - Verify: Works without login
4. **URL Format Test**:
   - Verify URL is: `https://.../events/{eventId}/forms/{formId}?token={token}`
   - No duplicate path segments

---

### Issue #9: "View Signup List" Button Not Clickable (No href)

**Classification**: Backend API Issue (Email Template + Parameters)
**Severity**: P1 (High - Email Link Missing)
**Status**: NOT FIXED

#### Root Cause

**Problem**: Email shows "View Signup List" button but it's not clickable (missing `href` attribute).

**Evidence from Screenshot**: Email HTML shows:
```handlebars
{{#HasSignupLists}}
  <a>View Signup List</a>
{{/HasSignupLists}}
```

**Expected HTML**:
```html
<a href="https://lankaconnect.com/events/{eventId}/signup-lists">View Signup List</a>
```

**Root Cause Analysis**:

**Issue 1: Missing href Attribute in Template**

Template file likely has:
```html
{{#HasSignupLists}}
  <a style="...">View Signup List</a>
{{/HasSignupLists}}
```

Should be:
```html
{{#HasSignupLists}}
  <a href="{{SignupListUrl}}" style="...">View Signup List</a>
{{/HasSignupLists}}
```

**Issue 2: Missing SignupListUrl Parameter**

Handler might not be setting `SignupListUrl` parameter.

**Evidence from Code**:

**1. Check Handler** (`FormResponseUpdatedEmailHandler.cs`):
```csharp
// Lines 108-121: Email parameters creation
var emailParams = FormResponseEmailParams.CreateUpdate(
    userName: response.RespondentName ?? "User",
    userEmail: response.RespondentEmail,
    eventId: eventEntity.Id,
    eventTitle: eventEntity.Title.Value,
    formTitle: form.Title,
    responseSummary: responseSummary,
    editFormUrl: editUrl,
    eventStartDate: eventEntity.StartDate,
    timeZoneId: eventEntity.TimeZoneId,
    eventLocation: eventEntity.Location?.ToString() ?? "TBA",
    eventDetailsUrl: _emailUrlHelper.BuildEventDetailsUrl(eventEntity.Id),
    updatedAt: domainEvent.OccurredAt
);

// Lines 122-148: Optional parameters
// ... (no call to WithSignupListsUrl)
```

**Missing**: No call to `emailParams.WithSignupListsUrl(url)` method!

**2. Check Email Params Class**:
```bash
grep -A10 "WithSignupListsUrl" src/LankaConnect.Shared/Email/TypedEmailParams/FormResponseEmailParams.cs
```

**Investigation Required**:
1. Does `FormResponseEmailParams` have `WithSignupListsUrl()` method?
2. Does email template have `{{SignupListUrl}}` placeholder?
3. Is `HasSignupLists` flag being set correctly?

#### Fix Strategy

**Step 1: Verify Template Structure**

Check migration template file:
```bash
cat Template_Correction/staging/template-form-response-update-modified.html | grep -A5 -B5 "SignupList"
```

**Expected Template Code**:
```html
{{#HasSignupLists}}
  <tr>
    <td style="padding: 20px 0;">
      <a href="{{SignupListUrl}}"
         style="display: inline-block; padding: 12px 24px; background-color: #ea580c; color: white; text-decoration: none; border-radius: 6px; font-weight: 600;">
        View Signup Lists
      </a>
    </td>
  </tr>
{{/HasSignupLists}}
```

**Step 2: Add Missing Parameter to Handler**

In `FormResponseUpdatedEmailHandler.cs`:
```csharp
// After creating email params (around line 121):
var emailParams = FormResponseEmailParams.CreateUpdate(/* ... */);

// Add optional fields
if (!string.IsNullOrWhiteSpace(form.Description))
{
    emailParams.WithFormDescription(form.Description);
}

if (form.ResponseDeadline.HasValue)
{
    emailParams.WithResponseDeadline(form.ResponseDeadline);
}

// Check if event has signup lists
var hasSignupLists = eventEntity.SignupLists?.Any() ?? false;
if (hasSignupLists)
{
    var signupListUrl = _emailUrlHelper.BuildSignupListsUrl(eventEntity.Id);
    emailParams.WithSignupListsUrl(signupListUrl);
}
```

**Step 3: Add EmailUrlHelper Method** (If Missing)

In `src/LankaConnect.Shared/Email/Helpers/EmailUrlHelper.cs`:
```csharp
public string BuildSignupListsUrl(Guid eventId)
{
    return $"{_baseUrl}/events/{eventId}#signup-lists";
}
```

**Step 4: Verify FormResponseEmailParams Method**

Check if `WithSignupListsUrl()` exists:
```csharp
// FormResponseEmailParams.cs
public FormResponseEmailParams WithSignupListsUrl(string signupListUrl)
{
    _hasSignupLists = true;
    _signupListUrl = signupListUrl;
    return this;
}

public override Dictionary<string, string> ToDictionary()
{
    var parameters = new Dictionary<string, string>
    {
        { EmailTemplateContract.Common.UserName, UserName },
        // ... other parameters
        { EmailTemplateContract.Event.HasSignupLists, _hasSignupLists.ToString().ToLower() },
    };

    if (_hasSignupLists)
    {
        parameters[EmailTemplateContract.Event.SignupListUrl] = _signupListUrl ?? "";
    }

    return parameters;
}
```

**Step 5: Update EmailTemplateContract** (If Constants Missing)

In `src/LankaConnect.Shared/Email/Contracts/EmailTemplateContract.cs`:
```csharp
public static class Event
{
    public const string EventTitle = "EventTitle";
    public const string HasSignupLists = "HasSignupLists";
    public const string SignupListUrl = "SignupListUrl";
    // ... other constants
}
```

**Step 6: Update Migration Template** (If href Missing)

Edit template file and update migration:
```html
<!-- Add href attribute -->
{{#HasSignupLists}}
  <tr>
    <td style="padding: 20px 0;">
      <a href="{{SignupListUrl}}"
         style="display: inline-block; padding: 12px 24px; background-color: #ea580c; color: white; text-decoration: none; border-radius: 6px; font-weight: 600;">
        View Signup Lists
      </a>
    </td>
  </tr>
{{/HasSignupLists}}
```

#### Files to Modify
- `src/LankaConnect.Application/Events/EventHandlers/FormResponseUpdatedEmailHandler.cs` (add signup list URL)
- `src/LankaConnect.Shared/Email/Helpers/EmailUrlHelper.cs` (add BuildSignupListsUrl method)
- `src/LankaConnect.Shared/Email/TypedEmailParams/FormResponseEmailParams.cs` (verify method exists)
- `src/LankaConnect.Shared/Email/Contracts/EmailTemplateContract.cs` (verify constants)
- `Template_Correction/staging/template-form-response-update-modified.html` (add href if missing)
- New migration: `Phase6A116_FixFormResponseEmailSignupListButton.cs` (if template needs update)

#### Effort Estimate
- **Investigation**: 1 hour
- **Handler Update**: 2 hours
- **Template Update**: 1 hour (if needed)
- **Testing**: 1 hour
- **Total**: 5 hours

#### Risk Level
- **Risk**: Low
- **Impact**: Email template and parameter logic

#### Testing Strategy
1. **Email Rendering Test**:
   - Submit response to form for event WITH signup lists
   - Update response
   - Check email received
   - Verify: "View Signup Lists" button visible
2. **Link Click Test**:
   - Click "View Signup Lists" button
   - Verify: Navigates to event page, scrolls to signup lists section
3. **Conditional Display Test**:
   - Submit response for event WITHOUT signup lists
   - Check email
   - Verify: "View Signup Lists" button NOT shown
4. **URL Format Test**:
   - Verify URL is: `https://.../events/{eventId}#signup-lists`

---

## SYSTEMIC ISSUES

### Common Root Causes

#### 1. Email Template System Issues (Affects Issues #4, #5, #8, #9)

**Pattern**: 4 out of 9 issues are email-related, all stemming from:
- **Migration Not Applied**: Phase6A112 migration may not have been run on staging
- **Template Parameter Mismatches**: Handler sends parameters but template doesn't use them
- **HTML Rendering Issues**: Template auto-escapes HTML instead of rendering raw
- **Missing URL Parameters**: Edit links, signup list links not being set

**Systemic Solution**:
1. **Email Template Validation Service** (Already exists per MEMORY.md):
   - Verify all parameters from `EmailTemplateContract.cs` match template placeholders
   - Run validation on startup
   - Log warnings for mismatches

2. **Migration Verification Script**:
```bash
#!/bin/bash
# scripts/verify_email_migrations.sh
# Run after deployment to verify email template migrations applied

echo "Checking Phase6A112 migration..."
psql $DATABASE_URL -c "SELECT * FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" LIKE '%Phase6A112%';"

echo "Checking template content..."
psql $DATABASE_URL -c "SELECT name, LEFT(html_template, 100) FROM communications.email_templates WHERE name LIKE 'template-form-response%';"
```

3. **Email Template Testing Suite**:
```csharp
// tests/LankaConnect.IntegrationTests/Email/FormResponseEmailTests.cs
[Fact]
public async Task FormResponseUpdateEmail_ShouldRenderAllParameters()
{
    // Arrange
    var handler = new FormResponseUpdatedEmailHandler(/* deps */);
    var domainEvent = CreateTestDomainEvent();

    // Act
    await handler.Handle(domainEvent, CancellationToken.None);

    // Assert
    var sentEmail = _emailServiceMock.SentEmails.Last();
    Assert.DoesNotContain("{{", sentEmail.HtmlBody); // No unrendered placeholders
    Assert.Contains("<br/>", sentEmail.HtmlBody); // HTML line breaks rendered
    Assert.Contains("https://", sentEmail.HtmlBody); // URLs present
}
```

#### 2. Frontend Cache Invalidation Issues (Affects Issue #6)

**Pattern**: React Query cache not properly invalidated after mutations.

**Root Cause**: Query keys include optional parameters (like `accessToken`) which creates separate cache entries.

**Systemic Solution**:
1. **Standardize Query Key Structure**:
   - Remove optional parameters from query keys
   - Use predicate-based invalidation for complex cases

2. **Cache Invalidation Testing**:
```typescript
// tests/hooks/useEventForms.test.ts
it('should invalidate cache after update', async () => {
  const { result } = renderHook(() => useUpdateFormResponse());

  await act(async () => {
    await result.current.mutateAsync({ eventId, formId, data });
  });

  // Verify cache invalidation
  const cachedData = queryClient.getQueryData(formKeys.myResponse(eventId, formId));
  expect(cachedData).toBeUndefined(); // Cache cleared
});
```

#### 3. UX Consistency Issues (Affects Issues #1, #2, #7)

**Pattern**: UI doesn't differentiate between user states (first-time vs returning, authenticated vs anonymous).

**Root Cause**: Components lack conditional rendering based on user context.

**Systemic Solution**:
1. **User Context Hook**:
```typescript
// useUserContext.ts
export const useUserContext = () => {
  const { data: session } = useSession();
  const isAuthenticated = !!session?.user;
  const userId = session?.user?.id;

  return {
    isAuthenticated,
    userId,
    isAnonymous: !isAuthenticated,
  };
};
```

2. **Conditional Rendering Pattern**:
```typescript
// Standardize across all form components
const { isAuthenticated } = useUserContext();

if (isAuthenticated) {
  // Show member-specific UI
} else {
  // Show anonymous-specific UI with token instructions
}
```

---

### Deployment Gaps

#### Phase 6A.115 Deployment Status

**✅ Code Deployment**: CONFIRMED
- Backend: Deployed successfully (Run ID: 22038432032)
- Frontend: Deployed successfully (Run ID: 22038432055)
- Commits: d2bc4bcb, b671fe85, 34a0ca70, 007fac65

**❌ Database Migration**: UNKNOWN
- Phase6A112 migration applied status: **UNVERIFIED**
- No confirmation that staging database was migrated
- Email template issues suggest migration may not have run

**Investigation Script**:
```bash
#!/bin/bash
# scripts/verify_staging_deployment.sh

echo "=== Checking Staging Deployment Status ==="

echo "1. Backend Container Status:"
az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group <rg> \
  --query "properties.latestRevisionName"

echo "2. Database Migration Status:"
az postgres flexible-server execute \
  --name <db-server> \
  --database-name lankaconnect-staging \
  --querytext "SELECT * FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\" DESC LIMIT 5;"

echo "3. Email Template Check:"
az postgres flexible-server execute \
  --name <db-server> \
  --database-name lankaconnect-staging \
  --querytext "SELECT name, updated_at FROM communications.email_templates WHERE name LIKE 'template-form-response%';"
```

---

### Missing Validation

#### Tests That Would Have Caught These Issues

**1. Email Integration Tests** (Would catch Issues #4, #5, #8, #9):
```csharp
// Missing test: FormResponseUpdatedEmailIntegrationTests.cs
[Fact]
public async Task UpdateFormResponse_ShouldSendEmailWithCorrectParameters()
{
    // Arrange
    var response = await SubmitFormResponse();

    // Act
    await UpdateFormResponse(response.Id);

    // Assert
    var email = await _mailHog.GetLastEmail();
    Assert.DoesNotContain("{{", email.Html); // No placeholders
    Assert.Contains("<br/>", email.Html); // Line breaks
    Assert.Contains("https://", email.Html); // URLs present
    Assert.DoesNotContain("/events/{eventId}/events/", email.Html); // No duplicate paths
}
```

**2. Cache Invalidation Tests** (Would catch Issue #6):
```typescript
// Missing test: useEventForms.cache.test.ts
it('should show updated data after navigation', async () => {
  const { result } = renderHook(() => useUpdateFormResponse());

  // Update response
  await result.current.mutateAsync({ eventId, formId, data });

  // Navigate away and back (simulate)
  queryClient.clear();
  const { result: newResult } = renderHook(() => useMyFormResponse(eventId, formId));

  await waitFor(() => {
    expect(newResult.current.data).toEqual(updatedData);
  });
});
```

**3. E2E Tests** (Would catch all UI issues):
```typescript
// Missing test: formResponse.e2e.test.ts
test('form response flow for anonymous user', async ({ page }) => {
  // Submit form
  await page.goto(`/events/${eventId}/forms/${formId}`);
  await page.fill('[name="email"]', 'test@example.com');
  await page.click('button[type="submit"]');

  // Verify success message shows token link
  await expect(page.locator('text=Save this link')).toBeVisible();

  // Copy token link
  const tokenLink = await page.locator('[data-testid="edit-link"]').textContent();

  // Open in new context (simulate different browser)
  const newContext = await browser.newContext();
  const newPage = await newContext.newPage();
  await newPage.goto(tokenLink);

  // Verify form loads with data
  await expect(newPage.locator('[name="email"]')).toHaveValue('test@example.com');
});
```

---

## RECOMMENDED FIX SEQUENCE

### Phase 1: Critical Fixes (Deploy Today) - P0 Issues

**Priority Order**:
1. **Issue #4: Email Placeholder Parameters** (2 hours)
   - Verify Phase6A112 migration applied
   - If not: Apply migration
   - If yes: Debug parameter mapping

2. **Issue #5: Email Line Breaks** (3 hours)
   - Fix template HTML rendering (triple braces)
   - OR change backend to use plain text with line breaks

3. **Issue #8: Email Edit Button 404** (4 hours)
   - Fix URL generation logic in handler
   - Add token to URL

4. **Issue #3: Token-Based Edit 400 Error** (8 hours)
   - Investigate backend token validation
   - Fix handler to accept X-Access-Token header
   - Add repository method if missing

**Total Time**: 17 hours (~2 days)
**Risk**: Medium (email system + auth logic changes)
**Blocker**: Must verify database migration status FIRST

---

### Phase 2: High Priority (Deploy Tomorrow) - P1 Issues

**Priority Order**:
1. **Issue #9: Signup List Button Missing href** (5 hours)
   - Add SignupListUrl parameter to handler
   - Update template with href attribute

2. **Issue #6: Cache Not Showing Updated Data** (5 hours)
   - Fix query key structure
   - Update invalidation logic
   - Add refetch on mount

3. **Issue #2: Success Message Member vs Anonymous** (2 hours)
   - Add conditional messaging based on auth state
   - Show copy-to-clipboard for anonymous users

4. **Issue #1: First-Time Visitor Button Text** (1 hour)
   - Add conditional button text
   - Update info messages

**Total Time**: 13 hours (~1.5 days)
**Risk**: Low (mostly UI changes)

---

### Phase 3: Enhancements (Deploy Next Week) - P2 Issues

**Priority Order**:
1. **Issue #7: Event Details Show Response Data** (11 hours)
   - Create FormResponseSummary component
   - Add backend endpoint (if needed)
   - Update event details page tab

**Total Time**: 11 hours (~1.5 days)
**Risk**: Low (new feature)

---

## RISK ASSESSMENT

### Overall Deployment Risk: **MEDIUM**

**Critical Concerns**:
1. **Email System Fragility**: 4 out of 9 issues are email-related
   - Risk: Email changes could break other email types
   - Mitigation: Test ALL email types after deployment (not just form response emails)

2. **Token-Based Auth**: Issue #3 requires changes to authentication logic
   - Risk: Could break authenticated user flows
   - Mitigation: Test both authenticated and anonymous flows

3. **Database Migration Status**: Unknown if Phase6A112 applied
   - Risk: Applying migration in production could fail if conflicts exist
   - Mitigation: Test migration on staging copy first

### Rollback Plan

**If Critical Issues Occur After Deployment**:

1. **Email System Failure**:
```bash
# Rollback to previous migration
dotnet ef database update 20260214000000_PreviousMigration

# OR disable email sending temporarily
az containerapp update \
  --name lankaconnect-api-staging \
  --set-env-vars "EmailService__Enabled=false"
```

2. **Auth/Token Issues**:
```bash
# Rollback backend code
git revert <commit-sha>
git push origin develop

# Redeploy previous version
gh workflow run deploy-staging.yml
```

3. **Frontend Cache Issues**:
```typescript
// Emergency fix: Clear all caches on page load
useEffect(() => {
  queryClient.clear();
}, []);
```

### Testing Checklist Before Production

**Must Pass ALL Before Promoting to Production**:
- [ ] Phase6A112 migration applied successfully
- [ ] Email sent with all parameters replaced (no `{{}}` placeholders)
- [ ] Email line breaks render correctly (one answer per line)
- [ ] Email "Edit Your Response" button links to valid URL
- [ ] Email "View Signup Lists" button links to valid URL
- [ ] Token-based edit link works in different browser
- [ ] Authenticated user can edit without token
- [ ] Cache shows updated data after navigation
- [ ] Success messages differentiate member vs anonymous
- [ ] First-time visitor sees "Submit" not "Edit"
- [ ] All existing email types still work (regression test)

---

## APPENDICES

### A. Database Migration Verification Queries

```sql
-- Check migration history
SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
WHERE "MigrationId" LIKE '%Phase6A%'
ORDER BY "MigrationId" DESC
LIMIT 10;

-- Check email template content
SELECT
    name,
    subject_template,
    CASE
        WHEN html_template LIKE '%{{UserName}}%' THEN 'Has placeholders'
        WHEN html_template LIKE '%{{{ResponseSummary}}}%' THEN 'Renders HTML (triple braces)'
        WHEN html_template LIKE '%{{ResponseSummary}}%' THEN 'Escapes HTML (double braces)'
        ELSE 'Unknown'
    END as rendering_mode,
    updated_at
FROM communications.email_templates
WHERE name IN (
    'template-form-response-confirmation',
    'template-form-response-update',
    'template-form-response-cancellation'
);

-- Check for unrendered placeholders
SELECT
    name,
    (LENGTH(html_template) - LENGTH(REPLACE(html_template, '{{', ''))) / 2 as placeholder_count
FROM communications.email_templates
WHERE name LIKE 'template-form-response%';
```

### B. Email Template Parameter Checklist

**Required for All Form Response Emails**:
- [ ] `{{UserName}}` - Respondent name
- [ ] `{{UserEmail}}` - Respondent email
- [ ] `{{EventTitle}}` - Event name
- [ ] `{{FormTitle}}` - Form name
- [ ] `{{{ResponseSummary}}}` - Answer summary (with HTML line breaks)
- [ ] `{{EventStartDate}}` - Event start time
- [ ] `{{EventLocation}}` - Event location
- [ ] `{{EventDetailsUrl}}` - Link to event details

**Optional (Conditional)**:
- [ ] `{{FormDescription}}` - Form description (if exists)
- [ ] `{{ResponseDeadline}}` - Deadline (if exists)
- [ ] `{{EventImage}}` - Event image (if exists)
- [ ] `{{OrganizerContactName}}` - Organizer contact (if exists)
- [ ] `{{EditFormUrl}}` - Edit link WITH token
- [ ] `{{SignupListUrl}}` - Signup lists link (if event has lists)

### C. React Query Cache Key Standards

**Standard Pattern** (Use This):
```typescript
// Query keys should NOT include tokens/auth
formKeys.myResponse(eventId, formId)  // Good

// Tokens sent in headers, not cache keys
formKeys.myResponse(eventId, formId, accessToken)  // Avoid
```

**Invalidation Pattern**:
```typescript
// Invalidate by entity IDs only
queryClient.invalidateQueries({
  queryKey: formKeys.myResponse(eventId, formId),
});

// For complex cases, use predicate
queryClient.invalidateQueries({
  predicate: (query) => {
    const [type, action, ...ids] = query.queryKey;
    return type === 'forms' && ids.includes(eventId);
  },
});
```

---

**End of Root Cause Analysis**

**Next Steps**:
1. User reviews priorities and approves fix sequence
2. Create Phase 6A.116 for critical email fixes
3. Create Phase 6A.117 for UX improvements
4. Create Phase 6A.118 for enhancements
5. Test each phase thoroughly in staging
6. Promote to production only after ALL tests pass
