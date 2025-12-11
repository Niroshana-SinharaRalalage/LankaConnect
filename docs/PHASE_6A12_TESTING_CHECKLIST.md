# Phase 6A.12 Event Media Upload - Testing Checklist

**Date Created:** 2025-12-01
**Feature:** Event Image/Video Upload System
**Environment:** Azure Staging + Local UI

---

## Pre-Testing Verification

### 1. Deployment Status
- [ ] Check GitHub Actions: [Deploy to Azure Staging workflow](https://github.com/yourusername/lankaconnect/actions)
- [ ] Verify latest commit deployed: `49d16c5` (Session 22 frontend integration)
- [ ] Check Azure Container App revision is active:
  ```bash
  az containerapp revision list \
    --name lankaconnect-api-staging \
    --resource-group lankaconnect-staging \
    --query "[].{name:name, createdTime:properties.createdTime, active:properties.active}" \
    --output table
  ```

### 2. Local UI Setup
- [ ] Frontend running: `cd web && npm run dev`
- [ ] Pointing to staging API: Check `NEXT_PUBLIC_API_URL` in `.env.local`
- [ ] Proxy working: Verify requests to `http://localhost:3000/api/proxy/*` forward to staging

---

## Test Suite 1: Image Upload (ImageUploader Component)

### Test 1.1: Upload Single Image
**Location:** Event Manage Page (`/events/{id}/manage`)

1. [ ] Navigate to an event you own
2. [ ] Locate "Event Images" card in right column
3. [ ] Click "Choose files" or drag-and-drop an image
4. [ ] Verify file validation:
   - [ ] Accepts: .jpg, .jpeg, .png, .gif, .webp
   - [ ] Rejects: .pdf, .doc, .txt
   - [ ] Max size: 10MB
5. [ ] Verify upload progress indicator shows
6. [ ] Verify image appears in gallery with display order badge
7. [ ] Verify page auto-refreshes after upload
8. [ ] Verify toast/success message appears

**Expected Result:** Image uploads successfully, appears in gallery, display order = (last order + 1)

---

### Test 1.2: Upload Multiple Images (Batch)
1. [ ] Select 3 images at once
2. [ ] Verify all 3 images upload with progress indicators
3. [ ] Verify sequential display orders (e.g., 2, 3, 4)
4. [ ] Verify all images appear in gallery sorted by display order

**Expected Result:** All images upload successfully with correct ordering

---

### Test 1.3: Maximum Images Limit
1. [ ] Upload images until reaching 10 total
2. [ ] Verify upload button becomes disabled
3. [ ] Verify error message: "Maximum 10 images allowed"
4. [ ] Try uploading another image
5. [ ] Verify upload is blocked

**Expected Result:** System enforces 10 image maximum

---

### Test 1.4: Delete Image
1. [ ] Click delete button (X) on an image
2. [ ] Verify confirmation dialog appears (if implemented)
3. [ ] Confirm deletion
4. [ ] Verify image removed from gallery
5. [ ] Verify remaining images resequence correctly (no gaps in display order)
6. [ ] Verify Azure Blob Storage cleanup:
   ```bash
   # Check Azure Container Apps logs
   az containerapp logs show \
     --name lankaconnect-api-staging \
     --resource-group lankaconnect-staging \
     --tail 50 \
     --follow false

   # Look for: "Successfully deleted blob from storage"
   ```

**Expected Result:** Image deleted from UI and database, blob cleanup logs appear

---

### Test 1.5: Replace Image
1. [ ] Click replace button on an image (if implemented)
2. [ ] Select new image
3. [ ] Verify old image replaced (same ID, same display order)
4. [ ] Verify old blob cleanup logs

**Expected Result:** Image replaced, old blob deleted

---

## Test Suite 2: Media Gallery (Event Detail Page)

### Test 2.1: View Image Gallery
**Location:** Event Detail Page (`/events/{id}`)

1. [ ] Navigate to event with images
2. [ ] Verify "Photos (N)" section appears after event details
3. [ ] Verify responsive grid: 2 cols (mobile), 3 cols (tablet), 4 cols (desktop)
4. [ ] Verify display order badges visible on thumbnails
5. [ ] Verify hover effect (opacity change)

**Expected Result:** Images display in responsive grid with proper styling

---

### Test 2.2: Lightbox Modal - Single Image
1. [ ] Click on any image thumbnail
2. [ ] Verify lightbox opens with full-screen overlay
3. [ ] Verify image displays at max size (maintain aspect ratio)
4. [ ] Verify close button (X) in top-right
5. [ ] Verify image counter: "1 / N" at bottom
6. [ ] Click close button
7. [ ] Verify lightbox closes

**Expected Result:** Lightbox opens/closes smoothly, image displays properly

---

### Test 2.3: Lightbox Carousel Navigation
1. [ ] Open lightbox on first image
2. [ ] Click right arrow button
3. [ ] Verify advances to image #2
4. [ ] Verify counter updates: "2 / N"
5. [ ] Click left arrow button
6. [ ] Verify goes back to image #1
7. [ ] Test wraparound:
   - [ ] From last image, click right → goes to first image
   - [ ] From first image, click left → goes to last image

**Expected Result:** Carousel navigation works smoothly with wraparound

---

### Test 2.4: Keyboard Navigation
1. [ ] Open lightbox
2. [ ] Press `ESC` key
3. [ ] Verify lightbox closes
4. [ ] Open lightbox again
5. [ ] Press `→` (right arrow) key
6. [ ] Verify advances to next image (if implemented)
7. [ ] Press `←` (left arrow) key
8. [ ] Verify goes to previous image (if implemented)

**Expected Result:** Keyboard shortcuts work as expected

---

### Test 2.5: Click Outside to Close
1. [ ] Open lightbox
2. [ ] Click on the dark backdrop (outside the image)
3. [ ] Verify lightbox closes

**Expected Result:** Clicking backdrop closes lightbox

---

## Test Suite 3: Video Display (If Videos Exist)

### Test 3.1: View Video Thumbnails
**Location:** Event Detail Page (`/events/{id}`)

1. [ ] Navigate to event with videos
2. [ ] Verify "Videos (N)" section appears
3. [ ] Verify responsive grid: 1 col (mobile), 2 cols (tablet), 3 cols (desktop)
4. [ ] Verify video thumbnails display
5. [ ] Verify play button overlay (white circle with play icon)
6. [ ] Verify display order badges

**Expected Result:** Video thumbnails display with play button overlay

---

### Test 3.2: Lightbox Video Playback
1. [ ] Click on video thumbnail
2. [ ] Verify lightbox opens
3. [ ] Verify video player loads
4. [ ] Verify video controls available (play, pause, volume, fullscreen)
5. [ ] Click play button
6. [ ] Verify video plays
7. [ ] Verify thumbnail used as poster image

**Expected Result:** Video plays in lightbox with full controls

---

## Test Suite 4: Blob Cleanup (Backend Verification)

### Test 4.1: Image Deletion Cleanup
**Goal:** Verify blobs are deleted from Azure Blob Storage when images are removed

1. [ ] Note image blob name before deletion (check database or network tab)
2. [ ] Delete image from UI
3. [ ] Check application logs for cleanup:
   ```bash
   az containerapp logs show \
     --name lankaconnect-api-staging \
     --resource-group lankaconnect-staging \
     --tail 100 \
     --follow false | grep -i "ImageRemovedEventHandler"
   ```
4. [ ] Look for log entries:
   - [ ] "Processing ImageRemovedFromEventDomainEvent"
   - [ ] "Successfully deleted blob from storage"
5. [ ] Verify no error logs appear

**Expected Result:** Logs show successful blob deletion, no errors

---

### Test 4.2: Video Deletion Cleanup
1. [ ] Delete video from event (if endpoint exists)
2. [ ] Check logs for cleanup of **both** video and thumbnail blobs:
   ```bash
   az containerapp logs show \
     --name lankaconnect-api-staging \
     --resource-group lankaconnect-staging \
     --tail 100 \
     --follow false | grep -i "VideoRemovedEventHandler"
   ```
3. [ ] Verify logs show:
   - [ ] "Successfully deleted video blob from storage"
   - [ ] "Successfully deleted thumbnail blob from storage"

**Expected Result:** Both video and thumbnail blobs cleaned up

---

## Test Suite 5: Error Handling

### Test 5.1: Invalid File Type
1. [ ] Try uploading .txt file
2. [ ] Verify error message: "Invalid file type"
3. [ ] Verify upload blocked

---

### Test 5.2: File Too Large
1. [ ] Try uploading image > 10MB
2. [ ] Verify error message: "File too large (max 10MB)"
3. [ ] Verify upload blocked

---

### Test 5.3: Network Failure
1. [ ] Start upload
2. [ ] Disable network (or kill backend)
3. [ ] Verify error message appears
4. [ ] Verify UI doesn't break
5. [ ] Re-enable network
6. [ ] Verify retry works

---

### Test 5.4: Unauthorized Access
1. [ ] Log out
2. [ ] Try accessing `/events/{id}/manage` for someone else's event
3. [ ] Verify access denied or redirect to login

---

## Test Suite 6: UI/UX Best Practices

### Test 6.1: Responsive Design
- [ ] Test on mobile (375px width)
- [ ] Test on tablet (768px width)
- [ ] Test on desktop (1920px width)
- [ ] Verify grid adjusts correctly at each breakpoint
- [ ] Verify lightbox works on all screen sizes

---

### Test 6.2: Accessibility
- [ ] Verify alt text on images
- [ ] Verify ARIA labels on buttons
- [ ] Test keyboard navigation (Tab, Enter, ESC)
- [ ] Verify focus indicators visible
- [ ] Test with screen reader (optional)

---

### Test 6.3: Loading States
- [ ] Verify loading spinner during upload
- [ ] Verify skeleton loaders for images (if implemented)
- [ ] Verify disabled state on buttons during operations

---

### Test 6.4: Dark Mode
- [ ] Enable dark mode (if supported)
- [ ] Verify MediaGallery renders correctly
- [ ] Verify lightbox backdrop is dark
- [ ] Verify text colors have sufficient contrast

---

## Test Suite 7: Performance

### Test 7.1: Large Image Upload
1. [ ] Upload 9.9MB image (just under limit)
2. [ ] Verify upload completes successfully
3. [ ] Verify progress indicator updates smoothly
4. [ ] Verify no UI freezing

---

### Test 7.2: Multiple Rapid Uploads
1. [ ] Upload 5 images in quick succession
2. [ ] Verify all uploads complete
3. [ ] Verify no race conditions
4. [ ] Verify display orders are correct

---

### Test 7.3: Image Gallery Scrolling
1. [ ] Upload 10 images
2. [ ] Scroll through gallery
3. [ ] Verify smooth scrolling
4. [ ] Verify images load properly (no broken images)

---

## Issue Tracking

### Bugs Found
| # | Description | Severity | Status | Notes |
|---|-------------|----------|--------|-------|
| 1 |             |          |        |       |
| 2 |             |          |        |       |

### Improvements Needed
| # | Description | Priority | Assigned To |
|---|-------------|----------|-------------|
| 1 |             |          |             |
| 2 |             |          |             |

---

## Sign-Off

- [ ] All critical tests passing
- [ ] No P0/P1 bugs blocking deployment
- [ ] Documentation updated
- [ ] Ready for production deployment

**Tested By:** _______________
**Date:** _______________
**Notes:** _______________

---

## Next Steps After Testing

1. If tests pass: Create VideoUploader component (Option 2)
2. If tests fail: Document bugs in PROGRESS_TRACKER.md and fix
3. Implement drag-and-drop reordering (Option 3)
4. Review event creation form integration (Option 4)

**Related Documentation:**
- [PHASE_6A12_EVENT_MEDIA_UPLOAD_SUMMARY.md](./PHASE_6A12_EVENT_MEDIA_UPLOAD_SUMMARY.md)
- [EventMediaUploadArchitecture.md](./architecture/EventMediaUploadArchitecture.md)
- [PROGRESS_TRACKER.md](./PROGRESS_TRACKER.md) - Session 22
