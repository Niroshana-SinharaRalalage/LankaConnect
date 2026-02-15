# Root Cause Analysis: Signup Forms UI/UX Issues

**Date:** 2026-02-15
**Analyst:** Claude (SPARC Architecture Agent)
**Issue Category:** UI/Frontend (React Query Cache + UX Pattern)
**Severity:** Medium (UX degradation, no data loss)

---

## Executive Summary

Four related UX issues affect the Signup Forms management interface, all stemming from **navigation-based state refresh** rather than **reactive cache updates**. The application uses `router.push()` to redirect after mutations, relying on page reload to show updated data instead of leveraging React Query's cache invalidation. Additionally, toast notifications are used instead of the app's preferred inline message pattern.

**Issue Category:** **Frontend UI/UX** (React Query cache management + notification pattern inconsistency)

---

## Issue Summary

### Issue 1: Create Form Success - Toast Instead of Inline Message
**Symptom:** After creating a Signup Form, system shows success **toast message**, but user prefers **inline message** (green banner with checkmark icon)

**Location:** `web/src/app/events/[id]/manage/create-form/page.tsx:92-93`

### Issue 2: Create Form - UI Doesn't Update Without Refresh
**Symptom:** After creating a form, it doesn't appear in the Signup Forms tab until browser refresh

**Location:** `web/src/app/events/[id]/manage/create-form/page.tsx:93` (navigation-based refresh)

### Issue 3: Publish Form Success - Toast Instead of Inline Message
**Symptom:** After publishing a Signup Form, system shows success **toast message**, but user prefers **inline message**

**Location:** `web/src/presentation/components/features/events/FormManagementSection.tsx:43-44`

### Issue 4: Publish Form - UI Doesn't Update Without Refresh
**Symptom:** After publishing a form, the status badge doesn't update from "Draft" to "Active" until browser refresh

**Location:** `web/src/presentation/components/features/events/FormManagementSection.tsx:42-70` (mutation callbacks)

---

## Investigation Findings

### 1. Component Architecture

```
┌─────────────────────────────────────────────────────┐
│ EventManagePage (/events/[id]/manage?tab=forms)    │
│  - Uses useSearchParams() to read ?tab=forms        │
│  - Renders TabPanel with EventFormsTab              │
└─────────────────────┬───────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────┐
│ EventFormsTab Component                             │
│  - Fetches forms: useEventForms(eventId)            │
│  - Passes forms to FormManagementSection            │
└─────────────────────┬───────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────┐
│ FormManagementSection Component                     │
│  - Displays form cards with status badges           │
│  - Handles publish/close/reopen/delete mutations    │
│  - Uses toast notifications (NOT inline messages)   │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│ Create Form Page (/events/[id]/manage/create-form) │
│  - Form builder with drag-drop questions            │
│  - OnSuccess: router.push() to manage?tab=forms     │
│  - Uses toast notifications (NOT inline messages)   │
└─────────────────────────────────────────────────────┘
```

### 2. Current Create Form Flow (Issues 1 & 2)

**File:** `web/src/app/events/[id]/manage/create-form/page.tsx`

```typescript
// Lines 90-99: Create form mutation
const createFormMutation = useCreateEventForm({
  onSuccess: () => {
    toast.success('Form created successfully');  // ❌ Issue 1: Toast instead of inline
    router.push(`/events/${eventId}/manage?tab=forms`);  // ❌ Issue 2: Navigation refresh
  },
  onError: (error) => {
    setSubmitError(error.message || 'Failed to create form');
    toast.error(error.message || 'Failed to create form');
  },
});
```

**Problem Analysis:**
1. ❌ **Toast notification** used instead of inline message banner
2. ❌ **Navigation-based refresh**: `router.push()` triggers page navigation, which:
   - Unmounts current component
   - Mounts manage page
   - Re-fetches forms list via `useEventForms()`
   - Relies on page reload to show new form
3. ❌ **No optimistic update**: User doesn't see the new form immediately
4. ❌ **Inconsistent with app patterns**: Other create flows use inline messages

### 3. React Query Hook Analysis

**File:** `web/src/presentation/hooks/useEventForms.ts`

```typescript
// Lines 275-292: Create form mutation
export function useCreateEventForm(
  options?: UseMutationOptions<
    string,
    ApiError,
    { eventId: string; request: CreateEventFormRequest }
  >
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ eventId, request }) => eventsRepository.createEventForm(eventId, request),
    onSuccess: (_, { eventId }) => {
      // ✅ CORRECTLY invalidates cache
      queryClient.invalidateQueries({ queryKey: formKeys.list(eventId) });
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(eventId) });
    },
    ...options,
  });
}
```

