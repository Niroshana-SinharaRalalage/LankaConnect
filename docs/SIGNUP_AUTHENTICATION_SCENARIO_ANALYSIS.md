# Sign-Up List Authentication Scenario Analysis

**Date:** 2025-12-09
**Requested By:** User
**Analysis:** Verify system handles all authentication scenarios correctly

## Requirements

### User's Expected Behavior:

1. **Anonymous users (not LankaConnect members):**
   - ✅ Should be able to register for events
   - ❓ Should be able to sign up for signup lists

2. **User trying to signup for signup list WITHOUT event registration:**
   - ✅ Should be asked to register for the event first

3. **LankaConnect member NOT logged in:**
   - ✅ If trying to signup: Should be asked to LOG IN (not register again)

---

## Current Implementation Analysis

### 1. Event Registration Flow

**Anonymous Registration Support:** ✅ YES

**Evidence:**
- File: `web/src/infrastructure/api/types/events.types.ts`
- Type: `AnonymousRegistrationRequest` exists
- Backend: `POST /api/events/{id}/rsvp-anonymous` endpoint exists
- Frontend: `EventRegistrationForm` handles both anonymous and authenticated users

**Verdict:** ✅ **Anonymous users CAN register for events**

---

### 2. Sign-Up List Commitments

**Anonymous Support:** ❌ NO

**Evidence:**
- File: `src/LankaConnect.API/Controllers/EventsController.cs:1289`
- Endpoint: `POST /api/events/{eventId}/signups/{signupId}/items/{itemId}/commit`
- Authorization: `[Authorize]` attribute **REQUIRES authentication**

```csharp
[HttpPost("{eventId:guid}/signups/{signupId:guid}/items/{itemId:guid}/commit")]
[Authorize]  // ❌ BLOCKS anonymous users
public async Task<IActionResult> CommitToSignUpItem(...)
```

**Frontend Enforcement:**
- File: `web/src/presentation/components/features/events/SignUpManagementSection.tsx:213`
- Logic: Redirects to login if `!userId`

```typescript
if (!userId) {
  router.push(`/login?redirect=/events/${eventId}`);
  return;
}
```

**Verdict:** ❌ **Anonymous users CANNOT commit to signup lists**

**Problem:** Inconsistent with event registration (which allows anonymous)

---

### 3. Email Validation Logic

**Current Implementation:**

**Backend Query:** `GetEventRegistrationByEmailQueryHandler`
- File: `src/LankaConnect.Application/Events/Queries/GetEventRegistrationByEmail/GetEventRegistrationByEmailQueryHandler.cs:26-31`
- Logic: Checks if `Registration.Contact.Email` matches

```csharp
var exists = await _context.Registrations
    .AnyAsync(r =>
        r.EventId == request.EventId &&
        r.Contact != null &&
        r.Contact.Email == request.Email,
        cancellationToken);
```

**What it checks:** ✅ Email is registered for event
**What it DOESN'T check:** ❌ Email belongs to a LankaConnect user account

**Frontend Usage:**
- File: `web/src/presentation/components/features/events/SignUpCommitmentModal.tsx:157`
- Error: "This email is not registered for the event. You must register for the event first."

```typescript
const isRegistered = await eventsRepository.checkEventRegistrationByEmail(eventId, email.trim());

if (!isRegistered) {
  setErrors({
    email: 'This email is not registered for the event. You must register for the event first.'
  });
}
```

**Verdict:** ⚠️ **Partially works, but cannot distinguish user types**

---

## Scenario Coverage Matrix

| Scenario | Expected Behavior | Current Behavior | Status |
|----------|-------------------|------------------|--------|
| **Anonymous user, not registered for event** | Ask to register for event first | ✅ Works - email validation catches this | ✅ |
| **Anonymous user, registered for event** | Allow signup commitment | ❌ Blocks - requires authentication | ❌ |
| **LankaConnect member, not logged in, not registered** | Ask to log in | ⚠️ Redirects to login, but could be clearer | ⚠️ |
| **LankaConnect member, not logged in, registered** | Ask to log in | ⚠️ Redirects to login | ⚠️ |
| **LankaConnect member, logged in, not registered** | Ask to register for event | ✅ Email validation catches this | ✅ |
| **LankaConnect member, logged in, registered** | Allow signup commitment | ✅ Works | ✅ |

---

## Problems Identified

### Problem 1: Anonymous Users Cannot Commit to Sign-Up Lists

**Issue:** Backend API requires authentication (`[Authorize]` attribute)

**Impact:**
- Breaks UX parity with event registration (which allows anonymous)
- Forces all users to create LankaConnect accounts just to commit to bringing items
- Inconsistent user experience

**What's Missing:**
- Anonymous commit endpoint (like `rsvp-anonymous` for events)
- Backend support for CommitToSignUpItem without userId

---

### Problem 2: Cannot Distinguish LankaConnect Members from Anonymous Users

**Issue:** Email validation only checks event registration, not user account existence

**Impact:**
- Cannot show contextual messages:
  - "You're a LankaConnect member! Please log in" vs.
  - "Please register for the event first"
- Suboptimal UX - users don't know if they should log in or register

