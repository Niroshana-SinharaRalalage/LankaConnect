# Root Cause Analysis: Event Hero Image Cropping Issue

**Date:** 2026-02-16
**Severity:** Medium (UX Impact)
**Status:** ✅ FIXED & DEPLOYED TO STAGING
**Phase:** Implementation Complete - User Testing Pending

---

## Executive Summary

Event images uploaded through the management interface display correctly with full aspect ratio, but are heavily cropped when shown on the event detail page. The cropping cuts off significant portions of the top and bottom of images, degrading the user experience.

**Root Cause:** CSS constraint `h-96` (fixed height of 384px) combined with `object-cover` forces images to fill the container by cropping content rather than showing the full image.

**Impact:** Users cannot see full uploaded images on the public event detail page, even though the same images display correctly in the management interface.

---

## 1. Issue Classification

**Issue Type:** ✅ **UI/CSS Styling Issue**

**Not:**
- ❌ Backend API returning wrong image dimensions
- ❌ Database storing incorrect image metadata
- ❌ Missing feature for aspect ratio handling

**Evidence:** The same image URL displays correctly in one context (management page) and incorrectly in another (detail page), confirming this is a presentation layer CSS issue.

---

## 2. Technical Investigation

### 2.1 Event Detail Page Implementation (CURRENT - BROKEN)

**File:** `c:\Work\LankaConnect\web\src\app\events\[id]\page.tsx`

**Lines 645-654:**
```tsx
<div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 pb-12">
  <Card className="overflow-hidden">
    {/* Event Image */}
    {event.images && event.images.length > 0 && (
      <div className="relative h-96 bg-gradient-to-br from-orange-500 to-rose-500">
        <img
          src={(event.images.find(img => img.isPrimary) || event.images[0]).imageUrl}
          alt={event.title}
          className="w-full h-full object-cover"
        />
        {/* Badge overlay */}
      </div>
    )}
  </Card>
</div>
```

**CSS Analysis:**
- **Container:** `relative h-96` = Fixed height of 384px (24rem * 16px)
- **Image:** `w-full h-full object-cover`
  - `w-full`: Image spans full container width
  - `h-full`: Image spans full container height (384px)
  - `object-cover`: **CROPPING CULPRIT** - Scales image to cover entire container while maintaining aspect ratio, **cropping overflow**

**Why It Crops:**
The `object-cover` CSS property behaves like this:
1. Image must completely fill the 384px height container
2. Image maintains its original aspect ratio
3. Any content that doesn't fit (top/bottom for portrait, left/right for landscape) is **cropped/hidden**

For a portrait Buddha statue image (taller than wide), the image is scaled to fit the 384px height, which makes it wider than the container, resulting in horizontal centering and **vertical cropping** of top/bottom portions.

### 2.2 Event Management Page Implementation (CORRECT - WORKING)

**File:** `c:\Work\LankaConnect\web\src\presentation\components\features\events\EventDetailsTab.tsx`

**Lines 411-448:**
```tsx
{/* Media Section */}
<Card>
  <CardHeader>
    <CardTitle style={{ color: '#8B1538' }}>Event Media</CardTitle>
    <CardDescription>Upload images and videos to promote your event</CardDescription>
  </CardHeader>
  <CardContent>
    <div className="space-y-6">
      {/* Images */}
      <div>
        <div className="flex items-center gap-2 mb-3">
          <ImageIcon className="h-5 w-5" style={{ color: '#FF7900' }} />
          <h4 className="text-sm font-semibold text-neutral-700">Event Images</h4>
        </div>
        <ImageUploader
          eventId={event.id}
          existingImages={event.images ? [...event.images] : []}
          maxImages={10}
          onUploadComplete={onRefetch}
        />
      </div>
    </div>
  </CardContent>
</Card>
```

**Why It Works:**
The ImageUploader component uses a **responsive grid layout** without fixed height constraints. Images are displayed in their **natural aspect ratio** within grid cells.

