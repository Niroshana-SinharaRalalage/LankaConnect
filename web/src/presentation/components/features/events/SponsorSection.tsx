'use client';

import { useMemo, useRef, useState } from 'react';
import { Award, ImagePlus, X, Handshake, Pencil } from 'lucide-react';
import { CollapsibleSection } from '@/presentation/components/ui/CollapsibleSection';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import {
  useCreateMoneySponsor,
  useCreateItemSponsor,
  usePublicEventSponsors,  // Phase 6A.150: PII-free public read for the in-section sponsor wall
  useUploadSponsorImage,
} from '@/presentation/hooks/useSponsors';
import { usePublicSponsorshipPackages } from '@/presentation/hooks/useSponsorshipPackages';  // Phase 6A.157
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import type {
  SponsorConfigurationDto,
  SponsorDto,
  PublicSponsorDto,  // Phase 6A.150
  SponsorshipPackagePublicDto,  // Phase 6A.157
} from '@/infrastructure/api/types/events.types';
import { EditSponsorModal } from './EditSponsorModal';
import { PublicSponsorshipPackageCard } from './PublicSponsorshipPackageCard';  // Phase 6A.157
import { PurchaseSponsorshipPackageModal } from './PurchaseSponsorshipPackageModal';  // Phase 6A.157

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
  // Phase 6A.151 — sponsor self-edit modal state.
  const [editingMySponsor, setEditingMySponsor] = useState<SponsorDto | null>(null);
  // Phase 6A.157 — buyer purchase modal state. Null pkg = closed.
  const [purchasingPackage, setPurchasingPackage] = useState<SponsorshipPackagePublicDto | null>(null);

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
  // section so visitors see who already sponsored.
  // Phase 6A.150 — switched from useEventSponsors (auth-required) to
  // usePublicEventSponsors (anonymous-allowed, PII-free). The backend handler
  // already filters to image-bearing confirmed sponsors and pre-sorts by
  // contribution magnitude — the response is exactly what we want to render.
  const { data: sponsorsResponse } = usePublicEventSponsors(eventId, sponsorConfig.isEnabled === true);

  // Phase 6A.157 — public buyer-facing packages, gated on the EnablePackages
  // flag. Server returns [] for events that haven't opted in (so the gate is
  // belt-and-suspenders — the section below also self-hides on empty list).
  const { data: publicPackages = [] } = usePublicSponsorshipPackages(
    eventId,
    sponsorConfig.isEnabled === true && sponsorConfig.enablePackages === true,
  );
  const sponsorsWithImages: PublicSponsorDto[] = sponsorsResponse?.sponsors ?? [];

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
      {/* Phase 6A.145 Commit 8 — confirmed sponsors shown inside this section so
          visitors see who has already sponsored before adding their own.
          Phase 6A.148.W5.D10 — variable name kept (sponsorsWithImages) but the
          backend filter no longer requires ImageUrl. Missing-logo sponsors get
          an initials placeholder so the layout stays consistent. */}
      {sponsorsWithImages.length > 0 && (
        <div className="mb-4">
          <h4 className="text-sm font-semibold text-neutral-700 mb-2 flex items-center gap-2">
            <Handshake className="h-4 w-4 text-indigo-500" />
            Sponsors
          </h4>
          <div className="flex flex-wrap gap-3">
            {sponsorsWithImages.map((s) => {
              const displayName = s.sponsorOrganization || s.sponsorName;
              const words = displayName.trim().split(/\s+/).filter(Boolean);
              const initials = words.length === 0
                ? '?'
                : words.length === 1
                  ? words[0]!.slice(0, 2).toUpperCase()
                  : (words[0]![0]! + words[1]![0]!).toUpperCase();

              return (
                <div
                  key={s.id}
                  className="flex flex-col items-center w-24 rounded-lg border border-neutral-200 bg-white p-2"
                  title={displayName}
                >
                  {s.imageUrl ? (
                    // eslint-disable-next-line @next/next/no-img-element
                    <img
                      src={s.imageUrl}
                      alt={displayName}
                      className="h-12 w-12 object-contain"
                    />
                  ) : (
                    <div
                      className="h-12 w-12 flex items-center justify-center rounded bg-gradient-to-br from-indigo-100 to-amber-100"
                      aria-hidden="true"
                    >
                      <span className="text-sm font-semibold text-indigo-700">{initials}</span>
                    </div>
                  )}
                  <span className="mt-1 w-full truncate text-center text-xs text-neutral-600">
                    {displayName}
                  </span>
                </div>
              );
            })}
          </div>
        </div>
      )}

      {/*
       * Phase 6A.157 — sponsorship package grid (buyer-facing). Mounted ABOVE
       * the custom-amount mode toggle per the user-locked decision in 6A.156:
       * packages render as cards above the existing form so buyers see the
       * curated tiers first and can fall back to a custom amount below.
       *
       * Self-gated on EnablePackages (via the query enabled flag) AND on
       * non-empty list — events that haven't opted in OR have no active
       * packages see the original UI completely unchanged (zero regression
       * risk for non-packages sponsor flows).
       */}
      {sponsorConfig.enablePackages === true && publicPackages.length > 0 && (
        <div className="mb-6">
          <h4 className="text-sm font-semibold text-neutral-700 mb-3 flex items-center gap-2">
            <Award className="h-4 w-4 text-amber-500" />
            Sponsorship Packages
          </h4>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {publicPackages.map((pkg) => (
              <PublicSponsorshipPackageCard
                key={pkg.id}
                pkg={pkg}
                onSelect={(selected) => setPurchasingPackage(selected)}
              />
            ))}
          </div>
          {/* Divider between curated packages and the free-form custom-amount form */}
          <div className="mt-6 mb-2 flex items-center gap-3">
            <div className="flex-1 h-px bg-neutral-200" />
            <span className="text-xs text-neutral-500 uppercase tracking-wide">
              Or choose your own amount
            </span>
            <div className="flex-1 h-px bg-neutral-200" />
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
                      : sponsor.status === 'Refunded'
                      ? 'bg-rose-100 text-rose-700 line-through'
                      : 'bg-neutral-100 text-neutral-600'
                  }`}>
                    {sponsor.status === 'RecordedItem' ? 'Recorded' : sponsor.status}
                  </span>
                  <span className="text-xs text-neutral-500">
                    {new Date(sponsor.createdAt).toLocaleDateString()}
                  </span>
                  {/* Phase 6A.151 — self-edit. Anonymous sponsors have no sponsorUserId
                      so they never reach this list (mySponsors is JWT-scoped). The
                      backend re-checks authz and the modal disables fields per matrix. */}
                  <button
                    type="button"
                    onClick={() => setEditingMySponsor(sponsor)}
                    className="inline-flex items-center justify-center h-7 w-7 rounded text-neutral-500 hover:text-indigo-600 hover:bg-indigo-50"
                    aria-label={`Edit sponsorship from ${sponsor.sponsorName}`}
                    title="Edit"
                  >
                    <Pencil className="h-3.5 w-3.5" />
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Phase 6A.151 — sponsor self-edit modal */}
      <EditSponsorModal
        eventId={eventId}
        sponsor={editingMySponsor}
        isOrganizer={false}
        open={editingMySponsor !== null}
        onClose={() => setEditingMySponsor(null)}
        onSaved={() => setEditingMySponsor(null)}
      />

      {/* Phase 6A.157 — buyer purchase modal. Portal'd to document.body so its
          submit is isolated from any parent <form> (per 6A.156-fix-2 contract). */}
      <PurchaseSponsorshipPackageModal
        eventId={eventId}
        pkg={purchasingPackage}
        isOpen={purchasingPackage !== null}
        onClose={() => setPurchasingPackage(null)}
      />
    </CollapsibleSection>
  );
}
