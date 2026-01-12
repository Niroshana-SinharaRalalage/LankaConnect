# Phase 6A.X Improvements - Organizer Contact Feature

## Issues Identified from User Testing

### Issue 1: Phone Number Not Auto-Populating
**Problem**: Phone number field doesn't auto-populate when "Publish my contact information" is checked.

**Root Cause**:
- Backend `LoginUserResponse` doesn't include `phoneNumber` field
- Frontend `UserDto` (auth.types.ts) doesn't have `phoneNumber` property
- Auto-population logic only handles `fullName` and `email`

**Solution**:
1. ✅ Backend: Add `PhoneNumber` to `LoginUserResponse` (src/LankaConnect.Application/Auth/Commands/LoginUser/LoginUserResponse.cs)
2. ✅ Frontend: Add `phoneNumber` to `UserDto` interface (web/src/infrastructure/api/types/auth.types.ts)
3. ✅ Frontend: Update auto-populate logic to include phone number in both EventCreationForm and EventEditForm

### Issue 2: Organizer Contact Not Showing in Event Details
**Problem**: User reports not seeing organizer contact section in event details page.

**Investigation**:
- ✅ API returns correct data: `publishOrganizerContact: true`, with name, phone, email
- ✅ TypeScript types include all fields
- ✅ Display component code is correct (EventDetailsTab.tsx lines 242-292)

**Possible Causes**:
1. **Cache Issue**: Browser may have cached old version without organizer contact fields
2. **Build Issue**: Frontend deployment may not have latest changes
3. **Data Issue**: Event being viewed may not have organizer contact published

**Solution**:
1. Verify latest frontend deployment has organizer contact display code
2. Hard refresh browser (Ctrl+Shift+R) to clear cache
3. Test with known event that has organizer contact (ID: 0458806b-8672-4ad5-a7cb-f5346f1b282a)
4. Add console.log to debug data flow

### Issue 3: Event Manage Page Layout - Verbose/Messy/Unclear
**Problem**: Event management page displays too much information in a disorganized way, making it hard to read.

**Current Issues**:
- All fields displayed in single column with no visual hierarchy
- No clear grouping of related fields
- Organizer contact mixed with other event details
- Pricing information not clearly separated
- Location details verbose
- No clear distinction between required and optional fields

**Solution - Implement Tabbed/Accordion Layout**:

#### Option A: Tabs (Recommended)
Organize into logical tabs:
1. **Basic Info** - Title, Description, Date/Time, Capacity, Category
2. **Location** - Address, City, State, Country, Map
3. **Pricing** - Ticket pricing, Adult/Child pricing, Group tiers
4. **Contact & Email** - Organizer contact, Email groups
5. **Media** - Photos, Videos
6. **Advanced** - Status, Registration stats, Created/Updated dates

#### Option B: Accordion Sections
Collapsible sections:
1. ✅ **Event Details** (expanded by default) - Basic info
2. 🔽 **Location & Venue** (collapsed) - Location details
3. 🔽 **Pricing & Registration** (collapsed) - All pricing options
4. 🔽 **Organizer Contact** (collapsed) - Contact publication settings
5. 🔽 **Email & Notifications** (collapsed) - Email groups
6. 🔽 **Media Gallery** (collapsed) - Photos/Videos
7. 🔽 **Event Statistics** (collapsed) - Registration counts, status

#### Option C: Card Grid (Most Modern)
Organize into cards with visual hierarchy:
```
┌─────────────────────┬─────────────────────┐
│  📅 Event Details   │  📍 Location       │
│  - Title           │  - Address         │
│  - Description     │  - City            │
│  - Dates           │  - Map preview     │
└─────────────────────┴─────────────────────┘
┌─────────────────────┬─────────────────────┐
│  💰 Pricing         │  👤 Organizer      │
│  - Ticket types    │  - Contact info    │
│  - Group tiers     │  - Publish toggle  │
└─────────────────────┴─────────────────────┘
┌─────────────────────┬─────────────────────┐
│  📧 Email Groups    │  📊 Statistics     │
│  - Selected groups │  - Registrations   │
│  - Preview         │  - Status          │
└─────────────────────┴─────────────────────┘
```

---

## Implementation Plan

### Phase 1: Fix Phone Number Auto-Population

#### Backend Changes
**File**: `src/LankaConnect.Application/Auth/Commands/LoginUser/LoginUserResponse.cs`
```csharp
public record LoginUserResponse(
    Guid UserId,
    string Email,
    string FullName,
    string? PhoneNumber,  // ADD THIS
    UserRole Role,
    string AccessToken,
    string RefreshToken,
    DateTime TokenExpiresAt,
    bool IsEmailVerified,
    UserRole? PendingUpgradeRole = null,
    DateTime? UpgradeRequestedAt = null,
    string? ProfilePhotoUrl = null);
```

**File**: `src/LankaConnect.Application/Auth/Commands/LoginUser/LoginUserHandler.cs`
Update response mapping:
```csharp
return Result.Ok(new LoginUserResponse(
    user.Id.Value,
    user.Email.Value,
    $"{user.FirstName} {user.LastName}",
    user.PhoneNumber,  // ADD THIS
    user.Role,
    accessToken,
    refreshToken,
    tokenExpiresAt,
    user.IsEmailVerified,
    // ... rest
));
```

