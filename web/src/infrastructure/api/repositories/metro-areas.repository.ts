import { apiClient } from '../client/api-client';

/**
 * Metro Area Response DTO from API
 * Matches backend MetroAreaDto structure
 */
export interface MetroAreaDto {
  id: string;
  name: string;
  state: string;
  centerLatitude: number;
  centerLongitude: number;
  radiusMiles: number;
  isStateLevelArea: boolean;
  isActive: boolean;
}

/**
 * Metro Areas Repository
 * Handles all metro area-related API calls
 * Phase 6A.9: Fix for hardcoded metro area IDs
 */
export const metroAreasRepository = {
  /**
   * Get all active metro areas
   * Endpoint: GET /api/metro-areas?activeOnly=true
   */
  async getAll(activeOnly: boolean = true): Promise<MetroAreaDto[]> {
    const data = await apiClient.get<MetroAreaDto[]>('/metro-areas', {
      params: { activeOnly },
    });
    return data;
  },

  /**
   * Get metro areas by state
   * Client-side filtering of all metros by state code
   */
  async getByState(stateCode: string): Promise<MetroAreaDto[]> {
    const allMetros = await this.getAll();
    return allMetros.filter(metro => metro.state === stateCode);
  },

  /**
   * Get a single metro area by ID
   * Client-side lookup from all metros
   */
  async getById(id: string): Promise<MetroAreaDto | undefined> {
    const allMetros = await this.getAll();
    return allMetros.find(metro => metro.id === id);
  },
};
