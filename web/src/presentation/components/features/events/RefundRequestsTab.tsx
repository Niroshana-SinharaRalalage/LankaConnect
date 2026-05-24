'use client';

import { useCallback, useEffect, useState } from 'react';
import { Clock, CheckCircle2, XCircle, AlertTriangle, RotateCw } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import type {
  OrganizerRefundRequestDto,
  RefundRequestStatus,
} from '@/infrastructure/api/types/refund-request.types';
import { RefundApprovalDialog } from './RefundApprovalDialog';
import { RefundRejectDialog } from './RefundRejectDialog';

interface RefundRequestsTabProps {
  eventId: string;
}

const STATUS_TABS: ReadonlyArray<{ label: string; value: RefundRequestStatus | 'All' }> = [
  { label: 'Pending', value: 'Pending' },
  { label: 'Approved', value: 'Approved' },
  { label: 'Processing', value: 'Processing' },
  { label: 'Completed', value: 'Completed' },
  { label: 'Rejected', value: 'Rejected' },
  { label: 'All', value: 'All' },
];

/**
 * Phase 6A.148 — organizer queue inside AttendeeManagementTab. Lists refund
 * requests for the event with status tabs and the Approve / Reject actions.
 *
 * Overdue badge fires when a Pending request is older than 3 days (operator
 * SLA per product decision D3 — no auto-action, just surface it).
 */
export function RefundRequestsTab({ eventId }: RefundRequestsTabProps) {
  const [statusFilter, setStatusFilter] = useState<RefundRequestStatus | 'All'>('Pending');
  const [requests, setRequests] = useState<OrganizerRefundRequestDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [approveTarget, setApproveTarget] = useState<OrganizerRefundRequestDto | null>(null);
  const [rejectTarget, setRejectTarget] = useState<OrganizerRefundRequestDto | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const filter = statusFilter === 'All' ? undefined : statusFilter;
      const data = await eventsRepository.listEventRefundRequests(eventId, filter);
      setRequests(data ?? []);
    } catch (err) {
      console.error('[RefundRequestsTab] load failed:', err);
      const msg = err instanceof Error ? err.message : 'Failed to load refund requests.';
      setError(msg);
      setRequests([]);
    } finally {
      setLoading(false);
    }
  }, [eventId, statusFilter]);

  useEffect(() => {
    void load();
  }, [load]);

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div className="flex flex-wrap gap-2">
          {STATUS_TABS.map((t) => (
            <button
              key={t.value}
              type="button"
              onClick={() => setStatusFilter(t.value)}
              className={`px-3 py-1.5 rounded-full text-xs font-medium transition-colors ${
                statusFilter === t.value
                  ? 'bg-blue-600 text-white'
                  : 'bg-neutral-100 text-neutral-700 hover:bg-neutral-200'
              }`}
            >
              {t.label}
            </button>
          ))}
        </div>
        <button
          type="button"
          onClick={() => void load()}
          disabled={loading}
          className="text-neutral-500 hover:text-neutral-700 disabled:opacity-50"
          aria-label="Refresh"
        >
          <RotateCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
        </button>
      </div>

      {error && (
        <div className="rounded-md bg-red-50 border border-red-200 p-3 text-sm text-red-700">
          {error}
        </div>
      )}

      {loading ? (
        <div className="rounded-md bg-neutral-50 p-6 text-center text-sm text-neutral-500">
          Loading refund requests…
        </div>
      ) : requests.length === 0 ? (
        <div className="rounded-md bg-neutral-50 p-6 text-center text-sm text-neutral-500">
          No {statusFilter === 'All' ? '' : statusFilter.toLowerCase() + ' '}refund requests.
        </div>
      ) : (
        <div className="space-y-3">
          {requests.map((r) => (
            <RefundRequestCard
              key={r.id}
              request={r}
              onReview={() => setApproveTarget(r)}
              onReject={() => setRejectTarget(r)}
            />
          ))}
        </div>
      )}

      {approveTarget && (
        <RefundApprovalDialog
          open
          eventId={eventId}
          request={approveTarget}
          onClose={() => setApproveTarget(null)}
          onApproved={() => {
            setApproveTarget(null);
            void load();
          }}
        />
      )}

      {rejectTarget && (
        <RefundRejectDialog
          open
          eventId={eventId}
          refundRequestId={rejectTarget.id}
          attendeeName={undefined}
          onClose={() => setRejectTarget(null)}
          onRejected={() => {
            setRejectTarget(null);
            void load();
          }}
        />
      )}
    </div>
  );
}

