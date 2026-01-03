/**
 * Metro Areas Constants - Phase 5B.3
 *
 * Generated from backend MetroAreaSeeder.cs with GUID-based IDs.
 * Includes all 50 US states with state-level areas plus 100+ major metro areas.
 *
 * Data Structure:
 * - 50 state-level entries (All [StateName]) - used for broad geographic coverage
 * - 100+ major US metro areas grouped by state
 * - Each metro uses backend GUID as ID for consistency with API
 */

import { MetroArea } from '../models/MetroArea';

/**
 * US State Information
 */
export interface StateInfo {
  readonly code: string;
  readonly name: string;
}

export const US_STATES: readonly StateInfo[] = [
  { code: 'AL', name: 'Alabama' },
  { code: 'AK', name: 'Alaska' },
  { code: 'AZ', name: 'Arizona' },
  { code: 'AR', name: 'Arkansas' },
  { code: 'CA', name: 'California' },
  { code: 'CO', name: 'Colorado' },
  { code: 'CT', name: 'Connecticut' },
  { code: 'DE', name: 'Delaware' },
  { code: 'FL', name: 'Florida' },
  { code: 'GA', name: 'Georgia' },
  { code: 'HI', name: 'Hawaii' },
  { code: 'ID', name: 'Idaho' },
  { code: 'IL', name: 'Illinois' },
  { code: 'IN', name: 'Indiana' },
  { code: 'IA', name: 'Iowa' },
  { code: 'KS', name: 'Kansas' },
  { code: 'KY', name: 'Kentucky' },
  { code: 'LA', name: 'Louisiana' },
  { code: 'ME', name: 'Maine' },
  { code: 'MD', name: 'Maryland' },
  { code: 'MA', name: 'Massachusetts' },
  { code: 'MI', name: 'Michigan' },
  { code: 'MN', name: 'Minnesota' },
  { code: 'MS', name: 'Mississippi' },
  { code: 'MO', name: 'Missouri' },
  { code: 'MT', name: 'Montana' },
  { code: 'NE', name: 'Nebraska' },
  { code: 'NV', name: 'Nevada' },
  { code: 'NH', name: 'New Hampshire' },
  { code: 'NJ', name: 'New Jersey' },
  { code: 'NM', name: 'New Mexico' },
  { code: 'NY', name: 'New York' },
  { code: 'NC', name: 'North Carolina' },
  { code: 'ND', name: 'North Dakota' },
  { code: 'OH', name: 'Ohio' },
  { code: 'OK', name: 'Oklahoma' },
  { code: 'OR', name: 'Oregon' },
  { code: 'PA', name: 'Pennsylvania' },
  { code: 'RI', name: 'Rhode Island' },
  { code: 'SC', name: 'South Carolina' },
  { code: 'SD', name: 'South Dakota' },
  { code: 'TN', name: 'Tennessee' },
  { code: 'TX', name: 'Texas' },
  { code: 'UT', name: 'Utah' },
  { code: 'VT', name: 'Vermont' },
  { code: 'VA', name: 'Virginia' },
  { code: 'WA', name: 'Washington' },
  { code: 'WV', name: 'West Virginia' },
  { code: 'WI', name: 'Wisconsin' },
  { code: 'WY', name: 'Wyoming' },
] as const;

/**
 * All Metro Areas - Organized by State
 * GUIDs match backend seeder for consistency
 */
