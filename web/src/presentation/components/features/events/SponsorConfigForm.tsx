'use client';

import { useRef, useState } from 'react';
import { ImagePlus, X, Handshake } from 'lucide-react';
import { Input } from '@/presentation/components/ui/Input';
import { Button } from '@/presentation/components/ui/Button';
import { useUploadSponsorImage, useDeleteSponsorImage } from '@/presentation/hooks/useSponsors';

interface SponsorConfigFormProps {
  /** Whether sponsorships are enabled */
  isEnabled: boolean;
  onEnabledChange: (enabled: boolean) => void;
  /** Whether to accept monetary sponsorships */
  acceptMoneySponsors: boolean;
  onAcceptMoneySponsorsChange: (accept: boolean) => void;
  /** Whether to accept item-based sponsorships */
  acceptItemSponsors: boolean;
  onAcceptItemSponsorsChange: (accept: boolean) => void;
  /** Minimum monetary sponsorship amount (optional) */
  minSponsorAmount: number | null;
  onMinSponsorAmountChange: (amount: number | null) => void;
  /** Custom sponsor message (optional) */
  sponsorMessage: string;
  onSponsorMessageChange: (message: string) => void;
  /** Whether to show sponsor list publicly */
  showSponsorList: boolean;
  onShowSponsorListChange: (show: boolean) => void;
  /**
   * Phase 6A.143 — when both provided, render a sponsor banner image upload widget.
   * Banner endpoint requires sponsor config to ALREADY be saved + enabled server-side,
   * so eventId is only meaningful in edit mode after a save.
   */
  eventId?: string;
  sponsorImageUrl?: string | null;
}

/**
 * Phase 6A.143 — sponsor banner image widget. Shown only when eventId is known
 * (edit mode after save) AND sponsorships are server-confirmed enabled. Mirrors
 * the AddOnImageField pattern in AddOnDefinitionEditor.
 */
function SponsorBannerField({
  eventId,
  imageUrl,
}: {
  eventId: string;
  imageUrl: string | null | undefined;
}) {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [error, setError] = useState<string | null>(null);
  const uploadMutation = useUploadSponsorImage();
  const deleteMutation = useDeleteSponsorImage();
  const isBusy = uploadMutation.isPending || deleteMutation.isPending;

  const handleFile = async (file: File | null) => {
    if (!file) return;
    setError(null);
    if (file.size > 5 * 1024 * 1024) {
      setError('Image too large (max 5MB).');
      return;
    }
    try {
      await uploadMutation.mutateAsync({ eventId, file });
    } catch (err: any) {
      console.error('Sponsor banner upload failed:', err);
      setError(err?.response?.data?.detail || err?.message || 'Upload failed. Please try again.');
    } finally {
      if (fileInputRef.current) fileInputRef.current.value = '';
    }
  };

  const handleRemove = async () => {
    setError(null);
    try {
      await deleteMutation.mutateAsync({ eventId });
    } catch (err: any) {
      console.error('Sponsor banner delete failed:', err);
      setError(err?.response?.data?.detail || err?.message || 'Delete failed. Please try again.');
    }
  };

  return (
    <div className="space-y-2">
      <label className="block text-sm font-medium text-gray-700">Sponsor Banner (optional)</label>
      <div className="flex items-start gap-3">
        {imageUrl ? (
          /* eslint-disable-next-line @next/next/no-img-element */
          <img
            src={imageUrl}
            alt="Sponsor banner"
            className="h-20 w-40 rounded border border-neutral-200 object-cover bg-neutral-50"
          />
        ) : (
          <div className="flex h-20 w-40 items-center justify-center rounded border border-dashed border-neutral-300 bg-neutral-50 text-neutral-400">
            <Handshake className="h-7 w-7" />
          </div>
        )}
        <div className="flex flex-col gap-1">
          <input
            ref={fileInputRef}
            type="file"
            accept="image/jpeg,image/png,image/webp,image/gif"
            className="hidden"
            onChange={(e) => handleFile(e.target.files?.[0] ?? null)}
            disabled={isBusy}
          />
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => fileInputRef.current?.click()}
            disabled={isBusy}
          >
            <ImagePlus className="h-4 w-4 mr-1" />
            {imageUrl ? 'Replace banner' : 'Upload banner'}
          </Button>
          {imageUrl && (
            <Button type="button" variant="ghost" size="sm" onClick={handleRemove} disabled={isBusy}>
              <X className="h-4 w-4 mr-1 text-red-500" />
              Remove
            </Button>
          )}
        </div>
      </div>
      <p className="text-xs text-gray-500">
        JPG / PNG / WebP / GIF, max 5MB. Banner appears above the sponsor form on the event page.
      </p>
      {error && <p className="text-xs text-red-600">{error}</p>}
    </div>
  );
}

/**
 * Sponsor configuration section for event create/edit forms.
 * Follows the DonationConfigForm pattern.
 */
