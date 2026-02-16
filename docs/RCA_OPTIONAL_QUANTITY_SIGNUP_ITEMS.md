# Root Cause Analysis: Optional Quantity for Sign-Up Items

**Issue Date:** 2026-02-16
**Issue Reporter:** User
**Severity:** Feature Gap (Medium Priority)
**Status:** Analysis Complete - Awaiting Implementation Approval

---

## Executive Summary

The current signup lists feature requires ALL items to have a specific numeric quantity (min: 1, max: 1000). This creates a usability limitation for real-world scenarios where items cannot be precisely quantified (e.g., "Assorted Fruits", "Miscellaneous Snacks", "Whatever you can bring").

**Classification:** This is a **Feature Gap** requiring architectural enhancements across all layers (Domain → Database → API → UI).

**Impact Scope:**
- **Domain Layer:** Core validation logic
- **Database Schema:** Column constraints and data types
- **Backend API:** DTOs, validation, and business logic
- **Frontend UI:** Form controls and display patterns
- **User Experience:** Organizer workflow and attendee perception

---

## 1. Classification

### Issue Type: ✅ Feature Gap (Missing Functionality)

**Analysis:**
- ❌ **Not a UI issue** - This is not just a display problem
- ❌ **Not an auth issue** - No permissions/security concerns
- ❌ **Not a backend API issue** - Logic works as designed
- ❌ **Not just a database schema issue** - Requires design decisions beyond schema
- ✅ **Feature gap** - System lacks support for a legitimate business requirement

**Reasoning:**
The system was intentionally designed to require numeric quantities for inventory management. However, real-world use cases reveal that this design assumption is too restrictive. Events often need items where:
1. Exact quantities are unknown until collection
2. Flexible contributions are encouraged
3. "Bring what you can" is more appropriate than fixed numbers

---

## 2. Impact Analysis by Architectural Layer

### 2.1 Domain Layer (`LankaConnect.Domain.Events.Entities`)

**Current Implementation:**
```csharp
// SignUpItem.cs (Line 18-19, 39, 69-73)
public int Quantity { get; private set; }           // Non-nullable, required
public int RemainingQuantity { get; private set; }  // Non-nullable, required

// Validation in Create() method
if (quantity <= 0)
    return Result<SignUpItem>.Failure("Quantity must be greater than 0");

if (quantity > 1000)
    return Result<SignUpItem>.Failure("Quantity cannot exceed 1000");
```

**Impact:**
- **Invariant Violation:** Domain enforces "Quantity must be > 0" at creation
- **Business Logic:** RemainingQuantity calculations depend on numeric math
- **Commitment Logic:** `AddCommitment()` validates `commitQuantity > RemainingQuantity`

**Required Changes:**
1. Make `Quantity` nullable: `int?` or introduce "unlimited" sentinel value
2. Update validation logic to allow null/unlimited quantities
3. Modify commitment validation for unlimited items
4. Add new methods: `IsUnlimitedQuantity()`, `GetDisplayQuantity()`

---

### 2.2 Database Schema (`events.sign_up_items` table)

**Current Schema (Migration: `20251129201535_AddSignUpItemCategorySupport.cs`):**
```sql
CREATE TABLE events.sign_up_items (
    id uuid PRIMARY KEY,
    sign_up_list_id uuid NOT NULL,
    item_description varchar(200) NOT NULL,
    quantity integer NOT NULL,              -- ❌ NOT NULL constraint
    remaining_quantity integer NOT NULL,    -- ❌ NOT NULL constraint
    item_category integer NOT NULL,
    notes varchar(500),
    created_at timestamp NOT NULL,
    updated_at timestamp
);
```

**EF Core Configuration (`SignUpItemConfiguration.cs`):**
```csharp
builder.Property(si => si.Quantity)
    .HasColumnName("quantity")
    .IsRequired();  // ❌ EF Core enforces non-null
```

**Impact:**
- **Data Constraint:** PostgreSQL schema enforces `NOT NULL` on quantity columns
- **Migration Required:** ALTER TABLE to allow NULL values
- **Data Consistency:** Existing records (all have numeric quantities) remain valid
- **Index Performance:** No impact (quantity not indexed)

**Required Changes:**
1. Create migration to `ALTER COLUMN quantity DROP NOT NULL`
2. Consider: Keep `remaining_quantity` non-null (use -1 for unlimited)
3. Update EF Core configuration: `IsRequired(false)`

---

### 2.3 Backend API Layer

**Current DTOs (`SignUpItemDto` - events.types.ts lines 454-468):**
```typescript
export interface SignUpItemDto {
  id: string;
  itemDescription: string;
  quantity: number;              // ❌ Non-nullable number
  remainingQuantity: number;     // ❌ Non-nullable number
  itemCategory: SignUpItemCategory;
  notes?: string | null;
  commitments: SignUpCommitmentDto[];
  isFullyCommitted: boolean;     // ❌ Cannot be computed for unlimited
  committedQuantity: number;     // ❌ Loses meaning for unlimited
}
```