export const ALL_METRO_AREAS: readonly MetroArea[] = [
  // =====================
  // ALABAMA
  // =====================
  {
    id: '01000000-0000-0000-0000-000000000001',
    name: 'All Alabama',
    state: 'AL',
    cities: ['Statewide'],
    centerLat: 32.8067,
    centerLng: -86.7113,
    radiusMiles: 200,
  },
  {
    id: '01111111-1111-1111-1111-111111111001',
    name: 'Birmingham',
    state: 'AL',
    cities: ['Birmingham', 'Hoover', 'Vestavia Hills', 'Alabaster', 'Bessemer'],
    centerLat: 33.5186,
    centerLng: -86.8104,
    radiusMiles: 30,
  },
  {
    id: '01111111-1111-1111-1111-111111111002',
    name: 'Montgomery',
    state: 'AL',
    cities: ['Montgomery', 'Prattville', 'Millbrook'],
    centerLat: 32.3792,
    centerLng: -86.3077,
    radiusMiles: 25,
  },
  {
    id: '01111111-1111-1111-1111-111111111003',
    name: 'Mobile',
    state: 'AL',
    cities: ['Mobile', 'Prichard', 'Saraland', 'Chickasaw'],
    centerLat: 30.6954,
    centerLng: -88.0399,
    radiusMiles: 25,
  },

  // =====================
  // ALASKA
  // =====================
  {
    id: '02000000-0000-0000-0000-000000000001',
    name: 'All Alaska',
    state: 'AK',
    cities: ['Statewide'],
    centerLat: 64.0685,
    centerLng: -152.2782,
    radiusMiles: 300,
  },
  {
    id: '02111111-1111-1111-1111-111111111001',
    name: 'Anchorage',
    state: 'AK',
    cities: ['Anchorage', 'Eagle River', 'Girdwood'],
    centerLat: 61.2181,
    centerLng: -149.9003,
    radiusMiles: 30,
  },

  // =====================
  // ARIZONA
  // =====================
  {
    id: '04000000-0000-0000-0000-000000000001',
    name: 'All Arizona',
    state: 'AZ',
    cities: ['Statewide'],
    centerLat: 33.7298,
    centerLng: -111.4312,
    radiusMiles: 200,
  },
  {
    id: '04111111-1111-1111-1111-111111111001',
    name: 'Phoenix',
    state: 'AZ',
    cities: ['Phoenix', 'Scottsdale', 'Tempe', 'Glendale', 'Chandler', 'Gilbert', 'Peoria', 'Surprise'],
    centerLat: 33.4484,
    centerLng: -112.0742,
    radiusMiles: 35,
  },
  {
    id: '04111111-1111-1111-1111-111111111002',
    name: 'Tucson',
    state: 'AZ',
    cities: ['Tucson', 'Oro Valley', 'Marana', 'Sahuarita'],
    centerLat: 32.2226,
    centerLng: -110.9747,
    radiusMiles: 30,
  },
  {
    id: '04111111-1111-1111-1111-111111111003',
    name: 'Mesa',
    state: 'AZ',
    cities: ['Mesa', 'Apache Junction', 'Queen Creek'],
    centerLat: 33.4152,
    centerLng: -111.8317,
    radiusMiles: 25,
  },

  // =====================
  // ARKANSAS
  // =====================
  {
    id: '05000000-0000-0000-0000-000000000001',
    name: 'All Arkansas',
    state: 'AR',
    cities: ['Statewide'],
    centerLat: 34.9697,
    centerLng: -92.3731,
    radiusMiles: 200,
  },
  {
    id: '05111111-1111-1111-1111-111111111001',
    name: 'Little Rock',
    state: 'AR',
    cities: ['Little Rock', 'North Little Rock', 'Conway', 'Benton'],
    centerLat: 34.7465,
    centerLng: -92.2896,
    radiusMiles: 30,
  },
  {
    id: '05111111-1111-1111-1111-111111111002',
    name: 'Fayetteville',
    state: 'AR',
    cities: ['Fayetteville', 'Springdale', 'Rogers', 'Bentonville'],
    centerLat: 36.0627,
    centerLng: -94.1734,
    radiusMiles: 25,
  },

  // =====================
  // CALIFORNIA
  // =====================
  {
    id: '06000000-0000-0000-0000-000000000001',
    name: 'All California',
    state: 'CA',
    cities: ['Statewide'],
    centerLat: 36.1162,
    centerLng: -119.6816,
    radiusMiles: 250,
  },
  {
    id: '06111111-1111-1111-1111-111111111001',
    name: 'Los Angeles',
    state: 'CA',
    cities: ['Los Angeles', 'Long Beach', 'Anaheim', 'Santa Ana', 'Irvine', 'Glendale', 'Pasadena', 'Torrance', 'Burbank'],
    centerLat: 34.0522,
    centerLng: -118.2437,
    radiusMiles: 40,
  },
  {
    id: '06111111-1111-1111-1111-111111111002',
    name: 'San Francisco Bay Area',
    state: 'CA',
    cities: ['San Francisco', 'Oakland', 'San Jose', 'Berkeley', 'Fremont', 'Hayward', 'Sunnyvale', 'Santa Clara'],
    centerLat: 37.7749,
    centerLng: -122.4194,
    radiusMiles: 40,
  },
  {
    id: '06111111-1111-1111-1111-111111111003',
    name: 'San Diego',
    state: 'CA',
    cities: ['San Diego', 'Chula Vista', 'Oceanside', 'Carlsbad', 'El Cajon', 'Vista'],
    centerLat: 32.7157,
    centerLng: -117.1611,
    radiusMiles: 35,
  },
  {
    id: '06111111-1111-1111-1111-111111111004',
    name: 'Sacramento',
    state: 'CA',
    cities: ['Sacramento', 'Elk Grove', 'Roseville', 'Folsom', 'Davis'],
    centerLat: 38.5816,
    centerLng: -121.4944,
    radiusMiles: 30,
  },
  {
    id: '06111111-1111-1111-1111-111111111005',
    name: 'Fresno',
    state: 'CA',
    cities: ['Fresno', 'Clovis', 'Madera', 'Sanger'],
    centerLat: 36.7469,
    centerLng: -119.7726,
    radiusMiles: 25,
  },
  {
    id: '06111111-1111-1111-1111-111111111006',
    name: 'Inland Empire',
    state: 'CA',
    cities: ['Riverside', 'San Bernardino', 'Ontario', 'Rancho Cucamonga', 'Corona', 'Moreno Valley'],
    centerLat: 33.9819,
    centerLng: -117.2466,
    radiusMiles: 35,
  },

  // =====================
  // COLORADO
  // =====================
  {
    id: '08000000-0000-0000-0000-000000000001',
    name: 'All Colorado',
    state: 'CO',
    cities: ['Statewide'],
    centerLat: 39.0598,
    centerLng: -105.3111,
    radiusMiles: 200,
  },
  {
    id: '08111111-1111-1111-1111-111111111001',
    name: 'Denver',
    state: 'CO',
    cities: ['Denver', 'Aurora', 'Lakewood', 'Thornton', 'Arvada', 'Westminster', 'Centennial'],
    centerLat: 39.7392,
    centerLng: -104.9903,
    radiusMiles: 35,
  },
  {
    id: '08111111-1111-1111-1111-111111111002',
    name: 'Colorado Springs',
    state: 'CO',
    cities: ['Colorado Springs', 'Fountain', 'Monument'],
    centerLat: 38.8339,
    centerLng: -104.8202,
    radiusMiles: 30,
  },

  // =====================
  // CONNECTICUT
  // =====================
  {
    id: '09000000-0000-0000-0000-000000000001',
    name: 'All Connecticut',
    state: 'CT',
    cities: ['Statewide'],
    centerLat: 41.5978,
    centerLng: -72.7554,
    radiusMiles: 150,
  },
  {
    id: '09111111-1111-1111-1111-111111111001',
    name: 'Hartford',
    state: 'CT',
    cities: ['Hartford', 'West Hartford', 'New Britain', 'Bristol'],
    centerLat: 41.7658,
    centerLng: -72.6734,
    radiusMiles: 25,
  },
  {
    id: '09111111-1111-1111-1111-111111111002',
    name: 'Bridgeport',
    state: 'CT',
    cities: ['Bridgeport', 'Stamford', 'Norwalk', 'Danbury'],
    centerLat: 41.1834,
    centerLng: -73.1959,
    radiusMiles: 25,
  },

  // =====================
  // DELAWARE
  // =====================
  {
    id: '10000000-0000-0000-0000-000000000001',
    name: 'All Delaware',
    state: 'DE',
    cities: ['Statewide'],
    centerLat: 39.3185,
    centerLng: -75.5244,
    radiusMiles: 120,
  },
  {
    id: '10111111-1111-1111-1111-111111111001',
    name: 'Wilmington',
    state: 'DE',
    cities: ['Wilmington', 'Newark', 'Dover'],
    centerLat: 39.7391,
    centerLng: -75.5244,
    radiusMiles: 25,
  },

  // =====================
  // FLORIDA
  // =====================
  {
    id: '12000000-0000-0000-0000-000000000001',
    name: 'All Florida',
    state: 'FL',
    cities: ['Statewide'],
    centerLat: 27.6648,
    centerLng: -81.5158,
    radiusMiles: 250,
  },
  {
    id: '12111111-1111-1111-1111-111111111001',
    name: 'Miami',
    state: 'FL',
    cities: ['Miami', 'Fort Lauderdale', 'Hollywood', 'Coral Gables', 'Hialeah', 'Pembroke Pines'],
    centerLat: 25.7617,
    centerLng: -80.1918,
    radiusMiles: 35,
  },
  {
    id: '12111111-1111-1111-1111-111111111002',
    name: 'Orlando',
    state: 'FL',
    cities: ['Orlando', 'Kissimmee', 'Winter Park', 'Sanford', 'Altamonte Springs'],
    centerLat: 28.5421,
    centerLng: -81.3723,
    radiusMiles: 30,
  },
  {
    id: '12111111-1111-1111-1111-111111111003',
    name: 'Tampa Bay',
    state: 'FL',
    cities: ['Tampa', 'St. Petersburg', 'Clearwater', 'Brandon', 'Largo'],
    centerLat: 27.9506,
    centerLng: -82.4572,
    radiusMiles: 30,
  },
  {
    id: '12111111-1111-1111-1111-111111111004',
    name: 'Jacksonville',
    state: 'FL',
    cities: ['Jacksonville', 'Jacksonville Beach', 'Orange Park'],
    centerLat: 30.3322,
    centerLng: -81.6557,
    radiusMiles: 30,
  },

  // =====================
  // GEORGIA
  // =====================
  {
    id: '13000000-0000-0000-0000-000000000001',
    name: 'All Georgia',
    state: 'GA',
    cities: ['Statewide'],
    centerLat: 33.0406,
    centerLng: -83.6431,
    radiusMiles: 200,
  },
  {
    id: '13111111-1111-1111-1111-111111111001',
    name: 'Atlanta',
    state: 'GA',
    cities: ['Atlanta', 'Sandy Springs', 'Roswell', 'Johns Creek', 'Marietta', 'Alpharetta', 'Smyrna'],
    centerLat: 33.7490,
    centerLng: -84.3880,
    radiusMiles: 40,
  },
  {
    id: '13111111-1111-1111-1111-111111111002',
    name: 'Savannah',
    state: 'GA',
    cities: ['Savannah', 'Pooler', 'Hinesville'],
    centerLat: 32.0809,
    centerLng: -81.0912,
    radiusMiles: 25,
  },

  // =====================
  // HAWAII
  // =====================
  {
    id: '15000000-0000-0000-0000-000000000001',
    name: 'All Hawaii',
    state: 'HI',
    cities: ['Statewide'],
    centerLat: 21.0943,
    centerLng: -157.4981,
    radiusMiles: 200,
  },
  {
    id: '15111111-1111-1111-1111-111111111001',
    name: 'Honolulu',
    state: 'HI',
    cities: ['Honolulu', 'Pearl City', 'Kailua', 'Waipahu'],
    centerLat: 21.3099,
    centerLng: -157.8581,
    radiusMiles: 30,
  },

  // =====================
  // IDAHO
  // =====================
  {
    id: '16000000-0000-0000-0000-000000000001',
    name: 'All Idaho',
    state: 'ID',
    cities: ['Statewide'],
    centerLat: 44.2405,
    centerLng: -114.4787,
    radiusMiles: 200,
  },
  {
    id: '16111111-1111-1111-1111-111111111001',
    name: 'Boise',
    state: 'ID',
    cities: ['Boise', 'Meridian', 'Nampa', 'Caldwell'],
    centerLat: 43.6150,
    centerLng: -116.2023,
    radiusMiles: 30,
  },

  // =====================
  // ILLINOIS
  // =====================
  {
    id: '17000000-0000-0000-0000-000000000001',
    name: 'All Illinois',
    state: 'IL',
    cities: ['Statewide'],
    centerLat: 40.3495,
    centerLng: -88.9861,
    radiusMiles: 200,
  },
  {
    id: '17111111-1111-1111-1111-111111111001',
    name: 'Chicago',
    state: 'IL',
    cities: ['Chicago', 'Aurora', 'Naperville', 'Joliet', 'Rockford', 'Elgin', 'Waukegan', 'Cicero', 'Schaumburg', 'Evanston'],
    centerLat: 41.8781,
    centerLng: -87.6298,
    radiusMiles: 45,
  },

  // =====================
  // INDIANA
  // =====================
  {
    id: '18000000-0000-0000-0000-000000000001',
    name: 'All Indiana',
    state: 'IN',
    cities: ['Statewide'],
    centerLat: 39.8494,
    centerLng: -86.2604,
    radiusMiles: 200,
  },
  {
    id: '18111111-1111-1111-1111-111111111001',
    name: 'Indianapolis',
    state: 'IN',
    cities: ['Indianapolis', 'Carmel', 'Fishers', 'Noblesville', 'Greenwood', 'Lawrence'],
    centerLat: 39.7684,
    centerLng: -86.1581,
    radiusMiles: 35,
  },

  // =====================
  // IOWA
  // =====================
  {
    id: '19000000-0000-0000-0000-000000000001',
    name: 'All Iowa',
    state: 'IA',
    cities: ['Statewide'],
    centerLat: 42.0115,
    centerLng: -93.2105,
    radiusMiles: 200,
  },
  {
    id: '19111111-1111-1111-1111-111111111001',
    name: 'Des Moines',
    state: 'IA',
    cities: ['Des Moines', 'West Des Moines', 'Ankeny', 'Urbandale'],
    centerLat: 41.5868,
    centerLng: -93.6250,
    radiusMiles: 30,
  },

  // =====================
  // KANSAS
  // =====================
  {
    id: '20000000-0000-0000-0000-000000000001',
    name: 'All Kansas',
    state: 'KS',
    cities: ['Statewide'],
    centerLat: 38.5266,
    centerLng: -96.7265,
    radiusMiles: 200,
  },
  {
    id: '20111111-1111-1111-1111-111111111001',
    name: 'Kansas City',
    state: 'KS',
    cities: ['Kansas City', 'Overland Park', 'Olathe', 'Lawrence', 'Shawnee'],
    centerLat: 39.0997,
    centerLng: -94.5786,
    radiusMiles: 35,
  },

  // =====================
  // KENTUCKY
  // =====================
  {
    id: '21000000-0000-0000-0000-000000000001',
    name: 'All Kentucky',
    state: 'KY',
    cities: ['Statewide'],
    centerLat: 37.6681,
    centerLng: -84.6701,
    radiusMiles: 200,
  },
  {
    id: '21111111-1111-1111-1111-111111111001',
    name: 'Louisville',
    state: 'KY',
    cities: ['Louisville', 'Jeffersontown', 'Shively', 'St. Matthews'],
    centerLat: 38.2527,
    centerLng: -85.7585,
    radiusMiles: 30,
  },

  // =====================
  // LOUISIANA
  // =====================
  {
    id: '22000000-0000-0000-0000-000000000001',
    name: 'All Louisiana',
    state: 'LA',
    cities: ['Statewide'],
    centerLat: 31.1695,
    centerLng: -91.8749,
    radiusMiles: 200,
  },
  {
    id: '22111111-1111-1111-1111-111111111001',
    name: 'New Orleans',
    state: 'LA',
    cities: ['New Orleans', 'Metairie', 'Kenner', 'Baton Rouge'],
    centerLat: 29.9511,
    centerLng: -90.2623,
    radiusMiles: 30,
  },

  // =====================
  // MAINE
  // =====================
  {
    id: '23000000-0000-0000-0000-000000000001',
    name: 'All Maine',
    state: 'ME',
    cities: ['Statewide'],
    centerLat: 44.6939,
    centerLng: -69.3819,
    radiusMiles: 180,
  },
  {
    id: '23111111-1111-1111-1111-111111111001',
    name: 'Portland',
    state: 'ME',
    cities: ['Portland', 'South Portland', 'Westbrook', 'Biddeford'],
    centerLat: 43.6591,
    centerLng: -70.2568,
    radiusMiles: 25,
  },

  // =====================
  // MARYLAND
  // =====================
  {
    id: '24000000-0000-0000-0000-000000000001',
    name: 'All Maryland',
    state: 'MD',
    cities: ['Statewide'],
    centerLat: 39.0639,
    centerLng: -76.8021,
    radiusMiles: 180,
  },
  {
    id: '24111111-1111-1111-1111-111111111001',
    name: 'Baltimore',
    state: 'MD',
    cities: ['Baltimore', 'Columbia', 'Germantown', 'Silver Spring', 'Rockville'],
    centerLat: 39.2904,
    centerLng: -76.6122,
    radiusMiles: 30,
  },

  // =====================
  // MASSACHUSETTS
  // =====================
  {
    id: '25000000-0000-0000-0000-000000000001',
    name: 'All Massachusetts',
    state: 'MA',
    cities: ['Statewide'],
    centerLat: 42.2352,
    centerLng: -71.0275,
    radiusMiles: 150,
  },
  {
    id: '25111111-1111-1111-1111-111111111001',
    name: 'Boston',
    state: 'MA',
    cities: ['Boston', 'Cambridge', 'Quincy', 'Lynn', 'Newton', 'Somerville', 'Waltham', 'Brookline'],
    centerLat: 42.3601,
    centerLng: -71.0589,
    radiusMiles: 35,
  },

  // =====================
  // MICHIGAN
  // =====================
  {
    id: '26000000-0000-0000-0000-000000000001',
    name: 'All Michigan',
    state: 'MI',
    cities: ['Statewide'],
    centerLat: 43.3266,
    centerLng: -84.5361,
    radiusMiles: 200,
  },
  {
    id: '26111111-1111-1111-1111-111111111001',
    name: 'Detroit',
    state: 'MI',
    cities: ['Detroit', 'Warren', 'Sterling Heights', 'Ann Arbor', 'Livonia', 'Dearborn', 'Westland'],
    centerLat: 42.3314,
    centerLng: -83.0458,
    radiusMiles: 40,
  },

  // =====================
  // MINNESOTA
  // =====================
  {
    id: '27000000-0000-0000-0000-000000000001',
    name: 'All Minnesota',
    state: 'MN',
    cities: ['Statewide'],
    centerLat: 45.6945,
    centerLng: -93.9196,
    radiusMiles: 200,
  },
  {
    id: '27111111-1111-1111-1111-111111111001',
    name: 'Minneapolis-St. Paul',
    state: 'MN',
    cities: ['Minneapolis', 'St. Paul', 'Rochester', 'Bloomington', 'Brooklyn Park', 'Plymouth'],
    centerLat: 44.9537,
    centerLng: -93.0900,
    radiusMiles: 35,
  },

  // =====================
  // MISSISSIPPI
  // =====================
  {
    id: '28000000-0000-0000-0000-000000000001',
    name: 'All Mississippi',
    state: 'MS',
    cities: ['Statewide'],
    centerLat: 32.7416,
    centerLng: -89.6787,
    radiusMiles: 200,
  },
  {
    id: '28111111-1111-1111-1111-111111111001',
    name: 'Jackson',
    state: 'MS',
    cities: ['Jackson', 'Gulfport', 'Biloxi', 'Hattiesburg'],
    centerLat: 32.2988,
    centerLng: -90.1848,
    radiusMiles: 25,
  },

  // =====================
  // MISSOURI
  // =====================
  {
    id: '29000000-0000-0000-0000-000000000001',
    name: 'All Missouri',
    state: 'MO',
    cities: ['Statewide'],
    centerLat: 38.4561,
    centerLng: -92.2884,
    radiusMiles: 200,
  },
  {
    id: '29111111-1111-1111-1111-111111111001',
    name: 'St. Louis',
    state: 'MO',
    cities: ['St. Louis', 'St. Charles', "O'Fallon", 'St. Peters', 'Florissant'],
    centerLat: 38.6270,
    centerLng: -90.1994,
    radiusMiles: 35,
  },
  {
    id: '29111111-1111-1111-1111-111111111002',
    name: 'Kansas City',
    state: 'MO',
    cities: ['Kansas City', 'Independence', 'Lee\'s Summit', 'Blue Springs'],
    centerLat: 39.0997,
    centerLng: -94.5786,
    radiusMiles: 35,
  },

  // =====================
  // MONTANA
  // =====================
  {
    id: '30000000-0000-0000-0000-000000000001',
    name: 'All Montana',
    state: 'MT',
    cities: ['Statewide'],
    centerLat: 46.9219,
    centerLng: -109.6333,
    radiusMiles: 250,
  },
  {
    id: '30111111-1111-1111-1111-111111111001',
    name: 'Billings',
    state: 'MT',
    cities: ['Billings', 'Missoula', 'Great Falls', 'Bozeman'],
    centerLat: 45.7833,
    centerLng: -103.8014,
    radiusMiles: 25,
  },

  // =====================
  // NEBRASKA
  // =====================
  {
    id: '31000000-0000-0000-0000-000000000001',
    name: 'All Nebraska',
    state: 'NE',
    cities: ['Statewide'],
    centerLat: 41.4925,
    centerLng: -99.9018,
    radiusMiles: 200,
  },
  {
    id: '31111111-1111-1111-1111-111111111001',
    name: 'Omaha',
    state: 'NE',
    cities: ['Omaha', 'Lincoln', 'Bellevue', 'Grand Island'],
    centerLat: 41.2565,
    centerLng: -95.9345,
    radiusMiles: 30,
  },

  // =====================
  // NEVADA
  // =====================
  {
    id: '32000000-0000-0000-0000-000000000001',
    name: 'All Nevada',
    state: 'NV',
    cities: ['Statewide'],
    centerLat: 38.8026,
    centerLng: -117.0554,
    radiusMiles: 200,
  },
  {
    id: '32111111-1111-1111-1111-111111111001',
    name: 'Las Vegas',
    state: 'NV',
    cities: ['Las Vegas', 'Henderson', 'North Las Vegas', 'Paradise'],
    centerLat: 36.1699,
    centerLng: -115.1398,
    radiusMiles: 30,
  },
  {
    id: '32111111-1111-1111-1111-111111111002',
    name: 'Reno',
    state: 'NV',
    cities: ['Reno', 'Sparks', 'Carson City'],
    centerLat: 39.5296,
    centerLng: -119.8138,
    radiusMiles: 25,
  },

  // =====================
  // NEW HAMPSHIRE
  // =====================
  {
    id: '33000000-0000-0000-0000-000000000001',
    name: 'All New Hampshire',
    state: 'NH',
    cities: ['Statewide'],
    centerLat: 43.4525,
    centerLng: -71.3102,
    radiusMiles: 150,
  },
  {
    id: '33111111-1111-1111-1111-111111111001',
    name: 'Manchester',
    state: 'NH',
    cities: ['Manchester', 'Nashua', 'Concord', 'Derry'],
    centerLat: 42.9956,
    centerLng: -71.4548,
    radiusMiles: 25,
  },

  // =====================
  // NEW JERSEY
  // =====================
  {
    id: '34000000-0000-0000-0000-000000000001',
    name: 'All New Jersey',
    state: 'NJ',
    cities: ['Statewide'],
    centerLat: 40.2206,
    centerLng: -74.7597,
    radiusMiles: 150,
  },
  {
    id: '34111111-1111-1111-1111-111111111001',
    name: 'Newark',
    state: 'NJ',
    cities: ['Newark', 'Jersey City', 'Paterson', 'Elizabeth', 'Edison', 'Trenton'],
    centerLat: 40.7357,
    centerLng: -74.1724,
    radiusMiles: 30,
  },

  // =====================
  // NEW MEXICO
  // =====================
  {
    id: '35000000-0000-0000-0000-000000000001',
    name: 'All New Mexico',
    state: 'NM',
    cities: ['Statewide'],
    centerLat: 34.8405,
    centerLng: -106.2371,
    radiusMiles: 250,
  },
  {
    id: '35111111-1111-1111-1111-111111111001',
    name: 'Albuquerque',
    state: 'NM',
    cities: ['Albuquerque', 'Rio Rancho', 'Santa Fe', 'Las Cruces'],
    centerLat: 35.0844,
    centerLng: -106.6504,
    radiusMiles: 30,
  },

  // =====================
  // NEW YORK
  // =====================
  {
    id: '36000000-0000-0000-0000-000000000001',
    name: 'All New York',
    state: 'NY',
    cities: ['Statewide'],
    centerLat: 42.1657,
    centerLng: -74.9481,
    radiusMiles: 250,
  },
  {
    id: '36111111-1111-1111-1111-111111111001',
    name: 'New York City',
    state: 'NY',
    cities: ['Manhattan', 'Brooklyn', 'Queens', 'Bronx', 'Staten Island', 'Yonkers', 'White Plains'],
    centerLat: 40.7128,
    centerLng: -74.0060,
    radiusMiles: 40,
  },
  {
    id: '36111111-1111-1111-1111-111111111002',
    name: 'Buffalo',
    state: 'NY',
    cities: ['Buffalo', 'Cheektowaga', 'West Seneca', 'Amherst', 'Tonawanda', 'Niagara Falls'],
    centerLat: 42.8864,
    centerLng: -78.8784,
    radiusMiles: 25,
  },
  {
    id: '36111111-1111-1111-1111-111111111003',
    name: 'Albany',
    state: 'NY',
    cities: ['Albany', 'Schenectady', 'Troy', 'Saratoga Springs'],
    centerLat: 42.6526,
    centerLng: -73.7562,
    radiusMiles: 25,
  },

  // =====================
  // NORTH CAROLINA
  // =====================
  {
    id: '37000000-0000-0000-0000-000000000001',
    name: 'All North Carolina',
    state: 'NC',
    cities: ['Statewide'],
    centerLat: 35.6301,
    centerLng: -79.8064,
    radiusMiles: 200,
  },
  {
    id: '37111111-1111-1111-1111-111111111001',
    name: 'Charlotte',
    state: 'NC',
    cities: ['Charlotte', 'Concord', 'Gastonia', 'Rock Hill'],
    centerLat: 35.2271,
    centerLng: -80.8431,
    radiusMiles: 30,
  },
  {
    id: '37111111-1111-1111-1111-111111111002',
    name: 'Raleigh',
    state: 'NC',
    cities: ['Raleigh', 'Durham', 'Chapel Hill', 'Cary', 'Apex'],
    centerLat: 35.7796,
    centerLng: -78.6382,
    radiusMiles: 30,
  },

  // =====================
  // NORTH DAKOTA
  // =====================
  {
    id: '38000000-0000-0000-0000-000000000001',
    name: 'All North Dakota',
    state: 'ND',
    cities: ['Statewide'],
    centerLat: 47.5289,
    centerLng: -99.7840,
    radiusMiles: 250,
  },
  {
    id: '38111111-1111-1111-1111-111111111001',
    name: 'Fargo',
    state: 'ND',
    cities: ['Fargo', 'West Fargo', 'Moorhead (MN)'],
    centerLat: 46.8772,
    centerLng: -96.7898,
    radiusMiles: 30,
  },
  {
    id: '38111111-1111-1111-1111-111111111002',
    name: 'Bismarck',
    state: 'ND',
    cities: ['Bismarck', 'Mandan', 'Lincoln'],
    centerLat: 46.8083,
    centerLng: -100.7837,
    radiusMiles: 25,
  },

  // =====================
  // OHIO
  // =====================
  {
    id: '39000000-0000-0000-0000-000000000001',
    name: 'All Ohio',
    state: 'OH',
    cities: ['Statewide'],
    centerLat: 40.4173,
    centerLng: -82.9071,
    radiusMiles: 200,
  },
  {
    id: '39111111-1111-1111-1111-111111111001',
    name: 'Cleveland',
    state: 'OH',
    cities: ['Cleveland', 'Lakewood', 'Parma', 'Cleveland Heights', 'Shaker Heights', 'Euclid', 'Mentor', 'Strongsville', 'Brunswick', 'Westlake', 'Aurora'],
    centerLat: 41.4993,
    centerLng: -81.6944,
    radiusMiles: 30,
  },
  {
    id: '39111111-1111-1111-1111-111111111002',
    name: 'Columbus',
    state: 'OH',
    cities: ['Columbus', 'Dublin', 'Westerville', 'Grove City', 'Hilliard', 'Gahanna', 'Upper Arlington', 'Reynoldsburg', 'Pickerington', 'Worthington'],
    centerLat: 39.9612,
    centerLng: -82.9988,
    radiusMiles: 30,
  },
  {
    id: '39111111-1111-1111-1111-111111111003',
    name: 'Cincinnati',
    state: 'OH',
    cities: ['Cincinnati', 'Mason', 'Hamilton', 'Fairfield', 'Middletown', 'Lebanon', 'Blue Ash', 'Sharonville', 'West Chester', 'Forest Park'],
    centerLat: 39.1031,
    centerLng: -84.5120,
    radiusMiles: 30,
  },
  {
    id: '39111111-1111-1111-1111-111111111004',
    name: 'Toledo',
    state: 'OH',
    cities: ['Toledo', 'Sylvania', 'Perrysburg', 'Oregon', 'Maumee', 'Bowling Green', 'Northwood', 'Rossford'],
    centerLat: 41.6528,
    centerLng: -83.5379,
    radiusMiles: 25,
  },

  // =====================
  // OKLAHOMA
  // =====================
  {
    id: '40000000-0000-0000-0000-000000000001',
    name: 'All Oklahoma',
    state: 'OK',
    cities: ['Statewide'],
    centerLat: 35.5653,
    centerLng: -96.9289,
    radiusMiles: 200,
  },
  {
    id: '40111111-1111-1111-1111-111111111001',
    name: 'Oklahoma City',
    state: 'OK',
    cities: ['Oklahoma City', 'Tulsa', 'Norman', 'Broken Arrow', 'Edmond'],
    centerLat: 35.4676,
    centerLng: -97.5164,
    radiusMiles: 30,
  },

  // =====================
  // OREGON
  // =====================
  {
    id: '41000000-0000-0000-0000-000000000001',
    name: 'All Oregon',
    state: 'OR',
    cities: ['Statewide'],
    centerLat: 43.8041,
    centerLng: -120.5542,
    radiusMiles: 200,
  },
  {
    id: '41111111-1111-1111-1111-111111111001',
    name: 'Portland',
    state: 'OR',
    cities: ['Portland', 'Eugene', 'Salem', 'Gresham', 'Hillsboro', 'Beaverton'],
    centerLat: 45.5152,
    centerLng: -122.6784,
    radiusMiles: 30,
  },

  // =====================
  // PENNSYLVANIA
  // =====================
  {
    id: '42000000-0000-0000-0000-000000000001',
    name: 'All Pennsylvania',
    state: 'PA',
    cities: ['Statewide'],
    centerLat: 40.5908,
    centerLng: -77.2098,
    radiusMiles: 200,
  },
  {
    id: '42111111-1111-1111-1111-111111111001',
    name: 'Philadelphia',
    state: 'PA',
    cities: ['Philadelphia', 'Reading', 'Allentown', 'Bethlehem', 'Lancaster'],
    centerLat: 39.9526,
    centerLng: -75.1652,
    radiusMiles: 35,
  },
  {
    id: '42111111-1111-1111-1111-111111111002',
    name: 'Pittsburgh',
    state: 'PA',
    cities: ['Pittsburgh', 'Bethel Park', 'Monroeville', 'Mt. Lebanon', 'Ross Township', 'Moon Township'],
    centerLat: 40.4406,
    centerLng: -79.9959,
    radiusMiles: 30,
  },

  // =====================
  // RHODE ISLAND
  // =====================
  {
    id: '44000000-0000-0000-0000-000000000001',
    name: 'All Rhode Island',
    state: 'RI',
    cities: ['Statewide'],
    centerLat: 41.6809,
    centerLng: -71.5118,
    radiusMiles: 120,
  },
  {
    id: '44111111-1111-1111-1111-111111111001',
    name: 'Providence',
    state: 'RI',
    cities: ['Providence', 'Warwick', 'Cranston', 'Pawtucket'],
    centerLat: 41.8240,
    centerLng: -71.4128,
    radiusMiles: 25,
  },

  // =====================
  // SOUTH CAROLINA
  // =====================
  {
    id: '45000000-0000-0000-0000-000000000001',
    name: 'All South Carolina',
    state: 'SC',
    cities: ['Statewide'],
    centerLat: 33.8361,
    centerLng: -80.9066,
    radiusMiles: 200,
  },
  {
    id: '45111111-1111-1111-1111-111111111001',
    name: 'Charleston',
    state: 'SC',
    cities: ['Charleston', 'Columbia', 'North Charleston', 'Mount Pleasant'],
    centerLat: 32.7765,
    centerLng: -79.9711,
    radiusMiles: 25,
  },

  // =====================
  // SOUTH DAKOTA
  // =====================
  {
    id: '46000000-0000-0000-0000-000000000001',
    name: 'All South Dakota',
    state: 'SD',
    cities: ['Statewide'],
    centerLat: 44.2998,
    centerLng: -99.4388,
    radiusMiles: 250,
  },
  {
    id: '46111111-1111-1111-1111-111111111001',
    name: 'Sioux Falls',
    state: 'SD',
    cities: ['Sioux Falls', 'Brandon', 'Harrisburg', 'Tea'],
    centerLat: 43.5446,
    centerLng: -96.7311,
    radiusMiles: 30,
  },
  {
    id: '46111111-1111-1111-1111-111111111002',
    name: 'Rapid City',
    state: 'SD',
    cities: ['Rapid City', 'Box Elder', 'Summerset', 'Black Hawk'],
    centerLat: 44.0805,
    centerLng: -103.2310,
    radiusMiles: 25,
  },

  // =====================
  // TENNESSEE
  // =====================
  {
    id: '47000000-0000-0000-0000-000000000001',
    name: 'All Tennessee',
    state: 'TN',
    cities: ['Statewide'],
    centerLat: 35.7478,
    centerLng: -86.6923,
    radiusMiles: 200,
  },
  {
    id: '47111111-1111-1111-1111-111111111001',
    name: 'Nashville',
    state: 'TN',
    cities: ['Nashville', 'Franklin', 'Murfreesboro', 'Brentwood'],
    centerLat: 36.1627,
    centerLng: -86.7816,
    radiusMiles: 30,
  },
  {
    id: '47111111-1111-1111-1111-111111111002',
    name: 'Memphis',
    state: 'TN',
    cities: ['Memphis', 'Germantown', 'Collierville', 'Bartlett'],
    centerLat: 35.1495,
    centerLng: -90.0490,
    radiusMiles: 30,
  },

  // =====================
  // TEXAS
  // =====================
  {
    id: '48000000-0000-0000-0000-000000000001',
    name: 'All Texas',
    state: 'TX',
    cities: ['Statewide'],
    centerLat: 31.9686,
    centerLng: -99.9018,
    radiusMiles: 300,
  },
  {
    id: '48111111-1111-1111-1111-111111111001',
    name: 'Houston',
    state: 'TX',
    cities: ['Houston', 'Sugar Land', 'The Woodlands', 'Pearland', 'League City', 'Pasadena', 'Katy'],
    centerLat: 29.7604,
    centerLng: -95.3698,
    radiusMiles: 40,
  },
  {
    id: '48111111-1111-1111-1111-111111111002',
    name: 'Dallas-Fort Worth',
    state: 'TX',
    cities: ['Dallas', 'Fort Worth', 'Arlington', 'Plano', 'Irving', 'Garland', 'Frisco', 'McKinney', 'Grand Prairie'],
    centerLat: 32.7767,
    centerLng: -96.7970,
    radiusMiles: 40,
  },
  {
    id: '48111111-1111-1111-1111-111111111003',
    name: 'Austin',
    state: 'TX',
    cities: ['Austin', 'Round Rock', 'Cedar Park', 'Georgetown', 'Pflugerville', 'San Marcos', 'Leander'],
    centerLat: 30.2672,
    centerLng: -97.7431,
    radiusMiles: 30,
  },
  {
    id: '48111111-1111-1111-1111-111111111004',
    name: 'San Antonio',
    state: 'TX',
    cities: ['San Antonio', 'New Braunfels', 'Schertz', 'Seguin', 'Universal City', 'Converse'],
    centerLat: 29.4241,
    centerLng: -98.4936,
    radiusMiles: 30,
  },

  // =====================
  // UTAH
  // =====================
  {
    id: '49000000-0000-0000-0000-000000000001',
    name: 'All Utah',
    state: 'UT',
    cities: ['Statewide'],
    centerLat: 39.3210,
    centerLng: -111.0937,
    radiusMiles: 200,
  },
  {
    id: '49111111-1111-1111-1111-111111111001',
    name: 'Salt Lake City',
    state: 'UT',
    cities: ['Salt Lake City', 'West Valley City', 'Provo', 'West Jordan', 'Orem', 'Sandy'],
    centerLat: 40.7608,
    centerLng: -111.8910,
    radiusMiles: 30,
  },

  // =====================
  // VERMONT
  // =====================
  {
    id: '50000000-0000-0000-0000-000000000001',
    name: 'All Vermont',
    state: 'VT',
    cities: ['Statewide'],
    centerLat: 44.0459,
    centerLng: -72.7107,
    radiusMiles: 150,
  },
  {
    id: '50111111-1111-1111-1111-111111111001',
    name: 'Burlington',
    state: 'VT',
    cities: ['Burlington', 'South Burlington', 'Essex', 'Winooski'],
    centerLat: 44.4759,
    centerLng: -73.2121,
    radiusMiles: 25,
  },

  // =====================
  // VIRGINIA
  // =====================
  {
    id: '51000000-0000-0000-0000-000000000001',
    name: 'All Virginia',
    state: 'VA',
    cities: ['Statewide'],
    centerLat: 37.7693,
    centerLng: -78.1694,
    radiusMiles: 200,
  },
  {
    id: '51111111-1111-1111-1111-111111111001',
    name: 'Richmond',
    state: 'VA',
    cities: ['Richmond', 'Virginia Beach', 'Norfolk', 'Chesapeake', 'Arlington', 'Alexandria'],
    centerLat: 37.5407,
    centerLng: -77.4360,
    radiusMiles: 30,
  },

  // =====================
  // WASHINGTON
  // =====================
  {
    id: '53000000-0000-0000-0000-000000000001',
    name: 'All Washington',
    state: 'WA',
    cities: ['Statewide'],
    centerLat: 47.7511,
    centerLng: -120.7401,
    radiusMiles: 250,
  },
  {
    id: '53111111-1111-1111-1111-111111111001',
    name: 'Seattle',
    state: 'WA',
    cities: ['Seattle', 'Bellevue', 'Tacoma', 'Everett', 'Kent', 'Renton', 'Spokane', 'Redmond', 'Kirkland'],
    centerLat: 47.6062,
    centerLng: -122.3321,
    radiusMiles: 35,
  },

  // =====================
  // WEST VIRGINIA
  // =====================
  {
    id: '54000000-0000-0000-0000-000000000001',
    name: 'All West Virginia',
    state: 'WV',
    cities: ['Statewide'],
    centerLat: 38.5976,
    centerLng: -80.4549,
    radiusMiles: 200,
  },
  {
    id: '54111111-1111-1111-1111-111111111001',
    name: 'Charleston',
    state: 'WV',
    cities: ['Charleston', 'Huntington', 'South Charleston', 'St. Albans'],
    centerLat: 38.3498,
    centerLng: -81.6326,
    radiusMiles: 25,
  },
  {
    id: '54111111-1111-1111-1111-111111111002',
    name: 'Huntington',
    state: 'WV',
    cities: ['Huntington', 'Ashland (KY)', 'Ironton (OH)', 'Barboursville'],
    centerLat: 38.4192,
    centerLng: -82.4452,
    radiusMiles: 30,
  },

  // =====================
  // WISCONSIN
  // =====================
  {
    id: '55000000-0000-0000-0000-000000000001',
    name: 'All Wisconsin',
    state: 'WI',
    cities: ['Statewide'],
    centerLat: 44.2685,
    centerLng: -89.6165,
    radiusMiles: 200,
  },
  {
    id: '55111111-1111-1111-1111-111111111001',
    name: 'Milwaukee',
    state: 'WI',
    cities: ['Milwaukee', 'Madison', 'Green Bay', 'Kenosha', 'Racine'],
    centerLat: 43.0389,
    centerLng: -87.9065,
    radiusMiles: 30,
  },

  // =====================
  // WYOMING
  // =====================
  {
    id: '56000000-0000-0000-0000-000000000001',
    name: 'All Wyoming',
    state: 'WY',
    cities: ['Statewide'],
    centerLat: 42.7559,
    centerLng: -107.3025,
    radiusMiles: 250,
  },
  {
    id: '56111111-1111-1111-1111-111111111001',
    name: 'Cheyenne',
    state: 'WY',
    cities: ['Cheyenne', 'Laramie', 'Pine Bluffs'],
    centerLat: 41.1400,
    centerLng: -104.8202,
    radiusMiles: 25,
  },
  {
    id: '56111111-1111-1111-1111-111111111002',
    name: 'Casper',
    state: 'WY',
    cities: ['Casper', 'Mills', 'Evansville', 'Bar Nunn'],
    centerLat: 42.8501,
    centerLng: -106.3252,
    radiusMiles: 25,
  },
] as const;

