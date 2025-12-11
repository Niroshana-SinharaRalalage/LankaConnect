# Newsletter Location Tracking Strategy

## Problem Statement

**Question**: How do we know which notifications to send to newsletter subscribers without knowing their location?

**Example**: No point sending an email about an event in Ohio to a user in Florida who registered for newsletters.

---

## Solution: Multi-Layered Location Detection

### Strategy 1: Collect Location During Newsletter Signup ✅ RECOMMENDED

**Enhance the newsletter signup form to capture location:**

#### Updated Newsletter Form (Footer Component)

```tsx
// C:\Work\LankaConnect\web\src\presentation\components\layout\Footer.tsx

interface NewsletterFormData {
  email: string;
  location?: {
    metroAreaId: string;
    metroName: string;
    state: string;
  };
  preferences?: {
    allLocations: boolean; // User wants all events regardless of location
    radius?: number; // Miles from their location (50, 100, 250)
  };
}

const handleNewsletterSubmit = async (e: React.FormEvent) => {
  e.preventDefault();

  // Get user's detected or selected location
  const { selectedMetroArea, userLocation } = useMetroArea();

  const payload = {
    email,
    location: selectedMetroArea ? {
      metroAreaId: selectedMetroArea.id,
      metroName: selectedMetroArea.name,
      state: selectedMetroArea.state,
    } : null,
    detectedLocation: userLocation ? {
      latitude: userLocation.latitude,
      longitude: userLocation.longitude,
      accuracy: userLocation.accuracy,
    } : null,
    preferences: {
      allLocations: receiveAllEvents, // Checkbox in form
      radius: selectedRadius, // Dropdown: 50, 100, 250 miles
    },
    timestamp: new Date().toISOString(),
  };

  await newsletterService.subscribe(payload);
};
```

#### Enhanced Newsletter Form UI

```tsx
<form onSubmit={handleNewsletterSubmit}>
  {/* Email Input */}
  <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} />

  {/* Location Selection */}
  <div className="mt-3">
    <label className="text-sm text-white/80 mb-2 block">
      Get notifications for events in:
    </label>

    <MetroAreaSelector
      value={selectedMetro}
      onChange={setSelectedMetro}
      compact={true}
    />

    <button
      type="button"
      onClick={detectUserLocation}
      className="text-xs text-white/70 hover:text-white mt-2"
    >
      📍 Use my current location
    </button>
  </div>

  {/* Notification Preferences */}
  <div className="mt-3 space-y-2">
    <label className="flex items-center text-sm text-white/80">
      <input
        type="checkbox"
        checked={receiveAllEvents}
        onChange={(e) => setReceiveAllEvents(e.target.checked)}
        className="mr-2"
      />
      Send me events from all locations (I'm interested everywhere)
    </label>

    {!receiveAllEvents && (
      <div>
        <label className="text-xs text-white/70 block mb-1">
          Also notify me about events within:
        </label>
        <select
          value={radius}
          onChange={(e) => setRadius(Number(e.target.value))}
          className="text-sm px-3 py-1 rounded bg-white/20 text-white"
        >
          <option value="50">50 miles</option>
          <option value="100">100 miles</option>
          <option value="250">250 miles (state-wide)</option>
        </select>
      </div>
    )}
  </div>

  {/* Subscribe Button */}
  <button type="submit">Subscribe</button>
</form>
```

---

### Strategy 2: IP Geolocation Fallback (Backend)

**If user doesn't provide location, detect it from IP address:**

#### Backend API Implementation

```typescript
// Backend: /api/newsletter/subscribe

import geoip from 'geoip-lite';

export async function POST(req: Request) {
  const { email, location, detectedLocation, preferences } = await req.json();

  let finalLocation = location;

  // If no location provided, detect from IP
  if (!finalLocation) {
    const ipAddress = req.headers.get('x-forwarded-for') ||
                      req.headers.get('x-real-ip') ||
                      req.connection.remoteAddress;

    const geo = geoip.lookup(ipAddress);

    if (geo) {
      // Find closest metro area to IP coordinates
      const closestMetro = findClosestMetroArea(geo.ll[0], geo.ll[1]);

      finalLocation = {
        metroAreaId: closestMetro.id,
        metroName: closestMetro.name,
        state: closestMetro.state,
        source: 'ip-geolocation',
        confidence: 'medium', // IP is less accurate than GPS
      };
    }
  }

  // Save to database
  await db.newsletterSubscribers.create({
    email,
    metro_area_id: finalLocation?.metroAreaId,
    state: finalLocation?.state,
    latitude: detectedLocation?.latitude,
    longitude: detectedLocation?.longitude,
    receive_all_locations: preferences?.allLocations || false,
    notification_radius_miles: preferences?.radius || 50,
    location_source: finalLocation?.source || 'user-selected',
    subscribed_at: new Date(),
  });

  return Response.json({ success: true });
}
```