**What's Missing:**
- Query to check if email belongs to a User account
- Logic to provide different messages based on account status

---

### Problem 3: Redirect to Login for All Non-Authenticated Users

**Issue:** Frontend redirects everyone to login page

**Impact:**
- Anonymous users (who don't have accounts) are sent to login
- Creates confusion - they might think they need an account
- No clear path for anonymous users to commit

**Current Code:**
```typescript
// SignUpManagementSection.tsx:213
if (!userId) {
  router.push(`/login?redirect=/events/${eventId}`);
  return;
}
```

**What Should Happen:**
- Check if email belongs to LankaConnect user
  - YES → Redirect to login
  - NO → Allow anonymous commitment

---

## Recommended Solutions

### Solution 1: Add Anonymous Sign-Up Commitment Support

**Backend Changes:**

1. Create new endpoint: `POST /api/events/{eventId}/signups/{signupId}/items/{itemId}/commit-anonymous`
2. Add `[AllowAnonymous]` attribute
3. Use email + contact info instead of userId

**Frontend Changes:**

1. Update `SignUpCommitmentModal` to support anonymous flow
2. Show email/name/phone fields for anonymous users
3. Call anonymous endpoint when userId is not available

**Benefits:**
- Parity with event registration
- Better user experience
- No forced account creation

---

### Solution 2: Enhanced Email Validation with User Account Check

**Backend Changes:**

1. Update `GetEventRegistrationByEmailQueryHandler` to return:
   ```csharp
   public record RegistrationStatus(
       bool IsRegistered,
       bool HasUserAccount,
       Guid? UserId
   );
   ```

2. Check Users table:
   ```csharp
   var user = await _context.Users
       .FirstOrDefaultAsync(u => u.Email == request.Email);

   return new RegistrationStatus(
       IsRegistered: exists,
       HasUserAccount: user != null,
       UserId: user?.Id
   );
   ```

**Frontend Changes:**

1. Update validation logic:
   ```typescript
   const status = await checkRegistration(email);

   if (!status.isRegistered) {
     if (status.hasUserAccount) {
       // LankaConnect member, not registered
       setError("You're a LankaConnect member! Please register for this event first.");
     } else {
       // Anonymous user, not registered
       setError("Please register for this event first.");
     }
   } else if (status.hasUserAccount && !userId) {
     // LankaConnect member, registered, but not logged in
     setError("You're already registered! Please log in to commit.");
     // Show login link
   } else {
       // Anonymous user, registered - allow commitment
   }
   ```

**Benefits:**
- Contextual error messages
- Better user guidance
- Distinguishes member vs anonymous users

---

### Solution 3: Smart Authentication Flow

**Update `openCommitmentModal()` in SignUpManagementSection:**

```typescript
const openCommitmentModal = async (signUpListId, item, existingCommitment) => {
  if (!userId) {
    // Check if email (from form or profile) belongs to a user account
    const emailToCheck = getEmailFromContext();
    const status = await checkIfUserExists(emailToCheck);

    if (status.hasUserAccount) {
      // LankaConnect member - redirect to login
      router.push(`/login?redirect=/events/${eventId}`);
    } else {
      // Anonymous user - open modal for anonymous commitment
      setCommitModalOpen(true);
      setIsAnonymousCommitment(true);
    }
    return;
  }

  // Logged in - proceed normally
  setCommitModalOpen(true);
};
```

**Benefits:**
- Smart routing based on user type
- No confusion about login vs anonymous
- Seamless experience for both user types

---

## Current Workarounds

### Temporary Solution (Already Implemented):

**File:** `SignUpManagementSection.tsx:213`
- Always redirects to login if not authenticated
- Works for LankaConnect members
- **Doesn't work for anonymous users**

**File:** `SignUpCommitmentModal.tsx:416`
- Shows login link in error message
- Fallback for session expiration
- Still doesn't support anonymous commitment

---

## Next Steps

### High Priority:
1. ✅ **Decision needed:** Should anonymous users be able to commit to signup lists?
   - If YES → Implement Solution 1 + Solution 2
   - If NO → Document this limitation clearly in UI

2. ⚠️ **UX improvement:** Implement Solution 2 (enhanced email validation)
   - Helps distinguish LankaConnect members from anonymous users
   - Provides better error messages

3. ⚠️ **Consistency:** Either:
   - Add anonymous support to signup commitments (like events), OR
   - Require authentication for both events AND signup lists

### Testing Scenarios:
- [ ] Anonymous user, not registered → See "register for event first"
- [ ] Anonymous user, registered → Can commit to signup lists
- [ ] LankaConnect member, not logged in → See "log in" message
- [ ] LankaConnect member, logged in → Can commit normally

---

## Summary

**What Works:**
- ✅ Anonymous event registration
- ✅ Email validation for event registration
- ✅ Redirect to login for unauthenticated users

**What Doesn't Work:**
- ❌ Anonymous users cannot commit to signup lists
- ❌ Cannot distinguish LankaConnect members from anonymous users
- ❌ Inconsistent UX between events and signup lists

**Recommendation:**
Implement **Solution 1** (anonymous signup commitment) + **Solution 2** (enhanced validation) for complete coverage of all scenarios.