**API Validation (`AddSignUpItemRequest`, `UpdateSignUpItemRequest`):**
```csharp
// Backend validators enforce quantity > 0
public class AddSignUpItemRequestValidator : AbstractValidator<AddSignUpItemRequest>
{
    RuleFor(x => x.Quantity)
        .GreaterThan(0).WithMessage("Quantity must be greater than 0")
        .LessThanOrEqualTo(1000).WithMessage("Quantity cannot exceed 1000");
}
```

**Impact:**
- **API Contract Changes:** Breaking change to DTOs
- **Validation Logic:** Need conditional validation (if quantity specified → validate range)
- **Response Serialization:** Null handling in JSON responses

**Required Changes:**
1. Update DTOs: `quantity?: number | null`
2. Add `isUnlimitedQuantity: boolean` flag
3. Update validators to allow null quantity
4. Update mappers/projectors in Application layer

---

### 2.4 Frontend UI Layer

**Current UI (`SignUpManagementSection.tsx` lines 681-683):**
```tsx
<span className="text-xs bg-blue-100 text-blue-800 px-2 py-1 rounded-full font-semibold">
  Suggested Quantity: {item.quantity}  {/* ❌ Assumes always numeric */}
</span>
```

**Form Validation (Create/Edit Sign-Up Items):**
```tsx
// Current: Quantity field is required numeric input
<input
  type="number"
  min="1"
  max="1000"
  required  // ❌ Always required
  value={quantity}
/>
```

**Impact:**
- **Form Controls:** Need checkbox/toggle for "Unlimited quantity"
- **Display Logic:** Show "∞" or "As many as you can bring" instead of number
- **Progress Tracking:** Cannot show "5 of ∞ committed" - UX challenge
- **Commitment Modal:** User input changes (numeric vs text acknowledgment)

**Required Changes:**
1. Add "Unlimited quantity" checkbox in item creation form
2. Conditionally show/hide numeric input based on checkbox
3. Update display logic to handle null quantities
4. Design UX for committing to unlimited items

---

## 3. Architectural Design Patterns (Evaluation)

### Option A: Nullable Quantity (Recommended ✅)

**Design:**
```csharp
public int? Quantity { get; private set; }  // NULL = unlimited

public bool IsUnlimitedQuantity() => Quantity == null;

public int GetDisplayQuantity() => Quantity ?? -1;  // -1 for display as "∞"
```

**Database:**
```sql
ALTER TABLE events.sign_up_items
  ALTER COLUMN quantity DROP NOT NULL,
  ALTER COLUMN remaining_quantity DROP NOT NULL;
```

**DTOs:**
```typescript
interface SignUpItemDto {
  quantity: number | null;          // NULL = unlimited
  remainingQuantity: number | null; // NULL = unlimited
  isUnlimitedQuantity: boolean;     // Computed flag
}
```

**Pros:**
- ✅ Semantically clear: NULL = "no limit"
- ✅ No magic numbers/sentinel values
- ✅ Database-native approach (NULL is standard SQL)
- ✅ Minimal code changes (nullable types are well-supported)
- ✅ Backward compatible (existing items keep numeric quantities)

**Cons:**
- ⚠️ Requires null checks throughout codebase
- ⚠️ Commitment validation logic becomes conditional

---

### Option B: Boolean Flag + Quantity (Verbose)

**Design:**
```csharp
public bool IsUnlimited { get; private set; }
public int Quantity { get; private set; }  // Ignored if IsUnlimited=true
```

**Pros:**
- ✅ Explicit intent (IsUnlimited flag is self-documenting)
- ✅ No null checks needed

**Cons:**
- ❌ Redundant data (quantity meaningless when IsUnlimited=true)
- ❌ Data integrity risk (both could be set incorrectly)
- ❌ More complex validation (must ensure consistency)

---

### Option C: Sentinel Value (Anti-Pattern ❌)

**Design:**
```csharp
public int Quantity { get; private set; }  // -1 or 0 = unlimited
```

**Pros:**
- ✅ No schema changes needed
- ✅ No null handling

**Cons:**
- ❌ Magic number anti-pattern (poor readability)
- ❌ Confusing: What does -1 mean without context?
- ❌ Validation complexity (must special-case sentinel value)
- ❌ Risk of arithmetic errors (sum quantities = unexpected results)

---

### Option D: Item Type Enum (Over-Engineering)

**Design:**
```csharp
public enum QuantityType { Quantified, Unquantified }
public QuantityType QuantityType { get; private set; }
public int? Quantity { get; private set; }
```

**Cons:**
- ❌ Adds unnecessary complexity
- ❌ Enum + nullable int is redundant (Option A is simpler)

