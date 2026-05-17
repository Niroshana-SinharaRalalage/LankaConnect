'use client';

import { useMemo, useState } from 'react';
import { X, AlertCircle } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import type {
  OrganizerRefundRequestDto,
  ApproveLineItemInput,
} from '@/infrastructure/api/types/refund-request.types';

interface RefundApprovalDialogProps {
  open: boolean;
  eventId: string;
  request: OrganizerRefundRequestDto;
  onClose: () => void;
  onApproved: () => void;
}

/**
 * Phase 6A.148 — organizer approval dialog with per-line approved amounts.
 *
 * Each line defaults to its requested amount, with a checkbox to "include" the
 * line (un-ticking sets approved to zero, which the backend treats as a per-line
 * Rejected). At least one line must end up with a non-zero approved amount — the
 * backend enforces this (architect F2: all-zero approvals are 400; organizer
 * must use Reject for that).
 *
 * Concurrency: 409 Conflict from the backend means another organizer approved
 * first. We surface a clear message and ask the user to refresh.
 */
export function RefundApprovalDialog({
  open,
  eventId,
  request,
  onClose,
  onApproved,
}: RefundApprovalDialogProps) {
  type LineState = {
    lineItemId: string;
    type: string;
    requestedAmount: number;
    currency: string;
    included: boolean;
    approvedAmount: string;
  };

  const initialLines: LineState[] = useMemo(
    () =>
      request.lineItems.map((li) => ({
        lineItemId: li.id,
        type: li.type,
        requestedAmount: li.requestedAmount,
        currency: li.requestedCurrency,
        included: true,
        approvedAmount: li.requestedAmount.toFixed(2),
      })),
    [request.lineItems],
  );

  const [lines, setLines] = useState<LineState[]>(initialLines);
  const [organizerNotes, setOrganizerNotes] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (!open) return null;

  const updateLine = (lineItemId: string, patch: Partial<LineState>) => {
    setLines((prev) => prev.map((l) => (l.lineItemId === lineItemId ? { ...l, ...patch } : l)));
  };

  const totalApproved = lines.reduce((sum, l) => {
    if (!l.included) return sum;
    const n = parseFloat(l.approvedAmount);
    return sum + (Number.isFinite(n) && n > 0 ? n : 0);
  }, 0);

  const totalRequested = lines.reduce((sum, l) => sum + l.requestedAmount, 0);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    // Pre-validate per-line amounts: must be 0 ≤ approved ≤ requested.
    const errors: string[] = [];
    const payload: ApproveLineItemInput[] = [];

    for (const l of lines) {
      const approved = l.included ? parseFloat(l.approvedAmount) : 0;
      if (l.included) {
        if (!Number.isFinite(approved) || approved < 0) {
          errors.push(`Line ${l.type}: amount must be a number ≥ 0.`);
          continue;
        }
        if (approved > l.requestedAmount) {
          errors.push(
            `Line ${l.type}: ${approved.toFixed(2)} exceeds requested ${l.requestedAmount.toFixed(2)}.`,
          );
          continue;
        }
      }
      payload.push({
        lineItemId: l.lineItemId,
        approvedAmount: approved,
        // currency is a backend enum string; widen via cast at API boundary.
        currency: l.currency as ApproveLineItemInput['currency'],
      });
    }

    if (errors.length) {
      setError(errors.join(' '));
      return;
    }

    if (totalApproved <= 0) {
      setError(
        'At least one line must have a non-zero approved amount. To decline everything, use the Decline button instead.',
      );
      return;
    }

    setIsSubmitting(true);
    try {
      await eventsRepository.approveRefundRequest(eventId, request.id, {
        organizerNotes: organizerNotes.trim() || null,
        perLineApprovedAmounts: payload,
      });
      onApproved();
      onClose();
    } catch (err) {
      console.error('[RefundApprovalDialog] approve failed:', err);
      // The api-client maps backend ProblemDetails to Error.message.
      const msg = err instanceof Error ? err.message : 'Approval failed. Please try again.';
      // 409 surfaces as a generic Error; show a clearer line for it.
      if (/conflict/i.test(msg) || /already approved/i.test(msg) || msg.includes('409')) {
        setError(
          'Another organizer reviewed this request just now. Refresh the list and try again.',
        );
      } else {
        setError(msg);
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
      <div className="w-full max-w-2xl rounded-lg bg-white shadow-xl max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between border-b border-neutral-200 p-4">
          <h2 className="text-lg font-semibold text-neutral-900">Approve Refund Request</h2>
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
          {request.requesterReason && (
            <div className="rounded-md bg-neutral-50 border border-neutral-200 p-3">
              <p className="text-xs font-semibold text-neutral-600 uppercase tracking-wide mb-1">
                Attendee&apos;s reason
              </p>
              <p className="text-sm text-neutral-800 italic">
                &ldquo;{request.requesterReason}&rdquo;
              </p>
            </div>
          )}

          <div className="space-y-2">
            <p className="text-xs font-semibold text-neutral-700 uppercase tracking-wide">
              Lines to refund
            </p>
            <p className="text-xs text-neutral-500">
              Uncheck a line to reject it, or lower the amount for a partial refund.
            </p>

            {lines.map((l) => (
              <div
                key={l.lineItemId}
                className="grid grid-cols-12 items-center gap-3 rounded-md border border-neutral-200 p-3"
              >
                <div className="col-span-1">
                  <input
                    type="checkbox"
                    checked={l.included}
                    onChange={(e) => updateLine(l.lineItemId, { included: e.target.checked })}
                    className="h-4 w-4 rounded border-neutral-300 text-blue-600 focus:ring-blue-500"
                    aria-label={`Include ${l.type} in approval`}
                  />
                </div>
                <div className="col-span-4 text-sm text-neutral-900">{l.type}</div>
                <div className="col-span-3 text-xs text-neutral-500">
                  Requested: {formatMoney(l.requestedAmount, l.currency)}
                </div>
                <div className="col-span-4">
                  <Input
                    type="number"
                    step="0.01"
                    min="0"
                    max={l.requestedAmount}
                    value={l.approvedAmount}
                    onChange={(e) => updateLine(l.lineItemId, { approvedAmount: e.target.value })}
                    disabled={!l.included}
                    aria-label={`Approved amount for ${l.type}`}
                  />
                </div>
              </div>
            ))}
          </div>

          <div>
            <label className="block text-xs font-semibold text-neutral-700 uppercase tracking-wide mb-1">
              Internal notes (optional, not shown to attendee)
            </label>
            <textarea
              value={organizerNotes}
              onChange={(e) => setOrganizerNotes(e.target.value)}
              maxLength={2000}
              rows={2}
              placeholder="Audit notes for your records."
              className="block w-full rounded-md border border-neutral-300 px-3 py-2 text-sm text-neutral-900 placeholder-neutral-400 focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
            />
          </div>

          <div className="rounded-md bg-blue-50 border border-blue-200 p-3 text-sm">
            <div className="flex justify-between font-medium text-blue-900">
              <span>Total approved</span>
              <span>{formatMoney(totalApproved, lines[0]?.currency ?? 'USD')}</span>
            </div>
            <div className="flex justify-between text-xs text-blue-700 mt-1">
              <span>Original requested</span>
              <span>{formatMoney(totalRequested, lines[0]?.currency ?? 'USD')}</span>
            </div>
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
            <Button type="submit" disabled={isSubmitting || totalApproved <= 0}>
              {isSubmitting ? 'Approving…' : `Approve ${formatMoney(totalApproved, lines[0]?.currency ?? 'USD')}`}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}

function formatMoney(amount: number, currency: string): string {
  try {
    return new Intl.NumberFormat(undefined, { style: 'currency', currency }).format(amount);
  } catch {
    return `${amount.toFixed(2)} ${currency}`;
  }
}
