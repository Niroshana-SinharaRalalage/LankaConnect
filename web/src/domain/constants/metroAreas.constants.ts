/**
 * Metro Areas Constants
 *
 * Defines major metropolitan areas in Ohio and across the United States.
 * These are domain constants used for location-based filtering and discovery.
 */

import { MetroArea } from '../models/MetroArea';

/**
 * Ohio Metropolitan Areas
 */
export const OHIO_METRO_AREAS: readonly MetroArea[] = [
  {
    id: 'cleveland-oh',
    name: 'Cleveland',
    state: 'OH',
    cities: [
      'Cleveland',
      'Lakewood',
      'Parma',
      'Cleveland Heights',
      'Shaker Heights',
      'Euclid',
      'Mentor',
      'Strongsville',
      'Brunswick',
      'Westlake',
      'Aurora', // Added for mock feed data compatibility
    ],
    centerLat: 41.4993,
    centerLng: -81.6944,
    radiusMiles: 30,
  },
  {
    id: 'columbus-oh',
    name: 'Columbus',
    state: 'OH',
    cities: [
      'Columbus',
      'Dublin',
      'Westerville',
      'Grove City',
      'Hilliard',
      'Gahanna',
      'Upper Arlington',
      'Reynoldsburg',
      'Pickerington',
      'Worthington',
    ],
    centerLat: 39.9612,
    centerLng: -82.9988,
    radiusMiles: 35,
  },
  {
    id: 'cincinnati-oh',
    name: 'Cincinnati',
    state: 'OH',
    cities: [
      'Cincinnati',
      'Mason',
      'Hamilton',
      'Fairfield',
      'Middletown',
      'Lebanon',
      'Blue Ash',
      'Sharonville',
      'West Chester',
      'Forest Park',
    ],
    centerLat: 39.1031,
    centerLng: -84.5120,
    radiusMiles: 30,
  },
  {
    id: 'toledo-oh',
    name: 'Toledo',
    state: 'OH',
    cities: [
      'Toledo',
      'Sylvania',
      'Perrysburg',
      'Oregon',
      'Maumee',
      'Bowling Green',
      'Northwood',
      'Rossford',
    ],
    centerLat: 41.6528,
    centerLng: -83.5379,
    radiusMiles: 25,
  },
  {
    id: 'akron-oh',
    name: 'Akron',
    state: 'OH',
    cities: [
      'Akron',
      'Canton',
      'Massillon',
      'Cuyahoga Falls',
      'Stow',
      'Kent',
      'Barberton',
      'Green',
      'Hudson',
    ],
    centerLat: 41.0814,
    centerLng: -81.5190,
    radiusMiles: 25,
  },
  {
    id: 'dayton-oh',
    name: 'Dayton',
    state: 'OH',
    cities: [
      'Dayton',
      'Kettering',
      'Beavercreek',
      'Huber Heights',
      'Fairborn',
      'Centerville',
      'Miamisburg',
      'Springboro',
      'Englewood',
    ],
    centerLat: 39.7589,
    centerLng: -84.1916,
    radiusMiles: 25,
  },
] as const;

/**
 * Major US Metropolitan Areas
 */
