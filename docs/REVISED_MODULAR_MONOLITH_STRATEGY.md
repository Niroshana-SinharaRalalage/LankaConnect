# LankaConnect: Modular Monolith Strategy (Revised)

## 🎯 Executive Summary

**Decision:** Build as **Modular Monolith First**, extract to **Microservices Later**

This strategy provides:
- ✅ **Fast time to production:** 3-4 weeks (vs 6-9 weeks for microservices)
- ✅ **Lower cost:** ~$78-110/month (vs $156-225 for microservices)
- ✅ **Easier for small team:** 2-3 developers won't feel distributed systems overhead
- ✅ **Future reusability:** Clean module boundaries make extraction easy when needed
- ✅ **Prove features first:** Validate marketplace/forum/business profile work before architectural overhead

---

## 📊 Context: Why This Approach?

### User Requirements (Clarified)
- **Primary driver:** Reusability in 2+ other applications (concrete business plans for Marketplace reuse)
- **Timeline:** 4-6 weeks to production is acceptable
- **Team size:** 2-3 developers (small team)
- **Goal:** Clean separation + future extraction capability

### System Architect's Valid Concerns
- Microservices add 100% cost increase ($78 → $156/month)
- Small teams (2-3 devs) struggle with microservices coordination overhead
- No immediate scaling/performance problems to solve
- Shared database = "distributed monolith" (not true microservices benefits)
- Martin Fowler: "Start with monolith, extract to microservices when proven necessary"

### The Solution: Best of Both Worlds
- Build with **modular boundaries** (easy to extract later)
- Deploy as **monolith initially** (speed + simplicity + cost)
- Extract to **containers when needed** (when reusing in other apps, or team grows)

---

## 🏗️ Architecture: Modular Monolith with Clean Boundaries

### Repository Structure (Single Repo, Modular Design)
```
lankaconnect/
├── src/
│   ├── LankaConnect.Shared/              # Shared kernel
│   │   ├── Common/                       # BaseEntity, ValueObject, etc.
│   │   ├── Auth/                         # Authentication services
│   │   ├── ReferenceData/                # Shared reference data
│   │   └── Interfaces/                   # Cross-module interfaces
│   │
│   ├── LankaConnect.Events/              # Events module (existing, refactored)
│   │   ├── Events.Domain/
│   │   ├── Events.Application/
│   │   ├── Events.Infrastructure/
│   │   └── Events.API/
│   │
│   ├── LankaConnect.Marketplace/         # Marketplace module (NEW)
│   │   ├── Marketplace.Domain/
│   │   ├── Marketplace.Application/
│   │   ├── Marketplace.Infrastructure/
│   │   └── Marketplace.API/
│   │
│   ├── LankaConnect.BusinessProfile/     # Business Profile module (NEW)
│   │   ├── BusinessProfile.Domain/
│   │   ├── BusinessProfile.Application/
│   │   ├── BusinessProfile.Infrastructure/
│   │   └── BusinessProfile.API/
│   │
│   ├── LankaConnect.Forum/               # Forum module (NEW)
│   │   ├── Forum.Domain/
│   │   ├── Forum.Application/
│   │   ├── Forum.Infrastructure/
│   │   └── Forum.API/
│   │
│   └── LankaConnect.API/                 # Main API host (composition root)
│       ├── Program.cs                    # Registers all modules
│       ├── appsettings.json              # Shared configuration
│       └── Dockerfile                    # Single container
│
└── web/                                  # Frontend (Next.js)
    └── src/
        ├── app/
        ├── infrastructure/api/
        └── presentation/
```

### Module Boundaries (Strict Rules)
**✅ ALLOWED:**
- Module references `Shared` project
- Module exposes public API via controllers
- Module uses own database schema
- Module publishes domain events to shared event bus

**❌ FORBIDDEN:**
- Module A directly references Module B code
- Module A directly queries Module B database tables
- Circular dependencies between modules
- Shared entities across module boundaries

### Database Structure (PostgreSQL with Schema Separation)
```sql
-- Single PostgreSQL database, multiple schemas
lankaconnect_db
├── shared                    -- Auth, users, reference data
│   ├── users
│   ├── refresh_tokens
│   ├── metro_areas
│   └── reference_values
├── events                    -- Events module
│   ├── events
│   ├── registrations
│   ├── sign_up_lists
│   └── tickets
├── marketplace               -- Marketplace module
│   ├── products
│   ├── shopping_carts
│   ├── orders
│   └── promotions
├── business                  -- Business Profile module
│   ├── profiles
│   ├── services
│   └── reviews
└── forum                     -- Forum module
    ├── forums
    ├── posts
    └── comments
```

