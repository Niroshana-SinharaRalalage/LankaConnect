# Agent Instruction Template for Spawned Agents

**Use this template when spawning agents that cannot have back-and-forth conversation.**

---

## Agent Task Template

### Context
```
Module: [Marketplace/Business/Forum/Events]
Branch: [feature/marketplace-module]
Worktree: [../lc-marketplace]
Database Schema: [marketplace]
```

### Objective
```
Build complete [MODULE NAME] following Clean Architecture + DDD + TDD.

DELIVERABLES:
- [ ] Domain layer (aggregates, value objects, domain events)
- [ ] Application layer (commands, queries, handlers)
- [ ] Infrastructure layer (repositories, database context)
- [ ] API layer (controllers, DTOs)
- [ ] Tests (90%+ coverage)
- [ ] Database migrations
- [ ] Deployment to staging
```

### Detailed Requirements

#### 1. Domain Layer Requirements
```
AGGREGATES TO BUILD:
- [Aggregate 1]:
  - Properties: [list all properties with types]
  - Behaviors: [list all methods]
  - Validation rules: [list all business rules]
  - Domain events: [list events to raise]

- [Aggregate 2]:
  - Properties: ...
  - Behaviors: ...

VALUE OBJECTS TO BUILD:
- [ValueObject 1]: [exact specification]
- [ValueObject 2]: [exact specification]

DOMAIN SERVICES (if needed):
- [Service 1]: [what it does, when to use]
```

#### 2. Application Layer Requirements
```
COMMANDS (Write operations):
- CreateProduct
  - Input: ProductName, Description, Price, CategoryId, Inventory
  - Validation: Name required, Price > 0, Inventory >= 0
  - Output: ProductId
  - Side effects: Raises ProductCreatedEvent

- UpdateProductInventory
  - Input: ProductId, NewQuantity
  - Validation: Quantity >= 0
  - Output: Success boolean
  - Side effects: Raises LowStockWarningEvent if quantity < 10

QUERIES (Read operations):
- GetProductById
  - Input: ProductId
  - Output: ProductDto

- SearchProducts
  - Input: SearchTerm, CategoryId?, MinPrice?, MaxPrice?, PageSize, PageNumber
  - Output: PagedList<ProductDto>
```

#### 3. Infrastructure Layer Requirements
```
REPOSITORIES:
- IProductRepository
  - GetByIdAsync(ProductId id)
  - SearchAsync(ProductSearchCriteria criteria)
  - AddAsync(Product product)
  - UpdateAsync(Product product)

DATABASE CONTEXT:
- Schema: [marketplace]
- Tables: Products, Categories, ProductVariants, Inventory
- Relationships: Product -> Category (many-to-one), Product -> Variants (one-to-many)

MIGRATIONS:
- Initial: Create Products, Categories tables
- Add ProductVariants table
- Add Inventory tracking
```

#### 4. API Layer Requirements
```
ENDPOINTS:
POST /api/marketplace/products
- Auth: Required (role: Admin)
- Request: CreateProductRequest DTO
- Response: 201 Created with ProductId
- Errors: 400 (validation), 401 (unauthorized), 500 (server error)

GET /api/marketplace/products/{id}
- Auth: Optional (public)
- Response: 200 OK with ProductDto
- Errors: 404 (not found), 500 (server error)

GET /api/marketplace/products/search
- Auth: Optional (public)
- Query params: term, categoryId, minPrice, maxPrice, page, pageSize
- Response: 200 OK with PagedList<ProductDto>
```

#### 5. Third-Party Integrations
```
STRIPE INTEGRATION:
- Use Stripe.net NuGet package
- Create Checkout Session for cart
- Webhook handling for payment confirmation
- Secret keys: Use Azure Key Vault (appsettings reference)

SENDGRID INTEGRATION:
- Send order confirmation emails
- Template: OrderConfirmation
- API key: From Azure Key Vault
```

### Architecture Constraints

#### MUST Follow These Patterns
```
✅ Clean Architecture layers (Domain/Application/Infrastructure/API)
✅ CQRS with MediatR
✅ Repository pattern with EF Core
✅ FluentValidation for input validation
✅ Domain events with MediatR notifications
✅ DTOs for API requests/responses (never expose domain entities)
```