export const US_METRO_AREAS: readonly MetroArea[] = [
  {
    id: 'nyc-ny',
    name: 'New York City',
    state: 'NY',
    cities: [
      'Manhattan',
      'Brooklyn',
      'Queens',
      'Bronx',
      'Staten Island',
      'Jersey City',
      'Newark',
      'Hoboken',
      'Yonkers',
      'White Plains',
    ],
    centerLat: 40.7128,
    centerLng: -74.0060,
    radiusMiles: 40,
  },
  {
    id: 'los-angeles-ca',
    name: 'Los Angeles',
    state: 'CA',
    cities: [
      'Los Angeles',
      'Long Beach',
      'Anaheim',
      'Santa Ana',
      'Irvine',
      'Glendale',
      'Pasadena',
      'Torrance',
      'Burbank',
      'Costa Mesa',
    ],
    centerLat: 34.0522,
    centerLng: -118.2437,
    radiusMiles: 45,
  },
  {
    id: 'chicago-il',
    name: 'Chicago',
    state: 'IL',
    cities: [
      'Chicago',
      'Aurora',
      'Naperville',
      'Joliet',
      'Rockford',
      'Elgin',
      'Waukegan',
      'Cicero',
      'Schaumburg',
      'Evanston',
    ],
    centerLat: 41.8781,
    centerLng: -87.6298,
    radiusMiles: 40,
  },
  {
    id: 'houston-tx',
    name: 'Houston',
    state: 'TX',
    cities: [
      'Houston',
      'Sugar Land',
      'The Woodlands',
      'Pearland',
      'League City',
      'Pasadena',
      'Missouri City',
      'Katy',
      'Baytown',
    ],
    centerLat: 29.7604,
    centerLng: -95.3698,
    radiusMiles: 40,
  },
  {
    id: 'phoenix-az',
    name: 'Phoenix',
    state: 'AZ',
    cities: [
      'Phoenix',
      'Mesa',
      'Chandler',
      'Scottsdale',
      'Glendale',
      'Gilbert',
      'Tempe',
      'Peoria',
      'Surprise',
    ],
    centerLat: 33.4484,
    centerLng: -112.0740,
    radiusMiles: 35,
  },
  {
    id: 'philadelphia-pa',
    name: 'Philadelphia',
    state: 'PA',
    cities: [
      'Philadelphia',
      'Camden',
      'Wilmington',
      'Reading',
      'Trenton',
      'Allentown',
      'Bethlehem',
    ],
    centerLat: 39.9526,
    centerLng: -75.1652,
    radiusMiles: 40,
  },
  {
    id: 'san-antonio-tx',
    name: 'San Antonio',
    state: 'TX',
    cities: [
      'San Antonio',
      'New Braunfels',
      'Schertz',
      'Seguin',
      'Universal City',
      'Converse',
    ],
    centerLat: 29.4241,
    centerLng: -98.4936,
    radiusMiles: 30,
  },
  {
    id: 'san-diego-ca',
    name: 'San Diego',
    state: 'CA',
    cities: [
      'San Diego',
      'Chula Vista',
      'Oceanside',
      'Carlsbad',
      'El Cajon',
      'Vista',
      'San Marcos',
      'Encinitas',
    ],
    centerLat: 32.7157,
    centerLng: -117.1611,
    radiusMiles: 30,
  },
  {
    id: 'dallas-tx',
    name: 'Dallas',
    state: 'TX',
    cities: [
      'Dallas',
      'Fort Worth',
      'Arlington',
      'Plano',
      'Irving',
      'Garland',
      'Frisco',
      'McKinney',
      'Grand Prairie',
    ],
    centerLat: 32.7767,
    centerLng: -96.7970,
    radiusMiles: 40,
  },
  {
    id: 'san-jose-ca',
    name: 'San Jose',
    state: 'CA',
    cities: [
      'San Jose',
      'Sunnyvale',
      'Santa Clara',
      'Mountain View',
      'Palo Alto',
      'Milpitas',
      'Cupertino',
      'Los Gatos',
    ],
    centerLat: 37.3382,
    centerLng: -121.8863,
    radiusMiles: 25,
  },
  {
    id: 'austin-tx',
    name: 'Austin',
    state: 'TX',
    cities: [
      'Austin',
      'Round Rock',
      'Cedar Park',
      'Georgetown',
      'Pflugerville',
      'San Marcos',
      'Leander',
    ],
    centerLat: 30.2672,
    centerLng: -97.7431,
    radiusMiles: 30,
  },
  {
    id: 'seattle-wa',
    name: 'Seattle',
    state: 'WA',
    cities: [
      'Seattle',
      'Bellevue',
      'Tacoma',
      'Everett',
      'Kent',
      'Renton',
      'Spokane',
      'Redmond',
      'Kirkland',
    ],
    centerLat: 47.6062,
    centerLng: -122.3321,
    radiusMiles: 35,
  },
  {
    id: 'denver-co',
    name: 'Denver',
    state: 'CO',
    cities: [
      'Denver',
      'Aurora',
      'Lakewood',
      'Boulder',
      'Fort Collins',
      'Thornton',
      'Arvada',
      'Westminster',
    ],
    centerLat: 39.7392,
    centerLng: -104.9903,
    radiusMiles: 30,
  },
  {
    id: 'boston-ma',
    name: 'Boston',
    state: 'MA',
    cities: [
      'Boston',
      'Cambridge',
      'Quincy',
      'Lynn',
      'Newton',
      'Somerville',
      'Waltham',
      'Brookline',
    ],
    centerLat: 42.3601,
    centerLng: -71.0589,
    radiusMiles: 30,
  },
  {
    id: 'atlanta-ga',
    name: 'Atlanta',
    state: 'GA',
    cities: [
      'Atlanta',
      'Sandy Springs',
      'Roswell',
      'Johns Creek',
      'Marietta',
      'Alpharetta',
      'Smyrna',
      'Dunwoody',
    ],
    centerLat: 33.7490,
    centerLng: -84.3880,
    radiusMiles: 35,
  },
  {
    id: 'pittsburgh-pa',
    name: 'Pittsburgh',
    state: 'PA',
    cities: [
      'Pittsburgh',
      'Bethel Park',
      'Monroeville',
      'Mt. Lebanon',
      'Ross Township',
      'Moon Township',
    ],
    centerLat: 40.4406,
    centerLng: -79.9959,
    radiusMiles: 30,
  },
  {
    id: 'buffalo-ny',
    name: 'Buffalo',
    state: 'NY',
    cities: [
      'Buffalo',
      'Cheektowaga',
      'West Seneca',
      'Amherst',
      'Tonawanda',
      'Niagara Falls',
    ],
    centerLat: 42.8864,
    centerLng: -78.8784,
    radiusMiles: 25,
  },
  {
    id: 'indianapolis-in',
    name: 'Indianapolis',
    state: 'IN',
    cities: [
      'Indianapolis',
      'Carmel',
      'Fishers',
      'Noblesville',
      'Greenwood',
      'Lawrence',
    ],
    centerLat: 39.7684,
    centerLng: -86.1581,
    radiusMiles: 35,
  },
  {
    id: 'fort-wayne-in',
    name: 'Fort Wayne',
    state: 'IN',
    cities: [
      'Fort Wayne',
      'New Haven',
      'Huntertown',
      'Leo-Cedarville',
    ],
    centerLat: 41.0793,
    centerLng: -85.1394,
    radiusMiles: 20,
  },
  // Add Ohio metros to US list
  ...OHIO_METRO_AREAS,
] as const;

