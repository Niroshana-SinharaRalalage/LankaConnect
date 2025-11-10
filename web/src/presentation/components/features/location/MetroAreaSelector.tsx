'use client';

/**
 * MetroAreaSelector Component
 *
 * Dropdown selector for metro areas with geolocation support.
 * Features:
 * - Dropdown select for all metro areas
 * - "Detect My Location" button with geolocation
 * - Sorts metros by distance when location is available
 * - Shows "Nearby" badge for closest metros (within 50 miles)
 * - Loading state during detection
 * - Error state for geolocation failures
 * - Keyboard accessible
 * - ARIA labels for screen readers
 */

import React, { useMemo, useState } from 'react';
import { MapPin, Loader2, MapPinned } from 'lucide-react';
import type { MetroArea } from '@/domain/models/MetroArea';
import type { UserLocation } from '@/domain/models/Location';
import { calculateDistance } from '@/presentation/utils/distance';

/**
 * Metro area with distance information
 */
interface MetroWithDistance extends MetroArea {
  distance?: number;
  isNearby?: boolean;
}

/**
 * Component props
 */
interface MetroAreaSelectorProps {
  /** Currently selected metro area ID */
  value: string | null;
  /** Metro areas to display */
  metros: readonly MetroArea[];
  /** Callback when selection changes */
  onChange: (metroId: string | null) => void;
  /** User's current location (optional) */
  userLocation?: UserLocation | null;
  /** Whether location detection is in progress */
  isDetecting?: boolean;
  /** Error message from location detection */
  detectionError?: string | null;
  /** Callback to trigger location detection */
  onDetectLocation?: () => void;
  /** Placeholder text for the select */
  placeholder?: string;
  /** Whether the selector is disabled */
  disabled?: boolean;
}

/**
 * Distance threshold for "nearby" badge (in miles)
 */
const NEARBY_THRESHOLD_MILES = 50;

/**
 * MetroAreaSelector Component
 */
