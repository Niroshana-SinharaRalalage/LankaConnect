# WWW Subdomain Implementation Guide

**Date**: 2026-02-15
**Objective**: Add `www.lankaconnect.app` with 301 redirect to `lankaconnect.app`
**DNS Provider**: Namecheap
**Estimated Time**: 30 minutes

---

## STRATEGY

**Canonical URL**: `lankaconnect.app` (non-www)
**Redirect Flow**: `www.lankaconnect.app` → (301 redirect) → `lankaconnect.app`

---

## IMPLEMENTATION STEPS

### STEP 1: Add WWW Subdomain to Azure Container App (10 minutes)

#### 1.1 Login to Azure CLI

```bash
# Login to Azure
az login

# Verify you're in the correct subscription
az account show

# Set correct subscription if needed
# az account set --subscription "YOUR-SUBSCRIPTION-ID"
```

#### 1.2 Add WWW Custom Domain

```bash
# Add www.lankaconnect.app to the Container App
az containerapp hostname add \
  --hostname www.lankaconnect.app \
  --name lankaconnect-ui-prod \
  --resource-group lankaconnect-prod
```

**Expected Output**:
```json
{
  "bindingType": "SniEnabled",
  "certificateId": null,
  "name": "www.lankaconnect.app"
}
```

#### 1.3 Get Verification Token (if required)

```bash
# Azure will provide a verification token
# You may need to add this as a TXT record in Namecheap
az containerapp hostname list \
  --name lankaconnect-ui-prod \
  --resource-group lankaconnect-prod \
  --query "[?name=='www.lankaconnect.app']"
```

---

### STEP 2: Configure DNS in Namecheap (5 minutes)

#### 2.1 Login to Namecheap
- Go to: https://ap.www.namecheap.com/Domains/DomainControlPanel/lankaconnect.app/advancedns
- Navigate to: **Domain List** → **lankaconnect.app** → **Advanced DNS**

#### 2.2 Add CNAME Record for WWW

**Current DNS Records** (from your screenshot):
```
Type    Host    Value
CNAME   @       lankaconnect-ui-prod.graystone-d581eaeb.eastus2.azurecontainerapps.io
CNAME   api     lankaconnect-api-prod.graystone-d581eaeb.eastus2.azurecontainerapps.io
```

**Add This New Record**:
```
Type    Host    Value                                                              TTL
CNAME   www     lankaconnect-ui-prod.graystone-d581eaeb.eastus2.azurecontainerapps.io   30 min
```

#### 2.3 Click "Save All Changes"

#### 2.4 Add TXT Record (if Azure requires verification)

**Only if Azure asks for domain verification**:
```
Type    Host    Value                                                TTL
TXT     @       ms-domain-verification=XXXXX-XXXXX-XXXXX-XXXXX       30 min
```

(Replace `XXXXX-XXXXX-XXXXX-XXXXX` with token from Azure)

---

### STEP 3: Bind SSL Certificate (10 minutes)

Wait 5-10 minutes for DNS propagation, then bind the certificate:

```bash
# Verify DNS propagation first
nslookup www.lankaconnect.app

# Bind certificate (Azure auto-managed SSL)
az containerapp hostname bind \
  --hostname www.lankaconnect.app \
  --name lankaconnect-ui-prod \
  --resource-group lankaconnect-prod \
  --validation-method CNAME
```

**Expected Output**:
```json
{
  "bindingType": "SniEnabled",
  "certificateId": "/subscriptions/.../certificates/...",
  "name": "www.lankaconnect.app"
}
```

---

### STEP 4: Create Next.js Redirect Middleware (5 minutes)

#### 4.1 Create Middleware File

Create file: `web/middleware.ts`