/**
 * Helper function to find metro area by ID
 */
export function getMetroById(id: string): MetroArea | undefined {
  return ALL_METRO_AREAS.find(metro => metro.id === id);
}

/**
 * Helper function to get metro areas by state (includes state-level areas)
 * Returns all metros for a state, including the state-level "All [State]" option
 */
export function getMetrosByState(stateCode: string): readonly MetroArea[] {
  return ALL_METRO_AREAS.filter(metro => metro.state === stateCode);
}

/**
 * Helper function to get full state name from state code
 */
export function getStateName(stateCode: string): string | undefined {
  return US_STATES.find(state => state.code === stateCode)?.name;
}

/**
 * Helper function to search metro areas by name or city
 * Supports fuzzy matching on metro name and city names
 */
export function searchMetrosByName(query: string): readonly MetroArea[] {
  const lowerQuery = query.toLowerCase().trim();

  if (!lowerQuery) {
    return ALL_METRO_AREAS;
  }

  return ALL_METRO_AREAS.filter(
    metro =>
      metro.name.toLowerCase().includes(lowerQuery) ||
      metro.cities.some(city => city.toLowerCase().includes(lowerQuery)) ||
      metro.state.toLowerCase() === lowerQuery
  );
}

/**
 * Helper function to check if a metro area is state-level (All [State])
 */