**Analysis:**
- ✅ **Cache invalidation is CORRECT** - The hook properly invalidates:
  - `formKeys.list(eventId)` - Forms list cache
  - `eventKeys.detail(eventId)` - Event detail cache
- ❌ **Problem is in component usage** - The `router.push()` in component's onSuccess handler navigates away BEFORE the cache invalidation can trigger UI updates

### 4. Current Publish Form Flow (Issues 3 & 4)

**File:** `web/src/presentation/components/features/events/FormManagementSection.tsx`

```typescript
// Lines 42-70: Publish mutation
const publishForm = usePublishEventForm({
  onSuccess: () => toast.success('Form published successfully'),  // ❌ Issue 3: Toast
  onError: (error) => toast.error(error.message || 'Failed to publish form'),
});

// Handler
const handlePublish = async (formId: string) => {
  await publishForm.mutateAsync({ eventId, formId });  // ❌ Issue 4: No immediate UI update
};
```

**React Query Hook:**
```typescript
// Lines 379-392: Publish form mutation
export function usePublishEventForm(
  options?: UseMutationOptions<void, ApiError, { eventId: string; formId: string }>
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ eventId, formId }) => eventsRepository.publishEventForm(eventId, formId),
    onSuccess: (_, { eventId, formId }) => {
      queryClient.invalidateQueries({ queryKey: formKeys.list(eventId) });  // ✅ Correct
      queryClient.invalidateQueries({ queryKey: formKeys.detail(eventId, formId) });  // ✅ Correct
    },
    ...options,
  });
}
```

**Problem Analysis:**
1. ❌ **Toast notification** used instead of inline message
2. ❌ **Async timing issue**: The `await publishForm.mutateAsync()` completes, but:
   - Cache invalidation is async
   - UI doesn't re-render until React Query detects stale data
   - User sees stale "Draft" badge until refetch completes
3. ✅ **Cache invalidation is CORRECT** - Hook properly invalidates both list and detail caches
4. ❌ **No immediate visual feedback** - Status badge remains "Draft" until refetch

### 5. Inline Message Pattern Analysis

**Pattern Found:** `bg-green-50 border border-green-200 rounded-lg` with CheckCircle icon

**Example 1:** Form submission success (Phase 6A.115)
**File:** `web/src/app/events/[id]/forms/[formId]/page.tsx:419-426`

```tsx
{successMessage && (
  <div className="mt-4 p-4 bg-green-50 border border-green-200 rounded-lg flex items-start gap-3">
    <CheckCircle className="w-5 h-5 text-green-600 flex-shrink-0 mt-0.5" />
    <div>
      <p className="text-sm font-medium text-green-900">{successMessage}</p>
    </div>
  </div>
)}
```

**Example 2:** Resend confirmation success
**File:** `web/src/presentation/components/features/events/ResendConfirmationDialog.tsx:113-125`

```tsx
{status === 'success' && (
  <div className="p-4 bg-green-50 border border-green-200 rounded-lg flex items-start gap-3">
    <CheckCircle className="w-5 h-5 text-green-600 flex-shrink-0 mt-0.5" />
    <div className="flex-1">
      <p className="text-sm font-medium text-green-900">
        Confirmation email sent successfully
      </p>
      <p className="text-xs text-green-700 mt-1">
        Email sent to {attendee.email}
      </p>
    </div>
  </div>
)}
```

**Pattern Characteristics:**
- ✅ **Inline placement** - Appears near action button (not global toast)
- ✅ **Persistent** - Stays visible until user navigates away
- ✅ **Contextual** - Related to specific form/action
- ✅ **Icon-based** - CheckCircle for success, AlertCircle/XCircle for errors
- ✅ **Semantic colors** - Green for success, Red for errors

### 6. Similar Patterns in Codebase

**Comparison with Phase 6A.111.1 (Form Update Timeout Fix):**

**File:** `web/src/presentation/hooks/useEventForms.ts:712-760`

