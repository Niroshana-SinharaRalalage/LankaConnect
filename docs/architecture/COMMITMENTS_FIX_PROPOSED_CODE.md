# Proposed Code Fix: Missing Commitments Collection

**Date**: 2025-12-19
**Issue**: Commitments not loading in API response
**Root Cause**: Missing EF Core backing field configuration
**Complexity**: Low (1 line of code)
**Risk**: Low (backwards compatible)

---

## Code Changes Required

### File 1: SignUpItemConfiguration.cs

**Path**: `src/LankaConnect.Infrastructure/Data/Configurations/SignUpItemConfiguration.cs`

**Current Code** (Lines 64-67):
```csharp
// Configure relationship with SignUpCommitments
builder.HasMany(si => si.Commitments)
    .WithOne()
    .HasForeignKey(sc => sc.SignUpItemId)
    .OnDelete(DeleteBehavior.Cascade);
```

**Fixed Code**:
```csharp
// Configure relationship with SignUpCommitments
builder.HasMany(si => si.Commitments)
    .WithOne()
    .HasForeignKey(sc => sc.SignUpItemId)
    .OnDelete(DeleteBehavior.Cascade);

// CRITICAL FIX: EF Core must use backing field to populate read-only Commitments collection
// SignUpItem.Commitments is IReadOnlyList (no setter) backed by private List<> _commitments
// Without explicit field access mode, EF Core cannot hydrate the collection
// Pattern matches SignUpListConfiguration.cs (line 83-84) which works correctly
builder.Navigation(si => si.Commitments)
    .UsePropertyAccessMode(PropertyAccessMode.Field);
```

**Justification**:
- `SignUpItem.Commitments` is `IReadOnlyList<SignUpCommitment>` (read-only property)
- Backed by `private readonly List<SignUpCommitment> _commitments = new();`
- EF Core cannot populate read-only properties without explicit field access directive
- Same pattern already used successfully for `SignUpList.Items` (SignUpListConfiguration.cs:83-84)

---

## No Migration Required

**Why**: This is a configuration-only change that affects EF Core's runtime behavior, not the database schema.

**Verification**:
- Table structure (`sign_up_commitments`) remains unchanged
- Foreign key constraint already exists
- Navigation property already defined
- Only changing how EF Core populates the in-memory collection

---

## Testing the Fix

### Manual Test Steps

1. **Apply the code fix** to `SignUpItemConfiguration.cs`
2. **Rebuild the project**:
   ```bash
   dotnet build src/LankaConnect.Infrastructure/LankaConnect.Infrastructure.csproj
   ```
3. **Start the API** (or restart if already running)
4. **Call the endpoint**:
   ```bash
   GET /api/events/0458806b-8672-4ad5-a7cb-f5346f1b282a/signups
   ```
5. **Verify Rice Tay item** now has commitments array populated:
   ```json
   {
       "id": "9dbce508-743a-4cfd-a222-0c3acafd8bbd",
       "itemDescription": "Rice Tay",
       "commitments": [
           // Should now show commitments if they exist in DB
           // OR stay empty if truly no commitments exist
       ]
   }
   ```

### Expected Outcomes

