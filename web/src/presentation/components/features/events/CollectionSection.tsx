'use client';

import { useState } from 'react';
import { Wallet } from 'lucide-react';
import { CollapsibleSection } from '@/presentation/components/ui/CollapsibleSection';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { useCreateCollection } from '@/presentation/hooks/useCollections';
import type { CollectionConfigurationDto } from '@/infrastructure/api/types/events.types';

interface CollectionSectionProps {
  eventId: string;
  collectionConfig: CollectionConfigurationDto;
  /** Public collection summary -- goal progress, contributor count */
  publicSummary?: {
    totalAmount: number;
    goalAmount?: number | null;
    goalProgressPercent?: number | null;
    completedCollections: number;
    contributorCount: number;
    currency: string;
  } | null;
}

/**
 * Public standalone collection (fundraising contribution) UI shown on event details page.
 * Allows any visitor to contribute at any time when collections are enabled.
 */
export function CollectionSection({ eventId, collectionConfig, publicSummary }: CollectionSectionProps) {
  const [selectedAmount, setSelectedAmount] = useState<number | null>(
    collectionConfig.suggestedAmounts.length > 0 ? collectionConfig.suggestedAmounts[0] : null
  );
  const [customAmount, setCustomAmount] = useState('');
  const [isCustom, setIsCustom] = useState(false);
  const [contributorName, setContributorName] = useState('');
  const [contributorEmail, setContributorEmail] = useState('');
  const [contributorPhone, setContributorPhone] = useState('');
  const [contributorNotes, setContributorNotes] = useState('');
  const [error, setError] = useState<string | null>(null);

  const createCollection = useCreateCollection();

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
      setError('Please select or enter a contribution amount.');
      return;
    }

    if (!contributorName.trim()) {
      setError('Please enter your name.');
      return;
    }

    if (!contributorEmail.trim()) {
      setError('Please enter your email address.');
      return;
    }

    if (collectionConfig.minAmount && effectiveAmount < collectionConfig.minAmount) {
      setError(`Minimum contribution amount is $${collectionConfig.minAmount.toFixed(2)}.`);
      return;
    }

    if (collectionConfig.maxAmount && effectiveAmount > collectionConfig.maxAmount) {
      setError(`Maximum contribution amount is $${collectionConfig.maxAmount.toFixed(2)}.`);
      return;
    }

    try {
      const checkoutUrl = await createCollection.mutateAsync({
        eventId,
        request: {
          contributorName: contributorName.trim(),
          contributorEmail: contributorEmail.trim(),
          contributorPhone: contributorPhone.trim() || null,
          contributorNotes: contributorNotes.trim() || null,
          amount: effectiveAmount,
          successUrl: `${window.location.origin}/events/${eventId}?collection=success`,
          cancelUrl: `${window.location.origin}/events/${eventId}?collection=cancelled`,
        },
      });

      if (checkoutUrl) {
        window.location.href = checkoutUrl;
      }
    } catch (err: any) {
      setError(err?.response?.data?.detail || 'Failed to process contribution. Please try again.');
    }
  };

  // Compute goal progress values
  const hasGoalProgress =
    collectionConfig.showProgress &&
    collectionConfig.goalAmount != null &&
    collectionConfig.goalAmount > 0;

  const goalProgressPercent =
    hasGoalProgress && publicSummary
      ? publicSummary.goalProgressPercent ?? Math.min(100, (publicSummary.totalAmount / collectionConfig.goalAmount!) * 100)
      : 0;

  return (
    <CollapsibleSection
      title="Contribute to Event Fund"
      icon={<Wallet className="h-5 w-5 text-violet-600" />}
      description={collectionConfig.collectionMessage || undefined}
      defaultOpen={false}
    >
        {/* Goal Progress Bar */}
        {hasGoalProgress && publicSummary && (
          <div className="mb-4 p-3 bg-violet-50 border border-violet-200 rounded-lg">
            <div className="flex items-center justify-between mb-2">
              <span className="text-sm font-semibold text-violet-800">
                ${publicSummary.totalAmount.toFixed(2)} raised
              </span>
              <span className="text-sm text-violet-600">
                of ${collectionConfig.goalAmount!.toFixed(2)} goal
              </span>
            </div>
            <div className="w-full h-2.5 bg-violet-100 rounded-full overflow-hidden">
              <div
                className="h-full bg-violet-600 rounded-full transition-all duration-500"
                style={{ width: `${Math.min(100, goalProgressPercent)}%` }}
              />
            </div>
            <p className="text-xs text-violet-600 mt-1.5">
              {goalProgressPercent.toFixed(0)}% of goal reached
            </p>
          </div>
        )}

        {/* Collection Summary (no goal) */}
        {!hasGoalProgress && publicSummary && publicSummary.completedCollections > 0 && (
          <div className="mb-4 p-3 bg-violet-50 border border-violet-200 rounded-lg">
            <div className="flex items-center gap-3">
              <Wallet className="h-5 w-5 text-violet-600" />
              <div>
                <p className="text-sm font-semibold text-violet-800">
                  ${publicSummary.totalAmount.toFixed(2)} {publicSummary.currency} raised
                </p>
                {collectionConfig.showContributorCount && publicSummary.contributorCount > 0 && (
                  <p className="text-xs text-violet-600 mt-0.5">
                    {publicSummary.contributorCount} contributor{publicSummary.contributorCount !== 1 ? 's' : ''}
                  </p>
                )}
              </div>
            </div>
          </div>
        )}

        {/* Contributor count inline (when goal progress is shown) */}
        {hasGoalProgress && collectionConfig.showContributorCount && publicSummary && publicSummary.contributorCount > 0 && (
          <p className="mb-4 text-xs text-violet-600 flex items-center gap-1.5">
            <Wallet className="h-3.5 w-3.5" />
            {publicSummary.contributorCount} contributor{publicSummary.contributorCount !== 1 ? 's' : ''}
          </p>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          {/* Suggested Amounts */}
          {collectionConfig.suggestedAmounts.length > 0 && (
            <div>
              <label className="block text-sm font-medium text-neutral-700 mb-2">
                Select an amount
              </label>
              <div className="flex gap-2 flex-wrap">
                {collectionConfig.suggestedAmounts.map((amount) => (
                  <button
                    key={amount}
                    type="button"
                    onClick={() => handleSelectAmount(amount)}
                    className={`px-4 py-2 rounded-full text-sm font-medium border transition-colors ${
                      selectedAmount === amount && !isCustom
                        ? 'bg-violet-600 text-white border-violet-600'
                        : 'bg-white text-neutral-700 border-neutral-300 hover:border-violet-300'
                    }`}
                  >
                    ${amount.toFixed(2)}
                  </button>
                ))}
              </div>
            </div>
          )}

          {/* Custom Amount */}
          {collectionConfig.allowCustomAmount && (
            <div>
              <label htmlFor="customCollectionAmount" className="block text-sm font-medium text-neutral-700 mb-1">
                {collectionConfig.suggestedAmounts.length > 0 ? 'Or enter a custom amount' : 'Enter amount'}
              </label>
              <div className="relative">
                <span className="absolute left-3 top-1/2 -translate-y-1/2 text-neutral-500">$</span>
                <Input
                  id="customCollectionAmount"
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

          {/* Contributor Information */}
          <div className="space-y-3">
            <div>
              <label htmlFor="contributorName" className="block text-sm font-medium text-neutral-700 mb-1">
                Your Name *
              </label>
              <Input
                id="contributorName"
                value={contributorName}
                onChange={(e) => setContributorName(e.target.value)}
                placeholder="Enter your name"
                required
              />
            </div>
            <div>
              <label htmlFor="contributorEmail" className="block text-sm font-medium text-neutral-700 mb-1">
                Email Address *
              </label>
              <Input
                id="contributorEmail"
                type="email"
                value={contributorEmail}
                onChange={(e) => setContributorEmail(e.target.value)}
                placeholder="your@email.com"
                required
              />
            </div>
            <div>
              <label htmlFor="contributorPhone" className="block text-sm font-medium text-neutral-700 mb-1">
                Phone (optional)
              </label>
              <Input
                id="contributorPhone"
                type="tel"
                value={contributorPhone}
                onChange={(e) => setContributorPhone(e.target.value)}
                placeholder="(555) 123-4567"
              />
            </div>
            <div>
              <label htmlFor="contributorNotes" className="block text-sm font-medium text-neutral-700 mb-1">
                Message (optional)
              </label>
              <textarea
                id="contributorNotes"
                value={contributorNotes}
                onChange={(e) => setContributorNotes(e.target.value)}
                placeholder="Add a note with your contribution..."
                className="w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-violet-500"
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
            disabled={createCollection.isPending || effectiveAmount <= 0}
            className="w-full"
            style={{ background: '#7C3AED' }}
          >
            {createCollection.isPending
              ? 'Processing...'
              : effectiveAmount > 0
                ? `Contribute $${effectiveAmount.toFixed(2)}`
                : 'Select an amount'}
          </Button>
        </form>
    </CollapsibleSection>
  );
}
