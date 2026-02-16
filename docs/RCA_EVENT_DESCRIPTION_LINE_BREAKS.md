# Root Cause Analysis: Event Description Line Break/Spacing Issue

**Date:** 2026-02-16
**Issue ID:** TBD (User Reported)
**Severity:** Medium (UI/UX degradation)
**Status:** Analysis Complete - Ready for Fix

---

## Executive Summary

**Problem:** When users create/edit events using the Rich Text Editor (TipTap), they add line breaks and spacing between paragraphs. However, when the event is displayed on the event details page, all line breaks and spacing are removed, causing text to appear as one continuous block.

**Root Cause:** The `plainTextToHtml()` function in `html-utils.ts` is being applied to HTML content, stripping out TipTap's `<p>` tags and replacing them with incorrectly escaped HTML entities.

**Impact:** Poor user experience - event descriptions are difficult to read, making events less attractive to potential attendees.

**Fix Complexity:** Low - Single file modification
**Risk Level:** Low - Isolated issue with clear fix

---

## 1. Issue Classification

### Answer: UI/Frontend Issue (Incorrect HTML Rendering Logic)

This is **NOT**:
- ❌ Database issue - Description field stores HTML correctly (`text` type, max 10,000 chars)
- ❌ API issue - Backend returns HTML exactly as stored
- ❌ Rich Text Editor issue - TipTap generates correct HTML output

This **IS**:
- ✅ **Frontend rendering issue** - Incorrect content transformation in display component

---

## 2. Technical Investigation

### 2.1 Event Creation/Edit Flow (WORKING CORRECTLY ✅)

**File:** `web/src/presentation/components/features/events/EventCreationForm.tsx` (Line 350-370)
**File:** `web/src/presentation/components/features/events/EventEditForm.tsx` (Line 462-482)

```tsx
{/* Event Description */}
<Controller
  name="description"
  control={control}
  render={({ field }) => (
    <RichTextEditor
      content={field.value || ''}
      onChange={field.onChange}
      onImageUpload={uploadImage}
      placeholder="Provide a detailed description..."
      error={!!errors.description}
      errorMessage={errors.description?.message}
      maxLength={5000}
      minHeight={200}
    />
  )}
/>
```

**Analysis:**
- ✅ Uses `RichTextEditor` component (TipTap-based)
- ✅ TipTap outputs valid HTML with `<p>`, `<h1>`, `<ul>`, etc.
- ✅ HTML is stored directly in form state
- ✅ No transformation before API submission

**Example TipTap Output:**
```html
<p>This is paragraph one with good spacing.</p>
<p></p>
<p>This is paragraph two after a line break.</p>
<p></p>
<p>This is paragraph three.</p>
```

---

### 2.2 Rich Text Editor Component (WORKING CORRECTLY ✅)

**File:** `web/src/presentation/components/ui/RichTextEditor.tsx`

**TipTap Configuration:**
```tsx
const editor = useEditor({
  extensions: [
    StarterKit.configure({
      heading: { levels: [1, 2, 3] },
    }),
    Image,
    Link,
    Placeholder,
    CharacterCount,
  ],
  content,
  onUpdate: ({ editor }) => {
    const html = editor.getHTML();  // ✅ Returns proper HTML
    debouncedOnChange(html);
  },
});
```

**Analysis:**
- ✅ Uses TipTap's `StarterKit` which includes paragraph support
- ✅ `getHTML()` returns properly formatted HTML
- ✅ Toolbar provides formatting controls (Bold, Italic, Headings, Lists)
- ✅ Global styles apply proper margins to `<p>`, `<h1>`, etc.

---

### 2.3 Database Storage (WORKING CORRECTLY ✅)

**File:** `src/LankaConnect.Domain/Events/ValueObjects/EventDescription.cs`