**Scenario A (Orphaned Quantity - Data Corruption)**:
- After fix: `commitments: []` (still empty - commitments don't exist)
- But: `committedQuantity` will still show 2 (incorrect)
- **Next step**: Run data repair SQL to recalculate `remaining_quantity`

**Scenario B (EF Core Hydration Failure - Config Bug)**:
- After fix: `commitments: [...]` (now populated with user data!)
- And: `committedQuantity` matches `commitments.length`
- **Result**: Issue fully resolved

---

## Integration Test (New)

**File**: `tests/LankaConnect.Application.Tests/Events/Queries/GetEventSignUpLists/GetEventSignUpListsQueryHandlerTests.cs`

**Add this test**:

```csharp
[Fact]
public async Task Handle_ShouldLoadCommitmentsForAllItems()
{
    // Arrange - Create event with sign-up items that have commitments
    var organizer = await CreateTestUserAsync();
    var @event = await CreateTestEventAsync(organizer.Id);

    var signUpList = SignUpList.Create(
        "Food & Drinks",
        "Bring food items",
        SignUpType.Predefined,
        hasMandatoryItems: true,
        hasPreferredItems: false,
        hasSuggestedItems: false,
        hasOpenItems: false
    ).Value;

    @event.AddSignUpList(signUpList);

    var itemResult = SignUpItem.Create(
        signUpList.Id,
        "Rice Tay",
        quantity: 5,
        SignUpItemCategory.Mandatory,
        notes: "White Rice"
    );

    signUpList.AddItem(itemResult.Value);

    // Add a commitment
    var user = await CreateTestUserAsync();
    itemResult.Value.AddCommitment(
        user.Id,
        commitQuantity: 2,
        commitNotes: "Will bring",
        contactName: "John Doe",
        contactEmail: "john@example.com",
        contactPhone: null
    );

    await _eventRepository.AddAsync(@event);
    await _unitOfWork.CommitAsync();

    // Act
    var query = new GetEventSignUpListsQuery(@event.Id);
    var result = await _handler.Handle(query, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();

    var riceItem = result.Value
        .SelectMany(list => list.Items)
        .First(item => item.ItemDescription == "Rice Tay");

    // CRITICAL: Commitments collection must be populated
    riceItem.Commitments.Should().NotBeEmpty("commitments must be loaded from database");
    riceItem.Commitments.Should().HaveCount(1, "one user committed");

    var commitment = riceItem.Commitments[0];
    commitment.UserId.Should().Be(user.Id);
    commitment.Quantity.Should().Be(2);
    commitment.ContactName.Should().Be("John Doe");
    commitment.ContactEmail.Should().Be("john@example.com");

    // Verify calculated quantity matches actual commitments
    var calculatedCommitted = riceItem.Quantity - riceItem.RemainingQuantity;
    var actualCommitted = riceItem.Commitments.Sum(c => c.Quantity);
    actualCommitted.Should().Be(calculatedCommitted,
        "committedQuantity should match sum of actual commitments");
}

[Fact]
public async Task Handle_ShouldHaveEmptyCommitmentsWhenNoCommitmentsExist()
{
    // Arrange - Create event with sign-up item but NO commitments
    var organizer = await CreateTestUserAsync();
    var @event = await CreateTestEventAsync(organizer.Id);

    var signUpList = SignUpList.Create(
        "Food & Drinks",
        "Bring food items",
        SignUpType.Predefined,
        hasMandatoryItems: true,
        hasPreferredItems: false,
        hasSuggestedItems: false,
        hasOpenItems: false
    ).Value;

    @event.AddSignUpList(signUpList);

    var itemResult = SignUpItem.Create(
        signUpList.Id,
        "Boiled Eggs",
        quantity: 30,
        SignUpItemCategory.Mandatory,
        notes: null
    );

    signUpList.AddItem(itemResult.Value);

    await _eventRepository.AddAsync(@event);
    await _unitOfWork.CommitAsync();

    // Act
    var query = new GetEventSignUpListsQuery(@event.Id);
    var result = await _handler.Handle(query, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();

    var eggsItem = result.Value
        .SelectMany(list => list.Items)
        .First(item => item.ItemDescription == "Boiled Eggs");

    // Should have empty commitments (not null!)
    eggsItem.Commitments.Should().NotBeNull("commitments collection must be initialized");
    eggsItem.Commitments.Should().BeEmpty("no commitments were made");
    eggsItem.RemainingQuantity.Should().Be(30, "all quantity is remaining");

    var calculatedCommitted = eggsItem.Quantity - eggsItem.RemainingQuantity;
    calculatedCommitted.Should().Be(0, "no commitments should mean 0 committed quantity");
}

[Fact]
public async Task Handle_AllItems_ShouldHaveConsistentCommitmentCounts()
{
    // Arrange - Create event with multiple items and varying commitments
    var organizer = await CreateTestUserAsync();
    var @event = await CreateTestEventAsync(organizer.Id);

    var signUpList = SignUpList.Create(
        "Food & Drinks",
        "Bring food items",
        SignUpType.Predefined,
        hasMandatoryItems: true,
        hasPreferredItems: true,
        hasSuggestedItems: true,
        hasOpenItems: false
    ).Value;

    @event.AddSignUpList(signUpList);

    // Item 1: Fully committed
    var item1 = SignUpItem.Create(signUpList.Id, "Item 1", 10, SignUpItemCategory.Mandatory).Value;
    signUpList.AddItem(item1);
    var user1 = await CreateTestUserAsync();
    item1.AddCommitment(user1.Id, 10, contactName: "User 1");

    // Item 2: Partially committed
    var item2 = SignUpItem.Create(signUpList.Id, "Item 2", 20, SignUpItemCategory.Preferred).Value;
    signUpList.AddItem(item2);
    var user2 = await CreateTestUserAsync();
    item2.AddCommitment(user2.Id, 5, contactName: "User 2");

    // Item 3: No commitments
    var item3 = SignUpItem.Create(signUpList.Id, "Item 3", 15, SignUpItemCategory.Suggested).Value;
    signUpList.AddItem(item3);

    await _eventRepository.AddAsync(@event);
    await _unitOfWork.CommitAsync();

    // Act
    var query = new GetEventSignUpListsQuery(@event.Id);
    var result = await _handler.Handle(query, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();

    var items = result.Value.SelectMany(list => list.Items).ToList();
    items.Should().HaveCount(3);

    // Verify ALL items have consistent commitment data
    foreach (var item in items)
    {
        var calculatedCommitted = item.Quantity - item.RemainingQuantity;
        var actualCommitted = item.Commitments.Sum(c => c.Quantity);

        actualCommitted.Should().Be(calculatedCommitted,
            $"Item '{item.ItemDescription}' has inconsistent commitment data: " +
            $"calculated={calculatedCommitted}, actual={actualCommitted}");

        if (calculatedCommitted > 0)
        {
            item.Commitments.Should().NotBeEmpty(
                $"Item '{item.ItemDescription}' shows {calculatedCommitted} committed but has empty commitments array");
        }
    }

    // Specific assertions for each item
    var fetchedItem1 = items.First(i => i.ItemDescription == "Item 1");
    fetchedItem1.Commitments.Should().HaveCount(1);
    fetchedItem1.Commitments.Sum(c => c.Quantity).Should().Be(10);

    var fetchedItem2 = items.First(i => i.ItemDescription == "Item 2");
    fetchedItem2.Commitments.Should().HaveCount(1);
    fetchedItem2.Commitments.Sum(c => c.Quantity).Should().Be(5);

    var fetchedItem3 = items.First(i => i.ItemDescription == "Item 3");
    fetchedItem3.Commitments.Should().BeEmpty();
}
```

---

## Rollback Plan

**If fix causes issues**:

1. **Revert code change**:
   ```csharp
   // Remove the lines added to SignUpItemConfiguration.cs
   // builder.Navigation(si => si.Commitments)
   //     .UsePropertyAccessMode(PropertyAccessMode.Field);
   ```

2. **Rebuild and redeploy**:
   ```bash
   dotnet build
   dotnet publish
   ```

3. **Restart API**

**Risk of rollback**: Very low - reverting to previous working state.

---

## Deployment Checklist

### Pre-Deployment
- [ ] Code review completed
- [ ] Integration tests pass locally
- [ ] All existing tests still pass
- [ ] Diagnostic SQL queries run on staging database
- [ ] Root cause confirmed (Scenario A or B)

### Deployment
- [ ] Apply code fix to `SignUpItemConfiguration.cs`
- [ ] Build solution (verify no compilation errors)
- [ ] Run all unit and integration tests
- [ ] Deploy to staging environment
- [ ] Verify API endpoint returns commitments for test event

### Post-Deployment Verification
- [ ] Rice Tay item shows correct commitments (or empty if none exist)
- [ ] Run system-wide audit query (Query 6 in diagnostic script)
- [ ] If data repair needed (Scenario A), apply SQL repair script
- [ ] Verify all events show consistent commitment data
- [ ] Monitor logs for any EF Core errors
- [ ] User confirms issue is resolved

### Production Deployment
- [ ] All staging verification complete
- [ ] Backup production database before data repair (if needed)
- [ ] Deploy code fix to production
- [ ] Run diagnostic queries on production database
- [ ] Apply data repair if needed (with rollback plan ready)
- [ ] Monitor for 24 hours
- [ ] Create ADR document with findings

---

## Success Metrics

**Technical Success**:
- ✅ Commitments collection loads correctly for all items
- ✅ `committedQuantity` matches `commitments.sum(c => c.Quantity)`
- ✅ No EF Core errors in logs
- ✅ All integration tests pass

**Business Success**:
- ✅ Organizers can see who committed to bring items
- ✅ Contact information (name, email, phone) displays correctly
- ✅ No user complaints about missing commitment data
- ✅ Sign-up system is reliable and trustworthy

---

## Related Changes

**No other changes required** if fix resolves issue:
- ❌ No migration needed
- ❌ No API contract changes
- ❌ No frontend changes
- ❌ No domain logic changes

**Potential follow-up work**:
- 🔄 Consider making `RemainingQuantity` a computed property (prevents future drift)
- 🔄 Add database constraint to ensure quantity integrity
- 🔄 Add monitoring/alerting for commitment discrepancies
- 🔄 Document pattern in architectural guidelines

---

**Confidence Level**: Very High

This is a well-understood EF Core configuration pattern that already works correctly in `SignUpListConfiguration.cs` for the `Items` collection. The same fix should work for the `Commitments` collection.
