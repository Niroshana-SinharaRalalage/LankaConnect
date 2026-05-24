'use client';

import { useState } from 'react';
import { X, AlertCircle } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import type {
  RefundLineItemInput,
  RefundCurrency,
} from '@/infrastructure/api/types/refund-request.types';

interface RequestRefundDialogProps {
  open: boolean;
  eventId: string;
  /**
   * Pre-built line items for the user's registration. Caller is responsible for
   * sourcing these from the user's payment history (Ticket = RegistrationPayment.Id,
   * AddOn = AddOnPurchase.Id, Collection = Collection.Id, Sponsor = Sponsor.Id).
   * For MVP the attendee page passes just the Ticket line.
   */
  availableLines: RefundLineItemInput[];
  onClose: () => void;
  onSubmitted: () => void;
}

/**
 * Phase 6A.148 — modal for the attendee to submit a refund request.
 *
 * Per-line checkboxes default to all-selected; attendee uncomments lines they
 * don't want to request. Free-text reason is optional. The organizer makes the
 * final call on per-line amounts at approval time.
 */
export function RequestRefundDialog({
  open,
  eventId,
  availableLines,
  onClose,
  onSubmitted,
}: RequestRefundDialogProps) {
  const [selected, setSelected] = useState<Set<string>>(
    () => new Set(availableLines.map((li) => `${li.type}:${li.referenceId}`)),
  );
  const [reason, setReason] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (!open) return null;

  const handleToggle = (key: string) => {
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      return next;
    });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    const chosen = availableLines.filter((li) =>
      selected.has(`${li.type}:${li.referenceId}`),
    );

    if (chosen.length === 0) {
      setError('Please select at least one item to request a refund for.');
      return;
    }

    setIsSubmitting(true);
    try {
      await eventsRepository.createRefundRequest(eventId, {
        requesterReason: reason.trim() || null,
        lineItems: chosen,
      });
      onSubmitted();
      onClose();
    } catch (err) {
      console.error('[RequestRefundDialog] create failed:', err);
      const msg =
        err instanceof Error
          ? err.message
          : 'Failed to submit refund request. Please try again.';
      setError(msg);
    } finally {
      setIsSubmitting(false);
    }
  };

  const totalRequested = availableLines
    .filter((li) => selected.has(`${li.type}:${li.referenceId}`))
    .reduce((sum, li) => sum + li.requestedAmount, 0);

  // Currency picked from first available line (assumes single currency per registration).
  const currency: RefundCurrency = availableLines[0]?.currency ?? 'USD';

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
      data-testid="request-refund-dialog"
    >
      <div className="w-full max-w-lg rounded-lg bg-white shadow-xl max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between border-b border-neutral-200 p-4">
          <h2 className="text-lg font-semibold text-neutral-900">Request Refund</h2>
          <button
            type="button"
            onClick={onClose}
            disabled={isSubmitting}
            className="text-neutral-400 hover:text-neutral-600"
            aria-label="Close"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4 p-4">
          <p className="text-sm text-neutral-600">
            Your refund request will be reviewed by the event organizer. No money is
            moved until they approve.
          </p>

          {availableLines.length > 0 ? (
            <div className="space-y-2">
              <p className="text-xs font-semibold text-neutral-700 uppercase tracking-wide">
                Items to refund
              </p>
              {availableLines.map((li) => {
                const key = `${li.type}:${li.referenceId}`;
                return (
                  <label
                    key={key}
                    className="flex items-center justify-between gap-3 rounded-md border border-neutral-200 p-3 hover:bg-neutral-50 cursor-pointer"
                  >
                    <div className="flex items-center gap-3 min-w-0">
                      <input
                        type="checkbox"
                        checked={selected.has(key)}
                        onChange={() => handleToggle(key)}
                        className="h-4 w-4 rounded border-neutral-300 text-blue-600 focus:ring-blue-500"
                      />
                      <span className="text-sm text-neutral-900 truncate">{li.type}</span>
                    </div>
                    <span className="text-sm font-medium text-neutral-700">
                      {formatMoney(li.requestedAmount, li.currency)}
                    </span>
                  </label>
                );
              })}
            </div>
          ) : (
            <div className="rounded-md bg-neutral-50 p-3 text-sm text-neutral-600">
              No refundable charges found on your registration.
            </div>
          )}

          <div>
            <label className="block text-xs font-semibold text-neutral-700 uppercase tracking-wide mb-1">
              Reason (optional)
            </label>
            <textarea
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              maxLength={1000}
              rows={3}
              placeholder="Tell the organizer why you're requesting a refund."
              className="block w-full rounded-md border border-neutral-300 px-3 py-2 text-sm text-neutral-900 placeholder-neutral-400 focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
            />
            <p className="text-xs text-neutral-500 mt-1">{reason.length} / 1000</p>
          </div>

          <div className="rounded-md bg-blue-50 border border-blue-200 p-3 text-sm">
            <p className="font-medium text-blue-900">
              Total requested: {formatMoney(totalRequested, currency)}
            </p>
            <p className="text-blue-700 text-xs mt-1">
              The organizer may approve all, some, or none of this amount.
            </p>
          </div>

          {error && (
            <div className="flex items-start gap-2 rounded-md bg-red-50 border border-red-200 p-3">
              <AlertCircle className="h-5 w-5 text-red-600 flex-shrink-0 mt-0.5" />
              <p className="text-sm text-red-700">{error}</p>
            </div>
          )}

          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="outline" onClick={onClose} disabled={isSubmitting}>
              Cancel
            </Button>
            <Button type="submit" disabled={isSubmitting || availableLines.length === 0}>
              {isSubmitting ? 'Submitting…' : 'Submit Request'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}

function formatMoney(amount: number, currency: RefundCurrency): string {
  try {
    return new Intl.NumberFormat(undefined, { style: 'currency', currency }).format(amount);
  } catch {
    return `${amount.toFixed(2)} ${currency}`;
  }
}
