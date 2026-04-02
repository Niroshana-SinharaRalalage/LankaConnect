'use client';

import { useState } from 'react';
import { Wallet } from 'lucide-react';
import { Input } from '@/presentation/components/ui/Input';
import type { CollectionConfigurationDto } from '@/infrastructure/api/types/events.types';

interface CollectionOptionInFormProps {
  collectionConfig: CollectionConfigurationDto;
  onCollectionChange: (amount: number | null, notes: string | null) => void;
}

/**
 * Lightweight collection contribution option shown inside the registration form.
 * Allows attendees to add a collection contribution during event registration (combined checkout).
 * Phase 6A.137E: Follows the DonationOptionInForm pattern.
 */
export function CollectionOptionInForm({ collectionConfig, onCollectionChange }: CollectionOptionInFormProps) {
  const [selectedAmount, setSelectedAmount] = useState<number | null>(null);
  const [customAmount, setCustomAmount] = useState('');
  const [isCustom, setIsCustom] = useState(false);
  const [notes, setNotes] = useState('');

  const handleSelectAmount = (amount: number) => {
    if (selectedAmount === amount && !isCustom) {
      // Deselect
      setSelectedAmount(null);
      onCollectionChange(null, notes || null);
    } else {
      setSelectedAmount(amount);
      setIsCustom(false);
      setCustomAmount('');
      onCollectionChange(amount, notes || null);
    }
  };

  const handleCustomAmountChange = (value: string) => {
    setCustomAmount(value);
    setIsCustom(true);
    setSelectedAmount(null);
    const parsed = parseFloat(value);
    const effectiveAmount = parsed > 0 ? parsed : null;

    // Validate against min/max
    if (effectiveAmount !== null) {
      if (collectionConfig.minAmount && effectiveAmount < collectionConfig.minAmount) {
        onCollectionChange(null, notes || null);
        return;
      }
      if (collectionConfig.maxAmount && effectiveAmount > collectionConfig.maxAmount) {
        onCollectionChange(null, notes || null);
        return;
      }
    }

    onCollectionChange(effectiveAmount, notes || null);
  };

  const handleNotesChange = (value: string) => {
    setNotes(value);
    const effectiveAmount = isCustom
      ? (parseFloat(customAmount) > 0 ? parseFloat(customAmount) : null)
      : selectedAmount;
    onCollectionChange(effectiveAmount, value || null);
  };

  return (
    <div className="rounded-lg border border-blue-200 bg-blue-50/50 p-4">
      <div className="flex items-center gap-2 mb-3">
        <Wallet className="h-4 w-4 text-blue-500" />
        <span className="text-sm font-medium text-neutral-800">
          Contribute to collection (optional)
        </span>
      </div>

      {collectionConfig.collectionMessage && (
        <p className="text-xs text-neutral-600 mb-3">{collectionConfig.collectionMessage}</p>
      )}

      {/* Suggested Amounts */}
      {collectionConfig.suggestedAmounts.length > 0 && (
        <div className="flex gap-2 flex-wrap mb-3">
          {collectionConfig.suggestedAmounts.map((amount) => (
            <button
              key={amount}
              type="button"
              onClick={() => handleSelectAmount(amount)}
              className={`px-3 py-1.5 rounded-full text-xs font-medium border transition-colors ${
                selectedAmount === amount && !isCustom
                  ? 'bg-blue-500 text-white border-blue-500'
                  : 'bg-white text-neutral-700 border-neutral-300 hover:border-blue-300'
              }`}
            >
              ${amount.toFixed(2)}
            </button>
          ))}
        </div>
      )}

      {/* Custom Amount */}
      {collectionConfig.allowCustomAmount && (
        <div className="relative mb-3">
          <span className="absolute left-3 top-1/2 -translate-y-1/2 text-neutral-500 text-sm">$</span>
          <Input
            type="number"
            min={collectionConfig.minAmount || 1}
            max={collectionConfig.maxAmount || undefined}
            step="0.01"
            value={customAmount}
            onChange={(e) => handleCustomAmountChange(e.target.value)}
            placeholder={`Custom amount${collectionConfig.minAmount ? ` (min $${collectionConfig.minAmount})` : ''}`}
            className="pl-7 text-sm h-9"
          />
        </div>
      )}

      {/* Notes */}
      <Input
        type="text"
        value={notes}
        onChange={(e) => handleNotesChange(e.target.value)}
        placeholder="Add a note (optional)"
        className="text-sm h-9"
        maxLength={200}
      />
    </div>
  );
}
