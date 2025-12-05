# Database Schema and CQRS Handlers Verification

**Date**: 2025-12-04
**Purpose**: Comprehensive verification of database structure and command/query handlers for sign-up lists feature

---

## ✅ DATABASE SCHEMA CONFIRMED

### Schema: `events`

All sign-up tables are in the `events` schema.

---

## 📊 Table 1: `events.sign_up_lists`

**Purpose**: Stores sign-up list metadata with category flags

| Column Name | Data Type | Constraints | Description |
|------------|-----------|-------------|-------------|
| `id` | uuid | PRIMARY KEY, NOT NULL | Sign-up list ID (generated in domain) |
| `event_id` | uuid | FOREIGN KEY → events.events.Id, NOT NULL | Parent event reference |
| `category` | varchar(100) | NOT NULL | Sign-up list name (e.g., "Food & Drinks") |
| `description` | varchar(500) | NOT NULL | Sign-up list description |
| `sign_up_type` | varchar(20) | NOT NULL | Legacy field (Open/Predefined) |
| `has_mandatory_items` | boolean | NOT NULL, DEFAULT false | Category flag: has mandatory items |
| `has_preferred_items` | boolean | NOT NULL, DEFAULT false | Category flag: has preferred items |
| `has_suggested_items` | boolean | NOT NULL, DEFAULT false | Category flag: has suggested items |
| `predefined_items` | jsonb | NOT NULL | Legacy field (deprecated) |
| `created_at` | timestamp with time zone | NOT NULL | Record creation timestamp |
| `updated_at` | timestamp with time zone | NULLABLE | Last update timestamp |

**Indexes**:
- `ix_sign_up_lists_event_id` on `event_id`
- `ix_sign_up_lists_category` on `category`

**Created by Migration**: `20251123163612_AddSignUpListAndSignUpCommitmentTables`
**Enhanced by Migration**: `20251129201535_AddSignUpItemCategorySupport` (added category flags)

---

## 📊 Table 2: `events.sign_up_items`

**Purpose**: Stores individual items within sign-up lists, categorized as Mandatory/Preferred/Suggested

| Column Name | Data Type | Constraints | Description |
|------------|-----------|-------------|-------------|
| `id` | uuid | PRIMARY KEY, NOT NULL | Sign-up item ID (generated in domain) |
| `sign_up_list_id` | uuid | FOREIGN KEY → events.sign_up_lists.id, NOT NULL | Parent sign-up list reference |
| `item_description` | varchar(200) | NOT NULL | Item description (e.g., "Rice (2 cups)") |
| `quantity` | integer | NOT NULL | Total quantity needed |
| `remaining_quantity` | integer | NOT NULL | Remaining quantity available |
| `item_category` | integer | NOT NULL | Enum: 0=Mandatory, 1=Preferred, 2=Suggested |
| `notes` | varchar(500) | NULLABLE | Additional notes for the item |
| `created_at` | timestamp with time zone | NOT NULL | Record creation timestamp |
| `updated_at` | timestamp with time zone | NULLABLE | Last update timestamp |

**Indexes**:
- `ix_sign_up_items_list_id` on `sign_up_list_id`
- `ix_sign_up_items_category` on `item_category`

**Foreign Key Cascades**: ON DELETE CASCADE (when sign-up list is deleted, items are deleted)

**Created by Migration**: `20251129201535_AddSignUpItemCategorySupport`

---

## 📊 Table 3: `events.sign_up_commitments`

**Purpose**: Stores user commitments to bring items (supports both legacy and new models)

| Column Name | Data Type | Constraints | Description |
|------------|-----------|-------------|-------------|
| `id` | uuid | PRIMARY KEY, NOT NULL | Commitment ID (generated in domain) |
| `sign_up_item_id` | uuid | FOREIGN KEY → events.sign_up_items.id, NULLABLE | Link to specific item (new model) |
| `user_id` | uuid | NOT NULL | User who made the commitment |
| `item_description` | varchar(500) | NOT NULL | What user is bringing |
| `quantity` | integer | NOT NULL | Quantity user is bringing |
| `committed_at` | timestamp with time zone | NOT NULL | When commitment was made |
| `notes` | varchar(1000) | NULLABLE | User's notes about the commitment |
| `contact_name` | varchar(200) | NULLABLE | Contact name (Phase 2 feature) |
| `contact_email` | varchar(200) | NULLABLE | Contact email (Phase 2 feature) |
| `contact_phone` | varchar(20) | NULLABLE | Contact phone (Phase 2 feature) |
| `SignUpListId` | uuid | NULLABLE | Legacy: link to sign-up list (Open sign-ups) |
| `created_at` | timestamp with time zone | NOT NULL | Record creation timestamp |
| `updated_at` | timestamp with time zone | NULLABLE | Last update timestamp |

