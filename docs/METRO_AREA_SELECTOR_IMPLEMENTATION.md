# MetroAreaSelector Component Implementation

## Overview
Created a comprehensive metro area selector component with geolocation support, following clean architecture principles and the existing codebase patterns.

## Files Created

### 1. `web/src/presentation/utils/geolocation.ts`
**Purpose**: Browser geolocation API wrapper with proper error handling

**Key Functions**:
- `requestGeolocation()`: Returns UserLocation or null
- `requestGeolocationWithError()`: Returns detailed error information
- `isGeolocationAvailable()`: Check browser support
- `checkGeolocationPermission()`: Query permission status

**Error Handling**:
- Permission denied
- Position unavailable (GPS off, no signal)
- Request timeout
- Browser not supported
- Unknown errors

**Features**:
- Configurable timeout (default: 10 seconds)
- Configurable maximum age (default: 5 minutes)
- Returns UserLocation domain model
- Creates LocationError domain objects
- TypeScript strict types

### 2. `web/src/presentation/utils/distance.ts`
**Purpose**: Haversine formula implementation for distance calculations

**Key Functions**:
- `calculateDistance()`: Returns distance in miles
- `calculateDistanceKm()`: Returns distance in kilometers
- `formatDistance()`: Format for display (e.g., "5 mi")
- `formatDistancePrecise()`: Format with decimals
- `isWithinRadius()`: Check if within radius

**Features**:
- Accurate great-circle distance calculation
- Supports both miles and kilometers
- Utility functions for formatting
- Earth's radius constants (3959 mi, 6371 km)

### 3. `web/src/presentation/components/features/location/MetroAreaContext.tsx`
**Purpose**: React Context for global metro area state management

**State**:
- `selectedMetroArea`: Currently selected metro
- `userLocation`: User's detected location
- `isDetecting`: Loading state
- `detectionError`: Error message

**Actions**:
- `setMetroArea()`: Set selected metro (persists to localStorage)
- `detectLocation()`: Trigger geolocation detection
- `clearLocation()`: Clear user location

**Features**:
- Persists to localStorage:
  - `lankaconnect_selected_metro`
  - `lankaconnect_user_location`
- Loads state on mount
- Handles Date serialization/deserialization
- Error handling for localStorage failures
- TypeScript strict types

**Usage**:
```tsx
import { MetroAreaProvider, useMetroArea } from '@/presentation/components/features/location/MetroAreaContext';

// Wrap app with provider
<MetroAreaProvider>
  <App />
</MetroAreaProvider>

// Use in components
const { selectedMetroArea, userLocation, detectLocation } = useMetroArea();
```

### 4. `web/src/presentation/components/features/location/MetroAreaSelector.tsx`
**Purpose**: Metro area selector component with geolocation

**Features**:
- **Dropdown Select**: All metro areas from constants
- **Geolocation Button**: "Detect My Location" with loading state
- **Smart Sorting**: Sorts by distance when location available
- **Nearby Badge**: Shows "Nearby" for metros within 50 miles
- **Distance Display**: Shows distance in miles for each metro
- **Loading State**: Spinner during detection
- **Error State**: Displays geolocation errors
- **Success State**: Confirmation when location detected
- **Accessibility**:
  - Keyboard navigation
  - ARIA labels
  - Screen reader support