```typescript
import { NextRequest, NextResponse } from 'next/server';

/**
 * Middleware to handle www → non-www redirect
 * SEO Best Practice: Canonical URL is lankaconnect.app (non-www)
 */
export function middleware(request: NextRequest) {
  const url = request.nextUrl.clone();
  const hostname = request.headers.get('host') || '';

  // Redirect www to non-www (301 Permanent Redirect)
  if (hostname === 'www.lankaconnect.app') {
    url.hostname = 'lankaconnect.app';
    return NextResponse.redirect(url, 301);
  }

  return NextResponse.next();
}

// Apply middleware to all routes
export const config = {
  matcher: '/:path*',
};
```

#### 4.2 Test Locally (Optional)

```bash
# Test with curl (simulate www hostname)
curl -H "Host: www.lankaconnect.app" http://localhost:3000

# Expected: Should see redirect or localhost behavior
```

---

### STEP 5: Deploy to Production (5 minutes)

#### 5.1 Commit and Push

```bash
cd c:/Work/LankaConnect

# Stage changes
git add web/middleware.ts

# Commit
git commit -m "feat(www): Add www to non-www redirect middleware

- Add web/middleware.ts to handle www.lankaconnect.app redirect
- Implements 301 permanent redirect to lankaconnect.app
- SEO best practice: single canonical domain

Refs: docs/RCA_WWW_SUBDOMAIN_MISSING.md"

# Push to main (triggers production deployment)
git push origin main
```

#### 5.2 Monitor Deployment

```bash
# Watch GitHub Actions deployment
# Go to: https://github.com/YOUR-ORG/LankaConnect/actions

# Or watch from CLI
gh run watch
```

#### 5.3 Wait for Deployment (5-10 minutes)

GitHub Actions will:
1. Build Next.js with new middleware
2. Push Docker image to ACR
3. Update Container App
4. Run smoke tests

---

### STEP 6: Verification & Testing (5 minutes)

#### 6.1 Verify DNS Resolution

```bash
# Should resolve to Azure Container App
nslookup www.lankaconnect.app

# Expected output:
# Name:    lankaconnect-ui-prod.graystone-d581eaeb.eastus2.azurecontainerapps.io
# Aliases: www.lankaconnect.app
```

#### 6.2 Test HTTPS Works

```bash
# Should return 301 redirect to lankaconnect.app
curl -I https://www.lankaconnect.app

# Expected output:
# HTTP/1.1 301 Moved Permanently
# Location: https://lankaconnect.app/
```

#### 6.3 Test in Browser

Open browser and test:
- [ ] `https://www.lankaconnect.app` → Redirects to `https://lankaconnect.app`
- [ ] `https://lankaconnect.app` → Loads normally
- [ ] SSL certificate is valid for both domains
- [ ] No browser warnings

#### 6.4 Verify SSL Certificate

```bash
# Check certificate covers both domains
openssl s_client -connect www.lankaconnect.app:443 -servername www.lankaconnect.app 2>/dev/null | openssl x509 -noout -text | grep -A1 "Subject Alternative Name"

# Expected: DNS:lankaconnect.app, DNS:www.lankaconnect.app
```

#### 6.5 Test CORS (Backend)

```bash
# Test API with www origin
curl -X OPTIONS https://lankaconnect-api-prod.graystone-d581eaeb.eastus2.azurecontainerapps.io/api/health \
  -H "Origin: https://www.lankaconnect.app" \
  -H "Access-Control-Request-Method: GET" \
  -v

# Expected: Should see CORS headers
# Access-Control-Allow-Origin: https://www.lankaconnect.app
```

---

## ROLLBACK PLAN (If Something Goes Wrong)

### If DNS Issues:
```bash
# Remove DNS record from Namecheap
# Delete the CNAME record for www
```

### If Azure Issues:
```bash
# Remove custom domain from Container App
az containerapp hostname delete \
  --hostname www.lankaconnect.app \
  --name lankaconnect-ui-prod \
  --resource-group lankaconnect-prod
```

