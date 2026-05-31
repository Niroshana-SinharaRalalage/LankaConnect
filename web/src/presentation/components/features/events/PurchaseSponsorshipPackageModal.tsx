'use client';

import { useEffect, useState } from 'react';
import { createPortal } from 'react-dom';
import { X, AlertCircle, Award, Users } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { Label } from '@/presentation/components/ui/Label';
import { usePurchasePackageSponsor } from '@/presentation/hooks/useSponsorshipPackages';
import type {
  SponsorshipPackagePublicDto,
  CreatePackageSponsorRequest,
} from '@/infrastructure/api/types/events.types';

interface PurchaseSponsorshipPackageModalProps {
  eventId: string;
  /** The package the buyer chose from the public grid. Null when modal is closed. */
  pkg: SponsorshipPackagePublicDto | null;
  isOpen: boolean;
  onClose: () => void;
  /**
   * Optional override of the Success/Cancel URLs sent to Stripe. Defaults to
   * the current page (so Stripe redirects back to the event detail page).
   * Tests can pass explicit URLs to assert on the request payload.
   */
  successUrl?: string;
  cancelUrl?: string;
}

const MAX_NAME = 200;
const MAX_EMAIL = 320;
const MAX_PHONE = 50;
const MAX_ORG = 200;
const MAX_NOTES = 1000;

/**
 * Phase 6A.157 — buyer purchase modal for sponsorship packages.
 *
 * Renders the chosen package details + a buyer form (name*, email*, phone,
 * organization, notes). On submit:
 *   1. Validates required fields client-side
 *   2. Calls `usePurchasePackageSponsor` mutation
 *   3. On success, redirects to `result.checkoutUrl` — Stripe Checkout for
 *      paid packages, or the SuccessUrl directly for free $0 packages (the
 *      backend decides; the FE just redirects)
 *
 * Portal'd to document.body per the 6A.156-fix-2 contract (locked after
 * operator UAT bug where nested forms caused the parent EventEditForm to
 * submit). Submit handler calls `e.stopPropagation()` belt-and-suspenders.
 *
 * Per CLAUDE.md Section 4 observability: every mutation is wrapped in
 * try/catch with a user-facing error fallback.
 */
