'use client';

import { MapPin } from 'lucide-react';
import { TreeDropdown, type TreeNode } from '@/presentation/components/ui/TreeDropdown';
import { useMetroAreas } from '@/presentation/hooks/useMetroAreas';

interface LocationFilterProps {
  selectedMetroAreaIds: string[];
  onMetroAreasChange: (metroAreaIds: string[]) => void;
  className?: string;
}

/**
 * LocationFilter component for event filtering
 * Phase 6A.47: Filter events by state and metro areas using TreeDropdown
 *
 * Features:
 * - Hierarchical tree structure (State > Metro Areas)
 * - Multi-select metro areas
 * - Reuses existing TreeDropdown component
 * - Fetches metro areas from API
 *
 * Design Note: Leverages existing metro area infrastructure from user profile
 */
export function LocationFilter({
  selectedMetroAreaIds,
  onMetroAreasChange,
  className = '',
}: LocationFilterProps) {
  const { metroAreas, isLoading } = useMetroAreas();

  // Transform metro areas API data into TreeNode structure
  const treeNodes: TreeNode[] = metroAreas.reduce((acc: TreeNode[], metroArea) => {
    // Find or create state node
    let stateNode = acc.find((node) => node.id === metroArea.state);

    if (!stateNode) {
      stateNode = {
        id: metroArea.state,
        label: metroArea.state,
        checked: false,
        children: [],
      };
      acc.push(stateNode);
    }

    // Add metro area as child
    stateNode.children!.push({
      id: metroArea.id,
      label: metroArea.name,
      checked: selectedMetroAreaIds.includes(metroArea.id),
    });

    return acc;
  }, []);

  if (isLoading) {
    return (
      <div className={`relative ${className}`}>
        <div className="block w-full pl-10 pr-10 py-2.5 border border-gray-300 rounded-lg text-sm text-gray-400">
          <MapPin className="inline h-5 w-5 mr-2" />
          Loading locations...
        </div>
      </div>
    );
  }

  return (
    <div className={`relative ${className}`}>
      <label htmlFor="location-filter" className="sr-only">
        Filter by location
      </label>
      <div className="absolute inset-y-0 left-0 flex items-center pl-3 pointer-events-none z-10">
        <MapPin className="h-5 w-5 text-gray-400" aria-hidden="true" />
      </div>
      <div className="pl-10">
        <TreeDropdown
          nodes={treeNodes}
          selectedIds={selectedMetroAreaIds}
          onSelectionChange={onMetroAreasChange}
          placeholder="All Locations"
          className="w-full"
        />
      </div>
    </div>
  );
}
