# Open Items Cancellation Bug - Visual Diagrams

## The Bug in Action

### Scenario: User cancels registration with "Delete commitments" checked

```
BEFORE CANCELLATION:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Event: "Temple Potluck"
User: Alice (userId: abc-123)

Sign-Up Lists:
┌─────────────────────────────────────────────────────────────────┐
│ "Food & Drinks"                                                  │
├─────────────────────────────────────────────────────────────────┤
│ MANDATORY ITEMS:                                                 │
│   ✓ Rice (qty: 5 kg)                                            │
│     • Alice committed: 2 kg  ← Will stay after cancel          │
│                                                                  │
│ SUGGESTED ITEMS:                                                 │
│   ✓ Beverages (qty: 10 bottles)                                │
│     • Alice committed: 3 bottles  ← Will stay after cancel     │
│                                                                  │
│ OPEN ITEMS:                                                      │
│   ✓ "Homemade Lasagna" (qty: 1, created by Alice)              │
│     • Alice committed: 1  ← BUG: Item stays after cancel ❌    │
└─────────────────────────────────────────────────────────────────┘

Alice's Registration: CONFIRMED ✓
```

```
AFTER CANCELLATION (Current Broken Behavior):
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Event: "Temple Potluck"
User: Alice (userId: abc-123)

Sign-Up Lists:
┌─────────────────────────────────────────────────────────────────┐
│ "Food & Drinks"                                                  │
├─────────────────────────────────────────────────────────────────┤
│ MANDATORY ITEMS:                                                 │
│   ✓ Rice (qty: 5 kg)                                            │
│     • [No commitments]  ← Commitment deleted ✅                │
│     • [Sign Up button visible] ← Correct ✅                    │
│                                                                  │
│ SUGGESTED ITEMS:                                                 │
│   ✓ Beverages (qty: 10 bottles)                                │
│     • [No commitments]  ← Commitment deleted ✅                │
│     • [Sign Up button visible] ← Correct ✅                    │
│                                                                  │
│ OPEN ITEMS:                                                      │
│   ✓ "Homemade Lasagna" (qty: 1, created by Alice)              │
│     • [No commitments]  ← Commitment deleted ✅                │
│     • [Sign Up button visible] ← WRONG! Item should be gone ❌ │
│     • "Your item" badge still showing ❌                       │
└─────────────────────────────────────────────────────────────────┘

Alice's Registration: CANCELLED ✓
Alice's Reaction: "I cancelled. Why is my Lasagna item still there?" 😕
```

```
EXPECTED BEHAVIOR (After Fix):
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Event: "Temple Potluck"
User: Alice (userId: abc-123)

Sign-Up Lists:
┌─────────────────────────────────────────────────────────────────┐
│ "Food & Drinks"                                                  │
├─────────────────────────────────────────────────────────────────┤
│ MANDATORY ITEMS:                                                 │
│   ✓ Rice (qty: 5 kg)                                            │
│     • [No commitments]  ← Commitment deleted ✅                │
│     • [Sign Up button visible] ← Correct ✅                    │
│                                                                  │
│ SUGGESTED ITEMS:                                                 │
│   ✓ Beverages (qty: 10 bottles)                                │
│     • [No commitments]  ← Commitment deleted ✅                │
│     • [Sign Up button visible] ← Correct ✅                    │
│                                                                  │
│ OPEN ITEMS:                                                      │
│   [Empty - Alice's item was deleted] ← FIXED! ✅               │
│   • [Sign Up button for category visible] ← Correct ✅         │
└─────────────────────────────────────────────────────────────────┘

Alice's Registration: CANCELLED ✓
Alice's Reaction: "Perfect! My Lasagna is gone too." 😊
```

---

## Code Flow Comparison

### Working Path: Mandatory/Suggested Items

```
┌─────────────────────────────────────────────────────────────┐
│ User clicks "Cancel Sign Up" on Mandatory item              │
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ Frontend: handleCancelSignUp(signUpListId, itemId)         │
│                                                              │
│   commitToSignUpItem.mutateAsync({                         │
│     eventId,                                                │
│     signupId: signUpListId,                                │
│     itemId: itemId,                                        │
│     userId: userId,                                        │
│     quantity: 0,  ← MAGIC NUMBER: Signals cancellation    │
│     notes: '',                                             │
│     contactName: '',                                       │
│     contactEmail: '',                                      │
│     contactPhone: '',                                      │
│   })                                                       │
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ API: POST /events/{id}/signups/{id}/items/{id}/commit     │
│ Body: { quantity: 0, ... }                                 │
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ Backend: CommitToSignUpItemCommandHandler                   │
│                                                              │
│   existingCommitment = item.Commitments.Find(userId)       │
│   if (existingCommitment != null) {                        │
│     item.UpdateCommitment(userId, 0, ...)  ← quantity=0   │
│   }                                                        │
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ Domain: SignUpItem.UpdateCommitment(userId, newQuantity=0) │
│                                                              │
│   if (newQuantity == 0) {  ← SPECIAL CASE DETECTED         │
│     RemainingQuantity += existingCommitment.Quantity       │
│     _commitments.Remove(existingCommitment)                │
│     MarkAsUpdated()                                        │
│     return Result.Success()                                │
│   }                                                        │
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ Database:                                                    │
│   DELETE FROM signup_commitments WHERE id = xxx             │
│   UPDATE signup_items SET remaining_quantity = 5            │
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ Frontend Re-renders:                                         │
│   • Item still visible ✅                                   │
│   • userItemCommitment = null                               │
│   • Shows "Sign Up" button ✅                              │
│   • NO "Cancel Sign Up" button ✅                          │
└─────────────────────────────────────────────────────────────┘
```