**Why Schema Separation Matters:**
- Easy to extract to separate databases later (just change connection string)
- No cross-schema foreign keys (modules are decoupled)
- Clear ownership boundaries
- Migration scripts organized by schema

---

## 🚀 Implementation Timeline: 4-Week Plan

### Week 1: Foundation & Refactoring

**Tasks:**
1. ✅ **Refactor existing Events code** into `LankaConnect.Events` module structure
   - Move `Domain/Events/` → `Events.Domain/`
   - Move `Application/Events/` → `Events.Application/`
   - Move `Infrastructure/Data/` (events-related) → `Events.Infrastructure/`
   - Split 2,286-line `EventsController.cs` into focused controllers:
     - `EventsController.cs` (core CRUD)
     - `EventRegistrationsController.cs` (registrations)
     - `EventSignUpsController.cs` (sign-up lists)
     - `EventMediaController.cs` (images/videos)

2. ✅ **Create Shared module** (`LankaConnect.Shared/`)
   - Extract common code: `BaseEntity`, `ValueObject`, interfaces
   - Extract auth services: JWT validation, current user service
   - Extract reference data service

3. ✅ **Update dependency injection** in `Program.cs`
   - Register Events module services
   - Register Shared services
   - Test that Events module still works

**Deliverable:** Clean module structure, Events module refactored and working

---

### Week 2: Marketplace Module

**Tasks:**
1. ✅ **Build Marketplace.Domain**
   - Aggregates: `Product`, `Order`, `ShoppingCart`, `Promotion`
   - Value Objects: `Money`, `ProductImage`, `ShippingAddress`, `InventoryLevel`
   - Enums: `ProductBadge`, `OrderStatus`, `ShippingProvider`

2. ✅ **Build Marketplace.Application**
   - Commands: `CreateProduct`, `UpdateStock`, `AddToCart`, `Checkout`, `ShipOrder`
   - Queries: `GetProducts`, `SearchProducts`, `GetCart`, `GetOrders`
   - Command/Query handlers with validation

3. ✅ **Build Marketplace.Infrastructure**
   - Create `marketplace` schema in database
   - Entity configurations for EF Core
   - Repositories: `ProductRepository`, `OrderRepository`, `CartRepository`
   - Migrations for marketplace schema

4. ✅ **Build Marketplace.API**
   - Controllers: `ProductsController`, `CartController`, `OrdersController`, `PromotionsController`
   - DTOs for API requests/responses
   - Stripe integration (reuse existing setup)

5. ✅ **Implement Stripe Checkout**
   - Create checkout session endpoint
   - Webhook handler for payment completion
   - Inventory deduction on successful payment

6. ✅ **Implement Shipping Integration**
   - USPS API for label generation
   - UPS API for label generation
   - FedEx API for label generation
   - Store labels in Azure Blob Storage

7. ✅ **Register module** in `Program.cs`

**Deliverable:** Complete Marketplace module with shopping cart, payments, shipping

---

### Week 3: Business Profile & Forum Modules (Parallel Development)

#### Team Member 1: Business Profile Module

**Tasks:**
1. ✅ **Build BusinessProfile.Domain**
   - Aggregates: `BusinessProfile`, `BusinessService`, `Review`
   - Value Objects: `ContactInformation`, `PriceRange`
   - Enums: `BusinessCategory`, `ApprovalStatus`

2. ✅ **Build BusinessProfile.Application**
   - Commands: `CreateProfile`, `UpdateProfile`, `SubmitForApproval`, `ApproveProfile`, `PublishProfile`
   - Queries: `GetProfiles`, `GetProfileById`, `GetPendingApprovals`
   - Approval workflow logic

3. ✅ **Build BusinessProfile.Infrastructure**
   - Create `business` schema
   - Entity configurations
   - Repositories: `BusinessProfileRepository`, `BusinessServiceRepository`

4. ✅ **Build BusinessProfile.API**
   - Controllers: `BusinessProfilesController`, `BusinessServicesController`, `BusinessAdminController`
   - Admin notification system

5. ✅ **Register module** in `Program.cs`

**Deliverable:** Complete Business Profile module with approval workflow

#### Team Member 2: Forum Module

**Tasks:**
1. ✅ **Build Forum.Domain**
   - Aggregates: `Forum`, `ForumPost`, `Comment`
   - Enums: `ModerationStatus`, `ForumCategory`

