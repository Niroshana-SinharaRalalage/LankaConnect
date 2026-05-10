# ADR-002: Tenancy Strategy for Commerce Module

| | |
|---|---|
| **Status** | Accepted (2026-04-26 — D2 resolved: one cart per storefront) |
| **Date** | 2026-04-26 |
| **Decision Owner** | Niroshana (Founder/Architect) |
| **Reviewers** | system-architect agent |
| **Supersedes** | — |
| **Related** | ADR-001 (i18n scope), Phase 3 plan (Commerce engine) |

## Context

The Commerce module powers three storefronts under a single domain: **LankaSeyla** (clothing), **LankaMart** (groceries), **LankaNivasa** (home goods). All three share identical mechanics:

- Cart, Orders, Inventory, Coupons, Shipping, Stripe Checkout
- Same backend infrastructure
- Same user accounts (one customer can shop all three)

They differ in:

- Catalog (saree ≠ saucepan)
- Branding / theming
- Shipping rules
- Tax rules (potentially per category)

The decision: how to model storefronts in the data layer? This is load-bearing for Phase 3 design. Getting it wrong means schema rework after launch.

## Options Considered

### Option A: Row-level multi-tenant with `storefront_id` (RECOMMENDED)

Single `commerce` schema. Every commerce table (`products`, `carts`, `orders`, `inventory`, `coupons`, `shipping_methods`) carries a `storefront_id` column. EF Core query filter ensures all queries are storefront-scoped automatically.

```csharp
public class CommerceDbContext : BaseDbContext {
    protected override void OnModelCreating(ModelBuilder b) {
        b.Entity<Product>().HasQueryFilter(p => p.StorefrontId == _currentStorefront.Id);
        // Applied to every commerce entity — leak protection
    }
}
```

### Option B: Schema-per-tenant

Three schemas: `commerce_seyla`, `commerce_mart`, `commerce_nivasa`. Each storefront has identical tables in its own schema. DbContext switches schema based on tenant.

### Option C: Database-per-tenant

Separate Postgres database per storefront. Maximum isolation; maximum operational cost.

### Option D: Shared product, multi-storefront association

Single `products` table; products have a `storefronts: text[]` array indicating where they appear. Queries filter on array membership.

## Decision

**Adopt Option A (Row-level multi-tenant with `storefront_id`).**

### Implementation specifics

- `StorefrontId` value object in `Commerce.Domain` (strong-typed, not raw `Guid`)
- Every Commerce aggregate root carries `StorefrontId`
- `CommerceDbContext` applies global query filter automatically using injected `ICurrentStorefrontAccessor`:
  ```csharp
  protected override void OnModelCreating(ModelBuilder b) {
      b.Entity<Product>().HasQueryFilter(p => p.StorefrontId == _storefrontAccessor.CurrentStorefrontId);
      b.Entity<Cart>().HasQueryFilter(c => c.StorefrontId == _storefrontAccessor.CurrentStorefrontId);
      // ... applied to every Commerce entity
  }
  ```
- `ICurrentStorefrontAccessor.CurrentStorefrontId` resolved per-request from path (`/lanka-seyla/...` → `storefronts.seyla`)
- **Cart scoping**: ONE cart per storefront per user (a customer can have a Seyla cart AND a Mart cart simultaneously). Cart entity has unique constraint on `(user_id, storefront_id)`. This is product-decision, not just architecture.
- Stripe metadata tags every charge with `storefront_id` for reconciliation (per ADR-003)
- Admin UIs require explicit storefront context switcher
- Cross-storefront queries (analytics, fraud detection, revenue rollup) bypass query filter explicitly via `IgnoreQueryFilters()`
- **Audit log**: every Commerce mutation writes to `platform.audit_events` (defined in BuildingBlocks W2.4) with `storefront_id` field for per-storefront audit trails

## Consequences

### Positive

- Single schema, single migration history, single ops surface — solo founder economics
- Adding storefront 4, 5, ...N is a config row insert, no schema change
- Cross-storefront analytics trivial (no schema joins)
- One Stripe account, one Stripe dashboard, one reconciliation job (per ADR-003)
- Customer with one account can shop all three storefronts seamlessly
- Storefront-scoped data isolation enforced at ORM layer (defense in depth)
- Industry-standard pattern (Shopify, BigCommerce internal model)

### Negative / Trade-offs

- Bug in EF query filter = potential data leak across storefronts. Mitigated by:
  - ArchTest rule requiring `HasQueryFilter` on every Commerce entity
  - Integration test per entity verifying isolation
  - Code review checklist item for any new Commerce query
- A single noisy storefront (e.g., Mart flash sale) impacts other storefronts at the DB level. Mitigated by:
  - Phase B per-module container split (Commerce gets its own ACA replica scaling)
  - Postgres connection pool sized for peak storefront, not sum
  - Read replica for analytics queries

### Risks

- **Risk: Forgetting query filter on a new entity.** Mitigation: custom test (NOT NetArchTest — that doesn't introspect EF Core configuration) boots `CommerceDbContext` and reflects on `Model.GetEntityTypes()` to assert each Commerce entity has `IQueryFilter != null`. Implementation:
  ```csharp
  // tests/architecture/CommerceTenancyTests.cs
  [Fact]
  public void Every_commerce_entity_has_storefront_query_filter() {
      using var ctx = new CommerceDbContext(testOptions);
      var entityTypes = ctx.Model.GetEntityTypes()
          .Where(t => t.ClrType.Namespace.StartsWith("LankaConnect.Modules.Commerce.Domain"));
      foreach (var et in entityTypes) {
          Assert.NotNull(et.GetQueryFilter());
      }
  }
  ```
- **Risk: Cross-storefront customer order pollution.** Mitigation: orders explicitly carry storefront; per the cart scoping decision above, customers have one cart per storefront — no switch-and-clear semantics needed.
- **Risk: Tenant isolation audit (e.g., regulatory review).** Mitigation: Audit log (`platform.audit_events`) includes storefront_id on every action; reports filterable per storefront.

## Rejected Alternatives

- **Option B (schema-per-tenant)**: 3× migration overhead per change. Rejected — solo founder cannot absorb the multiplier.
- **Option C (DB-per-tenant)**: Maximum isolation, maximum cost. Appropriate for regulated/compliance scenarios. Not warranted here.
- **Option D (shared products with array membership)**: Conflates products that should be separate. A saree and a saucepan are not the same product just because they're both sellable; storefront identity is intrinsic to merchandising.

## Migration Path to Stronger Isolation (if ever needed)

- If one storefront grows to demand its own DB: extract to schema-per-tenant first (cheap), then DB-per-tenant. The row-level model is the floor, not the ceiling.

## References

- Architect review: 2026-04-26 (Question 1 — Tenancy decision flagged as load-bearing)
- Phase 3 commerce engine plan
