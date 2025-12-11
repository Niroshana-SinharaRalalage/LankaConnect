# Phase 5B: Quick Testing Reference

## 🎯 Where to Test

**Location**: Profile Page (`/profile`)
**Component**: Preferred Metro Areas Section
**Status**: ✅ Production Ready

---

## ⚡ 5-Minute Quick Test

### 1. **View Mode** (30 seconds)
```
✓ Go to /profile
✓ Find "Preferred Metro Areas" section
✓ See current metros OR "No metro areas selected"
✓ See Edit button
```

### 2. **Edit Mode** (30 seconds)
```
✓ Click Edit button
✓ See checkboxes appear
✓ See "Save" and "Cancel" buttons
✓ See counter: "X of 10 selected"
```

### 3. **Select Metros** (1 minute)
```
✓ Check 3 metros
✓ See counter increase: "3 of 10 selected"
✓ Uncheck one
✓ See counter decrease: "2 of 10 selected"
```

### 4. **Save** (1.5 minutes)
```
✓ Click Save
✓ Wait 2-3 seconds for API response
✓ See "Preferred metro areas saved successfully" message
✓ Exit edit mode
✓ See selected metros as badges
```

### 5. **Cancel** (1 minute)
```
✓ Click Edit again
✓ Uncheck all metros
✓ Click Cancel
✓ Verify it reverts to previously saved metros
```

**Total Time**: ~5 minutes ✅

---

## 🔍 Detailed Testing Scenarios

### **Scenario 1: Basic Flow**
| Step | Action | Expected |
|------|--------|----------|
| 1 | Navigate to /profile | Section visible |
| 2 | Click Edit | Checkboxes appear |
| 3 | Select Cleveland | Checked, counter = 1 |
| 4 | Select Denver | Checked, counter = 2 |
| 5 | Click Save | Success message, exit edit |
| 6 | Verify display | Shows "Cleveland" and "Denver" badges |

### **Scenario 2: Max Limit Test**
| Step | Action | Expected |
|------|--------|----------|
| 1 | Click Edit | Edit mode active |
| 2 | Select 10 metros | Counter shows "10 of 10" |
| 3 | Try to select 11th | NOT checked, error appears |
| 4 | Uncheck one | Counter = 9, error disappears |

### **Scenario 3: Privacy/Clear All**
| Step | Action | Expected |
|------|--------|----------|
| 1 | Click Edit | Edit mode active |
| 2 | Uncheck all metros | Counter shows "0 of 10" |
| 3 | Click Save | Success message |
| 4 | Verify | Shows "No metro areas selected" |

### **Scenario 4: Error Handling**
| Step | Action | Expected |
|------|--------|----------|
| 1 | Open DevTools (F12) | DevTools visible |
| 2 | Go to Network tab | Network tab selected |
| 3 | Disconnect (Offline) | Go offline mode |
| 4 | Edit and Save | API fails, error message shows |
| 5 | Go back online | Reconnect network |
| 6 | Save again | Success message |

---

## 🎮 Interactive Testing

### **Using Browser DevTools**

#### **Watch API Calls**
```
1. Open DevTools (F12)
2. Go to Network tab
3. Select 3 metros and click Save
4. Watch the PUT request appear
5. Click on it to see:
   - Request body: { "metroAreaIds": [...] }
   - Response: Updated profile
   - Status: 200 (success)
```

#### **Check Console for Errors**
```
1. Open DevTools (F12)
2. Go to Console tab
3. Perform actions (Edit, Select, Save)
4. Should see: NO RED ERROR MESSAGES
5. Good sign: Console is clean
```

#### **Test on Mobile**
```
1. Open DevTools (F12)
2. Press Ctrl+Shift+M (Responsive Design Mode)
3. Select iPhone 12 preset
4. Verify:
   - Single column layout
   - Text readable
   - Buttons work with touch
5. Try iPad preset
6. Verify: Two column layout
```

---

## ✅ Test Checklist (Quick Version)