**Indexes**:
- `ix_sign_up_commitments_user_id` on `user_id`
- `ix_sign_up_commitments_sign_up_item_id` on `sign_up_item_id`
- `IX_sign_up_commitments_SignUpListId` on `SignUpListId` (legacy)

**Foreign Key Cascades**: ON DELETE CASCADE (when item is deleted, commitments are deleted)

**Created by Migration**: `20251123163612_AddSignUpListAndSignUpCommitmentTables`
**Enhanced by Migration**: `20251129201535_AddSignUpItemCategorySupport` (added sign_up_item_id, notes)
**Enhanced by Migration**: `20251204172917_AddContactInfoToSignUpCommitments` (added contact fields)

---

## 🏗️ ENTITY FRAMEWORK CORE CONFIGURATION

### SignUpListConfiguration.cs

**Location**: `src/LankaConnect.Infrastructure/Data/Configurations/SignUpListConfiguration.cs`

**Key Configuration**:
```csharp
// Configure relationship to SignUpItems (new category-based model)
builder.HasMany(s => s.Items)
    .WithOne()
    .HasForeignKey(i => i.SignUpListId)
    .OnDelete(DeleteBehavior.Cascade);

// CRITICAL: Use backing field "_items" for EF Core change tracking
builder.Navigation(s => s.Items)
    .UsePropertyAccessMode(PropertyAccessMode.Field);
```

**Why This Matters**:
- Domain model uses private `_items` field with `IReadOnlyList<SignUpItem>` property wrapper
- Without explicit backing field configuration, EF Core can't track changes
- `UsePropertyAccessMode(PropertyAccessMode.Field)` tells EF to access private `_items` directly

### SignUpItemConfiguration.cs

**Location**: `src/LankaConnect.Infrastructure/Data/Configurations/SignUpItemConfiguration.cs`

**Key Configuration**:
```csharp
// Configure relationship with SignUpList
builder.HasOne<SignUpList>()
    .WithMany(sl => sl.Items)
    .HasForeignKey(si => si.SignUpListId)
    .OnDelete(DeleteBehavior.Cascade);

// Configure relationship with SignUpCommitments
builder.HasMany(si => si.Commitments)
    .WithOne()
    .HasForeignKey(sc => sc.SignUpItemId)
    .OnDelete(DeleteBehavior.Cascade);
```

### SignUpCommitmentConfiguration.cs

**Location**: `src/LankaConnect.Infrastructure/Data/Configurations/SignUpCommitmentConfiguration.cs`

---

## 📝 CQRS COMMANDS

### 1. CreateSignUpListWithItemsCommand

**Location**: `src/LankaConnect.Application/Events/Commands/CreateSignUpListWithItems/CreateSignUpListWithItemsCommandHandler.cs`

**Purpose**: Create a sign-up list with items in a single transactional operation

**Handler Flow**:
```csharp
1. Get event by ID
2. Convert command items to tuple format
3. Call SignUpList.CreateWithCategoriesAndItems() domain method
   → Creates SignUpList entity
   → Calls AddItem() for each item
   → Items added to private _items collection
4. Call @event.AddSignUpList()
5. Call _unitOfWork.CommitAsync()
   → EF Core persists SignUpList
   → EF Core persists all Items (via backing field tracking)
6. Return SignUpList ID
```

**Critical**: Step 5 is where EF Core must properly track the `_items` collection to persist items to database.

### 2. AddSignUpItemCommand

**Location**: `src/LankaConnect.Application/Events/Commands/AddSignUpItem/AddSignUpItemCommandHandler.cs`

**Purpose**: Add a new item to an existing sign-up list

### 3. RemoveSignUpItemCommand

**Location**: `src/LankaConnect.Application/Events/Commands/RemoveSignUpItem/RemoveSignUpItemCommandHandler.cs`

**Purpose**: Remove an item from a sign-up list

### 4. UpdateSignUpListCommand

**Location**: `src/LankaConnect.Application/Events/Commands/UpdateSignUpList/UpdateSignUpListCommandHandler.cs`

**Purpose**: Update sign-up list category name, description, and category flags

### 5. CommitToSignUpItemCommand

**Location**: `src/LankaConnect.Application/Events/Commands/CommitToSignUpItem/CommitToSignUpItemCommandHandler.cs`

**Purpose**: User commits to bringing a specific item (Phase 2: includes contact info)

### 6. RemoveSignUpListFromEventCommand

**Location**: `src/LankaConnect.Application/Events/Commands/RemoveSignUpListFromEvent/RemoveSignUpListFromEventCommandHandler.cs`

**Purpose**: Remove an entire sign-up list from an event

---

## 📖 CQRS QUERIES

### 1. GetEventSignUpListsQuery

**Location**: `src/LankaConnect.Application/Events/Queries/GetEventSignUpLists/GetEventSignUpListsQueryHandler.cs`

