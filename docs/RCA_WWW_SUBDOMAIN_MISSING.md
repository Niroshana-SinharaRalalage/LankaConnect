# ROOT CAUSE ANALYSIS: Production WWW Subdomain Missing

**Date**: 2026-02-15
**Issue**: `www.lankaconnect.app` does not resolve (DNS failure)
**Severity**: Medium (SEO + UX impact)
**Type**: Infrastructure - DNS Configuration

---

## EXECUTIVE SUMMARY

The production domain `lankaconnect.app` works correctly, but the www subdomain (`www.lankaconnect.app`) does not exist. This is a **DNS configuration gap** - the www CNAME record was never added to Azure Container Apps or the DNS provider.

**Good News**: Backend code is already configured to support www subdomain. This is a pure infrastructure fix with no code changes required.

---

## ISSUE DISCOVERED

### What Works ✅
- `https://lankaconnect.app` → HTTP 200 OK
- Resolves to: `lankaconnect-ui-prod.graystone-d581eaeb.eastus2.azurecontainerapps.io` (68.220.222.98)

### What Doesn't Work ❌
- `https://www.lankaconnect.app` → DNS resolution failure
- Error: `Could not resolve host: www.lankaconnect.app`

---

## DIAGNOSTIC EVIDENCE

### 1. DNS Test Results

```bash
# Non-WWW (WORKS)
$ nslookup lankaconnect.app
Name:    lankaconnect-ui-prod.graystone-d581eaeb.eastus2.azurecontainerapps.io
Address:  68.220.222.98
Aliases:  lankaconnect.app

# WWW Subdomain (FAILS - Non-existent domain)
$ nslookup www.lankaconnect.app
*** can't find www.lankaconnect.app: Non-existent domain
```

### 2. HTTP Test Results

```bash
# Non-WWW (200 OK)
$ curl -I https://lankaconnect.app
HTTP/1.1 200 OK
vary: rsc, next-router-state-tree, next-router-prefetch
x-nextjs-cache: HIT
x-powered-by: Next.js
content-type: text/html; charset=utf-8

# WWW (DNS Failure)
$ curl -I https://www.lankaconnect.app
curl: (6) Could not resolve host: www.lankaconnect.app
```

### 3. Backend CORS Configuration