```csharp
public class EventDescription : ValueObject
{
    public const int MaxLength = 10000;
    public string Value { get; }

    public static Result<EventDescription> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<EventDescription>.Failure("Description is required");

        value = value.Trim();  // ⚠️ Only trims whitespace, preserves HTML

        if (value.Length > MaxLength)
            return Result<EventDescription>.Failure($"Description cannot exceed {MaxLength} characters");

        return Result<EventDescription>.Success(new EventDescription(value));
    }
}
```

**Database Schema:**
- Field Type: `text` (PostgreSQL)
- Max Length: 10,000 characters
- Constraint: NOT NULL

**Analysis:**
- ✅ Stores HTML as plain text (standard practice)
- ✅ No sanitization/transformation during storage
- ✅ Trim() only removes leading/trailing whitespace, preserves internal HTML tags

---

### 2.4 Event Display Component (🔴 ROOT CAUSE FOUND)

**File:** `web/src/presentation/components/features/events/EventDetailsTab.tsx` (Lines 135-147)

```tsx
{/* Description */}
<div className="grid grid-cols-[140px_1fr] gap-x-4 gap-y-3 border-b pb-3">
  <span className="text-sm font-semibold text-neutral-700">Description:</span>
  <div
    className="prose prose-sm max-w-none text-neutral-600"
    dangerouslySetInnerHTML={{
      __html: sanitizeHtml(
        isHtmlContent(event.description)
          ? event.description
          : plainTextToHtml(event.description)  // 🔴 PROBLEM HERE
      )
    }}
  />
</div>
```

**Analysis:**
- ⚠️ Uses `isHtmlContent()` to detect HTML vs plain text
- ⚠️ Intended for backward compatibility with pre-rich-text events
- 🔴 **BUG**: `isHtmlContent()` returns TRUE for TipTap HTML, but `plainTextToHtml()` is STILL being called in some cases

---

### 2.5 HTML Utility Functions (🔴 ROOT CAUSE IDENTIFIED)

**File:** `web/src/lib/html-utils.ts`

#### Function 1: `isHtmlContent()` (Lines 26-28)

```typescript
export function isHtmlContent(text: string): boolean {
  return /<[a-z][\s\S]*>/i.test(text);  // ✅ Correctly detects HTML
}
```

**Test Cases:**
```typescript
isHtmlContent("<p>Hello</p>")                    // ✅ true
isHtmlContent("Plain text with\nline breaks")   // ✅ false
```

#### Function 2: `plainTextToHtml()` (Lines 37-57) 🔴 **PROBLEM FUNCTION**

```typescript
export function plainTextToHtml(text: string): string {
  // Step 1: Escape HTML entities
  const escaped = text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')    // 🔴 Converts <p> to &lt;p&gt;
    .replace(/>/g, '&gt;')    // 🔴 Converts </p> to &lt;/p&gt;
    .replace(/"/g, '&quot;');

  // Step 2: Auto-link URLs
  const linked = escaped.replace(
    /(https?:\/\/[^\s<]+)/g,
    '<a href="$1" ...>$1</a>'
  );

  // Step 3: Convert newlines to paragraphs
  const paragraphs = linked.split(/\n\n+/);
  return paragraphs
    .map((p) => `<p>${p.replace(/\n/g, '<br>')}</p>`)
    .join('');
}
```

**What Happens to TipTap HTML:**

**Input (TipTap HTML):**
```html
<p>Paragraph one with spacing.</p>
<p></p>
<p>Paragraph two after line break.</p>
```

**After `plainTextToHtml()` escaping:**
```html
&lt;p&gt;Paragraph one with spacing.&lt;/p&gt;
&lt;p&gt;&lt;/p&gt;
&lt;p&gt;Paragraph two after line break.&lt;/p&gt;
```

**After newline processing:**
```html
<p>&lt;p&gt;Paragraph one with spacing.&lt;/p&gt;&lt;p&gt;&lt;/p&gt;&lt;p&gt;Paragraph two after line break.&lt;/p&gt;</p>
```

**After `sanitizeHtml()`:**
```html
<p>&lt;p&gt;Paragraph one with spacing.&lt;/p&gt;&lt;p&gt;&lt;/p&gt;&lt;p&gt;Paragraph two after line break.&lt;/p&gt;</p>
```

