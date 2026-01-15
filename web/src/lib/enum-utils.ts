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
 * Handles both string (from API) and numeric (enum) values
 *
 * Backend returns status as STRING (e.g., "Active"), but frontend enum is numeric.
 * This function normalizes the comparison.
 *
 * @param status - The status value from API (can be string or number)
 * @param expectedStatus - The NewsletterStatus enum value to compare against
 * @returns true if status matches
 */
export function isNewsletterStatus(
  status: NewsletterStatus | string | number | undefined | null,
  expectedStatus: NewsletterStatus
): boolean {
  if (status === undefined || status === null) return false;

  // If status is a string, compare with expected status name
  if (typeof status === 'string') {
    const normalizedStatus = status.toLowerCase();
    switch (expectedStatus) {
      case NewsletterStatus.Draft:
        return normalizedStatus === 'draft';
      case NewsletterStatus.Active:
        return normalizedStatus === 'active';
      case NewsletterStatus.Inactive:
        return normalizedStatus === 'inactive';
      case NewsletterStatus.Sent:
        return normalizedStatus === 'sent';
      default:
        return false;
    }
  }

  // If status is a number, compare directly with enum
  return status === expectedStatus;
}

/**
 * Check if newsletter status is Draft
 */
export function isNewsletterDraft(status: NewsletterStatus | string | number | undefined | null): boolean {
  return isNewsletterStatus(status, NewsletterStatus.Draft);
}

/**
 * Check if newsletter status is Active
 */
export function isNewsletterActive(status: NewsletterStatus | string | number | undefined | null): boolean {
  return isNewsletterStatus(status, NewsletterStatus.Active);
}

/**
 * Check if newsletter status is Inactive
 */
export function isNewsletterInactive(status: NewsletterStatus | string | number | undefined | null): boolean {
  return isNewsletterStatus(status, NewsletterStatus.Inactive);
}

/**
 * Check if newsletter status is Sent
 */
export function isNewsletterSent(status: NewsletterStatus | string | number | undefined | null): boolean {
  return isNewsletterStatus(status, NewsletterStatus.Sent);
}

/**
 * Check if newsletter has a known/valid status
 */
export function isNewsletterStatusKnown(status: NewsletterStatus | string | number | undefined | null): boolean {
  return isNewsletterDraft(status) ||
         isNewsletterActive(status) ||
         isNewsletterInactive(status) ||
         isNewsletterSent(status);
}