2. ✅ **Build Forum.Application**
   - Commands: `CreateForum`, `CreatePost`, `AddComment`, `ReplyToComment`
   - Queries: `GetForums`, `GetPosts`, `GetComments`, `GetPendingModeration`
   - Content moderation service (dictionary + AI)

3. ✅ **Build Forum.Infrastructure**
   - Create `forum` schema
   - Entity configurations
   - Repositories: `ForumRepository`, `PostRepository`, `CommentRepository`
   - Azure Content Moderator integration

4. ✅ **Build Forum.API**
   - Controllers: `ForumsController`, `PostsController`, `CommentsController`, `ModerationController`
   - Bad word filtering middleware

5. ✅ **Register module** in `Program.cs`

**Deliverable:** Complete Forum module with content moderation

---

### Week 4: Frontend & Testing & Deployment

**Tasks:**
1. ✅ **Build Frontend Pages**
   - Marketplace pages: catalog, cart, checkout, orders
   - Business Profile pages: directory, detail, my-profile, admin-approvals
   - Forum pages: forum-list, post-detail, create-post, moderation

2. ✅ **Create API Repositories**
   - `marketplace.repository.ts`
   - `business-profile.repository.ts`
   - `forum.repository.ts`

3. ✅ **Update Navigation**
   - Add "Marketplace", "Business", "Forum" links

4. ✅ **Comprehensive Testing**
   - Unit tests for all modules (90%+ coverage)
   - Integration tests for API endpoints
   - E2E tests for critical user journeys

5. ✅ **Deploy to Azure Container Apps**
   - Build Docker container (single container with all modules)
   - Deploy to Azure
   - Run smoke tests
   - **GO LIVE! 🎉**

**Deliverable:** Complete application deployed to production

---

## 📦 Module Design Principles (For Future Extraction)

### 1. Self-Contained Modules
Each module must be independently deployable (when extracted):
- Has own Domain, Application, Infrastructure, API layers
- Has own database schema (no cross-schema FK constraints)
- Has own migrations folder
- Doesn't directly reference other module code

### 2. Communication Patterns

**In Monolith (Now):**
- Use **MediatR commands/queries** for cross-module communication
- Use **domain events** for async notifications
- Use **shared interfaces** in `Shared` project

**In Microservices (Later):**
- MediatR commands → HTTP API calls
- Domain events → Message queue (RabbitMQ/Azure Service Bus)
- Shared interfaces → API contracts (OpenAPI)

**Example:**
```csharp
// Now (Monolith): Events module notifies Marketplace about user registration
public class UserRegisteredForEventHandler : INotificationHandler<UserRegisteredForEvent>
{
    private readonly IMediator _mediator;  // In-process communication

    public async Task Handle(UserRegisteredForEvent notification, CancellationToken ct)
    {
        // Send welcome email, update analytics, etc.
        await _mediator.Send(new SendWelcomeEmailCommand(notification.UserId));
    }
}

// Later (Microservices): Same logic, different transport
public class UserRegisteredForEventHandler : INotificationHandler<UserRegisteredForEvent>
{
    private readonly IMessageBus _messageBus;  // Out-of-process communication

    public async Task Handle(UserRegisteredForEvent notification, CancellationToken ct)
    {
        // Publish to message queue, consumed by Email service
        await _messageBus.Publish(new UserRegisteredMessage(notification.UserId));
    }
}
```

### 3. Dependency Rules
```
┌─────────────────────────────────────┐
│         LankaConnect.API            │  ← Composition root (knows all modules)
│         (Presentation)              │
└─────────────────────────────────────┘
            ↓ depends on ↓
┌──────────────┬──────────────┬────────────────┬──────────────┐
│ Events.API   │Marketplace.API│BusinessProfile│  Forum.API   │
│              │              │     .API      │              │
└──────────────┴──────────────┴────────────────┴──────────────┘
            ↓ depends on ↓
┌──────────────┬──────────────┬────────────────┬──────────────┐
│Events.       │Marketplace.   │BusinessProfile │Forum.        │
│Application   │Application    │.Application    │Application   │
└──────────────┴──────────────┴────────────────┴──────────────┘
            ↓ depends on ↓
┌──────────────┬──────────────┬────────────────┬──────────────┐
│Events.Domain │Marketplace.   │BusinessProfile │Forum.Domain  │
│              │Domain         │.Domain         │              │
└──────────────┴──────────────┴────────────────┴──────────────┘
            ↓ depends on ↓
┌─────────────────────────────────────────────────────────────┐
│              LankaConnect.Shared                            │
│  (Common, Auth, ReferenceData, Interfaces)                  │
└─────────────────────────────────────────────────────────────┘
```