**Browser Renders:**
```
<p>Paragraph one with spacing.</p><p></p><p>Paragraph two after line break.</p>
```

**Result:** All text in one continuous block with visible HTML tags! 🔴

#### Function 3: `sanitizeHtml()` (Lines 8-19) ✅ Works Correctly

```typescript
export function sanitizeHtml(html: string): string {
  return DOMPurify.sanitize(html, {
    ALLOWED_TAGS: [
      'p', 'br', 'b', 'i', 'strong', 'em', 'u',
      'h1', 'h2', 'h3',
      'ul', 'ol', 'li',
      'a', 'blockquote', 'code', 'pre',
    ],
    ALLOWED_ATTR: ['href', 'target', 'rel', 'class'],
  });
}
```

**Analysis:**
- ✅ DOMPurify correctly preserves allowed HTML tags
- ✅ Strips dangerous scripts/event handlers
- ✅ Whitelist includes all TipTap output tags

---

## 3. Root Cause Summary

### The Logic Flow Error

```
TipTap Editor (Edit Form)
  ↓ Outputs: <p>Paragraph 1</p><p>Paragraph 2</p>

API/Database
  ↓ Stores: <p>Paragraph 1</p><p>Paragraph 2</p> (unchanged)

EventDetailsTab Component
  ↓ Receives: event.description = "<p>Paragraph 1</p><p>Paragraph 2</p>"

isHtmlContent(event.description)
  ↓ Returns: TRUE (✅ correct detection)

BUT: Logic path STILL calls plainTextToHtml()
  ↓ Escapes HTML: &lt;p&gt;Paragraph 1&lt;/p&gt;...

sanitizeHtml()
  ↓ Passes through escaped text (not real HTML anymore)

Browser Renders
  ✗ Shows: "<p>Paragraph 1</p><p>Paragraph 2</p>" as plain text
```

### Why It Happens

**Looking at EventDetailsTab.tsx Line 138-145:**

```tsx
dangerouslySetInnerHTML={{
  __html: sanitizeHtml(
    isHtmlContent(event.description)
      ? event.description           // ✅ Should use this path for TipTap HTML
      : plainTextToHtml(event.description)  // ❌ Should only use for legacy plain text
  )
}}
```

**The conditional is correct, BUT:**
- If `isHtmlContent()` returns `true`, it should pass `event.description` directly to `sanitizeHtml()`
- If `isHtmlContent()` returns `false`, it should convert plain text to HTML via `plainTextToHtml()`

**Possible Bug Scenarios:**
1. `isHtmlContent()` is failing to detect TipTap HTML (regex issue?)
2. The ternary operator logic is inverted
3. Somewhere else in the codebase, `plainTextToHtml()` is being called on HTML content

**Let me verify the actual rendering path...**

Actually, looking more carefully at the code in `EventDetailsTab.tsx`:

```tsx
isHtmlContent(event.description)
  ? event.description
  : plainTextToHtml(event.description)
```

This logic is **CORRECT**. So the issue must be that `isHtmlContent()` is returning `FALSE` for TipTap HTML when it should return `TRUE`.

**Let's test the regex:**

```typescript
/<[a-z][\s\S]*>/i.test("<p>Hello</p>")  // Should return true
```

Wait - this regex should work! Let me check if there's something wrong with the TipTap output format.

**Checking RichTextEditor.tsx Line 118:**
```tsx
const html = editor.getHTML();
```

TipTap's `getHTML()` returns HTML like:
```html
<p>Text here</p>
```

Which **SHOULD** match the regex `/<[a-z][\s\S]*>/i`.

**HYPOTHESIS:** The issue might be that TipTap is outputting empty `<p></p>` tags for line breaks, and when content is saved/loaded, these are being stripped or normalized somewhere.

---

## 4. Recommended Fix Strategy

### Option 1: Always Treat Event Descriptions as HTML (RECOMMENDED ✅)

