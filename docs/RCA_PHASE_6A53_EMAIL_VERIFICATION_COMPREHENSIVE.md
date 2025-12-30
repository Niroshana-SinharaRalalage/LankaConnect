# Root Cause Analysis: Phase 6A.53 Member Email Verification System Issues

**Date:** 2025-12-30
**Environment:** Azure Container Apps (Staging)
**Status:** Multiple Critical Issues Identified
**Analysis Depth:** Comprehensive architectural review with evidence-based diagnosis

---

## Executive Summary

Phase 6A.53 Member Email Verification System has four distinct issues affecting the complete user verification flow. Analysis reveals a combination of database template caching, configuration errors, UI layout inconsistencies, and API contract mismatches. All issues stem from different root causes and require coordinated fixes.

### Critical Finding
**Migration 20251229231742 was applied to database** (confirmed in `__EFMigrationsHistory`), BUT the template content may not have updated due to:
1. Email service template caching
2. Database query not refreshing cached templates
3. Potential Azure Container Apps deployment cache

---

## Issue 1: Email Template Shows Old Layout

### Categorization
- **Category:** Database Issue / Caching Issue
- **Severity:** High
- **Dependencies:** None (independent issue)
- **Scope:** Email rendering system

### Root Cause Analysis

**PRIMARY CAUSE:** Email template caching in Azure staging environment is serving stale template despite migration being applied.

**EVIDENCE CHAIN:**

1. **Migration Applied Successfully**
   - File: `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Migrations\20251229231742_Phase6A53Fix3_RevertToCorrectTemplateNoLogo.cs`
   - Migration exists in `__EFMigrationsHistory` table (user confirmed)
   - Migration contains clean HTML template without decorative elements (lines 23-84)
   - Template UPDATE SQL targets `communications.email_templates WHERE name = 'member-email-verification'`

2. **Template Service Architecture**
   - File: `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Email\Services\EmailService.cs`
   - Line 96: `var template = await _emailTemplateRepository.GetByNameAsync(templateName, cancellationToken);`
   - Line 110: `var renderResult = await _templateService.RenderTemplateAsync(templateName, parameters, cancellationToken);`
   - **NO explicit cache invalidation after migration**

3. **Email Configuration**
   - File: `c:\Work\LankaConnect\src\LankaConnect.API\appsettings.json`
   - Lines 109-110: `"CacheTemplates": true, "TemplateCacheExpiryInMinutes": 60`
   - **Templates are cached for 60 minutes**

4. **Actual Template Content (Migration)**
   - Lines 36-38: Clean gradient header `<h1 style="margin: 0; font-size: 28px; font-weight: bold; color: white;">Welcome to LankaConnect!</h1>`
   - Lines 71-77: Clean footer with NO decorative stars, NO logo image
   - Template matches event registration email layout (simple gradient, text only)

5. **User Feedback**
   - "still email layout is wrong" - confirms visual mismatch
   - "copy the registration email layout" - user wants `registration-confirmation` template style

**COMPARISON WITH EVENT REGISTRATION TEMPLATE:**

Event Registration Template (from `temp_migration.sql`):
```html
<!-- Header -->
<div class="header">
    <h1>Registration Confirmed!</h1>
</div>

<!-- Footer -->
<div class="footer">
    <p>&copy; 2025 LankaConnect. All rights reserved.</p>
</div>
```

Member Verification Template (from migration 20251229231742):
```html
<!-- Header -->
<h1 style="margin: 0; font-size: 28px; font-weight: bold; color: white;">Welcome to LankaConnect!</h1>

<!-- Footer -->
<p style="color: rgba(255,255,255,0.8); font-size: 12px; margin: 0;">&copy; 2025 LankaConnect. All rights reserved.</p>
```

**TEMPLATES ARE ALREADY ALIGNED.** The issue is NOT the migration content, but **cache serving old version**.

### Investigation Steps Needed