**Rule:** Modules can depend on `Shared`, but NOT on each other.

### 4. Testing Strategy
Each module has own test projects:
- `Events.Domain.Tests` (unit tests)
- `Events.Application.Tests` (unit + integration tests)
- `Events.API.Tests` (integration tests)

This ensures modules are independently testable (critical for extraction).

---

## 🔄 Future Extraction Strategy (When Needed)

### When to Extract to Microservices?

✅ **Extract when:**
- Building 2nd application that needs Marketplace (your concrete plan)
- Team grows to 4+ developers (coordination overhead justifies separation)
- Proven performance bottleneck in specific module (production data shows need)
- Need independent deployment cadence (e.g., Marketplace updates daily, Events monthly)

❌ **Don't extract if:**
- Team is still 2-3 developers (overhead too high)
- No reuse requirements yet (YAGNI principle)
- Features still changing frequently (microservices make refactoring harder)

### Extraction Process (When Ready)

**Step 1: Choose Module to Extract** (e.g., Marketplace)

**Step 2: Create New Repository**
```
lankaconnect-marketplace/
├── src/
│   ├── Marketplace.Domain/          ← Copy from monolith
│   ├── Marketplace.Application/     ← Copy from monolith
│   ├── Marketplace.Infrastructure/  ← Copy from monolith
│   └── Marketplace.API/             ← Copy from monolith
├── tests/
└── Dockerfile
```

**Step 3: Convert Inter-Module Communication**
```csharp
// Before (in monolith):
await _mediator.Send(new GetUserByIdQuery(userId));

// After (in microservice):
var user = await _httpClient.GetAsync($"https://auth-api/users/{userId}");
```

**Step 4: Split Database (if needed)**
- Option A: Keep shared database, separate schemas (simpler)
- Option B: Migrate to separate database (more isolation)

**Step 5: Deploy as Separate Container**
- Build Docker image
- Deploy to Azure Container Apps
- Update API Gateway routing (if using gateway)

**Step 6: Update Frontend**
- Change API endpoint URLs (if different)
- Test all features still work

**Estimated Extraction Time:** 2-3 days per module (because code already has clean boundaries)

---

## 💰 Cost Comparison: Modular Monolith vs Microservices

### Year 1 Total Cost of Ownership (TCO)

| Category | Modular Monolith | Microservices (6 Services) | Savings |
|----------|------------------|----------------------------|---------|
| **Development Time** | 4 weeks | 6-9 weeks | **2-5 weeks saved** |
| **Infrastructure (Monthly)** | $78-110 | $156-225 | **$78-115/month saved** |
| **Infrastructure (Year 1)** | $936-1,320 | $1,872-2,700 | **$936-1,380 saved** |
| **Development Cost** (@ $50/hr) | $8,000 (4 weeks × 40h × $50) | $12,000-18,000 (6-9 weeks) | **$4,000-10,000 saved** |
| **Maintenance/Debugging** | Baseline | +50% (distributed debugging) | **Less complexity** |
| **Feature Development Speed** | Baseline | -30-50% (coordination overhead) | **Faster delivery** |
| **Time to Production** | 4 weeks | 6-9 weeks | **2-5 weeks earlier** |
| **Year 1 TCO** | **~$9,000-10,000** | **~$14,000-21,000** | **~$5,000-11,000 saved** |

### When Microservices Costs Are Justified
- When extracting for reuse in 2nd application (ROI from sharing code)
- When team size justifies coordination overhead (4+ devs)
- When scaling needs justify infrastructure cost (proven by production data)

---

## ✅ Decision Checklist

Before extracting to microservices, verify:

- [ ] **Business need is concrete** (not speculative future-proofing)
  - ✅ You have: 2+ apps planned using Marketplace
- [ ] **Team size supports it** (4+ developers)
  - ❌ You have: 2-3 developers (small team, overhead will be felt)
- [ ] **Production data proves need** (performance bottlenecks, scaling issues)
  - ❌ Not in production yet, no data
- [ ] **ROI justifies cost** (benefits outweigh 2x cost increase)
  - ⚠️ When reusing Marketplace, ROI improves (shared development cost)

**Recommendation:** Build modular monolith NOW, extract Marketplace when ready to build 2nd app.

---

## 🚀 Getting Started: Multi-Agent Coordination