export function MetroAreaSelector({
  value,
  metros,
  onChange,
  userLocation,
  isDetecting = false,
  detectionError,
  onDetectLocation,
  placeholder = 'Select your metro area',
  disabled = false,
}: MetroAreaSelectorProps) {
  const [isOpen, setIsOpen] = useState(false);

  /**
   * Calculate distances and sort metros if user location is available
   */
  const sortedMetros = useMemo<MetroWithDistance[]>(() => {
    if (!userLocation) {
      return metros.map(metro => ({ ...metro }));
    }

    // Calculate distance for each metro
    const metrosWithDistance = metros.map(metro => {
      const distance = calculateDistance(
        userLocation.latitude,
        userLocation.longitude,
        metro.centerLat,
        metro.centerLng
      );

      return {
        ...metro,
        distance,
        isNearby: distance <= NEARBY_THRESHOLD_MILES,
      };
    });

    // Sort by distance (closest first), but put state-level metros at the end
    return metrosWithDistance.sort((a, b) => {
      // State-level metros (starting with 'all-') should come last
      const isStateA = a.id.startsWith('all-');
      const isStateB = b.id.startsWith('all-');

      if (isStateA && !isStateB) return 1;
      if (!isStateA && isStateB) return -1;

      // Both are regional or both are state-level: sort by distance
      const distA = a.distance ?? Infinity;
      const distB = b.distance ?? Infinity;
      return distA - distB;
    });
  }, [metros, userLocation]);

  /**
   * Group metros into nearby and other categories
   */
  const groupedMetros = useMemo(() => {
    if (!userLocation) {
      return {
        nearby: [],
        other: sortedMetros,
      };
    }

    const nearby = sortedMetros.filter(m => m.isNearby);
    const other = sortedMetros.filter(m => !m.isNearby);

    return { nearby, other };
  }, [sortedMetros, userLocation]);

  /**
   * Find selected metro
   */
  const selectedMetro = useMemo(
    () => metros.find(m => m.id === value),
    [metros, value]
  );

  /**
   * Handle selection change
   */
  const handleChange = (event: React.ChangeEvent<HTMLSelectElement>) => {
    const newValue = event.target.value || null;
    onChange(newValue);
  };

  /**
   * Handle detect location button click
   */
  const handleDetectClick = () => {
    if (onDetectLocation && !isDetecting) {
      onDetectLocation();
    }
  };

  return (
    <div className="space-y-2">
      {/* Select Dropdown */}
      <div className="relative">
        <label htmlFor="metro-area-select" className="sr-only">
          Select Metro Area
        </label>
        <select
          id="metro-area-select"
          value={value || ''}
          onChange={handleChange}
          disabled={disabled || isDetecting}
          className="w-full px-3 py-2 pr-10 bg-white border-2 border-[#e0e0e0] rounded-lg text-sm transition-all duration-200 focus:outline-none focus:border-[#FF7900] focus:ring-2 focus:ring-[#FF7900]/20 disabled:opacity-50 disabled:cursor-not-allowed appearance-none"
          style={{ color: '#333' }}
          aria-label="Metro area selector"
          aria-describedby={detectionError ? 'location-error' : undefined}
        >
          <option value="">{placeholder}</option>

          {/* Nearby metros group */}
          {userLocation && groupedMetros.nearby.length > 0 && (
            <optgroup label="━━━ Nearby Metro Areas ━━━">
              {groupedMetros.nearby.map(metro => (
                <option key={metro.id} value={metro.id}>
                  {metro.name}, {metro.state}
                  {metro.distance !== undefined && ` - ${Math.round(metro.distance)} mi away`}
                </option>
              ))}
            </optgroup>
          )}

          {/* Other metros group */}
          {userLocation && groupedMetros.nearby.length > 0 && (
            <optgroup label="━━━ All Metro Areas ━━━">
              {groupedMetros.other.map(metro => (
                <option key={metro.id} value={metro.id}>
                  {metro.name}, {metro.state}
                  {metro.distance !== undefined && ` - ${Math.round(metro.distance)} mi away`}
                </option>
              ))}
            </optgroup>
          )}

          {/* No location detected - show all metros without grouping */}
          {!userLocation && sortedMetros.map(metro => (
            <option key={metro.id} value={metro.id}>
              {metro.name}, {metro.state}
            </option>
          ))}
        </select>

        {/* Dropdown Icon */}
        <div className="absolute right-3 top-1/2 -translate-y-1/2 pointer-events-none">
          <MapPin className="w-4 h-4" style={{ color: '#FF7900' }} />
        </div>
      </div>

      {/* Selected Metro Display */}
      {selectedMetro && (
        <div className="flex items-center gap-2 text-xs" style={{ color: '#8B1538' }}>
          <MapPinned className="w-3.5 h-3.5" />
          <span className="font-semibold">
            {selectedMetro.name}, {selectedMetro.state}
          </span>
          {sortedMetros.find(m => m.id === selectedMetro.id)?.distance !== undefined && (
            <span style={{ color: '#666' }}>
              ({Math.round(sortedMetros.find(m => m.id === selectedMetro.id)!.distance!)} miles away)
            </span>
          )}
        </div>
      )}

      {/* Detect Location Button */}
      {onDetectLocation && (
        <button
          type="button"
          onClick={handleDetectClick}
          disabled={disabled || isDetecting}
          className="w-full flex items-center justify-center gap-2 px-3 py-2 bg-white border-2 border-[#FF7900] rounded-lg text-xs font-medium transition-all duration-200 hover:bg-[#FF7900]/5 focus:outline-none focus:ring-2 focus:ring-[#FF7900]/20 disabled:opacity-50 disabled:cursor-not-allowed"
          style={{ color: '#FF7900' }}
          aria-label={isDetecting ? 'Detecting location...' : 'Detect my location'}
        >
          {isDetecting ? (
            <>
              <Loader2 className="w-3.5 h-3.5 animate-spin" />
              <span>Detecting Location...</span>
            </>
          ) : (
            <>
              <MapPin className="w-3.5 h-3.5" />
              <span>Detect My Location</span>
            </>
          )}
        </button>
      )}

      {/* Detection Error */}
      {detectionError && (
        <div
          id="location-error"
          className="p-2 text-xs bg-red-50 border border-red-200 rounded-lg"
          style={{ color: '#DC2626' }}
          role="alert"
        >
          {detectionError}
        </div>
      )}

      {/* Location Detected Success */}
      {userLocation && !detectionError && !isDetecting && (
        <div className="flex items-center gap-2 p-2 text-xs bg-green-50 border border-green-200 rounded-lg" style={{ color: '#16A34A' }}>
          <MapPin className="w-3.5 h-3.5" />
          <span>
            Location detected! Metros sorted by distance.
          </span>
        </div>
      )}
    </div>
  );
}