#### Frontend Changes
**File**: `web/src/infrastructure/api/types/auth.types.ts`
```typescript
export interface UserDto {
  userId: string;
  email: string;
  fullName: string;
  phoneNumber?: string | null; // ADD THIS
  role: UserRole;
  // ... rest
}
```

**File**: `web/src/presentation/components/features/events/EventCreationForm.tsx`
Update useEffect:
```typescript
useEffect(() => {
  if (publishOrganizerContact && user) {
    const currentName = watch('organizerContactName');
    const currentEmail = watch('organizerContactEmail');
    const currentPhone = watch('organizerContactPhone'); // ADD THIS

    if (!currentName) {
      setValue('organizerContactName', user.fullName);
    }
    if (!currentEmail) {
      setValue('organizerContactEmail', user.email);
    }
    // ADD THIS
    if (!currentPhone && user.phoneNumber) {
      setValue('organizerContactPhone', user.phoneNumber);
    }
  }
}, [publishOrganizerContact, user, setValue, watch]);
```

**File**: `web/src/presentation/components/features/events/EventEditForm.tsx`
Same changes as EventCreationForm.

---

### Phase 2: Debug Organizer Contact Display

#### Verification Steps
1. Check latest deployment timestamp
2. Hard refresh browser (Ctrl+Shift+R)
3. Open browser DevTools → Network tab
4. Load event details page
5. Check API response includes organizer contact fields
6. Check React DevTools for component props

#### Add Debug Logging
**File**: `web/src/presentation/components/features/events/EventDetailsTab.tsx`
```typescript
// Add before conditional render (line 242)
console.log('🔍 [DEBUG] Event organizer contact:', {
  publishOrganizerContact: event.publishOrganizerContact,
  organizerContactName: event.organizerContactName,
  organizerContactEmail: event.organizerContactEmail,
  organizerContactPhone: event.organizerContactPhone,
  shouldDisplay: event.publishOrganizerContact && event.organizerContactName
});

{/* Phase 6A.X: Organizer Contact Details */}
{event.publishOrganizerContact && event.organizerContactName && (
  // ... existing code
)}
```

---

### Phase 3: Improve Event Manage Page Layout

#### Recommended: Card Grid Layout with Sections

**File**: `web/src/presentation/components/features/events/EventManagePage.tsx` (or similar)

