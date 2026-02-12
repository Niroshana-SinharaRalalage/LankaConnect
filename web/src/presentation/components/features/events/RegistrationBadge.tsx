'use client';

import { RegistrationStatus } from '@/infrastructure/api/types/events.types';

/**
 * Phase 6A.46: Registration Status Badge Component
 * Displays "You are registered" badge on event cards for CONFIRMED registrations only
 * Issue #2: Fixed to check registration status instead of boolean flag
 */

interface RegistrationBadgeProps {
  registrationStatus?: RegistrationStatus | null;
  compact?: boolean; // For different layouts (default: false)
}

export function RegistrationBadge({ registrationStatus, compact = false }: RegistrationBadgeProps) {
  // Issue #2: Only show badge for Confirmed registrations, not Preliminary
  // Phase 6A.X: Backend uses JsonStringEnumConverter, so enum values come as strings
  // Check both string value and numeric enum value for compatibility
  const isConfirmed = registrationStatus === ('Confirmed' as any) || registrationStatus === RegistrationStatus.Confirmed;
  if (!isConfirmed) return null;

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
