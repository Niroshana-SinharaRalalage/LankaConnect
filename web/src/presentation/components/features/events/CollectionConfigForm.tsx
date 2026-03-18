'use client';

import { useState } from 'react';
import { Wallet, Plus, X } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';
import { Input } from '@/presentation/components/ui/Input';

interface CollectionConfigFormProps {
  /** Whether collections (event fund) are enabled */
  isEnabled: boolean;
  onEnabledChange: (enabled: boolean) => void;
  /** Fundraising goal amount (optional, null = open-ended) */
  goalAmount: number | null;
  onGoalAmountChange: (amount: number | null) => void;
  /** Whether to show progress toward goal publicly */
  showProgress: boolean;
  onShowProgressChange: (show: boolean) => void;
  /** Suggested contribution amounts (max 5) */
  suggestedAmounts: number[];
  onSuggestedAmountsChange: (amounts: number[]) => void;
  /** Whether contributors can enter custom amounts */
  allowCustomAmount: boolean;
  onAllowCustomAmountChange: (allow: boolean) => void;
  /** Minimum contribution amount (optional) */
  minAmount: number | null;
  onMinAmountChange: (amount: number | null) => void;
  /** Maximum contribution amount (optional) */
  maxAmount: number | null;
  onMaxAmountChange: (amount: number | null) => void;
  /** Custom collection message (optional) */
  collectionMessage: string;
  onCollectionMessageChange: (message: string) => void;
  /** Whether to show contributor count publicly */
  showContributorCount: boolean;
  onShowContributorCountChange: (show: boolean) => void;
}

/**
 * Collection (Event Fund) configuration section for event create/edit forms.
 * Follows the DonationConfigForm pattern.
 */
