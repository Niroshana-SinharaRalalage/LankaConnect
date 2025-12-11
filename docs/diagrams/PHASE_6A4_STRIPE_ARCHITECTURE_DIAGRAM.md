# Phase 6A.4: Stripe Payment Service Architecture Diagram

## C4 Model - Component Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          PRESENTATION LAYER (API)                            │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  WebhooksController                                                  │   │
│  │  [Route: /api/webhooks/stripe]                                       │   │
│  │  - HandleStripeWebhook()                                             │   │
│  │  - Validates signature                                               │   │
│  │  - Returns 200 OK to Stripe                                          │   │
│  └────────────────────────────┬────────────────────────────────────────┘   │
└────────────────────────────────┼─────────────────────────────────────────────┘
                                 │
                                 │ IStripePaymentService
                                 │
┌────────────────────────────────▼─────────────────────────────────────────────┐
│                         APPLICATION LAYER                                     │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  IStripePaymentService (Interface)                                     │  │
│  │  - CreateCheckoutSessionAsync()                                        │  │
│  │  - CreateCustomerPortalSessionAsync()                                  │  │
│  │  - CreateOrUpdateCustomerAsync()                                       │  │
│  │  - ProcessWebhookAsync()                                               │  │
│  │  - HandleSubscriptionCreatedAsync()                                    │  │
│  │  - HandleSubscriptionUpdatedAsync()                                    │  │
│  │  - HandleSubscriptionDeletedAsync()                                    │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                               │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  Repository Interfaces                                                 │  │
│  │  - IStripeCustomerRepository                                           │  │
│  │  - IStripeWebhookEventRepository                                       │  │
│  │  - IUserRepository                                                     │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────────────────┘
                                 │
                                 │ implements
                                 │
┌────────────────────────────────▼─────────────────────────────────────────────┐
│                        INFRASTRUCTURE LAYER                                   │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  StripePaymentService (Implementation)                                 │  │
│  │  ┌─────────────────────────────────────────────────────────────────┐  │  │
│  │  │  Dependencies:                                                   │  │  │
│  │  │  - IStripeClient (Stripe.net SDK)                               │  │  │
│  │  │  - IStripeCustomerRepository                                    │  │  │
│  │  │  - IStripeWebhookEventRepository                                │  │  │
│  │  │  - IUserRepository                                              │  │  │
│  │  │  - IUnitOfWork                                                  │  │  │
│  │  │  - ILogger<StripePaymentService>                                │  │  │
│  │  │  - StripeOptions (configuration)                                │  │  │
│  │  └─────────────────────────────────────────────────────────────────┘  │  │
│  │                                                                           │  │
│  │  Methods:                                                                 │  │
│  │  ┌─────────────────────────────────────────────────────────────────┐  │  │
│  │  │  CreateCheckoutSessionAsync()                                    │  │  │
│  │  │  ├─> GetPriceIdForRole(role)                                     │  │  │
│  │  │  ├─> CreateOrUpdateCustomerAsync()                               │  │  │
│  │  │  ├─> _stripeClient.Checkout.Sessions.CreateAsync()              │  │  │
│  │  │  └─> return Result<string>                                       │  │  │
│  │  └─────────────────────────────────────────────────────────────────┘  │  │
│  │                                                                           │  │
│  │  ┌─────────────────────────────────────────────────────────────────┐  │  │
│  │  │  ProcessWebhookAsync(payload, signature)                         │  │  │
│  │  │  ├─> EventUtility.ConstructEvent() [validate signature]          │  │  │
│  │  │  ├─> Check idempotency (_webhookRepository)                      │  │  │
│  │  │  ├─> Route to handler by event type                              │  │  │
│  │  │  │   ├─> customer.subscription.created                           │  │  │
│  │  │  │   ├─> customer.subscription.updated                           │  │  │
│  │  │  │   ├─> customer.subscription.deleted                           │  │  │
│  │  │  │   ├─> invoice.payment_succeeded                               │  │  │
│  │  │  │   └─> invoice.payment_failed                                  │  │  │
│  │  │  └─> Mark as processed                                           │  │  │
│  │  └─────────────────────────────────────────────────────────────────┘  │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                               │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  StripeCustomerRepository                                              │  │
│  │  extends Repository<StripeCustomer>                                    │  │
│  │  - GetByUserIdAsync()                                                  │  │
│  │  - GetByStripeCustomerIdAsync()                                        │  │
│  │  - ExistsByUserIdAsync()                                               │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                               │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  StripeWebhookEventRepository                                          │  │
│  │  extends Repository<StripeWebhookEvent>                                │  │
│  │  - GetByEventIdAsync()                                                 │  │
│  │  - IsEventProcessedAsync()                                             │  │
│  │  - GetUnprocessedEventsAsync()                                         │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
└───────────────────────────────┬───────────────────────────────────────────────┘
                                │
                                │ calls
                                │
