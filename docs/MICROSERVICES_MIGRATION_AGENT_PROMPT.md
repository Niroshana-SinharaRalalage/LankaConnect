# LankaConnect Microservices Migration - Multi-Agent Coordination Prompt

## 🎯 Mission Overview

Transform LankaConnect from a Clean Architecture monolith to a microservices architecture with API Gateway, while maintaining zero downtime and building three new services (Marketplace, Business Profile, Forum) in parallel.

---

## 📊 Current Architecture Summary

**Tech Stack:**
- Backend: .NET 8 with Clean Architecture (Domain, Application, Infrastructure, API)
- Frontend: Next.js 16 + React 19 (TypeScript)
- Database: PostgreSQL with schema-based separation
- Auth: JWT with refresh tokens
- Infra: Azure Container Apps, Redis, Azure Blob Storage, Stripe, Hangfire

**Current Structure:**
- Monolithic .NET API with 24 controllers (EventsController has 2,286 lines)
- Single Next.js frontend
- Single PostgreSQL database with schemas: identity, events, reference_data, etc.
- Already well-separated at code level (Domain/Application/Infrastructure/API layers)

---

## 🏗️ Target Architecture

### Repository Structure (Multi-Repo)
```
lankaconnect-frontend/                # Frontend ONLY (Next.js)
lankaconnect-api-gateway/             # API Gateway + Shared Auth Service
lankaconnect-events-api/              # Events Microservice (Extracted)
lankaconnect-marketplace-api/         # Marketplace Microservice (New)
lankaconnect-business-profile-api/    # Business Profile Microservice (New)
lankaconnect-forum-api/               # Forum Microservice (New)
lankaconnect-shared/                  # Shared NuGet Package (Optional)
```

### Database Strategy
**Single PostgreSQL instance with schema separation:**
- `identity` schema → Shared (accessed via API Gateway Auth Service)
- `reference_data` schema → Shared (accessed via API Gateway Reference Service)
- `events` schema → Events API only
- `marketplace` schema → Marketplace API only
- `business` schema → Business Profile API only
- `forum` schema → Forum API only

### API Gateway Responsibilities
- JWT authentication/validation
- Token refresh mechanism
- User context injection (X-User-Id, X-User-Role headers)
- Reference data caching service (read-only)
- Routing to microservices
- Rate limiting, CORS, logging, circuit breaker

---

## 🚀 Migration Strategy: Two-Phase Approach

### Phase 1: Foundation & Events Extraction (2-3 weeks)
**Goal:** Extract Events API and set up infrastructure for microservices

**Agent Teams (Run in Parallel):**
1. Infrastructure & API Gateway Team
2. Events API Extraction Team
3. Frontend Integration Team

### Phase 2: New Services Development (4-6 weeks)
**Goal:** Build Marketplace, Business Profile, Forum in parallel

**Agent Teams (Run in Parallel):**
1. Marketplace API Team
2. Business Profile API Team
3. Forum API Team
4. Frontend Features Team

---

## 👥 PHASE 1 AGENT TEAMS (Parallel Execution)

### Team 1: Infrastructure & API Gateway Agent

**Primary Responsibility:** Build the foundation for microservices architecture

**Tasks:**
1. **Create API Gateway Repository** (`lankaconnect-api-gateway`)
   - Initialize .NET 8 Web API project
   - Install Ocelot or YARP for gateway routing
   - Set up project structure:
     ```
     src/
     ├── Authentication/
     │   ├── JwtValidationMiddleware.cs
     │   ├── TokenRefreshService.cs
     │   └── AuthenticationExtensions.cs
     ├── ReferenceData/
     │   ├── ReferenceDataService.cs (cached read-only)
     │   └── Models/
     ├── Routing/
     │   ├── ocelot.json (or YARP appsettings)
     │   └── RouteConfiguration.cs
     ├── Middleware/
     │   ├── RateLimitingMiddleware.cs
     │   ├── LoggingMiddleware.cs
     │   └── ExceptionHandlingMiddleware.cs
     └── Program.cs
     ```

2. **Extract Authentication Logic**
   - Copy JWT validation logic from existing `LankaConnect.API/Security/`
   - Implement token validation middleware
   - Implement token refresh endpoint (`POST /api/auth/refresh`)
   - Inject user context into downstream requests (headers: X-User-Id, X-User-Role, X-User-Email)
   - Support role-based routing

3. **Implement Reference Data Service**
   - Read-only service that caches reference data from `reference_data` schema
   - Endpoints:
     - `GET /api/reference/event-categories`
     - `GET /api/reference/user-roles`
     - `GET /api/reference/metro-areas`
   - Use Redis cache with fallback to database
   - TTL: 1 hour

4. **Configure Routing**
   - Route `/api/auth/*` to Auth service (in API Gateway itself)
   - Route `/api/reference/*` to Reference Data service (in API Gateway itself)
   - Route `/api/events/*` to Events API (to be extracted)
   - Route `/api/marketplace/*` to Marketplace API (Phase 2)
   - Route `/api/business/*` to Business Profile API (Phase 2)
   - Route `/api/forum/*` to Forum API (Phase 2)
   - Implement circuit breaker pattern (fail after 3 retries)

5. **Create Shared NuGet Package** (`lankaconnect-shared`)
   - Extract common code from existing monolith:
     - `Common/BaseEntity.cs`
     - `Common/ValueObject.cs`
     - `Common/Interfaces/` (IRepository, IUnitOfWork, ICommand, IQuery, etc.)
     - `Common/Extensions/`
   - Publish to private Azure Artifacts feed or local NuGet source

