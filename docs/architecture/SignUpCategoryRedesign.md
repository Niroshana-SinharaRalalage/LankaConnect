# Event Sign-Up Category Redesign - Technical Specification

## Overview
This document details the technical implementation for the new category-based sign-up system, replacing the current "Open/Predefined" model with flexible "Mandatory/Preferred/Suggested" categories.

## Database Schema Changes

### New SignUpItemCategory Enum
```csharp
public enum SignUpItemCategory
{
    Mandatory = 0,    // Required items
    Preferred = 1,    // Highly desired items
    Suggested = 2     // Optional items
}
```

### Updated SignUpList Table Structure
```sql
CREATE TABLE sign_up_lists (
    id UUID PRIMARY KEY,
    event_id UUID NOT NULL REFERENCES events(id),
    category VARCHAR(100) NOT NULL,  -- e.g., "Food & Drinks", "Decorations"
    description TEXT,
    has_mandatory_items BOOLEAN DEFAULT FALSE,
    has_preferred_items BOOLEAN DEFAULT FALSE,
    has_suggested_items BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP,
    FOREIGN KEY (event_id) REFERENCES events(id) ON DELETE CASCADE
);
```

### New SignUpItem Table (replaces predefinedItems array)
```sql
CREATE TABLE sign_up_items (
    id UUID PRIMARY KEY,
    sign_up_list_id UUID NOT NULL REFERENCES sign_up_lists(id),
    item_description VARCHAR(200) NOT NULL,
    quantity INT NOT NULL DEFAULT 1,
    item_category INT NOT NULL,  -- 0=Mandatory, 1=Preferred, 2=Suggested
    remaining_quantity INT NOT NULL,
    notes TEXT,
    created_at TIMESTAMP DEFAULT NOW(),
    FOREIGN KEY (sign_up_list_id) REFERENCES sign_up_lists(id) ON DELETE CASCADE
);

CREATE INDEX idx_sign_up_items_list_id ON sign_up_items(sign_up_list_id);
CREATE INDEX idx_sign_up_items_category ON sign_up_items(item_category);
```

### Updated SignUpCommitment Table
```sql
CREATE TABLE sign_up_commitments (
    id UUID PRIMARY KEY,
    sign_up_item_id UUID NOT NULL REFERENCES sign_up_items(id),
    user_id UUID NOT NULL,
    quantity INT NOT NULL DEFAULT 1,
    committed_at TIMESTAMP DEFAULT NOW(),
    notes TEXT,
    FOREIGN KEY (sign_up_item_id) REFERENCES sign_up_items(id) ON DELETE CASCADE,
    UNIQUE(sign_up_item_id, user_id)  -- One commitment per user per item
);

CREATE INDEX idx_sign_up_commitments_item_id ON sign_up_commitments(sign_up_item_id);
CREATE INDEX idx_sign_up_commitments_user_id ON sign_up_commitments(user_id);
```

## Domain Model Changes

### SignUpList Aggregate
```csharp
public class SignUpList : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public string Category { get; private set; }
    public string Description { get; private set; }

    // Category flags
    public bool HasMandatoryItems { get; private set; }
    public bool HasPreferredItems { get; private set; }
    public bool HasSuggestedItems { get; private set; }

    // Items collection
    private readonly List<SignUpItem> _items = new();
    public IReadOnlyList<SignUpItem> Items => _items.AsReadOnly();

    // Methods
    public Result AddItem(string description, int quantity, SignUpItemCategory category);
    public Result RemoveItem(Guid itemId);
    public Result UpdateItem(Guid itemId, string description, int quantity);
}
```

### SignUpItem Entity
```csharp
public class SignUpItem : Entity
{
    public Guid Id { get; private set; }
    public Guid SignUpListId { get; private set; }
    public string ItemDescription { get; private set; }
    public int Quantity { get; private set; }
    public SignUpItemCategory ItemCategory { get; private set; }
    public int RemainingQuantity { get; private set; }
    public string? Notes { get; private set; }

    private readonly List<SignUpCommitment> _commitments = new();
    public IReadOnlyList<SignUpCommitment> Commitments => _commitments.AsReadOnly();

    public Result CommitQuantity(Guid userId, int quantity, string? notes);
    public Result CancelCommitment(Guid userId);
}
```

