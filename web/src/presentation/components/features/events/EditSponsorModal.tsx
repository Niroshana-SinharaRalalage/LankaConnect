'use client';

import { useEffect, useState } from 'react';
import { X, Save, Loader2 } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import {
  useUpdateSponsor,
  useUploadSponsorImage,
  useDeleteSponsorImage,
} from '@/presentation/hooks/useSponsors';
import type {
  SponsorDto,
  UpdateSponsorRequest,
} from '@/infrastructure/api/types/events.types';
import { SponsorImagePicker } from './SponsorImagePicker';

interface EditSponsorModalProps {
  eventId: string;
  sponsor: SponsorDto | null;
  /** True when invoked from the organizer's SponsorsManagementTab; false when from sponsor-self */
  isOrganizer: boolean;
  open: boolean;
  onClose: () => void;
  onSaved?: (updated: SponsorDto) => void;
}

/**
 * Phase 6A.151 — shared modal for editing an existing sponsor. Used by:
 *   - SponsorsManagementTab row-action (isOrganizer=true)
 *   - SponsorSection "Your Sponsorships" row (isOrganizer=false)
 *
 * The component shows/hides fields per the state-edit matrix. Backend enforces
 * the same matrix (Sponsor.UpdateXxx) — the FE just disables disallowed inputs
 * so the user doesn't get surprised by a 400 after clicking Save.
 *
 * Matrix (FE-mirror of domain rules):
 *                            | Notes | Org | Amount   | Item* | Name |
 *   Pending Money            |  ✓    |  ✓  |   🚫    |  n/a  |  ✓  |
 *   Completed Stripe         |  ✓    |  ✓  |   🚫    |  n/a  |  ✓  |
 *   Completed off-platform   |  ✓    |  ✓  | 👤(org) |  n/a  |  ✓  |
 *   RecordedItem             |  ✓    |  ✓  |  n/a    |  ✓    |  ✓  |
 *   Failed/Abandoned/Refunded| 👤    |  🚫 |   🚫    |  🚫   |  🚫 |
 *
 * Image edits ride the existing POST/DELETE /sponsors/{id}/image endpoints
 * (out of scope for this modal — handled separately via the row image cell on
 * SponsorsManagementTab).
 */
