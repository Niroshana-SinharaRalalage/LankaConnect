/**
 * Phase 6A.X: Revenue Breakdown Preview Component
 *
 * A compact, inline preview of revenue breakdown for event creation/edit forms.
 * Shows the organizer what they will receive after fees and taxes.
 *
 * Features:
 * - Real-time calculation based on ticket price and location
 * - Shows detailed breakdown on hover/click
 * - Warning message for very low payouts
 * - Fetches fee rates from backend configuration
 */

'use client';

import React, { useMemo, useState, useEffect } from 'react';
import { Info, AlertTriangle, ChevronDown, ChevronUp } from 'lucide-react';
import { Currency } from '@/infrastructure/api/types/events.types';
import { useCommissionSettings } from '@/infrastructure/api/hooks/useReferenceData';
import {
  calculateRevenueBreakdown,
  isPayoutWarningThreshold,
  type CommissionSettings,
} from '@/presentation/lib/utils/revenue-calculator';

interface RevenueBreakdownPreviewProps {
  /** Ticket price amount */
  ticketPrice: number | undefined | null;
  /** Currency for the price */
  currency: Currency;
  /** US state name or code (for tax calculation) */
  state?: string | null;
  /** Country (tax only applies if "United States") */
  country?: string | null;
  /** Label for the price type (e.g., "Adult", "Child", "Ticket") */
  priceLabel?: string;
  /** Whether to show compact version */
  compact?: boolean;
}

/**
 * Format currency amount with symbol
 */
function formatCurrency(amount: number, currency: Currency): string {
  const symbols: Record<Currency, string> = {
    [Currency.USD]: '$',
    [Currency.LKR]: 'Rs.',
    [Currency.GBP]: '\u00A3',
    [Currency.EUR]: '\u20AC',
    [Currency.CAD]: 'C$',
    [Currency.AUD]: 'A$',
  };
  const symbol = symbols[currency] || '$';
  return `${symbol}${amount.toFixed(2)}`;
}

/**
 * Format percentage rate for display
 */
function formatRate(rate: number): string {
  return `${(rate * 100).toFixed(1).replace(/\.0$/, '')}%`;
}

/**
 * Revenue Breakdown Preview - shows inline preview with expandable details
 */