1. **Verify Database State:**
   ```sql
   -- Connect to Azure staging database
   SELECT
       name,
       description,
       LENGTH(html_template) as template_length,
       CASE
           WHEN html_template LIKE '%decorative%' THEN 'OLD (has decorative elements)'
           WHEN html_template LIKE '%LankaConnect!</h1>%' THEN 'NEW (clean header)'
           ELSE 'UNKNOWN'
       END as template_version,
       updated_at
   FROM communications.email_templates
   WHERE name = 'member-email-verification';
   ```

2. **Check Template Cache:**
   - Restart Azure Container App to invalidate in-memory cache
   - OR wait 60 minutes for cache expiry
   - OR manually invalidate cache if cache key is accessible

3. **Verify Email Service Logs:**
   ```bash
   # Check Azure Container Apps logs for template loading
   az containerapp logs show \
     --name lankaconnect-api-staging \
     --resource-group <resource-group> \
     --follow

   # Look for: "Email template 'member-email-verification' not found" or cache-related messages
   ```

4. **Test Email Send After Cache Clear:**
   - Trigger new registration
   - Capture email HTML source
   - Compare against migration template (lines 23-84)

### Fix Strategy

**APPROACH:** Multi-layer cache invalidation with verification

**FILES TO CHANGE:**
1. **Azure Container App Restart** (via Azure Portal or CLI)
   ```bash
   az containerapp restart \
     --name lankaconnect-api-staging \
     --resource-group <resource-group>
   ```

2. **Verify appsettings.Staging.json** (if exists)
   - Check if staging overrides `CacheTemplates` or `TemplateCacheExpiryInMinutes`
   - Consider setting `CacheTemplates: false` for staging environment

3. **Optional: Add Cache Invalidation to Migration**
   - Not in current migration, but future migrations could include:
   ```csharp
   // After template update, clear cache
   migrationBuilder.Sql("NOTIFY email_template_updated, 'member-email-verification';");
   ```

**TESTING STRATEGY:**
1. ✅ Restart Azure Container App
2. ✅ Wait 5 minutes for full restart
3. ✅ Trigger new user registration
4. ✅ Check email HTML source for clean header (no decorative elements)
5. ✅ Compare footer - should have NO logo image, just text
6. ✅ Verify gradient matches event registration emails

**RISKS:**
- Container restart causes brief downtime (1-2 minutes)
- If database UPDATE didn't execute, template will still be wrong after restart
- Possible that staging uses different database than expected

---

## Issue 2: Verification Link Returns 404

### Categorization
- **Category:** Configuration Issue
- **Severity:** Critical (blocks verification flow)
- **Dependencies:** Must be fixed before Issue 3 can be tested
- **Scope:** Application URL configuration

### Root Cause Analysis

**PRIMARY CAUSE:** `ApplicationUrls:FrontendBaseUrl` in staging configuration points to backend API URL instead of frontend URL.

**EVIDENCE CHAIN:**

1. **Email Link Generated**
   - User provided: `https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecomm.io/verify-email?token=...`
   - Domain: `lankaconnect-api-staging` (API server, not frontend)
   - Result: HTTP 404 (backend has no `/verify-email` route)

2. **URL Generation Logic**
   - File: `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Email\Configuration\ApplicationUrlsOptions.cs`
   - Line 41: `return $"{FrontendBaseUrl.TrimEnd('/')}{EmailVerificationPath}?token={token}";`
   - Uses `FrontendBaseUrl` from config + `/verify-email` path

3. **Configuration File**
   - File: `c:\Work\LankaConnect\src\LankaConnect.API\appsettings.json`
   - Line 129: `"FrontendBaseUrl": "https://lankaconnect.com"`
   - **This is PRODUCTION URL, not staging**

4. **Event Handler Logs**
   - File: `c:\Work\LankaConnect\src\LankaConnect.Application\Users\EventHandlers\MemberVerificationRequestedEventHandler.cs`
   - Line 44: `var verificationUrl = _urlsService.GetEmailVerificationUrl(domainEvent.VerificationToken);`
   - Line 58-59: Logs `VerificationUrl: {VerificationUrl}` - should show actual URL used