export function EditSponsorModal({
  eventId,
  sponsor,
  isOrganizer,
  open,
  onClose,
  onSaved,
}: EditSponsorModalProps) {
  const [name, setName] = useState('');
  const [notes, setNotes] = useState('');
  const [organization, setOrganization] = useState('');
  const [amount, setAmount] = useState('');
  const [itemName, setItemName] = useState('');
  const [itemDescription, setItemDescription] = useState('');
  const [estimatedValue, setEstimatedValue] = useState('');
  // Phase 6A.151 C6 — image edit state. `imageFile` is a NEW file the user
  // queued (will POST /image on Save). `removeExisting` is a flag set when
  // the user clicked "Remove" on the existing server-side image (will DELETE
  // /image on Save). They are mutually exclusive — picking a file after a
  // remove cancels the remove.
  const [imageFile, setImageFile] = useState<File | null>(null);
  const [removeExistingImage, setRemoveExistingImage] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const updateSponsor = useUpdateSponsor();
  const uploadImage = useUploadSponsorImage();
  const deleteImage = useDeleteSponsorImage();

  // Reset form when sponsor changes / modal opens
  useEffect(() => {
    if (!sponsor) return;
    setName(sponsor.sponsorName ?? '');
    setNotes(sponsor.sponsorNotes ?? '');
    setOrganization(sponsor.sponsorOrganization ?? '');
    setAmount(sponsor.amount != null ? String(sponsor.amount) : '');
    setItemName(sponsor.itemName ?? '');
    setItemDescription(sponsor.itemDescription ?? '');
    setEstimatedValue(sponsor.estimatedValue != null ? String(sponsor.estimatedValue) : '');
    setImageFile(null);
    setRemoveExistingImage(false);
    setError(null);
  }, [sponsor?.id, open]);

  // Mutual-exclusion: picking a new file cancels a pending remove-existing.
  const handleImageChange = (file: File | null) => {
    setImageFile(file);
    if (file !== null) setRemoveExistingImage(false);
  };

  const handleRemoveExisting = () => {
    setRemoveExistingImage(true);
    setImageFile(null);
  };

  if (!open || !sponsor) return null;

  const isMoney = sponsor.sponsorType === 'Money';
  const isItem = sponsor.sponsorType === 'Item' || sponsor.sponsorType === 'RecordedItem';
  const status = sponsor.status;

  // Derive per-field "can edit" flags from the matrix.
  const isPendingMoney = isMoney && status === 'Pending';
  const isStripeCompleted = isMoney && status === 'Completed' && !!sponsor.paymentCompletedAt && !!sponsor.stripeFeeAmount;
  // off-platform completed = Completed money sponsor that lacks Stripe-side fields.
  const isOffPlatformCompleted = isMoney && status === 'Completed' && !sponsor.stripeFeeAmount;
  const isRecordedItem = isItem && status === 'RecordedItem';
  const isTerminal = status === 'Failed' || status === 'Abandoned' || status === 'Refunded';

  // Notes: editable on non-terminal; organizer-only notes on terminal.
  const canEditNotes = !isTerminal || isOrganizer;
  // Organization, Name: editable on non-terminal only. Terminal = locked.
  const canEditOrganization = !isTerminal;
  const canEditName = !isTerminal;
  // Amount: only off-platform-completed money sponsors AND organizer.
  const canEditAmount = isOffPlatformCompleted && isOrganizer;
  // Item details: RecordedItem only.
  const canEditItemDetails = isRecordedItem;
  // Image: editable on every state EXCEPT terminal. Existing image stays visible
  // in read-only mode on terminal so organizers can see what was there.
  const canEditImage = !isTerminal;

  const handleSave = async () => {
    setError(null);
    setSaving(true);
    try {
      // PATCH semantics: only send fields that changed and that the user can edit.
      const patch: UpdateSponsorRequest = {};
      if (canEditName && name.trim() !== (sponsor.sponsorName ?? '')) patch.name = name.trim();
      if (canEditNotes && notes !== (sponsor.sponsorNotes ?? '')) patch.notes = notes.trim() || null;
      if (canEditOrganization && organization !== (sponsor.sponsorOrganization ?? '')) {
        patch.organization = organization.trim() || null;
      }
      if (canEditAmount && amount !== (sponsor.amount != null ? String(sponsor.amount) : '')) {
        const parsed = parseFloat(amount);
        if (!Number.isFinite(parsed) || parsed <= 0) {
          setError('Amount must be a positive number');
          setSaving(false);
          return;
        }
        patch.amount = parsed;
        patch.currency = sponsor.currency ?? 'USD';
      }
      if (canEditItemDetails) {
        if (itemName !== (sponsor.itemName ?? '')) patch.itemName = itemName.trim() || null;
        if (itemDescription !== (sponsor.itemDescription ?? '')) {
          patch.itemDescription = itemDescription.trim() || null;
        }
        if (estimatedValue !== (sponsor.estimatedValue != null ? String(sponsor.estimatedValue) : '')) {
          const parsed = estimatedValue === '' ? null : parseFloat(estimatedValue);
          if (parsed != null && (!Number.isFinite(parsed) || parsed < 0)) {
            setError('Estimated value cannot be negative');
            setSaving(false);
            return;
          }
          patch.estimatedValue = parsed;
        }
      }

      const hasImageChange = canEditImage && (imageFile !== null || removeExistingImage);
      if (Object.keys(patch).length === 0 && !hasImageChange) {
        setError('No changes to save');
        setSaving(false);
        return;
      }

      // Phase 6A.151 C6 — sequential save: PATCH text first, then image
      // upload/delete. Each step has its own network call so a failure
      // doesn't strand the other. React Query invalidation in the hooks
      // refreshes the table after the chain completes.
      let updated: SponsorDto = sponsor;
      if (Object.keys(patch).length > 0) {
        updated = await updateSponsor.mutateAsync({
          eventId,
          sponsorId: sponsor.id,
          request: patch,
        });
      }

      if (hasImageChange) {
        if (imageFile) {
          try {
            await uploadImage.mutateAsync({
              eventId,
              sponsorId: sponsor.id,
              file: imageFile,
            });
          } catch (imgErr) {
            // eslint-disable-next-line no-console
            console.error('[EditSponsorModal] image upload failed:', imgErr);
            setError('Saved text fields, but the image upload failed. Try again.');
            setSaving(false);
            return;
          }
        } else if (removeExistingImage) {
          try {
            await deleteImage.mutateAsync({
              eventId,
              sponsorId: sponsor.id,
            });
          } catch (imgErr) {
            // eslint-disable-next-line no-console
            console.error('[EditSponsorModal] image delete failed:', imgErr);
            setError('Saved text fields, but the image remove failed. Try again.');
            setSaving(false);
            return;
          }
        }
      }

      onSaved?.(updated);
      onClose();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to save changes';
      setError(msg);
      // eslint-disable-next-line no-console
      console.error('[EditSponsorModal] save failed:', err);
    } finally {
      setSaving(false);
    }
  };

  const handleClose = () => {
    if (saving) return;
    onClose();
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
      data-testid="edit-sponsor-modal"
      role="dialog"
      aria-modal="true"
      aria-labelledby="edit-sponsor-title"
    >
      <div className="w-full max-w-2xl rounded-lg bg-white shadow-xl max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between border-b border-neutral-200 p-4">
          <h2 id="edit-sponsor-title" className="text-lg font-semibold text-neutral-900">
            Edit sponsorship
          </h2>
          <button
            type="button"
            onClick={handleClose}
            disabled={saving}
            className="text-neutral-400 hover:text-neutral-600 disabled:opacity-50"
            aria-label="Close"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        <div className="space-y-4 p-4">
          <p className="text-sm text-neutral-600">
            Type: <span className="font-medium text-neutral-800">{sponsor.sponsorType}</span>
            {' · '}
            Status: <span className="font-medium text-neutral-800">{status}</span>
            {isTerminal && (
              <span className="ml-2 text-xs text-neutral-500">
                (terminal state — only organizers can annotate notes)
              </span>
            )}
          </p>

          {/* Name */}
          <div>
            <label className="block text-xs font-medium text-neutral-600 mb-1">Sponsor name</label>
            <Input
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Sponsor name"
              disabled={!canEditName || saving}
            />
          </div>

          {/* Organization */}
          <div>
            <label className="block text-xs font-medium text-neutral-600 mb-1">Organization (optional)</label>
            <Input
              value={organization}
              onChange={(e) => setOrganization(e.target.value)}
              placeholder="Company or organization name"
              disabled={!canEditOrganization || saving}
            />
          </div>

          {/* Phase 6A.151 C6 — image upload / replace / remove.
              Hidden on terminal states (Failed/Abandoned/Refunded) since the
              image is tied to the original transaction. */}
          {canEditImage && (
            <SponsorImagePicker
              value={imageFile}
              onChange={handleImageChange}
              existingImageUrl={removeExistingImage ? null : sponsor.imageUrl}
              onRemoveExisting={sponsor.imageUrl && !removeExistingImage ? handleRemoveExisting : undefined}
              disabled={saving}
              helperText={
                removeExistingImage
                  ? 'Existing image will be removed when you click Save changes.'
                  : imageFile
                    ? 'New image will be uploaded when you click Save changes.'
                    : undefined
              }
            />
          )}
          {!canEditImage && sponsor.imageUrl && (
            <div>
              <label className="block text-xs font-medium text-neutral-600 mb-1">Current logo</label>
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img
                src={sponsor.imageUrl}
                alt="Sponsor logo"
                className="h-16 w-16 rounded object-contain bg-white border border-neutral-200"
              />
              <p className="text-xs text-neutral-500 mt-1">
                Image is locked because this sponsorship is in a terminal state.
              </p>
            </div>
          )}

          {/* Notes */}
          <div>
            <label className="block text-xs font-medium text-neutral-600 mb-1">Notes (optional)</label>
            <textarea
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder="Internal notes or sponsor note"
              disabled={!canEditNotes || saving}
              className="w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 disabled:bg-neutral-50 disabled:text-neutral-500"
              rows={3}
            />
          </div>

          {/* Money — amount (only editable for off-platform completed money + organizer) */}
          {isMoney && (
            <div>
              <label className="block text-xs font-medium text-neutral-600 mb-1">
                Amount ({sponsor.currency ?? 'USD'})
              </label>
              <div className="relative">
                <span className="absolute left-3 top-1/2 -translate-y-1/2 text-neutral-500">$</span>
                <Input
                  type="number"
                  min="0.01"
                  step="0.01"
                  value={amount}
                  onChange={(e) => setAmount(e.target.value)}
                  disabled={!canEditAmount || saving}
                  className="pl-7"
                />
              </div>
              {!canEditAmount && (
                <p className="text-xs text-neutral-500 mt-1">
                  {isPendingMoney
                    ? 'Amount cannot change on a Pending sponsorship — the Stripe checkout would be stale.'
                    : isStripeCompleted
                      ? 'Stripe-completed amount edits are not supported yet (top-up / partial-refund flow pending).'
                      : isTerminal
                        ? 'Terminal state — amount is locked.'
                        : 'Only an organizer can change the amount on an off-platform sponsorship.'}
                </p>
              )}
            </div>
          )}

          {/* Item details — only RecordedItem */}
          {isRecordedItem && (
            <>
              <div>
                <label className="block text-xs font-medium text-neutral-600 mb-1">Item name</label>
                <Input
                  value={itemName}
                  onChange={(e) => setItemName(e.target.value)}
                  disabled={!canEditItemDetails || saving}
                />
              </div>
              <div>
                <label className="block text-xs font-medium text-neutral-600 mb-1">
                  Item description (optional)
                </label>
                <textarea
                  value={itemDescription}
                  onChange={(e) => setItemDescription(e.target.value)}
                  disabled={!canEditItemDetails || saving}
                  className="w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 disabled:bg-neutral-50 disabled:text-neutral-500"
                  rows={2}
                />
              </div>
              <div>
                <label className="block text-xs font-medium text-neutral-600 mb-1">
                  Estimated value (optional)
                </label>
                <div className="relative">
                  <span className="absolute left-3 top-1/2 -translate-y-1/2 text-neutral-500">$</span>
                  <Input
                    type="number"
                    min="0"
                    step="0.01"
                    value={estimatedValue}
                    onChange={(e) => setEstimatedValue(e.target.value)}
                    disabled={!canEditItemDetails || saving}
                    className="pl-7"
                  />
                </div>
              </div>
            </>
          )}

          {error && (
            <p className="text-sm text-red-600 bg-red-50 border border-red-200 rounded-md px-3 py-2">
              {error}
            </p>
          )}
        </div>

        <div className="flex justify-end gap-2 border-t border-neutral-200 p-4">
          <Button variant="outline" onClick={handleClose} disabled={saving}>
            Cancel
          </Button>
          <Button onClick={handleSave} disabled={saving}>
            {saving ? (
              <>
                <Loader2 className="h-4 w-4 mr-1 animate-spin" /> Saving…
              </>
            ) : (
              <>
                <Save className="h-4 w-4 mr-1" /> Save changes
              </>
            )}
          </Button>
        </div>
      </div>
    </div>
  );
}
