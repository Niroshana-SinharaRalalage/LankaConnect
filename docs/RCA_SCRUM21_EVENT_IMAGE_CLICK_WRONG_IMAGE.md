# Root Cause Analysis: SCRUM-21 - Event Image Click Shows Wrong Image

**Date**: 2026-01-27
**Author**: Architecture Agent
**Status**: Root Cause Identified
**Severity**: Medium (UX Bug)

---

## 1. Executive Summary

When a registered user views the details of one of their registered events and clicks on an event image thumbnail in the gallery, the lightbox modal displays a different (wrong) image than expected.

**Root Cause**: Array index mismatch between the sorted thumbnail grid and the unsorted `images` array used for lightbox display in `MediaGallery.tsx`.

**Layer Affected**: Frontend UI (React Component)

---

## 2. Bug Description

### User Flow
1. User navigates to event details page (`/events/[id]`)
2. User scrolls down to the "Photos" section in the Media Gallery
3. User clicks on a specific image thumbnail
4. Lightbox modal opens showing a DIFFERENT image than the one clicked

### Expected Behavior
Clicking on thumbnail #3 should display image #3 in the lightbox.

### Actual Behavior
Clicking on thumbnail #3 may display image #1, #2, or any other image depending on the `displayOrder` values.

---

## 3. Root Cause Analysis

### 3.1 Location of Bug

**File**: `c:\Work\LankaConnect\web\src\presentation\components\features\events\MediaGallery.tsx`

**Lines**: 79-98 (thumbnail grid) and 174-179 (lightbox image display)

### 3.2 Technical Explanation

The bug occurs due to an **array index mismatch** between:

1. **Thumbnail Grid (sorted)**: Images are rendered sorted by `displayOrder`:
   ```tsx
   // Lines 80-98: Thumbnails are SORTED by displayOrder
   {[...images]
     .sort((a, b) => a.displayOrder - b.displayOrder)
     .map((image, index) => (
       <button
         key={image.id}
         onClick={() => openImageLightbox(index)}  // <-- INDEX from SORTED array
         ...
       >
   ```

2. **Lightbox Display (unsorted)**: The lightbox accesses the ORIGINAL unsorted `images` array:
   ```tsx
   // Lines 174-179: Accesses UNSORTED images array
   {mediaType === 'image' && images[currentIndex] && (
     <img
       src={images[currentIndex].imageUrl}  // <-- INDEX applied to UNSORTED array
       alt={`Event photo ${images[currentIndex].displayOrder}`}
       ...
     />
   )}
   ```

### 3.3 Example Scenario

Consider images with the following order from API:
```
images = [
  { id: 'img-1', displayOrder: 3, imageUrl: 'image3.jpg' },
  { id: 'img-2', displayOrder: 1, imageUrl: 'image1.jpg' },
  { id: 'img-3', displayOrder: 2, imageUrl: 'image2.jpg' },
]
```

**After sorting for thumbnails:**
```
Position 0 -> img-2 (displayOrder: 1) -> onClick sets index=0
Position 1 -> img-3 (displayOrder: 2) -> onClick sets index=1
Position 2 -> img-1 (displayOrder: 3) -> onClick sets index=2
```

**When lightbox opens with index=0 (clicked first thumbnail):**
```
images[0] = img-1 (displayOrder: 3, image3.jpg)  // WRONG!
```

**Expected:** Should show `image1.jpg` (displayOrder: 1)
**Actual:** Shows `image3.jpg` (displayOrder: 3)

---

## 4. Affected Components

| Component | File Path | Impact |
|-----------|-----------|--------|
| MediaGallery | `web/src/presentation/components/features/events/MediaGallery.tsx` | Primary bug location |
| Event Detail Page | `web/src/app/events/[id]/page.tsx` | Consumes MediaGallery |

### Related Code (Not Affected)

| Component | File Path | Status |
|-----------|-----------|--------|
| ImageUploader | `web/src/presentation/components/features/events/ImageUploader.tsx` | NOT affected - uses different pattern with `localImages` state |
| Backend EventImageDto | `src/LankaConnect.Application/Events/Common/EventDto.cs` | NOT affected - returns correct data |
| API Types | `web/src/infrastructure/api/types/events.types.ts` | NOT affected - types are correct |

---

## 5. Proposed Fix

### Option A: Store Sorted Array in State (Recommended)

Create a memoized sorted array and use it consistently for both display and lightbox:

```tsx
// Add at the top of the component function
const sortedImages = useMemo(
  () => [...images].sort((a, b) => a.displayOrder - b.displayOrder),
  [images]
);

// Update totalImages to use sortedImages
const totalImages = sortedImages.length;

// Update thumbnail rendering to use sortedImages
{sortedImages.map((image, index) => (
  <button
    key={image.id}
    onClick={() => openImageLightbox(index)}
    ...
  >
    <img src={image.imageUrl} ... />
  </button>
))}

// Update lightbox rendering to use sortedImages
{mediaType === 'image' && sortedImages[currentIndex] && (
  <img
    src={sortedImages[currentIndex].imageUrl}
    alt={`Event photo ${sortedImages[currentIndex].displayOrder}`}
    ...
  />
)}
```

