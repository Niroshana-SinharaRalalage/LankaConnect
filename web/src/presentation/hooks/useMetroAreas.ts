import { useState, useEffect } from 'react';
import { metroAreasRepository, MetroAreaDto } from '@/infrastructure/api/repositories/metro-areas.repository';

/**
 * Metro Areas State for UI
 * Includes grouped and sorted data for UI rendering
 */
export interface MetroAreasState {
  /** All metro areas loaded from API */
  metroAreas: MetroAreaDto[];

  /** Metro areas grouped by state for dropdown rendering */
  metroAreasByState: Map<string, MetroAreaDto[]>;

  /** State-level metros only (All [State]) */
  stateLevelMetros: MetroAreaDto[];

  /** City-level metros only (excludes All [State]) */
  cityLevelMetros: MetroAreaDto[];

  /** Loading state */
  isLoading: boolean;

  /** Error message if fetch failed */
  error: string | null;
}

/**
 * Hook to fetch and manage metro areas from the API
 * Phase 6A.9: Replaces hardcoded constants with dynamic API data
 *
 * Usage:
 * ```tsx
 * const { metroAreas, metroAreasByState, isLoading } = useMetroAreas();
 * ```
 */
export function useMetroAreas(): MetroAreasState {
  const [metroAreas, setMetroAreas] = useState<MetroAreaDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;

    async function fetchMetroAreas() {
      try {
        console.log('[useMetroAreas] Starting to fetch metro areas...');
        setIsLoading(true);
        setError(null);

        const data = await metroAreasRepository.getAll(true);
        console.log('[useMetroAreas] Successfully fetched', data.length, 'metro areas');

        if (isMounted) {
          setMetroAreas(data);
        }
      } catch (err) {
        console.error('[useMetroAreas] ERROR fetching metro areas:', err);
        if (isMounted) {
          const errorMessage = err instanceof Error ? err.message : 'Failed to load metro areas';
          setError(errorMessage);
          console.error('Error fetching metro areas:', err);
        }
      } finally {
        if (isMounted) {
          console.log('[useMetroAreas] Finished fetching (loading=false)');
          setIsLoading(false);
        }
      }
    }

    fetchMetroAreas();

    return () => {
      isMounted = false;
    };
  }, []);

  // Group metros by state
  const metroAreasByState = new Map<string, MetroAreaDto[]>();
  for (const metro of metroAreas) {
    if (!metroAreasByState.has(metro.state)) {
      metroAreasByState.set(metro.state, []);
    }
    metroAreasByState.get(metro.state)!.push(metro);
  }

  // Sort metros within each state (state-level first, then alphabetically)
  for (const [, metros] of metroAreasByState) {
    metros.sort((a, b) => {
      if (a.isStateLevelArea && !b.isStateLevelArea) return -1;
      if (!a.isStateLevelArea && b.isStateLevelArea) return 1;
      return a.name.localeCompare(b.name);
    });
  }

  // Separate state-level and city-level metros
  const stateLevelMetros = metroAreas.filter(m => m.isStateLevelArea);
  const cityLevelMetros = metroAreas.filter(m => !m.isStateLevelArea);

  return {
    metroAreas,
    metroAreasByState,
    stateLevelMetros,
    cityLevelMetros,
    isLoading,
    error,
  };
}