┌────────────────────────────────▼─────────────────────────────────────────────┐
│                           DOMAIN LAYER                                        │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  User (Aggregate Root)                                                 │  │
│  │  Properties:                                                           │  │
│  │  - StripeCustomerId: string?                                           │  │
│  │  - StripeSubscriptionId: string?                                       │  │
│  │  - SubscriptionStatus: SubscriptionStatus                              │  │
│  │  - SubscriptionStartDate: DateTime?                                    │  │
│  │  - SubscriptionEndDate: DateTime?                                      │  │
│  │  - TrialEndDate: DateTime?                                             │  │
│  │                                                                           │  │
│  │  Methods:                                                              │  │
│  │  - SetStripeCustomerId(string customerId)                              │  │
│  │  - ActivateSubscription(subscriptionId, startDate, trialEndDate)       │  │
│  │  - UpdateSubscriptionStatus(SubscriptionStatus status)                 │  │
│  │  - CancelSubscription(endDate)                                         │  │
│  │                                                                           │  │
│  │  Domain Events:                                                        │  │
│  │  - UserSubscriptionActivatedEvent                                      │  │
│  │  - UserSubscriptionStatusChangedEvent                                  │  │
│  │  - UserSubscriptionCanceledEvent                                       │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                               │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  SubscriptionStatus (Enum)                                             │  │
│  │  - None                                                                │  │
│  │  - Trialing                                                            │  │
│  │  - Active                                                              │  │
│  │  - PastDue                                                             │  │
│  │  - Canceled                                                            │  │
│  │  - Unpaid                                                              │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────────────────┘
                                 │
                                 │ persists to
                                 │
┌────────────────────────────────▼─────────────────────────────────────────────┐
│                        INFRASTRUCTURE ENTITIES                                │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  StripeCustomer (Infrastructure Entity)                                │  │
│  │  - Id: Guid                                                            │  │
│  │  - UserId: Guid                                                        │  │
│  │  - StripeCustomerId: string                                            │  │
│  │  - Email: string                                                       │  │
│  │  - Name: string                                                        │  │
│  │  - StripeCreatedAt: DateTime                                           │  │
│  │  Table: stripe_customers                                               │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                               │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  StripeWebhookEvent (Infrastructure Entity)                            │  │
│  │  - Id: Guid                                                            │  │
│  │  - EventId: string (evt_xxx)                                           │  │
│  │  - EventType: string                                                   │  │
│  │  - Processed: bool                                                     │  │
│  │  - ProcessedAt: DateTime?                                              │  │
│  │  - ErrorMessage: string?                                               │  │
│  │  - AttemptCount: int                                                   │  │
│  │  Table: stripe_webhook_events                                          │  │
│  │  Purpose: Idempotency + retry tracking                                 │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────────────────┘
                                 │
                                 │ stores in
                                 │