6. **Docker & Local Development Setup**
   - Create `Dockerfile` for API Gateway
   - Create `docker-compose.yml` for local development:
     ```yaml
     services:
       api-gateway:
         build: ../lankaconnect-api-gateway
         ports: ["8000:8000"]
         environment:
           - ConnectionStrings__DefaultConnection=Host=db;Database=lankaconnect;...
           - Redis__Configuration=redis:6379

       events-api:
         build: ../lankaconnect-events-api
         ports: ["8001:8001"]

       frontend:
         build: ../lankaconnect-frontend
         ports: ["3000:3000"]
         environment:
           - NEXT_PUBLIC_API_URL=http://api-gateway:8000

       db:
         image: postgres:16-alpine
         ports: ["5432:5432"]
         volumes:
           - postgres_data:/var/lib/postgresql/data

       redis:
         image: redis:7-alpine
         ports: ["6379:6379"]
     ```

7. **Azure Deployment Configuration**
   - Create Azure Container Registry (ACR)
   - Create Azure Container App for API Gateway
   - Configure environment variables (connection strings, JWT secrets, Redis)
   - Set up Application Insights for logging

**Deliverables:**
- [ ] `lankaconnect-api-gateway` repository with working code
- [ ] `lankaconnect-shared` NuGet package
- [ ] `docker-compose.yml` for local development
- [ ] Azure deployment scripts
- [ ] API Gateway successfully validates JWT tokens
- [ ] API Gateway successfully routes to downstream services
- [ ] Comprehensive README with setup instructions

**Success Criteria:**
- API Gateway starts without errors
- JWT validation works (test with existing tokens)
- Reference data endpoints return cached data
- Routes correctly proxy to downstream services
- Docker Compose orchestrates all services locally

---

### Team 2: Events API Extraction Agent

**Primary Responsibility:** Extract Events API from monolith to standalone microservice

**Tasks:**
1. **Create Events API Repository** (`lankaconnect-events-api`)
   - Initialize .NET 8 Web API project
   - Install dependencies:
     - Entity Framework Core 8
     - Npgsql (PostgreSQL driver)
     - AutoMapper
     - FluentValidation
     - MediatR
     - Hangfire (for background jobs)
   - Reference `lankaconnect-shared` NuGet package

2. **Copy Events Domain Code**
   - Source: `src/LankaConnect.Domain/Events/`
   - Destination: `lankaconnect-events-api/src/Domain/Events/`
   - Copy these files:
     - `Event.cs` (aggregate root)
     - `Registration.cs`
     - `SignUpList.cs`
     - `SignUpItem.cs`
     - `SignUpCommitment.cs`
     - `EventTemplate.cs`
     - `EventImage.cs`
     - `EventVideo.cs`
     - `Ticket.cs`
     - All value objects (EventLocation, EventDates, EventCapacity, etc.)
     - All enums (EventStatus, RegistrationStatus, etc.)

3. **Copy Events Application Code**
   - Source: `src/LankaConnect.Application/Events/`
   - Destination: `lankaconnect-events-api/src/Application/Events/`
   - Copy these folders:
     - `Commands/` (40+ command handlers)
     - `Queries/` (20+ query handlers)
     - `EventHandlers/` (MediatR notification handlers)
     - `Repositories/`
     - `Services/`
     - `BackgroundJobs/`

4. **Copy Events Infrastructure Code**
   - Source: `src/LankaConnect.Infrastructure/Data/`
   - Destination: `lankaconnect-events-api/src/Infrastructure/Data/`
   - Copy these files:
     - `AppDbContext.cs` (rename to `EventsDbContext.cs`, keep only events-related DbSets)
     - `Configurations/` (only event-related entity configurations)
     - `Repositories/EventRepository.cs`
     - `Repositories/RegistrationRepository.cs`
     - `Repositories/EventTemplateRepository.cs`
     - `Migrations/` (filter to events schema only)

5. **Copy Events API Controller**
   - Source: `src/LankaConnect.API/Controllers/EventsController.cs` (2,286 lines)
   - Destination: `lankaconnect-events-api/src/API/Controllers/EventsController.cs`
   - Update to use new namespace
   - Remove unnecessary dependencies

6. **Update Dependencies & Configuration**
   - Update `EventsDbContext`:
     - Remove non-events entities
     - Keep only events schema tables
     - Configure schema: `modelBuilder.HasDefaultSchema("events");`
   - Update `appsettings.json`:
     ```json
     {
       "ConnectionStrings": {
         "DefaultConnection": "Host=...;Database=lankaconnect;..."
       },
       "Jwt": {
         "ValidateIssuer": false,
         "ValidateAudience": false,
         "ValidateLifetime": true,
         "ValidateIssuerSigningKey": true,
         "Secret": "[same as API Gateway]"
       },
       "Redis": {
         "Configuration": "..."
       }
     }
     ```
   - Configure dependency injection in `Program.cs`

7. **Implement User Context from API Gateway**
   - Create middleware to extract user info from headers:
     ```csharp
     public class ApiGatewayUserContextMiddleware
     {
         public async Task InvokeAsync(HttpContext context)
         {
             var userId = context.Request.Headers["X-User-Id"];
             var userRole = context.Request.Headers["X-User-Role"];
             // Store in HttpContext.Items for use by handlers
         }
     }
     ```

8. **Database Migrations**
   - Keep only events schema migrations
   - Test migrations against shared PostgreSQL database
   - Ensure no conflicts with other schemas

9. **Background Jobs (Hangfire)**
   - Configure Hangfire to use same PostgreSQL database (separate schema: `hangfire_events`)
   - Copy background jobs:
     - `SendEventNotificationJob`
     - `SendEventReminderJob`
     - etc.

10. **Testing**
    - Copy existing event tests
    - Update test database connection to use `events` schema only
    - Ensure all 50+ endpoints work correctly

