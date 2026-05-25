'use client';

import { CheckCircle2, Clock, XCircle, AlertCircle } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import type { AttendeeRefundRequestDto } from '@/infrastructure/api/types/refund-request.types';

interface RefundRequestStatusBannerProps {
  refundRequest: AttendeeRefundRequestDto;
  isWithdrawing?: boolean;
  onWithdraw?: () => void;
}

/**
 * Phase 6A.148 — attendee-facing status banner shown on the registration page when
 * a refund request exists for the current registration.
 *
 * Shows the status, requester reason (if any), rejection reason (if any), and a
 * Withdraw button while the request is still Pending. `organizerNotes` are NOT
 * exposed here — the API DTO doesn't include them (privacy boundary).
 */
export function RefundRequestStatusBanner({
  refundRequest,
  isWithdrawing,
  onWithdraw,
}: RefundRequestStatusBannerProps) {
  const { status, requesterReason, rejectionReason, requestedAt, reviewedAt, completedAt } = refundRequest;

  // Status -> visual config
  let icon = <Clock className="h-5 w-5 text-amber-600" />;
  let title = '';
  let message = '';
  let bgClass = 'bg-amber-50 border-amber-200';
  let titleClass = 'text-amber-900';
  let bodyClass = 'text-amber-800';

  switch (status) {
    case 'Pending':
      icon = <Clock className="h-5 w-5 text-amber-600" />;
      title = 'Refund Requested — Awaiting Organizer Approval';
      message =
        "Your refund request has been received. The event organizer will review it soon and you'll be notified by email when they respond.";
      bgClass = 'bg-amber-50 border-amber-200';
      titleClass = 'text-amber-900';
      bodyClass = 'text-amber-800';
      break;
    case 'Approved':
    case 'Processing':
      icon = <Clock className="h-5 w-5 text-blue-600" />;
      title = 'Refund Approved — Processing via Stripe';
      message =
        'Your refund has been approved and is being processed by Stripe. It typically takes 5-10 business days to appear in your account.';
      bgClass = 'bg-blue-50 border-blue-200';
      titleClass = 'text-blue-900';
      bodyClass = 'text-blue-800';
      break;
    case 'Completed':
      icon = <CheckCircle2 className="h-5 w-5 text-emerald-600" />;
      title = 'Refund Completed';
      message = 'Your refund has been processed successfully.';
      bgClass = 'bg-emerald-50 border-emerald-200';
      titleClass = 'text-emerald-900';
      bodyClass = 'text-emerald-800';
      break;
    case 'Rejected':
      icon = <XCircle className="h-5 w-5 text-red-600" />;
      title = 'Refund Request Declined';
      message = rejectionReason
        ? `Reason from organizer: ${rejectionReason}`
        : 'The organizer declined your refund request. Contact them directly if you have questions.';
      bgClass = 'bg-red-50 border-red-200';
      titleClass = 'text-red-900';
      bodyClass = 'text-red-800';
      break;
    case 'Withdrawn':
      // Hide the banner once withdrawn — the registration card is back to normal.
      return null;
    default:
      icon = <AlertCircle className="h-5 w-5 text-neutral-600" />;
      title = `Refund Status: ${status}`;
      message = '';
      bgClass = 'bg-neutral-50 border-neutral-200';
      titleClass = 'text-neutral-900';
      bodyClass = 'text-neutral-800';
  }

  return (
    <div className={`rounded-lg border p-4 mb-4 ${bgClass}`} role="status">
      <div className="flex items-start gap-3">
        <div className="flex-shrink-0 mt-0.5">{icon}</div>
        <div className="flex-1 min-w-0">
          <h4 className={`text-sm font-semibold ${titleClass} mb-1`}>{title}</h4>
          <p className={`text-sm ${bodyClass}`}>{message}</p>

          {requesterReason && (
            <p className={`text-xs ${bodyClass} mt-2 italic`}>
              Your reason: &ldquo;{requesterReason}&rdquo;
            </p>
          )}

          <div className={`text-xs ${bodyClass} mt-2 opacity-75`}>
            Requested {formatDateTime(requestedAt)}
            {reviewedAt && ` · Reviewed ${formatDateTime(reviewedAt)}`}
            {completedAt && ` · Completed ${formatDateTime(completedAt)}`}
          </div>

          {status === 'Pending' && onWithdraw && (
            <div className="mt-3">
              <Button
                variant="outline"
                size="sm"
                onClick={onWithdraw}
                disabled={isWithdrawing}
              >
                {isWithdrawing ? 'Withdrawing…' : 'Withdraw refund request'}
              </Button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

function formatDateTime(iso: string): string {
  try {
    return new Date(iso).toLocaleString(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
    });
  } catch {
    return iso;
  }
}
