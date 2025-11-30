# Sign-Up Category Redesign - Implementation Progress

## Session Date: 2025-11-29

### Overview
Complete redesign of the Event Sign-Up system from binary "Open/Predefined" model to flexible "Mandatory/Preferred/Suggested" category-based system.

## Requirements
- **User Request**: Replace Open/Predefined with three categories (Mandatory, Preferred, Suggested)
- **Item Quantity**: Each item needs a quantity field
- **Flexible Categories**: Event creators can select which categories to enable (one, two, or all three)
- **Fix Current Bug**: Create Sign-up button not saving

## Documentation Updates ✅

### 1. Requirements Documentation
- **File**: [docs/architecture/EventFeatureRequirements.md](./architecture/EventFeatureRequirements.md)
- **Status**: ✅ COMPLETE
- **Changes**: Updated Section 3 with detailed category-based system specification including:
  - Three priority categories (Mandatory, Preferred, Suggested)
  - Item structure with quantity support
  - Flexible category selection
  - User interaction flows
  - Example use cases (Potluck, Temple event, Cleanup)

### 2. Technical Specification
- **File**: [docs/architecture/SignUpCategoryRedesign.md](./architecture/SignUpCategoryRedesign.md)
- **Status**: ✅ COMPLETE
- **Contents**:
  - Complete database schema
  - Domain model design
  - API endpoint specifications
  - Frontend UI mockups
  - Migration plan and rollout strategy

## Domain Layer Implementation ✅

### 1. SignUpItemCategory Enum
- **File**: `src/LankaConnect.Domain/Events/Enums/SignUpItemCategory.cs`
- **Status**: ✅ COMPLETE
- **Values**:
  - Mandatory = 0
  - Preferred = 1
  - Suggested = 2

### 2. SignUpItem Entity
- **File**: `src/LankaConnect.Domain/Events/Entities/SignUpItem.cs`
- **Status**: ✅ COMPLETE
- **Features**:
  - Item description and quantity tracking
  - Category assignment
  - Remaining quantity management
  - Commitment tracking (HasMany SignUpCommitments)
  - Domain validation
  - Methods: `AddCommitment`, `CancelCommitment`, `UpdateDescription`, `UpdateQuantity`

### 3. SignUpCommitment Entity Update
- **File**: `src/LankaConnect.Domain/Events/Entities/SignUpCommitment.cs`
- **Status**: ✅ COMPLETE
- **Changes**:
  - Added `SignUpItemId` (nullable for backward compatibility)
  - Added `Notes` field
  - New factory method: `CreateForItem` (for category-based model)
  - Kept existing `Create` method (for legacy Open sign-ups)

### 4. SignUpList Aggregate Update
- **File**: `src/LankaConnect.Domain/Events/Entities/SignUpList.cs`
- **Status**: ✅ COMPLETE
- **New Properties**:
  - `HasMandatoryItems`, `HasPreferredItems`, `HasSuggestedItems`
  - `Items` collection (IReadOnlyList<SignUpItem>)
- **New Methods**:
  - `CreateWithCategories` - Factory for category-based lists
  - `AddItem`, `RemoveItem`, `GetItem`
  - `GetItemsByCategory`, `GetTotalItemCount`, `GetFullyCommittedItemCount`
  - `IsCategoryBased`, `IsLegacyPredefined`
- **Backward Compatibility**: All legacy methods preserved

## Infrastructure Layer Implementation ✅

### 1. SignUpItemConfiguration
- **File**: `src/LankaConnect.Infrastructure/Data/Configurations/SignUpItemConfiguration.cs`
- **Status**: ✅ COMPLETE
- **Table**: `sign_up_items`
- **Columns**:
  - `id` (UUID, PK)
  - `sign_up_list_id` (UUID, FK → sign_up_lists)
  - `item_description` (VARCHAR(200))
  - `quantity` (INT)
  - `remaining_quantity` (INT)
  - `item_category` (INT) // 0=Mandatory, 1=Preferred, 2=Suggested
  - `notes` (VARCHAR(500), nullable)
  - `created_at`, `updated_at`
- **Indexes**:
  - `ix_sign_up_items_list_id`
  - `ix_sign_up_items_category`