11. **Docker & Deployment**
    - Create `Dockerfile`
    - Update `docker-compose.yml` to include events-api service
    - Create Azure Container App configuration

**Deliverables:**
- [ ] `lankaconnect-events-api` repository with working code
- [ ] All Events API endpoints functional (50+ endpoints)
- [ ] Database migrations applied successfully
- [ ] Background jobs running correctly
- [ ] Comprehensive unit and integration tests
- [ ] Docker container builds successfully
- [ ] README with API documentation

**Success Criteria:**
- Events API starts without errors
- All existing event features work correctly
- Can create, update, delete events
- Can register for events (member and anonymous)
- Email notifications sent correctly
- Background jobs execute on schedule
- Performance comparable to monolith

---

### Team 3: Frontend Integration Agent

**Primary Responsibility:** Update frontend to call API Gateway instead of direct monolith

**Tasks:**
1. **Update API Base URL**
   - Update `.env.local`:
     ```
     NEXT_PUBLIC_API_URL=http://localhost:8000  # API Gateway for local
     ```
   - Update production `.env.production`:
     ```
     NEXT_PUBLIC_API_URL=https://api-gateway.lankaconnect.com
     ```

2. **Update API Client**
   - File: `web/src/infrastructure/api/client/api-client.ts`
   - Verify interceptors still work with API Gateway
   - Update token refresh endpoint to `/api/auth/refresh` (now in gateway)
   - Test automatic token refresh on 401 responses

3. **Update Auth Repository**
   - File: `web/src/infrastructure/api/repositories/auth.repository.ts`
   - All auth endpoints now route through API Gateway
   - Verify login, register, logout, password reset all work

4. **Update Events Repository**
   - File: `web/src/infrastructure/api/repositories/events.repository.ts`
   - No changes needed (routes still `/api/events/*`)
   - Test all event operations

5. **Testing**
   - Test login flow
   - Test event creation
   - Test event registration
   - Test event search
   - Test image/video upload
   - Test anonymous registration
   - Verify all existing features work

6. **Error Handling**
   - Update error messages if API Gateway returns different formats
   - Handle gateway-specific errors (503 Service Unavailable, circuit breaker open, etc.)

7. **Documentation**
   - Update README with new API Gateway URL
   - Document local development setup with Docker Compose

**Deliverables:**
- [ ] Frontend successfully calls API Gateway
- [ ] All existing features work correctly
- [ ] Comprehensive testing completed
- [ ] Updated documentation

**Success Criteria:**
- Frontend starts without errors
- Can log in successfully
- Can create/view/edit events
- Can register for events
- All existing UI features functional
- No console errors
- Performance comparable to previous setup

---

## 👥 PHASE 2 AGENT TEAMS (Parallel Execution)

**Prerequisites:** Phase 1 completed and deployed

---

### Team 4: Marketplace API Agent

**Primary Responsibility:** Build complete marketplace microservice with shopping cart, Stripe payments, inventory, shipping

**Requirements:**
- Admin/AdminManager can list products, manage inventory, create discounts/promotions
- Users can browse products, add to cart, checkout with Stripe
- Inventory tracking with "low stock" warnings (<10 items)
- Product badges: Discounted, Sold Out, New Arrival, Best Seller, Organic (admin-managed)
- Promotions: "20% off over $100", "Free shipping over $75"
- Shipping integration: USPS, UPS, FedEx label generation
- Separate `marketplace` schema in shared database

**Tasks:**
1. **Create Repository** (`lankaconnect-marketplace-api`)
   - Initialize .NET 8 Web API project
   - Reference `lankaconnect-shared` NuGet package
   - Set up Clean Architecture structure

2. **Design Domain Model**
   - Aggregates:
     - `Product` (name, description, price, discount, category, images, stock, badges)
     - `Order` (items, total, shipping address, payment status, stripe session ID)
     - `ShoppingCart` (user ID, items, expiration)
     - `Promotion` (type, criteria, discount amount, start/end date)
   - Value Objects:
     - `Money` (amount, currency)
     - `ProductImage`
     - `ShippingAddress`
     - `InventoryLevel`
   - Enums:
     - `ProductBadge` (Discounted, SoldOut, NewArrival, BestSeller, Organic)
     - `OrderStatus` (Pending, PaymentCompleted, Shipped, Delivered, Cancelled)
     - `ShippingProvider` (USPS, UPS, FedEx)