### **Must Pass**
- [ ] Component renders
- [ ] Can click Edit
- [ ] Can select metros
- [ ] Counter updates
- [ ] Can save changes
- [ ] Success message shows
- [ ] Edit mode exits

### **Should Work**
- [ ] Can cancel without saving
- [ ] Max 10 limit enforced
- [ ] Error on network failure
- [ ] Works on mobile
- [ ] Tab navigation works

### **Nice to Verify**
- [ ] Proper grouping by state
- [ ] Success message auto-dismisses
- [ ] Branding colors correct
- [ ] Labels are associated with checkboxes

---

## 🚨 Common Issues & Fixes

| Problem | Solution |
|---------|----------|
| Nothing appears | Verify logged in, on /profile |
| Checkboxes don't work | Try click on label, check console |
| Save doesn't work | Check Network tab, verify API URL |
| Wrong data shows | Refresh page, clear cache |
| Mobile layout broken | Check DevTools responsive mode |
| Accessibility not working | Try Tab key, check ARIA labels |

---

## 📊 Performance Expectations

| Action | Time | Status |
|--------|------|--------|
| Click Edit | <100ms | Instant ✅ |
| Select metro | <50ms | Instant ✅ |
| Click Save | 2-3 sec | Normal ✅ |
| API response | 500-2000ms | Depends on network ✅ |
| Success message | Immediate | <500ms ✅ |

---

## 🎨 Visual Elements to Check

### **Colors**
- [ ] MapPin icon: Orange (#FF7900)
- [ ] Title: Maroon (#8B1538)
- [ ] Success message: Green (#10B981)
- [ ] Error message: Red (#EF4444)

### **Layout**
- [ ] Mobile: 1 column
- [ ] Tablet: 2 columns
- [ ] Desktop: 3 columns
- [ ] Responsive without scrolling (except content)

### **Typography**
- [ ] Title readable
- [ ] Metro names readable
- [ ] Counter text visible
- [ ] Error text stands out

---

## 🔗 Related Files

**Component Implementation**:
- `src/presentation/components/features/profile/PreferredMetroAreasSection.tsx`
- `tests/unit/presentation/components/features/profile/PreferredMetroAreasSection.test.tsx`

**API Integration**:
- `src/infrastructure/api/repositories/profile.repository.ts`
- `src/presentation/store/useProfileStore.ts`

**Constants & Models**:
- `src/domain/models/UserProfile.ts`
- `src/domain/constants/profile.constants.ts`
- `src/domain/constants/metroAreas.constants.ts`

**Full Testing Guide**:
- `docs/PHASE_5B_TESTING_GUIDE.md`

---

## 💡 Pro Tips

1. **Test in Incognito Mode** to avoid cache issues
2. **Use Different Browsers** to verify compatibility
3. **Check Mobile Landscape** mode in DevTools
4. **Monitor API Response Times** for baseline performance
5. **Test with slow network** (DevTools → Network → Throttling)
6. **Use Screen Reader** for accessibility testing
7. **Test Error Cases** by going offline then online

---

## 🎓 Learning Path

1. **Quick Test First** (5 min) - Basic functionality
2. **DevTools Inspection** (10 min) - API calls and console
3. **Responsive Testing** (5 min) - Mobile/tablet/desktop
4. **Error Scenarios** (5 min) - Network failures
5. **Accessibility** (5 min) - Keyboard and screen reader
6. **Performance** (5 min) - Timing and smoothness

**Total**: ~35 minutes for comprehensive testing

---

## ✨ Success Criteria

**All of these should work:**
- ✅ View current selections
- ✅ Edit and save new selections
- ✅ Max 10 limit enforced
- ✅ Cancel without saving
- ✅ Success message appears
- ✅ Error messages on failure
- ✅ Works on all devices
- ✅ No console errors
- ✅ API calls successful (200 status)
- ✅ Accessible with keyboard

**If all above pass**: Component is PRODUCTION READY ✅

---

**Questions?** See `PHASE_5B_TESTING_GUIDE.md` for detailed scenarios.