**Purpose**: Get all sign-up lists for an event with their items and commitments

**Handler Flow**:
```csharp
1. Get event by ID (includes SignUpLists.Items navigation)
2. Map SignUpLists to SignUpListDto
   → Includes Items array
   → Includes Commitments for each item
   → Includes category flags
3. Return List<SignUpListDto>
```

**Repository Include Statement** (EventRepository.cs line 24-29):
```csharp
return await _dbSet
    .Include(e => e.Images)
    .Include(e => e.Videos)
    .Include(e => e.SignUpLists)
        .ThenInclude(s => s.Commitments)
    .Include(e => e.SignUpLists)
        .ThenInclude(s => s.Items)
            .ThenInclude(i => i.Commitments)
    .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
```

**This ensures**:
- All sign-up lists are loaded
- All items for each sign-up list are loaded
- All commitments for each item are loaded

---

## 🔍 THE BUG AND THE FIX

### The Problem

**Symptoms**:
- User creates sign-up list with items via CREATE page
- Items appear in manage page initially
- When opening EDIT page, items show "No items added yet"
- Database query shows `sign_up_items` table is EMPTY

**Root Cause**:
EF Core configuration did NOT explicitly tell EF to use the `_items` backing field for change tracking.

**Domain Model**:
```csharp
// SignUpList.cs
private readonly List<SignUpItem> _items = new();
public IReadOnlyList<SignUpItem> Items => _items.AsReadOnly();
```

**What Happened**:
1. `CreateSignUpListWithItemsCommandHandler` calls `SignUpList.CreateWithCategoriesAndItems()`
2. Domain method creates SignUpList and calls `AddItem()` for each item
3. `AddItem()` adds items to private `_items` list ✅ (in memory)
4. `_unitOfWork.CommitAsync()` is called
5. EF Core sees SignUpList entity is new → INSERT INTO sign_up_lists ✅
6. **EF Core tries to track Items collection** ❌
7. **EF Core sees `IReadOnlyList<SignUpItem>` property** ❌
8. **EF Core doesn't know about `_items` backing field** ❌
9. **Items are NOT persisted to database** ❌

### The Fix (Commit 3f84c59)

**File**: `src/LankaConnect.Infrastructure/Data/Configurations/SignUpListConfiguration.cs`

**Lines 77-79**:
```csharp
// CRITICAL: Use backing field "_items" for EF Core change tracking
builder.Navigation(s => s.Items)
    .UsePropertyAccessMode(PropertyAccessMode.Field);
```

**How It Works Now**:
1. EF Core configuration explicitly tells EF to use `_items` backing field
2. When `AddItem()` adds items to `_items`, EF Core tracks the change ✅
3. When `CommitAsync()` is called:
   - EF Core sees SignUpList is new → INSERT INTO sign_up_lists ✅
   - EF Core sees items in `_items` collection → INSERT INTO sign_up_items (for each item) ✅
   - All items are persisted with correct `sign_up_list_id` foreign key ✅
4. Edit page loads sign-up list from database
5. Items array is populated → tables show items correctly ✅

---

## ✅ VERIFICATION CHECKLIST

- [x] **Database Schema**: 3 tables in `events` schema with correct columns
- [x] **Migrations**: Applied to database (verified by table structure)
- [x] **EF Core Configuration**: SignUpListConfiguration uses backing field
- [x] **Domain Entities**: SignUpList, SignUpItem, SignUpCommitment with proper navigation
- [x] **Command Handlers**: CreateSignUpListWithItems, AddSignUpItem, RemoveSignUpItem, UpdateSignUpList
- [x] **Query Handlers**: GetEventSignUpLists with Items and Commitments
- [x] **Repository Includes**: EventRepository includes SignUpLists.Items navigation
- [x] **Backing Field Fix**: Deployed to staging (commit 3f84c59)
- [ ] **User Testing**: User needs to create new sign-up list and verify items persist

---

## 🧪 NEXT VERIFICATION STEP

**User should test on staging**:

1. Navigate to event in staging environment
2. Click "Manage" → "Add Sign-Up List"
3. Create sign-up list with items in all three categories:
   - Mandatory: "Rice (2 cups)"
   - Preferred: "Water bottles"
   - Suggested: "Paper plates"
4. Click "Create Sign-Up List"
5. **VERIFY**: Items appear in manage page
6. Click "Edit" on the sign-up list
7. **VERIFY**: All items load correctly in edit page
8. **VERIFY**: Can add new items
9. **VERIFY**: Can delete items

If items appear correctly, the fix is CONFIRMED working.

If items still don't appear, we need to investigate deeper (possible EF Core tracking issue or database migration not applied).

---

**Document Created**: 2025-12-04
**Status**: Database structure CONFIRMED, Handlers CONFIRMED, Bug fix DEPLOYED
**Next**: User verification on staging environment
