# Stripe Webhook Authentication Fix

**Date**: 2025-12-17
**Issue**: Stripe webhooks not reaching controller due to authentication middleware blocking requests
**Status**: FIXED - Ready for deployment

## Root Cause Analysis

### The Problem

Stripe webhooks were being blocked by JWT authentication middleware BEFORE they could reach the `PaymentsController.Webhook()` endpoint, despite having `[AllowAnonymous]` attribute.

**Issue Flow**:
```
Stripe → Azure Container App → CORS → Authentication Middleware (JWT VALIDATION FAILS)
→ Request REJECTED → Never reaches PaymentsController
```

### Why This Happened

1. **Authentication middleware ran before routing** (line 354 in Program.cs)
2. Authentication middleware attempted JWT validation on ALL requests
3. Stripe webhooks don't send JWT tokens (they use `Stripe-Signature` header instead)
4. Without routing context, middleware couldn't see `[AllowAnonymous]` attribute
5. Request rejected with HTTP 400 before reaching controller

### Evidence

- No "POST /api/payments/webhook" in Azure logs
- No "Processing webhook event" logs
- No "Webhook endpoint reached" logs
- User completed payment but no email/ticket generated

## The Fix

### Changes Made

**File**: `src/LankaConnect.API/Program.cs`

**Change 1**: Added `app.UseRouting()` before authentication middleware

```csharp
// BEFORE (Line ~354):
app.UseCustomAuthentication();
app.MapControllers();

// AFTER (Line 355):
app.UseRouting();  // NEW: Enable routing BEFORE authentication
app.UseCustomAuthentication();
app.MapControllers();
```

**Why This Works**:
- `UseRouting()` analyzes the request and determines which endpoint will handle it
- Routing populates endpoint metadata (including `[AllowAnonymous]`)
- Authentication middleware can now see and respect `[AllowAnonymous]`
- Webhooks bypass JWT validation and reach the controller

**Change 2**: Enhanced webhook logging in `PaymentsController.cs`

Added diagnostic logging at the very start of the webhook method:

```csharp
_logger.LogInformation("Webhook endpoint reached - Method: {Method}, Path: {Path}...");
_logger.LogInformation("Webhook body received - Length: {Length}, HasSignature: {HasSignature}...");
```

This provides immediate confirmation when webhooks start working.

## Architecture Context

### Correct Middleware Order

```
1. CORS (handles preflight requests)
2. CORS Error Handler (preserves headers on errors)
3. Serilog Request Logging (logs all requests)
4. UseRouting() ← CRITICAL: Determines endpoint metadata
5. UseAuthentication() ← Now has routing context
6. UseAuthorization() ← Respects [AllowAnonymous]
7. MapControllers() ← Actual endpoint execution
```

### Why Order Matters

ASP.NET Core middleware runs in a pipeline. Each middleware can:
- Process the request
- Call the next middleware
- Process the response

**Authentication middleware needs routing context** to:
1. Know which endpoint will handle the request
2. Read endpoint metadata (`[AllowAnonymous]`, `[Authorize]`, etc.)
3. Make authorization decisions based on metadata

Without `UseRouting()` first, authentication middleware sees ALL requests as requiring authentication.

## Verification Steps

### 1. Build Verification
```bash
dotnet build src/LankaConnect.API/LankaConnect.API.csproj
# Should show: Build succeeded. 0 Warning(s) 0 Error(s)
```

### 2. Local Testing (if running locally)
```bash
# Start the API
dotnet run --project src/LankaConnect.API

# In another terminal, test webhook endpoint
curl -X POST http://localhost:5000/api/payments/webhook \
  -H "Content-Type: application/json" \
  -d '{}'

# Should see HTTP 400 (signature verification failed - expected)
# Should see in logs: "Webhook endpoint reached"
```

### 3. Azure Deployment
```bash
# Use the deployment script
bash scripts/deploy-webhook-fix.sh
```

### 4. Post-Deployment Verification

**Check 1**: Endpoint accessibility
```bash
curl -X POST https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/payments/webhook
# Should return HTTP 400 (expected - no signature)
```

**Check 2**: Azure logs
```bash
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group LankaConnect-ResourceGroup \
  --follow
```

Look for:
- ✓ "Webhook endpoint reached" - Endpoint is now accessible
- ✓ "Processing webhook event" - Signature verified
- ✓ "Successfully completed payment" - Payment processed

**Check 3**: Test with real Stripe event
```bash
# Trigger test event in Stripe dashboard
# Or use Stripe CLI:
stripe trigger checkout.session.completed
```

Expected results:
- Webhook shows in Azure logs within 5 seconds
- Email sent to user
- Ticket visible in user's dashboard

## Rollback Plan

If the fix causes issues:

**Option 1: Revert via Azure Portal**
1. Go to Container App → Revisions
2. Select previous revision
3. Click "Activate"

**Option 2: Revert code changes**
```bash
git revert <commit-hash>
git push
# Redeploy
```

**Option 3: Quick fix for testing**

If you need to temporarily bypass authentication for webhooks while debugging:

```csharp
// In AuthenticationExtensions.cs - UseCustomAuthentication method
app.Use(async (context, next) =>
{
    // Skip authentication for webhook endpoint
    if (context.Request.Path.StartsWithSegments("/api/payments/webhook"))
    {
        await next();
        return;
    }
    await next();
});

app.UseAuthentication();
app.UseAuthorization();
```

## Testing Checklist

After deployment, verify:

- [ ] Health endpoint returns HTTP 200: `GET /health`
- [ ] Webhook endpoint returns HTTP 400 for empty requests: `POST /api/payments/webhook`
- [ ] Azure logs show "Webhook endpoint reached" when testing
- [ ] Authenticated endpoints still require JWT (test `GET /api/events/my-events`)
- [ ] Stripe test event triggers webhook processing
- [ ] Email is sent after payment completion
- [ ] Ticket is generated and visible in user dashboard

## Related Files

- `src/LankaConnect.API/Program.cs` - Middleware pipeline configuration
- `src/LankaConnect.API/Controllers/PaymentsController.cs` - Webhook endpoint
- `src/LankaConnect.API/Extensions/AuthenticationExtensions.cs` - Authentication setup
- `scripts/deploy-webhook-fix.sh` - Deployment automation

## References

- [ASP.NET Core Middleware Order](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/)
- [ASP.NET Core Routing](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing)
- [Stripe Webhook Signature Verification](https://stripe.com/docs/webhooks/signatures)

## Next Steps

1. Deploy to staging environment
2. Test with Stripe test events
3. Monitor for 24 hours
4. If successful, deploy to production
5. Update Stripe webhook URLs in production account
6. Archive this document for future reference

---

**Key Takeaway**: When using `[AllowAnonymous]` on endpoints like webhooks, ensure `UseRouting()` is called BEFORE `UseAuthentication()` in the middleware pipeline. This allows the authentication middleware to read endpoint metadata and respect the `[AllowAnonymous]` attribute.