```typescript
// Phase 6A.111: Comprehensive cache invalidation
onSuccess: (_, { eventId, formId, accessToken }) => {
  // 1. Invalidate specific response (token-based)
  if (accessToken) {
    queryClient.invalidateQueries({ queryKey: formKeys.myResponse(eventId, formId, accessToken) });
  }

  // 2. Invalidate user-based response query
  queryClient.invalidateQueries({ queryKey: ['formResponse', 'my', eventId, formId] });

  // 3. Invalidate form detail
  queryClient.invalidateQueries({ queryKey: formKeys.detail(eventId, formId) });

  // 4. Invalidate ALL paginated responses
  queryClient.invalidateQueries({
    queryKey: formKeys.responsesList(eventId, formId),
    exact: false  // Invalidates all pages
  });

  // 5. Invalidate form list
  queryClient.invalidateQueries({ queryKey: formKeys.list(eventId) });

  // 6. Invalidate ALL form queries (wildcard)
  queryClient.invalidateQueries({ queryKey: formKeys.all });

  // 7. Immediate refetch (don't wait for staleTime)
  queryClient.refetchQueries({ queryKey: formKeys.detail(eventId, formId) });
},
```

**Key Differences:**
- ✅ **Immediate refetch** - Forces UI update without waiting for staleTime
- ✅ **Wildcard invalidation** - Invalidates `formKeys.all` to catch all related queries
- ✅ **No navigation** - Stays on same page, UI updates reactively

---

## Root Cause Identification

### Issue 1 & 3: Toast vs Inline Messages

**Root Cause:** Inconsistent notification pattern selection

**Why Toast Was Used:**
- Form creation/publish operations are perceived as "global" actions
- Toast pattern was copied from other mutation examples without considering UX consistency
- No documented pattern guidelines for when to use toast vs inline messages

**User Preference:**
- Inline messages are more contextual and persistent
- User can see success message while reviewing the form list
- Toast disappears after 3-5 seconds, potentially missed

### Issue 2: Create Form - No Immediate UI Update

**Root Cause:** Navigation-based state refresh instead of reactive cache updates

**Technical Explanation:**
1. Component calls `router.push(`/events/${eventId}/manage?tab=forms`)`
2. Navigation triggers component unmount
3. React Query's `invalidateQueries()` runs AFTER navigation starts
4. Manage page mounts and fetches forms with `useEventForms()`
5. New form appears ONLY because of fresh fetch, not cache update

**Why This Happens:**
- `onSuccess` handler in component overrides the reactive cache pattern
- Developer prioritized "redirect to list" over "update list in place"
- Pattern mimics traditional server-side form submission (POST → Redirect → GET)

**Correct Pattern:**
- Stay on manage page
- Show inline success message
- Let React Query cache invalidation trigger `useEventForms()` refetch
- UI updates automatically when new data arrives

### Issue 4: Publish Form - Status Badge Not Updating

**Root Cause:** Async cache invalidation timing + no immediate refetch

**Technical Explanation:**
1. User clicks "Publish" button
2. `publishForm.mutateAsync()` calls API
3. API returns 200 OK
4. `onSuccess` callback runs `invalidateQueries()`
5. React Query marks cache as stale but doesn't refetch immediately (due to `staleTime: 5 minutes`)
6. User sees old "Draft" badge until:
   - Manual refresh
   - Window focus (triggers `refetchOnWindowFocus: true`)
   - 5 minutes pass (staleTime expires)

**React Query Config:**
```typescript
export function useEventForms(eventId: string | undefined) {
  return useQuery({
    queryKey: formKeys.list(eventId || ''),
    queryFn: () => eventsRepository.getEventForms(eventId!),
    enabled: !!eventId,
    staleTime: 5 * 60 * 1000, // 5 minutes - ❌ Delays refetch after invalidation
    refetchOnWindowFocus: true,
    retry: 1,
  });
}
```

**Why This Happens:**
- `invalidateQueries()` only marks cache as stale
- Doesn't trigger immediate refetch if `staleTime` hasn't elapsed
- `refetchOnWindowFocus` works, but user shouldn't need to switch tabs

**Solution Used in Phase 6A.111:**
```typescript
queryClient.refetchQueries({ queryKey: formKeys.detail(eventId, formId) });
// Forces immediate refetch, bypassing staleTime
```

---

## Comparison with Similar Flows

### ✅ Good Example: Form Response Update (Phase 6A.111)

**File:** `web/src/presentation/hooks/useEventForms.ts:712-760`

**Pattern:**
- ✅ Comprehensive cache invalidation (7 levels)
- ✅ Immediate refetch with `refetchQueries()`
- ✅ No navigation - stays on same page
- ✅ UI updates reactively

