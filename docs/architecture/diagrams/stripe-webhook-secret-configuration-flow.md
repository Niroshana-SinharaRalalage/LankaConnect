# Stripe Webhook Secret Configuration Flow - Visual Diagrams

## Current State (Broken) - HTTP 400 Invalid Signature

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        STRIPE WEBHOOK REQUEST                           │
│  POST /api/payments/webhook                                             │
│  Headers: Stripe-Signature: t=1234,v1=abc123...                        │
│  Body: { "id": "evt_123", "type": "checkout.session.completed" }       │
└─────────────────────────────────────────────────────────────────────────┘
                                    ↓
┌─────────────────────────────────────────────────────────────────────────┐
│              AZURE CONTAINER APPS INGRESS (HTTPS)                       │
│  FQDN: lankaconnect-api-staging.azurewebsites.net                       │
│  Port: 5000                                                             │
└─────────────────────────────────────────────────────────────────────────┘
                                    ↓
┌─────────────────────────────────────────────────────────────────────────┐
│                   ASP.NET CORE MIDDLEWARE PIPELINE                      │
│  1. CORS                                                                │
│  2. Authentication                                                      │
│  3. Routing → PaymentsController.Webhook()                             │
└─────────────────────────────────────────────────────────────────────────┘
                                    ↓
┌─────────────────────────────────────────────────────────────────────────┐
│              PAYMENTSCONTROLLER.WEBHOOK() [Line 225]                    │
│  var json = await new StreamReader(Request.Body).ReadToEndAsync();     │
│  var signature = Request.Headers["Stripe-Signature"].ToString();       │
│                                                                         │
│  // Inject StripeOptions via IOptions<StripeOptions>                   │
│  private readonly StripeOptions _stripeOptions; // ⚠️ WebhookSecret=""  │
└─────────────────────────────────────────────────────────────────────────┘
                                    ↓
┌─────────────────────────────────────────────────────────────────────────┐
│           EVENTUTILITY.CONSTRUCTEVENT() [Line 232-235]                  │
│  EventUtility.ConstructEvent(                                           │
│    json,                                                                │
│    signature,                                                           │
│    _stripeOptions.WebhookSecret  // ❌ Value = "" (EMPTY STRING)        │
│  );                                                                     │
└─────────────────────────────────────────────────────────────────────────┘
                                    ↓
┌─────────────────────────────────────────────────────────────────────────┐
│                   SIGNATURE VERIFICATION FAILURE                        │
│  Expected: whsec_vjs9C1dm0onSqZ5aDv16g3NngChfHnIX                       │
│  Actual:   "" (empty string)                                            │
│  Result:   throw StripeException("Invalid signature")                  │
└─────────────────────────────────────────────────────────────────────────┘
                                    ↓
┌─────────────────────────────────────────────────────────────────────────┐
│                    HTTP 400 BAD REQUEST RESPONSE                        │
│  { "Error": "Invalid signature" }                                      │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Configuration Chain (Current - Broken)