3. **Database Schema** (`marketplace` schema)
   ```sql
   CREATE SCHEMA marketplace;

   CREATE TABLE marketplace.products (
     id UUID PRIMARY KEY,
     name VARCHAR(200) NOT NULL,
     description TEXT,
     price DECIMAL(10, 2) NOT NULL,
     discount_percentage INT DEFAULT 0,
     category_id UUID NOT NULL,
     stock_quantity INT NOT NULL DEFAULT 0,
     is_active BOOLEAN DEFAULT true,
     created_at TIMESTAMP NOT NULL,
     updated_at TIMESTAMP
   );

   CREATE TABLE marketplace.product_images (
     id UUID PRIMARY KEY,
     product_id UUID REFERENCES marketplace.products(id),
     image_url VARCHAR(500) NOT NULL,
     display_order INT NOT NULL
   );

   CREATE TABLE marketplace.product_badges (
     product_id UUID REFERENCES marketplace.products(id),
     badge_type VARCHAR(50) NOT NULL,
     PRIMARY KEY (product_id, badge_type)
   );

   CREATE TABLE marketplace.shopping_carts (
     id UUID PRIMARY KEY,
     user_id UUID NOT NULL,
     expires_at TIMESTAMP NOT NULL,
     created_at TIMESTAMP NOT NULL
   );

   CREATE TABLE marketplace.cart_items (
     id UUID PRIMARY KEY,
     cart_id UUID REFERENCES marketplace.shopping_carts(id),
     product_id UUID NOT NULL,
     quantity INT NOT NULL,
     unit_price DECIMAL(10, 2) NOT NULL
   );

   CREATE TABLE marketplace.orders (
     id UUID PRIMARY KEY,
     user_id UUID NOT NULL,
     order_number VARCHAR(50) UNIQUE NOT NULL,
     subtotal DECIMAL(10, 2) NOT NULL,
     discount_amount DECIMAL(10, 2) DEFAULT 0,
     shipping_cost DECIMAL(10, 2) NOT NULL,
     total_amount DECIMAL(10, 2) NOT NULL,
     status VARCHAR(50) NOT NULL,
     shipping_provider VARCHAR(50),
     tracking_number VARCHAR(100),
     stripe_session_id VARCHAR(255),
     payment_completed_at TIMESTAMP,
     shipped_at TIMESTAMP,
     delivered_at TIMESTAMP,
     created_at TIMESTAMP NOT NULL
   );

   CREATE TABLE marketplace.order_items (
     id UUID PRIMARY KEY,
     order_id UUID REFERENCES marketplace.orders(id),
     product_id UUID NOT NULL,
     product_name VARCHAR(200) NOT NULL,
     quantity INT NOT NULL,
     unit_price DECIMAL(10, 2) NOT NULL,
     discount_applied DECIMAL(10, 2) DEFAULT 0
   );

   CREATE TABLE marketplace.promotions (
     id UUID PRIMARY KEY,
     code VARCHAR(50) UNIQUE,
     description TEXT NOT NULL,
     type VARCHAR(50) NOT NULL,  -- PercentageDiscount, FreeShipping
     criteria JSONB NOT NULL,    -- {"min_order_amount": 100}
     discount_value DECIMAL(10, 2),
     is_active BOOLEAN DEFAULT true,
     start_date TIMESTAMP NOT NULL,
     end_date TIMESTAMP,
     created_at TIMESTAMP NOT NULL
   );

   CREATE TABLE marketplace.shipping_addresses (
     id UUID PRIMARY KEY,
     user_id UUID NOT NULL,
     full_name VARCHAR(200) NOT NULL,
     address_line1 VARCHAR(255) NOT NULL,
     address_line2 VARCHAR(255),
     city VARCHAR(100) NOT NULL,
     state VARCHAR(50) NOT NULL,
     postal_code VARCHAR(20) NOT NULL,
     country VARCHAR(50) NOT NULL,
     phone VARCHAR(20),
     is_default BOOLEAN DEFAULT false
   );
   ```

4. **Implement Commands (Write Operations)**
   - Products:
     - `CreateProductCommand`
     - `UpdateProductCommand`
     - `UpdateProductStockCommand`
     - `ApplyDiscountCommand`
     - `ManageProductBadgesCommand`
   - Shopping Cart:
     - `AddItemToCartCommand`
     - `UpdateCartItemQuantityCommand`
     - `RemoveItemFromCartCommand`
     - `ClearCartCommand`
   - Orders:
     - `CreateOrderCommand`
     - `CheckoutCommand` (creates Stripe session)
     - `CompletePaymentCommand` (webhook handler)
     - `ShipOrderCommand`
     - `GenerateShippingLabelCommand`
   - Promotions:
     - `CreatePromotionCommand`
     - `ApplyPromotionCodeCommand`

5. **Implement Queries (Read Operations)**
   - `GetProductsQuery` (with filters: category, price range, in stock, badges)
   - `GetProductByIdQuery`
   - `SearchProductsQuery` (full-text search)
   - `GetFeaturedProductsQuery`
   - `GetBestSellersQuery`
   - `GetShoppingCartQuery`
   - `GetOrdersQuery`
   - `GetOrderByIdQuery`
   - `GetActivePromotionsQuery`

6. **Implement API Controllers**
   - `ProductsController`:
     - GET /api/marketplace/products (with filters)
     - GET /api/marketplace/products/{id}
     - POST /api/marketplace/products (Admin only)
     - PUT /api/marketplace/products/{id} (Admin only)
     - POST /api/marketplace/products/{id}/discount (Admin only)
     - POST /api/marketplace/products/{id}/badges (Admin only)
   - `CartController`:
     - GET /api/marketplace/cart
     - POST /api/marketplace/cart/items
     - PUT /api/marketplace/cart/items/{id}
     - DELETE /api/marketplace/cart/items/{id}
   - `OrdersController`:
     - GET /api/marketplace/orders
     - GET /api/marketplace/orders/{id}
     - POST /api/marketplace/orders/checkout (creates Stripe session)
     - POST /api/marketplace/orders/{id}/ship (Admin only)
   - `PromotionsController`:
     - GET /api/marketplace/promotions
     - POST /api/marketplace/promotions (Admin only)
     - POST /api/marketplace/cart/apply-promotion

7. **Stripe Integration**
   - Reuse existing Stripe configuration from monolith
   - Create Stripe Checkout Session on checkout
   - Implement webhook handler for `payment_intent.succeeded`
   - Update order status on successful payment
   - Reduce inventory on successful payment

8. **Shipping Integration**
   - Integrate with USPS, UPS, FedEx APIs for label generation
   - Store shipping labels in Azure Blob Storage
   - Return tracking number on shipment

9. **Inventory Management**
   - Decrement stock on successful payment
   - Show "Only X items left" when stock < 10
   - Prevent checkout if out of stock
   - Background job to mark out-of-stock products