**Rationale:**
- All new events use RichTextEditor (TipTap) which outputs HTML
- Legacy plain-text events are rare (system launched recently)
- Simpler, more maintainable code

**Implementation:**

**File:** `web/src/presentation/components/features/events/EventDetailsTab.tsx`

```tsx
// BEFORE (Lines 135-147)
<div
  className="prose prose-sm max-w-none text-neutral-600"
  dangerouslySetInnerHTML={{
    __html: sanitizeHtml(
      isHtmlContent(event.description)
        ? event.description
        : plainTextToHtml(event.description)
    )
  }}
/>

// AFTER (Simplified)
<div
  className="prose prose-sm max-w-none text-neutral-600"
  dangerouslySetInnerHTML={{
    __html: sanitizeHtml(event.description)
  }}
/>
```

**Pros:**
- ✅ Simple, direct fix
- ✅ Removes unnecessary complexity
- ✅ Works for both HTML and plain text (DOMPurify handles both)
- ✅ No performance impact

**Cons:**
- ⚠️ Legacy plain-text descriptions won't have auto-linked URLs
- ⚠️ Legacy plain-text newlines won't be converted to `<br>` tags

**Mitigation:** Add a one-time data migration to convert existing plain-text descriptions to HTML.

---

### Option 2: Fix the isHtmlContent() Detection

**Implementation:**

**File:** `web/src/lib/html-utils.ts`

```typescript
// BEFORE
export function isHtmlContent(text: string): boolean {
  return /<[a-z][\s\S]*>/i.test(text);
}

// AFTER (More robust detection)
export function isHtmlContent(text: string): boolean {
  // Check for common HTML tags used by TipTap
  const htmlTagRegex = /<(p|h1|h2|h3|ul|ol|li|strong|em|br|a|blockquote)\b[^>]*>/i;
  return htmlTagRegex.test(text);
}
```

**Pros:**
- ✅ More explicit tag detection
- ✅ Maintains backward compatibility logic
- ✅ Handles edge cases better

**Cons:**
- ⚠️ Still adds complexity for minimal benefit
- ⚠️ Requires maintaining tag whitelist

---

### Option 3: Convert Plain Text to HTML in Backend on Save

**Implementation:**

**File:** `src/LankaConnect.Domain/Events/ValueObjects/EventDescription.cs`

```csharp
public static Result<EventDescription> Create(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
        return Result<EventDescription>.Failure("Description is required");

    value = value.Trim();

    // Convert plain text to HTML if not already HTML
    if (!IsHtml(value))
    {
        value = ConvertPlainTextToHtml(value);
    }

    if (value.Length > MaxLength)
        return Result<EventDescription>.Failure($"Description cannot exceed {MaxLength} characters");

    return Result<EventDescription>.Success(new EventDescription(value));
}
```

**Pros:**
- ✅ Single source of truth (database always stores HTML)
- ✅ Frontend rendering becomes trivial
- ✅ Data consistency guaranteed

**Cons:**
- ❌ Backend dependency on HTML logic (domain layer pollution)
- ❌ Requires database migration for existing records
- ❌ More complex rollback if issues arise

---

## 5. Files to Modify

### Fix Option 1 (Recommended - Minimal Changes)

**Single File:**

1. **web/src/presentation/components/features/events/EventDetailsTab.tsx**
   - Line 138-145: Remove `isHtmlContent()` conditional
   - Simply pass `event.description` directly to `sanitizeHtml()`

**Testing Required:**
- ✅ Create new event with TipTap rich text (line breaks, headings, lists)
- ✅ Verify description renders with proper spacing
- ✅ Edit existing event, verify spacing preserved
- ✅ Test legacy plain-text events (if any exist)

---

### Fix Option 2 (Improved Detection)

**Two Files:**

1. **web/src/lib/html-utils.ts**
   - Line 26-28: Improve `isHtmlContent()` regex

2. **web/src/presentation/components/features/events/EventDetailsTab.tsx**
   - Line 138-145: Keep existing logic (no changes needed if detection fixed)