```
┌──────────────────────────────────────────────────────────────────────────┐
│                        AZURE KEY VAULT                                   │
│  Vault: lankaconnect-staging-kv                                          │
│  Secret Name: stripe-webhook-secret                                      │
│  Secret Value: whsec_vjs9C1dm0onSqZ5aDv16g3NngChfHnIX ✅ CORRECT         │
└──────────────────────────────────────────────────────────────────────────┘
                                    ↓
                          ❌ NOT REFERENCED IN DEPLOYMENT
                                    ↓
┌──────────────────────────────────────────────────────────────────────────┐
│                   CONTAINER APP ENVIRONMENT VARIABLES                    │
│  ✅ Stripe__SecretKey=secretref:stripe-secret-key                        │
│  ✅ Stripe__PublishableKey=secretref:stripe-publishable-key              │
│  ❌ Stripe__WebhookSecret=??? NOT SET                                    │
└──────────────────────────────────────────────────────────────────────────┘
                                    ↓
                     ❌ NO ENVIRONMENT VARIABLE TO BIND
                                    ↓
┌──────────────────────────────────────────────────────────────────────────┐
│                   APPSETTINGS.STAGING.JSON                               │
│  "Stripe": {                                                             │
│    "SecretKey": "${STRIPE_SECRET_KEY}",              ✅                  │
│    "PublishableKey": "${STRIPE_PUBLISHABLE_KEY}"     ✅                  │
│    // ❌ MISSING: "WebhookSecret": "${STRIPE_WEBHOOK_SECRET}"            │
│  }                                                                       │
└──────────────────────────────────────────────────────────────────────────┘
                                    ↓
                       ❌ PROPERTY NOT IN SCHEMA
                                    ↓
┌──────────────────────────────────────────────────────────────────────────┐
│          CONFIGURATION BINDING (DependencyInjection.cs:252)              │
│  services.Configure<StripeOptions>(                                      │
│    configuration.GetSection("Stripe")                                    │
│  );                                                                      │
│  Result: Only binds SecretKey + PublishableKey                           │
│          WebhookSecret = "" (default)                                    │
└──────────────────────────────────────────────────────────────────────────┘
                                    ↓
┌──────────────────────────────────────────────────────────────────────────┐
│                        STRIPEOPTIONS INSTANCE                            │
│  public class StripeOptions {                                            │
│    public string SecretKey { get; set; } = "";        ✅ Populated       │
│    public string PublishableKey { get; set; } = "";   ✅ Populated       │
│    public string WebhookSecret { get; set; } = "";    ❌ EMPTY           │
│  }                                                                       │
└──────────────────────────────────────────────────────────────────────────┘
                                    ↓
┌──────────────────────────────────────────────────────────────────────────┐
│                    PAYMENTSCONTROLLER (Injected)                         │
│  IOptions<StripeOptions> _stripeOptions                                  │
│  _stripeOptions.WebhookSecret → "" ❌                                    │
└──────────────────────────────────────────────────────────────────────────┘
```

---

## Fixed Configuration Chain

