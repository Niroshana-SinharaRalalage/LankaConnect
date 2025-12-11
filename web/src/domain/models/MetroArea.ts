/**
 * Metro Area Domain Model
 *
 * Represents a metropolitan area with geographic boundaries.
 * This is a value object in the location domain.
 */

/**
 * Geographic coordinates value object
 */
export interface Coordinates {
  readonly latitude: number;
  readonly longitude: number;
}

/**
 * Value Object: Metropolitan Area
 *
 * Represents a geographic metropolitan area with its boundaries and cities.
 * Immutable and self-validating.
 */
export interface MetroArea {
  readonly id: string;
  readonly name: string;
  readonly state: string;
  readonly cities: readonly string[];
  readonly centerLat: number;
  readonly centerLng: number;
  readonly radiusMiles: number;
}

/**
 * Factory function to create a MetroArea with validation
 */
export function createMetroArea(data: {
  id: string;
  name: string;
  state: string;
  cities: string[];
  centerLat: number;
  centerLng: number;
  radiusMiles: number;
}): MetroArea {
  // Validation
  if (!data.id.trim()) {
    throw new Error('Metro area ID cannot be empty');
  }
  if (!data.name.trim()) {
    throw new Error('Metro area name cannot be empty');
  }
  if (!data.state.trim()) {
    throw new Error('Metro area state cannot be empty');
  }
  if (data.cities.length === 0) {
    throw new Error('Metro area must have at least one city');
  }
  if (data.centerLat < -90 || data.centerLat > 90) {
    throw new Error('Invalid latitude: must be between -90 and 90');
  }
  if (data.centerLng < -180 || data.centerLng > 180) {
    throw new Error('Invalid longitude: must be between -180 and 180');
  }
  if (data.radiusMiles <= 0) {
    throw new Error('Radius must be positive');
  }

  return {
    id: data.id,
    name: data.name,
    state: data.state,
    cities: [...data.cities],
    centerLat: data.centerLat,
    centerLng: data.centerLng,
    radiusMiles: data.radiusMiles,
  };
}

/**
 * Check if coordinates are within metro area radius
 */
export function isWithinMetroArea(
  metro: MetroArea,
  coords: Coordinates
): boolean {
  const distance = calculateDistance(
    metro.centerLat,
    metro.centerLng,
    coords.latitude,
    coords.longitude
  );
  return distance <= metro.radiusMiles;
}

/**
 * Calculate distance between two points using Haversine formula
 * Returns distance in miles
 */
function calculateDistance(
  lat1: number,
  lon1: number,
  lat2: number,
  lon2: number
): number {
  const R = 3959; // Earth's radius in miles
  const dLat = toRadians(lat2 - lat1);
  const dLon = toRadians(lon2 - lon1);

  const a =
    Math.sin(dLat / 2) * Math.sin(dLat / 2) +
    Math.cos(toRadians(lat1)) *
    Math.cos(toRadians(lat2)) *
    Math.sin(dLon / 2) *
    Math.sin(dLon / 2);

  const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
  return R * c;
}

/**
 * Convert degrees to radians
 */
function toRadians(degrees: number): number {
  return degrees * (Math.PI / 180);
}

/**
 * Find nearest metro area to given coordinates
 */
export function findNearestMetroArea(
  coords: Coordinates,
  metroAreas: readonly MetroArea[]
): MetroArea | null {
  if (metroAreas.length === 0) {
    return null;
  }

  let nearest = metroAreas[0];
  let minDistance = calculateDistance(
    nearest.centerLat,
    nearest.centerLng,
    coords.latitude,
    coords.longitude
  );

  for (const metro of metroAreas.slice(1)) {
    const distance = calculateDistance(
      metro.centerLat,
      metro.centerLng,
      coords.latitude,
      coords.longitude
    );
    if (distance < minDistance) {
      minDistance = distance;
      nearest = metro;
    }
  }

  return nearest;
}