10. **Badge Management**
    - Admin can add/remove badges from products
    - Automatic badges:
      - "Discounted" when discount_percentage > 0
      - "Sold Out" when stock_quantity = 0
      - "New Arrival" for products < 30 days old
      - "Best Seller" based on order count (background job)

11. **Promotion Engine**
    - Evaluate promotions during checkout
    - Apply highest value promotion automatically
    - Support:
      - Percentage discount over minimum amount
      - Free shipping over minimum amount
      - Fixed amount discount

12. **Testing**
    - Unit tests for domain logic
    - Integration tests for API endpoints
    - Test Stripe integration (test mode)
    - Test inventory deduction
    - Test promotion application

13. **Docker & Deployment**
    - Create Dockerfile
    - Update docker-compose.yml
    - Deploy to Azure Container Apps

**Deliverables:**
- [ ] `lankaconnect-marketplace-api` repository with full implementation
- [ ] Complete CRUD for products, orders, cart, promotions
- [ ] Stripe payment integration working
- [ ] Inventory management functional
- [ ] Shipping label generation implemented
- [ ] Comprehensive tests (90%+ coverage)
- [ ] API documentation (Swagger)
- [ ] README with setup instructions

**Success Criteria:**
- Can browse products with filters
- Can add products to cart
- Can apply promotions
- Can checkout with Stripe
- Payment webhook updates order status
- Inventory decrements correctly
- Shipping labels generated successfully
- Low stock warnings displayed
- Badges displayed correctly on products

---

### Team 5: Business Profile API Agent

**Primary Responsibility:** Build business profile microservice with approval workflow

**Requirements:**
- Business Owners membership type ($10/month paid, initial free with approval)
- Create/update business profile (poster, description, contact, goods/services)
- List goods/services with images, descriptions, prices (NOT purchasable on platform)
- Submit profile for approval
- Admin notification and approval workflow
- Separate `business` schema in shared database

**Tasks:**
1. **Create Repository** (`lankaconnect-business-profile-api`)
   - Initialize .NET 8 Web API project
   - Reference `lankaconnect-shared` NuGet package

2. **Design Domain Model**
   - Aggregates:
     - `BusinessProfile` (owner ID, name, description, category, contact, approval status)
     - `BusinessService` (name, description, price range, images)
     - `BusinessReview` (user ID, rating, comment, approved)
   - Value Objects:
     - `ContactInformation` (email, phone, website, address)
     - `PriceRange` (min, max, currency)
   - Enums:
     - `BusinessCategory` (Restaurant, Shop, Service, Professional, etc.)
     - `ApprovalStatus` (Draft, PendingApproval, Approved, Rejected)

3. **Database Schema** (`business` schema)
   ```sql
   CREATE SCHEMA business;

   CREATE TABLE business.profiles (
     id UUID PRIMARY KEY,
     owner_user_id UUID NOT NULL,
     business_name VARCHAR(200) NOT NULL,
     description TEXT,
     category VARCHAR(100) NOT NULL,
     poster_image_url VARCHAR(500),
     contact_email VARCHAR(255),
     contact_phone VARCHAR(20),
     website_url VARCHAR(500),
     address_line1 VARCHAR(255),
     address_line2 VARCHAR(255),
     city VARCHAR(100),
     state VARCHAR(50),
     postal_code VARCHAR(20),
     approval_status VARCHAR(50) NOT NULL DEFAULT 'Draft',
     approved_by_user_id UUID,
     approved_at TIMESTAMP,
     rejection_reason TEXT,
     is_published BOOLEAN DEFAULT false,
     published_at TIMESTAMP,
     created_at TIMESTAMP NOT NULL,
     updated_at TIMESTAMP
   );

   CREATE TABLE business.services (
     id UUID PRIMARY KEY,
     profile_id UUID REFERENCES business.profiles(id),
     name VARCHAR(200) NOT NULL,
     description TEXT,
     price_min DECIMAL(10, 2),
     price_max DECIMAL(10, 2),
     is_discounted BOOLEAN DEFAULT false,
     discount_percentage INT,
     discount_valid_until TIMESTAMP,
     display_order INT NOT NULL,
     created_at TIMESTAMP NOT NULL,
     updated_at TIMESTAMP
   );

   CREATE TABLE business.service_images (
     id UUID PRIMARY KEY,
     service_id UUID REFERENCES business.services(id),
     image_url VARCHAR(500) NOT NULL,
     display_order INT NOT NULL
   );

   CREATE TABLE business.reviews (
     id UUID PRIMARY KEY,
     profile_id UUID REFERENCES business.profiles(id),
     user_id UUID NOT NULL,
     rating INT NOT NULL CHECK (rating BETWEEN 1 AND 5),
     comment TEXT,
     is_approved BOOLEAN DEFAULT false,
     created_at TIMESTAMP NOT NULL
   );

   CREATE TABLE business.admin_notifications (
     id UUID PRIMARY KEY,
     profile_id UUID REFERENCES business.profiles(id),
     notification_type VARCHAR(50) NOT NULL,
     message TEXT NOT NULL,
     is_read BOOLEAN DEFAULT false,
     created_at TIMESTAMP NOT NULL
   );
   ```

4. **Implement Commands**
   - Profiles:
     - `CreateBusinessProfileCommand`
     - `UpdateBusinessProfileCommand`
     - `SubmitProfileForApprovalCommand`
     - `ApproveProfileCommand` (Admin only)
     - `RejectProfileCommand` (Admin only)
     - `PublishProfileCommand` (Business Owner, after approval)
   - Services:
     - `AddServiceCommand`
     - `UpdateServiceCommand`
     - `DeleteServiceCommand`
     - `ApplyDiscountToServiceCommand`