```
┌──────────────────────────────────────────────────────────────────────────┐
│                        AZURE KEY VAULT                                   │
│  Vault: lankaconnect-staging-kv                                          │
│  Secret Name: stripe-webhook-secret                                      │
│  Secret Value: whsec_vjs9C1dm0onSqZ5aDv16g3NngChfHnIX ✅                 │
└──────────────────────────────────────────────────────────────────────────┘
                                    ↓
                    ✅ REFERENCED VIA SECRETREF IN DEPLOYMENT
                                    ↓
┌──────────────────────────────────────────────────────────────────────────┐
│                   CONTAINER APP ENVIRONMENT VARIABLES                    │
│  ✅ Stripe__SecretKey=secretref:stripe-secret-key                        │
│  ✅ Stripe__PublishableKey=secretref:stripe-publishable-key              │
│  ✅ Stripe__WebhookSecret=secretref:stripe-webhook-secret  [FIX ADDED]   │
└──────────────────────────────────────────────────────────────────────────┘
                                    ↓
               ✅ AZURE RESOLVES SECRETREF FROM KEY VAULT
                                    ↓
┌──────────────────────────────────────────────────────────────────────────┐
│              RESOLVED ENVIRONMENT VARIABLES (Runtime)                    │
│  Stripe__SecretKey=sk_test_...                        ✅                 │
│  Stripe__PublishableKey=pk_test_...                   ✅                 │
│  Stripe__WebhookSecret=whsec_vjs9C1dm0onSqZ5aDv16g3NngChfHnIX  ✅        │
└──────────────────────────────────────────────────────────────────────────┘
                                    ↓
                  ✅ ENVIRONMENT VARIABLE AVAILABLE FOR BINDING
                                    ↓
┌──────────────────────────────────────────────────────────────────────────┐
│                   APPSETTINGS.STAGING.JSON [FIXED]                       │
│  "Stripe": {                                                             │
│    "SecretKey": "${STRIPE_SECRET_KEY}",              ✅                  │
│    "PublishableKey": "${STRIPE_PUBLISHABLE_KEY}",    ✅                  │
│    "WebhookSecret": "${STRIPE_WEBHOOK_SECRET}"       ✅ [FIX ADDED]      │
│  }                                                                       │
└──────────────────────────────────────────────────────────────────────────┘
                                    ↓
                       ✅ PROPERTY EXISTS IN SCHEMA
                                    ↓
┌──────────────────────────────────────────────────────────────────────────┐
│          CONFIGURATION BINDING (DependencyInjection.cs:252)              │
│  services.Configure<StripeOptions>(                                      │
│    configuration.GetSection("Stripe")                                    │
│  );                                                                      │
│  Result: Binds ALL properties including WebhookSecret ✅                 │
└──────────────────────────────────────────────────────────────────────────┘
                                    ↓
┌──────────────────────────────────────────────────────────────────────────┐
│                  STRIPEOPTIONS INSTANCE [FIXED]                          │
│  public class StripeOptions {                                            │
│    public string SecretKey { get; set; }          ✅ = "sk_test_..."     │
│    public string PublishableKey { get; set; }     ✅ = "pk_test_..."     │
│    public string WebhookSecret { get; set; }      ✅ = "whsec_vjs9..."   │
│  }                                                                       │
└──────────────────────────────────────────────────────────────────────────┘
                                    ↓
┌──────────────────────────────────────────────────────────────────────────┐
│                    PAYMENTSCONTROLLER (Injected)                         │
│  IOptions<StripeOptions> _stripeOptions                                  │
│  _stripeOptions.WebhookSecret → "whsec_vjs9C1dm..." ✅ CORRECT           │
└──────────────────────────────────────────────────────────────────────────┘
                                    ↓
┌──────────────────────────────────────────────────────────────────────────┐
│           EVENTUTILITY.CONSTRUCTEVENT() [Now Works]                      │
│  EventUtility.ConstructEvent(                                            │
│    json,                                                                 │
│    signature: "t=1234,v1=abc123...",                                     │
│    webhookSecret: "whsec_vjs9C1dm0onSqZ5aDv16g3NngChfHnIX"  ✅           │
│  );                                                                      │
│  → Signature verification SUCCESS ✅                                     │
└──────────────────────────────────────────────────────────────────────────┘
                                    ↓
┌──────────────────────────────────────────────────────────────────────────┐
│                    HTTP 200 OK RESPONSE ✅                               │
│  Webhook processed successfully                                          │
│  Event recorded in database                                              │
│  Checkout session completed → Signup confirmed                           │
└──────────────────────────────────────────────────────────────────────────┘
```

---

## Configuration Hierarchy Layers