export function SponsorConfigForm({
  isEnabled,
  onEnabledChange,
  acceptMoneySponsors,
  onAcceptMoneySponsorsChange,
  acceptItemSponsors,
  onAcceptItemSponsorsChange,
  minSponsorAmount,
  onMinSponsorAmountChange,
  sponsorMessage,
  onSponsorMessageChange,
  showSponsorList,
  onShowSponsorListChange,
  eventId,
  sponsorImageUrl,
}: SponsorConfigFormProps) {
  const showTypeWarning = isEnabled && !acceptMoneySponsors && !acceptItemSponsors;

  return (
    <div className="space-y-4">
        {/* Enable Toggle */}
        <div className="flex items-start space-x-3">
          <input
            type="checkbox"
            id="enableSponsors"
            checked={isEnabled}
            onChange={(e) => onEnabledChange(e.target.checked)}
            className="mt-1 h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
          />
          <label htmlFor="enableSponsors" className="text-sm font-medium text-gray-700">
            Enable sponsorships for this event
          </label>
        </div>

        {isEnabled && (
          <div className="ml-7 space-y-4 p-4 border border-gray-200 rounded-lg bg-gray-50">
            {/* Sponsor Types */}
            <div className="space-y-3">
              <label className="block text-sm font-medium text-gray-700">
                Accepted Sponsor Types
              </label>

              <div className="flex items-start space-x-3">
                <input
                  type="checkbox"
                  id="acceptMoneySponsors"
                  checked={acceptMoneySponsors}
                  onChange={(e) => onAcceptMoneySponsorsChange(e.target.checked)}
                  className="mt-1 h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                />
                <div>
                  <label htmlFor="acceptMoneySponsors" className="text-sm font-medium text-gray-700">
                    Accept monetary sponsorships (via Stripe)
                  </label>
                  <p className="text-xs text-gray-500 mt-0.5">
                    Sponsors can contribute money through secure online payment
                  </p>
                </div>
              </div>

              <div className="flex items-start space-x-3">
                <input
                  type="checkbox"
                  id="acceptItemSponsors"
                  checked={acceptItemSponsors}
                  onChange={(e) => onAcceptItemSponsorsChange(e.target.checked)}
                  className="mt-1 h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                />
                <div>
                  <label htmlFor="acceptItemSponsors" className="text-sm font-medium text-gray-700">
                    Accept item-based sponsorships
                  </label>
                  <p className="text-xs text-gray-500 mt-0.5">
                    Sponsors can commit to providing items, services, or in-kind contributions
                  </p>
                </div>
              </div>

              {showTypeWarning && (
                <p className="text-sm text-red-600 font-medium">
                  Must accept at least one sponsor type (monetary or item-based)
                </p>
              )}
            </div>

            {/* Min Sponsor Amount (only when money sponsors enabled) */}
            {acceptMoneySponsors && (
              <div className="space-y-2">
                <label htmlFor="minSponsorAmount" className="block text-sm font-medium text-gray-700">
                  Minimum Sponsorship Amount (optional)
                </label>
                <div className="relative max-w-xs">
                  <span className="absolute left-3 top-1/2 -translate-y-1/2 text-neutral-500 text-sm">$</span>
                  <Input
                    id="minSponsorAmount"
                    type="number"
                    min="1.00"
                    step="0.01"
                    value={minSponsorAmount ?? ''}
                    onChange={(e) => {
                      const val = e.target.value;
                      onMinSponsorAmountChange(val ? parseFloat(val) : null);
                    }}
                    placeholder="e.g. 50.00"
                    className="pl-7 text-sm"
                  />
                </div>
              </div>
            )}

            {/* Phase 6A.143 — Sponsor banner image upload. Only available when the
                config has been server-confirmed enabled (eventId provided) — endpoint
                rejects upload otherwise. New-event flow: save with sponsors enabled
                first, then come back to upload a banner. */}
            {eventId && (
              <SponsorBannerField eventId={eventId} imageUrl={sponsorImageUrl} />
            )}
            {!eventId && (
              <p className="text-xs text-gray-500 italic">
                Save the event with sponsorships enabled, then you can upload a sponsor banner image.
              </p>
            )}

            {/* Sponsor Message */}
            <div className="space-y-2">
              <label htmlFor="sponsorMessage" className="block text-sm font-medium text-gray-700">
                Sponsor Message (optional)
              </label>
              <textarea
                id="sponsorMessage"
                value={sponsorMessage}
                onChange={(e) => onSponsorMessageChange(e.target.value)}
                placeholder="Support our event as a sponsor! Your generous contribution helps make this possible..."
                maxLength={500}
                rows={2}
                className="w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
              <p className="text-xs text-gray-500">{sponsorMessage.length}/500 characters</p>
            </div>

            {/* Show Sponsor List */}
            <div className="flex items-start space-x-3">
              <input
                type="checkbox"
                id="showSponsorList"
                checked={showSponsorList}
                onChange={(e) => onShowSponsorListChange(e.target.checked)}
                className="mt-1 h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
              />
              <div>
                <label htmlFor="showSponsorList" className="text-sm font-medium text-gray-700">
                  Display sponsor list publicly on event page
                </label>
                <p className="text-xs text-gray-500 mt-0.5">
                  Show sponsors and their contributions to all event visitors
                </p>
              </div>
            </div>
        </div>
      )}
    </div>
  );
}
