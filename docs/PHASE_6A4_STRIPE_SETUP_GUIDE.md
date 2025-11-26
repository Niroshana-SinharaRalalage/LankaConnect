# Phase 6A.4: Stripe API Keys Configuration Guide

**Purpose**: Configure Stripe test API keys in Azure Container Apps for staging environment
**Environment**: Azure Container Apps Staging
**Status**: Required before E2E testing

---

## Step 1: Get Stripe Test API Keys

### 1.1 Login to Stripe Dashboard
1. Go to https://dashboard.stripe.com/
2. Login with your Stripe account
3. **IMPORTANT**: Toggle to **Test Mode** (switch in top right corner should say "Test mode")

### 1.2 Retrieve API Keys
1. Navigate to **Developers** → **API Keys**
2. Copy the following keys:
   - **Publishable key**: Starts with `pk_test_...`
   - **Secret key**: Click "Reveal test key token" → Starts with `sk_test_...`

**Example** (these are fake, use your real test keys):
```
Publishable key: pk_test_51ABCDEFGHijklmnopQRSTUVwxyz1234567890
Secret key: sk_test_51ABCDEFGHijklmnopQRSTUVwxyz1234567890
```

---

## Step 2: Configure Azure App Settings

### Option A: Azure Portal (Recommended for First-Time Setup)

1. **Open Azure Portal**
   - Go to https://portal.azure.com/
   - Login with your Azure account

2. **Navigate to Container App**
   - Search for "lankaconnect-api-staging"
   - Click on the Container App resource

3. **Open Environment Variables**
   - In left menu, click **Settings** → **Environment variables**
   - Click **+ Add** button

4. **Add Stripe Keys**

   **Add Secret Key**:
   - Name: `Stripe__SecretKey`
   - Value: `sk_test_...` (paste your secret key)
   - Click **Save**

   **Add Publishable Key**:
   - Name: `Stripe__PublishableKey`
   - Value: `pk_test_...` (paste your publishable key)
   - Click **Save**

5. **Restart Container App**
   - Click **Overview** in left menu
   - Click **Restart** button
   - Wait for restart to complete (~1-2 minutes)

### Option B: Azure CLI (Quick Setup)

```bash
# Set Stripe Secret Key
az containerapp update \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-rg \
  --set-env-vars "Stripe__SecretKey=sk_test_YOUR_SECRET_KEY"

# Set Stripe Publishable Key
az containerapp update \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-rg \
  --set-env-vars "Stripe__PublishableKey=pk_test_YOUR_PUBLISHABLE_KEY"

# Restart container
az containerapp revision restart \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-rg
```

---

## Step 3: Verify Configuration

### 3.1 Check Config Endpoint
```bash
curl https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/payments/config
```

**Expected Response** (should return 200):
```json
{
  "publishableKey": "pk_test_..."
}
```

**If you get 500 error**:
- Stripe__SecretKey is not configured or is invalid
- Check Azure App Settings and verify key format

### 3.2 Check Application Logs
```bash
# View recent logs
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-rg \
  --tail 50
```

**Look for**:
- ✅ "Stripe client initialized successfully"
- ❌ "Stripe:SecretKey is not configured" (means key missing)

---

## Step 4: Configure Stripe Products and Prices

### 4.1 Create Products in Stripe Dashboard

**Product 1: Event Organizer - Monthly**
1. Navigate to **Products** → **+ Add Product**
2. **Name**: "Event Organizer - Monthly"
3. **Pricing Model**: Standard pricing
4. **Price**: $20.00 USD
5. **Billing Period**: Monthly
6. **Price ID**: Copy the generated price ID (e.g., `price_1ABC123...`)
7. Click **Save product**

**Product 2: Event Organizer - Annual**
1. Click **+ Add Product**
2. **Name**: "Event Organizer - Annual"
3. **Price**: $200.00 USD
4. **Billing Period**: Yearly
5. **Price ID**: Copy the generated price ID
6. Click **Save product**

### 4.2 Update Frontend Price IDs

**File**: `web/src/presentation/components/features/payments/SubscriptionUpgradeModal.tsx`