---

## 4. UX Design Recommendations

### 4.1 Organizer Experience (Create/Edit Item)

**Scenario 1: Creating Mandatory Item**
```
┌─────────────────────────────────────────────┐
│ Add Mandatory Item                          │
├─────────────────────────────────────────────┤
│ Item Description: *                         │
│ ┌─────────────────────────────────────────┐ │
│ │ Chicken Curry                           │ │
│ └─────────────────────────────────────────┘ │
│                                             │
│ ☐ Flexible quantity (accept any amount)    │  <-- NEW CHECKBOX
│                                             │
│ Quantity: *                                 │
│ ┌───────┐                                   │
│ │   5   │                                   │  <-- Disabled if checkbox checked
│ └───────┘                                   │
│                                             │
│ Notes:                                      │
│ ┌─────────────────────────────────────────┐ │
│ │ Enough for 5 people                     │ │
│ └─────────────────────────────────────────┘ │
└─────────────────────────────────────────────┘
```

**Scenario 2: Flexible Quantity Item**
```
┌─────────────────────────────────────────────┐
│ Add Suggested Item                          │
├─────────────────────────────────────────────┤
│ Item Description: *                         │
│ ┌─────────────────────────────────────────┐ │
│ │ Assorted Fruits                         │ │
│ └─────────────────────────────────────────┘ │
│                                             │
│ ☑ Flexible quantity (accept any amount)    │  <-- CHECKED
│                                             │
│ Quantity:                                   │
│ ┌───────┐ (disabled)                        │
│ │   -   │                                   │  <-- Grayed out
│ └───────┘                                   │
│                                             │
│ Notes:                                      │
│ ┌─────────────────────────────────────────┐ │
│ │ Bring whatever fresh fruits you can     │ │
│ └─────────────────────────────────────────┘ │
└─────────────────────────────────────────────┘
```

---

### 4.2 Attendee Experience (Public View)

**Quantified Item Display:**
```
┌─────────────────────────────────────────────────┐
│ 🍛 Chicken Curry                                │
│ Suggested Quantity: 5                           │
│ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ │
│ ████████████░░░░░░░░░░░░░░░░░░ 60%             │
│ 3 of 5 filled • 2 remaining                     │
│                                                 │
│ Participants:                                   │
│ • Sarah Johnson (2)                             │
│ • Mike Chen (1)                                 │
│                                                 │
│ [Sign Up] [View Details]                        │
└─────────────────────────────────────────────────┘
```

**Unlimited Item Display (Option 1: Count-Based):**
```
┌─────────────────────────────────────────────────┐
│ 🍎 Assorted Fruits                              │
│ Flexible Quantity • Bring what you can          │
│ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ │
│ ✓ 4 people committed                            │  <-- No "remaining"
│                                                 │
│ Participants:                                   │
│ • Sarah Johnson (some)                          │
│ • Mike Chen (some)                              │
│ • Lisa Park (some)                              │
│ • John Smith (some)                             │
│                                                 │
│ [Sign Up] [View Details]                        │
└─────────────────────────────────────────────────┘
```

**Unlimited Item Display (Option 2: Detailed Commitments):**
```
┌─────────────────────────────────────────────────┐
│ 🍪 Homemade Desserts                            │
│ Flexible Quantity • As many as you want         │
│ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ │
│ ✓ 3 people committed                            │
│                                                 │
│ Participants:                                   │
│ • Sarah - "Chocolate chip cookies"              │
│ • Mike - "Brownies and cupcakes"                │
│ • Lisa - "Apple pie"                            │
│                                                 │
│ [Sign Up] [View Details]                        │
└─────────────────────────────────────────────────┘
```

---

### 4.3 Commitment Modal (How Users Sign Up)

**For Quantified Items (Existing UX - No Change):**
```
┌─────────────────────────────────────────────┐
│ Sign Up for: Chicken Curry                  │
├─────────────────────────────────────────────┤
│ Available: 2 remaining out of 5              │
│                                             │
│ How many will you bring? *                  │
│ ┌───────┐                                   │
│ │   2   │ [▲] [▼]                           │
│ └───────┘                                   │
│                                             │
│ Your Name:                                  │
│ ┌─────────────────────────────────────────┐ │
│ │ Sarah Johnson                           │ │
│ └─────────────────────────────────────────┘ │
│                                             │
│ Notes (optional):                           │
│ ┌─────────────────────────────────────────┐ │
│ │ Enough for 10 people                    │ │
│ └─────────────────────────────────────────┘ │
│                                             │
│          [Cancel]  [Confirm Sign Up]        │
└─────────────────────────────────────────────┘
```

