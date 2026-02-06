/**
 * Utility functions for working with TypeScript enums
 * Phase 6A.47: Provides reusable enum helpers to avoid hardcoding values
 * Phase 6A.74 Part 10: Added newsletter status helpers
 */

import { EventCategory } from '@/infrastructure/api/types/events.types';
import { NewsletterStatus } from '@/infrastructure/api/types/newsletters.types';

/**
 * Get event category options for dropdowns
 * Dynamically generates options from the EventCategory enum
 * This ensures frontend stays in sync with backend enum changes
 */
export function getEventCategoryOptions(): Array<{ value: EventCategory; label: string }> {
  return Object.entries(EventCategory)
    .filter(([key, value]) => typeof value === 'number') // Filter out reverse mappings
    .map(([key, value]) => ({
      value: value as EventCategory,
      label: key, // Use the enum key as the label (Religious, Cultural, etc.)
    }));
}

/**
 * Get event category label from enum value
 */
export function getEventCategoryLabel(category: EventCategory): string {
  return EventCategory[category] || 'Unknown';
}

// ==================== Newsletter Status Helpers ====================

/**
 * Check if newsletter status matches a specific status
 * Phase 6A.74 Part 11 Issue #4 Fix: Backend stores status as string
 * Database column: VARCHAR(20) with values "Draft", "Active", "Inactive", "Sent"
 *
 * @param status - The status value from API (string enum value)
 * @param expectedStatus - The NewsletterStatus enum value to compare against
 * @returns true if status matches
 */
export function isNewsletterStatus(
  status: NewsletterStatus | string | undefined | null,
  expectedStatus: NewsletterStatus
): boolean {
  if (status === undefined || status === null) return false;

  // Direct comparison - both are strings now
  if (status === expectedStatus) return true;

  // Case-insensitive fallback for safety
  if (typeof status === 'string') {
    return status.toLowerCase() === expectedStatus.toLowerCase();
  }

  return false;
}

/**
 * Check if newsletter status is Draft
 */
export function isNewsletterDraft(status: NewsletterStatus | string | undefined | null): boolean {
  return isNewsletterStatus(status, NewsletterStatus.Draft);
}

/**
 * Check if newsletter status is Active
 */
export function isNewsletterActive(status: NewsletterStatus | string | undefined | null): boolean {
  return isNewsletterStatus(status, NewsletterStatus.Active);
}

/**
 * Check if newsletter status is Inactive
 */
export function isNewsletterInactive(status: NewsletterStatus | string | undefined | null): boolean {
  return isNewsletterStatus(status, NewsletterStatus.Inactive);
}

/**
 * Check if newsletter status is Sent
 */
export function isNewsletterSent(status: NewsletterStatus | string | undefined | null): boolean {
  return isNewsletterStatus(status, NewsletterStatus.Sent);
}

/**
 * Check if newsletter has a known/valid status
 */
export function isNewsletterStatusKnown(status: NewsletterStatus | string | undefined | null): boolean {
  return isNewsletterDraft(status) ||
         isNewsletterActive(status) ||
         isNewsletterInactive(status) ||
         isNewsletterSent(status);
}