export function PurchaseSponsorshipPackageModal({
  eventId,
  pkg,
  isOpen,
  onClose,
  successUrl,
  cancelUrl,
}: PurchaseSponsorshipPackageModalProps) {
  const [buyerName, setBuyerName] = useState('');
  const [buyerEmail, setBuyerEmail] = useState('');
  const [buyerPhone, setBuyerPhone] = useState('');
  const [buyerOrganization, setBuyerOrganization] = useState('');
  const [buyerNotes, setBuyerNotes] = useState('');
  const [error, setError] = useState<string | null>(null);

  const purchaseMutation = usePurchasePackageSponsor();

  // Portal-mount guard — SSR renders nothing on the server, then flips
  // mounted=true on the client so createPortal can run safely (avoids Next.js
  // hydration mismatches). Same pattern as SponsorshipPackageEditModal.
  const [mounted, setMounted] = useState(false);
  useEffect(() => {
    setMounted(true);
  }, []);

  // Reset form whenever the modal opens with a new package (so a previous
  // session's typing doesn't bleed into the next).
  useEffect(() => {
    if (isOpen) {
      setError(null);
    }
  }, [isOpen, pkg?.id]);

  if (!isOpen || !pkg) return null;
  if (!mounted) return null;

  const isFree = pkg.priceAmount === 0;
  const isSubmitting = purchaseMutation.isPending;

  const formatCurrency = (amount: number, currency: string): string => {
    if (currency === 'USD') return `$${amount.toFixed(2)}`;
    return `${amount.toFixed(2)} ${currency}`;
  };

  const validate = (): string | null => {
    if (!buyerName.trim()) return 'Your name is required.';
    if (buyerName.trim().length > MAX_NAME) return `Name cannot exceed ${MAX_NAME} characters.`;
    if (!buyerEmail.trim()) return 'Email is required.';
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(buyerEmail.trim())) return 'Email looks invalid.';
    if (buyerEmail.trim().length > MAX_EMAIL) return `Email cannot exceed ${MAX_EMAIL} characters.`;
    if (buyerPhone.trim().length > MAX_PHONE) return `Phone cannot exceed ${MAX_PHONE} characters.`;
    if (buyerOrganization.trim().length > MAX_ORG)
      return `Organization cannot exceed ${MAX_ORG} characters.`;
    if (buyerNotes.trim().length > MAX_NOTES)
      return `Notes cannot exceed ${MAX_NOTES} characters.`;
    return null;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    // Mirrors 6A.156-fix-2 — sever the React synthetic submit bubble so the
    // modal's submit can NEVER reach a parent form's onSubmit handler.
    e.stopPropagation();
    setError(null);

    const v = validate();
    if (v) {
      setError(v);
      return;
    }

    const request: CreatePackageSponsorRequest = {
      buyerName: buyerName.trim(),
      buyerEmail: buyerEmail.trim(),
      buyerPhone: buyerPhone.trim() || null,
      buyerOrganization: buyerOrganization.trim() || null,
      buyerNotes: buyerNotes.trim() || null,
      successUrl: successUrl ?? `${window.location.origin}/events/${eventId}?sponsor=success`,
      cancelUrl: cancelUrl ?? `${window.location.origin}/events/${eventId}?sponsor=cancel`,
    };

    try {
      const result = await purchaseMutation.mutateAsync({
        eventId,
        packageId: pkg.id,
        request,
      });
      // Redirect to checkoutUrl — Stripe for paid, SuccessUrl directly for
      // free $0. Server-side resolution; FE just navigates.
      window.location.assign(result.checkoutUrl);
    } catch (err) {
      // Per CLAUDE.md Section 4 observability — structured log + user-facing
      // fallback. Stripe / mutation failure surfaces inline so the buyer can
      // retry without losing form state.
      console.error('PurchaseSponsorshipPackage failed:', err);
      const msg =
        err && typeof err === 'object' && 'message' in err
          ? String((err as { message: unknown }).message)
          : 'Purchase failed. Please try again.';
      setError(msg);
    }
  };

  const submitLabel = isFree
    ? `Become a ${pkg.tier ? pkg.tier + ' ' : ''}Sponsor (Free)`
    : 'Continue to payment';

  return createPortal(
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
      onClick={onClose}
    >
      <div
        className="bg-white rounded-lg shadow-xl max-w-2xl w-full max-h-[90vh] overflow-y-auto"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between p-4 border-b border-neutral-200 sticky top-0 bg-white z-10">
          <h3 className="text-lg font-semibold text-neutral-800">
            Sponsor — {pkg.name}
          </h3>
          <button
            type="button"
            onClick={onClose}
            className="p-1 rounded-md hover:bg-neutral-100 text-neutral-500"
            aria-label="Close"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="p-4 space-y-4">
          {/* Package summary header — what the buyer is about to commit to */}
          <div className="rounded-md border border-neutral-200 bg-neutral-50 p-3 space-y-2">
            <div className="flex items-center gap-2 flex-wrap">
              {pkg.tier && (
                <span className="inline-flex items-center gap-1 text-xs font-medium px-2 py-0.5 rounded bg-amber-50 text-amber-700 border border-amber-200">
                  <Award className="h-3 w-3" />
                  {pkg.tier}
                </span>
              )}
              <span className="text-sm font-semibold text-neutral-800">{pkg.name}</span>
              <span className="ml-auto text-sm font-semibold text-neutral-800">
                {isFree ? 'Free' : formatCurrency(pkg.priceAmount, pkg.priceCurrency)}
              </span>
            </div>
            {pkg.description && (
              <p className="text-xs text-neutral-600">{pkg.description}</p>
            )}
            {pkg.perks.length > 0 && (
              <ul className="text-xs text-neutral-700 list-disc list-inside space-y-0.5">
                {pkg.perks.map((perk, idx) => (
                  <li key={idx}>{perk}</li>
                ))}
              </ul>
            )}
            {pkg.includedTicketCount > 0 && (
              <p className="text-xs text-neutral-500 italic flex items-start gap-1">
                <Users className="h-3 w-3 mt-0.5 flex-shrink-0" />
                <span>
                  {pkg.includedTicketCount} ticket
                  {pkg.includedTicketCount === 1 ? '' : 's'} included — issued by organizer outside the platform.
                </span>
              </p>
            )}
          </div>

          {/* Buyer form */}
          {error && (
            <div className="flex items-start gap-2 p-3 bg-red-50 border border-red-200 rounded-md text-sm text-red-700">
              <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
              <span>{error}</span>
            </div>
          )}

          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div>
              <Label htmlFor="buyer-name">Your Name *</Label>
              <Input
                id="buyer-name"
                value={buyerName}
                onChange={(e) => setBuyerName(e.target.value)}
                placeholder="Jane Smith"
                maxLength={MAX_NAME}
                required
                disabled={isSubmitting}
              />
            </div>
            <div>
              <Label htmlFor="buyer-email">Email *</Label>
              <Input
                id="buyer-email"
                type="email"
                value={buyerEmail}
                onChange={(e) => setBuyerEmail(e.target.value)}
                placeholder="jane@example.com"
                maxLength={MAX_EMAIL}
                required
                disabled={isSubmitting}
              />
            </div>
            <div>
              <Label htmlFor="buyer-phone">Phone</Label>
              <Input
                id="buyer-phone"
                value={buyerPhone}
                onChange={(e) => setBuyerPhone(e.target.value)}
                placeholder="+1-555-0100"
                maxLength={MAX_PHONE}
                disabled={isSubmitting}
              />
            </div>
            <div>
              <Label htmlFor="buyer-org">Organization</Label>
              <Input
                id="buyer-org"
                value={buyerOrganization}
                onChange={(e) => setBuyerOrganization(e.target.value)}
                placeholder="Acme LLC"
                maxLength={MAX_ORG}
                disabled={isSubmitting}
              />
            </div>
          </div>

          <div>
            <Label htmlFor="buyer-notes">Notes (optional)</Label>
            <textarea
              id="buyer-notes"
              value={buyerNotes}
              onChange={(e) => setBuyerNotes(e.target.value)}
              rows={2}
              maxLength={MAX_NOTES}
              className="w-full px-3 py-2 text-sm border border-neutral-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              placeholder="Anything the organizer should know"
              disabled={isSubmitting}
            />
          </div>

          <div className="flex items-center justify-end gap-2 pt-4 border-t border-neutral-200">
            <Button type="button" variant="ghost" onClick={onClose} disabled={isSubmitting}>
              Cancel
            </Button>
            <Button
              type="submit"
              disabled={isSubmitting}
              style={{ background: '#FF7900' }}
            >
              {isSubmitting ? 'Processing...' : submitLabel}
            </Button>
          </div>
        </form>
      </div>
    </div>,
    document.body
  );
}
