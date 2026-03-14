'use client';

import * as React from 'react';
import { cn } from '@/presentation/lib/utils';
import { NewsletterMainType, getMainTypeColor } from '@/lib/newsletter-type-utils';

export interface NewsletterTypeBadgeProps {
  type: NewsletterMainType;
  className?: string;
}

/**
 * NewsletterTypeBadge Component
 * Displays the main newsletter type (Newsletter or Notification) as a colored pill badge.
 *
 * Color mapping:
 * - Newsletter: Orange (#FF7900) — LankaConnect primary brand
 * - Notification: Purple (#8B5CF6) — distinct visual identity
 */
export function NewsletterTypeBadge({ type, className }: NewsletterTypeBadgeProps) {
  return (
    <span
      className={cn(
        'px-2 py-1 rounded-full text-xs font-semibold text-white whitespace-nowrap',
        className
      )}
      style={{ backgroundColor: getMainTypeColor(type) }}
    >
      {type}
    </span>
  );
}
