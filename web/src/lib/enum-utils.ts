/**
 * Utility functions for working with TypeScript enums
 * Phase 6A.47: Provides reusable enum helpers to avoid hardcoding values
 */

import { EventCategory } from '@/infrastructure/api/types/events.types';

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