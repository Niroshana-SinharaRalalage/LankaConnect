/**
 * Geolocation Utility
 *
 * Provides browser geolocation API wrapper with proper error handling.
 * Returns UserLocation domain model or null on failure.
 */

import { createUserLocation, createLocationError, type UserLocation, type LocationError } from '@/domain/models/Location';

/**
 * Geolocation request options
 */
interface GeolocationOptions {
  timeout?: number;
  maximumAge?: number;
  enableHighAccuracy?: boolean;
}

/**
 * Default geolocation options
 */
const DEFAULT_OPTIONS: GeolocationOptions = {
  timeout: 10000, // 10 seconds
  maximumAge: 300000, // 5 minutes
  enableHighAccuracy: false, // Faster response, acceptable accuracy
};

/**
 * Request user's current geolocation
 *
 * @param options - Geolocation options
 * @returns Promise resolving to UserLocation or null on failure
 *
 * Error cases:
 * - Browser doesn't support geolocation
 * - User denies permission
 * - Position unavailable (GPS off, no signal)
 * - Request timeout
 */
export async function requestGeolocation(
  options: GeolocationOptions = DEFAULT_OPTIONS
): Promise<UserLocation | null> {
  // Check if geolocation is supported
  if (!('geolocation' in navigator)) {
    console.error('Geolocation is not supported by this browser');
    return null;
  }

  return new Promise((resolve) => {
    navigator.geolocation.getCurrentPosition(
      // Success callback
      (position) => {
        try {
          const location = createUserLocation({
            latitude: position.coords.latitude,
            longitude: position.coords.longitude,
            accuracy: position.coords.accuracy,
            timestamp: new Date(position.timestamp),
          });
          resolve(location);
        } catch (error) {
          console.error('Failed to create location object:', error);
          resolve(null);
        }
      },
      // Error callback
      (error) => {
        console.error('Geolocation error:', error.message);
        resolve(null);
      },
      // Options
      {
        timeout: options.timeout,
        maximumAge: options.maximumAge,
        enableHighAccuracy: options.enableHighAccuracy,
      }
    );
  });
}

/**
 * Request geolocation with detailed error information
 *
 * @param options - Geolocation options
 * @returns Promise resolving to success/error result
 */
export async function requestGeolocationWithError(
  options: GeolocationOptions = DEFAULT_OPTIONS
): Promise<{ location: UserLocation | null; error: LocationError | null }> {
  // Check if geolocation is supported
  if (!('geolocation' in navigator)) {
    return {
      location: null,
      error: createLocationError(
        'position_unavailable',
        'Geolocation is not supported by this browser'
      ),
    };
  }

  return new Promise((resolve) => {
    navigator.geolocation.getCurrentPosition(
      // Success callback
      (position) => {
        try {
          const location = createUserLocation({
            latitude: position.coords.latitude,
            longitude: position.coords.longitude,
            accuracy: position.coords.accuracy,
            timestamp: new Date(position.timestamp),
          });
          resolve({ location, error: null });
        } catch (error) {
          resolve({
            location: null,
            error: createLocationError('unknown', 'Failed to process location data'),
          });
        }
      },
      // Error callback
      (error) => {
        let locationError: LocationError;

        switch (error.code) {
          case error.PERMISSION_DENIED:
            locationError = createLocationError(
              'permission_denied',
              'Location permission was denied. Please enable location access in your browser settings.'
            );
            break;
          case error.POSITION_UNAVAILABLE:
            locationError = createLocationError(
              'position_unavailable',
              'Location information is unavailable. Please check your device settings.'
            );
            break;
          case error.TIMEOUT:
            locationError = createLocationError(
              'timeout',
              'Location request timed out. Please try again.'
            );
            break;
          default:
            locationError = createLocationError(
              'unknown',
              error.message || 'An unknown error occurred while getting location'
            );
        }

        resolve({ location: null, error: locationError });
      },
      // Options
      {
        timeout: options.timeout,
        maximumAge: options.maximumAge,
        enableHighAccuracy: options.enableHighAccuracy,
      }
    );
  });
}

/**
 * Check if geolocation is available in the browser
 */
export function isGeolocationAvailable(): boolean {
  return 'geolocation' in navigator;
}

/**
 * Check geolocation permission status
 * Note: Not all browsers support the Permissions API
 */
export async function checkGeolocationPermission(): Promise<PermissionState | 'unavailable'> {
  if (!('permissions' in navigator)) {
    return 'unavailable';
  }

  try {
    const result = await navigator.permissions.query({ name: 'geolocation' });
    return result.state;
  } catch (error) {
    console.error('Failed to query geolocation permission:', error);
    return 'unavailable';
  }
}
