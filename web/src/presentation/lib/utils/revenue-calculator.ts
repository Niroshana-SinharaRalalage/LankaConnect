/**
 * Phase 6A.X: Client-side Revenue Breakdown Calculator
 *
 * This utility provides a preview calculation of revenue breakdown for event organizers.
 * The actual breakdown is calculated on the backend; this is for UI preview purposes only.
 *
 * Formula (tax-inclusive):
 * 1. Gross Amount = Ticket Price (what buyer pays)
 * 2. Sales Tax = Gross - (Gross / (1 + taxRate))
 * 3. Taxable Amount = Gross - Sales Tax
 * 4. Stripe Fee = (Taxable * stripeFeeRate) + stripeFeeFixed
 * 5. Platform Commission = Taxable * platformCommissionRate
 * 6. Organizer Payout = Taxable - Stripe Fee - Platform Commission
 */

import { Currency, RevenueBreakdownDto } from '@/infrastructure/api/types/events.types';

/**
 * Commission settings from backend configuration
 * These are fetched from /api/reference-data/commission-settings
 */
export interface CommissionSettings {
  platformCommissionRate: number;
  stripeFeeRate: number;
  stripeFeeFixed: number;
}

// Default fallback values (match appsettings.json defaults)
const DEFAULT_COMMISSION_SETTINGS: CommissionSettings = {
  platformCommissionRate: 0.02,
  stripeFeeRate: 0.029,
  stripeFeeFixed: 0.30,
};

// Cached commission settings (fetched once from API)
let cachedCommissionSettings: CommissionSettings | null = null;
let settingsFetchPromise: Promise<CommissionSettings> | null = null;

/**
 * Fetch commission settings from backend API
 * Caches the result for subsequent calls
 */
export async function fetchCommissionSettings(): Promise<CommissionSettings> {
  // Return cached settings if available
  if (cachedCommissionSettings) {
    return cachedCommissionSettings;
  }

  // Return existing fetch promise if in progress
  if (settingsFetchPromise) {
    return settingsFetchPromise;
  }

  // Start new fetch
  settingsFetchPromise = fetch('/api/reference-data/commission-settings')
    .then(async (response) => {
      if (!response.ok) {
        console.warn('Failed to fetch commission settings, using defaults');
        return DEFAULT_COMMISSION_SETTINGS;
      }
      const data = await response.json();
      cachedCommissionSettings = {
        platformCommissionRate: data.platformCommissionRate ?? DEFAULT_COMMISSION_SETTINGS.platformCommissionRate,
        stripeFeeRate: data.stripeFeeRate ?? DEFAULT_COMMISSION_SETTINGS.stripeFeeRate,
        stripeFeeFixed: data.stripeFeeFixed ?? DEFAULT_COMMISSION_SETTINGS.stripeFeeFixed,
      };
      return cachedCommissionSettings;
    })
    .catch((error) => {
      console.warn('Error fetching commission settings:', error);
      return DEFAULT_COMMISSION_SETTINGS;
    })
    .finally(() => {
      settingsFetchPromise = null;
    });

  return settingsFetchPromise;
}

/**
 * Get current commission settings (sync version)
 * Returns cached settings or defaults if not yet fetched
 */
export function getCommissionSettings(): CommissionSettings {
  return cachedCommissionSettings ?? DEFAULT_COMMISSION_SETTINGS;
}

/**
 * Clear cached commission settings (useful for testing or config refresh)
 */
export function clearCommissionSettingsCache(): void {
  cachedCommissionSettings = null;
  settingsFetchPromise = null;
}

// US State Tax Rates (2025 data from Tax Foundation)
// Source: https://taxfoundation.org/data/all/state/sales-tax-rates/
const US_STATE_TAX_RATES: Record<string, number> = {
  AL: 0.04,
  AK: 0.0,
  AZ: 0.056,
  AR: 0.065,
  CA: 0.0725,
  CO: 0.029,
  CT: 0.0635,
  DE: 0.0,
  FL: 0.06,
  GA: 0.04,
  HI: 0.04,
  ID: 0.06,
  IL: 0.0625,
  IN: 0.07,
  IA: 0.06,
  KS: 0.065,
  KY: 0.06,
  LA: 0.0445,
  ME: 0.055,
  MD: 0.06,
  MA: 0.0625,
  MI: 0.06,
  MN: 0.0688,
  MS: 0.07,
  MO: 0.0423,
  MT: 0.0,
  NE: 0.055,
  NV: 0.0685,
  NH: 0.0,
  NJ: 0.0663,
  NM: 0.0512,
  NY: 0.04,
  NC: 0.0475,
  ND: 0.05,
  OH: 0.0575,
  OK: 0.045,
  OR: 0.0,
  PA: 0.06,
  RI: 0.07,
  SC: 0.06,
  SD: 0.045,
  TN: 0.07,
  TX: 0.0625,
  UT: 0.0595,
  VT: 0.06,
  VA: 0.053,
  WA: 0.065,
  WV: 0.06,
  WI: 0.05,
  WY: 0.04,
  DC: 0.06,
};