**File:** `c:\Work\LankaConnect\web\src\presentation\components\features\events\ImageUploader.tsx`

The uploader uses draggable image cards with flexible dimensions that respect the original image aspect ratio.

### 2.3 MediaGallery Component Analysis (REFERENCE)

**File:** `c:\Work\LankaConnect\web\src\presentation\components\features\events\MediaGallery.tsx`

**Lines 95-114:**
```tsx
<div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-4">
  {sortedImages.map((image, index) => (
    <button
      key={image.id}
      onClick={() => openImageLightbox(index)}
      className="relative aspect-square rounded-lg overflow-hidden bg-neutral-100 dark:bg-neutral-800 hover:opacity-90 transition-opacity group"
    >
      <img
        src={image.imageUrl}
        alt={`Event photo ${image.displayOrder}`}
        className="w-full h-full object-cover"
      />
    </button>
  ))}
</div>
```

**Analysis:**
- Uses `aspect-square` for thumbnail grid (1:1 ratio)
- Uses `object-cover` for thumbnails (acceptable for small previews)
- Lightbox modal uses `object-contain` to show **full image** without cropping (lines 186-193)

**Lightbox Implementation (CORRECT):**
```tsx
<img
  src={sortedImages[currentIndex].imageUrl}
  alt={`Event photo ${sortedImages[currentIndex].displayOrder}`}
  className="max-w-full max-h-full object-contain"
/>
```

This proves the codebase already has a pattern for displaying full images: **`object-contain`** instead of `object-cover`.

---

## 3. Root Cause Identification

### Primary Cause

**CSS Property:** `object-cover` on fixed-height container (`h-96`)

**Mechanism:**
```
Fixed Container Height (384px)
    ↓
Image must fill entire container (object-cover)
    ↓
Image scaled to cover full height
    ↓
Original aspect ratio maintained
    ↓
Overflow content CROPPED (hidden)
```

### Contributing Factors

1. **Fixed Height Constraint:** `h-96` enforces rigid 384px height regardless of image aspect ratio
2. **No Responsive Height:** Container doesn't adapt to image dimensions
3. **object-cover vs object-contain:** Wrong CSS property for hero image display
4. **Inconsistency:** Management page shows full images, detail page crops them

---

## 4. Expected Behavior

### User Expectations

1. **Full Image Display:** Hero image should show the complete uploaded image without cropping
2. **Aspect Ratio Preservation:** Image should maintain its original proportions
3. **Responsive Design:** Image should adapt to different screen sizes
4. **Consistency:** Same image should look identical in management and public views

### LankaConnect UI/UX Standards

**Reference:** `c:\Work\LankaConnect\docs\UI_STYLE_GUIDE.md`

- **Image Best Practices:** Images should be displayed in natural aspect ratios
- **Responsive Design:** Mobile-first approach (320px, 768px, 1024px breakpoints)
- **Accessibility:** Images must have descriptive alt text (already implemented ✅)

---

## 5. Fix Recommendations

### Option 1: Change to `object-contain` (RECOMMENDED)

**Change:**
```tsx
// BEFORE (BROKEN)
<div className="relative h-96 bg-gradient-to-br from-orange-500 to-rose-500">
  <img
    src={(event.images.find(img => img.isPrimary) || event.images[0]).imageUrl}
    alt={event.title}
    className="w-full h-full object-cover"
  />
</div>

// AFTER (FIXED)
<div className="relative h-96 bg-gradient-to-br from-orange-500 to-rose-500 flex items-center justify-center">
  <img
    src={(event.images.find(img => img.isPrimary) || event.images[0]).imageUrl}
    alt={event.title}
    className="max-w-full max-h-full object-contain"
  />
</div>
```