**For Unlimited Items (New UX - Option A: Simple Acknowledgment):**
```
┌─────────────────────────────────────────────┐
│ Sign Up for: Assorted Fruits                │
├─────────────────────────────────────────────┤
│ This item has flexible quantity - bring     │
│ whatever you can!                           │
│                                             │
│ ☑ I commit to bringing this item            │  <-- Simple checkbox
│                                             │
│ Your Name:                                  │
│ ┌─────────────────────────────────────────┐ │
│ │ Sarah Johnson                           │ │
│ └─────────────────────────────────────────┘ │
│                                             │
│ What will you bring? (optional):            │
│ ┌─────────────────────────────────────────┐ │
│ │ A variety of fresh fruits from the      │ │
│ │ farmer's market                         │ │
│ └─────────────────────────────────────────┘ │
│                                             │
│          [Cancel]  [Confirm Sign Up]        │
└─────────────────────────────────────────────┘
```

**For Unlimited Items (New UX - Option B: Quantity + Notes):**
```
┌─────────────────────────────────────────────┐
│ Sign Up for: Assorted Fruits                │
├─────────────────────────────────────────────┤
│ This item has flexible quantity             │
│                                             │
│ Approximate quantity (optional):            │
│ ┌───────┐                                   │
│ │   5   │ (e.g., 5 plates, 5 servings)     │
│ └───────┘                                   │
│                                             │
│ Your Name:                                  │
│ ┌─────────────────────────────────────────┐ │
│ │ Sarah Johnson                           │ │
│ └─────────────────────────────────────────┘ │
│                                             │
│ What will you bring?:                       │
│ ┌─────────────────────────────────────────┐ │
│ │ 5 pounds of apples, oranges, grapes     │ │
│ └─────────────────────────────────────────┘ │
│                                             │
│          [Cancel]  [Confirm Sign Up]        │
└─────────────────────────────────────────────┘
```

**Recommendation:** Use **Option B** for unlimited items:
- Allows users to specify approximate quantity if they want
- Maintains consistency with quantified items (similar form structure)
- Notes field captures specific details
- Backend stores quantity as NULL, but UI shows "Approx: 5" if user enters it

---

## 5. Edge Cases & Considerations

### 5.1 Data Consistency

**Edge Case 1: What if someone commits "100" to an unlimited item?**
- **Scenario:** Item has no quantity limit, but user commits to "100 servings"
- **Solution:** Store commitment quantity normally, but don't decrement RemainingQuantity
- **Display:** Show "Sarah committed to ~100" without affecting overall progress

**Edge Case 2: Can an unlimited item be changed to quantified later?**
- **Scenario:** Organizer creates "Bring what you can", later wants to cap at 10
- **Solution:** Allow conversion, but warn if existing commitments exceed new limit
- **Validation:** If sum(commitments) > new quantity, show error or auto-adjust

**Edge Case 3: Partial fulfillment tracking**
- **Scenario:** Unlimited item has 5 commitments. How do we know if it's "enough"?
- **Solution:** Organizer sees commitment count, not quantity filled
- **Display:** "5 people committed" instead of "5 of 10 filled"

---

### 5.2 Migration Strategy

