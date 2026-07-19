# Geo Capability — Usage Guide (Phase B onboarding)

**Status:** LIVE (Wave 8.5 GAP-6 landed 2026-07-19)
**Audience:** Phase B first-slice teams — LankaHomes, LankaMart, LankaBusiness, LankaSeyla, LankaNivasa, LankaTemples.
**Related:** [`COMMON_COMPONENTS_INVENTORY_2026_07_16.md`](./COMMON_COMPONENTS_INVENTORY_2026_07_16.md) §4.2-4.5 GAP-6, [`ENTERPRISE_ARCHITECTURE_BLUEPRINT.md`](./ENTERPRISE_ARCHITECTURE_BLUEPRINT.md).

---

## 1. What this capability gives you

Three primitives, sitting in the SharedKernel + BuildingBlocks layer:

| Primitive | Package | Purpose |
|---|---|---|
| `Address` | `SharedKernel.Geo` | Postal address VO (Street, City, State, Zip, Country). |
| `GeoCoordinate` | `SharedKernel.Geo` | Lat/lng VO + `DistanceKmTo(other)` Haversine distance in km. |
| `WithinRadiusKm<T>(...)` | `BuildingBlocks.Application.Geo` | LINQ extension that filters `IEnumerable<T>` by radius. Also `WithinRadiusKmWithDistance<T>(...)` for nearest-first ordering. |
| `ContactInfo` | `SharedKernel.Contact` | Composite VO — Phone, Email, Website, PhysicalAddress. |

**When to reach for these:** any aggregate with "physical location" or "how to reach me" semantics. Every Phase B product except LankaNivasa's forum-only slice needs at least `GeoCoordinate + Address + ContactInfo`.

---

## 2. Add `GeoCoordinate` to a new aggregate

Model it as an owned VO on the aggregate. EF Core 8 handles the pair as two shadow columns (`Latitude`, `Longitude`) on the aggregate table — no separate table.

```csharp
using LankaConnect.SharedKernel.Geo;

public sealed class HomeListing : Entity<Guid>, IAuditable
{
    public Address Location { get; private set; }
    public GeoCoordinate Coordinates { get; private set; }
    // ...
}
```