File: [src/LankaConnect.API/Program.cs:157-167](../src/LankaConnect.API/Program.cs#L157-L167)

```csharp
options.AddPolicy("Production", policy =>
{
    policy.WithOrigins(
        "https://lankaconnect.com",
        "https://www.lankaconnect.com",
        "https://lankaconnect.app",
        "https://www.lankaconnect.app")  // ✅ Already configured
          .AllowAnyMethod()
          .AllowAnyHeader()
          .AllowCredentials();
});
```

**Status**: ✅ Backend is ready to support www subdomain

### 4. Azure Container App Configuration

- **Resource Group**: `lankaconnect-prod`
- **Container App**: `lankaconnect-ui-prod`
- **Current Custom Domains**: `lankaconnect.app` (apex only)
- **Missing**: `www.lankaconnect.app` subdomain

---

## ROOT CAUSE

**DNS Configuration Incomplete** - The www subdomain was never configured during initial production deployment.

### What's Missing:
1. **Azure Container App**: www subdomain not added to custom domains
2. **DNS Provider**: No CNAME record for www subdomain
3. **SSL Certificate**: May need to include www in certificate (depends on Azure's auto-cert)

### Why This Happened:
- Initial deployment focused on apex domain (`lankaconnect.app`)
- www subdomain configuration was likely overlooked
- No validation step to ensure both variants work

---

## IMPACT ASSESSMENT

### SEO Impact: ⚠️ MEDIUM
- Users typing `www.lankaconnect.app` get "site not found" error
- Search engines may penalize for broken www variant
- Duplicate content issues if www eventually works without redirect
- Lost organic traffic from www searches

### User Experience: ⚠️ LOW-MEDIUM
- Most users will access `lankaconnect.app` directly
- Browser autocomplete may add www, causing confusion
- Email links/marketing materials might use www variant

### Business Impact: ⚠️ LOW
- Not blocking core functionality
- Minimal revenue impact (most traffic uses non-www)
- Brand perception slightly affected (looks unprofessional)

---

## FIX PLAN

### Option 1: Add WWW and Redirect to Non-WWW (RECOMMENDED)

**Strategy**: Make www.lankaconnect.app work and redirect to lankaconnect.app

**Steps**:
1. Add `www.lankaconnect.app` as custom domain in Azure Container App
2. Configure DNS CNAME record: `www.lankaconnect.app` → `lankaconnect-ui-prod.graystone-d581eaeb.eastus2.azurecontainerapps.io`
3. Enable SSL certificate for www subdomain (Azure auto-managed)
4. Configure 301 redirect: `https://www.lankaconnect.app` → `https://lankaconnect.app`

**Azure CLI Commands**:
```bash
# Step 1: Add www subdomain to Container App
az containerapp hostname add \
  --hostname www.lankaconnect.app \
  --name lankaconnect-ui-prod \
  --resource-group lankaconnect-prod

# Step 2: Bind certificate (Azure auto-managed)
az containerapp hostname bind \
  --hostname www.lankaconnect.app \
  --name lankaconnect-ui-prod \
  --resource-group lankaconnect-prod \
  --environment lankaconnect-prod-env \
  --validation-method CNAME
```

**DNS Configuration** (at DNS provider - e.g., GoDaddy, Cloudflare):
```
Type    Name    Value
CNAME   www     lankaconnect-ui-prod.graystone-d581eaeb.eastus2.azurecontainerapps.io
```

**Redirect Configuration** (Next.js middleware):
```typescript
// web/middleware.ts
export function middleware(request: NextRequest) {
  const url = request.nextUrl.clone();

  // Redirect www to non-www
  if (url.hostname === 'www.lankaconnect.app') {
    url.hostname = 'lankaconnect.app';
    return NextResponse.redirect(url, 301); // Permanent redirect
  }

  return NextResponse.next();
}
```

**Pros**:
- ✅ SEO-friendly (single canonical URL)
- ✅ Best practice (pick one and redirect)
- ✅ Minimal configuration

**Cons**:
- ⚠️ Requires Next.js middleware change
- ⚠️ Slight latency for www users (one extra hop)

---

### Option 2: Add WWW Only (No Redirect)

**Strategy**: Make both work independently without redirect

**Steps**:
1. Same Azure + DNS configuration as Option 1
2. Skip redirect logic

**Pros**:
- ✅ No code changes needed
- ✅ Both URLs work

**Cons**:
- ❌ SEO duplicate content issue
- ❌ Not recommended best practice
- ❌ Analytics split across two URLs

---

### Option 3: Do Nothing (NOT RECOMMENDED)

**Strategy**: Keep only non-www working

**Pros**:
- ✅ Zero effort

**Cons**:
- ❌ Poor user experience (www fails)
- ❌ SEO penalty
- ❌ Unprofessional appearance

---

## RECOMMENDATION

**Choose Option 1**: Add www subdomain with 301 redirect to non-www

**Rationale**:
1. SEO best practice (canonical URL)
2. Better user experience (both URLs work)
3. Professional domain setup
4. Minimal effort (2 Azure commands + 1 DNS record + small middleware change)

---

## IMPLEMENTATION CHECKLIST

### Phase 1: Azure Configuration
- [ ] Add `www.lankaconnect.app` custom domain to Container App
- [ ] Verify SSL certificate auto-provisioning for www
- [ ] Test `https://www.lankaconnect.app` returns 200 OK

### Phase 2: DNS Configuration (at DNS Provider)
- [ ] Add CNAME record: `www` → `lankaconnect-ui-prod.graystone-d581eaeb.eastus2.azurecontainerapps.io`
- [ ] Wait for DNS propagation (5-60 minutes)
- [ ] Verify with `nslookup www.lankaconnect.app`

### Phase 3: Redirect Logic (Code Change)
- [ ] Create/update `web/middleware.ts` with www → non-www redirect
- [ ] Test locally with HOST header: `curl -H "Host: www.lankaconnect.app" http://localhost:3000`
- [ ] Deploy to production
- [ ] Verify redirect: `curl -I https://www.lankaconnect.app` shows 301 → `https://lankaconnect.app`

### Phase 4: Validation
- [ ] Test both URLs in browser
- [ ] Check SSL certificate covers both domains
- [ ] Verify CORS headers work for both origins
- [ ] Update Google Search Console (if configured)

---

## TESTING COMMANDS

```bash
# Test DNS resolution
nslookup www.lankaconnect.app
nslookup lankaconnect.app

# Test HTTP/HTTPS
curl -I https://www.lankaconnect.app
curl -I https://lankaconnect.app

# Test redirect (after implementation)
curl -I https://www.lankaconnect.app
# Expected: HTTP/1.1 301 Moved Permanently
# Expected: Location: https://lankaconnect.app

# Test SSL certificate
openssl s_client -connect www.lankaconnect.app:443 -servername www.lankaconnect.app
# Expected: Subject Alternative Names should include both www and non-www
```

---

## PREVENTION FOR FUTURE

1. **Deployment Checklist**: Add "Verify both www and non-www work" step
2. **DNS Monitoring**: Set up monitoring for both domains
3. **Documentation**: Document canonical URL strategy in deployment guide
4. **Automated Tests**: Add smoke tests for both URL variants

---

## RELATED FILES

- [src/LankaConnect.API/Program.cs](../src/LankaConnect.API/Program.cs#L157-L167) - CORS configuration
- [.github/workflows/deploy-ui-production.yml](../.github/workflows/deploy-ui-production.yml) - Production deployment
- [src/LankaConnect.API/appsettings.Production.json](../src/LankaConnect.API/appsettings.Production.json#L80) - Frontend URL config

---

## QUESTIONS FOR USER

1. **Who manages DNS records?** (GoDaddy, Cloudflare, Azure DNS, other?)
2. **Do you want www → non-www redirect?** (Recommended: Yes)
3. **Any other domains to configure?** (e.g., `lankaconnect.com`)

---

**Status**: AWAITING USER DECISION
**Next Action**: User to choose Option 1, 2, or 3
**Estimated Fix Time**: 30 minutes (Option 1)
