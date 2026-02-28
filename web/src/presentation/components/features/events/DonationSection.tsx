'use client';

import { useState } from 'react';
import { Heart } from 'lucide-react';
import { CollapsibleSection } from '@/presentation/components/ui/CollapsibleSection';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { useCreateDonation } from '@/presentation/hooks/useDonations';
import type { DonationConfigurationDto, PublicDonationSummaryDto, DonationDto } from '@/infrastructure/api/types/events.types';

interface DonationSectionProps {
  eventId: string;
  donationConfig: DonationConfigurationDto;
  /** Public donation summary (when organizer enabled ShowDonationSummary) */
  publicSummary?: PublicDonationSummaryDto | null;
  /** Logged-in user's own donations for this event */
  myDonations?: DonationDto[] | null;
}

/**
 * Public standalone donation UI shown on event details page.
 * Allows any visitor to donate at any time when donations are enabled.
 */
export function DonationSection({ eventId, donationConfig, publicSummary, myDonations }: DonationSectionProps) {
  const [selectedAmount, setSelectedAmount] = useState<number | null>(
    donationConfig.suggestedAmounts.length > 0 ? donationConfig.suggestedAmounts[0] : null
  );
  const [customAmount, setCustomAmount] = useState('');
  const [isCustom, setIsCustom] = useState(false);
  const [donorName, setDonorName] = useState('');
  const [donorEmail, setDonorEmail] = useState('');
  const [donorPhone, setDonorPhone] = useState('');
  const [donorNotes, setDonorNotes] = useState('');
  const [error, setError] = useState<string | null>(null);

  const createDonation = useCreateDonation();

  const effectiveAmount = isCustom ? parseFloat(customAmount) || 0 : (selectedAmount || 0);

  const handleSelectAmount = (amount: number) => {
    setSelectedAmount(amount);
    setIsCustom(false);
    setCustomAmount('');
    setError(null);
  };

  const handleCustomAmountChange = (value: string) => {
    setCustomAmount(value);
    setIsCustom(true);
    setSelectedAmount(null);
    setError(null);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (effectiveAmount <= 0) {
      setError('Please select or enter a donation amount.');
      return;
    }

    if (!donorName.trim()) {
      setError('Please enter your name.');
      return;
    }

    if (!donorEmail.trim()) {
      setError('Please enter your email address.');
      return;
    }

    if (donationConfig.minAmount && effectiveAmount < donationConfig.minAmount) {
      setError(`Minimum donation amount is $${donationConfig.minAmount.toFixed(2)}.`);
      return;
    }

    if (donationConfig.maxAmount && effectiveAmount > donationConfig.maxAmount) {
      setError(`Maximum donation amount is $${donationConfig.maxAmount.toFixed(2)}.`);
      return;
    }

    try {
      const checkoutUrl = await createDonation.mutateAsync({
        eventId,
        request: {
          donorName: donorName.trim(),
          donorEmail: donorEmail.trim(),
          donorPhone: donorPhone.trim() || null,
          donorNotes: donorNotes.trim() || null,
          amount: effectiveAmount,
          successUrl: `${window.location.origin}/events/${eventId}?donation=success`,
          cancelUrl: `${window.location.origin}/events/${eventId}?donation=cancelled`,
        },
      });

      if (checkoutUrl) {
        window.location.href = checkoutUrl;
      }
    } catch (err: any) {
      setError(err?.response?.data?.detail || 'Failed to process donation. Please try again.');
    }
  };

  return (
    <CollapsibleSection
      title="Support This Event"
      icon={<Heart className="h-5 w-5 text-rose-500" />}
      description={donationConfig.donationMessage || undefined}
      defaultOpen={false}
    >
        {/* Public Donation Summary */}
        {publicSummary && publicSummary.completedDonations > 0 && (
          <div className="mb-4 p-3 bg-rose-50 border border-rose-200 rounded-lg">
            <div className="flex items-center gap-3">
              <Heart className="h-5 w-5 text-rose-600" />
              <div>
                <p className="text-sm font-semibold text-rose-800">
                  {publicSummary.completedDonations} donation{publicSummary.completedDonations !== 1 ? 's' : ''} received
                </p>
                <p className="text-xs text-rose-600 mt-0.5">
                  ${publicSummary.netRaisedAmount.toFixed(2)} {publicSummary.currency} raised
                </p>
              </div>
            </div>
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          {/* Suggested Amounts */}
          {donationConfig.suggestedAmounts.length > 0 && (
            <div>
              <label className="block text-sm font-medium text-neutral-700 mb-2">
                Select an amount
              </label>
              <div className="flex gap-2 flex-wrap">
                {donationConfig.suggestedAmounts.map((amount) => (
                  <button
                    key={amount}
                    type="button"
                    onClick={() => handleSelectAmount(amount)}
                    className={`px-4 py-2 rounded-full text-sm font-medium border transition-colors ${
                      selectedAmount === amount && !isCustom
                        ? 'bg-rose-500 text-white border-rose-500'
                        : 'bg-white text-neutral-700 border-neutral-300 hover:border-rose-300'
                    }`}
                  >
                    ${amount.toFixed(2)}
                  </button>
                ))}
              </div>
            </div>
          )}

          {/* Custom Amount */}
          {donationConfig.allowCustomAmount && (
            <div>
              <label htmlFor="customAmount" className="block text-sm font-medium text-neutral-700 mb-1">
                {donationConfig.suggestedAmounts.length > 0 ? 'Or enter a custom amount' : 'Enter amount'}
              </label>
              <div className="relative">
                <span className="absolute left-3 top-1/2 -translate-y-1/2 text-neutral-500">$</span>
                <Input
                  id="customAmount"
                  type="number"
                  min="1"
                  step="0.01"
                  value={customAmount}
                  onChange={(e) => handleCustomAmountChange(e.target.value)}
                  placeholder="0.00"
                  className="pl-7"
                />
              </div>
            </div>
          )}

          {/* Donor Information */}
          <div className="space-y-3">
            <div>
              <label htmlFor="donorName" className="block text-sm font-medium text-neutral-700 mb-1">
                Your Name *
              </label>
              <Input
                id="donorName"
                value={donorName}
                onChange={(e) => setDonorName(e.target.value)}
                placeholder="Enter your name"
                required
              />
            </div>
            <div>
              <label htmlFor="donorEmail" className="block text-sm font-medium text-neutral-700 mb-1">
                Email Address *
              </label>
              <Input
                id="donorEmail"
                type="email"
                value={donorEmail}
                onChange={(e) => setDonorEmail(e.target.value)}
                placeholder="your@email.com"
                required
              />
            </div>
            <div>
              <label htmlFor="donorPhone" className="block text-sm font-medium text-neutral-700 mb-1">
                Phone (optional)
              </label>
              <Input
                id="donorPhone"
                type="tel"
                value={donorPhone}
                onChange={(e) => setDonorPhone(e.target.value)}
                placeholder="(555) 123-4567"
              />
            </div>
            <div>
              <label htmlFor="donorNotes" className="block text-sm font-medium text-neutral-700 mb-1">
                Message (optional)
              </label>
              <textarea
                id="donorNotes"
                value={donorNotes}
                onChange={(e) => setDonorNotes(e.target.value)}
                placeholder="Add a note with your donation..."
                className="w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-rose-500"
                rows={2}
              />
            </div>
          </div>

          {/* Error Message */}
          {error && (
            <div className="p-3 bg-red-50 border border-red-200 rounded-lg text-sm text-red-600">
              {error}
            </div>
          )}

          {/* Submit Button */}
          <Button
            type="submit"
            disabled={createDonation.isPending || effectiveAmount <= 0}
            className="w-full"
            style={{ background: '#8B1538' }}
          >
            {createDonation.isPending
              ? 'Processing...'
              : effectiveAmount > 0
                ? `Donate $${effectiveAmount.toFixed(2)}`
                : 'Select an amount'}
          </Button>
        </form>

        {/* Your Donations */}
        {myDonations && myDonations.length > 0 && (
          <div className="mt-4 pt-4 border-t border-neutral-200">
            <h4 className="text-sm font-semibold text-neutral-700 mb-2">Your Donations</h4>
            <div className="space-y-2">
              {myDonations.map((donation) => (
                <div key={donation.id} className="flex items-center justify-between py-2 px-3 bg-white rounded border border-rose-100">
                  <div className="flex items-center gap-3">
                    <span className="text-sm font-semibold text-neutral-900">${donation.amount.toFixed(2)}</span>
                    {donation.isBundled && (
                      <span className="text-xs text-neutral-500">(with registration)</span>
                    )}
                  </div>
                  <div className="flex items-center gap-3">
                    <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${
                      donation.status === 'Completed'
                        ? 'bg-green-100 text-green-700'
                        : donation.status === 'Pending'
                        ? 'bg-yellow-100 text-yellow-700'
                        : 'bg-neutral-100 text-neutral-600'
                    }`}>
                      {donation.status}
                    </span>
                    <span className="text-xs text-neutral-500">
                      {new Date(donation.createdAt).toLocaleDateString()}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}
    </CollapsibleSection>
  );
}