┌────────────────────────────────▼─────────────────────────────────────────────┐
│                          DATABASE (PostgreSQL)                                │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  users table                                                           │  │
│  │  - stripe_customer_id (varchar)                                        │  │
│  │  - stripe_subscription_id (varchar)                                    │  │
│  │  - subscription_status (int)                                           │  │
│  │  - subscription_start_date (timestamp)                                 │  │
│  │  - subscription_end_date (timestamp)                                   │  │
│  │  - trial_end_date (timestamp)                                          │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                               │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  stripe_customers table                                                │  │
│  │  - id (uuid, PK)                                                       │  │
│  │  - user_id (uuid, FK → users.id)                                      │  │
│  │  - stripe_customer_id (varchar, UNIQUE)                                │  │
│  │  - email (varchar)                                                     │  │
│  │  - name (varchar)                                                      │  │
│  │  - stripe_created_at (timestamp)                                       │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                               │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  stripe_webhook_events table                                           │  │
│  │  - id (uuid, PK)                                                       │  │
│  │  - event_id (varchar, UNIQUE)                                          │  │
│  │  - event_type (varchar)                                                │  │
│  │  - processed (boolean)                                                 │  │
│  │  - processed_at (timestamp)                                            │  │
│  │  - error_message (text)                                                │  │
│  │  - attempt_count (int)                                                 │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────────────────┘
                                 │
                                 │ external API
                                 │
┌────────────────────────────────▼─────────────────────────────────────────────┐
│                         EXTERNAL SERVICES                                     │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  Stripe API                                                            │  │
│  │  ┌─────────────────────────────────────────────────────────────────┐  │  │
│  │  │  Stripe.net SDK (IStripeClient)                                  │  │  │
│  │  │  - Customers.CreateAsync()                                       │  │  │
│  │  │  - Checkout.Sessions.CreateAsync()                               │  │  │
│  │  │  - BillingPortal.Sessions.CreateAsync()                          │  │  │
│  │  │  - Subscriptions.UpdateAsync()                                   │  │  │
│  │  └─────────────────────────────────────────────────────────────────┘  │  │
│  │                                                                           │  │
│  │  Webhook Endpoints:                                                       │  │
│  │  - POST /api/webhooks/stripe                                             │  │
│  │  - Signature: Stripe-Signature header                                    │  │
│  │  - Events: subscription.created, subscription.updated, etc.              │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────────────────┘
```

---

## Data Flow Diagrams

### 1. User Subscribes to Event Organizer Plan

```
┌──────────┐     (1) Click Subscribe      ┌─────────────────────┐
│  User    │ ────────────────────────────> │  Frontend (React)   │
│ (Browser)│                                └──────────┬──────────┘
└──────────┘                                           │
                                                       │ (2) POST /api/payment/checkout
                                                       │
                                                       ▼
                                             ┌─────────────────────┐
                                             │  API Controller     │
                                             └──────────┬──────────┘
                                                        │
                                      (3) CreateCheckoutSessionAsync(userId, role)
                                                        │
                                                        ▼
                                             ┌─────────────────────┐
                                             │ StripePaymentService│
                                             └──────────┬──────────┘
                                                        │
                          ┌─────────────────────────────┼─────────────────────────────┐
                          │                             │                             │
                    (4) Check if                  (5) Get Price ID          (6) Create/Update
                customer exists                   for role                    Stripe Customer
                          │                             │                             │
                          ▼                             ▼                             ▼
                ┌─────────────────────┐      ┌─────────────────┐         ┌──────────────────┐
                │StripeCustomerRepo   │      │ StripeOptions   │         │ Stripe API       │
                └─────────────────────┘      └─────────────────┘         │ Customers.Create │
                                                                          └──────────────────┘
                                                        │
                                      (7) Create Checkout Session
                                                        │
                                                        ▼
                                             ┌─────────────────────┐
                                             │  Stripe API         │
                                             │  Checkout.Sessions  │
                                             │  .CreateAsync()     │
                                             └──────────┬──────────┘
                                                        │
                                        (8) Return checkout URL
                                                        │
                                                        ▼
┌──────────┐     (9) Redirect to Stripe     ┌─────────────────────┐
│  User    │ <──────────────────────────────│  Frontend (React)   │
│ (Browser)│                                 └─────────────────────┘
└────┬─────┘
     │
     │ (10) Enter payment details on Stripe
     │
     ▼
┌──────────────────┐
│ Stripe Checkout  │
│ (Hosted Page)    │
└────┬─────────────┘
     │
     │ (11) Payment successful
     │
     │ (12) Stripe sends webhook: customer.subscription.created
     │
     ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          WEBHOOK PROCESSING                                  │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │  (13) POST /api/webhooks/stripe                                         │ │