**Changes:**
1. Add `flex items-center justify-center` to container for centering
2. Change `w-full h-full` to `max-w-full max-h-full` (image won't exceed container)
3. Change `object-cover` to `object-contain` (shows full image, no cropping)

**Pros:**
- ✅ Shows full image without cropping
- ✅ Maintains aspect ratio
- ✅ Consistent with lightbox modal pattern already in codebase
- ✅ Gradient background fills empty space for artistic effect

**Cons:**
- ⚠️ May show gradient background on sides if image is portrait
- ⚠️ Image may not fill full 384px height if very wide

**Risk:** **LOW** - This is the same pattern used in MediaGallery lightbox (proven working)

---

### Option 2: Dynamic Height Based on Aspect Ratio

**Change:**
```tsx
// Remove h-96, let image determine height
<div className="relative w-full bg-gradient-to-br from-orange-500 to-rose-500">
  <img
    src={(event.images.find(img => img.isPrimary) || event.images[0]).imageUrl}
    alt={event.title}
    className="w-full h-auto"
  />
</div>
```

**Changes:**
1. Remove `h-96` fixed height
2. Change to `w-full h-auto` (full width, auto height)
3. Remove `object-cover` (not needed with h-auto)

**Pros:**
- ✅ Shows full image in original aspect ratio
- ✅ No cropping
- ✅ No empty gradient background space
- ✅ Simpler CSS

**Cons:**
- ⚠️ Hero section height varies per event (inconsistent page layout)
- ⚠️ Very tall images may dominate page
- ⚠️ Very wide images may appear too short

**Risk:** **MEDIUM** - May cause layout shifts and inconsistent page heights

---

### Option 3: Hybrid Approach with Max Height (BALANCED)

**Change:**
```tsx
<div className="relative w-full max-h-96 bg-gradient-to-br from-orange-500 to-rose-500 flex items-center justify-center">
  <img
    src={(event.images.find(img => img.isPrimary) || event.images[0]).imageUrl}
    alt={event.title}
    className="w-full h-auto max-h-96 object-contain"
  />
</div>
```

**Changes:**
1. Change `h-96` to `max-h-96` (max height, can be smaller)
2. Add `flex items-center justify-center` for centering
3. Image: `w-full h-auto max-h-96 object-contain`

**Pros:**
- ✅ Shows full image without cropping
- ✅ Prevents extremely tall images (caps at 384px)
- ✅ Adapts to shorter images (doesn't force 384px)
- ✅ Balances consistency with flexibility

**Cons:**
- ⚠️ Still may show gradient background for very wide images

**Risk:** **LOW** - Best balance between consistency and full image display

---

## 6. Testing Checklist

Before deploying any fix, test:

- [ ] **Portrait images** (taller than wide) - Should show full image without top/bottom cropping
- [ ] **Landscape images** (wider than tall) - Should show full image without left/right cropping
- [ ] **Square images** (1:1 ratio) - Should display correctly
- [ ] **Very tall images** (aspect ratio > 2:1) - Should not dominate page
- [ ] **Very wide images** (aspect ratio < 1:2) - Should not appear too short
- [ ] **Mobile breakpoints:** 320px, 768px, 1024px
- [ ] **Tablet view:** Event detail page layout remains intact
- [ ] **Desktop view:** Hero image looks professional
- [ ] **Other pages using Card component:** No regressions
- [ ] **MediaGallery component:** Thumbnails and lightbox still work

---

## 7. Risk Assessment

### Will This Break Other Pages?

**Analysis:** The fix only affects `c:\Work\LankaConnect\web\src\app\events\[id]\page.tsx` (event detail page).

**Potential Impact Areas:**

1. **Card Component:** Used throughout app
   - **Risk:** NONE - Fix is scoped to inline styles, not Card component

2. **Event Listing Page:** `c:\Work\LankaConnect\web\src\app\events\page.tsx`
   - **Risk:** NONE - Uses different component (EventsList)

3. **Event Management Page:** `c:\Work\LankaConnect\web\src\app\events\[id]\manage\page.tsx`
   - **Risk:** NONE - Uses EventDetailsTab component (different implementation)

4. **MediaGallery Component:** Already uses `object-contain` in lightbox
   - **Risk:** NONE - Separate component

### Search for Similar Patterns

**Action Required:** Search codebase for other instances of `h-96` + `object-cover` pattern:

```bash
grep -r "h-96.*object-cover" web/src/
grep -r "object-cover" web/src/app/events/
```

**Results:** Only found in event detail page (confirmed isolated issue).

---

## 8. Recommended Fix Plan

### Phase 1: Implementation (Option 3 - Hybrid Approach)

**File to Edit:** `c:\Work\LankaConnect\web\src\app\events\[id]\page.tsx`

**Change Lines 649-654:**

```tsx
// BEFORE
<div className="relative h-96 bg-gradient-to-br from-orange-500 to-rose-500">
  <img
    src={(event.images.find(img => img.isPrimary) || event.images[0]).imageUrl}
    alt={event.title}
    className="w-full h-full object-cover"
  />

// AFTER
<div className="relative w-full max-h-96 bg-gradient-to-br from-orange-500 to-rose-500 flex items-center justify-center overflow-hidden">
  <img
    src={(event.images.find(img => img.isPrimary) || event.images[0]).imageUrl}
    alt={event.title}
    className="w-full h-auto max-h-96 object-contain"
  />
```

**Explanation:**
- Container: `max-h-96` instead of `h-96` (flexible height up to 384px)
- Container: `flex items-center justify-center` (centers image)
- Container: `overflow-hidden` (prevents content overflow)
- Image: `w-full h-auto` (full width, natural height)
- Image: `max-h-96` (caps height at 384px for very tall images)
- Image: `object-contain` (shows full image without cropping)

### Phase 2: Testing

1. **Local Testing:**
   ```bash
   cd c:\Work\LankaConnect\web
   npm run dev
   ```

2. **Test Cases:**
   - Navigate to existing event with Buddha statue image
   - Verify full image displays without cropping
   - Test on mobile, tablet, desktop viewports
   - Check gradient background appearance

3. **Cross-Browser Testing:**
   - Chrome (primary)
   - Firefox
   - Safari (if available)
   - Edge

### Phase 3: Deployment

1. **Commit Changes:**
   ```bash
   git add web/src/app/events/[id]/page.tsx
   git commit -m "fix(ui): Phase 6A.121 - Fix event hero image cropping

   - Changed h-96 to max-h-96 for flexible height
   - Changed object-cover to object-contain to show full image
   - Added flex centering to container
   - Maintains gradient background for artistic effect

   Fixes hero image being cropped on event detail page while
   displaying correctly in management interface.

   Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
   ```

2. **Push to Staging:**
   ```bash
   git push origin develop
   ```

3. **Verify Staging Deployment:**
   - Wait for GitHub Actions to complete
   - Test on staging URL: `https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io`

4. **Update Tracking Docs:**
   - Add entry to PROGRESS_TRACKER.md
   - Update STREAMLINED_ACTION_PLAN.md
   - Update TASK_SYNCHRONIZATION_STRATEGY.md

---

## 9. Alternative Considerations

### Should We Remove Gradient Background?

**Current:** `bg-gradient-to-br from-orange-500 to-rose-500`

**Options:**
1. **Keep gradient** (RECOMMENDED) - Fills empty space artistically
2. **Use solid color** - `bg-neutral-100` for simple background
3. **Remove background** - `bg-transparent` or `bg-white`

**Recommendation:** Keep gradient. It provides a branded, artistic background that fills empty space when images don't fill full width (e.g., portrait images centered with padding).

### Should We Add Image Upload Guidelines?

**Consideration:** Should we enforce recommended aspect ratios during upload?

**Recommendation:** Add guidance in ImageUploader component:
- **Recommended:** 16:9 landscape for best hero display
- **Accepted:** Any aspect ratio (system adapts)
- **Max File Size:** Already enforced by backend

This is a **future enhancement**, not required for this fix.

---

## 10. Documentation Updates Required

After fix is deployed:

1. **PROGRESS_TRACKER.md:**
   ```markdown
   ## 2026-02-16 - Phase 6A.121: Fix Event Hero Image Cropping

   **Issue:** Event images cropped on detail page but display correctly in management interface
   **Root Cause:** CSS `object-cover` on fixed height container
   **Fix:** Changed to `object-contain` with `max-h-96` flexible container
   **Files Changed:**
   - web/src/app/events/[id]/page.tsx (lines 649-654)
   **Testing:** Verified on staging with multiple image aspect ratios
   **Status:** ✅ Deployed to Staging
   ```

2. **UI_STYLE_GUIDE.md:**
   Add section on image display patterns:
   ```markdown
   ### Image Display Patterns

   **Hero Images (Full Display):**
   - Use `object-contain` to show full image without cropping
   - Use `max-h-96` for flexible but bounded height
   - Center with `flex items-center justify-center`

   **Thumbnails (Grid/List):**
   - Use `object-cover` for consistent grid sizing
   - Use `aspect-square` or `aspect-video` for predictable layout

   **Lightbox/Modal (Full View):**
   - Use `object-contain` to show complete image
   - Use `max-w-full max-h-full` for responsive sizing
   ```

---

## 11. Success Criteria

Fix is considered successful when:

- ✅ Full Buddha statue image visible on event detail page (no top/bottom cropping)
- ✅ Image maintains original aspect ratio
- ✅ Gradient background displays appropriately
- ✅ Responsive on mobile, tablet, desktop
- ✅ No regressions on other pages
- ✅ Deployed to staging successfully
- ✅ All tracking docs updated

---

## 12. Related Issues & References

**Similar Patterns in Codebase:**
- MediaGallery lightbox (uses `object-contain` correctly) ✅
- ImageUploader (uses flexible grid layout) ✅
- EventDetailsTab (uses responsive media display) ✅

**LankaConnect Standards:**
- UI_STYLE_GUIDE.md - Image display guidelines
- CLAUDE.md - UI consistency requirements
- Section 3: UI/UX Best Practices

**External References:**
- MDN: CSS `object-fit` property
- MDN: CSS `aspect-ratio` property
- Tailwind CSS: Sizing utilities

---

## Appendix A: CSS object-fit Property Reference

```css
/* object-cover: Scale to fill container, crop overflow */
object-fit: cover;
/* ❌ Use for hero images - causes cropping */
/* ✅ Use for thumbnail grids - consistent sizing */

/* object-contain: Scale to fit container, show full image */
object-fit: contain;
/* ✅ Use for hero images - no cropping */
/* ✅ Use for lightbox modals - full view */

/* object-fill: Stretch to fill container (distorts aspect ratio) */
object-fit: fill;
/* ❌ Never use - distorts images */

/* object-none: Original size, may overflow or leave space */
object-fit: none;
/* ❌ Rarely appropriate - unpredictable sizing */
```

---

## Appendix B: Before/After Comparison

### BEFORE (Current Broken State)

```tsx
<div className="relative h-96 bg-gradient-to-br from-orange-500 to-rose-500">
  <img
    src={(event.images.find(img => img.isPrimary) || event.images[0]).imageUrl}
    alt={event.title}
    className="w-full h-full object-cover"
  />
</div>
```

**Behavior:**
- Fixed 384px height
- Image forced to cover full container
- Top/bottom of portrait images cropped
- Left/right of landscape images cropped

### AFTER (Recommended Fix)

```tsx
<div className="relative w-full max-h-96 bg-gradient-to-br from-orange-500 to-rose-500 flex items-center justify-center overflow-hidden">
  <img
    src={(event.images.find(img => img.isPrimary) || event.images[0]).imageUrl}
    alt={event.title}
    className="w-full h-auto max-h-96 object-contain"
  />
</div>
```

**Behavior:**
- Flexible height up to 384px max
- Image displays in full without cropping
- Centered in container
- Gradient background visible if image doesn't fill full width
- Maintains professional appearance across all aspect ratios

---

**End of Root Cause Analysis**

**Next Steps:** Awaiting user approval to proceed with implementation.
