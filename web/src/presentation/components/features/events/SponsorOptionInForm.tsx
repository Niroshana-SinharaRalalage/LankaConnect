'use client';

import { useState } from 'react';
import { Award, Loader2 } from 'lucide-react';
import { Input } from '@/presentation/components/ui/Input';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import type { SponsorConfigurationDto } from '@/infrastructure/api/types/events.types';
import { SponsorImagePicker } from './SponsorImagePicker';

interface SponsorOptionInFormProps {
  eventId: string;
  sponsorConfig: SponsorConfigurationDto;
  onSponsorChange: (amount: number | null, organization: string | null, notes: string | null) => void;
  /**
   * Phase 6A.151 C7 — emitted after the user picks a logo and the file
   * successfully pre-uploads to the staging-blob endpoint. Both args null
   * when the user clears the picker or the upload fails.
   */
  onStagingBlobChange?: (blobName: string | null, blobUrl: string | null) => void;
}

/**
 * Lightweight money sponsorship option shown inside the registration form.
 * Allows attendees to add a monetary sponsorship during event registration (combined checkout).
 * Phase 6A.137E: Follows the DonationOptionInForm pattern.
 * Phase 6A.151 C7: Optional logo upload — file is pre-staged to the backend
 *   on file-pick (since the registration submit triggers a Stripe Checkout
 *   redirect, there is no post-submit window to upload). Handler attaches
 *   the staged blob to the Sponsor row in-tx with ticket purchase.
 * Note: Only money sponsors can be bundled. Item sponsors remain standalone.
 */
export function SponsorOptionInForm({
  eventId,
  sponsorConfig,
  onSponsorChange,
  onStagingBlobChange,
}: SponsorOptionInFormProps) {
  const [amount, setAmount] = useState('');
  const [organization, setOrganization] = useState('');
  const [notes, setNotes] = useState('');
  // Phase 6A.137F: Visible validation error instead of silent nulling (Bug 2)
  const [validationError, setValidationError] = useState<string | null>(null);
  // Phase 6A.151 C7 — image pre-staging state.
  const [imageFile, setImageFile] = useState<File | null>(null);
  const [uploadingImage, setUploadingImage] = useState(false);
  const [imageError, setImageError] = useState<string | null>(null);

  const handleImageChange = async (file: File | null) => {
    setImageError(null);
    if (file === null) {
      setImageFile(null);
      onStagingBlobChange?.(null, null);
      return;
    }
    setImageFile(file);
    setUploadingImage(true);
    try {
      const result = await eventsRepository.uploadSponsorStagingImage(eventId, file);
      onStagingBlobChange?.(result.blobName, result.blobUrl);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Logo upload failed';
      // eslint-disable-next-line no-console
      console.error('[SponsorOptionInForm] staging-image upload failed:', err);
      setImageError(`${msg}. Registration will continue without a logo.`);
      // Clear so the form doesn't submit a stale staged file
      setImageFile(null);
      onStagingBlobChange?.(null, null);
    } finally {
      setUploadingImage(false);
    }
  };

  const handleAmountChange = (value: string) => {
    setAmount(value);
    const parsed = parseFloat(value);
    const effectiveAmount = parsed > 0 ? parsed : null;

    // Phase 6A.137F: Show visible error when amount is below minimum instead of silently nulling
    if (effectiveAmount !== null && sponsorConfig.minSponsorAmount && effectiveAmount < sponsorConfig.minSponsorAmount) {
      setValidationError(`Minimum sponsorship amount is $${sponsorConfig.minSponsorAmount.toFixed(2)}`);
      onSponsorChange(null, organization || null, notes || null);
      return;
    }

    setValidationError(null);
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
            className={`pl-7 text-sm h-9 ${validationError ? 'border-red-500 focus:ring-red-500' : ''}`}
          />
        </div>
        {/* Phase 6A.137F: Visible validation error message */}
        {validationError && (
          <p className="text-xs text-red-600 mt-1">{validationError}</p>
        )}

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

        {/* Phase 6A.151 C7 — optional logo upload */}
        <div className="pt-2">
          <SponsorImagePicker
            value={imageFile}
            onChange={handleImageChange}
            disabled={uploadingImage}
            label="Sponsor logo (optional)"
            helperText={
              uploadingImage
                ? undefined
                : imageFile
                  ? 'Logo will be attached when payment completes.'
                  : 'Upload your organization logo. JPEG/PNG/WebP, max 5 MB.'
            }
          />
          {uploadingImage && (
            <p className="mt-1 flex items-center gap-1 text-xs text-amber-700">
              <Loader2 className="h-3 w-3 animate-spin" />
              Uploading logo…
            </p>
          )}
          {imageError && <p className="mt-1 text-xs text-red-600">{imageError}</p>}
        </div>
      </div>
    </div>
  );
}
