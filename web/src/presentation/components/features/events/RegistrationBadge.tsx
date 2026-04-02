'use client';

import { RegistrationStatus } from '@/infrastructure/api/types/events.types';

/**
 * Phase 6A.46: Registration Status Badge Component
 * Displays registration status badge on event cards
 * Issue #2: Fixed to check registration status instead of boolean flag
 * Phase 6A.137: Added Preliminary (payment processing) and RefundRequested badge variants
 */

interface RegistrationBadgeProps {
  registrationStatus?: RegistrationStatus | null;
  compact?: boolean; // For different layouts (default: false)
}

// Phase 6A.137: Helper to check status — handles both string (JsonStringEnumConverter) and numeric enum values
function isStatus(status: RegistrationStatus | null | undefined, ...values: Array<string | RegistrationStatus>): boolean {
  if (status == null) return false;
  return values.some(v => status === v || status === (v as any));
}

export function RegistrationBadge({ registrationStatus, compact = false }: RegistrationBadgeProps) {
  if (registrationStatus == null) return null;

  // Phase 6A.137: Confirmed/CheckedIn/Attended — green badge
  if (isStatus(registrationStatus, 'Confirmed', RegistrationStatus.Confirmed, 'CheckedIn', RegistrationStatus.CheckedIn, 'Attended', RegistrationStatus.Attended)) {
    return (
      <div className="inline-flex items-center gap-1.5 px-2.5 py-1 bg-green-50 border border-green-200 rounded-md">
        <svg
          className="w-4 h-4 text-green-600"
          fill="none"
          strokeLinecap="round"
          strokeLinejoin="round"
          strokeWidth="2"
          viewBox="0 0 24 24"
          stroke="currentColor"
          aria-hidden="true"
        >
          <path d="M5 13l4 4L19 7"></path>
        </svg>
        <span className="text-xs font-medium text-green-800">
          {compact ? 'Registered' : 'You are registered'}
        </span>
      </div>
    );
  }

  // Phase 6A.137: Preliminary — amber badge (payment processing)
  if (isStatus(registrationStatus, 'Preliminary', RegistrationStatus.Preliminary)) {
    return (
      <div className="inline-flex items-center gap-1.5 px-2.5 py-1 bg-amber-50 border border-amber-200 rounded-md">
        <svg
          className="w-4 h-4 text-amber-600 animate-pulse"
          fill="none"
          strokeLinecap="round"
          strokeLinejoin="round"
          strokeWidth="2"
          viewBox="0 0 24 24"
          stroke="currentColor"
          aria-hidden="true"
        >
          <path d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"></path>
        </svg>
        <span className="text-xs font-medium text-amber-800">
          {compact ? 'Processing' : 'Payment Processing...'}
        </span>
      </div>
    );
  }

  // Phase 6A.137: RefundRequested — orange badge
  if (isStatus(registrationStatus, 'RefundRequested', RegistrationStatus.RefundRequested)) {
    return (
      <div className="inline-flex items-center gap-1.5 px-2.5 py-1 bg-orange-50 border border-orange-200 rounded-md">
        <svg
          className="w-4 h-4 text-orange-600"
          fill="none"
          strokeLinecap="round"
          strokeLinejoin="round"
          strokeWidth="2"
          viewBox="0 0 24 24"
          stroke="currentColor"
          aria-hidden="true"
        >
          <path d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4.5c-.77-.833-2.694-.833-3.464 0L3.34 16.5c-.77.833.192 2.5 1.732 2.5z"></path>
        </svg>
        <span className="text-xs font-medium text-orange-800">
          {compact ? 'Refund Pending' : 'Refund in Progress'}
        </span>
      </div>
    );
  }

  // Phase 6A.137: Waitlisted — blue badge
  if (isStatus(registrationStatus, 'Waitlisted', RegistrationStatus.Waitlisted)) {
    return (
      <div className="inline-flex items-center gap-1.5 px-2.5 py-1 bg-blue-50 border border-blue-200 rounded-md">
        <svg
          className="w-4 h-4 text-blue-600"
          fill="none"
          strokeLinecap="round"
          strokeLinejoin="round"
          strokeWidth="2"
          viewBox="0 0 24 24"
          stroke="currentColor"
          aria-hidden="true"
        >
          <path d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"></path>
        </svg>
        <span className="text-xs font-medium text-blue-800">
          {compact ? 'Waitlisted' : 'You are waitlisted'}
        </span>
      </div>
    );
  }

  return null;
}