#### MUST NOT Do These
```
❌ NO circular dependencies between modules
❌ NO direct references to other modules (Events, Business, Forum)
❌ NO hardcoded connection strings or secrets
❌ NO breaking existing Events module functionality
❌ NO UI components without referencing UI_STYLE_GUIDE.md
```

### Test Requirements

#### Test Coverage Targets
```
- Domain tests: 95%+ (all aggregates, value objects, domain logic)
- Application tests: 90%+ (all command/query handlers)
- Integration tests: 80%+ (API endpoints, database operations)
```

#### Test Scenarios to Cover
```
DOMAIN TESTS:
- Product creation with valid data succeeds
- Product creation with invalid data throws exceptions
- LowStockWarningEvent raised when inventory < 10
- ProductVariant pricing calculation is correct

APPLICATION TESTS:
- CreateProductCommandHandler creates product successfully
- CreateProductCommandHandler validates input correctly
- SearchProductsQueryHandler filters by category
- SearchProductsQueryHandler paginates results

INTEGRATION TESTS:
- POST /api/marketplace/products creates product in database
- GET /api/marketplace/products/search returns filtered results
- Stripe checkout creates valid session
- Payment webhook updates order status
```

### UI Requirements (if applicable)

#### Pages to Build
```
/marketplace - Product catalog
  - Layout: Grid view with filters sidebar
  - Components: ProductCard, FilterPanel, SearchBar, Pagination
  - Styling: Use UI_STYLE_GUIDE.md components

/marketplace/products/[id] - Product detail
  - Layout: Image gallery + details + add to cart
  - Components: ImageGallery, ProductInfo, AddToCartButton
  - Styling: Use UI_STYLE_GUIDE.md Button, Card components
```

#### Component Requirements
```
<ProductCard>
  - Props: product (id, name, price, image)
  - Displays: Image, name, price, "Add to Cart" button
  - Uses: Button from UI_STYLE_GUIDE.md
  - State: Cart item count in Zustand store

MUST USE EXISTING COMPONENTS:
- Button (from UI_STYLE_GUIDE.md)
- Input (from UI_STYLE_GUIDE.md)
- Card (from UI_STYLE_GUIDE.md)
- Alert (from UI_STYLE_GUIDE.md)
```

### Deployment Requirements

#### Azure Container Apps Deployment
```
ENVIRONMENT VARIABLES:
- ConnectionStrings__DefaultConnection (Azure PostgreSQL)
- StripeSettings__SecretKey (from Azure Key Vault)
- SendGridSettings__ApiKey (from Azure Key Vault)

HEALTH CHECKS:
- Endpoint: /health
- Must verify: Database connection, Stripe API, SendGrid API

POST-DEPLOYMENT VERIFICATION:
1. Run: curl https://[staging-url]/health
2. Test: POST /api/marketplace/products with valid data
3. Test: GET /api/marketplace/products/search
4. Verify: Database migrations applied
5. Check: Container logs for errors
```

### Coordination Requirements

#### Memory Hooks for Agent Coordination
```
STORE IN MEMORY:
npx claude-flow@alpha hooks post-edit --memory-key "swarm/marketplace/product-schema" --file "Product.cs"
npx claude-flow@alpha hooks post-edit --memory-key "swarm/marketplace/api-contracts" --file "ProductDto.cs"

READ FROM MEMORY (for Frontend agent):
npx claude-flow@alpha hooks session-restore --read-key "swarm/marketplace/api-contracts"
```

#### Documentation Updates
```
BEFORE COMPLETION, UPDATE:
1. docs/PROGRESS_TRACKER.md
   - Session summary
   - Files changed
   - Tests added
   - Deployment status

2. docs/STREAMLINED_ACTION_PLAN.md
   - Mark phase complete
   - Link to summary document

3. docs/TASK_SYNCHRONIZATION_STRATEGY.md
   - Update phase status
   - Add deliverables
```

### Decision Points (If Agent Needs to Make Choices)

