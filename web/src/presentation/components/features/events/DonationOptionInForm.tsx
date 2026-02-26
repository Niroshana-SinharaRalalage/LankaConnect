'use client';

import { useState } from 'react';
import { Heart } from 'lucide-react';
import { Input } from '@/presentation/components/ui/Input';
import type { DonationConfigurationDto } from '@/infrastructure/api/types/events.types';

interface DonationOptionInFormProps {
  donationConfig: DonationConfigurationDto;
  onDonationChange: (amount: number | null) => void;
}

/**
 * Lightweight donation add-on shown inside the registration form.
 * Allows donors to add a donation during event registration (combined checkout).
 */
export function DonationOptionInForm({ donationConfig, onDonationChange }: DonationOptionInFormProps) {
  const [selectedAmount, setSelectedAmount] = useState<number | null>(null);
  const [customAmount, setCustomAmount] = useState('');
  const [isCustom, setIsCustom] = useState(false);

  const handleSelectAmount = (amount: number) => {
    if (selectedAmount === amount && !isCustom) {
      // Deselect
      setSelectedAmount(null);
      onDonationChange(null);
    } else {
      setSelectedAmount(amount);
      setIsCustom(false);
      setCustomAmount('');
      onDonationChange(amount);
    }
  };

  const handleCustomAmountChange = (value: string) => {
    setCustomAmount(value);
    setIsCustom(true);
    setSelectedAmount(null);
    const parsed = parseFloat(value);
    onDonationChange(parsed > 0 ? parsed : null);
  };

  return (
    <div className="rounded-lg border border-rose-200 bg-rose-50/50 p-4">
      <div className="flex items-center gap-2 mb-3">
        <Heart className="h-4 w-4 text-rose-500" />
        <span className="text-sm font-medium text-neutral-800">
          Add a donation (optional)
        </span>
      </div>

      {donationConfig.donationMessage && (
        <p className="text-xs text-neutral-600 mb-3">{donationConfig.donationMessage}</p>
      )}

      {/* Suggested Amounts */}
      {donationConfig.suggestedAmounts.length > 0 && (
        <div className="flex gap-2 flex-wrap mb-3">
          {donationConfig.suggestedAmounts.map((amount) => (
            <button
              key={amount}
              type="button"
              onClick={() => handleSelectAmount(amount)}
              className={`px-3 py-1.5 rounded-full text-xs font-medium border transition-colors ${
                selectedAmount === amount && !isCustom
                  ? 'bg-rose-500 text-white border-rose-500'
                  : 'bg-white text-neutral-700 border-neutral-300 hover:border-rose-300'
              }`}
            >
              ${amount.toFixed(2)}
            </button>
          ))}
        </div>
      )}

      {/* Custom Amount */}
      {donationConfig.allowCustomAmount && (
        <div className="relative">
          <span className="absolute left-3 top-1/2 -translate-y-1/2 text-neutral-500 text-sm">$</span>
          <Input
            type="number"
            min="1"
            step="0.01"
            value={customAmount}
            onChange={(e) => handleCustomAmountChange(e.target.value)}
            placeholder="Custom amount"
            className="pl-7 text-sm h-9"
          />
        </div>
      )}
    </div>
  );
}