### Option B: Pass Image ID Instead of Index

Store the clicked image ID and find it in the array:

```tsx
const [currentImageId, setCurrentImageId] = useState<string | null>(null);

const openImageLightbox = (imageId: string) => {
  setCurrentImageId(imageId);
  setMediaType('image');
  setLightboxOpen(true);
};

const currentImage = images.find(img => img.id === currentImageId);

// In lightbox:
{mediaType === 'image' && currentImage && (
  <img src={currentImage.imageUrl} ... />
)}
```

### Recommended: Option A

Option A is recommended because:
1. Maintains existing index-based navigation pattern
2. Works seamlessly with existing prev/next navigation
3. Minimal code changes required
4. Better performance (no find() on each render)

---

## 6. Risk Assessment

### Low Risk
- Fix is isolated to `MediaGallery.tsx` component
- No backend changes required
- No database changes required
- No API contract changes

### Testing Requirements
1. **Unit Test**: Verify sorted array is used consistently
2. **Manual Test**: Click each thumbnail and verify correct image displays
3. **Manual Test**: Navigate prev/next in lightbox and verify order
4. **Edge Cases**: Events with single image, events with no images

### Regression Risk
- **ImageUploader**: May need similar fix review (uses different pattern, appears OK)
- **Video Gallery**: Same pattern exists, should verify (lines 110-131 and 182-192)

---

## 7. Implementation Checklist

- [ ] Add `useMemo` import if not present
- [ ] Create `sortedImages` memoized array
- [ ] Create `sortedVideos` memoized array (for consistency)
- [ ] Update thumbnail grid to use `sortedImages` without inline sort
- [ ] Update thumbnail grid to use `sortedVideos` without inline sort
- [ ] Update lightbox image display to use `sortedImages[currentIndex]`
- [ ] Update lightbox video display to use `sortedVideos[currentIndex]`
- [ ] Update `goToPrevious` and `goToNext` to use sorted array lengths
- [ ] Write unit tests for MediaGallery component
- [ ] Manual testing on staging environment
- [ ] Update PROGRESS_TRACKER.md

---

## 8. Code Snippet for Fix

```tsx
'use client';

import { useState, useMemo } from 'react';  // ADD useMemo
import { X, Play, ChevronLeft, ChevronRight } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { Dialog, DialogContent } from '@/presentation/components/ui/Dialog';
import { EventImageDto, EventVideoDto } from '@/infrastructure/api/types/events.types';

interface MediaGalleryProps {
  images?: readonly EventImageDto[];
  videos?: readonly EventVideoDto[];
  className?: string;
}

export function MediaGallery({ images = [], videos = [], className = '' }: MediaGalleryProps) {
  const [lightboxOpen, setLightboxOpen] = useState(false);
  const [currentIndex, setCurrentIndex] = useState(0);
  const [mediaType, setMediaType] = useState<'image' | 'video'>('image');

  // FIX: Sort images once and use consistently
  const sortedImages = useMemo(
    () => [...images].sort((a, b) => a.displayOrder - b.displayOrder),
    [images]
  );

  // FIX: Sort videos once and use consistently
  const sortedVideos = useMemo(
    () => [...videos].sort((a, b) => a.displayOrder - b.displayOrder),
    [videos]
  );

  const totalImages = sortedImages.length;
  const totalVideos = sortedVideos.length;
  const totalMedia = totalImages + totalVideos;

  // ... rest of component using sortedImages and sortedVideos
}
```

---

## 9. Verification Steps

After fix is applied:

1. **Create Test Event**: Create event with 3+ images having non-sequential displayOrder
2. **View Event**: Navigate to event details page
3. **Click Thumbnail**: Click on second thumbnail in gallery
4. **Verify Lightbox**: Confirm lightbox shows the exact image that was clicked
5. **Navigate**: Use prev/next buttons to verify navigation order matches thumbnail order
6. **Repeat for Videos**: If videos exist, verify same behavior

---

## 10. References

- **Bug Report**: SCRUM-21
- **Component Path**: `web/src/presentation/components/features/events/MediaGallery.tsx`
- **Related Tests**: `web/src/presentation/components/features/events/__tests__/` (to be created)

---

## 11. Conclusion

The bug is a straightforward array index mismatch caused by sorting the images for display but using the unsorted array for lightbox access. The fix is low-risk and isolated to a single component. The same pattern should be verified for video handling in the same component.

**Recommended Action**: Implement Option A (memoized sorted arrays) and add unit tests to prevent regression.