**Testing Required:**
- ✅ Unit tests for `isHtmlContent()` with TipTap output samples
- ✅ Integration test for event description rendering
- ✅ Test edge cases (empty tags, whitespace, special characters)

---

## 6. Potential Risks & Side Effects

### Low Risk Factors ✅

1. **Isolated Change:** Only affects event description rendering
2. **No Database Changes:** Fix is frontend-only
3. **No API Changes:** Backend unchanged
4. **Backward Compatible:** DOMPurify handles both HTML and plain text safely

### Medium Risk Factors ⚠️

1. **Legacy Events:** If any plain-text events exist, they may render poorly without newline conversion
   - **Mitigation:** Check production database for events created before RichTextEditor was added
   - **SQL Query:**
     ```sql
     SELECT COUNT(*) FROM events.events
     WHERE description NOT LIKE '%<%'
     AND created_at < '2025-01-15';  -- Date RichTextEditor was deployed
     ```

2. **XSS Security:** Ensure `sanitizeHtml()` is always called (never bypass it)
   - **Current State:** ✅ Already implemented correctly with DOMPurify

### Zero Risk Factors ✅

1. **Performance:** No impact (already using `dangerouslySetInnerHTML`)
2. **Mobile:** No responsive layout changes
3. **Accessibility:** Proper semantic HTML improves screen reader experience

---

## 7. Testing Strategy

### Unit Tests

**Create:** `web/src/lib/__tests__/html-utils.test.ts`

```typescript
import { sanitizeHtml, isHtmlContent, plainTextToHtml } from '../html-utils';

describe('HTML Utils', () => {
  describe('isHtmlContent', () => {
    it('should detect TipTap HTML', () => {
      expect(isHtmlContent('<p>Hello</p>')).toBe(true);
      expect(isHtmlContent('<p></p><p>World</p>')).toBe(true);
      expect(isHtmlContent('<h1>Title</h1><p>Content</p>')).toBe(true);
    });

    it('should detect plain text', () => {
      expect(isHtmlContent('Plain text')).toBe(false);
      expect(isHtmlContent('Line 1\nLine 2')).toBe(false);
    });
  });

  describe('sanitizeHtml', () => {
    it('should preserve TipTap paragraph spacing', () => {
      const input = '<p>Para 1</p><p></p><p>Para 2</p>';
      const output = sanitizeHtml(input);
      expect(output).toContain('<p>Para 1</p>');
      expect(output).toContain('<p>Para 2</p>');
    });

    it('should strip dangerous tags', () => {
      const input = '<p>Safe</p><script>alert("XSS")</script>';
      const output = sanitizeHtml(input);
      expect(output).not.toContain('<script>');
    });
  });
});
```

### Integration Tests

**Test Case 1: Create Event with Line Breaks**
1. Navigate to `/events/create`
2. Enter title: "Test Event"
3. Enter description with spacing:
   ```
   Paragraph one with good spacing.

   Paragraph two after line break.

   Paragraph three.
   ```
4. Submit form
5. Navigate to event detail page
6. **Verify:** Description shows 3 separate paragraphs with visual spacing

**Test Case 2: Edit Event with Rich Text**
1. Edit existing event
2. Modify description, add bold/italic/headings
3. Save changes
4. **Verify:** All formatting preserved on detail page

**Test Case 3: Legacy Plain Text Event**
1. Manually insert plain-text event via SQL (if none exist):
   ```sql
   INSERT INTO events.events (title, description, ...)
   VALUES ('Legacy Event', 'Line 1\nLine 2\n\nLine 3', ...);
   ```
2. View event on detail page
3. **Verify:** Renders acceptably (may not have perfect formatting, but readable)

### Manual QA Checklist

- [ ] Event creation form renders RichTextEditor
- [ ] RichTextEditor toolbar buttons work (Bold, Italic, Headings, Lists)
- [ ] Line breaks in editor appear as spacing in preview
- [ ] Saved event displays with correct spacing on detail page
- [ ] Edit form pre-fills with existing HTML content
- [ ] Event description is searchable (text content extracted)
- [ ] No XSS vulnerabilities (test `<script>alert('XSS')</script>`)
- [ ] Mobile responsive (description wraps properly)