- **Styling**:
  - White background
  - Saffron (#FF7900) border and accents
  - Maroon (#8B1538) text
  - Focus states with ring
  - Hover effects

**Props**:
```typescript
interface MetroAreaSelectorProps {
  value: string | null;                    // Selected metro ID
  metros: readonly MetroArea[];            // Available metros
  onChange: (metroId: string | null) => void;
  userLocation?: UserLocation | null;      // User's location
  isDetecting?: boolean;                   // Loading state
  detectionError?: string | null;          // Error message
  onDetectLocation?: () => void;           // Detect callback
  placeholder?: string;                    // Placeholder text
  disabled?: boolean;                      // Disabled state
}
```

**Usage Example**:
```tsx
import { MetroAreaSelector } from '@/presentation/components/features/location/MetroAreaSelector';
import { useMetroArea } from '@/presentation/components/features/location/MetroAreaContext';
import { ALL_METRO_AREAS } from '@/domain/constants/metroAreas.constants';

function MyComponent() {
  const {
    selectedMetroArea,
    userLocation,
    isDetecting,
    detectionError,
    setMetroArea,
    detectLocation,
  } = useMetroArea();

  return (
    <MetroAreaSelector
      value={selectedMetroArea?.id || null}
      metros={ALL_METRO_AREAS}
      onChange={(id) => {
        const metro = ALL_METRO_AREAS.find(m => m.id === id);
        setMetroArea(metro || null);
      }}
      userLocation={userLocation}
      isDetecting={isDetecting}
      detectionError={detectionError}
      onDetectLocation={detectLocation}
    />
  );
}
```

## Architecture Compliance

### Clean Architecture
- **Domain Layer**: Uses MetroArea and UserLocation domain models
- **Presentation Layer**: All components in presentation layer
- **Separation of Concerns**: Utils separated from components
- **Dependency Inversion**: Components depend on domain abstractions

### Domain-Driven Design
- **Value Objects**: MetroArea, UserLocation, LocationError
- **Domain Models**: Immutable, self-validating
- **Constants**: Uses metroAreas.constants
- **Domain Functions**: calculateDistance, isWithinMetroArea

### Existing Patterns
- **'use client' directive**: All client components marked
- **TypeScript strict**: Full type safety
- **Error handling**: Comprehensive error states
- **Loading states**: Proper loading indicators
- **Accessibility**: ARIA labels, keyboard navigation
- **Styling**: Matches existing color scheme

## Edge Cases Handled

### Geolocation
1. Browser doesn't support geolocation
2. User denies permission
3. Position unavailable (GPS off)
4. Request timeout
5. Unknown errors
6. Permission API unavailable

### Distance Calculation
1. Null/undefined locations
2. Invalid coordinates (handled by domain validation)
3. Empty metro arrays
4. Distance calculations across date line

### State Management
1. localStorage failures (quota exceeded, disabled)
2. Invalid JSON in localStorage
3. Date serialization/deserialization
4. Missing or corrupted data

### UI States
1. Loading state during detection
2. Error state with user-friendly messages
3. Success state confirmation
4. Disabled state (no duplicate requests)
5. Empty selection (placeholder)

## Color Scheme
- **Saffron**: #FF7900 (Primary, buttons, borders, icons)
- **Maroon**: #8B1538 (Text, headings)
- **Gray**: #333, #666, #e0e0e0 (Secondary text, borders)
- **White**: #FFFFFF (Backgrounds)
- **Success**: #16A34A (Success messages)
- **Error**: #DC2626 (Error messages)

## Performance Optimizations
1. **useMemo**: Distance calculations only when location changes
2. **localStorage**: Prevents unnecessary API calls
3. **Debouncing**: Could be added to geolocation requests
4. **Lazy loading**: Icons loaded on demand

## Testing Considerations
1. Mock geolocation API
2. Test permission states
3. Test distance calculations
4. Test sorting logic
5. Test localStorage persistence
6. Test error scenarios
7. Test accessibility

## Future Enhancements
1. **Auto-detect on mount**: Optional prop to detect location immediately
2. **Radius filter**: Filter metros by distance radius
3. **Map view**: Show metros on a map
4. **Recent selections**: Remember last 3 selected metros
5. **Search/filter**: Search metros by name or city
6. **Multi-select**: Select multiple metros
7. **Debouncing**: Prevent rapid geolocation requests
8. **Caching**: Cache distances for performance

## Integration Steps

1. **Add Provider to Layout**:
```tsx
// app/layout.tsx
import { MetroAreaProvider } from '@/presentation/components/features/location/MetroAreaContext';

export default function RootLayout({ children }) {
  return (
    <html>
      <body>
        <MetroAreaProvider>
          {children}
        </MetroAreaProvider>
      </body>
    </html>
  );
}
```

2. **Use in Components**:
```tsx
import { MetroAreaSelector } from '@/presentation/components/features/location/MetroAreaSelector';
import { useMetroArea } from '@/presentation/components/features/location/MetroAreaContext';
import { ALL_METRO_AREAS } from '@/domain/constants/metroAreas.constants';

function EventListPage() {
  const { selectedMetroArea, setMetroArea, detectLocation, userLocation, isDetecting, detectionError } = useMetroArea();

  return (
    <MetroAreaSelector
      value={selectedMetroArea?.id || null}
      metros={ALL_METRO_AREAS}
      onChange={(id) => {
        const metro = ALL_METRO_AREAS.find(m => m.id === id);
        setMetroArea(metro || null);
      }}
      userLocation={userLocation}
      isDetecting={isDetecting}
      detectionError={detectionError}
      onDetectLocation={detectLocation}
    />
  );
}
```

## Dependencies
- **Domain Models**: MetroArea, UserLocation, LocationError
- **Domain Constants**: metroAreas.constants
- **Icons**: lucide-react (MapPin, Loader2, MapPinned)
- **React**: useState, useEffect, useContext, useMemo, useCallback

## File Paths
- `C:\Work\LankaConnect\web\src\presentation\utils\geolocation.ts`
- `C:\Work\LankaConnect\web\src\presentation\utils\distance.ts`
- `C:\Work\LankaConnect\web\src\presentation\components\features\location\MetroAreaContext.tsx`
- `C:\Work\LankaConnect\web\src\presentation\components\features\location\MetroAreaSelector.tsx`

---

**Status**: Implementation Complete
**Date**: 2025-11-08
**Component**: MetroAreaSelector with Geolocation Support