5. **Expected Flow**
   - Email link should point to **Next.js frontend** running on staging
   - Frontend staging URL unknown (not provided in context)
   - Link should be: `https://<frontend-staging-url>/verify-email?token=...`

**ARCHITECTURAL ISSUE:**
The email contains the API URL because either:
1. `appsettings.Staging.json` is missing or has wrong `FrontendBaseUrl`
2. `ApplicationUrls:FrontendBaseUrl` was manually set to API URL
3. Deployment process doesn't update `FrontendBaseUrl` for staging environment

### Investigation Steps Needed

1. **Find Staging Configuration:**
   ```bash
   # Check if appsettings.Staging.json exists
   find . -name "appsettings.Staging.json"

   # If exists, read FrontendBaseUrl
   cat src/LankaConnect.API/appsettings.Staging.json | grep -A 5 "ApplicationUrls"
   ```

2. **Check Azure Container App Environment Variables:**
   ```bash
   az containerapp show \
     --name lankaconnect-api-staging \
     --resource-group <resource-group> \
     --query "properties.template.containers[0].env"
   ```

3. **Identify Frontend Staging URL:**
   - Where is Next.js frontend deployed for staging?
   - Is it running on localhost:3000 (dev only)?
   - Or is there an Azure Static Web App / Azure Web App for frontend?

4. **Review Deployment Workflow:**
   - File: `.github/workflows/deploy-staging.yml` (if exists)
   - Check if it sets `ApplicationUrls__FrontendBaseUrl` environment variable

### Fix Strategy

**APPROACH:** Create/update staging configuration with correct frontend URL

**FILES TO CHANGE:**

**Option 1: Create appsettings.Staging.json** (if missing)
```json
{
  "ApplicationUrls": {
    "FrontendBaseUrl": "http://localhost:3000",  // OR staging frontend URL
    "EmailVerificationPath": "/verify-email",
    "UnsubscribePath": "/unsubscribe",
    "EventDetailsPath": "/events/{eventId}"
  }
}
```

**Option 2: Set Environment Variable in Azure Container App**
```bash
az containerapp update \
  --name lankaconnect-api-staging \
  --resource-group <resource-group> \
  --set-env-vars "ApplicationUrls__FrontendBaseUrl=http://localhost:3000"
```

**Option 3: Update GitHub Actions Workflow**
```yaml
# In deploy-staging.yml
- name: Set Container App Environment Variables
  run: |
    az containerapp update \
      --name lankaconnect-api-staging \
      --resource-group ${{ secrets.AZURE_RESOURCE_GROUP }} \
      --set-env-vars \
        "ApplicationUrls__FrontendBaseUrl=${{ secrets.STAGING_FRONTEND_URL }}"
```

**TESTING STRATEGY:**
1. ✅ Update configuration with correct frontend URL
2. ✅ Deploy backend to staging (or restart if using env vars)
3. ✅ Trigger new user registration
4. ✅ Check email for verification link
5. ✅ Verify link domain matches frontend staging URL
6. ✅ Click link - should load frontend verification page (not 404)

**RISKS:**
- Frontend staging URL unknown - need to confirm deployment strategy
- If frontend runs on localhost:3000 (dev only), need to deploy frontend to staging
- Possible CORS issues if frontend and backend on different domains