### Different Path: Open Items (Direct Cancel Button)

```
┌─────────────────────────────────────────────────────────────┐
│ User clicks "Cancel Sign Up" on Open item                   │
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ Frontend: handleCancelOpenItem(signUpListId, itemId)       │
│                                                              │
│   cancelOpenSignUpItem.mutateAsync({                       │
│     eventId,                                                │
│     signupId: signUpListId,                                │
│     itemId: itemId,                                        │
│   })                                                       │
│                                                              │
│   ← DIFFERENT API, NO quantity parameter                   │
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ API: DELETE /events/{id}/signups/{id}/open-items/{id}     │
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ Backend: CancelOpenSignUpItemCommandHandler                 │
│                                                              │
│   if (item.ItemCategory != Open)                           │
│     return Failure("Only Open items...")                   │
│                                                              │
│   if (!item.IsCreatedByUser(userId))                       │
│     return Failure("You can only cancel your own...")      │
│                                                              │
│   Step 1: item.CancelCommitment(userId) ← Cancel first     │
│   Step 2: signUpList.RemoveItem(itemId) ← Then delete      │
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ Domain: SignUpItem.CancelCommitment(userId)                 │
│   RemainingQuantity += commitment.Quantity                  │
│   _commitments.Remove(commitment)                           │
│   RaiseDomainEvent(CommitmentCancelledEvent)                │
│   MarkAsUpdated()                                           │
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ Domain: SignUpList.RemoveItem(itemId)                       │
│   if (item.GetCommitmentCount() > 0)                       │
│     return Failure("Cannot remove item with commitments")  │
│                                                              │
│   _items.Remove(item)  ← DELETE THE ENTIRE ITEM            │
│   MarkAsUpdated()                                           │
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ Database:                                                    │
│   DELETE FROM signup_commitments WHERE id = xxx             │
│   DELETE FROM signup_items WHERE id = yyy  ← Item deleted! │
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ Frontend Re-renders:                                         │
│   • Item NOT visible ✅ (deleted)                           │
│   • Shows "Sign Up" button for Open category ✅            │
└─────────────────────────────────────────────────────────────┘
```

### Broken Path: Registration Cancellation with "Delete Commitments"

```
┌─────────────────────────────────────────────────────────────┐
│ User clicks "Cancel Registration"                           │
│ Checks "Also delete my sign-up commitments"                 │
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ Frontend: Cancellation dialog submits                        │
│   deleteSignUpCommitments: true                             │
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ API: POST /events/{id}/cancel-rsvp                          │
│ Body: { deleteSignUpCommitments: true }                     │
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ Backend: CancelRsvpCommandHandler                           │
│                                                              │
│   registration.Cancel()  ← Cancel registration first        │
│                                                              │
│   if (request.DeleteSignUpCommitments) {                    │
│     event.CancelAllUserCommitments(userId)                 │
│     _eventRepository.Update(event)                         │
│   }                                                        │
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ Domain: Event.CancelAllUserCommitments(userId)              │
│                                                              │
│   foreach (signUpList in _signUpLists) {                   │
│     foreach (item in signUpList.Items) {                   │
│       if (item.Commitments.Any(c => c.UserId == userId)) { │
│         item.CancelCommitment(userId)  ← ONLY THIS         │
│       }                                                    │
│     }                                                      │
│   }                                                        │
│                                                              │
│   ❌ MISSING: Delete Open items created by userId          │
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ Domain: SignUpItem.CancelCommitment(userId)                 │
│   Called for EVERY item (Mandatory, Suggested, AND Open)   │
│                                                              │
│   RemainingQuantity += commitment.Quantity ✅               │
│   _commitments.Remove(commitment) ✅                        │
│   RaiseDomainEvent(CommitmentCancelledEvent) ✅             │
│   MarkAsUpdated() ✅                                        │
│                                                              │
│   ❌ Doesn't know/care if this is an Open item              │
│   ❌ Doesn't delete the item                                │
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ Database:                                                    │
│   DELETE FROM signup_commitments WHERE userId = xxx ✅      │
│   UPDATE signup_items SET remaining_quantity = ... ✅       │
│   ❌ NO DELETE for Open items                              │
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ Frontend Re-renders:                                         │
│   • Mandatory items still visible ✅                        │
│   • Suggested items still visible ✅                        │
│   • Open items STILL VISIBLE ❌ (should be deleted!)       │
│   • Shows "Sign Up" button for Open items ❌                │
│   • User sees "Your item" badge ❌                          │
│   • User confused: "I cancelled, why is my Lasagna here?"   │
└─────────────────────────────────────────────────────────────┘
```