#### Database Schema Update

```sql
CREATE TABLE newsletter_subscribers (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  email VARCHAR(255) UNIQUE NOT NULL,

  -- Location Information
  metro_area_id VARCHAR(50), -- e.g., 'cleveland-oh'
  state VARCHAR(2), -- e.g., 'OH'
  latitude DECIMAL(10, 8),
  longitude DECIMAL(11, 8),
  location_accuracy_meters INTEGER,
  location_source VARCHAR(20), -- 'user-selected', 'gps', 'ip-geolocation'

  -- Notification Preferences
  receive_all_locations BOOLEAN DEFAULT false,
  notification_radius_miles INTEGER DEFAULT 50,

  -- Standard fields
  status VARCHAR(20) DEFAULT 'active',
  subscribed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  verified BOOLEAN DEFAULT false,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Index for location-based queries
CREATE INDEX idx_newsletter_metro_area ON newsletter_subscribers(metro_area_id);
CREATE INDEX idx_newsletter_state ON newsletter_subscribers(state);
CREATE INDEX idx_newsletter_location ON newsletter_subscribers USING GIST (
  ll_to_earth(latitude, longitude)
);
```

---

### Strategy 3: Smart Email Filtering (When Sending Newsletters)

**When sending newsletter about an event, filter recipients intelligently:**

#### Email Sending Logic

```typescript
// Backend: /api/newsletter/send

interface Event {
  id: string;
  title: string;
  location: {
    metroAreaId: string;
    state: string;
    latitude: number;
    longitude: number;
  };
}

async function sendEventNewsletter(event: Event) {
  // Query 1: Users who want ALL events
  const allEventsUsers = await db.newsletterSubscribers.findMany({
    where: {
      receive_all_locations: true,
      status: 'active',
      verified: true,
    },
  });

  // Query 2: Users in the SAME metro area
  const localUsers = await db.newsletterSubscribers.findMany({
    where: {
      metro_area_id: event.location.metroAreaId,
      receive_all_locations: false,
      status: 'active',
      verified: true,
    },
  });

  // Query 3: Users in the SAME state (if state-level event)
  const stateUsers = await db.newsletterSubscribers.findMany({
    where: {
      state: event.location.state,
      metro_area_id: { not: event.location.metroAreaId }, // Exclude already included
      receive_all_locations: false,
      status: 'active',
      verified: true,
    },
  });

  // Query 4: Users WITHIN notification radius
  const nearbyUsers = await db.query(`
    SELECT * FROM newsletter_subscribers
    WHERE receive_all_locations = false
      AND status = 'active'
      AND verified = true
      AND earth_distance(
        ll_to_earth(latitude, longitude),
        ll_to_earth($1, $2)
      ) <= (notification_radius_miles * 1609.34) -- Convert miles to meters
  `, [event.location.latitude, event.location.longitude]);

  // Combine and deduplicate
  const recipients = new Set([
    ...allEventsUsers.map(u => u.email),
    ...localUsers.map(u => u.email),
    ...stateUsers.map(u => u.email),
    ...nearbyUsers.map(u => u.email),
  ]);

  // Send personalized emails
  for (const email of recipients) {
    const user = findUserByEmail(email);
    const distance = calculateDistance(
      user.latitude, user.longitude,
      event.location.latitude, event.location.longitude
    );

    await sendEmail({
      to: email,
      subject: `New Event in ${event.location.metroAreaId}`,
      body: renderEmailTemplate(event, distance, user),
    });
  }

  // Log newsletter send
  await db.newsletterSends.create({
    event_id: event.id,
    recipients_count: recipients.size,
    sent_at: new Date(),
  });
}
```

---

### Strategy 4: Preference Management (User Can Update)

**Allow users to update location preferences:**

#### Preference Update Page