│  │       Headers: Stripe-Signature                                         │ │
│  │       Body: Event JSON                                                  │ │
│  └────────────────────────┬───────────────────────────────────────────────┘ │
│                           │                                                  │
│                           │ (14) Validate signature                          │
│                           │                                                  │
│                           ▼                                                  │
│                ┌─────────────────────┐                                      │
│                │ EventUtility        │                                      │
│                │ .ConstructEvent()   │                                      │
│                └──────────┬──────────┘                                      │
│                           │                                                  │
│                           │ (15) Check idempotency                           │
│                           │                                                  │
│                           ▼                                                  │
│                ┌─────────────────────┐                                      │
│                │StripeWebhookEventRepo│                                     │
│                │.GetByEventIdAsync() │                                      │
│                └──────────┬──────────┘                                      │
│                           │                                                  │
│                           │ (16) Process subscription.created                │
│                           │                                                  │
│                           ▼                                                  │
│                ┌─────────────────────┐                                      │
│                │HandleSubscription   │                                      │
│                │CreatedAsync()       │                                      │
│                └──────────┬──────────┘                                      │
│                           │                                                  │
│          ┌────────────────┼────────────────┐                                │
│          │                │                │                                 │
│   (17) Get User    (18) Update User   (19) Raise Domain Event              │
│          │                │                │                                 │
│          ▼                ▼                ▼                                 │
│   ┌────────────┐  ┌────────────┐  ┌────────────────────────┐              │
│   │UserRepo    │  │User.Activate│  │UserSubscriptionActivated│             │
│   │.GetByIdAsync│ │Subscription()│ │Event                    │             │
│   └────────────┘  └────────────┘  └────────────────────────┘              │
│                           │                                                  │
│                           │ (20) Save to database                            │
│                           │                                                  │
│                           ▼                                                  │
│                ┌─────────────────────┐                                      │
│                │UnitOfWork           │                                      │
│                │.SaveChangesAsync()  │                                      │
│                └─────────────────────┘                                      │
└─────────────────────────────────────────────────────────────────────────────┘
                           │
                           │ (21) Return 200 OK to Stripe
                           │
                           ▼
                    ┌─────────────┐
                    │ Stripe      │
                    │ (confirmed) │
                    └─────────────┘
```

---

### 2. User Manages Subscription (Customer Portal)

```
┌──────────┐     (1) Click Manage Subscription     ┌─────────────────────┐
│  User    │ ───────────────────────────────────> │  Frontend (React)   │
│ (Browser)│                                        └──────────┬──────────┘
└──────────┘                                                   │
                                                               │ (2) POST /api/payment/portal
                                                               │
                                                               ▼
                                                     ┌─────────────────────┐
                                                     │  API Controller     │
                                                     └──────────┬──────────┘
                                                                │
                                          (3) CreateCustomerPortalSessionAsync(userId)
                                                                │
                                                                ▼
                                                     ┌─────────────────────┐
                                                     │ StripePaymentService│
                                                     └──────────┬──────────┘
                                                                │
                                      (4) Get StripeCustomerId from database
                                                                │
                                                                ▼
                                                     ┌─────────────────────┐
                                                     │ StripeCustomerRepo  │
                                                     │ .GetByUserIdAsync() │
                                                     └──────────┬──────────┘
                                                                │
                                      (5) Create Portal Session
                                                                │
                                                                ▼
                                                     ┌─────────────────────┐
                                                     │  Stripe API         │
                                                     │  BillingPortal      │
                                                     │  .Sessions.Create() │
                                                     └──────────┬──────────┘
                                                                │
                                            (6) Return portal URL
                                                                │
                                                                ▼
┌──────────┐     (7) Redirect to Stripe Portal     ┌─────────────────────┐
│  User    │ <──────────────────────────────────────│  Frontend (React)   │
│ (Browser)│                                         └─────────────────────┘
└────┬─────┘
     │
     │ (8) Update payment method / Cancel subscription
     │
     ▼