```
Layer 1: appsettings.json (Base Schema + Defaults)
┌──────────────────────────────────────────────────────────┐
│ "Stripe": {                                              │
│   "SecretKey": "",                                       │
│   "PublishableKey": "",                                  │
│   "WebhookSecret": ""  ← Schema definition required      │
│ }                                                        │
└──────────────────────────────────────────────────────────┘
                       ↓ (Overridden by)
Layer 2: appsettings.{Environment}.json (Environment Overrides)
┌──────────────────────────────────────────────────────────┐
│ "Stripe": {                                              │
│   "SecretKey": "${STRIPE_SECRET_KEY}",                   │
│   "PublishableKey": "${STRIPE_PUBLISHABLE_KEY}",         │
│   "WebhookSecret": "${STRIPE_WEBHOOK_SECRET}"            │
│ }                                                        │
└──────────────────────────────────────────────────────────┘
                       ↓ (Overridden by)
Layer 3: Environment Variables (Runtime Values)
┌──────────────────────────────────────────────────────────┐
│ Stripe__SecretKey=secretref:stripe-secret-key            │
│ Stripe__PublishableKey=secretref:stripe-publishable-key  │
│ Stripe__WebhookSecret=secretref:stripe-webhook-secret    │
└──────────────────────────────────────────────────────────┘
                       ↓ (Resolved by)
Layer 4: Azure Key Vault (via secretref resolution)
┌──────────────────────────────────────────────────────────┐
│ stripe-secret-key: sk_test_...                           │
│ stripe-publishable-key: pk_test_...                      │
│ stripe-webhook-secret: whsec_vjs9C1dm...                 │
└──────────────────────────────────────────────────────────┘
                       ↓ (Bound to)
Layer 5: IOptions<StripeOptions> (Dependency Injection)
┌──────────────────────────────────────────────────────────┐
│ StripeOptions {                                          │
│   SecretKey = "sk_test_...",                             │
│   PublishableKey = "pk_test_...",                        │
│   WebhookSecret = "whsec_vjs9C1dm..."                    │
│ }                                                        │
└──────────────────────────────────────────────────────────┘
```

---

## Why Container Restart Didn't Fix It

```
Container Restart Cycle
┌────────────────────────────────────────────────────────────┐
│ 1. Stop Container                                          │
│    ↓                                                       │
│ 2. Azure Container Apps reads environment variables        │
│    from current revision configuration                     │
│    ↓                                                       │
│ 3. Resolve secretref values from Key Vault                 │
│    ✅ stripe-webhook-secret → whsec_vjs9C1dm... (SUCCESS)  │
│    ↓                                                       │
│ 4. Start Container with environment variables              │
│    ✅ Stripe__SecretKey=sk_test_...                        │
│    ✅ Stripe__PublishableKey=pk_test_...                   │
│    ❌ Stripe__WebhookSecret=NOT SET (missing from config)  │
│    ↓                                                       │
│ 5. ASP.NET Core reads appsettings.Staging.json             │
│    ❌ No WebhookSecret property in Stripe section          │
│    ↓                                                       │
│ 6. Configuration binding populates StripeOptions           │
│    ❌ WebhookSecret = "" (default, not bound)              │
│    ↓                                                       │
│ 7. Container starts with SAME broken configuration         │
│    ❌ Still fails with HTTP 400 "Invalid signature"        │
└────────────────────────────────────────────────────────────┘

ROOT CAUSE: The environment variable was never set in the
deployment workflow, so restart just reloaded the same
incomplete configuration every time.
```

---

## Deployment Workflow Fix (Before vs After)

### BEFORE (Broken)
```yaml
# .github/workflows/deploy-staging.yml (lines 129-155)
az containerapp update \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --image ${{ env.AZURE_CONTAINER_REGISTRY }}.azurecr.io/lankaconnect-api:${{ github.sha }} \
  --replace-env-vars \
    ASPNETCORE_ENVIRONMENT=Staging \
    ConnectionStrings__DefaultConnection=secretref:database-connection-string \
    Jwt__Key=secretref:jwt-secret-key \
    Stripe__SecretKey=secretref:stripe-secret-key \
    Stripe__PublishableKey=secretref:stripe-publishable-key
    # ❌ MISSING: Stripe__WebhookSecret
```

### AFTER (Fixed)
```yaml
# .github/workflows/deploy-staging.yml (lines 129-156)
az containerapp update \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --image ${{ env.AZURE_CONTAINER_REGISTRY }}.azurecr.io/lankaconnect-api:${{ github.sha }} \
  --replace-env-vars \
    ASPNETCORE_ENVIRONMENT=Staging \
    ConnectionStrings__DefaultConnection=secretref:database-connection-string \
    Jwt__Key=secretref:jwt-secret-key \
    Stripe__SecretKey=secretref:stripe-secret-key \
    Stripe__PublishableKey=secretref:stripe-publishable-key \
    Stripe__WebhookSecret=secretref:stripe-webhook-secret  # ✅ ADDED
```