interface RefundRequestCardProps {
  request: OrganizerRefundRequestDto;
  onReview: () => void;
  onReject: () => void;
}

function RefundRequestCard({ request, onReview, onReject }: RefundRequestCardProps) {
  const { status, requestedAt, requesterReason, isOrganizerInitiated, scanGuardOverridden, lineItems } = request;
  const totalRequested = lineItems.reduce((s, li) => s + li.requestedAmount, 0);
  const currency = lineItems[0]?.requestedCurrency ?? 'USD';

  // Overdue if Pending > 3 days (D3 — operator SLA badge).
  const isOverdue =
    status === 'Pending' && Date.now() - new Date(requestedAt).getTime() > 3 * 24 * 60 * 60 * 1000;

  const statusBadge = (() => {
    switch (status) {
      case 'Pending':
        return (
          <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-amber-100 text-amber-800">
            <Clock className="h-3 w-3" /> Pending
          </span>
        );
      case 'Approved':
      case 'Processing':
        return (
          <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
            <Clock className="h-3 w-3" /> {status}
          </span>
        );
      case 'Completed':
        return (
          <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-emerald-100 text-emerald-800">
            <CheckCircle2 className="h-3 w-3" /> Completed
          </span>
        );
      case 'Rejected':
        return (
          <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-800">
            <XCircle className="h-3 w-3" /> Rejected
          </span>
        );
      case 'Withdrawn':
        return (
          <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-neutral-100 text-neutral-700">
            Withdrawn
          </span>
        );
      default:
        return <span className="text-xs text-neutral-600">{status}</span>;
    }
  })();

  return (
    <div className="rounded-lg border border-neutral-200 bg-white p-4">
      <div className="flex items-start justify-between gap-3 mb-2">
        <div className="flex items-center gap-2 flex-wrap">
          {statusBadge}
          {isOrganizerInitiated && (
            <span className="inline-block px-2 py-0.5 rounded-full text-xs font-medium bg-purple-100 text-purple-800">
              Organizer-initiated
            </span>
          )}
          {scanGuardOverridden && (
            <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-yellow-100 text-yellow-800">
              <AlertTriangle className="h-3 w-3" /> Scan-guard overridden
            </span>
          )}
          {isOverdue && (
            <span className="inline-block px-2 py-0.5 rounded-full text-xs font-medium bg-orange-100 text-orange-800">
              Overdue &gt; 3 days
            </span>
          )}
        </div>
        <div className="text-xs text-neutral-500 whitespace-nowrap">
          {new Date(requestedAt).toLocaleDateString(undefined, {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
          })}
        </div>
      </div>

      <div className="space-y-2 mb-3">
        {lineItems.map((li) => (
          <div key={li.id} className="flex justify-between text-sm">
            <span className="text-neutral-700">
              {li.type}
              {li.status !== 'Requested' && (
                <span className="ml-2 text-xs text-neutral-500">({li.status})</span>
              )}
            </span>
            <span className="text-neutral-900 font-medium">
              {formatMoney(li.requestedAmount, li.requestedCurrency)}
              {li.approvedAmount !== null && li.approvedAmount !== li.requestedAmount && (
                <span className="ml-2 text-xs text-blue-700">
                  → {formatMoney(li.approvedAmount, li.approvedCurrency ?? li.requestedCurrency)}
                </span>
              )}
            </span>
          </div>
        ))}
      </div>

      <div className="flex justify-between items-center pt-2 border-t border-neutral-100">
        <div className="text-sm font-semibold text-neutral-900">
          Total requested: {formatMoney(totalRequested, currency)}
        </div>
        {status === 'Pending' && (
          <div className="flex gap-2">
            <Button variant="outline" size="sm" onClick={onReject}>
              Decline
            </Button>
            <Button size="sm" onClick={onReview}>
              Review &amp; Approve
            </Button>
          </div>
        )}
      </div>

      {requesterReason && (
        <p className="text-xs text-neutral-600 mt-2 italic">
          Reason: &ldquo;{requesterReason}&rdquo;
        </p>
      )}

      {request.rejectionReason && (
        <p className="text-xs text-red-700 mt-2">
          Declined: {request.rejectionReason}
        </p>
      )}

      {request.organizerNotes && (
        <p className="text-xs text-neutral-500 mt-2">
          Notes (internal): {request.organizerNotes}
        </p>
      )}
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