┌──────────────────┐
│ Stripe Portal    │
│ (Hosted Page)    │
└────┬─────────────┘
     │
     │ (9) Changes applied, webhook sent
     │
     ▼
┌─────────────────────┐
│ Webhook Processing  │
│ (same as diagram 1) │
└─────────────────────┘
```

---

## Security Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          SECURITY LAYERS                                     │
│                                                                               │
│  Layer 1: HTTPS/TLS                                                          │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  All API communication encrypted with TLS 1.2+                         │  │
│  │  Certificate: Azure App Service managed certificate                    │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                               │
│  Layer 2: Webhook Signature Validation (CRITICAL)                            │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  EventUtility.ConstructEvent(payload, signature, webhookSecret)        │  │
│  │  - Validates HMAC SHA-256 signature                                    │  │
│  │  - Prevents replay attacks (timestamp check)                           │  │
│  │  - Rejects invalid signatures (returns 400)                            │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                               │
│  Layer 3: Idempotency Check                                                  │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  StripeWebhookEvent table                                              │  │
│  │  - Unique constraint on event_id                                       │  │
│  │  - Prevents duplicate processing if Stripe retries                     │  │
│  │  - Tracks attempt count and errors                                     │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                               │
│  Layer 4: Secret Management                                                  │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  Development: appsettings.Development.json (sk_test_...)               │  │
│  │  Staging: Azure Key Vault (sk_test_...)                                │  │
│  │  Production: Azure Key Vault (sk_live_...)                             │  │
│  │  Environment variables: Stripe__SecretKey, Stripe__WebhookSecret       │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                               │
│  Layer 5: PCI Compliance (Data Handling)                                     │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  ✅ Use Stripe Checkout (hosted payment page)                          │  │
│  │  ✅ Store only: StripeCustomerId, StripeSubscriptionId                 │  │
│  │  ❌ NEVER store: card numbers, CVV, expiration dates                   │  │
│  │  ✅ Stripe handles all sensitive payment data                          │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                               │
│  Layer 6: Database Transaction Boundaries                                    │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  Webhook processing wrapped in database transaction                    │  │
│  │  - Rollback on failure (maintains consistency)                         │  │
│  │  - Retry logic for transient failures                                  │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                               │
│  Layer 7: Audit Logging                                                      │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  Serilog structured logging                                            │  │
│  │  - All Stripe API calls logged (request/response)                      │  │
│  │  - All webhook events logged                                           │  │
│  │  - All errors logged with correlation IDs                              │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────────────────┘
```

---

## Technology Stack

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         TECHNOLOGY STACK                                     │
│                                                                               │
│  Backend (.NET 8)                                                            │
│  ├─ ASP.NET Core Web API                                                     │
│  ├─ Entity Framework Core 8.0                                                │
│  ├─ PostgreSQL 15+ (Azure Database for PostgreSQL)                           │
│  ├─ Stripe.net SDK 45.x                                                      │
│  ├─ Serilog (structured logging)                                             │
│  ├─ MediatR (CQRS pattern)                                                   │
│  └─ FluentValidation (request validation)                                    │
│                                                                               │
│  Payment Integration                                                          │
│  ├─ Stripe Checkout Sessions (subscription creation)                         │
│  ├─ Stripe Customer Portal (subscription management)                         │
│  ├─ Stripe Webhooks (subscription lifecycle events)                          │
│  └─ Stripe Test Mode (development/staging)                                   │
│                                                                               │
│  Infrastructure                                                               │
│  ├─ Azure App Service (API hosting)                                          │
│  ├─ Azure Key Vault (secret management)                                      │
│  ├─ Azure Static Web Apps (frontend hosting)                                 │
│  └─ Azure Database for PostgreSQL (database)                                 │
│                                                                               │
│  Testing                                                                      │
│  ├─ xUnit (unit testing)                                                     │
│  ├─ Moq (mocking)                                                            │
│  ├─ FluentAssertions (assertions)                                            │
│  └─ Stripe CLI (webhook testing)                                             │
└───────────────────────────────────────────────────────────────────────────────┘
```

