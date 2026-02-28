'use client';

import { useState } from 'react';
import { Heart, Plus, X } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';
import { Input } from '@/presentation/components/ui/Input';

interface DonationConfigFormProps {
  /** Whether donations are enabled */
  isEnabled: boolean;
  onEnabledChange: (enabled: boolean) => void;
  /** Suggested donation amounts (max 3) */
  suggestedAmounts: number[];
  onSuggestedAmountsChange: (amounts: number[]) => void;
  /** Whether donors can enter custom amounts */
  allowCustomAmount: boolean;
  onAllowCustomAmountChange: (allow: boolean) => void;
  /** Minimum donation amount (optional) */
  minAmount: number | null;
  onMinAmountChange: (amount: number | null) => void;
  /** Maximum donation amount (optional) */
  maxAmount: number | null;
  onMaxAmountChange: (amount: number | null) => void;
  /** Custom donation message (optional) */
  donationMessage: string;
  onDonationMessageChange: (message: string) => void;
  /** Whether to show donation summary publicly on event page */
  showDonationSummary: boolean;
  onShowDonationSummaryChange: (show: boolean) => void;
}

/**
 * Donation configuration section for event create/edit forms.
 * Follows the Organizer Contact toggle pattern.
 */
export function DonationConfigForm({
  isEnabled,
  onEnabledChange,
  suggestedAmounts,
  onSuggestedAmountsChange,
  allowCustomAmount,
  onAllowCustomAmountChange,
  minAmount,
  onMinAmountChange,
  maxAmount,
  onMaxAmountChange,
  donationMessage,
  onDonationMessageChange,
  showDonationSummary,
  onShowDonationSummaryChange,
}: DonationConfigFormProps) {
  const [newAmount, setNewAmount] = useState('');

  const handleAddAmount = () => {
    const parsed = parseFloat(newAmount);
    if (parsed > 0 && suggestedAmounts.length < 3 && !suggestedAmounts.includes(parsed)) {
      onSuggestedAmountsChange([...suggestedAmounts, parsed].sort((a, b) => a - b));
      setNewAmount('');
    }
  };

  const handleRemoveAmount = (amount: number) => {
    onSuggestedAmountsChange(suggestedAmounts.filter((a) => a !== amount));
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center gap-2">
          <Heart className="h-5 w-5" style={{ color: '#FF7900' }} />
          <CardTitle style={{ color: '#8B1538' }}>Donations (Optional)</CardTitle>
        </div>
        <CardDescription>
          Allow attendees and visitors to donate to support your event
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* Enable Toggle */}
        <div className="flex items-start space-x-3">
          <input
            type="checkbox"
            id="enableDonations"
            checked={isEnabled}
            onChange={(e) => onEnabledChange(e.target.checked)}
            className="mt-1 h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
          />
          <label htmlFor="enableDonations" className="text-sm font-medium text-gray-700">
            Enable donations for this event
          </label>
        </div>

        {isEnabled && (
          <div className="ml-7 space-y-4 p-4 border border-gray-200 rounded-lg bg-gray-50">
            {/* Donation Message */}
            <div className="space-y-2">
              <label htmlFor="donationMessage" className="block text-sm font-medium text-gray-700">
                Donation Message (optional)
              </label>
              <textarea
                id="donationMessage"
                value={donationMessage}
                onChange={(e) => onDonationMessageChange(e.target.value)}
                placeholder="Help us make this event special! Every contribution counts..."
                maxLength={500}
                rows={2}
                className="w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
              <p className="text-xs text-gray-500">{donationMessage.length}/500 characters</p>
            </div>

            {/* Suggested Amounts */}
            <div className="space-y-2">
              <label className="block text-sm font-medium text-gray-700">
                Suggested Amounts (up to 3)
              </label>
              <div className="flex flex-wrap gap-2 mb-2">
                {suggestedAmounts.map((amount) => (
                  <span
                    key={amount}
                    className="inline-flex items-center gap-1 px-3 py-1 rounded-full text-sm font-medium bg-rose-100 text-rose-700 border border-rose-200"
                  >
                    ${amount.toFixed(2)}
                    <button
                      type="button"
                      onClick={() => handleRemoveAmount(amount)}
                      className="ml-1 text-rose-500 hover:text-rose-700"
                    >
                      <X className="h-3 w-3" />
                    </button>
                  </span>
                ))}
              </div>
              {suggestedAmounts.length < 3 && (
                <div className="flex gap-2">
                  <div className="relative flex-1">
                    <span className="absolute left-3 top-1/2 -translate-y-1/2 text-neutral-500 text-sm">$</span>
                    <Input
                      type="number"
                      min="0.50"
                      step="0.01"
                      value={newAmount}
                      onChange={(e) => setNewAmount(e.target.value)}
                      onKeyDown={(e) => e.key === 'Enter' && (e.preventDefault(), handleAddAmount())}
                      placeholder="e.g. 25.00"
                      className="pl-7 text-sm"
                    />
                  </div>
                  <button
                    type="button"
                    onClick={handleAddAmount}
                    disabled={!newAmount || parseFloat(newAmount) <= 0}
                    className="px-3 py-2 rounded-md text-sm font-medium text-white disabled:opacity-50"
                    style={{ background: '#FF7900' }}
                  >
                    <Plus className="h-4 w-4" />
                  </button>
                </div>
              )}
            </div>

            {/* Allow Custom Amount */}
            <div className="flex items-start space-x-3">
              <input
                type="checkbox"
                id="allowCustomAmount"
                checked={allowCustomAmount}
                onChange={(e) => onAllowCustomAmountChange(e.target.checked)}
                className="mt-1 h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
              />
              <label htmlFor="allowCustomAmount" className="text-sm font-medium text-gray-700">
                Allow donors to enter a custom amount
              </label>
            </div>

            {/* Min / Max Amounts */}
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <label htmlFor="donationMinAmount" className="block text-sm font-medium text-gray-700">
                  Minimum Amount (optional)
                </label>
                <div className="relative">
                  <span className="absolute left-3 top-1/2 -translate-y-1/2 text-neutral-500 text-sm">$</span>
                  <Input
                    id="donationMinAmount"
                    type="number"
                    min="0.50"
                    step="0.01"
                    value={minAmount ?? ''}
                    onChange={(e) => {
                      const val = e.target.value;
                      onMinAmountChange(val ? parseFloat(val) : null);
                    }}
                    placeholder="0.50"
                    className="pl-7 text-sm"
                  />
                </div>
              </div>
              <div className="space-y-2">
                <label htmlFor="donationMaxAmount" className="block text-sm font-medium text-gray-700">
                  Maximum Amount (optional)
                </label>
                <div className="relative">
                  <span className="absolute left-3 top-1/2 -translate-y-1/2 text-neutral-500 text-sm">$</span>
                  <Input
                    id="donationMaxAmount"
                    type="number"
                    min="0.50"
                    step="0.01"
                    value={maxAmount ?? ''}
                    onChange={(e) => {
                      const val = e.target.value;
                      onMaxAmountChange(val ? parseFloat(val) : null);
                    }}
                    placeholder="1000.00"
                    className="pl-7 text-sm"
                  />
                </div>
              </div>
            </div>

            {/* Show Donation Summary Toggle */}
            <div className="flex items-start space-x-3">
              <input
                type="checkbox"
                id="showDonationSummary"
                checked={showDonationSummary}
                onChange={(e) => onShowDonationSummaryChange(e.target.checked)}
                className="mt-1 h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
              />
              <div>
                <label htmlFor="showDonationSummary" className="text-sm font-medium text-gray-700">
                  Show donation summary on event page
                </label>
                <p className="text-xs text-gray-500 mt-0.5">
                  Display total donation count and amount raised publicly to all visitors
                </p>
              </div>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