**New Structure**:
```tsx
<div className="container mx-auto px-4 py-8">
  {/* Page Header */}
  <div className="mb-8">
    <h1 className="text-3xl font-bold">Manage Event</h1>
    <p className="text-gray-600 mt-2">Edit your event details and settings</p>
  </div>

  {/* Main Grid */}
  <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
    {/* Card 1: Event Details */}
    <Card>
      <CardHeader className="bg-gradient-to-r from-blue-50 to-blue-100">
        <div className="flex items-center gap-2">
          <Calendar className="h-5 w-5 text-blue-600" />
          <CardTitle>Event Details</CardTitle>
        </div>
      </CardHeader>
      <CardContent className="pt-6">
        {/* Title, Description, Dates, Capacity */}
      </CardContent>
    </Card>

    {/* Card 2: Location */}
    <Card>
      <CardHeader className="bg-gradient-to-r from-green-50 to-green-100">
        <div className="flex items-center gap-2">
          <MapPin className="h-5 w-5 text-green-600" />
          <CardTitle>Location & Venue</CardTitle>
        </div>
      </CardHeader>
      <CardContent className="pt-6">
        {/* Address, City, State, Country */}
        {/* Optional: Mini map preview */}
      </CardContent>
    </Card>

    {/* Card 3: Pricing */}
    <Card>
      <CardHeader className="bg-gradient-to-r from-orange-50 to-orange-100">
        <div className="flex items-center gap-2">
          <DollarSign className="h-5 w-5 text-orange-600" />
          <CardTitle>Pricing & Tickets</CardTitle>
        </div>
      </CardHeader>
      <CardContent className="pt-6">
        {/* Ticket prices, Adult/Child, Group tiers */}
      </CardContent>
    </Card>

    {/* Card 4: Organizer Contact */}
    <Card>
      <CardHeader className="bg-gradient-to-r from-purple-50 to-purple-100">
        <div className="flex items-center gap-2">
          <Users className="h-5 w-5 text-purple-600" />
          <CardTitle>Organizer Contact</CardTitle>
        </div>
      </CardHeader>
      <CardContent className="pt-6">
        <div className="space-y-4">
          {/* Publish toggle */}
          <div className="flex items-center space-x-3 p-3 bg-purple-50 rounded-lg">
            <input
              type="checkbox"
              id="publishContact"
              checked={publishOrganizerContact}
              onChange={(e) => setPublishOrganizerContact(e.target.checked)}
              className="h-4 w-4 rounded border-gray-300"
            />
            <label htmlFor="publishContact" className="text-sm font-medium">
              Make my contact information visible to attendees
            </label>
          </div>

          {/* Conditional fields */}
          {publishOrganizerContact && (
            <div className="space-y-3 pl-4 border-l-2 border-purple-300">
              <div>
                <label className="text-sm font-medium text-gray-700">Name</label>
                <Input value={contactName} onChange={(e) => setContactName(e.target.value)} />
              </div>
              <div>
                <label className="text-sm font-medium text-gray-700">Email</label>
                <Input type="email" value={contactEmail} onChange={(e) => setContactEmail(e.target.value)} />
              </div>
              <div>
                <label className="text-sm font-medium text-gray-700">Phone</label>
                <Input type="tel" value={contactPhone} onChange={(e) => setContactPhone(e.target.value)} />
              </div>
            </div>
          )}
        </div>
      </CardContent>
    </Card>

    {/* Card 5: Email Groups */}
    <Card>
      <CardHeader className="bg-gradient-to-r from-indigo-50 to-indigo-100">
        <div className="flex items-center gap-2">
          <Mail className="h-5 w-5 text-indigo-600" />
          <CardTitle>Email Groups</CardTitle>
        </div>
      </CardHeader>
      <CardContent className="pt-6">
        {/* Email group selection */}
      </CardContent>
    </Card>

    {/* Card 6: Statistics */}
    <Card>
      <CardHeader className="bg-gradient-to-r from-teal-50 to-teal-100">
        <div className="flex items-center gap-2">
          <BarChart className="h-5 w-5 text-teal-600" />
          <CardTitle>Event Statistics</CardTitle>
        </div>
      </CardHeader>
      <CardContent className="pt-6">
        <div className="grid grid-cols-2 gap-4">
          <div className="text-center p-4 bg-teal-50 rounded-lg">
            <div className="text-2xl font-bold text-teal-700">{registrations}</div>
            <div className="text-sm text-gray-600">Registrations</div>
          </div>
          <div className="text-center p-4 bg-teal-50 rounded-lg">
            <div className="text-2xl font-bold text-teal-700">{capacity}</div>
            <div className="text-sm text-gray-600">Capacity</div>
          </div>
        </div>
      </CardContent>
    </Card>
  </div>

  {/* Full-width Media Section */}
  <Card className="mt-6">
    <CardHeader className="bg-gradient-to-r from-pink-50 to-pink-100">
      <div className="flex items-center gap-2">
        <Image className="h-5 w-5 text-pink-600" />
        <CardTitle>Media Gallery</CardTitle>
      </div>
    </CardHeader>
    <CardContent className="pt-6">
      {/* Photos and videos grid */}
    </CardContent>
  </Card>

  {/* Action Buttons */}
  <div className="mt-8 flex justify-end gap-4">
    <Button variant="outline" onClick={() => router.push('/events')}>
      Cancel
    </Button>
    <Button onClick={handleSave} className="bg-blue-600 hover:bg-blue-700">
      Save Changes
    </Button>
  </div>
</div>
```

---

## Testing Checklist

### Phone Number Auto-Population
- [ ] Backend LoginUserResponse includes PhoneNumber
- [ ] Frontend UserDto includes phoneNumber
- [ ] Auto-populate logic updated in EventCreationForm
- [ ] Auto-populate logic updated in EventEditForm
- [ ] Build succeeds (backend: 0 errors, frontend: 0 TypeScript errors)
- [ ] Deploy backend to Azure staging
- [ ] Deploy frontend to Azure staging
- [ ] Test: Login → Create Event → Check "Publish contact" → Verify phone auto-fills
- [ ] Test: Login → Edit Event → Check "Publish contact" → Verify phone auto-fills

### Organizer Contact Display
- [ ] Latest frontend deployed to staging
- [ ] Hard refresh browser
- [ ] Navigate to event with published contact (ID: 0458806b-8672-4ad5-a7cb-f5346f1b282a)
- [ ] Verify "Event Organizer Contact" section displays
- [ ] Verify Name, Email, Phone all visible
- [ ] Verify Email and Phone are clickable links
- [ ] Test on different browsers (Chrome, Firefox, Edge)

### Event Manage Page Layout
- [ ] Identify current manage/edit page file
- [ ] Implement card grid layout
- [ ] Visual hierarchy with color-coded headers
- [ ] Conditional display for organizer contact section
- [ ] Responsive design (mobile, tablet, desktop)
- [ ] Test all form interactions still work
- [ ] Verify save functionality
- [ ] User feedback on readability improvement

---

## Implementation Priority

1. **HIGH**: Phone Number Auto-Population (blocking user workflow)
2. **HIGH**: Debug Organizer Contact Display (user reports not seeing it)
3. **MEDIUM**: Event Manage Page Layout (usability improvement)

---

## Next Steps

1. Implement Phase 1 (Phone Number) - Backend and Frontend
2. Deploy and verify in staging
3. Debug Phase 2 (Display issue) with user assistance
4. Plan Phase 3 (Layout) - Get user preference on layout option
5. Implement chosen layout
6. Full end-to-end testing
7. Update email templates
8. Update documentation