**Result:** User sees changes immediately without manual refresh

### ❌ Bad Example: Signup List Creation

**File:** `web/src/app/events/[id]/manage/create-signup-list/page.tsx:278`

```typescript
// Navigate to manage page after successful creation
router.push(`/events/${eventId}/manage`);
```

**Pattern:**
- ❌ Navigation-based refresh
- ❌ Relies on fresh fetch after page load
- ❌ User leaves creation page immediately

**Why It Works:** Different UX flow - user doesn't need to stay on creation page

### ✅ Good Example: Event Form Submission

**File:** `web/src/app/events/[id]/forms/[formId]/page.tsx:419-426`

**Pattern:**
- ✅ Inline success message (green banner with CheckCircle)
- ✅ Persistent until user navigates away
- ✅ Contextual placement near submit button

**Result:** Clear, persistent feedback for user

---

## Comprehensive Fix Plan

### Fix 1: Replace Toast with Inline Success Message (Create Form)

**File:** `web/src/app/events/[id]/manage/create-form/page.tsx`

**Changes Required:**

1. **Add state for success message:**
```typescript
const [showSuccessMessage, setShowSuccessMessage] = useState(false);
const [createdFormTitle, setCreatedFormTitle] = useState('');
```

2. **Update mutation callback:**
```typescript
const createFormMutation = useCreateEventForm({
  onSuccess: (formId, { request }) => {
    setCreatedFormTitle(request.title);
    setShowSuccessMessage(true);
    // Don't navigate - stay on page to show message
    // router.push() removed
  },
  onError: (error) => {
    setSubmitError(error.message || 'Failed to create form');
  },
});
```

3. **Add inline success message UI (after form card):**
```tsx
{showSuccessMessage && (
  <div className="mb-8 p-4 bg-green-50 border border-green-200 rounded-lg flex items-start gap-3">
    <CheckCircle className="w-5 h-5 text-green-600 flex-shrink-0 mt-0.5" />
    <div className="flex-1">
      <p className="text-sm font-medium text-green-900">
        Form created successfully
      </p>
      <p className="text-sm text-green-700 mt-1">
        "{createdFormTitle}" has been created as a draft. You can now add more forms or go to manage page to publish it.
      </p>
      <div className="mt-3 flex gap-3">
        <Button
          size="sm"
          onClick={() => router.push(`/events/${eventId}/manage?tab=forms`)}
        >
          Go to Signup Forms
        </Button>
        <Button
          variant="outline"
          size="sm"
          onClick={() => {
            setShowSuccessMessage(false);
            // Reset form to create another
            setTitle('');
            setDescription('');
            setQuestions([]);
          }}
        >
          Create Another Form
        </Button>
      </div>
    </div>
  </div>
)}
```

4. **Remove toast import:**
```typescript
// Remove: import toast from 'react-hot-toast';
// Remove: toast.success('Form created successfully');
```

**Result:**
- ✅ Inline success message with contextual actions
- ✅ User can create another form or navigate to manage
- ✅ Persistent message until user takes action

---

### Fix 2: Enable Reactive UI Update (Create Form)

**File:** `web/src/app/events/[id]/manage/create-form/page.tsx`

**Changes Required:**

1. **Remove automatic navigation:**
```typescript
// BEFORE:
onSuccess: () => {
  toast.success('Form created successfully');
  router.push(`/events/${eventId}/manage?tab=forms`);  // ❌ Remove
},

// AFTER:
onSuccess: (formId, { request }) => {
  setCreatedFormTitle(request.title);
  setShowSuccessMessage(true);
  // User navigates manually via button in success message
},
```

2. **Cache invalidation already works** - No changes needed in `useCreateEventForm` hook

**Result:**
- ✅ User stays on creation page
- ✅ Can create multiple forms sequentially
- ✅ Manual navigation via success message button
- ✅ Manage page shows new form immediately when navigated (cache already invalidated)

---

### Fix 3: Replace Toast with Inline Success Message (Publish Form)

**File:** `web/src/presentation/components/features/events/FormManagementSection.tsx`

**Changes Required:**

1. **Add state for success message:**
```typescript
const [successMessage, setSuccessMessage] = useState<string | null>(null);
const [successFormTitle, setSuccessFormTitle] = useState<string>('');
```