### 2. SignUpListConfiguration Update
- **File**: `src/LankaConnect.Infrastructure/Data/Configurations/SignUpListConfiguration.cs`
- **Status**: ✅ COMPLETE
- **New Columns**:
  - `has_mandatory_items` (BOOLEAN, default false)
  - `has_preferred_items` (BOOLEAN, default false)
  - `has_suggested_items` (BOOLEAN, default false)

### 3. SignUpCommitmentConfiguration Update
- **File**: `src/LankaConnect.Infrastructure/Data/Configurations/SignUpCommitmentConfiguration.cs`
- **Status**: ✅ COMPLETE
- **New Columns**:
  - `sign_up_item_id` (UUID, nullable, FK → sign_up_items)
  - `notes` (VARCHAR(1000), nullable)
- **New Index**:
  - `ix_sign_up_commitments_sign_up_item_id`

### 4. EF Core Migration
- **Migration Name**: `20251129201535_AddSignUpItemCategorySupport`
- **Status**: ✅ COMPLETE
- **File**: `src/LankaConnect.Infrastructure/Data/Migrations/20251129201535_AddSignUpItemCategorySupport.cs`
- **Changes**:
  - Created `sign_up_items` table in events schema with all columns and indexes
  - Added category flags to `sign_up_lists` (has_mandatory_items, has_preferred_items, has_suggested_items)
  - Added `sign_up_item_id` and `notes` columns to `sign_up_commitments`
  - Moved `sign_up_lists` and `sign_up_commitments` tables to events schema
  - Created foreign key relationships with CASCADE delete
  - Created indexes for performance (ix_sign_up_items_list_id, ix_sign_up_items_category, ix_sign_up_commitments_sign_up_item_id)

### 5. AppDbContext Registration
- **File**: `src/LankaConnect.Infrastructure/Data/AppDbContext.cs`
- **Status**: ✅ COMPLETE
- **Changes**:
  - Added SignUpList, SignUpItem, SignUpCommitment to configured entity types
  - Registered all three configurations in OnModelCreating
  - Added schema mappings for events schema
  - Added using statement for Domain.Events.Entities

## Build Status
- **Backend Build**: ✅ SUCCESS (0 errors, 0 warnings)
- **Last Build**: 00:00:08.34

## Pending Work

### Application Layer
- [ ] Create `AddSignUpListWithCategoriesCommand`
- [ ] Create `AddSignUpItemCommand`
- [ ] Create `RemoveSignUpItemCommand`
- [ ] Create `CommitToSignUpItemCommand`
- [ ] Update query DTOs to include category fields
- [ ] Update existing handlers for backward compatibility

### API Layer
- [ ] Update EventsController endpoints
- [ ] Create new DTOs (AddSignUpListWithCategoriesRequest, AddSignUpItemRequest)
- [ ] Update response DTOs (SignUpListDto, SignUpItemDto)
- [ ] Add validation attributes

### Frontend Layer
- [ ] Update TypeScript enums (SignUpItemCategory)
- [ ] Update types (SignUpListDto, SignUpItemDto)
- [ ] Update React hooks (useEventSignUps, useAddSignUpItem)
- [ ] Redesign manage-signups page UI:
  - Category checkboxes (Mandatory, Preferred, Suggested)
  - Dynamic item sections for each enabled category
  - Quantity input for each item
  - Notes field

### Testing & Deployment
- [ ] Apply migration to staging database (requires Azure connection)
- [ ] Test category-based sign-up creation
- [ ] Test item commitment flow
- [ ] Test legacy sign-up lists (backward compatibility)
- [ ] Update STREAMLINED_ACTION_PLAN.md
- [ ] Update PROGRESS_TRACKER.md
- [ ] Git commit with detailed message

## Next Steps (In Order)
1. ✅ Migration generation complete
2. ⏳ Apply migration to staging database (user needs to run: `dotnet ef database update` with Azure connection)
3. 🔄 NOW: Update Application layer (Commands, Queries, DTOs)
4. Update API layer (Controllers, Request/Response DTOs)
5. Update Frontend (Types, Hooks, UI Components)
6. End-to-end testing
7. Documentation updates and git commit

## Notes
- **Backward Compatibility**: All legacy methods and tables preserved
- **Zero Downtime**: Migration is additive, no breaking changes to existing data
- **Incremental TDD**: Following zero-tolerance for compilation errors
- **Clean Architecture**: Domain → Infrastructure → Application → API → Frontend