5. **Implement Queries**
   - `GetBusinessProfilesQuery` (with filters: category, city, state)
   - `GetBusinessProfileByIdQuery`
   - `GetProfilesByOwnerQuery`
   - `GetPendingApprovalsQuery` (Admin only)
   - `SearchBusinessesQuery`

6. **Implement API Controllers**
   - `BusinessProfilesController`:
     - GET /api/business/profiles (public, only approved & published)
     - GET /api/business/profiles/{id}
     - GET /api/business/my-profiles (Business Owner's own profiles)
     - POST /api/business/profiles
     - PUT /api/business/profiles/{id}
     - POST /api/business/profiles/{id}/submit (submit for approval)
     - POST /api/business/profiles/{id}/publish (publish after approval)
   - `BusinessServicesController`:
     - GET /api/business/profiles/{profileId}/services
     - POST /api/business/profiles/{profileId}/services
     - PUT /api/business/services/{id}
     - DELETE /api/business/services/{id}
     - POST /api/business/services/{id}/discount
   - `BusinessAdminController`:
     - GET /api/business/admin/pending-approvals (Admin only)
     - POST /api/business/admin/profiles/{id}/approve (Admin only)
     - POST /api/business/admin/profiles/{id}/reject (Admin only)

7. **Approval Workflow**
   - When profile submitted:
     - Set status to `PendingApproval`
     - Create notification for Admin/AdminManager
     - Send email to admins
   - Admin can approve or reject
   - On approval:
     - Set status to `Approved`
     - Notify Business Owner
     - Allow publishing
   - On rejection:
     - Set status to `Rejected`
     - Store rejection reason
     - Notify Business Owner
     - Allow resubmission after edits

8. **Business Owner Membership**
   - Assume User entity already has `MembershipType` field
   - Verify user has `BusinessOwner` role before allowing profile creation
   - Membership upgrade handled by existing user management system

9. **Testing**
   - Unit tests for domain logic
   - Integration tests for API endpoints
   - Test approval workflow end-to-end
   - Test authorization (only owner can edit profile, only admin can approve)

10. **Docker & Deployment**
    - Create Dockerfile
    - Update docker-compose.yml
    - Deploy to Azure Container Apps

**Deliverables:**
- [ ] `lankaconnect-business-profile-api` repository with full implementation
- [ ] Complete CRUD for profiles and services
- [ ] Approval workflow functional
- [ ] Admin notification system working
- [ ] Comprehensive tests (90%+ coverage)
- [ ] API documentation (Swagger)

**Success Criteria:**
- Business Owners can create profiles
- Can add/update goods/services
- Can submit for approval
- Admins receive notifications
- Admins can approve/reject
- Published profiles visible to public
- Only approved profiles can be published

---

### Team 6: Forum API Agent

**Primary Responsibility:** Build forum discussion microservice with bad word filtering

**Requirements:**
- Forums created by Admin/AdminManager
- Members can post, comment, reply
- Bad word filtering before publishing (dictionary + AI-based)
- Separate `forum` schema in shared database

**Tasks:**
1. **Create Repository** (`lankaconnect-forum-api`)
   - Initialize .NET 8 Web API project
   - Reference `lankaconnect-shared` NuGet package

2. **Design Domain Model**
   - Aggregates:
     - `Forum` (name, description, category, admin-created)
     - `ForumPost` (forum ID, author, title, content, moderation status)
     - `Comment` (post ID, author, content, parent comment ID for replies)
   - Enums:
     - `ModerationStatus` (Pending, Approved, Rejected, Flagged)
     - `ForumCategory` (Visa/Immigration, Green Card, Students, Jobs, Temples, Charity, etc.)

3. **Database Schema** (`forum` schema)
   ```sql
   CREATE SCHEMA forum;

   CREATE TABLE forum.forums (
     id UUID PRIMARY KEY,
     name VARCHAR(200) NOT NULL,
     description TEXT,
     category VARCHAR(100) NOT NULL,
     icon_url VARCHAR(500),
     post_count INT DEFAULT 0,
     is_active BOOLEAN DEFAULT true,
     created_by_user_id UUID NOT NULL,
     created_at TIMESTAMP NOT NULL
   );

   CREATE TABLE forum.posts (
     id UUID PRIMARY KEY,
     forum_id UUID REFERENCES forum.forums(id),
     author_user_id UUID NOT NULL,
     title VARCHAR(300) NOT NULL,
     content TEXT NOT NULL,
     moderation_status VARCHAR(50) NOT NULL DEFAULT 'Pending',
     rejection_reason TEXT,
     is_pinned BOOLEAN DEFAULT false,
     view_count INT DEFAULT 0,
     comment_count INT DEFAULT 0,
     created_at TIMESTAMP NOT NULL,
     updated_at TIMESTAMP
   );

   CREATE TABLE forum.comments (
     id UUID PRIMARY KEY,
     post_id UUID REFERENCES forum.posts(id),
     parent_comment_id UUID REFERENCES forum.comments(id),
     author_user_id UUID NOT NULL,
     content TEXT NOT NULL,
     moderation_status VARCHAR(50) NOT NULL DEFAULT 'Pending',
     rejection_reason TEXT,
     created_at TIMESTAMP NOT NULL,
     updated_at TIMESTAMP
   );

   CREATE TABLE forum.bad_words (
     id UUID PRIMARY KEY,
     word VARCHAR(100) NOT NULL UNIQUE,
     severity VARCHAR(20) NOT NULL,
     created_at TIMESTAMP NOT NULL
   );

   CREATE TABLE forum.moderation_logs (
     id UUID PRIMARY KEY,
     content_type VARCHAR(50) NOT NULL,  -- Post, Comment
     content_id UUID NOT NULL,
     moderator_user_id UUID,
     action VARCHAR(50) NOT NULL,
     reason TEXT,
     created_at TIMESTAMP NOT NULL
   );
   ```

4. **Implement Commands**
   - Forums:
     - `CreateForumCommand` (Admin only)
     - `UpdateForumCommand` (Admin only)
     - `ArchiveForumCommand` (Admin only)
   - Posts:
     - `CreatePostCommand` (auto-moderation triggered)
     - `UpdatePostCommand`
     - `DeletePostCommand`
     - `ApprovePostCommand` (Auto or Admin)
     - `RejectPostCommand` (Auto or Admin)
   - Comments:
     - `AddCommentCommand` (auto-moderation triggered)
     - `ReplyToCommentCommand` (auto-moderation triggered)
     - `UpdateCommentCommand`
     - `DeleteCommentCommand`
     - `ApproveCommentCommand`
     - `RejectCommentCommand`

5. **Implement Queries**
   - `GetForumsQuery`
   - `GetForumByIdQuery`
   - `GetPostsByForumQuery` (with pagination)
   - `GetPostByIdQuery`
   - `GetCommentsQuery` (hierarchical, with replies)
   - `GetPendingModerationItemsQuery` (Admin only)

6. **Implement API Controllers**
   - `ForumsController`:
     - GET /api/forum/forums
     - GET /api/forum/forums/{id}
     - POST /api/forum/forums (Admin only)
     - PUT /api/forum/forums/{id} (Admin only)
   - `PostsController`:
     - GET /api/forum/forums/{forumId}/posts
     - GET /api/forum/posts/{id}
     - POST /api/forum/forums/{forumId}/posts
     - PUT /api/forum/posts/{id}
     - DELETE /api/forum/posts/{id}
   - `CommentsController`:
     - GET /api/forum/posts/{postId}/comments
     - POST /api/forum/posts/{postId}/comments
     - POST /api/forum/comments/{id}/replies
     - PUT /api/forum/comments/{id}
     - DELETE /api/forum/comments/{id}
   - `ModerationController`:
     - GET /api/forum/moderation/pending (Admin only)
     - POST /api/forum/moderation/posts/{id}/approve (Admin only)
     - POST /api/forum/moderation/posts/{id}/reject (Admin only)

7. **Bad Word Filtering (Multi-Layer)**
   - **Layer 1: Dictionary-Based**
     - Maintain list of bad words in `forum.bad_words` table
     - Admin can add/remove words
     - Fast exact match and regex match
     - Block content immediately if exact match found

   - **Layer 2: AI-Based (Azure Content Moderator or OpenAI)**
     - Send content to Azure Content Moderator API
     - Analyze for:
       - Profanity
       - Hate speech
       - Sexually explicit content
       - Violence
     - If flagged, set to `Pending` moderation

   - **Layer 3: Manual Review**
     - Admin reviews flagged content
     - Can approve or reject with reason

   - **Implementation**:
     ```csharp
     public class ContentModerationService
     {
         public async Task<ModerationResult> ModerateContent(string content)
         {
             // Layer 1: Dictionary check
             if (ContainsBadWords(content))
                 return ModerationResult.Rejected("Contains inappropriate language");

             // Layer 2: AI check
             var aiResult = await CallAzureContentModerator(content);
             if (aiResult.IsFlagged)
                 return ModerationResult.PendingReview("Flagged by AI");

             return ModerationResult.Approved();
         }
     }
     ```

8. **Moderation Workflow**
   - When post/comment created:
     - Run content moderation
     - If rejected: Store with rejection reason, notify author
     - If pending review: Notify admins
     - If approved: Publish immediately
   - Admin can override AI decisions
   - Track moderation history in `moderation_logs`

9. **Testing**
   - Unit tests for bad word filtering
   - Integration tests for API endpoints
   - Test moderation workflow end-to-end
   - Test hierarchical comments (replies)

10. **Docker & Deployment**
    - Create Dockerfile
    - Update docker-compose.yml
    - Deploy to Azure Container Apps

**Deliverables:**
- [ ] `lankaconnect-forum-api` repository with full implementation
- [ ] Complete CRUD for forums, posts, comments
- [ ] Multi-layer bad word filtering functional
- [ ] Moderation workflow working
- [ ] Admin moderation panel
- [ ] Comprehensive tests (90%+ coverage)
- [ ] API documentation (Swagger)

**Success Criteria:**
- Admins can create forums
- Members can post and comment
- Bad words blocked automatically
- AI flags inappropriate content
- Admins can review flagged content
- Hierarchical comments display correctly
- Performance acceptable (sub-second response times)

---

### Team 7: Frontend Features Agent

**Primary Responsibility:** Build frontend pages for Marketplace, Business Profile, Forum

**Tasks:**
1. **Marketplace Pages**
   - Product catalog page (`/marketplace`)
   - Product detail page (`/marketplace/products/[id]`)
   - Shopping cart page (`/marketplace/cart`)
   - Checkout page (`/marketplace/checkout`)
   - Order history page (`/marketplace/orders`)
   - Admin product management page (`/admin/marketplace/products`)

2. **Business Profile Pages**
   - Business directory page (`/business`)
   - Business detail page (`/business/[id]`)
   - Create/edit business profile (`/business/my-profile`)
   - My businesses page (`/business/my-businesses`)
   - Admin approval queue (`/admin/business/approvals`)

3. **Forum Pages**
   - Forum list page (`/forum`)
   - Forum detail page (`/forum/[id]`)
   - Post detail page (`/forum/posts/[id]`)
   - Create post page (`/forum/[id]/new-post`)
   - Admin forum management (`/admin/forum/moderation`)

4. **Create API Repositories**
   - `marketplace.repository.ts`
   - `business-profile.repository.ts`
   - `forum.repository.ts`

5. **Update Navigation**
   - Add "Marketplace", "Business", "Forum" to main nav
   - Update routing

6. **Testing**
   - E2E tests for all pages
   - Test API integration
   - Test Stripe checkout flow

**Deliverables:**
- [ ] All frontend pages implemented
- [ ] API repositories created
- [ ] Navigation updated
- [ ] Comprehensive E2E tests

**Success Criteria:**
- All pages render correctly
- All features functional
- No console errors
- Performance acceptable

---

## 🔧 Shared Technologies & Standards

### Backend (.NET 8)
- Clean Architecture (Domain, Application, Infrastructure, API)
- CQRS with MediatR
- Repository pattern with Unit of Work
- FluentValidation for input validation
- AutoMapper for DTO mapping
- Entity Framework Core 8 with PostgreSQL
- Serilog for structured logging
- Swagger/OpenAPI for documentation

### Frontend (Next.js 16)
- React 19 with TypeScript
- Zustand for state management
- Axios for API calls
- TailwindCSS for styling
- React Hook Form for forms
- Zod for validation

### Database
- PostgreSQL 16 with PostGIS
- Schema-based separation
- Entity Framework Core migrations

### Infrastructure
- Docker for containerization
- Docker Compose for local development
- Azure Container Apps for deployment
- Azure Container Registry
- Azure Blob Storage for files
- Redis for caching
- Application Insights for monitoring

### CI/CD
- GitHub Actions for build/test/deploy
- Separate pipelines per service
- Automated testing on PR
- Blue-green deployment

---

## 📊 Success Metrics

### Phase 1 Success Criteria
- [ ] API Gateway deployed and functional
- [ ] Events API extracted and deployed
- [ ] Frontend calling API Gateway successfully
- [ ] All existing event features work correctly
- [ ] Zero downtime during migration
- [ ] Performance comparable to monolith

### Phase 2 Success Criteria
- [ ] All 3 new services deployed
- [ ] All new features fully functional
- [ ] Frontend integrated with all services
- [ ] 90%+ test coverage on all services
- [ ] Comprehensive API documentation
- [ ] All services independently deployable

### Overall Success Criteria
- [ ] Clean microservices architecture
- [ ] Each service has <5 second response time (p95)
- [ ] All services highly available (>99.9%)
- [ ] Comprehensive monitoring and logging
- [ ] Complete CI/CD pipelines
- [ ] Security audit passed

---

## 🎯 Coordination & Communication

### Daily Stand-ups
- Each team reports progress, blockers, next steps
- Coordinate on shared resources (database, API Gateway)

### Code Reviews
- Cross-team reviews for shared code
- Security reviews for auth/payment code

### Integration Testing
- Weekly integration tests across all services
- E2E tests covering critical user journeys

### Documentation
- Each team maintains README for their service
- API documentation auto-generated from Swagger
- Architecture decisions documented in ADRs

---

## 🚨 Risk Mitigation

### Risk 1: Database Schema Conflicts
- **Mitigation:** Each service has dedicated schema, coordinate on migrations

### Risk 2: Authentication Issues
- **Mitigation:** Thorough testing of JWT validation, token refresh

### Risk 3: Performance Degradation
- **Mitigation:** Load testing, monitoring, caching strategy

### Risk 4: Data Consistency
- **Mitigation:** Use domain events, eventual consistency patterns

### Risk 5: Deployment Failures
- **Mitigation:** Blue-green deployment, rollback plan, health checks

---

## 📝 Final Deliverables

By the end of this multi-agent coordination:
- [ ] 6 independent microservices running in production
- [ ] Single Next.js frontend calling all services via API Gateway
- [ ] Comprehensive test suites (unit, integration, E2E)
- [ ] Complete API documentation
- [ ] Deployment automation (CI/CD)
- [ ] Monitoring and alerting setup
- [ ] Security audit passed
- [ ] Performance benchmarks met
- [ ] All documentation up to date

---

## 🎉 READY TO LAUNCH!

Once this prompt is approved, spawn all agents in parallel using Claude Code's Task tool:

```
[Single Message - Phase 1 Parallel Execution]:
  Task("Infrastructure & API Gateway", "[Full Phase 1 Team 1 tasks]", "backend-dev")
  Task("Events API Extraction", "[Full Phase 1 Team 2 tasks]", "coder")
  Task("Frontend Integration", "[Full Phase 1 Team 3 tasks]", "coder")

  TodoWrite { todos: [10+ todos covering all Phase 1 tasks] }
```

Then after Phase 1 completion:

```
[Single Message - Phase 2 Parallel Execution]:
  Task("Marketplace API", "[Full Phase 2 Team 4 tasks]", "backend-dev")
  Task("Business Profile API", "[Full Phase 2 Team 5 tasks]", "backend-dev")
  Task("Forum API", "[Full Phase 2 Team 6 tasks]", "backend-dev")
  Task("Frontend Features", "[Full Phase 2 Team 7 tasks]", "coder")

  TodoWrite { todos: [15+ todos covering all Phase 2 tasks] }
```

---

## 🤝 Coordination Hooks

Each agent MUST use hooks for coordination:

**Before work:**
```bash
npx claude-flow@alpha hooks pre-task --description "[task-name]"
npx claude-flow@alpha hooks session-restore --session-id "microservices-migration"
```

**During work:**
```bash
npx claude-flow@alpha hooks post-edit --file "[file]" --memory-key "migration/[agent]/[step]"
npx claude-flow@alpha hooks notify --message "[progress-update]"
```

**After work:**
```bash
npx claude-flow@alpha hooks post-task --task-id "[task-id]"
npx claude-flow@alpha hooks session-end --export-metrics true
```

---

**END OF MULTI-AGENT COORDINATION PROMPT**