export function isStateLevelArea(metroId: string): boolean {
  const metro = getMetroById(metroId);
  return metro?.name.startsWith('All ') ?? false;
}

/**
 * Get only state-level metro areas (All [State] entries)
 */
export function getStateLevelAreas(): readonly MetroArea[] {
  return ALL_METRO_AREAS.filter(metro => metro.name.startsWith('All '));
}

/**
 * Get only city-level metro areas (excludes state-level areas)
 */
export function getCityLevelAreas(): readonly MetroArea[] {
  return ALL_METRO_AREAS.filter(metro => !metro.name.startsWith('All '));
}

/**
 * Get metros grouped by state for dropdowns
 * Returns a map of state code to array of metros (state-level first, then city metros)
 */
export function getMetrosGroupedByState(): Map<string, readonly MetroArea[]> {
  const grouped = new Map<string, MetroArea[]>();

  for (const metro of ALL_METRO_AREAS) {
    if (!grouped.has(metro.state)) {
      grouped.set(metro.state, []);
    }
    grouped.get(metro.state)!.push(metro);
  }

  // Ensure state-level area is first in each group
  for (const [state, metros] of grouped) {
    metros.sort((a, b) => {
      if (a.name.startsWith('All ')) return -1;
      if (b.name.startsWith('All ')) return 1;
      return a.name.localeCompare(b.name);
    });
  }

  return grouped;
}