// State name to code mapping for conversion
const STATE_NAME_TO_CODE: Record<string, string> = {
  ALABAMA: 'AL',
  ALASKA: 'AK',
  ARIZONA: 'AZ',
  ARKANSAS: 'AR',
  CALIFORNIA: 'CA',
  COLORADO: 'CO',
  CONNECTICUT: 'CT',
  DELAWARE: 'DE',
  FLORIDA: 'FL',
  GEORGIA: 'GA',
  HAWAII: 'HI',
  IDAHO: 'ID',
  ILLINOIS: 'IL',
  INDIANA: 'IN',
  IOWA: 'IA',
  KANSAS: 'KS',
  KENTUCKY: 'KY',
  LOUISIANA: 'LA',
  MAINE: 'ME',
  MARYLAND: 'MD',
  MASSACHUSETTS: 'MA',
  MICHIGAN: 'MI',
  MINNESOTA: 'MN',
  MISSISSIPPI: 'MS',
  MISSOURI: 'MO',
  MONTANA: 'MT',
  NEBRASKA: 'NE',
  NEVADA: 'NV',
  'NEW HAMPSHIRE': 'NH',
  'NEW JERSEY': 'NJ',
  'NEW MEXICO': 'NM',
  'NEW YORK': 'NY',
  'NORTH CAROLINA': 'NC',
  'NORTH DAKOTA': 'ND',
  OHIO: 'OH',
  OKLAHOMA: 'OK',
  OREGON: 'OR',
  PENNSYLVANIA: 'PA',
  'RHODE ISLAND': 'RI',
  'SOUTH CAROLINA': 'SC',
  'SOUTH DAKOTA': 'SD',
  TENNESSEE: 'TN',
  TEXAS: 'TX',
  UTAH: 'UT',
  VERMONT: 'VT',
  VIRGINIA: 'VA',
  WASHINGTON: 'WA',
  'WEST VIRGINIA': 'WV',
  WISCONSIN: 'WI',
  WYOMING: 'WY',
  'DISTRICT OF COLUMBIA': 'DC',
};

/**
 * Convert state name to two-letter code
 */
function normalizeStateCode(stateInput: string | undefined | null): string | null {
  if (!stateInput) return null;

  const normalized = stateInput.trim().toUpperCase();

  // If already a 2-letter code, return it
  if (normalized.length === 2 && US_STATE_TAX_RATES[normalized] !== undefined) {
    return normalized;
  }

  // Try to convert from full name
  return STATE_NAME_TO_CODE[normalized] || null;
}

/**
 * Get tax rate for a given state
 * Returns 0 for non-US or unknown states
 */
export function getStateTaxRate(
  state: string | undefined | null,
  country: string | undefined | null
): number {
  // Only apply tax for United States
  if (!country || country.toUpperCase() !== 'UNITED STATES') {
    return 0;
  }

  const stateCode = normalizeStateCode(state);
  if (!stateCode) {
    return 0;
  }

  return US_STATE_TAX_RATES[stateCode] ?? 0;
}

/**
 * Format tax rate as display string (e.g., "7.25%")
 */
export function formatTaxRateDisplay(taxRate: number): string {
  if (taxRate === 0) {
    return '0%';
  }
  return `${(taxRate * 100).toFixed(2).replace(/\.?0+$/, '')}%`;
}

/**
 * Calculate revenue breakdown preview
 *
 * @param ticketPrice - The ticket price in dollars/units
 * @param currency - The currency enum value
 * @param state - US state name or code (optional)
 * @param country - Country name (optional, defaults to checking for "United States")
 * @param settings - Commission settings (optional, uses cached/default if not provided)
 * @returns RevenueBreakdownDto or null if price is invalid
 */
export function calculateRevenueBreakdown(
  ticketPrice: number | undefined | null,
  currency: Currency,
  state?: string | null,
  country?: string | null,
  settings?: CommissionSettings
): RevenueBreakdownDto | null {
  // Validate input
  if (ticketPrice === undefined || ticketPrice === null || ticketPrice <= 0) {
    return null;
  }

  // Use provided settings or get cached/default settings
  const commissionSettings = settings ?? getCommissionSettings();

  // Get tax rate based on location
  const taxRate = getStateTaxRate(state, country);

  // Calculate breakdown using tax-inclusive formula
  const grossAmount = ticketPrice;

  // Sales Tax = Gross - (Gross / (1 + taxRate))
  // For tax-inclusive pricing: preTaxAmount = grossAmount / (1 + taxRate)
  const preTaxAmount = taxRate > 0 ? grossAmount / (1 + taxRate) : grossAmount;
  const salesTaxAmount = grossAmount - preTaxAmount;

  // Taxable amount (amount after removing sales tax)
  const taxableAmount = preTaxAmount;

  // Stripe fee: configured rate + fixed amount (default: 2.9% + $0.30)
  const stripeFeeAmount = taxableAmount * commissionSettings.stripeFeeRate + commissionSettings.stripeFeeFixed;

  // Platform commission: configured rate (default: 2%)
  const platformCommissionAmount = taxableAmount * commissionSettings.platformCommissionRate;

  // Organizer payout: Taxable - Stripe - Platform
  const organizerPayoutAmount = taxableAmount - stripeFeeAmount - platformCommissionAmount;

  // Get state code for jurisdiction display
  const stateCode = normalizeStateCode(state);
  const taxJurisdiction = stateCode && country?.toUpperCase() === 'UNITED STATES' ? stateCode : null;

  return {
    grossAmount: Math.round(grossAmount * 100) / 100,
    salesTaxAmount: Math.round(salesTaxAmount * 100) / 100,
    taxableAmount: Math.round(taxableAmount * 100) / 100,
    stripeFeeAmount: Math.round(stripeFeeAmount * 100) / 100,
    platformCommissionAmount: Math.round(platformCommissionAmount * 100) / 100,
    organizerPayoutAmount: Math.round(organizerPayoutAmount * 100) / 100,
    currency,
    salesTaxRate: taxRate,
    taxRateDisplay: formatTaxRateDisplay(taxRate),
    taxJurisdiction,
  };
}

/**
 * Check if payout is very low (warning threshold)
 * Returns true if payout is less than $1 (or equivalent)
 */
export function isPayoutWarningThreshold(breakdown: RevenueBreakdownDto): boolean {
  return breakdown.organizerPayoutAmount < 1;
}