**EF configuration** (in the module's `IEntityTypeConfiguration<HomeListing>`):

```csharp
builder.OwnsOne(x => x.Coordinates, coord =>
{
    coord.Property(c => c.Latitude).HasColumnName("latitude");
    coord.Property(c => c.Longitude).HasColumnName("longitude");
});

builder.OwnsOne(x => x.Location, addr =>
{
    addr.Property(a => a.Street).HasColumnName("street").HasMaxLength(200);
    addr.Property(a => a.City).HasColumnName("city").HasMaxLength(100);
    addr.Property(a => a.State).HasColumnName("state").HasMaxLength(50);
    addr.Property(a => a.ZipCode).HasColumnName("zip_code").HasMaxLength(20);
    addr.Property(a => a.Country).HasColumnName("country").HasMaxLength(100);
});
```

**Construction site (aggregate factory):**

```csharp
var geoResult = GeoCoordinate.Create(latitude: 43.6426m, longitude: -79.3871m);
if (geoResult.IsFailure) return Result<HomeListing>.Failure(geoResult.Error);

var addressResult = Address.Create("1 Main St", "Toronto", "ON", "M5V 3A8", "Canada");
if (addressResult.IsFailure) return Result<HomeListing>.Failure(addressResult.Error);

var listing = new HomeListing(id: Guid.NewGuid(),
                              coordinates: geoResult.Value,
                              location: addressResult.Value);
```

---

## 3. Run a radius query

Two shapes — the plain filter, and the tuple-with-distance projection.

### 3.1 Plain filter

```csharp
using LankaConnect.BuildingBlocks.Application.Geo;
using LankaConnect.SharedKernel.Geo;

public sealed class GetListingsNearMeHandler(IHomeListingRepository repo)
    : IQueryHandler<GetListingsNearMeQuery, Result<IReadOnlyList<HomeListingDto>>>
{
    public async Task<Result<IReadOnlyList<HomeListingDto>>> Handle(
        GetListingsNearMeQuery query,
        CancellationToken ct)
    {
        var center = new GeoCoordinate(query.Latitude, query.Longitude);

        // Step 1 — SQL prefilter: bounding-box (~2 * radiusKm degrees at equator; adjust for lat).
        // Keeps the in-memory set small even at Product scale.
        var candidates = await repo.GetCandidatesInBoundingBoxAsync(center, query.RadiusKm, ct);

        // Step 2 — precise Haversine filter in memory.
        var near = candidates
            .WithinRadiusKm(center, query.RadiusKm, listing => listing.Coordinates)
            .Select(l => l.ToDto())
            .ToList();

        return Result<IReadOnlyList<HomeListingDto>>.Success(near);
    }
}
```

**Why the bounding-box prefilter?** `WithinRadiusKm` is Haversine in-memory. If you `SELECT * FROM listings` and hand the whole table in, you pay for every row. A cheap `WHERE lat BETWEEN a AND b AND lon BETWEEN c AND d` in SQL cuts the working set to the geographic tile the query cares about before the Haversine refinement runs. The repo interface should expose a `GetCandidatesInBoundingBoxAsync` for exactly this.

### 3.2 Nearest-first with distance projection

```csharp
var sorted = candidates
    .WithinRadiusKmWithDistance(center, query.RadiusKm, listing => listing.Coordinates)
    .OrderBy(x => x.DistanceKm)
    .Take(query.PageSize)
    .Select(x => new HomeListingWithDistanceDto(x.Item.ToDto(), x.DistanceKm))
    .ToList();
```

The DTO now carries the km distance so the browse UI can render "0.8 km away".

---

## 4. Model a `ContactInfo` on a directory listing

Business, Home, Mart, Seyla, and Nivasa entries all publish some subset of contact channels. Use `ContactInfo` so the aggregate never has to null-check four separate scalars.

```csharp
using LankaConnect.SharedKernel.Contact;
using LankaConnect.SharedKernel.Geo;

public sealed class BusinessListing : Entity<Guid>, IAuditable
{
    public ContactInfo Contact { get; private set; }
    // ...
}
```

**EF configuration** — `ContactInfo` is an owned VO with a nested owned `Address`:

```csharp
builder.OwnsOne(x => x.Contact, contact =>
{
    contact.Property<string?>("PhoneValue").HasColumnName("contact_phone");
    contact.Property<string?>("EmailValue").HasColumnName("contact_email");
    contact.Property(c => c.Website).HasColumnName("contact_website");

    contact.OwnsOne(c => c.PhysicalAddress, addr =>
    {
        addr.Property(a => a.Street).HasColumnName("address_street");
        addr.Property(a => a.City).HasColumnName("address_city");
        addr.Property(a => a.State).HasColumnName("address_state");
        addr.Property(a => a.ZipCode).HasColumnName("address_zip");
        addr.Property(a => a.Country).HasColumnName("address_country");
    });
});
```

Phone/Email materialization via shadow properties + a constructor mapper is the typical EF pattern for VOs-with-factories; see the LankaEvents `Attendee` mapping for a worked example.

**Construction:**

```csharp
// String-overload gate — parses Phone + Email via their own factory.
var contact = ContactInfo.Create(
    phone: "+1-416-555-0100",
    email: "info@abc-restaurant.com",
    website: "https://abc-restaurant.com",
    physicalAddress: address);

if (contact.IsFailure) return Result<BusinessListing>.Failure(contact.Error);
```

**Partial cards are valid.** A listing that publishes only an email address is a legit ContactInfo. Only "no channels at all" fails construction.

---

## 5. Design constraints (don't skip)

- **In-memory only, Phase A.** `WithinRadiusKm` is a LINQ extension. It does NOT push down to SQL. Keep the in-memory set small via a bounding-box SQL prefilter. If your Product starts pushing >10k candidate rows per query, escalate to the architect for a PostGIS `ST_DWithin` implementation.
- **~0.5% accuracy.** Haversine assumes a spherical Earth. Fine for diaspora radius searches ("temples within 25 km", "properties within 10 mi"). NOT fine for surveying / property-boundary work.
- **No polymorphic interface yet.** `IGeoRadiusQuery<T>` was floated during Wave 8.5 GAP-6 design and deferred — no consumer needs polymorphic dispatch today. Wrap `WithinRadiusKm` behind an interface at your call site if you need mocking; the primitive is stable.
- **Never leak `Location` navigation into a Product's SharedKernel-side type.** SharedKernel packages MUST NOT reference Products or Modules. If you want to say "give me all Homes near X", the query lives in `LankaHomes.Application`, not in SharedKernel.

---

## 6. ArchTest guardrails

Enforced in `tests/architecture/LankaConnect.ArchitectureTests/LayeringRules.cs`:

- `SharedKernel_Geo_DependsOnlyOnBuildingBlocks` — Geo may reach only BuildingBlocks; never a Product / Module.
- `SharedKernel_Contact_DependsOnlyOnBuildingBlocksAndSharedKernelGeo` — Contact may reach BuildingBlocks + SharedKernel.Geo (for Address composition); nothing else.
- `BuildingBlocks_Application_Geo_HasNoProductOrModuleDependencies` — the `BuildingBlocks.Application.Geo` namespace stays product-agnostic; no Product / Module type may appear on its dependency graph.

If any of these turn red, the fix is to move the offending type OUT of SharedKernel / BuildingBlocks into the Product where it belongs — not to relax the ArchTest.

---

## 7. Where these primitives live in the tree

```
src/SharedKernel/SharedKernel.Geo/
├── Address.cs
├── GeoCoordinate.cs                  ← + DistanceKmTo (Haversine)
├── MetroAreas/Common/MetroAreaDto.cs
└── AssemblyMarker.cs

src/SharedKernel/SharedKernel.Contact/
├── Email.cs
├── PhoneNumber.cs
├── ContactInfo.cs                    ← NEW (Wave 8.5 GAP-6)
└── AssemblyMarker.cs

src/BuildingBlocks/BuildingBlocks.Application/Geo/
└── GeoRadiusExtensions.cs            ← NEW (Wave 8.5 GAP-6)

tests/SharedKernel/SharedKernel.Geo.Tests/
└── GeoCoordinateDistanceTests.cs

tests/SharedKernel/SharedKernel.Contact.Tests/
└── ContactInfoTests.cs

tests/LankaConnect.BuildingBlocks.Application.Tests/Geo/
└── GeoRadiusExtensionsTests.cs
```

---

## 8. Change history

- **2026-07-19** — Wave 8.5 GAP-6 landed. `DistanceKmTo` + `WithinRadiusKm` + `ContactInfo` added. This document authored.
- **2026-07-18** — Wave 8.5 LayerInversion promoted Address + GeoCoordinate to SharedKernel.Geo (commit `839fec4a`) and Email + PhoneNumber to SharedKernel.Contact (commit `d13e2b0b`).

Questions → Tech Lead (channel `docs/coordination/agents/gap-closure-geo.md`).
