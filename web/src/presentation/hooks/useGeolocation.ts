import { useState, useEffect } from 'react';

interface GeolocationState {
  latitude: number | null;
  longitude: number | null;
  loading: boolean;
  error: string | null;
}

interface IpApiResponse {
  latitude: number;
  longitude: number;
  city?: string;
  region?: string;
  country?: string;
}

/**
 * Hook to detect user's geographic location
 *
 * Priority 1: Browser Geolocation API (most accurate, requires user permission)
 * Priority 2: IP-based geolocation (fallback, no permission needed)
 *
 * Used for anonymous users to show location-relevant events
 */
export function useGeolocation(enabled: boolean = true) {
  const [state, setState] = useState<GeolocationState>({
    latitude: null,
    longitude: null,
    loading: enabled,
    error: null,
  });

  useEffect(() => {
    if (!enabled) {
      setState({
        latitude: null,
        longitude: null,
        loading: false,
        error: null,
      });
      return;
    }

    let isMounted = true;

    const getLocation = async () => {
      // Priority 1: Try browser Geolocation API (most accurate)
      if ('geolocation' in navigator) {
        navigator.geolocation.getCurrentPosition(
          (position) => {
            if (isMounted) {
              setState({
                latitude: position.coords.latitude,
                longitude: position.coords.longitude,
                loading: false,
                error: null,
              });
            }
          },
          async (error) => {
            // Browser geolocation failed/denied - fallback to IP geolocation
            console.warn('Browser geolocation failed:', error.message);
            await getIpBasedLocation();
          },
          {
            timeout: 10000, // 10 second timeout
            maximumAge: 300000, // Cache for 5 minutes
            enableHighAccuracy: false, // Faster, less battery drain
          }
        );
      } else {
        // Browser doesn't support geolocation - use IP fallback
        await getIpBasedLocation();
      }
    };

    const getIpBasedLocation = async () => {
      try {
        // Using ipapi.co - free, no API key required
        // Rate limit: 1000 requests/day for free tier
        const response = await fetch('https://ipapi.co/json/', {
          method: 'GET',
          headers: {
            'Accept': 'application/json',
          },
        });

        if (!response.ok) {
          throw new Error(`IP geolocation API error: ${response.status}`);
        }

        const data: IpApiResponse = await response.json();

        if (isMounted && data.latitude && data.longitude) {
          setState({
            latitude: data.latitude,
            longitude: data.longitude,
            loading: false,
            error: null,
          });
        } else {
          throw new Error('Invalid response from IP geolocation service');
        }
      } catch (err) {
        console.error('IP-based geolocation failed:', err);
        if (isMounted) {
          setState({
            latitude: null,
            longitude: null,
            loading: false,
            error: err instanceof Error ? err.message : 'Failed to detect location',
          });
        }
      }
    };

    getLocation();

    return () => {
      isMounted = false;
    };
  }, [enabled]);

  return state;
}