### SignUpCommitment Entity
```csharp
public class SignUpCommitment : Entity
{
    public Guid Id { get; private set; }
    public Guid SignUpItemId { get; private set; }
    public Guid UserId { get; private set; }
    public int Quantity { get; private set; }
    public DateTime CommittedAt { get; private set; }
    public string? Notes { get; private set; }
}
```

## API Endpoints

### Create Sign-Up List
```http
POST /api/events/{eventId}/signups
Content-Type: application/json

{
  "category": "Food & Drinks",
  "description": "Please help us with food for the potluck",
  "hasMandatoryItems": true,
  "hasPreferredItems": true,
  "hasSuggestedItems": false,
  "items": [
    {
      "description": "Main Dish (Chicken Curry)",
      "quantity": 2,
      "itemCategory": 0,  // Mandatory
      "notes": "Serves 10 people per dish"
    },
    {
      "description": "Rice",
      "quantity": 3,
      "itemCategory": 1,  // Preferred
      "notes": "Large pot"
    }
  ]
}
```

### Get Sign-Up Lists for Event
```http
GET /api/events/{eventId}/signups

Response:
[
  {
    "id": "uuid",
    "category": "Food & Drinks",
    "description": "...",
    "hasMandatoryItems": true,
    "hasPreferredItems": true,
    "hasSuggestedItems": false,
    "items": [
      {
        "id": "uuid",
        "description": "Main Dish (Chicken Curry)",
        "quantity": 2,
        "itemCategory": 0,
        "remainingQuantity": 1,
        "commitments": [...]
      }
    ]
  }
]
```

### Commit to Item
```http
POST /api/events/{eventId}/signups/{signupId}/items/{itemId}/commit
Content-Type: application/json

{
  "userId": "uuid",
  "quantity": 1,
  "notes": "I'll bring homemade chicken curry"
}
```

## Frontend UI Changes

### Create Sign-Up List Form
```tsx
<form>
  <Input label="Category Name" placeholder="e.g., Food & Drinks" />
  <Textarea label="Description" />

  {/* Category Selection */}
  <div>
    <label>Select Item Categories:</label>
    <Checkbox label="Mandatory Items" />
    <Checkbox label="Preferred Items" />
    <Checkbox label="Suggested Items" />
  </div>

  {/* Mandatory Items Section */}
  {hasMandatoryItems && (
    <div>
      <h3>Mandatory Items</h3>
      {mandatoryItems.map(item => (
        <ItemRow key={item.id} item={item} onRemove={...} />
      ))}
      <ItemInput onAdd={(desc, qty) => addMandatoryItem(desc, qty)} />
    </div>
  )}

  {/* Preferred Items Section */}
  {hasPreferredItems && (
    <div>
      <h3>Preferred Items</h3>
      {preferredItems.map(item => (
        <ItemRow key={item.id} item={item} onRemove={...} />
      ))}
      <ItemInput onAdd={(desc, qty) => addPreferredItem(desc, qty)} />
    </div>
  )}

  {/* Suggested Items Section */}
  {hasSuggestedItems && (
    <div>
      <h3>Suggested Items</h3>
      {suggestedItems.map(item => (
        <ItemRow key={item.id} item={item} onRemove={...} />
      ))}
      <ItemInput onAdd={(desc, qty) => addSuggestedItem(desc, qty)} />
    </div>
  )}

  <Button>Create Sign-Up List</Button>
</form>
```

## Migration Steps

1. Create new `SignUpItemCategory` enum in Domain
2. Create new `SignUpItem` and `SignUpCommitment` entities
3. Update `SignUpList` aggregate to use new structure
4. Create EF Core migration:
   - Add `sign_up_items` table
   - Add category flags to `sign_up_lists`
   - Migrate existing data (if any)
   - Update `sign_up_commitments` to reference `sign_up_item_id`
5. Update Application layer commands/queries
6. Update API controllers
7. Update frontend types and hooks
8. Update UI components

## Breaking Changes
- Old `SignUpType` enum (Open/Predefined) will be deprecated
- `predefinedItems` array in `SignUpList` will be replaced with `Items` collection
- API contracts will change (backward incompatible)

## Rollout Plan
1. Stage 1: Backend migration + API updates (keep old endpoints for compatibility)
2. Stage 2: Frontend updates to use new API
3. Stage 3: Remove old API endpoints after frontend cutover
4. Stage 4: Data migration script for existing sign-ups (if any in production)