#### Decision 1: Product Variant Implementation
```
OPTION A: Separate ProductVariant table
  - Pros: Flexible, unlimited variants
  - Cons: More complex queries

OPTION B: JSON column for variants
  - Pros: Simpler schema
  - Cons: Harder to query/filter

RECOMMENDED: Option A (separate table)
REASON: Better queryability, follows relational design
```

#### Decision 2: Image Storage
```
OPTION A: Azure Blob Storage
  - Pros: Scalable, CDN support
  - Cons: External dependency

OPTION B: Database (bytea column)
  - Pros: Simple, no external service
  - Cons: Database bloat

RECOMMENDED: Option A (Azure Blob)
REASON: Aligns with existing Events module pattern
```

### Expected Deliverables Checklist

At completion, agent should have:

```
BACKEND:
- [ ] 15+ domain classes (aggregates, value objects)
- [ ] 8+ command handlers
- [ ] 6+ query handlers
- [ ] 4+ API controllers
- [ ] 12+ database migrations
- [ ] 60+ unit tests (90%+ coverage)
- [ ] 20+ integration tests

FRONTEND (if applicable):
- [ ] 8 pages built
- [ ] 15+ React components
- [ ] API repository for Marketplace
- [ ] Zustand store for cart
- [ ] TypeScript types for all DTOs

DEPLOYMENT:
- [ ] Deployed to staging
- [ ] Health check passing
- [ ] All tests passing
- [ ] API endpoints tested with curl
- [ ] Database migrations applied

DOCUMENTATION:
- [ ] API documentation (Swagger)
- [ ] Phase summary document created
- [ ] All 3 PRIMARY docs updated
- [ ] README updated
```

### Example Usage (How to Spawn Agent with This Template)

```javascript
Task(
  "Marketplace Developer",
  `Build complete Marketplace module following the detailed specification at:
   docs/AGENT_INSTRUCTION_TEMPLATE.md

   Specific focus areas:
   - Product catalog with categories, search, filters
   - Shopping cart with session persistence
   - Stripe checkout integration
   - Order management with status tracking
   - Inventory management with low-stock warnings (<10 items)
   - Admin product management
   - Promotion system (20% off, free shipping)
   - Shipping label generation (USPS, UPS, FedEx)

   Follow ALL requirements in the template exactly.
   Use memory hooks for coordination with other agents.
   Update all tracking docs before completion.
   Deploy to staging and verify.
  `,
  "backend-dev"
)
```

---

## Template Sections Explained

### Why Each Section Matters

**Context**: Tells agent where to work (branch, worktree, schema)

**Detailed Requirements**: Removes ambiguity - agent knows EXACTLY what to build

**Architecture Constraints**: Prevents agent from making wrong architectural choices

**Test Requirements**: Ensures comprehensive testing (agents might skip this otherwise)

**UI Requirements**: Ensures consistent UI using style guide

**Deployment Requirements**: Agent knows how to deploy and verify

**Coordination Requirements**: Agent knows how to coordinate with other agents

**Decision Points**: Pre-answers common questions so agent doesn't get stuck

**Deliverables Checklist**: Agent knows when they're done

---

## Tips for Using This Template

1. **Fill out EVERY section** - Incomplete sections = agent makes assumptions
2. **Be specific about types** - "Price: decimal" not just "Price"
3. **List ALL validation rules** - Prevents security holes
4. **Specify error handling** - 400 vs 404 vs 500 responses
5. **Include exact component names** - Use ProductCard, not "a card component"
6. **Reference existing patterns** - "Follow Events module pattern for..."
7. **Pre-make architectural decisions** - Don't leave choices to agent
8. **Include verification steps** - Agent tests their own work

---

## When to Use Spawned Agents vs Interactive

**Use Spawned Agents When:**
- ✅ Requirements are crystal clear
- ✅ You've filled out this template completely
- ✅ You want parallel execution for speed
- ✅ You're comfortable reviewing code after completion

**Use Interactive Agent (current conversation) When:**
- ✅ Requirements are evolving
- ✅ You want to make decisions as you go
- ✅ You need to ask questions mid-implementation
- ✅ You want tight control over every step

---

**BOTTOM LINE**: You don't HAVE to use spawned agents. Work with me interactively if you prefer control over speed.