**Backward Compatibility:**
- ✅ All existing items have numeric quantities → no data loss
- ✅ Database migration is safe (ADD NULL capability, don't change data)
- ✅ API clients using old DTOs will see `quantity: null` → graceful degradation

**Migration Steps:**
1. **Database:** ALTER TABLE to allow NULL (no data changes)
2. **Backend:** Update domain entities, validators, DTOs
3. **Frontend:** Update UI components, forms, display logic
4. **Testing:** Verify both quantified and unlimited items work

**Rollback Plan:**
- If rolled back, unlimited items (quantity=NULL) would cause validation errors
- **Mitigation:** Before rollback, run script to set NULL quantities to 999 (max allowed)

---

### 5.3 Progress Tracking for Unlimited Items

**Challenge:** How do we show "progress" when there's no target?

**Option 1: Count-Based (Recommended)**
```
✓ 4 people committed
```

**Option 2: Qualitative Indicator**
```
Interest Level: ●●●○○ (3 commitments)
```

**Option 3: Organizer-Defined Soft Goal**
```
Organizer's Note: "Hoping for at least 3 people"
Commitments: 5 ✓ Goal exceeded!
```

---

## 6. Recommended Solution: Detailed Implementation Plan

### 6.1 Phase 1: Domain Layer Changes

**File:** `src/LankaConnect.Domain/Events/Entities/SignUpItem.cs`

**Changes:**
```csharp
// Line 18-20: Make quantities nullable
public int? Quantity { get; private set; }
public int? RemainingQuantity { get; private set; }

// Add helper methods
public bool IsUnlimitedQuantity() => Quantity == null;

public int GetDisplayQuantity() => Quantity ?? -1;

public string GetQuantityDisplay() =>
    Quantity.HasValue ? Quantity.Value.ToString() : "Flexible";

// Update Create() validation (Line 69-73)
public static Result<SignUpItem> Create(
    Guid signUpListId,
    string itemDescription,
    int? quantity,  // <-- Now nullable
    SignUpItemCategory itemCategory,
    string? notes = null)
{
    // ... existing validations ...

    // NEW: Allow null quantity (unlimited)
    if (quantity.HasValue)
    {
        if (quantity.Value <= 0)
            return Result<SignUpItem>.Failure("Quantity must be greater than 0");

        if (quantity.Value > 1000)
            return Result<SignUpItem>.Failure("Quantity cannot exceed 1000");
    }

    var item = new SignUpItem(
        signUpListId,
        itemDescription.Trim(),
        quantity,
        itemCategory,
        notes?.Trim(),
        createdByUserId: null);

    return Result<SignUpItem>.Success(item);
}

// Update AddCommitment() validation (Line 141-142)
public Result AddCommitment(
    Guid userId,
    int commitQuantity,
    string? commitNotes = null,
    string? contactName = null,
    string? contactEmail = null,
    string? contactPhone = null)
{
    // ... existing validations ...

    // NEW: For unlimited items, allow any quantity
    if (RemainingQuantity.HasValue && commitQuantity > RemainingQuantity.Value)
        return Result.Failure($"Cannot commit {commitQuantity}. Only {RemainingQuantity.Value} remaining");

    // ... rest of method ...

    // NEW: Only decrement if quantified
    if (RemainingQuantity.HasValue)
        RemainingQuantity -= commitQuantity;

    // ... rest of method ...
}
```

**Test Coverage Required:**
1. Create unlimited item (quantity=null)
2. Commit to unlimited item (no remaining quantity check)
3. Create quantified item (existing behavior)
4. Validate quantity range (1-1000) when provided

---

### 6.2 Phase 2: Database Migration

**File:** `src/LankaConnect.Infrastructure/Data/Migrations/YYYYMMDD_MakeSignUpItemQuantityNullable.cs`

```csharp
public partial class MakeSignUpItemQuantityNullable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Allow NULL for quantity and remaining_quantity
        migrationBuilder.AlterColumn<int>(
            name: "quantity",
            schema: "events",
            table: "sign_up_items",
            type: "integer",
            nullable: true,  // <-- Change from false to true
            oldClrType: typeof(int),
            oldType: "integer",
            oldNullable: false);

        migrationBuilder.AlterColumn<int>(
            name: "remaining_quantity",
            schema: "events",
            table: "sign_up_items",
            type: "integer",
            nullable: true,  // <-- Change from false to true
            oldClrType: typeof(int),
            oldType: "integer",
            oldNullable: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Rollback: Set NULL values to 999 before making NOT NULL
        migrationBuilder.Sql(
            @"UPDATE events.sign_up_items
              SET quantity = 999, remaining_quantity = 999
              WHERE quantity IS NULL OR remaining_quantity IS NULL");

        migrationBuilder.AlterColumn<int>(
            name: "quantity",
            schema: "events",
            table: "sign_up_items",
            type: "integer",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "integer",
            oldNullable: true);

        migrationBuilder.AlterColumn<int>(
            name: "remaining_quantity",
            schema: "events",
            table: "sign_up_items",
            type: "integer",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "integer",
            oldNullable: true);
    }
}
```

**Update EF Core Configuration:**

**File:** `src/LankaConnect.Infrastructure/Data/Configurations/SignUpItemConfiguration.cs`

```csharp
builder.Property(si => si.Quantity)
    .HasColumnName("quantity")
    .IsRequired(false);  // <-- Change from IsRequired() to IsRequired(false)

builder.Property(si => si.RemainingQuantity)
    .HasColumnName("remaining_quantity")
    .IsRequired(false);  // <-- Change from IsRequired() to IsRequired(false)
```

---

### 6.3 Phase 3: Backend API Changes

**File:** `src/LankaConnect.Application/Events/Common/SignUpListDto.cs`

```csharp
public class SignUpItemDto
{
    public Guid Id { get; set; }
    public string ItemDescription { get; set; } = string.Empty;
    public int? Quantity { get; set; }  // <-- Now nullable
    public int? RemainingQuantity { get; set; }  // <-- Now nullable
    public SignUpItemCategory ItemCategory { get; set; }
    public string? Notes { get; set; }
    public List<SignUpCommitmentDto> Commitments { get; set; } = new();

    // Computed properties
    public bool IsFullyCommitted => RemainingQuantity.HasValue && RemainingQuantity.Value == 0;
    public int CommittedQuantity => Quantity.HasValue && RemainingQuantity.HasValue
        ? Quantity.Value - RemainingQuantity.Value
        : 0;
    public bool IsUnlimitedQuantity => !Quantity.HasValue;  // NEW

    // ... rest of properties ...
}
```

**File:** `src/LankaConnect.Application/Events/Commands/AddSignUpItem/AddSignUpItemCommandValidator.cs`

```csharp
public class AddSignUpItemRequestValidator : AbstractValidator<AddSignUpItemRequest>
{
    public AddSignUpItemRequestValidator()
    {
        RuleFor(x => x.ItemDescription)
            .NotEmpty().WithMessage("Item description is required")
            .MaximumLength(200);

        // NEW: Conditional quantity validation
        RuleFor(x => x.Quantity)
            .GreaterThan(0).When(x => x.Quantity.HasValue)
            .WithMessage("Quantity must be greater than 0 when specified")
            .LessThanOrEqualTo(1000).When(x => x.Quantity.HasValue)
            .WithMessage("Quantity cannot exceed 1000");

        RuleFor(x => x.Notes)
            .MaximumLength(500);
    }
}
```

**TypeScript Types:**

**File:** `web/src/infrastructure/api/types/events.types.ts`

```typescript
export interface SignUpItemDto {
  id: string;
  itemDescription: string;
  quantity: number | null;  // <-- Changed from number to number | null
  remainingQuantity: number | null;  // <-- Changed from number to number | null
  itemCategory: SignUpItemCategory;
  notes?: string | null;
  commitments: SignUpCommitmentDto[];
  isFullyCommitted: boolean;
  committedQuantity: number;
  isUnlimitedQuantity: boolean;  // <-- NEW computed flag
  createdByUserId?: string | null;
  isOpenItem: boolean;
}

export interface SignUpItemRequestDto {
  itemDescription: string;
  quantity: number | null;  // <-- Changed from number to number | null
  itemCategory: SignUpItemCategory;
  notes?: string | null;
}
```

---

### 6.4 Phase 4: Frontend UI Changes

**File:** `web/src/presentation/components/features/events/SignUpManagementSection.tsx`

**Change 1: Display Quantity (Lines 681-683)**
```tsx
{/* OLD */}
<span className="text-xs bg-blue-100 text-blue-800 px-2 py-1 rounded-full font-semibold">
  Suggested Quantity: {item.quantity}
</span>

{/* NEW */}
<span className="text-xs bg-blue-100 text-blue-800 px-2 py-1 rounded-full font-semibold">
  {item.isUnlimitedQuantity
    ? 'Flexible Quantity'
    : `Suggested Quantity: ${item.quantity}`
  }
</span>
```

**Change 2: Progress Display (Lines 688-694)**
```tsx
{/* OLD */}
<div className="text-xs text-muted-foreground mt-1 flex gap-3">
  <span>{item.committedQuantity} of {item.quantity} filled</span>
  <span className={remainingQty === 0 ? 'text-green-600 font-medium' : ''}>
    {remainingQty} remaining
  </span>
</div>

{/* NEW */}
<div className="text-xs text-muted-foreground mt-1 flex gap-3">
  {item.isUnlimitedQuantity ? (
    <span>✓ {item.commitments.length} people committed</span>
  ) : (
    <>
      <span>{item.committedQuantity} of {item.quantity} filled</span>
      <span className={remainingQty === 0 ? 'text-green-600 font-medium' : ''}>
        {remainingQty} remaining
      </span>
    </>
  )}
</div>
```

**Change 3: Create/Edit Item Form (NEW COMPONENT)**

**File:** `web/src/presentation/components/features/events/SignUpItemForm.tsx`

```tsx
export function SignUpItemForm({ onSubmit, initialValues }) {
  const [itemDescription, setItemDescription] = useState(initialValues?.itemDescription || '');
  const [isUnlimited, setIsUnlimited] = useState(initialValues?.quantity === null);
  const [quantity, setQuantity] = useState(initialValues?.quantity || 1);
  const [notes, setNotes] = useState(initialValues?.notes || '');

  return (
    <form onSubmit={(e) => {
      e.preventDefault();
      onSubmit({
        itemDescription,
        quantity: isUnlimited ? null : quantity,
        notes,
      });
    }}>
      {/* Item Description */}
      <div className="mb-4">
        <label className="block text-sm font-medium mb-1">
          Item Description *
        </label>
        <input
          type="text"
          value={itemDescription}
          onChange={(e) => setItemDescription(e.target.value)}
          required
          className="w-full px-3 py-2 border rounded-md"
        />
      </div>

      {/* Flexible Quantity Checkbox */}
      <div className="mb-4">
        <label className="flex items-center gap-2">
          <input
            type="checkbox"
            checked={isUnlimited}
            onChange={(e) => setIsUnlimited(e.target.checked)}
          />
          <span className="text-sm">
            Flexible quantity (accept any amount)
          </span>
        </label>
        <p className="text-xs text-muted-foreground mt-1">
          Check this if you want people to bring "whatever they can" instead of a specific quantity
        </p>
      </div>

      {/* Quantity Input (disabled if unlimited) */}
      {!isUnlimited && (
        <div className="mb-4">
          <label className="block text-sm font-medium mb-1">
            Quantity *
          </label>
          <input
            type="number"
            min="1"
            max="1000"
            value={quantity}
            onChange={(e) => setQuantity(parseInt(e.target.value) || 1)}
            required
            className="w-full px-3 py-2 border rounded-md"
          />
        </div>
      )}

      {/* Notes */}
      <div className="mb-4">
        <label className="block text-sm font-medium mb-1">
          Notes (optional)
        </label>
        <textarea
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          rows={3}
          className="w-full px-3 py-2 border rounded-md"
          placeholder="Any additional details..."
        />
      </div>

      <div className="flex gap-2">
        <Button type="submit">Save Item</Button>
        <Button type="button" variant="outline" onClick={onCancel}>
          Cancel
        </Button>
      </div>
    </form>
  );
}
```

---

### 6.5 Phase 5: Testing Strategy

**Unit Tests (Domain Layer):**
```csharp
// SignUpItemTests.cs
[Fact]
public void Create_WithNullQuantity_ShouldSucceed()
{
    var result = SignUpItem.Create(
        Guid.NewGuid(),
        "Assorted Fruits",
        quantity: null,  // Unlimited
        SignUpItemCategory.Suggested,
        "Bring what you can"
    );

    Assert.True(result.IsSuccess);
    Assert.True(result.Value.IsUnlimitedQuantity());
}

[Fact]
public void AddCommitment_ToUnlimitedItem_ShouldNotCheckRemaining()
{
    var item = SignUpItem.Create(
        Guid.NewGuid(),
        "Fruits",
        quantity: null,
        SignUpItemCategory.Suggested
    ).Value;

    // Should succeed regardless of commitment quantity
    var result = item.AddCommitment(Guid.NewGuid(), 1000, "Bringing tons");

    Assert.True(result.IsSuccess);
}

[Fact]
public void Create_WithQuantity_ShouldValidateRange()
{
    var resultZero = SignUpItem.Create(Guid.NewGuid(), "Item", 0, SignUpItemCategory.Mandatory);
    var resultNegative = SignUpItem.Create(Guid.NewGuid(), "Item", -5, SignUpItemCategory.Mandatory);
    var resultTooLarge = SignUpItem.Create(Guid.NewGuid(), "Item", 1001, SignUpItemCategory.Mandatory);

    Assert.True(resultZero.IsFailure);
    Assert.True(resultNegative.IsFailure);
    Assert.True(resultTooLarge.IsFailure);
}
```

**Integration Tests (API Layer):**
```csharp
[Fact]
public async Task CreateSignUpList_WithUnlimitedItems_ShouldSucceed()
{
    var request = new CreateSignUpListRequest
    {
        Category = "Food",
        Description = "Potluck items",
        HasSuggestedItems = true,
        Items = new[]
        {
            new SignUpItemRequestDto
            {
                ItemDescription = "Assorted Fruits",
                Quantity = null,  // Unlimited
                ItemCategory = SignUpItemCategory.Suggested,
                Notes = "Bring what you can"
            }
        }
    };

    var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/signups", request);

    response.StatusCode.Should().Be(HttpStatusCode.Created);
}
```

**E2E Tests (Frontend):**
```typescript
test('Organizer can create signup item with flexible quantity', async () => {
  await page.goto(`/events/${eventId}/signup-lists/create`);

  await page.fill('[name="itemDescription"]', 'Assorted Fruits');
  await page.check('[name="isUnlimited"]');  // Check "Flexible quantity"

  // Quantity field should be disabled
  await expect(page.locator('[name="quantity"]')).toBeDisabled();

  await page.click('button[type="submit"]');

  // Should see item listed with "Flexible Quantity" badge
  await expect(page.locator('text=Flexible Quantity')).toBeVisible();
});

test('Attendee can commit to unlimited quantity item', async () => {
  await page.goto(`/events/${eventId}`);

  // Click on unlimited item
  await page.click('text=Assorted Fruits');

  // Modal should NOT show quantity input for unlimited items
  await expect(page.locator('text=This item has flexible quantity')).toBeVisible();
  await expect(page.locator('[name="commitQuantity"]')).not.toBeVisible();

  await page.fill('[name="notes"]', 'Bringing 5 pounds of apples');
  await page.click('button:has-text("Confirm Sign Up")');

  // Should show commitment without quantity
  await expect(page.locator('text=Sarah Johnson (some)')).toBeVisible();
});
```

---

## 7. Risk Assessment & Mitigation

### 7.1 Technical Risks

| Risk | Severity | Probability | Mitigation |
|------|----------|-------------|------------|
| **Breaking API Changes** | High | High | Implement versioned DTOs, maintain backward compatibility via optional fields |
| **Database Migration Failure** | Medium | Low | Test migration on staging first, ensure rollback script works |
| **Null Reference Exceptions** | Medium | Medium | Add null checks throughout codebase, use nullable reference types (C# 8+) |
| **Commitment Counting Errors** | Low | Low | Unit test commitment logic for both quantified and unlimited items |

### 7.2 UX Risks

| Risk | Severity | Probability | Mitigation |
|------|----------|-------------|------------|
| **User Confusion** | Medium | Medium | Add help text, tooltips, and examples in UI. User testing before release. |
| **Organizer Misuse** | Low | Medium | Provide clear guidance on when to use unlimited vs quantified items |
| **Progress Tracking Ambiguity** | Medium | High | Use count-based display ("5 people committed") instead of percentage |

### 7.3 Business Risks

| Risk | Severity | Probability | Mitigation |
|------|----------|-------------|------------|
| **Over-Commitment** | Low | Medium | Organizer can monitor commitments and close signup early if needed |
| **Under-Supply** | Low | Low | Organizer still has visibility into commitment count and notes |

---

## 8. Implementation Timeline

**Estimated Effort:** 3-4 days (Senior Engineer)

### Day 1: Domain & Database
- ✅ Update domain entities (SignUpItem.cs)
- ✅ Write unit tests for domain changes
- ✅ Create database migration
- ✅ Update EF Core configuration
- ✅ Test migration on local database

### Day 2: Backend API
- ✅ Update DTOs (Application layer)
- ✅ Update validators
- ✅ Update command handlers
- ✅ Write integration tests
- ✅ Test API endpoints with Postman/curl

### Day 3: Frontend UI
- ✅ Update TypeScript types
- ✅ Create/modify UI components
- ✅ Update display logic
- ✅ Write component tests

### Day 4: Testing & Documentation
- ✅ E2E tests
- ✅ User acceptance testing
- ✅ Update API documentation
- ✅ Deployment to staging
- ✅ Final verification

---

## 9. Success Criteria

**Feature is considered complete when:**

1. ✅ Organizers can create signup items with OR without quantity
2. ✅ Database correctly stores NULL quantities
3. ✅ API returns correct DTOs for unlimited items
4. ✅ UI displays unlimited items with "Flexible Quantity" badge
5. ✅ Attendees can commit to unlimited items without quantity validation
6. ✅ Progress tracking shows commitment count instead of percentage
7. ✅ All tests pass (unit, integration, E2E)
8. ✅ Existing quantified items continue to work without regression
9. ✅ Deployed to staging and verified
10. ✅ Documentation updated

---

## 10. Alternative Considered: "Bring Your Own" Item Type

**Concept:** Instead of making quantity optional on existing items, create a new category type specifically for "Bring What You Can" items.

**Design:**
```csharp
public enum SignUpItemType { Quantified, BringYourOwn }

public class SignUpItem
{
    public SignUpItemType ItemType { get; private set; }
    public int? Quantity { get; private set; }  // NULL if BringYourOwn
}
```

**Pros:**
- ✅ Explicit separation of concerns
- ✅ Easier to reason about (no mixed states)

**Cons:**
- ❌ More complex UI (another dropdown/selector)
- ❌ Redundant with nullable quantity (Option A is simpler)
- ❌ Harder to convert between types

**Decision:** Rejected in favor of Option A (nullable quantity) for simplicity.

---

## 11. Conclusion & Recommendation

**Issue:** Current system requires ALL signup items to have specific numeric quantities, preventing flexible scenarios like "Assorted Fruits" or "Whatever you can bring".

**Root Cause:** Domain model and database schema were designed with the assumption that ALL items must be quantified (quantity > 0 is enforced).

**Recommended Solution:** **Option A - Nullable Quantity**
- Make `Quantity` and `RemainingQuantity` nullable (`int?`)
- NULL = "unlimited/flexible quantity"
- Maintain backward compatibility (existing items keep numeric quantities)
- Minimal code changes, clear semantics

**Implementation Scope:**
- Domain layer: Update validation logic
- Database: ALTER COLUMN to allow NULL
- Backend API: Update DTOs and validators
- Frontend UI: Add "Flexible quantity" checkbox, update display logic

**Estimated Effort:** 3-4 days

**Risk Level:** Low (backward compatible, well-tested pattern)

**Next Steps:**
1. Get stakeholder approval
2. Implement Phase 1 (Domain + Database)
3. Deploy to staging for testing
4. Implement Phases 2-4 (API + UI)
5. User acceptance testing
6. Production deployment

---

**Document Version:** 1.0
**Author:** Claude (SPARC Architecture Agent)
**Date:** 2026-02-16
**Status:** ✅ Analysis Complete - Awaiting Implementation Approval