2. **Update mutation callbacks:**
```typescript
const publishForm = usePublishEventForm({
  onSuccess: () => {
    // Don't use toast
    // Success handled in component UI
  },
  onError: (error) => toast.error(error.message || 'Failed to publish form'),
});

const handlePublish = async (formId: string) => {
  const form = forms.find(f => f.id === formId);
  await publishForm.mutateAsync({ eventId, formId });
  setSuccessFormTitle(form?.title || 'Form');
  setSuccessMessage('published');
  setTimeout(() => setSuccessMessage(null), 5000); // Auto-hide after 5s
};
```

3. **Add inline success message UI (above forms grid):**
```tsx
{successMessage && (
  <div className="mb-6 p-4 bg-green-50 border border-green-200 rounded-lg flex items-start gap-3">
    <CheckCircle className="w-5 h-5 text-green-600 flex-shrink-0 mt-0.5" />
    <div className="flex-1">
      <p className="text-sm font-medium text-green-900">
        {successMessage === 'published' && `"${successFormTitle}" published successfully`}
        {successMessage === 'closed' && `"${successFormTitle}" closed successfully`}
        {successMessage === 'reopened' && `"${successFormTitle}" reopened successfully`}
        {successMessage === 'deleted' && `Form deleted successfully`}
      </p>
    </div>
    <button
      onClick={() => setSuccessMessage(null)}
      className="text-green-600 hover:text-green-800"
    >
      <X className="w-4 h-4" />
    </button>
  </div>
)}
```

4. **Update other mutation handlers similarly:**
```typescript
const handleClose = async (formId: string) => {
  const form = forms.find(f => f.id === formId);
  await closeForm.mutateAsync({ eventId, formId });
  setSuccessFormTitle(form?.title || 'Form');
  setSuccessMessage('closed');
  setTimeout(() => setSuccessMessage(null), 5000);
};

const handleReopen = async (formId: string) => {
  const form = forms.find(f => f.id === formId);
  await reopenForm.mutateAsync({ eventId, formId });
  setSuccessFormTitle(form?.title || 'Form');
  setSuccessMessage('reopened');
  setTimeout(() => setSuccessMessage(null), 5000);
};

const handleDeleteConfirm = async () => {
  if (!formToDelete) return;
  await deleteForm.mutateAsync({ eventId, formId: formToDelete });
  setSuccessMessage('deleted');
  setTimeout(() => setSuccessMessage(null), 5000);
};
```

**Result:**
- ✅ Contextual inline message above forms list
- ✅ Auto-dismisses after 5 seconds
- ✅ Manual dismiss with X button
- ✅ Shows specific form title in message

---

### Fix 4: Enable Immediate Status Badge Update (Publish Form)

**File:** `web/src/presentation/hooks/useEventForms.ts`

**Changes Required:**

1. **Add immediate refetch to publish mutation:**
```typescript
export function usePublishEventForm(
  options?: UseMutationOptions<void, ApiError, { eventId: string; formId: string }>
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ eventId, formId }) => eventsRepository.publishEventForm(eventId, formId),
    onSuccess: (_, { eventId, formId }) => {
      // Existing invalidations
      queryClient.invalidateQueries({ queryKey: formKeys.list(eventId) });
      queryClient.invalidateQueries({ queryKey: formKeys.detail(eventId, formId) });

      // ✅ NEW: Force immediate refetch (bypass staleTime)
      queryClient.refetchQueries({ queryKey: formKeys.list(eventId) });
    },
    ...options,
  });
}
```

2. **Apply same pattern to close/reopen mutations:**
```typescript
export function useCloseEventForm(
  options?: UseMutationOptions<void, ApiError, { eventId: string; formId: string }>
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ eventId, formId }) => eventsRepository.closeEventForm(eventId, formId),
    onSuccess: (_, { eventId, formId }) => {
      queryClient.invalidateQueries({ queryKey: formKeys.list(eventId) });
      queryClient.invalidateQueries({ queryKey: formKeys.detail(eventId, formId) });
      queryClient.refetchQueries({ queryKey: formKeys.list(eventId) }); // ✅ Immediate refetch
    },
    ...options,
  });
}

export function useReopenEventForm(
  options?: UseMutationOptions<void, ApiError, { eventId: string; formId: string }>
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ eventId, formId }) => eventsRepository.reopenEventForm(eventId, formId),
    onSuccess: (_, { eventId, formId }) => {
      queryClient.invalidateQueries({ queryKey: formKeys.list(eventId) });
      queryClient.invalidateQueries({ queryKey: formKeys.detail(eventId, formId) });
      queryClient.refetchQueries({ queryKey: formKeys.list(eventId) }); // ✅ Immediate refetch
    },
    ...options,
  });
}
```

