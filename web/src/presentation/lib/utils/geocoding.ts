/**
 * Geocoding Utility
 * Converts addresses to latitude/longitude coordinates using Nominatim (OpenStreetMap)
 *
 * Features:
 * - Free, no API key required
 * - Rate limit: 1 request per second
 * - Proper user-agent header required
 */

export interface GeocodingResult {
  latitude: number;
  longitude: number;
  displayName: string;
}

/**
 * Geocodes an address using Nominatim OpenStreetMap API
 * @param address Street address
 * @param city City name
 * @param state State/province
 * @param country Country (defaults to United States)
 * @param zipCode ZIP/postal code (optional)
 * @returns Geocoding result with lat/long or null if not found
 */
export async function geocodeAddress(
  address: string,
  city: string,
  state?: string,
  country: string = 'United States',
  zipCode?: string
): Promise<GeocodingResult | null> {
  try {
    // Build search query
    const parts = [address, city, state, zipCode, country].filter(Boolean);
    const searchQuery = parts.join(', ');

    console.log('🗺️ Geocoding address:', searchQuery);

    // Nominatim API endpoint
    const url = new URL('https://nominatim.openstreetmap.org/search');
    url.searchParams.append('q', searchQuery);
    url.searchParams.append('format', 'json');
    url.searchParams.append('limit', '1');
    url.searchParams.append('addressdetails', '1');

    // IMPORTANT: Nominatim requires a valid User-Agent
    const response = await fetch(url.toString(), {
      headers: {
        'User-Agent': 'LankaConnect/1.0 (Event Management Platform)',
      },
    });

    if (!response.ok) {
      console.error('❌ Geocoding API error:', response.status, response.statusText);
      return null;
    }

    const results = await response.json();

    if (!results || results.length === 0) {
      console.warn('⚠️ No geocoding results found for:', searchQuery);
      return null;
    }

    const result = results[0];

    console.log('✅ Geocoding successful:', {
      latitude: parseFloat(result.lat),
      longitude: parseFloat(result.lon),
      displayName: result.display_name,
    });

    return {
      latitude: parseFloat(result.lat),
      longitude: parseFloat(result.lon),
      displayName: result.display_name,
    };
  } catch (error) {
    console.error('❌ Geocoding error:', error);
    return null;
  }
}

/**
 * Validates that coordinates are within reasonable bounds
 */
export function isValidCoordinate(lat: number, lon: number): boolean {
  return (
    lat >= -90 && lat <= 90 &&
    lon >= -180 && lon <= 180
  );
}