---

## Secret Rotation Flow (Post-Fix)

```
┌─────────────────────────────────────────────────────────────┐
│ Step 1: Generate New Webhook Secret in Stripe Dashboard    │
│   Old: whsec_vjs9C1dm0onSqZ5aDv16g3NngChfHnIX               │
│   New: whsec_ABC123newSecretXYZ789                          │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│ Step 2: Update Azure Key Vault Secret                      │
│   az keyvault secret set \                                  │
│     --vault-name lankaconnect-staging-kv \                  │
│     --name stripe-webhook-secret \                          │
│     --value "whsec_ABC123newSecretXYZ789"                   │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│ Step 3: Restart Container App                              │
│   az containerapp revision restart \                        │
│     --name lankaconnect-api-staging \                       │
│     --resource-group lankaconnect-staging                   │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│ Step 4: Azure Re-resolves secretref from Key Vault         │
│   Stripe__WebhookSecret=secretref:stripe-webhook-secret     │
│   → Fetches latest value from Key Vault                    │
│   → whsec_ABC123newSecretXYZ789 ✅                          │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│ Step 5: Configuration Binding Loads New Secret             │
│   StripeOptions.WebhookSecret = "whsec_ABC123..." ✅        │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│ Step 6: Webhook Signature Verification Uses New Secret     │
│   EventUtility.ConstructEvent(json, sig, newSecret) ✅      │
└─────────────────────────────────────────────────────────────┘

NO CODE CHANGES REQUIRED - Just update Key Vault + restart!
```

---

## Testing Workflow

```
┌───────────────────────────────────────────────────────────────┐
│ 1. Install Stripe CLI                                        │
│    https://stripe.com/docs/stripe-cli                        │
└───────────────────────────────────────────────────────────────┘
                           ↓
┌───────────────────────────────────────────────────────────────┐
│ 2. Authenticate with Stripe                                  │
│    stripe login                                              │
└───────────────────────────────────────────────────────────────┘
                           ↓
┌───────────────────────────────────────────────────────────────┐
│ 3. Start webhook listener (forwards to staging)             │
│    stripe listen --forward-to \                              │
│      https://lankaconnect-api-staging.azurewebsites.net\     │
│      /api/payments/webhook                                   │
│                                                              │
│    Output:                                                   │
│    > Ready! Forward webhook events to your local server      │
│    > Webhook signing secret: whsec_vjs9C1dm...               │
└───────────────────────────────────────────────────────────────┘
                           ↓
┌───────────────────────────────────────────────────────────────┐
│ 4. Trigger test event (separate terminal)                   │
│    stripe trigger checkout.session.completed                │
│                                                              │
│    Output:                                                   │
│    > Triggering checkout.session.completed event             │
│    > Event ID: evt_test_123ABC                               │
└───────────────────────────────────────────────────────────────┘
                           ↓
┌───────────────────────────────────────────────────────────────┐
│ 5. Observe webhook listener output                          │
│    BEFORE FIX:                                               │
│    ❌ --> POST /api/payments/webhook [400 BAD REQUEST]       │
│                                                              │
│    AFTER FIX:                                                │
│    ✅ --> POST /api/payments/webhook [200 OK]                │
└───────────────────────────────────────────────────────────────┘
                           ↓
┌───────────────────────────────────────────────────────────────┐
│ 6. Check container logs                                     │
│    az containerapp logs show \                               │
│      --name lankaconnect-api-staging \                       │
│      --resource-group lankaconnect-staging \                 │
│      --tail 50 --filter "Processing webhook event"          │
│                                                              │
│    Expected Output:                                          │
│    ✅ Processing webhook event evt_test_123ABC of type       │
│       checkout.session.completed                             │
└───────────────────────────────────────────────────────────────┘
```