/**
 * State-level area options for broader geographic coverage
 */
export const STATE_LEVEL_AREAS: readonly MetroArea[] = [
  {
    id: 'all-ohio',
    name: 'All Ohio',
    state: 'OH',
    cities: ['Statewide'],
    centerLat: 40.4173,
    centerLng: -82.9071,
    radiusMiles: 250,
  },
  {
    id: 'all-pennsylvania',
    name: 'All Pennsylvania',
    state: 'PA',
    cities: ['Statewide'],
    centerLat: 41.2033,
    centerLng: -77.1945,
    radiusMiles: 250,
  },
  {
    id: 'all-new-york',
    name: 'All New York',
    state: 'NY',
    cities: ['Statewide'],
    centerLat: 43.2994,
    centerLng: -74.2179,
    radiusMiles: 300,
  },
  {
    id: 'all-indiana',
    name: 'All Indiana',
    state: 'IN',
    cities: ['Statewide'],
    centerLat: 40.2672,
    centerLng: -86.1349,
    radiusMiles: 200,
  },
] as const;

/**
 * All metropolitan areas combined (regional metros first, then state-level options)
 */
export const ALL_METRO_AREAS = [...US_METRO_AREAS, ...STATE_LEVEL_AREAS] as const;

/**
 * Helper function to find metro area by ID
 */
export function getMetroAreaById(id: string): MetroArea | undefined {
  return ALL_METRO_AREAS.find(metro => metro.id === id);
}

/**
 * Helper function to get metro areas by state (includes state-level areas)
 */
export function getMetroAreasByState(state: string): readonly MetroArea[] {
  return ALL_METRO_AREAS.filter(metro => metro.state === state);
}

/**
 * Helper function to check if metro area is state-level
 */
export function isStateLevelArea(metroAreaId: string): boolean {
  return STATE_LEVEL_AREAS.some(area => area.id === metroAreaId);
}

/**
 * Helper function to search metro areas by name or city
 * Note: State-level areas will be included if query matches state name
 */
export function searchMetroAreas(query: string): readonly MetroArea[] {
  const lowerQuery = query.toLowerCase();
  return ALL_METRO_AREAS.filter(
    metro =>
      metro.name.toLowerCase().includes(lowerQuery) ||
      metro.cities.some(city => city.toLowerCase().includes(lowerQuery))
  );
}
