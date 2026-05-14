'use client';

import { useState } from 'react';
import { Award } from 'lucide-react';
import { CollapsibleSection } from '@/presentation/components/ui/CollapsibleSection';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { useCreateMoneySponsor, useCreateItemSponsor } from '@/presentation/hooks/useSponsors';
import type { SponsorConfigurationDto, SponsorDto } from '@/infrastructure/api/types/events.types';

type SponsorMode = 'money' | 'item';

interface SponsorSectionProps {
  eventId: string;
  sponsorConfig: SponsorConfigurationDto;
  mySponsors?: SponsorDto[] | null;
}

/**
 * Public-facing sponsor form shown on the event details page.
 * Supports dual-mode: Money sponsors (Stripe checkout) and Item sponsors (form submission).
 */
export function SponsorSection({ eventId, sponsorConfig, mySponsors }: SponsorSectionProps) {
  const defaultMode: SponsorMode = sponsorConfig.acceptMoneySponsors ? 'money' : 'item';
  const showToggle = sponsorConfig.acceptMoneySponsors && sponsorConfig.acceptItemSponsors;

  const [mode, setMode] = useState<SponsorMode>(defaultMode);

  // Common fields
  const [sponsorName, setSponsorName] = useState('');
  const [sponsorEmail, setSponsorEmail] = useState('');
  const [sponsorPhone, setSponsorPhone] = useState('');
  const [sponsorOrganization, setSponsorOrganization] = useState('');
  const [sponsorNotes, setSponsorNotes] = useState('');

  // Money mode fields
  const [amount, setAmount] = useState('');

  // Item mode fields
  const [itemName, setItemName] = useState('');
  const [itemDescription, setItemDescription] = useState('');
  const [estimatedValue, setEstimatedValue] = useState('');

  const [error, setError] = useState<string | null>(null);
  const [itemSuccess, setItemSuccess] = useState(false);

  const createMoneySponsor = useCreateMoneySponsor();
  const createItemSponsor = useCreateItemSponsor();

  const parsedAmount = parseFloat(amount) || 0;
  const isPending = createMoneySponsor.isPending || createItemSponsor.isPending;

  const handleModeChange = (newMode: SponsorMode) => {
    setMode(newMode);
    setError(null);
    setItemSuccess(false);
  };

  const validateCommonFields = (): boolean => {
    if (!sponsorName.trim()) {
      setError('Please enter your name.');
      return false;
    }
    if (!sponsorEmail.trim()) {
      setError('Please enter your email address.');
      return false;
    }
    return true;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setItemSuccess(false);

    if (!validateCommonFields()) return;

    if (mode === 'money') {
      if (parsedAmount <= 0) {
        setError('Please enter a sponsorship amount.');
        return;
      }

      if (sponsorConfig.minSponsorAmount && parsedAmount < sponsorConfig.minSponsorAmount) {
        setError(`Minimum sponsorship amount is $${sponsorConfig.minSponsorAmount.toFixed(2)}.`);
        return;
      }

      try {
        const checkoutUrl = await createMoneySponsor.mutateAsync({
          eventId,
          request: {
            sponsorName: sponsorName.trim(),
            sponsorEmail: sponsorEmail.trim(),
            sponsorPhone: sponsorPhone.trim() || null,
            sponsorOrganization: sponsorOrganization.trim() || null,
            sponsorNotes: sponsorNotes.trim() || null,
            amount: parsedAmount,
            currency: null,
            successUrl: `${window.location.origin}/events/${eventId}?sponsor=success`,
            cancelUrl: `${window.location.origin}/events/${eventId}?sponsor=cancelled`,
          },
        });

        if (checkoutUrl) {
          window.location.href = checkoutUrl;
        }
      } catch (err: any) {
        setError(err?.response?.data?.detail || 'Failed to process sponsorship. Please try again.');
      }
    } else {
      // Item mode
      if (!itemName.trim()) {
        setError('Please enter the item name.');
        return;
      }

      try {
        await createItemSponsor.mutateAsync({
          eventId,
          request: {
            sponsorName: sponsorName.trim(),
            sponsorEmail: sponsorEmail.trim(),
            sponsorPhone: sponsorPhone.trim() || null,
            sponsorOrganization: sponsorOrganization.trim() || null,
            sponsorNotes: sponsorNotes.trim() || null,
            itemName: itemName.trim(),
            itemDescription: itemDescription.trim() || null,
            estimatedValue: parseFloat(estimatedValue) || null,
          },
        });

        setItemSuccess(true);
        // Reset item-specific fields
        setItemName('');
        setItemDescription('');
        setEstimatedValue('');
      } catch (err: any) {
        setError(err?.response?.data?.detail || 'Failed to submit item sponsorship. Please try again.');
      }
    }
  };

  return (
    <CollapsibleSection
      title="Sponsor This Event"
      icon={<Award className="h-5 w-5 text-indigo-500" />}
      description={sponsorConfig.sponsorMessage || undefined}
      defaultOpen={!!sponsorConfig.sponsorImageUrl}
    >
      {/* Phase 6A.143 — full-width banner image when the organizer uploaded one.
          Rendered above the form so visitors see it without scrolling. Auto-expand
          (defaultOpen above) ensures the banner gets exposure when present. */}
      {sponsorConfig.sponsorImageUrl && (
        /* eslint-disable-next-line @next/next/no-img-element */
        <img
          src={sponsorConfig.sponsorImageUrl}
          alt="Sponsor banner"
          className="mb-4 w-full max-h-64 rounded-lg border border-neutral-200 object-cover bg-neutral-50"
        />
      )}

      {/* Mode Toggle */}
      {showToggle && (
        <div className="mb-4 flex rounded-lg bg-indigo-50 p-1">
          <button
            type="button"
            onClick={() => handleModeChange('money')}
            className={`flex-1 rounded-md px-3 py-2 text-sm font-medium transition-colors ${
              mode === 'money'
                ? 'bg-white text-indigo-700 shadow-sm'
                : 'text-neutral-600 hover:text-indigo-600'
            }`}
          >
            Money Sponsorship
          </button>
          <button
            type="button"
            onClick={() => handleModeChange('item')}
            className={`flex-1 rounded-md px-3 py-2 text-sm font-medium transition-colors ${
              mode === 'item'
                ? 'bg-white text-indigo-700 shadow-sm'
                : 'text-neutral-600 hover:text-indigo-600'
            }`}
          >
            Item Sponsorship
          </button>
        </div>
      )}

      {/* Item Success Message */}
      {itemSuccess && (
        <div className="mb-4 p-3 bg-green-50 border border-green-200 rounded-lg text-sm text-green-700">
          Thank you! Your item sponsorship has been recorded.
        </div>
      )}

      <form onSubmit={handleSubmit} className="space-y-4">
        {/* Common Fields */}
        <div className="space-y-3">
          <div>
            <label htmlFor="sponsorName" className="block text-sm font-medium text-neutral-700 mb-1">
              Sponsor Name *
            </label>
            <Input
              id="sponsorName"
              value={sponsorName}
              onChange={(e) => { setSponsorName(e.target.value); setError(null); }}
              placeholder="Enter your name"
              required
            />
          </div>
          <div>
            <label htmlFor="sponsorEmail" className="block text-sm font-medium text-neutral-700 mb-1">
              Email *
            </label>
            <Input
              id="sponsorEmail"
              type="email"
              value={sponsorEmail}
              onChange={(e) => { setSponsorEmail(e.target.value); setError(null); }}
              placeholder="your@email.com"
              required
            />
          </div>
          <div>
            <label htmlFor="sponsorPhone" className="block text-sm font-medium text-neutral-700 mb-1">
              Phone (optional)
            </label>
            <Input
              id="sponsorPhone"
              type="tel"
              value={sponsorPhone}
              onChange={(e) => setSponsorPhone(e.target.value)}
              placeholder="(555) 123-4567"
            />
          </div>
          <div>
            <label htmlFor="sponsorOrganization" className="block text-sm font-medium text-neutral-700 mb-1">
              Organization (optional)
            </label>
            <Input
              id="sponsorOrganization"
              value={sponsorOrganization}
              onChange={(e) => setSponsorOrganization(e.target.value)}
              placeholder="Company or organization name"
            />
          </div>
          <div>
            <label htmlFor="sponsorNotes" className="block text-sm font-medium text-neutral-700 mb-1">
              Notes (optional)
            </label>
            <textarea
              id="sponsorNotes"
              value={sponsorNotes}
              onChange={(e) => setSponsorNotes(e.target.value)}
              placeholder="Add a note with your sponsorship..."
              className="w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
              rows={2}
            />
          </div>
        </div>

        {/* Money Mode Fields */}
        {mode === 'money' && (
          <div>
            <label htmlFor="sponsorAmount" className="block text-sm font-medium text-neutral-700 mb-1">
              Sponsorship Amount *
            </label>
            <div className="relative">
              <span className="absolute left-3 top-1/2 -translate-y-1/2 text-neutral-500">$</span>
              <Input
                id="sponsorAmount"
                type="number"
                min={sponsorConfig.minSponsorAmount || 1}
                step="0.01"
                value={amount}
                onChange={(e) => { setAmount(e.target.value); setError(null); }}
                placeholder="0.00"
                className="pl-7"
              />
            </div>
            {sponsorConfig.minSponsorAmount && (
              <p className="mt-1 text-xs text-neutral-500">
                Minimum: ${sponsorConfig.minSponsorAmount.toFixed(2)}
              </p>
            )}
          </div>
        )}

        {/* Item Mode Fields */}
        {mode === 'item' && (
          <div className="space-y-3">
            <div>
              <label htmlFor="itemName" className="block text-sm font-medium text-neutral-700 mb-1">
                Item Name *
              </label>
              <Input
                id="itemName"
                value={itemName}
                onChange={(e) => { setItemName(e.target.value); setError(null); }}
                placeholder="e.g., Gift basket, Venue decoration"
                required
              />
            </div>
            <div>
              <label htmlFor="itemDescription" className="block text-sm font-medium text-neutral-700 mb-1">
                Item Description (optional)
              </label>
              <textarea
                id="itemDescription"
                value={itemDescription}
                onChange={(e) => setItemDescription(e.target.value)}
                placeholder="Describe the item you'd like to sponsor..."
                className="w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                rows={3}
              />
            </div>
            <div>
              <label htmlFor="estimatedValue" className="block text-sm font-medium text-neutral-700 mb-1">
                Estimated Value (optional)
              </label>
              <div className="relative">
                <span className="absolute left-3 top-1/2 -translate-y-1/2 text-neutral-500">$</span>
                <Input
                  id="estimatedValue"
                  type="number"
                  min="0"
                  step="0.01"
                  value={estimatedValue}
                  onChange={(e) => setEstimatedValue(e.target.value)}
                  placeholder="0.00"
                  className="pl-7"
                />
              </div>
            </div>
          </div>
        )}

        {/* Error Message */}
        {error && (
          <div className="p-3 bg-red-50 border border-red-200 rounded-lg text-sm text-red-600">
            {error}
          </div>
        )}

        {/* Non-refundable disclaimer (money sponsorships only) */}
        {mode === 'money' && (
          <p className="text-xs text-gray-500 italic">
            Sponsorships are non-refundable and will not be included in registration cancellation refunds.
          </p>
        )}

        {/* Submit Button */}
        {mode === 'money' ? (
          <Button
            type="submit"
            disabled={isPending || parsedAmount <= 0}
            className="w-full"
            style={{ background: '#4F46E5' }}
          >
            {createMoneySponsor.isPending
              ? 'Processing...'
              : parsedAmount > 0
                ? `Sponsor $${parsedAmount.toFixed(2)}`
                : 'Enter an amount'}
          </Button>
        ) : (
          <Button
            type="submit"
            disabled={isPending}
            className="w-full"
            style={{ background: '#4F46E5' }}
          >
            {createItemSponsor.isPending ? 'Submitting...' : 'Submit Item Sponsorship'}
          </Button>
        )}
      </form>

      {/* Your Sponsorships */}
      {mySponsors && mySponsors.length > 0 && (
        <div className="mt-4 pt-4 border-t border-neutral-200">
          <h4 className="text-sm font-semibold text-neutral-700 mb-2">Your Sponsorships</h4>
          <div className="space-y-2">
            {mySponsors.map((sponsor) => (
              <div key={sponsor.id} className="flex items-center justify-between py-2 px-3 bg-white rounded border border-indigo-100">
                <div className="flex items-center gap-3">
                  {sponsor.sponsorType === 'Money' ? (
                    <span className="text-sm font-semibold text-neutral-900">
                      ${(sponsor.amount ?? 0).toFixed(2)}
                    </span>
                  ) : (
                    <span className="text-sm font-semibold text-neutral-900">
                      {sponsor.itemName || 'Item'}
                    </span>
                  )}
                  <span className="text-xs text-neutral-500">
                    ({sponsor.sponsorType === 'Money' ? 'Monetary' : 'Item'})
                  </span>
                </div>
                <div className="flex items-center gap-3">
                  <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${
                    sponsor.status === 'Completed' || sponsor.status === 'RecordedItem'
                      ? 'bg-green-100 text-green-700'
                      : sponsor.status === 'Pending'
                      ? 'bg-yellow-100 text-yellow-700'
                      : 'bg-neutral-100 text-neutral-600'
                  }`}>
                    {sponsor.status === 'RecordedItem' ? 'Recorded' : sponsor.status}
                  </span>
                  <span className="text-xs text-neutral-500">
                    {new Date(sponsor.createdAt).toLocaleDateString()}
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
