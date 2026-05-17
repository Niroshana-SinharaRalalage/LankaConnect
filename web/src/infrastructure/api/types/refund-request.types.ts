/**
 * Phase 6A.148: TypeScript shapes mirroring the backend RefundRequest DTOs.
 *
 * Two projection types:
 * - AttendeeRefundRequestDto: returned from GET /refund-requests/me. INTENTIONALLY
 *   excludes `organizerNotes` (backend architect F6 — internal audit-only field).
 * - OrganizerRefundRequestDto: returned from GET /refund-requests. Includes the full
 *   set of audit fields visible only to organizers.
 */

export type RefundRequestStatus =
  | 'Pending'
  | 'Approved'
  | 'Processing'
  | 'Completed'
  | 'Rejected'
  | 'Withdrawn';

export type RefundLineItemType = 'Ticket' | 'AddOn' | 'Collection' | 'Sponsor';

export type RefundLineItemStatus =
  | 'Requested'
  | 'Approved'
  | 'Rejected'
  | 'Processing'
  | 'Refunded'
  | 'Failed';

export type RefundCurrency = 'USD' | 'LKR' | 'EUR' | 'GBP';

export interface RefundLineItemDto {
  id: string;
  type: RefundLineItemType;
  referenceId: string;
  requestedAmount: number;
  requestedCurrency: RefundCurrency;
  approvedAmount: number | null;
  approvedCurrency: RefundCurrency | null;
  status: RefundLineItemStatus;
  stripeRefundId: string | null;
  processedAt: string | null;
  failureReason: string | null;
}

/**
 * Attendee-facing projection. NO `organizerNotes` field — this is a strict privacy
 * boundary, asserted by a backend unit test. Do not add it to this type.
 */
export interface AttendeeRefundRequestDto {
  id: string;
  registrationId: string;
  status: RefundRequestStatus;
  requestedAt: string;
  requesterReason: string | null;
  reviewedAt: string | null;
  rejectionReason: string | null;
  completedAt: string | null;
  lineItems: RefundLineItemDto[];
}

/**
 * Organizer-facing projection. Used by the Refund Requests sub-tab on AttendeeManagementTab.
 */
export interface OrganizerRefundRequestDto {
  id: string;
  registrationId: string;
  requestedByUserId: string;
  isOrganizerInitiated: boolean;
  status: RefundRequestStatus;
  requestedAt: string;
  requesterReason: string | null;
  reviewedByUserId: string | null;
  reviewedAt: string | null;
  organizerNotes: string | null;
  rejectionReason: string | null;
  completedAt: string | null;
  scanGuardOverridden: boolean;
  lineItems: RefundLineItemDto[];
}

// ============ Request payloads ============

export interface RefundLineItemInput {
  type: RefundLineItemType;
  referenceId: string;
  requestedAmount: number;
  currency: RefundCurrency;
}

export interface CreateRefundRequestPayload {
  requesterReason?: string | null;
  lineItems: RefundLineItemInput[];
}

export interface CreateOrganizerInitiatedRefundPayload {
  registrationId: string;
  organizerNotes?: string | null;
  overrideScanGuard: boolean;
  lineItems: RefundLineItemInput[];
}

export interface ApproveLineItemInput {
  lineItemId: string;
  approvedAmount: number;
  currency: RefundCurrency;
}

export interface ApproveRefundRequestPayload {
  organizerNotes?: string | null;
  perLineApprovedAmounts: ApproveLineItemInput[];
}

export interface RejectRefundRequestPayload {
  rejectionReason: string;
}

export interface CreateRefundRequestResult {
  refundRequestId: string;
}
