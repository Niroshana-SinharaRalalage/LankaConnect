'use client';

import { useMemo, useRef, useState } from 'react';
import { Award, ImagePlus, X, Handshake } from 'lucide-react';
import { CollapsibleSection } from '@/presentation/components/ui/CollapsibleSection';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import {
  useCreateMoneySponsor,
  useCreateItemSponsor,
  useEventSponsors,
  useUploadSponsorImage,
} from '@/presentation/hooks/useSponsors';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
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
  const uploadImage = useUploadSponsorImage();

  // Phase 6A.145 Commit 8 — optional image upload (no threshold gate). User selects
  // a file; on submit we sequence: create sponsor → upload image to returned ID →
  // (Money only) redirect to Stripe.
  const [imageFile, setImageFile] = useState<File | null>(null);
  const imageInputRef = useRef<HTMLInputElement>(null);

  // Phase 6A.145 Commit 8 — render existing sponsors-with-images inside this
  // section so visitors see who already sponsored. Uses the same query as the
  // top preview strip. Public event details viewers may 403 on this endpoint;
  // when that happens, we just hide the block.
  const { data: sponsorsResponse } = useEventSponsors(eventId, sponsorConfig.isEnabled === true);
  const sponsorsWithImages = useMemo(() => {
    const sponsors = sponsorsResponse?.sponsors ?? [];
    return sponsors
      .filter((s: SponsorDto) => {
        if (!s.imageUrl) return false;
        if (s.sponsorType === 'Money') return s.status === 'Completed';
        if (s.sponsorType === 'Item') return s.status === 'RecordedItem';
        return false;
      })
      .sort((a: SponsorDto, b: SponsorDto) => {
        const av = a.amount ?? a.estimatedValue ?? 0;
        const bv = b.amount ?? b.estimatedValue ?? 0;
        if (bv !== av) return bv - av;
        return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
      });
  }, [sponsorsResponse]);

  const parsedAmount = parseFloat(amount) || 0;
  const isPending = createMoneySponsor.isPending || createItemSponsor.isPending || uploadImage.isPending;

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
        const result = await createMoneySponsor.mutateAsync({
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

        // Phase 6A.145 Commit 8 — if user attached an image, upload it to the
        // Pending sponsor BEFORE redirecting to Stripe. If upload fails we still
        // proceed to Stripe (don't block the payment); user can email the
        // organizer the logo as a fallback.
        if (imageFile && result.sponsorId) {
          try {
            await eventsRepository.uploadSponsorImage(eventId, result.sponsorId, imageFile);
          } catch (imgErr) {
            console.warn('[SponsorSection] image upload failed before Stripe redirect:', imgErr);
            // Non-fatal — continue to checkout. Image can be attached later.
          }
        }

        if (result.checkoutUrl) {
          window.location.href = result.checkoutUrl;
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
        const sponsorId = await createItemSponsor.mutateAsync({
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

        // Phase 6A.145 Commit 8 — attach image to the just-recorded Item sponsor.
        if (imageFile && sponsorId) {
          try {
            await eventsRepository.uploadSponsorImage(eventId, sponsorId, imageFile);
          } catch (imgErr) {
            console.warn('[SponsorSection] image upload failed for item sponsor:', imgErr);
            // Non-fatal — surface a soft notice but the sponsorship is recorded.
          }
        }

        setItemSuccess(true);
        // Reset item-specific fields
        setItemName('');
        setItemDescription('');
        setEstimatedValue('');
        setImageFile(null);
        if (imageInputRef.current) imageInputRef.current.value = '';
      } catch (err: any) {
        setError(err?.response?.data?.detail || 'Failed to submit item sponsorship. Please try again.');
      }
    }
  };

  const handleImageSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    if (file.size > 5 * 1024 * 1024) {
      setError('Image too large (max 5MB).');
      return;
    }
    setError(null);
    setImageFile(file);
  };

  const handleImageClear = () => {
    setImageFile(null);
    if (imageInputRef.current) imageInputRef.current.value = '';
  };

  return (
    <CollapsibleSection
      title="Sponsor This Event"
      icon={<Award className="h-5 w-5 text-indigo-500" />}
      description={sponsorConfig.sponsorMessage || undefined}
      defaultOpen={false}
    >
      {/* Phase 6A.145 Commit 8 — sponsors with images, shown inside this section
          so visitors see who has already sponsored before adding their own. */}
      {sponsorsWithImages.length > 0 && (
        <div className="mb-4">
          <h4 className="text-sm font-semibold text-neutral-700 mb-2 flex items-center gap-2">
            <Handshake className="h-4 w-4 text-indigo-500" />
            Sponsors
          </h4>
          <div className="flex flex-wrap gap-3">
            {sponsorsWithImages.map((s) => (
              <div
                key={s.id}
                className="flex flex-col items-center w-24 rounded-lg border border-neutral-200 bg-white p-2"
                title={s.sponsorOrganization || s.sponsorName}
              >
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img
                  src={s.imageUrl!}
                  alt={s.sponsorOrganization || s.sponsorName}
                  className="h-12 w-12 object-contain"
                />
                <span className="mt-1 w-full truncate text-center text-xs text-neutral-600">
                  {s.sponsorOrganization || s.sponsorName}
                </span>
              </div>
            ))}
          </div>
        </div>
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

          {/* Phase 6A.145 Commit 8 — optional sponsor logo/image. Any amount qualifies. */}
          <div>
            <label htmlFor="sponsorImage" className="block text-sm font-medium text-neutral-700 mb-1">
              Logo or Image (optional)
            </label>
            {imageFile ? (
              <div className="flex items-center justify-between rounded-md border border-neutral-300 bg-neutral-50 px-3 py-2 text-sm">
                <span className="truncate text-neutral-700">{imageFile.name}</span>
                <button
                  type="button"
                  onClick={handleImageClear}
                  className="ml-2 flex h-6 w-6 items-center justify-center rounded text-neutral-500 hover:bg-neutral-200 hover:text-neutral-700"
                  aria-label="Remove image"
                >
                  <X className="h-4 w-4" />
                </button>
              </div>
            ) : (
              <label
                htmlFor="sponsorImage"
                className="flex cursor-pointer items-center gap-2 rounded-md border border-dashed border-neutral-300 px-3 py-2 text-sm text-neutral-600 hover:border-indigo-400 hover:text-indigo-600"
              >
                <ImagePlus className="h-4 w-4" />
                <span>Attach a logo or image (max 5MB)</span>
              </label>
            )}
            <input
              ref={imageInputRef}
              id="sponsorImage"
              type="file"
              accept="image/png,image/jpeg,image/jpg,image/webp"
              className="hidden"
              onChange={handleImageSelect}
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
                  <span className="text-sm font-semibold text-neutral-900">
                    {sponsor.sponsorType === 'Money' ? 'Monetary' : (sponsor.itemName || 'Item')}
                  </span>
                  {sponsor.sponsorType === 'Item' && (
                    <span className="text-xs text-neutral-500">(Item)</span>
                  )}
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