export function CollectionConfigForm({
  isEnabled,
  onEnabledChange,
  goalAmount,
  onGoalAmountChange,
  showProgress,
  onShowProgressChange,
  suggestedAmounts,
  onSuggestedAmountsChange,
  allowCustomAmount,
  onAllowCustomAmountChange,
  minAmount,
  onMinAmountChange,
  maxAmount,
  onMaxAmountChange,
  collectionMessage,
  onCollectionMessageChange,
  showContributorCount,
  onShowContributorCountChange,
}: CollectionConfigFormProps) {
  const [newAmount, setNewAmount] = useState('');

  const handleAddAmount = () => {
    const parsed = parseFloat(newAmount);
    if (parsed >= 1.0 && suggestedAmounts.length < 5 && !suggestedAmounts.includes(parsed)) {
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
          <Wallet className="h-5 w-5" style={{ color: '#FF7900' }} />
          <CardTitle style={{ color: '#8B1538' }}>Event Fund / Collections (Optional)</CardTitle>
        </div>
        <CardDescription>
          Set up a fundraising collection to gather contributions for your event
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* Enable Toggle */}
        <div className="flex items-start space-x-3">
          <input
            type="checkbox"
            id="enableCollections"
            checked={isEnabled}
            onChange={(e) => onEnabledChange(e.target.checked)}
            className="mt-1 h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
          />
          <label htmlFor="enableCollections" className="text-sm font-medium text-gray-700">
            Enable event fund collections for this event
          </label>
        </div>

        {isEnabled && (
          <div className="ml-7 space-y-4 p-4 border border-gray-200 rounded-lg bg-gray-50">
            {/* Goal Amount */}
            <div className="space-y-2">
              <label htmlFor="collectionGoalAmount" className="block text-sm font-medium text-gray-700">
                Fundraising Goal (optional)
              </label>
              <div className="relative max-w-xs">
                <span className="absolute left-3 top-1/2 -translate-y-1/2 text-neutral-500 text-sm">$</span>
                <Input
                  id="collectionGoalAmount"
                  type="number"
                  min="1"
                  step="0.01"
                  value={goalAmount ?? ''}
                  onChange={(e) => {
                    const val = e.target.value;
                    onGoalAmountChange(val ? parseFloat(val) : null);
                  }}
                  placeholder="e.g. 5000.00"
                  className="pl-7 text-sm"
                />
              </div>
              <p className="text-xs text-gray-500">Leave empty for open-ended collections with no target</p>
            </div>

            {/* Show Progress */}
            <div className="flex items-start space-x-3">
              <input
                type="checkbox"
                id="collectionShowProgress"
                checked={showProgress}
                onChange={(e) => onShowProgressChange(e.target.checked)}
                className="mt-1 h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
              />
              <div>
                <label htmlFor="collectionShowProgress" className="text-sm font-medium text-gray-700">
                  Display progress bar on event page
                </label>
                <p className="text-xs text-gray-500 mt-0.5">
                  Show how much has been raised toward the goal
                </p>
              </div>
            </div>

            {/* Collection Message */}
            <div className="space-y-2">
              <label htmlFor="collectionMessage" className="block text-sm font-medium text-gray-700">
                Collection Message (optional)
              </label>
              <textarea
                id="collectionMessage"
                value={collectionMessage}
                onChange={(e) => onCollectionMessageChange(e.target.value)}
                placeholder="Help us fund this event! Your contribution makes a difference..."
                maxLength={500}
                rows={2}
                className="w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
              <p className="text-xs text-gray-500">{collectionMessage.length}/500 characters</p>
            </div>

            {/* Suggested Amounts */}
            <div className="space-y-2">
              <label className="block text-sm font-medium text-gray-700">
                Suggested Amounts (up to 5)
              </label>
              <div className="flex flex-wrap gap-2 mb-2">
                {suggestedAmounts.map((amount) => (
                  <span
                    key={amount}
                    className="inline-flex items-center gap-1 px-3 py-1 rounded-full text-sm font-medium bg-violet-100 text-violet-700 border border-violet-200"
                  >
                    ${amount.toFixed(2)}
                    <button
                      type="button"
                      onClick={() => handleRemoveAmount(amount)}
                      className="ml-1 text-violet-500 hover:text-violet-700"
                    >
                      <X className="h-3 w-3" />
                    </button>
                  </span>
                ))}
              </div>
              {suggestedAmounts.length < 5 && (
                <div className="flex gap-2">
                  <div className="relative flex-1">
                    <span className="absolute left-3 top-1/2 -translate-y-1/2 text-neutral-500 text-sm">$</span>
                    <Input
                      type="number"
                      min="1.00"
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
                    disabled={!newAmount || parseFloat(newAmount) < 1.0}
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
                id="collectionAllowCustom"
                checked={allowCustomAmount}
                onChange={(e) => onAllowCustomAmountChange(e.target.checked)}
                className="mt-1 h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
              />
              <label htmlFor="collectionAllowCustom" className="text-sm font-medium text-gray-700">
                Allow contributors to enter a custom amount
              </label>
            </div>

            {/* Min / Max Amounts */}
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <label htmlFor="collectionMinAmount" className="block text-sm font-medium text-gray-700">
                  Minimum Amount (optional)
                </label>
                <div className="relative">
                  <span className="absolute left-3 top-1/2 -translate-y-1/2 text-neutral-500 text-sm">$</span>
                  <Input
                    id="collectionMinAmount"
                    type="number"
                    min="1.00"
                    step="0.01"
                    value={minAmount ?? ''}
                    onChange={(e) => {
                      const val = e.target.value;
                      onMinAmountChange(val ? parseFloat(val) : null);
                    }}
                    placeholder="1.00"
                    className="pl-7 text-sm"
                  />
                </div>
              </div>
              <div className="space-y-2">
                <label htmlFor="collectionMaxAmount" className="block text-sm font-medium text-gray-700">
                  Maximum Amount (optional)
                </label>
                <div className="relative">
                  <span className="absolute left-3 top-1/2 -translate-y-1/2 text-neutral-500 text-sm">$</span>
                  <Input
                    id="collectionMaxAmount"
                    type="number"
                    min="1.00"
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

            {/* Show Contributor Count */}
            <div className="flex items-start space-x-3">
              <input
                type="checkbox"
                id="showContributorCount"
                checked={showContributorCount}
                onChange={(e) => onShowContributorCountChange(e.target.checked)}
                className="mt-1 h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
              />
              <div>
                <label htmlFor="showContributorCount" className="text-sm font-medium text-gray-700">
                  Show number of contributors publicly
                </label>
                <p className="text-xs text-gray-500 mt-0.5">
                  Display how many people have contributed on the event page
                </p>
              </div>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