---

## The Fix: Enhanced CancelAllUserCommitments()

```
┌─────────────────────────────────────────────────────────────┐
│ Domain: Event.CancelAllUserCommitments(userId)              │
│                                                              │
│   var itemsToRemove = new List<(Guid listId, Guid itemId)>│
│                                                              │
│   foreach (signUpList in _signUpLists) {                   │
│     foreach (item in signUpList.Items) {                   │
│       if (item.Commitments.Any(c => c.UserId == userId)) { │
│                                                              │
│         // Step 1: Cancel commitment (SAME AS BEFORE)       │
│         item.CancelCommitment(userId) ✅                   │
│                                                              │
│         // Step 2: NEW - Check if Open item created by user│
│         if (item.IsOpenItem() &&                           │
│             item.IsCreatedByUser(userId)) {                │
│           itemsToRemove.Add((signUpList.Id, item.Id))     │
│         }                                                  │
│       }                                                    │
│     }                                                      │
│   }                                                        │
│                                                              │
│   // Step 3: NEW - Remove Open items                       │
│   foreach (var (listId, itemId) in itemsToRemove) {       │
│     var list = _signUpLists.Find(s => s.Id == listId)     │
│     list.RemoveItem(itemId)  ✅                            │
│   }                                                        │
│                                                              │
│   MarkAsUpdated()                                           │
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ Database:                                                    │
│   DELETE FROM signup_commitments WHERE userId = xxx ✅      │
│   UPDATE signup_items SET remaining_quantity = ... ✅       │
│   DELETE FROM signup_items WHERE createdByUserId = xxx ✅   │
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ Frontend Re-renders:                                         │
│   • Mandatory items still visible ✅                        │
│   • Suggested items still visible ✅                        │
│   • Open items DELETED ✅                                   │
│   • Shows "Sign Up" button for Open category ✅            │
│   • User satisfied: "Perfect! Everything is cancelled."     │
└─────────────────────────────────────────────────────────────┘
```

---

## Why the Bug Exists

### Design History

1. **Initially**: Only Mandatory/Suggested items existed
   - Organizer creates items
   - Users commit to items
   - Items are permanent (survive commitment cancellation)

2. **Phase 6A.27**: Open items added
   - Users create their own items
   - User auto-commits to their item
   - Items are user-owned (should be deleted when user cancels)

3. **Two Cancellation Paths Emerged**:

   **Path A**: Direct "Cancel Sign Up" button
   - Creates dedicated `CancelOpenSignUpItemCommand`
   - Correctly deletes commitment + item ✅

   **Path B**: Registration cancellation
   - Reuses existing `Event.CancelAllUserCommitments()`
   - Only deletes commitments, not items ❌

### Why Different Paths?

**Mandatory/Suggested Design**:
```
User → Commits → Can cancel commitment → Item persists for others
```

**Open Items Design**:
```
User → Creates item → Auto-commits → Can cancel entire item → Item deleted
```

**The Oversight**: `Event.CancelAllUserCommitments()` was designed for permanent items. It was never updated when Open items (user-owned items) were added.

---

## Summary Table

| Aspect | Mandatory | Suggested | Open (Current) | Open (Fixed) |
|--------|-----------|-----------|----------------|--------------|
| **Created By** | Organizer | Organizer | User | User |
| **Lifecycle** | Permanent | Permanent | User-owned | User-owned |
| **Cancel via Button** | Keeps item ✅ | Keeps item ✅ | Deletes item ✅ | Deletes item ✅ |
| **Cancel via Registration** | Keeps item ✅ | Keeps item ✅ | **Keeps item ❌** | **Deletes item ✅** |
| **Entity** | `SignUpItem` | `SignUpItem` | `SignUpItem` | `SignUpItem` |
| **Domain Method** | `CancelCommitment()` | `CancelCommitment()` | `CancelCommitment()` | `CancelCommitment()` + `RemoveItem()` |

**The Fix**: Make registration cancellation behave the same as direct button cancellation for Open items.