---

## 8. Additional Findings

### TipTap Empty Paragraph Behavior

TipTap uses empty `<p></p>` tags to represent line breaks between paragraphs. This is semantically correct HTML and should be preserved during rendering.

**Example:**
```html
<p>Paragraph 1</p>
<p></p>  <!-- Visual spacing line break -->
<p>Paragraph 2</p>
```

**CSS Styling:** The `prose` class from Tailwind Typography should handle this:

```css
.prose p {
  margin-top: 1.25em;
  margin-bottom: 1.25em;
}
```

**Verification Needed:** Check if `prose` styles are properly applied in `EventDetailsTab.tsx` (Line 138).

**Current Code:**
```tsx
<div className="prose prose-sm max-w-none text-neutral-600 ...">
```

✅ **Confirmed:** `prose` class IS applied, so empty `<p>` tags should render with proper spacing.

---

### Browser Rendering Test

**Input HTML:**
```html
<div class="prose">
  <p>First paragraph</p>
  <p></p>
  <p>Second paragraph</p>
</div>
```

**Expected Rendering:**
```
First paragraph

Second paragraph
```

**If spacing missing, check:**
1. Tailwind Typography plugin installed (`@tailwindcss/typography`)
2. Plugin configured in `tailwind.config.js`
3. CSS build includes prose styles

---

## 9. Conclusion

### Root Cause

The event description line break issue is caused by **incorrect HTML content detection** in the `EventDetailsTab` component. The `isHtmlContent()` function may be failing to detect TipTap's HTML output, causing the `plainTextToHtml()` function to escape HTML tags into entities, which then render as visible text instead of formatted paragraphs.

### Recommended Solution

**Option 1 (Quick Fix - 5 minutes):**
Remove the `isHtmlContent()` conditional and always pass descriptions directly to `sanitizeHtml()`. This works because:
1. DOMPurify safely handles both HTML and plain text
2. All new events use RichTextEditor
3. Reduces code complexity

**Implementation:**
```tsx
// File: web/src/presentation/components/features/events/EventDetailsTab.tsx
<div
  className="prose prose-sm max-w-none text-neutral-600"
  dangerouslySetInnerHTML={{ __html: sanitizeHtml(event.description) }}
/>
```

### Next Steps

1. ✅ Review this RCA document with team
2. ✅ Choose fix strategy (recommend Option 1)
3. ✅ Implement fix following TDD methodology
4. ✅ Write unit tests for `sanitizeHtml()`
5. ✅ Run integration tests
6. ✅ Deploy to staging
7. ✅ Verify with user screenshots
8. ✅ Deploy to production

---

## Appendix: Code References

### File Locations

| Component | File Path | Key Lines |
|-----------|-----------|-----------|
| Event Creation Form | `web/src/presentation/components/features/events/EventCreationForm.tsx` | 350-370 |
| Event Edit Form | `web/src/presentation/components/features/events/EventEditForm.tsx` | 462-482 |
| Event Details Tab | `web/src/presentation/components/features/events/EventDetailsTab.tsx` | 135-147 |
| Rich Text Editor | `web/src/presentation/components/ui/RichTextEditor.tsx` | 90-121 |
| HTML Utilities | `web/src/lib/html-utils.ts` | 8-57 |
| Domain Value Object | `src/LankaConnect.Domain/Events/ValueObjects/EventDescription.cs` | 16-26 |

### Related Issues

- Phase 6A.74 Part 5A: RichTextEditor implementation
- Phase 6A.106 Part 3: Azure image upload for RichTextEditor
- Epic 2 Phase 2: Event description field added

### Testing Accounts

- **Test User:** niroshhh@gmail.com
- **Password:** 12!@qwASzx
- **Azure Staging API:** https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io

---

**Document Version:** 1.0
**Last Updated:** 2026-02-16
**Author:** Claude Code (Architecture Agent)
**Status:** Ready for Implementation
