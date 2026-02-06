# Quick Fix: Stripe Webhook Secret Configuration

**Issue:** HTTP 400 "Invalid signature" on Stripe webhooks
**Root Cause:** Missing configuration binding
**Fix Time:** 15 minutes

---

## The Fix (2 Lines)

### 1. Edit `src/LankaConnect.API/appsettings.Staging.json`

**Find this (around line 51):**
```json
"Stripe": {
  "SecretKey": "${STRIPE_SECRET_KEY}",
  "PublishableKey": "${STRIPE_PUBLISHABLE_KEY}"
}
```

**Change to this:**
```json
"Stripe": {
  "SecretKey": "${STRIPE_SECRET_KEY}",
  "PublishableKey": "${STRIPE_PUBLISHABLE_KEY}",
  "WebhookSecret": "${STRIPE_WEBHOOK_SECRET}"
}
```

### 2. Edit `.github/workflows/deploy-staging.yml`

**Find this (around line 154):**
```yaml
Stripe__SecretKey=secretref:stripe-secret-key \
Stripe__PublishableKey=secretref:stripe-publishable-key
```

**Change to this:**
```yaml
Stripe__SecretKey=secretref:stripe-secret-key \
Stripe__PublishableKey=secretref:stripe-publishable-key \
Stripe__WebhookSecret=secretref:stripe-webhook-secret
```

---

## Deploy

```bash
git add src/LankaConnect.API/appsettings.Staging.json
git add .github/workflows/deploy-staging.yml
git commit -m "fix(phase-6a24): Add Stripe webhook secret configuration binding"
git push origin develop
```

---

## Verify (After GitHub Actions Completes)

```bash
# Test webhook
stripe listen --forward-to https://lankaconnect-api-staging.azurewebsites.net/api/payments/webhook
stripe trigger checkout.session.completed

# Should see: HTTP 200 OK (not 400 Bad Request)
```

---

## Why This Works

**Before:**
- Secret exists in Key Vault ✅
- BUT not defined in appsettings.json ❌
- AND not set in deployment workflow ❌
- Result: Configuration binding skips it → empty string → signature fails

**After:**
- Secret exists in Key Vault ✅
- Defined in appsettings.json ✅
- Set in deployment workflow ✅
- Result: Configuration binding works → correct secret → signature succeeds

---

## Full Documentation

For detailed root cause analysis, see:
- [PHASE_6A_24_WEBHOOK_SECRET_ROOT_CAUSE_ANALYSIS.md](./PHASE_6A_24_WEBHOOK_SECRET_ROOT_CAUSE_ANALYSIS.md)
- [ADR-007-Stripe-Webhook-Secret-Azure-Container-Apps-Key-Vault.md](./ADR-007-Stripe-Webhook-Secret-Azure-Container-Apps-Key-Vault.md)
