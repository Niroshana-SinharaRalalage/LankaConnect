/**
 * Enum Mapping Utilities
 * Phase 6A.47: Utilities for mapping between reference data and TypeScript enums
 */

import type { ReferenceValue } from '../services/referenceData.service';
import { EventCategory, EventStatus } from '../types/events.types';

/**
 * Build a map from code to enum int value
 * Used for form submissions where we need to convert code → intValue
 *
 * @example
 * const map = buildCodeToIntMap(categories);
 * const value = map['Religious']; // Returns 0 (EventCategory.Religious)
 */
export function buildCodeToIntMap<T extends number>(
  data: ReferenceValue[] | undefined
): Record<string, T> {
  if (!data) return {} as Record<string, T>;

  return data.reduce((acc, item) => {
    acc[item.code] = item.intValue as T;
    return acc;
  }, {} as Record<string, T>);
}

/**
 * Get display name from enum int value
 * Used for displaying enum values in UI
 *
 * @example
 * const name = getNameFromIntValue(categories, EventCategory.Religious); // Returns 'Religious'
 */
export function getNameFromIntValue(
  data: ReferenceValue[] | undefined,
  intValue: number
): string | undefined {
  if (!data) return undefined;
  return data.find(item => item.intValue === intValue)?.name;
}

/**
 * Get display name from code
 * Used for displaying code values in UI
 *
 * @example
 * const name = getNameFromCode(categories, 'Religious'); // Returns 'Religious'
 */
export function getNameFromCode(
  data: ReferenceValue[] | undefined,
  code: string
): string | undefined {
  if (!data) return undefined;
  return data.find(item => item.code === code)?.name;
}

/**
 * Build EventCategory code to int value map
 * Specific helper for EventCategory enum
 */
export function buildEventCategoryMap(
  data: ReferenceValue[] | undefined
): Record<string, EventCategory> {
  return buildCodeToIntMap<EventCategory>(data);
}

/**
 * Build EventStatus code to int value map
 * Specific helper for EventStatus enum
 */
export function buildEventStatusMap(
  data: ReferenceValue[] | undefined
): Record<string, EventStatus> {
  return buildCodeToIntMap<EventStatus>(data);
}

/**
 * Convert reference data to dropdown options
 * Standard format for all dropdowns: { value: intValue, label: name }
 *
 * @example
 * const options = toDropdownOptions(categories);
 * // Returns: [{ value: 0, label: 'Religious' }, { value: 1, label: 'Cultural' }, ...]
 */
export function toDropdownOptions(
  data: ReferenceValue[] | undefined
): Array<{ value: number; label: string }> {
  if (!data) return [];

  return data
    .filter(item => item.intValue !== null) // Filter out null intValues
    .map(item => ({
      value: item.intValue as number, // Safe to cast after filter
      label: item.name,
    }))
    .sort((a, b) => a.value - b.value); // Sort by intValue for consistency
}
