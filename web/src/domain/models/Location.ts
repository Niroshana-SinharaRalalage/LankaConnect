/**
 * Location Domain Model
 *
 * Represents user location information with accuracy and timestamp.
 * This is a value object in the location domain.
 */

/**
 * Value Object: User Location
 *
 * Represents a user's geographic location at a specific point in time.
 * Immutable and self-validating.
 */
export interface UserLocation {
  readonly latitude: number;
  readonly longitude: number;
  readonly accuracy: number;
  readonly timestamp: Date;
}

/**
 * Factory function to create a UserLocation with validation
 */
export function createUserLocation(data: {
  latitude: number;
  longitude: number;
  accuracy: number;
  timestamp?: Date;
}): UserLocation {
  // Validation
  if (data.latitude < -90 || data.latitude > 90) {
    throw new Error('Invalid latitude: must be between -90 and 90');
  }
  if (data.longitude < -180 || data.longitude > 180) {
    throw new Error('Invalid longitude: must be between -180 and 180');
  }
  if (data.accuracy < 0) {
    throw new Error('Accuracy must be non-negative');
  }

  return {
    latitude: data.latitude,
    longitude: data.longitude,
    accuracy: data.accuracy,
    timestamp: data.timestamp || new Date(),
  };
}

/**
 * Check if location is accurate enough for use
 * @param location - User location to check
 * @param maxAccuracyMeters - Maximum acceptable accuracy in meters (default: 100m)
 */
export function isLocationAccurate(
  location: UserLocation,
  maxAccuracyMeters: number = 100
): boolean {
  return location.accuracy <= maxAccuracyMeters;
}

/**
 * Check if location is stale
 * @param location - User location to check
 * @param maxAgeMinutes - Maximum acceptable age in minutes (default: 30 minutes)
 */
export function isLocationStale(
  location: UserLocation,
  maxAgeMinutes: number = 30
): boolean {
  const now = new Date();
  const ageMs = now.getTime() - location.timestamp.getTime();
  const ageMinutes = ageMs / (1000 * 60);
  return ageMinutes > maxAgeMinutes;
}

/**
 * Location permission status
 */
export type LocationPermissionStatus =
  | 'granted'
  | 'denied'
  | 'prompt'
  | 'unavailable';

/**
 * Location error types
 */
export type LocationErrorType =
  | 'permission_denied'
  | 'position_unavailable'
  | 'timeout'
  | 'unknown';

/**
 * Value Object: Location Error
 */
export interface LocationError {
  readonly type: LocationErrorType;
  readonly message: string;
  readonly timestamp: Date;
}

/**
 * Create a location error
 */
export function createLocationError(
  type: LocationErrorType,
  message?: string
): LocationError {
  const defaultMessages: Record<LocationErrorType, string> = {
    permission_denied: 'Location permission was denied',
    position_unavailable: 'Location information is unavailable',
    timeout: 'Location request timed out',
    unknown: 'An unknown error occurred while getting location',
  };

  return {
    type,
    message: message || defaultMessages[type],
    timestamp: new Date(),
  };
}

/**
 * Location service response
 */
export type LocationResult =
  | { success: true; location: UserLocation }
  | { success: false; error: LocationError };

/**
 * Distance calculation utilities
 */
export const LocationUtils = {
  /**
   * Calculate distance between two locations in meters
   */
  calculateDistance(from: UserLocation, to: UserLocation): number {
    const R = 6371000; // Earth's radius in meters
    const lat1 = toRadians(from.latitude);
    const lat2 = toRadians(to.latitude);
    const deltaLat = toRadians(to.latitude - from.latitude);
    const deltaLon = toRadians(to.longitude - from.longitude);

    const a =
      Math.sin(deltaLat / 2) * Math.sin(deltaLat / 2) +
      Math.cos(lat1) *
      Math.cos(lat2) *
      Math.sin(deltaLon / 2) *
      Math.sin(deltaLon / 2);

    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    return R * c;
  },

  /**
   * Format distance for display
   */
  formatDistance(meters: number): string {
    if (meters < 1000) {
      return `${Math.round(meters)}m`;
    }
    const km = meters / 1000;
    return `${km.toFixed(1)}km`;
  },

  /**
   * Check if two locations are within a certain distance
   */
  isWithinDistance(
    from: UserLocation,
    to: UserLocation,
    maxDistanceMeters: number
  ): boolean {
    return this.calculateDistance(from, to) <= maxDistanceMeters;
  },
};

/**
 * Convert degrees to radians
 */
function toRadians(degrees: number): number {
  return degrees * (Math.PI / 180);
}
