'use client';

import { useState } from 'react';
import { X, AlertCircle } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';

interface RefundRejectDialogProps {
  open: boolean;
  eventId: string;
  refundRequestId: string;
  attendeeName?: string;
  onClose: () => void;
  onRejected: () => void;
}

/**
 * Phase 6A.148 — organizer rejection dialog. Reason is mandatory and is sent
 * to the attendee in the rejection email.
 */
export function RefundRejectDialog({
  open,
  eventId,
  refundRequestId,
  attendeeName,
  onClose,
  onRejected,
}: RefundRejectDialogProps) {
  const [reason, setReason] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (!open) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    const trimmed = reason.trim();
    if (!trimmed) {
      setError('Please provide a reason — it will be shown to the attendee.');
      return;
    }

    setIsSubmitting(true);
    try {
      await eventsRepository.rejectRefundRequest(eventId, refundRequestId, {
        rejectionReason: trimmed,
      });
      onRejected();
      onClose();
    } catch (err) {
      console.error('[RefundRejectDialog] reject failed:', err);
      const msg = err instanceof Error ? err.message : 'Failed to reject. Please try again.';
      setError(msg);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
      <div className="w-full max-w-md rounded-lg bg-white shadow-xl">
        <div className="flex items-center justify-between border-b border-neutral-200 p-4">
          <h2 className="text-lg font-semibold text-neutral-900">Decline Refund Request</h2>
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
            {attendeeName
              ? `Decline ${attendeeName}'s refund request. The reason below will be emailed to them.`
              : 'Decline this refund request. The reason below will be emailed to the attendee.'}
          </p>

          <div>
            <label className="block text-xs font-semibold text-neutral-700 uppercase tracking-wide mb-1">
              Rejection reason <span className="text-red-600">*</span>
            </label>
            <textarea
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              maxLength={1000}
              rows={4}
              required
              placeholder="e.g. Cancellation policy doesn't allow refunds within 48 hours of the event."
              className="block w-full rounded-md border border-neutral-300 px-3 py-2 text-sm text-neutral-900 placeholder-neutral-400 focus:border-red-500 focus:ring-1 focus:ring-red-500"
            />
            <p className="text-xs text-neutral-500 mt-1">{reason.length} / 1000</p>
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
            <Button
              type="submit"
              disabled={isSubmitting || !reason.trim()}
              style={{ backgroundColor: '#DC2626' }}
            >
              {isSubmitting ? 'Declining…' : 'Decline Request'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
