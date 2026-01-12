'use client';

import * as React from 'react';
import { NewsletterStatus } from '@/infrastructure/api/types/newsletters.types';
import { cn } from '@/presentation/lib/utils';

export interface NewsletterStatusBadgeProps {
  status: NewsletterStatus;
  className?: string;
}

/**
 * NewsletterStatusBadge Component
 * Displays newsletter status with color-coded badge
 * Phase 6A.74: Newsletter Feature
 *
 * Color mapping:
 * - Draft: Amber (#F59E0B) - Editable draft newsletters
 * - Active: Indigo (#6366F1) - Published and visible newsletters
 * - Inactive: Gray (#6B7280) - Expired newsletters
 * - Sent: Emerald (#10B981) - Newsletters that have been emailed
 */
export function NewsletterStatusBadge({ status, className }: NewsletterStatusBadgeProps) {
  const getStatusConfig = (status: NewsletterStatus) => {
    switch (status) {
      case NewsletterStatus.Draft:
        return { color: '#F59E0B', label: 'Draft' };
      case NewsletterStatus.Active:
        return { color: '#6366F1', label: 'Active' };
      case NewsletterStatus.Inactive:
        return { color: '#6B7280', label: 'Inactive' };
      case NewsletterStatus.Sent:
        return { color: '#10B981', label: 'Sent' };
      default:
        return { color: '#6B7280', label: 'Unknown' };
    }
  };

  const config = getStatusConfig(status);

  return (
    <span
      className={cn(
        'px-2 py-1 rounded-full text-xs font-semibold text-white whitespace-nowrap',
        className
      )}
      style={{ backgroundColor: config.color }}
    >
      {config.label}
    </span>
  );
}