**Result:**
- ✅ Status badge updates immediately (Draft → Active, Active → Closed, etc.)
- ✅ User sees change without manual refresh or window focus
- ✅ Consistent with Phase 6A.111 pattern

---

## File Change Summary

### Files to Modify:

1. **`web/src/app/events/[id]/manage/create-form/page.tsx`**
   - Add `showSuccessMessage` and `createdFormTitle` state
   - Update `createFormMutation` onSuccess callback (remove router.push, set success state)
   - Remove toast import and usage
   - Add inline success message component with action buttons
   - Import CheckCircle from lucide-react

2. **`web/src/presentation/components/features/events/FormManagementSection.tsx`**
   - Add `successMessage` and `successFormTitle` state
   - Update all mutation handlers (publish, close, reopen, delete) to set success message
   - Remove toast success calls (keep error toasts)
   - Add inline success message component above forms grid
   - Import CheckCircle and X from lucide-react

3. **`web/src/presentation/hooks/useEventForms.ts`**
   - Add `refetchQueries()` to `usePublishEventForm` onSuccess
   - Add `refetchQueries()` to `useCloseEventForm` onSuccess
   - Add `refetchQueries()` to `useReopenEventForm` onSuccess
   - No changes to `useCreateEventForm` (cache invalidation already correct)

**Total Files:** 3

**Lines Changed:** ~100-150 lines (mostly UI additions, minimal logic changes)

---

## Test Strategy

### Manual Testing Checklist:

#### Test 1: Create Form with Inline Message
- [ ] Navigate to Event Manage → Signup Forms tab
- [ ] Click "Create Form" button
- [ ] Fill form: Title="Test RSVP", add 2 questions
- [ ] Click "Create Form"
- [ ] **Expected:** Green inline success message appears
- [ ] **Expected:** Message shows "Test RSVP" has been created
- [ ] **Expected:** Two buttons: "Go to Signup Forms" and "Create Another Form"
- [ ] Click "Create Another Form"
- [ ] **Expected:** Success message clears, form resets
- [ ] Fill form: Title="Test Survey", add 1 question
- [ ] Click "Create Form"
- [ ] Click "Go to Signup Forms"
- [ ] **Expected:** Both "Test RSVP" and "Test Survey" appear in list immediately

#### Test 2: Publish Form with Immediate Badge Update
- [ ] Navigate to Event Manage → Signup Forms tab
- [ ] Find form with "Draft" status badge
- [ ] Click "Publish" button
- [ ] **Expected:** Button shows "Publishing..." during API call
- [ ] **Expected:** Green inline success message appears above list
- [ ] **Expected:** Status badge changes from "Draft" to "Active" immediately
- [ ] **Expected:** "Publish" button disappears, "Close" button appears
- [ ] **Expected:** Success message auto-dismisses after 5 seconds
- [ ] **Expected:** No manual refresh needed

#### Test 3: Close Form with Immediate Badge Update
- [ ] Find form with "Active" status badge
- [ ] Click "Close" button
- [ ] **Expected:** Green inline success message appears
- [ ] **Expected:** Status badge changes from "Active" to "Closed" immediately
- [ ] **Expected:** "Close" button disappears, "Reopen" button appears

#### Test 4: Reopen Form with Immediate Badge Update
- [ ] Find form with "Closed" status badge
- [ ] Click "Reopen" button
- [ ] **Expected:** Green inline success message appears
- [ ] **Expected:** Status badge changes from "Closed" to "Active" immediately
- [ ] **Expected:** "Reopen" button disappears, "Close" button appears

#### Test 5: Delete Form with Success Message
- [ ] Find draft form with 0 responses
- [ ] Click "Delete" button
- [ ] Confirm in dialog
- [ ] **Expected:** Green inline success message appears
- [ ] **Expected:** Form card disappears from grid immediately
- [ ] **Expected:** Success message auto-dismisses after 5 seconds

#### Test 6: Error Handling
- [ ] Disconnect internet
- [ ] Try to publish form
- [ ] **Expected:** Red toast error appears (toast kept for errors)
- [ ] **Expected:** Status badge remains unchanged
- [ ] Reconnect internet and retry
- [ ] **Expected:** Success flow works