```typescript
// Frontend: /newsletter/preferences?token=abc123

export default function NewsletterPreferencesPage() {
  const [preferences, setPreferences] = useState({
    metroAreaId: 'cleveland-oh',
    receiveAllLocations: false,
    notificationRadius: 50,
  });

  const handleSave = async () => {
    await fetch('/api/newsletter/preferences', {
      method: 'PUT',
      body: JSON.stringify({
        token: searchParams.get('token'),
        preferences,
      }),
    });
  };

  return (
    <div className="max-w-md mx-auto p-6">
      <h1 className="text-2xl font-bold mb-4">Newsletter Preferences</h1>

      <div className="space-y-4">
        <div>
          <label className="block text-sm font-medium mb-2">
            Your Location
          </label>
          <MetroAreaSelector
            value={preferences.metroAreaId}
            onChange={(id) => setPreferences({ ...preferences, metroAreaId: id })}
          />
        </div>

        <div>
          <label className="flex items-center">
            <input
              type="checkbox"
              checked={preferences.receiveAllLocations}
              onChange={(e) => setPreferences({
                ...preferences,
                receiveAllLocations: e.target.checked,
              })}
              className="mr-2"
            />
            <span className="text-sm">
              Send me events from all locations
            </span>
          </label>
        </div>

        {!preferences.receiveAllLocations && (
          <div>
            <label className="block text-sm font-medium mb-2">
              Notification Radius
            </label>
            <select
              value={preferences.notificationRadius}
              onChange={(e) => setPreferences({
                ...preferences,
                notificationRadius: Number(e.target.value),
              })}
              className="w-full px-3 py-2 border rounded"
            >
              <option value="25">25 miles</option>
              <option value="50">50 miles</option>
              <option value="100">100 miles</option>
              <option value="250">250 miles (state-wide)</option>
            </select>
          </div>
        )}

        <button
          onClick={handleSave}
          className="w-full bg-orange-500 text-white px-4 py-2 rounded"
        >
          Save Preferences
        </button>
      </div>
    </div>
  );
}
```

---

## Email Templates with Location Context

### Example Newsletter Email

```html
Subject: Sri Lankan New Year Event in Cleveland - 15 miles from you!

Hi there,

There's an exciting event happening near you:

📅 Sinhala & Tamil New Year Celebration 2025
📍 Cleveland, OH (15 miles from Akron)
🗓️ April 13, 2025

Join us for the biggest New Year celebration in Ohio! Traditional games,
authentic food, cultural performances, and family fun.

[View Event Details] [Add to Calendar] [RSVP]

---

You're receiving this because:
- You subscribed to LankaConnect newsletter
- You selected "Akron, OH" as your location
- This event is within your 50-mile notification radius

[Update Preferences] [Unsubscribe]
```

---

## Implementation Checklist

### Phase 1: Enhanced Newsletter Signup ✅
- [ ] Add MetroAreaSelector to newsletter form
- [ ] Add "Detect My Location" button
- [ ] Add "Receive all locations" checkbox
- [ ] Add notification radius dropdown
- [ ] Update backend API to accept location data
- [ ] Update database schema with location fields

### Phase 2: IP Geolocation Fallback ✅
- [ ] Install `geoip-lite` package on backend
- [ ] Implement IP-to-coordinates lookup
- [ ] Find closest metro area to coordinates
- [ ] Store location source ('user-selected' vs 'ip-geolocation')

### Phase 3: Smart Email Filtering ✅
- [ ] Write location-based subscriber queries
- [ ] Implement radius-based filtering using PostGIS
- [ ] Create email templates with distance context
- [ ] Add unsubscribe and preference links to emails

### Phase 4: Preference Management ✅
- [ ] Create `/newsletter/preferences` page
- [ ] Generate secure tokens for preference updates
- [ ] Allow users to update location without re-subscribing
- [ ] Send confirmation email when preferences change

---

## Analytics & Optimization

### Track Newsletter Effectiveness by Location

```sql
-- Which metro areas have highest open rates?
SELECT
  metro_area_id,
  COUNT(*) as subscribers,
  AVG(open_rate) as avg_open_rate,
  AVG(click_rate) as avg_click_rate
FROM newsletter_subscribers
JOIN newsletter_engagement ON newsletter_subscribers.id = newsletter_engagement.subscriber_id
GROUP BY metro_area_id
ORDER BY avg_open_rate DESC;

-- Which radius settings perform best?
SELECT
  notification_radius_miles,
  COUNT(*) as subscribers,
  AVG(engagement_score) as avg_engagement
FROM newsletter_subscribers
GROUP BY notification_radius_miles
ORDER BY avg_engagement DESC;
```

---

## Privacy & GDPR Compliance

1. **Consent**: Clearly state location will be used for relevant notifications
2. **Transparency**: Show exactly what data is collected (GPS vs IP)
3. **Control**: Easy preference updates and unsubscribe
4. **Data Minimization**: Only collect necessary location data
5. **Retention**: Delete location data when user unsubscribes

---

## Summary

**Multi-Layered Location Strategy:**

1. ✅ **Primary**: Ask user to select metro area during signup (most accurate)
2. ✅ **Secondary**: Detect location via GPS if user allows (very accurate)
3. ✅ **Fallback**: Use IP geolocation if no other data available (less accurate)
4. ✅ **Override**: Allow "all locations" option for users who want everything
5. ✅ **Customizable**: Users can set notification radius (25-250 miles)
6. ✅ **Updatable**: Preference management page for changing location

**Result**: Every newsletter subscriber has a location (either provided, detected, or defaulted), enabling smart, relevant email notifications that don't spam users with irrelevant distant events.

---

**Last Updated**: 2025-11-08
**Status**: Design Complete - Awaiting Implementation
**Priority**: High (P1) - Critical for newsletter effectiveness