### If Middleware Breaks:
```bash
# Revert the commit
git revert HEAD
git push origin main

# Or delete middleware.ts
git rm web/middleware.ts
git commit -m "revert: Remove www redirect middleware"
git push origin main
```

---

## SUCCESS CRITERIA

- [x] DNS resolves: `www.lankaconnect.app` → Azure Container App IP
- [x] HTTPS works: `https://www.lankaconnect.app` returns 200 or 301
- [x] SSL certificate valid for both `lankaconnect.app` and `www.lankaconnect.app`
- [x] Redirect works: `www.lankaconnect.app` → `lankaconnect.app` (301)
- [x] CORS headers work for both origins
- [x] No browser console errors
- [x] Google Search Console updated (if applicable)

---

## POST-IMPLEMENTATION

### 1. Update Google Search Console
- Add `www.lankaconnect.app` as property
- Set canonical URL to `lankaconnect.app`

### 2. Update Analytics
- Verify both domains tracked in Google Analytics
- Set up URL redirect tracking

### 3. Update Marketing Materials
- Verify email templates use canonical URL
- Update social media links
- Update business cards, brochures, etc.

### 4. Monitor for 7 Days
- Check Azure logs for redirect traffic
- Monitor SSL certificate auto-renewal
- Watch for any CORS errors

---

## TROUBLESHOOTING

### Issue: DNS not resolving after 1 hour
**Solution**:
- Check Namecheap DNS settings saved correctly
- Verify CNAME points to correct Azure hostname
- Try `nslookup www.lankaconnect.app 8.8.8.8` (use Google DNS)

### Issue: SSL certificate not provisioned
**Solution**:
```bash
# Check certificate status
az containerapp hostname list \
  --name lankaconnect-ui-prod \
  --resource-group lankaconnect-prod

# If certificate missing, retry binding
az containerapp hostname bind \
  --hostname www.lankaconnect.app \
  --name lankaconnect-ui-prod \
  --resource-group lankaconnect-prod \
  --validation-method CNAME
```

### Issue: Redirect not working
**Solution**:
- Check middleware.ts deployed correctly
- Verify container app restarted with new image
- Check Azure Container App logs:
```bash
az containerapp logs show \
  --name lankaconnect-ui-prod \
  --resource-group lankaconnect-prod \
  --tail 100
```

### Issue: CORS errors from www subdomain
**Solution**:
Backend already configured correctly ([Program.cs:163](../src/LankaConnect.API/Program.cs#L163)).
If issues persist, check API logs:
```bash
az containerapp logs show \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --tail 100 | grep CORS
```

---

## TIMELINE SUMMARY

| Step | Duration | Cumulative |
|------|----------|------------|
| 1. Azure Container App Config | 10 min | 10 min |
| 2. Namecheap DNS Config | 5 min | 15 min |
| 3. SSL Certificate Binding | 10 min | 25 min |
| 4. Create Middleware | 5 min | 30 min |
| 5. Deploy to Production | 5 min | 35 min |
| 6. DNS Propagation (wait) | 5-60 min | 40-95 min |
| 7. Verification | 5 min | 45-100 min |

**Total Active Work**: 45 minutes
**Total Elapsed Time**: 45 minutes - 2 hours (with DNS propagation)

---

## NOTES

- **No downtime expected**: `lankaconnect.app` continues working throughout
- **SEO impact**: Positive (consolidates link equity to single domain)
- **User impact**: Better UX (both URLs work)
- **Cost impact**: None (Azure managed SSL certificates are free)

---

## RELATED DOCUMENTS

- [RCA: WWW Subdomain Missing](RCA_WWW_SUBDOMAIN_MISSING.md)
- [Production Deployment Guide](PRODUCTION_DEPLOYMENT_COMPLETE_GUIDE.md)
- [UI Deployment Workflow](../.github/workflows/deploy-ui-production.yml)

---

**Ready to implement?** Follow the steps in order. Let me know if you encounter any issues!
