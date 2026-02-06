/**
 * Phase 6A.X: Revenue Breakdown Table Component
 *
 * Displays detailed fee breakdown for paid events showing:
 * - Ticket Price (what buyer pays)
 * - Sales Tax (state-based)
 * - Stripe Fee (2.9% + $0.30)
 * - Platform Commission (2%)
 * - Organizer Payout (net amount)
 */

import React from 'react';
import { RevenueBreakdownDto, Currency } from '@/infrastructure/api/types/events.types';
import { Info } from 'lucide-react';

interface RevenueBreakdownTableProps {
  breakdown: RevenueBreakdownDto;
  /** Whether to show compact version (fewer details) */
  compact?: boolean;
  /** Optional title override */
  title?: string;
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
 * Revenue Breakdown Table - shows detailed fee structure for paid events
 */
export function RevenueBreakdownTable({
  breakdown,
  compact = false,
  title = 'Revenue Breakdown (per ticket)',
}: RevenueBreakdownTableProps) {
  const {
    grossAmount,
    salesTaxAmount,
    taxableAmount,
    stripeFeeAmount,
    platformCommissionAmount,
    organizerPayoutAmount,
    currency,
    taxRateDisplay,
  } = breakdown;

  return (
    <div className="bg-neutral-50 rounded-lg p-4 border border-neutral-200">
      <div className="flex items-center gap-2 mb-3">
        <h4 className="text-sm font-semibold text-neutral-700">{title}</h4>
        <div className="group relative">
          <Info className="h-4 w-4 text-neutral-400 cursor-help" />
          <div className="absolute left-0 bottom-full mb-2 hidden group-hover:block z-10 w-64 p-2 bg-neutral-900 text-white text-xs rounded shadow-lg">
            Sales tax is based on event location. Stripe charges 2.9% + $0.30 per transaction.
            LankaConnect takes 2% platform commission.
          </div>
        </div>
      </div>

      <div className="space-y-2 text-sm">
        {/* Ticket Price (Gross) */}
        <div className="flex justify-between items-center">
          <span className="text-neutral-600">Ticket Price</span>
          <span className="font-medium text-neutral-900">{formatCurrency(grossAmount, currency)}</span>
        </div>

        {/* Sales Tax */}
        {salesTaxAmount > 0 && (
          <div className="flex justify-between items-center text-amber-700">
            <span>Sales Tax ({taxRateDisplay})</span>
            <span className="font-medium">-{formatCurrency(salesTaxAmount, currency)}</span>
          </div>
        )}

        {!compact && (
          <>
            {/* Taxable Amount (subtotal after tax) */}
            {salesTaxAmount > 0 && (
              <div className="flex justify-between items-center text-neutral-500 text-xs border-t border-neutral-200 pt-2">
                <span>Taxable Amount</span>
                <span>{formatCurrency(taxableAmount, currency)}</span>
              </div>
            )}
          </>
        )}

        {/* Stripe Fee */}
        <div className="flex justify-between items-center text-red-600">
          <span>Stripe Fee (2.9% + $0.30)</span>
          <span className="font-medium">-{formatCurrency(stripeFeeAmount, currency)}</span>
        </div>

        {/* Platform Commission */}
        <div className="flex justify-between items-center text-red-600">
          <span>Platform Commission (2%)</span>
          <span className="font-medium">-{formatCurrency(platformCommissionAmount, currency)}</span>
        </div>

        {/* Divider */}
        <div className="border-t-2 border-neutral-300 my-2" />

        {/* Organizer Payout */}
        <div className="flex justify-between items-center">
          <span className="text-green-700 font-semibold">Your Payout</span>
          <span className="text-lg font-bold text-green-700">
            {formatCurrency(organizerPayoutAmount, currency)}
          </span>
        </div>
      </div>

      {!compact && (
        <p className="text-[10px] text-neutral-400 mt-3">
          * Sales tax is collected and remitted based on event location
        </p>
      )}
    </div>
  );
}

/**
 * Simplified version for inline display (e.g., in event creation form)
 */
export function RevenueBreakdownInline({
  breakdown,
}: {
  breakdown: RevenueBreakdownDto;
}) {
  return (
    <div className="text-sm text-neutral-600 bg-green-50 border border-green-200 rounded-md p-3">
      <div className="flex justify-between items-center">
        <span>
          After fees ({breakdown.taxRateDisplay} tax + Stripe + 2% platform)
        </span>
        <span className="font-bold text-green-700">
          You&apos;ll receive: {formatCurrency(breakdown.organizerPayoutAmount, breakdown.currency)}
        </span>
      </div>
    </div>
  );
}

export default RevenueBreakdownTable;