### Edge Cases:

1. **Multiple rapid actions:**
   - Publish form, immediately close it
   - **Expected:** Both success messages appear in sequence

2. **Navigation during success message:**
   - Publish form, immediately switch to Attendees tab
   - Switch back to Signup Forms tab
   - **Expected:** Success message cleared (component unmount)

3. **Browser back button:**
   - Create form, see success message
   - Click "Go to Signup Forms", verify form appears
   - Press browser back button
   - **Expected:** Create form page loads, success message cleared

4. **Concurrent form creation:**
   - Open two tabs with same event
   - Create form in Tab 1
   - Switch to Tab 2, refresh
   - **Expected:** New form appears (cache invalidation works across tabs)

### Automated Testing (Future):

**Test File:** `web/src/presentation/components/features/events/__tests__/FormManagementSection.test.tsx`

```typescript
describe('FormManagementSection', () => {
  it('should show inline success message after publishing form', async () => {
    // Arrange: Render with draft form
    // Act: Click publish button
    // Assert: Inline message appears with CheckCircle icon
    // Assert: Status badge changes to "Active"
  });

  it('should auto-dismiss success message after 5 seconds', async () => {
    // Arrange: Render with draft form
    // Act: Publish form, wait 5 seconds
    // Assert: Success message disappears
  });

  it('should allow manual dismissal of success message', async () => {
    // Arrange: Render with published form
    // Act: Close form, click X button on success message
    // Assert: Success message disappears immediately
  });
});
```

---

## Risk Assessment

### Low Risk:
- ✅ No database changes
- ✅ No API changes
- ✅ No state management changes
- ✅ Pure UI/UX improvements

### Medium Risk:
- ⚠️ React Query cache timing - `refetchQueries()` could cause unnecessary API calls
  - **Mitigation:** Only refetch after mutations, not on every render
  - **Impact:** Minimal - one extra API call per mutation (negligible overhead)

### Rollback Plan:
- If issues occur, revert the 3 modified files
- No data migration needed
- No breaking changes

---

## Performance Considerations

### Before Fix:
- 1 API call on page navigation (after router.push)
- Manual refresh: 1 additional API call
- Window focus refetch: 1 additional API call

### After Fix:
- 1 API call on mutation success (refetchQueries)
- No manual refresh needed
- No unnecessary refetches

**Net Impact:** Neutral or slightly better (fewer user-initiated refreshes)

---

## Similar Issues Prevented

This fix establishes patterns that prevent:

1. **Navigation-based state refresh anti-pattern**
   - Future mutations should use cache invalidation + inline messages
   - Not router.push() + toast

2. **Inconsistent notification patterns**
   - Document when to use toast (global errors) vs inline (contextual success)
   - Update UI_STYLE_GUIDE.md with notification patterns

3. **Stale UI syndrome**
   - Always pair `invalidateQueries()` with `refetchQueries()` for immediate updates
   - Don't rely on staleTime for critical UI updates

---

## Documentation Updates Needed

1. **UI_STYLE_GUIDE.md**
   - Add section: "Notification Patterns"
   - Toast: Global errors, background tasks
   - Inline: Contextual success/errors near action

2. **PARALLEL_AGENT_COORDINATION.md**
   - Add pattern: "React Query Cache Management Best Practices"
   - Example: useEventForms immediate refetch pattern

3. **PROGRESS_TRACKER.md**
   - Log this fix as Phase 6A.116 or next available number

---

## Conclusion

**Root Cause Summary:**
- Issues 1 & 3: Inconsistent notification pattern (toast vs inline)
- Issue 2: Navigation-based refresh instead of reactive cache updates
- Issue 4: Async cache invalidation without immediate refetch

**Fix Complexity:** Low (UI-only changes, no backend/DB changes)

**Implementation Time:** 2-3 hours (includes testing)

**User Impact:** High positive (better UX, immediate feedback, no manual refreshes)

**Pattern Established:** Reactive UI updates with inline messages (consistent with Phase 6A.111)

---

**Next Steps:**
1. Review and approve this RCA
2. Implement fixes in order: Fix 4 → Fix 3 → Fix 2 → Fix 1
3. Test each fix incrementally
4. Deploy to staging
5. User acceptance testing
6. Document pattern in UI_STYLE_GUIDE.md
7. Mark as complete in PROGRESS_TRACKER.md

---

**End of Root Cause Analysis**
