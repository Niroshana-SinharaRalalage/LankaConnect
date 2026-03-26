'use client';

import { useState } from 'react';
import { Award } from 'lucide-react';
import { Input } from '@/presentation/components/ui/Input';
import type { SponsorConfigurationDto } from '@/infrastructure/api/types/events.types';

interface SponsorOptionInFormProps {
  sponsorConfig: SponsorConfigurationDto;
  onSponsorChange: (amount: number | null, organization: string | null, notes: string | null) => void;
}

/**
 * Lightweight money sponsorship option shown inside the registration form.
 * Allows attendees to add a monetary sponsorship during event registration (combined checkout).
 * Phase 6A.137E: Follows the DonationOptionInForm pattern.
 * Note: Only money sponsors can be bundled. Item sponsors remain standalone.
 */
export function SponsorOptionInForm({ sponsorConfig, onSponsorChange }: SponsorOptionInFormProps) {
  const [amount, setAmount] = useState('');
  const [organization, setOrganization] = useState('');
  const [notes, setNotes] = useState('');

  const handleAmountChange = (value: string) => {
    setAmount(value);
    const parsed = parseFloat(value);
    const effectiveAmount = parsed > 0 ? parsed : null;

    // Validate against min
    if (effectiveAmount !== null && sponsorConfig.minSponsorAmount && effectiveAmount < sponsorConfig.minSponsorAmount) {
      onSponsorChange(null, organization || null, notes || null);
      return;
    }

    onSponsorChange(effectiveAmount, organization || null, notes || null);
  };

  const handleOrganizationChange = (value: string) => {
    setOrganization(value);
    const parsed = parseFloat(amount);
    const effectiveAmount = parsed > 0 ? parsed : null;
    onSponsorChange(effectiveAmount, value || null, notes || null);
  };

  const handleNotesChange = (value: string) => {
    setNotes(value);
    const parsed = parseFloat(amount);
    const effectiveAmount = parsed > 0 ? parsed : null;
    onSponsorChange(effectiveAmount, organization || null, value || null);
  };

  return (
    <div className="rounded-lg border border-amber-200 bg-amber-50/50 p-4">
      <div className="flex items-center gap-2 mb-3">
        <Award className="h-4 w-4 text-amber-500" />
        <span className="text-sm font-medium text-neutral-800">
          Become a sponsor (optional)
        </span>
      </div>

      {sponsorConfig.sponsorMessage && (
        <p className="text-xs text-neutral-600 mb-3">{sponsorConfig.sponsorMessage}</p>
      )}

      {/* Sponsor Amount */}
      <div className="space-y-2">
        <div className="relative">
          <span className="absolute left-3 top-1/2 -translate-y-1/2 text-neutral-500 text-sm">$</span>
          <Input
            type="number"
            min={sponsorConfig.minSponsorAmount || 1}
            step="0.01"
            value={amount}
            onChange={(e) => handleAmountChange(e.target.value)}
            placeholder={`Sponsorship amount${sponsorConfig.minSponsorAmount ? ` (min $${sponsorConfig.minSponsorAmount})` : ''}`}
            className="pl-7 text-sm h-9"
          />
        </div>

        {/* Organization (optional) */}
        <Input
          type="text"
          value={organization}
          onChange={(e) => handleOrganizationChange(e.target.value)}
          placeholder="Organization name (optional)"
          className="text-sm h-9"
          maxLength={200}
        />

        {/* Notes (optional) */}
        <Input
          type="text"
          value={notes}
          onChange={(e) => handleNotesChange(e.target.value)}
          placeholder="Add a note (optional)"
          className="text-sm h-9"
          maxLength={200}
        />
      </div>
    </div>
  );
}