Replace placeholder price IDs with real ones:
```typescript
const pricing = {
  [PricingTier.General]: {
    monthly: { amount: 1000, priceId: 'price_YOUR_ACTUAL_MONTHLY_PRICE_ID' },
    annual: { amount: 10000, priceId: 'price_YOUR_ACTUAL_ANNUAL_PRICE_ID' },
  },
  [PricingTier.EventOrganizer]: {
    monthly: { amount: 2000, priceId: 'price_YOUR_ACTUAL_MONTHLY_PRICE_ID' },
    annual: { amount: 20000, priceId: 'price_YOUR_ACTUAL_ANNUAL_PRICE_ID' },
  },
};
```

**Note**: For testing, you can use the same prices for both General and EventOrganizer tiers.

---

## Step 5: Webhook Configuration (Optional for Now)

**Note**: Webhooks are not required for basic checkout flow testing. They're needed for:
- Subscription lifecycle events (renewal, cancellation)
- Payment failure handling
- Trial expiration

### To Configure Later:
1. In Stripe Dashboard, go to **Developers** → **Webhooks**
2. Click **+ Add endpoint**
3. **Endpoint URL**: `https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/payments/webhook`
4. **Events to send**:
   - `customer.subscription.created`
   - `customer.subscription.updated`
   - `customer.subscription.deleted`
   - `invoice.payment_succeeded`
   - `invoice.payment_failed`
5. Copy **Signing secret** (starts with `whsec_...`)
6. Add to Azure App Settings as `Stripe__WebhookSecret`

---

## Step 6: Test Locally

### 6.1 Start Local UI
```bash
cd c:\Work\LankaConnect\web
npm run dev
```

**Expected Output**:
```
▲ Next.js 16.0.1
- Local:        http://localhost:3000
- Environments: .env.local

✓ Ready in 2.5s
```

### 6.2 Verify API Connection
1. Open browser to `http://localhost:3000`
2. Open DevTools (F12) → Network tab
3. Login to application
4. Check Network tab for API calls to staging:
   - `https://lankaconnect-api-staging...` ✅

---

## Troubleshooting

### Issue: 500 Error on /api/payments/config

**Cause**: Stripe__SecretKey not configured or invalid

**Solution**:
1. Verify key format starts with `sk_test_`
2. Check Azure App Settings for typos in key name
3. Ensure no extra spaces in key value
4. Restart Container App after changes

### Issue: "Invalid API key provided"

**Cause**: Using wrong key type (e.g., live key in test mode)

**Solution**:
1. Verify you're in Stripe Test Mode
2. Use keys that start with `pk_test_` and `sk_test_`
3. Never use live keys (`pk_live_`, `sk_live_`) for testing

### Issue: "No such price" error during checkout

**Cause**: Price IDs in frontend don't match Stripe Dashboard

**Solution**:
1. Copy exact price IDs from Stripe Dashboard
2. Update SubscriptionUpgradeModal.tsx
3. Rebuild and redeploy frontend
4. Clear browser cache

---

## Security Notes

### ⚠️ IMPORTANT: Never Commit API Keys to Git

**DO**:
- Store keys in Azure App Settings (environment variables)
- Use `.env.local` for local development (gitignored)
- Rotate keys if accidentally exposed

**DON'T**:
- Commit keys to `appsettings.json`
- Share keys in chat/email
- Use live keys for testing

### Current Configuration
- ✅ `appsettings.json` has empty string placeholders
- ✅ Real keys stored in Azure App Settings
- ✅ Keys loaded via environment variables at runtime

---

## Ready for E2E Testing Checklist

Before proceeding to [PHASE_6A4_E2E_TESTING_GUIDE.md](./PHASE_6A4_E2E_TESTING_GUIDE.md):

- [ ] Stripe test mode account created
- [ ] Publishable key (`pk_test_...`) copied
- [ ] Secret key (`sk_test_...`) copied
- [ ] Azure App Settings configured with both keys
- [ ] Container App restarted
- [ ] `/api/payments/config` endpoint returns 200
- [ ] Products/Prices created in Stripe Dashboard
- [ ] Frontend price IDs updated (if using real prices)
- [ ] Local UI started (`npm run dev`)
- [ ] Test user account ready (Event Organizer role)

**When all checked**: Proceed to E2E testing! 🚀

---

## Next Steps

1. ✅ Complete this setup guide
2. ✅ Verify all endpoints return 200
3. 🔄 Follow [PHASE_6A4_E2E_TESTING_GUIDE.md](./PHASE_6A4_E2E_TESTING_GUIDE.md)
4. 🔄 Document test results
5. 🔄 Mark Phase 6A.4 as 100% complete

---

**Document Version**: 1.0
**Last Updated**: 2025-11-26
**Status**: Ready for use