export function RevenueBreakdownPreview({
  ticketPrice,
  currency,
  state,
  country,
  priceLabel = 'ticket',
  compact = false,
}: RevenueBreakdownPreviewProps) {
  const [expanded, setExpanded] = useState(false);

  // Fetch commission settings from backend configuration
  const { data: commissionSettings } = useCommissionSettings();

  // Map CommissionSettingsDto to CommissionSettings type
  const settings: CommissionSettings | undefined = useMemo(() => {
    if (!commissionSettings) return undefined;
    return {
      platformCommissionRate: commissionSettings.platformCommissionRate,
      stripeFeeRate: commissionSettings.stripeFeeRate,
      stripeFeeFixed: commissionSettings.stripeFeeFixed,
    };
  }, [commissionSettings]);

  // Calculate breakdown using memoization with fetched settings
  const breakdown = useMemo(
    () => calculateRevenueBreakdown(ticketPrice, currency, state, country, settings),
    [ticketPrice, currency, state, country, settings]
  );

  // Don't render if no valid breakdown
  if (!breakdown) {
    return null;
  }

  const showWarning = isPayoutWarningThreshold(breakdown);
  const hasTax = breakdown.salesTaxAmount > 0;

  // Get actual rates for display (use defaults if not loaded yet)
  const stripeFeeRate = settings?.stripeFeeRate ?? 0.029;
  const stripeFeeFixed = settings?.stripeFeeFixed ?? 0.30;
  const platformRate = settings?.platformCommissionRate ?? 0.02;

  // Compact version - just shows payout
  if (compact) {
    return (
      <div className="mt-2 p-2 bg-green-50 border border-green-200 rounded text-xs">
        <div className="flex justify-between items-center">
          <span className="text-neutral-600">
            After fees{hasTax ? ` (${breakdown.taxRateDisplay} tax)` : ''}
          </span>
          <span className="font-bold text-green-700">
            You&apos;ll receive: {formatCurrency(breakdown.organizerPayoutAmount, currency)}
          </span>
        </div>
      </div>
    );
  }

  return (
    <div className="mt-2 rounded border bg-neutral-50 border-neutral-200 text-xs overflow-hidden">
      {/* Header - always visible */}
      <button
        type="button"
        onClick={() => setExpanded(!expanded)}
        className="w-full p-2 flex justify-between items-center hover:bg-neutral-100 transition-colors"
      >
        <div className="flex items-center gap-2">
          <Info className="h-3.5 w-3.5 text-neutral-400" />
          <span className="text-neutral-600">
            Revenue breakdown per {priceLabel}
          </span>
        </div>
        <div className="flex items-center gap-2">
          <span className="font-bold text-green-700">
            Payout: {formatCurrency(breakdown.organizerPayoutAmount, currency)}
          </span>
          {expanded ? (
            <ChevronUp className="h-3.5 w-3.5 text-neutral-400" />
          ) : (
            <ChevronDown className="h-3.5 w-3.5 text-neutral-400" />
          )}
        </div>
      </button>

      {/* Expanded details */}
      {expanded && (
        <div className="p-3 pt-0 space-y-1.5 border-t border-neutral-200 bg-white">
          {/* Ticket Price */}
          <div className="flex justify-between items-center pt-2">
            <span className="text-neutral-500">Ticket Price</span>
            <span className="font-medium text-neutral-900">
              {formatCurrency(breakdown.grossAmount, currency)}
            </span>
          </div>

          {/* Sales Tax (if applicable) */}
          {hasTax && (
            <div className="flex justify-between items-center text-amber-700">
              <span>Sales Tax ({breakdown.taxRateDisplay})</span>
              <span className="font-medium">
                -{formatCurrency(breakdown.salesTaxAmount, currency)}
              </span>
            </div>
          )}

          {/* Stripe Fee - displays actual configured rates */}
          <div className="flex justify-between items-center text-red-600">
            <span>Stripe Fee ({formatRate(stripeFeeRate)} + ${stripeFeeFixed.toFixed(2)})</span>
            <span className="font-medium">
              -{formatCurrency(breakdown.stripeFeeAmount, currency)}
            </span>
          </div>

          {/* Platform Commission - displays actual configured rate */}
          <div className="flex justify-between items-center text-red-600">
            <span>Platform Commission ({formatRate(platformRate)})</span>
            <span className="font-medium">
              -{formatCurrency(breakdown.platformCommissionAmount, currency)}
            </span>
          </div>

          {/* Divider */}
          <div className="border-t border-neutral-200 my-1" />

          {/* Organizer Payout */}
          <div className="flex justify-between items-center">
            <span className="font-semibold text-green-700">Your Payout</span>
            <span className="text-base font-bold text-green-700">
              {formatCurrency(breakdown.organizerPayoutAmount, currency)}
            </span>
          </div>

          {/* Warning for low payout */}
          {showWarning && (
            <div className="flex items-start gap-2 mt-2 p-2 bg-amber-50 border border-amber-200 rounded text-amber-800">
              <AlertTriangle className="h-4 w-4 flex-shrink-0 mt-0.5" />
              <span>
                Low payout warning: Your payout is less than $1 per {priceLabel}. Consider a higher price.
              </span>
            </div>
          )}

          {/* Tax note */}
          {!hasTax && country?.toUpperCase() === 'UNITED STATES' && (
            <p className="text-[10px] text-neutral-400 mt-2">
              * Enter a US state to see applicable sales tax
            </p>
          )}

          {!hasTax && (!country || country.toUpperCase() !== 'UNITED STATES') && (
            <p className="text-[10px] text-neutral-400 mt-2">
              * Sales tax only applies to US events
            </p>
          )}
        </div>
      )}
    </div>
  );
}

export default RevenueBreakdownPreview;
