/**
 * Reference Data Service
 * Fetches all reference data from unified API endpoint
 * Phase 6A.47: Replace hardcoded constants with database-driven reference data
 */

import { apiClient } from '../client/api-client';

/**
 * Reference value from database (unified structure)
 */
export interface ReferenceValue {
  id: string;
  enumType: string;
  code: string;
  intValue: number | null;
  name: string;
  description: string | null;
  displayOrder: number;
  isActive: boolean;
  metadata: Record<string, unknown> | null;
}

/**
 * Event Interest option (mapped from EventCategory)
 * Replaces hardcoded CULTURAL_INTERESTS
 */
export interface EventInterestOption {
  code: string;
  name: string;
  description?: string;
}

/**
 * Fetch reference data by enum types
 * @param types - Array of enum types to fetch (e.g., ['EventCategory', 'EventStatus'])
 * @param activeOnly - Return only active values (default: true)
 */
export async function getReferenceDataByTypes(
  types: string[],
  activeOnly: boolean = true
): Promise<ReferenceValue[]> {
  const typesParam = types.join(',');
  const { data } = await apiClient.get<ReferenceValue[]>(
    `/reference-data?types=${typesParam}&activeOnly=${activeOnly}`
  );
  return data;
}

/**
 * Fetch Event Interests (EventCategory from database)
 * Replaces hardcoded CULTURAL_INTERESTS constant
 */
export async function getEventInterests(): Promise<EventInterestOption[]> {
  const data = await getReferenceDataByTypes(['EventCategory']);

  return data
    .map(item => ({
      code: item.code,
      name: item.name,
      description: item.description || undefined,
    }))
    .sort((a, b) => a.name.localeCompare(b.name));
}