### Phase 1: Weeks 1-4 (Modular Monolith Development)

Spawn **3 parallel agent teams** to work on different modules:

```javascript
[Single Message - Parallel Agent Execution]:
  Task("Refactor Events Module", "
    1. Restructure Events code into LankaConnect.Events module
    2. Split 2,286-line EventsController into focused controllers
    3. Create Events.Domain, Events.Application, Events.Infrastructure, Events.API
    4. Test that all existing event features work
    5. Update Program.cs to register Events module
  ", "coder")

  Task("Build Marketplace Module", "
    1. Build Marketplace.Domain (Product, Order, Cart aggregates)
    2. Build Marketplace.Application (Commands, Queries, Handlers)
    3. Build Marketplace.Infrastructure (DB schema, repositories, Stripe)
    4. Build Marketplace.API (Controllers, DTOs)
    5. Implement shopping cart, checkout, Stripe payments, shipping labels
    6. Create comprehensive tests
  ", "backend-dev")

  Task("Build Business Profile & Forum Modules", "
    1. Build BusinessProfile.Domain, Application, Infrastructure, API
    2. Implement approval workflow, admin notifications
    3. Build Forum.Domain, Application, Infrastructure, API
    4. Implement content moderation (dictionary + AI)
    5. Create comprehensive tests for both modules
  ", "backend-dev")

  Task("Build Frontend Features", "
    1. Build Marketplace pages (catalog, cart, checkout, orders)
    2. Build Business Profile pages (directory, my-profile, admin-approvals)
    3. Build Forum pages (forum-list, post-detail, create-post)
    4. Create API repositories for all modules
    5. Update navigation
    6. E2E tests for critical user journeys
  ", "coder")

  TodoWrite { todos: [
    {content: "Refactor Events into module structure", status: "pending", ...},
    {content: "Build Marketplace module", status: "pending", ...},
    {content: "Build Business Profile module", status: "pending", ...},
    {content: "Build Forum module", status: "pending", ...},
    {content: "Build frontend pages", status: "pending", ...},
    {content: "Deploy to Azure Container Apps", status: "pending", ...},
    {content: "Run comprehensive testing", status: "pending", ...},
    {content: "Go live in production", status: "pending", ...}
  ]}
```

---

## 📚 References

### Supporting Evidence for Modular Monolith Approach

**Martin Fowler (Microservices Expert):**
> "Almost all the successful microservice stories have started with a monolith that got too big and was broken up. Almost all the cases where I've heard of a system that was built as a microservice system from scratch, it has ended up in serious trouble."

**Shopify ($5B revenue, monolith):**
> "We've kept Shopify as a modular monolith. We have over 1,000 engineers working in the same codebase. The key is clear module boundaries and disciplined development practices."

**Stack Overflow (100M users, monolith):**
> "We run one of the largest websites in the world on a monolithic architecture. We have 9 web servers serving 1.3 billion page views per month."

**Your Clean Architecture foundation:**
> You already have Domain, Application, Infrastructure, API layers separated. This is 80% of the work needed for microservices. The remaining 20% (network boundaries, API Gateway, distributed tracing) can be added when needed.

---

## 🎯 Success Metrics

### Modular Monolith Success Criteria
- [ ] Production in 4 weeks (vs 6-9 for microservices)
- [ ] All modules have clean boundaries (no cross-module references)
- [ ] Each module has own database schema
- [ ] 90%+ test coverage per module
- [ ] Single Docker container deployment successful
- [ ] All features functional (Events, Marketplace, Business Profile, Forum)
- [ ] Cost: $78-110/month (vs $156-225 for microservices)

### Future Extraction Success Criteria
- [ ] Can extract module to separate service in 2-3 days
- [ ] No code changes needed in other modules when extracting
- [ ] Extracted service works independently
- [ ] ROI justified (reuse in 2+ apps OR team size justifies overhead)

---

## 📞 Decision Required

Based on this analysis, please confirm your approach:

**Option A ✅ RECOMMENDED:**
> "Build as modular monolith (4 weeks, $78-110/month). Extract Marketplace to separate container when ready to build 2nd application. Get to production faster, prove features work, lower cost."

**Option B:**
> "Proceed with full microservices migration (6-9 weeks, $156-225/month). Accept higher cost and complexity for immediate separation."

**Option C:**
> "Hybrid: Deploy monolith to production in 2 weeks (with just Events), then build 3 new modules as microservices."

---

**What's your decision? Ready to proceed with Option A?**