**DECISION NEEDED:**
- **Where should the verification link point to?**
  - Local dev frontend (http://localhost:3000) - only works for local testing
  - Azure Static Web App for frontend staging - requires frontend deployment
  - Same domain as API (subdomain setup) - requires DNS/routing configuration

---

## Issue 3: Verification Page Layout Broken

### Categorization
- **Category:** UI Issue
- **Severity:** High (poor user experience)
- **Dependencies:** Issue 2 must be fixed first (link must work)
- **Scope:** Frontend verification page layout

### Root Cause Analysis

**PRIMARY CAUSE:** Verification page uses completely different layout structure than login page, causing visual inconsistency.

**EVIDENCE CHAIN:**

1. **User Feedback**
   - "verification UI has the wrong layout"
   - "formatting is totally wrong compare to login page"
   - URL shown: `http://localhost:3000/verify-email?token=03a257edc6484a1db209d0363abbf5eb`

2. **Login Page Layout**
   - File: `c:\Work\LankaConnect\web\src\app\(auth)\login\page.tsx`
   - Structure:
     - Lines 10-12: Gradient background `linear-gradient(to-r, #FF7900, #8B1538, #006400)`
     - Lines 16: Split panel container `grid grid-cols-1 md:grid-cols-2`
     - Lines 18-81: **Left Panel** - Branding with OfficialLogo, welcome text, features
     - Lines 84-102: **Right Panel** - LoginForm
     - Lines 19-27: Decorative background pattern (SVG grid)
     - Lines 29-34: Decorative gradient blobs
     - Lines 38: `<OfficialLogo size="md" textColor="text-white" subtitleColor="text-white/90" linkTo="/" />`

3. **Verification Page Layout**
   - File: `c:\Work\LankaConnect\web\src\app\(auth)\verify-email\page.tsx`
   - Structure:
     - Lines 58-63: Background image `url(/images/batik-sri-lanka.jpg)` (NOT gradient)
     - Lines 67: Split panel container `grid grid-cols-1 md:grid-cols-2` ✅ SAME
     - Lines 69-134: **Left Panel** - Branding with Image (not OfficialLogo), features
     - Lines 137-162: **Right Panel** - EmailVerificationContent
     - Lines 70-77: Animated pulsing radial gradient (different from login)
     - Lines 82-91: Logo using `<Image>` tag directly (NOT OfficialLogo component)

**KEY DIFFERENCES:**

| Element | Login Page | Verification Page |
|---------|------------|-------------------|
| Background | Gradient | Batik image |
| Left Panel Gradient | 135deg gradient | 135deg gradient ✅ |
| Decorative Elements | SVG pattern + blobs | Pulsing radial gradient |
| Logo Component | `<OfficialLogo>` | `<Image>` tag directly |
| Features Icons | Gold background boxes | Gold background boxes ✅ |
| Right Panel Gradient | white to #fef9f5 | white to #fef9f5 ✅ |

**ACTUAL ISSUE:**
1. Verification page uses **batik background image** instead of **gradient background**
2. Verification page uses **direct Image tag** instead of **OfficialLogo component**
3. Verification page has **pulsing animation** instead of **SVG pattern + blobs**

**User wants:** Verification page to look EXACTLY like login page (same background, same logo, same decorations)

### Investigation Steps Needed

1. **Compare Component Usage:**
   - Verify that `OfficialLogo` component exists and works
   - Check if batik background image was intentional design choice
   - Confirm if pulsing animation was intentional

2. **Design Consistency Review:**
   - Review other auth pages (`/register`, `/forgot-password`)
   - Check if they use gradient or batik background
   - Determine if there's a design system document

3. **User Intent Clarification:**
   - Does user want ALL auth pages to match login?
   - Or just verification page?

### Fix Strategy

**APPROACH:** Make verification page layout identical to login page by replacing background, logo, and decorations.

**FILES TO CHANGE:**

1. **c:\Work\LankaConnect\web\src\app\(auth)\verify-email\page.tsx**

   **Changes needed:**
   - Line 58-63: Replace batik background with gradient (match login line 11-12)
   - Lines 82-91: Replace `<Image>` logo with `<OfficialLogo>` component (match login line 38)
   - Lines 70-77: Replace pulsing animation with SVG pattern + gradient blobs (match login lines 19-34)

   **Before (lines 58-63):**
   ```tsx
   backgroundImage: 'url(/images/batik-sri-lanka.jpg)',
   backgroundSize: 'cover',
   backgroundPosition: 'center',
   backgroundRepeat: 'no-repeat'
   ```

   **After (match login):**
   ```tsx
   background: 'linear-gradient(to-r, #FF7900, #8B1538, #006400)'
   ```

   **Before (lines 70-77):**
   ```tsx
   <div
     className="absolute -top-1/2 -right-1/2 w-[200%] h-[200%] pointer-events-none"
     style={{
       background: 'radial-gradient(circle, rgba(255,255,255,0.1) 0%, transparent 70%)',
       animation: 'pulse 8s ease-in-out infinite'
     }}
   />
   ```

   **After (match login lines 19-34):**
   ```tsx
   {/* Decorative Background Pattern */}
   <div className="absolute inset-0 opacity-10">
     <div
       className="absolute inset-0"
       style={{
         backgroundImage: `url("data:image/svg+xml,%3Csvg width='60' height='60' ...")`,
       }}
     ></div>
   </div>

   {/* Decorative gradient blobs */}
   <div className="absolute inset-0 overflow-hidden">
     <div className="absolute -top-24 -left-24 w-96 h-96 bg-orange-400/20 rounded-full blur-3xl"></div>
     <div className="absolute -bottom-24 -right-24 w-96 h-96 bg-emerald-400/20 rounded-full blur-3xl"></div>
     <div className="absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2 w-96 h-96 bg-rose-400/10 rounded-full blur-3xl"></div>
   </div>
   ```

   **Before (lines 82-91):**
   ```tsx
   <div className="flex items-center text-[2rem] font-bold mb-5">
     <Image
       src="/lankaconnect-logo-transparent.png"
       alt="LankaConnect"
       width={80}
       height={80}
       className="mr-5"
       priority
     />
     LankaConnect
   </div>
   ```

   **After (match login line 38):**
   ```tsx
   <OfficialLogo size="md" textColor="text-white" subtitleColor="text-white/90" linkTo="/" />
   ```

**TESTING STRATEGY:**
1. ✅ Update verification page layout to match login
2. ✅ Navigate to `/verify-email?token=test`
3. ✅ Compare side-by-side with `/login`
4. ✅ Verify gradient background matches
5. ✅ Verify OfficialLogo renders correctly
6. ✅ Verify decorative elements match (SVG pattern + blobs)
7. ✅ Check responsive layout on mobile

**RISKS:**
- Removing batik background may conflict with design intent
- OfficialLogo import might be missing (need to add import)
- SVG pattern data URI very long - need to copy exactly

**IMPORTS NEEDED:**
```tsx
import { OfficialLogo } from '@/presentation/components/atoms/OfficialLogo';
```

---

## Issue 4: API Contract Mismatch (User ID Required)

### Categorization
- **Category:** Backend API Issue / Frontend Integration Issue
- **Severity:** Critical (verification fails completely)
- **Dependencies:** Issue 2 must be fixed first
- **Scope:** API contract between frontend and backend

### Root Cause Analysis

**PRIMARY CAUSE:** Backend API requires `userId` in request body, but frontend only sends `token`. Email URL only contains `token` parameter.

**EVIDENCE CHAIN:**

1. **User Feedback**
   - Verification page error: "User ID is required"
   - Error message: "Verification failed"

2. **Backend API Contract**
   - File: `c:\Work\LankaConnect\src\LankaConnect.API\Controllers\AuthController.cs`
   - Line 457: `public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailCommand request, CancellationToken cancellationToken)`
   - Uses `VerifyEmailCommand` which expects BOTH `userId` and `token`

3. **VerifyEmailCommand Definition**
   - File: `c:\Work\LankaConnect\src\LankaConnect.Application\Communications\Commands\VerifyEmail\VerifyEmailCommand.cs`
   - Line 12-14: `public record VerifyEmailCommand(Guid UserId, string Token) : ICommand<VerifyEmailResponse>;`
   - **REQUIRES: UserId (Guid) AND Token (string)**

4. **Frontend API Call**
   - File: `c:\Work\LankaConnect\web\src\infrastructure\api\repositories\auth.repository.ts`
   - **Missing from provided context** - need to read this file
   - Component: `c:\Work\LankaConnect\web\src\presentation\components\features\auth\EmailVerification.tsx`
   - Line 29: `const response = await authRepository.verifyEmail(token);`
   - **ONLY SENDS: token (no userId)**

5. **Email URL Generation**
   - File: `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Email\Configuration\ApplicationUrlsOptions.cs`
   - Line 41: `return $"{FrontendBaseUrl.TrimEnd('/')}{EmailVerificationPath}?token={token}";`
   - **URL CONTAINS: Only token parameter**
   - **NO userId in URL**

**CONTRACT MISMATCH:**

```
Backend Expects: POST /api/auth/verify-email
Body: { userId: "guid", token: "string" }

Frontend Sends: POST /api/auth/verify-email
Body: { token: "string" }  // Missing userId

Email URL: /verify-email?token=abc123  // Only token in URL
```

**ARCHITECTURAL FLAW:**

The token-only approach is CORRECT for security. The backend should:
1. Look up user by token (token is unique and linked to user)
2. Verify token validity
3. Mark email as verified

The current implementation INCORRECTLY requires frontend to know userId, which:
- Exposes user IDs in URLs (security risk)
- Requires extra database lookup on frontend
- Breaks standard email verification patterns

**COMPARISON WITH PASSWORD RESET:**

Password Reset (same codebase):
- File: `c:\Work\LankaConnect\src\LankaConnect.API\Controllers\AuthController.cs`
- Line 420: `public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand request, CancellationToken cancellationToken)`
- ResetPasswordCommand likely contains: `{ token, newPassword }` (NOT userId)
- **Standard pattern: token is self-contained**

### Investigation Steps Needed

1. **Read Frontend Repository File:**
   ```typescript
   // c:\Work\LankaConnect\web\src\infrastructure\api\repositories\auth.repository.ts
   // Check how verifyEmail is implemented
   ```

2. **Check VerifyEmailCommandHandler:**
   ```bash
   # Find handler implementation
   find . -name "VerifyEmailCommandHandler.cs"
   # Check if it looks up user by token or requires userId parameter
   ```

3. **Check Database Schema:**
   ```sql
   -- Check if verification tokens are unique
   SELECT
       COUNT(*) as total_tokens,
       COUNT(DISTINCT email_verification_token) as unique_tokens
   FROM users.users
   WHERE email_verification_token IS NOT NULL;

   -- Check if token can be used to find user
   SELECT id, email, email_verification_token
   FROM users.users
   WHERE email_verification_token = 'example-token';
   ```

4. **Review Similar Implementations:**
   - How does password reset work? (token-only or token+email?)
   - How do other email verification systems work in this codebase?

### Fix Strategy

**APPROACH:** Change API to accept token-only (standard pattern) OR change frontend to send both token and userId (non-standard but matches current contract).

**RECOMMENDED: Option 1 - Fix Backend (Standard Pattern)**

This is the CORRECT architectural solution. Email verification should use token-only approach.

**FILES TO CHANGE:**

1. **c:\Work\LankaConnect\src\LankaConnect.Application\Communications\Commands\VerifyEmail\VerifyEmailCommand.cs**
   ```csharp
   // BEFORE
   public record VerifyEmailCommand(
       Guid UserId,
       string Token) : ICommand<VerifyEmailResponse>;

   // AFTER
   public record VerifyEmailCommand(
       string Token) : ICommand<VerifyEmailResponse>;
   ```

2. **c:\Work\LankaConnect\src\LankaConnect.Application\Communications\Commands\VerifyEmail\VerifyEmailCommandHandler.cs**
   ```csharp
   // BEFORE
   var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
   if (user == null) return Result.Failure("User not found");

   // AFTER
   var user = await _userRepository.GetByEmailVerificationTokenAsync(request.Token, cancellationToken);
   if (user == null) return Result.Failure("Invalid or expired verification token");
   ```

3. **c:\Work\LankaConnect\src\LankaConnect.Domain\Users\IUserRepository.cs**
   ```csharp
   // Add new method
   Task<User?> GetByEmailVerificationTokenAsync(string token, CancellationToken cancellationToken);
   ```

4. **c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Repositories\UserRepository.cs**
   ```csharp
   // Implement new method
   public async Task<User?> GetByEmailVerificationTokenAsync(string token, CancellationToken cancellationToken)
   {
       return await _context.Users
           .FirstOrDefaultAsync(u => u.EmailVerificationToken == token, cancellationToken);
   }
   ```

5. **Update Tests:**
   - VerifyEmailCommandHandlerTests.cs
   - AuthControllerTests.cs
   - Phase6A53VerificationTests.cs

**ALTERNATIVE: Option 2 - Fix Frontend (Non-Standard Pattern)**

Keep backend as-is, change frontend to send userId. This is NOT recommended because:
- Requires parsing userId from somewhere (not in email)
- Exposes user IDs unnecessarily
- Non-standard pattern

**FILES TO CHANGE (if choosing Option 2):**

1. **Email URL Generation:**
   ```csharp
   // c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Email\Configuration\ApplicationUrlsOptions.cs
   public string GetEmailVerificationUrl(string token, Guid userId)
   {
       ValidateFrontendBaseUrl();
       return $"{FrontendBaseUrl.TrimEnd('/')}{EmailVerificationPath}?token={token}&userId={userId}";
   }
   ```

2. **Event Handler:**
   ```csharp
   // c:\Work\LankaConnect\src\LankaConnect.Application\Users\EventHandlers\MemberVerificationRequestedEventHandler.cs
   var verificationUrl = _urlsService.GetEmailVerificationUrl(
       domainEvent.VerificationToken,
       domainEvent.UserId);
   ```

3. **Frontend Repository:**
   ```typescript
   // auth.repository.ts
   verifyEmail: async (token: string, userId: string) => {
       return apiClient.post('/auth/verify-email', { token, userId });
   }
   ```

4. **Frontend Component:**
   ```tsx
   // EmailVerification.tsx
   const searchParams = useSearchParams();
   const token = searchParams.get('token');
   const userId = searchParams.get('userId');

   const response = await authRepository.verifyEmail(token, userId);
   ```

**TESTING STRATEGY (Option 1 - Recommended):**
1. ✅ Implement backend changes (token-only lookup)
2. ✅ Update tests to match new contract
3. ✅ Run unit tests (all should pass)
4. ✅ Run integration tests
5. ✅ Deploy to staging
6. ✅ Trigger new registration
7. ✅ Click email verification link
8. ✅ Verify no "User ID is required" error
9. ✅ Verify verification succeeds

**RISKS (Option 1):**
- Breaking change to API contract (requires frontend update)
- Need to ensure email verification token is unique in database
- Need to handle expired/invalid tokens gracefully

**RISKS (Option 2):**
- Exposes user IDs in email URLs (security concern)
- Non-standard pattern (deviates from industry best practices)
- More complex frontend logic

---

## Fix Plan Priority Order

Based on dependencies and severity:

### Phase 1: Configuration Fixes (Critical Path)
**Must be fixed before anything else can be tested**

1. **Issue 2: Verification Link 404**
   - Create/update `appsettings.Staging.json`
   - Set correct `ApplicationUrls:FrontendBaseUrl`
   - Deploy backend to staging
   - **Estimated Time:** 30 minutes
   - **Risk:** Low

### Phase 2: Backend Architecture Fixes (Critical)
**Must be fixed for verification to work**

2. **Issue 4: API Contract Mismatch**
   - Implement token-only verification (Option 1)
   - Add `GetByEmailVerificationTokenAsync` to repository
   - Update command handler
   - Update tests
   - **Estimated Time:** 2 hours
   - **Risk:** Medium (breaking change)

### Phase 3: Cache and Template Fixes (High Priority)
**Must be fixed for correct email appearance**

3. **Issue 1: Email Template Layout**
   - Restart Azure Container App to clear cache
   - Verify database template content
   - Test new registration email
   - **Estimated Time:** 1 hour
   - **Risk:** Low

### Phase 4: UI Consistency Fixes (High Priority)
**Must be fixed for good UX**

4. **Issue 3: Verification Page Layout**
   - Update verification page to match login layout
   - Replace batik background with gradient
   - Replace Image tag with OfficialLogo
   - Update decorative elements
   - **Estimated Time:** 1 hour
   - **Risk:** Low

---

## Testing Checklist

### End-to-End Verification Flow

1. **Pre-Test Setup:**
   - [ ] All four issues fixed
   - [ ] Backend deployed to staging
   - [ ] Frontend deployed to staging (if separate)
   - [ ] Azure Container App restarted

2. **Registration Test:**
   - [ ] Navigate to registration page
   - [ ] Fill in user details (FirstName, LastName, Email, Password)
   - [ ] Submit registration
   - [ ] Verify HTTP 201 response
   - [ ] Verify user created in database

3. **Email Delivery Test:**
   - [ ] Check email inbox for verification email
   - [ ] Verify sender is correct
   - [ ] Verify subject is "Verify Your Email Address - LankaConnect"
   - [ ] Open email and verify layout matches event registration style
   - [ ] Verify clean gradient header (no decorative stars)
   - [ ] Verify clean footer (no logo image)
   - [ ] Verify verification button is present

4. **Verification Link Test:**
   - [ ] Copy verification link from email
   - [ ] Verify link domain is frontend staging URL (not API URL)
   - [ ] Verify link format: `https://<frontend-url>/verify-email?token=<token>`
   - [ ] Click verification link

5. **Verification Page Test:**
   - [ ] Verify page loads without 404 error
   - [ ] Verify layout matches login page (gradient background, OfficialLogo)
   - [ ] Verify no "User ID is required" error
   - [ ] Verify verification starts automatically
   - [ ] Verify success message appears
   - [ ] Verify redirect to login after 3 seconds

6. **Login Test:**
   - [ ] Login with verified account
   - [ ] Verify login succeeds
   - [ ] Verify access token returned
   - [ ] Verify user can access protected resources

### Deployment Verification Steps

1. **Database State:**
   ```sql
   -- Verify template content
   SELECT name, description, updated_at
   FROM communications.email_templates
   WHERE name = 'member-email-verification';

   -- Verify migration applied
   SELECT "MigrationId", "ProductVersion"
   FROM "__EFMigrationsHistory"
   WHERE "MigrationId" LIKE '%Phase6A53%'
   ORDER BY "MigrationId";
   ```

2. **Configuration State:**
   ```bash
   # Check environment variables
   az containerapp show \
     --name lankaconnect-api-staging \
     --resource-group <rg> \
     --query "properties.template.containers[0].env"
   ```

3. **Application Logs:**
   ```bash
   # Monitor verification flow
   az containerapp logs show \
     --name lankaconnect-api-staging \
     --resource-group <rg> \
     --follow | grep -i "verification"
   ```

---

## Similar Implementations Reference

### Event Registration Email (Working Example)

**Template:** `registration-confirmation`
- File: `c:\Work\LankaConnect\temp_migration.sql`
- Layout: Clean gradient header + content + clean footer
- NO decorative elements
- NO logo images
- Simple text and gradient styling

**This is the target layout for member verification email.**

### Password Reset Flow (Reference for Token-Only Pattern)

**Expected Implementation:**
- User requests password reset
- Email sent with reset link: `/reset-password?token=xyz`
- Token is unique and contains all needed info
- Backend looks up user by token
- NO userId required in URL or request body

**Member verification should follow the same pattern.**

---

## Conclusion

All four issues have distinct root causes and require coordinated fixes:

1. **Email Template:** Cache invalidation needed (restart Azure Container App)
2. **Verification Link:** Configuration fix needed (update FrontendBaseUrl)
3. **Page Layout:** UI consistency fix needed (match login page)
4. **API Contract:** Backend architecture fix needed (token-only verification)

**Total Estimated Fix Time:** 4.5 hours
**Deployment Count:** 1 (all fixes can be deployed together)
**Risk Level:** Medium (breaking API change requires coordinated frontend/backend deployment)

**Next Step:** User decision on:
- Where is frontend staging deployed? (needed for Issue 2)
- Approve backend breaking change for token-only verification (Issue 4)
