'use client';

import { useState, useEffect, useMemo } from 'react';
import { US_STATES } from '@/domain/constants/metroAreas.constants';
import { TreeDropdown, TreeNode } from '@/presentation/components/ui/TreeDropdown';
import { useMetroAreas } from '@/presentation/hooks/useMetroAreas';

interface NewsletterMetroSelectorProps {
  selectedMetroIds: string[];
  receiveAllLocations: boolean;
  onMetrosChange: (metroIds: string[]) => void;
  onReceiveAllChange: (receiveAll: boolean) => void;
  disabled?: boolean;
  maxSelections?: number;
}

/**
 * NewsletterMetroSelector Component
 * Phase 5B.8: Newsletter Metro Area Selection (Refactored)
 * Phase 6A.9: Updated to use API data via useMetroAreas hook
 *
 * Allows users to select multiple metro areas for newsletter subscriptions
 * or opt for all locations. Uses the TreeDropdown component for hierarchical selection.
 *
 * Refactored to:
 * - Remove state-level metro areas (entries like "All Alabama", etc.)
 * - Use TreeDropdown component with parent-child selection logic
 * - Transform metro data into TreeNode format
 * - Fetch data from API instead of hardcoded constants
 */
export function NewsletterMetroSelector({
  selectedMetroIds,
  receiveAllLocations,
  onMetrosChange,
  onReceiveAllChange,
  disabled = false,
  maxSelections = 20,
}: NewsletterMetroSelectorProps) {
  const [validationError, setValidationError] = useState<string>('');

  // Phase 6A.9: Fetch metro areas from API instead of hardcoded constants
  const {
    metroAreasByState,
    isLoading: metrosLoading,
    error: metrosError,
  } = useMetroAreas();

  // Check validation whenever selectedMetroIds changes
  useEffect(() => {
    if (selectedMetroIds.length > maxSelections) {
      setValidationError(`You cannot select more than ${maxSelections} metro areas`);
    } else {
      setValidationError('');
    }
  }, [selectedMetroIds, maxSelections]);

  /**
   * Transform metro areas data into TreeNode format for TreeDropdown
   * Each state becomes a parent node, city metros become children
   */
  const treeNodes: TreeNode[] = useMemo(() => {
    return US_STATES.map((state) => {
      const metrosForState = metroAreasByState.get(state.code) || [];

      // Filter out state-level metros (like "All Alabama")
      // Note: After database cleanup, there should be no state-level metros
      const cityMetros = metrosForState.filter((m) => !m.isStateLevelArea);

      // Only include states that have city metros
      if (cityMetros.length === 0) return null;

      // Create child nodes for each metro
      const children: TreeNode[] = cityMetros.map((metro) => ({
        id: metro.id,
        label: metro.name,
        checked: selectedMetroIds.includes(metro.id),
      }));

      // Create parent node for the state
      return {
        id: `state-${state.code}`,
        label: state.name,
        checked: children.every((child) => selectedMetroIds.includes(child.id)),
        children,
      };
    }).filter((node): node is TreeNode => node !== null);
  }, [metroAreasByState, selectedMetroIds]);

  const handleReceiveAllChange = (receiveAll: boolean) => {
    onReceiveAllChange(receiveAll);
    if (receiveAll) {
      onMetrosChange([]); // Clear selections when choosing all locations
      setValidationError('');
    }
  };

  const handleSelectionChange = (newSelectedIds: string[]) => {
    // Filter out state-level IDs (they start with "state-")
    const metroIds = newSelectedIds.filter((id) => !id.startsWith('state-'));
    console.log('[NewsletterMetroSelector] TreeDropdown selection changed:');
    console.log('  Raw IDs from TreeDropdown:', newSelectedIds);
    console.log('  Filtered metro IDs (state-* removed):', metroIds);
    console.log('  Calling onMetrosChange with', metroIds.length, 'IDs');
    onMetrosChange(metroIds);
  };

  // Show loading state while fetching metros
  if (metrosLoading) {
    return (
      <div className="space-y-4">
        <div>
          <label className="text-sm font-medium text-gray-700 mb-2 block">
            Get notifications for events in:
          </label>
          <p className="text-xs text-gray-500 mb-3">Loading metro areas...</p>
        </div>
        <div className="flex items-center justify-center p-4">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2" style={{ borderColor: '#FF7900' }}></div>
        </div>
      </div>
    );
  }

  // Show error if metros failed to load
  if (metrosError) {
    return (
      <div className="space-y-4">
        <div>
          <label className="text-sm font-medium text-gray-700 mb-2 block">
            Get notifications for events in:
          </label>
          <p className="text-xs text-red-600" role="alert">
            Failed to load metro areas: {metrosError}
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Header */}
      <div>
        <label className="text-sm font-medium text-gray-700 mb-2 block">
          Get notifications for events in:
        </label>
        <p className="text-xs text-gray-500 mb-3">
          Select up to {maxSelections} metro areas or receive updates from all locations
        </p>
      </div>

      {/* All Locations Checkbox */}
      <div className="mb-4">
        <label className="flex items-center text-sm text-gray-700 cursor-pointer">
          <input
            type="checkbox"
            checked={receiveAllLocations}
            onChange={(e) => handleReceiveAllChange(e.target.checked)}
            disabled={disabled}
            className="mr-2 w-4 h-4 rounded border-gray-300 text-[#FF7900] focus:ring-2 focus:ring-[#FF7900]"
          />
          <span className="font-medium">Send me events from all locations</span>
        </label>
      </div>

      {/* Metro Areas Selection (hidden when "all locations" is selected) */}
      {!receiveAllLocations && (
        <div className="space-y-3">
          {/* TreeDropdown Component */}
          <TreeDropdown
            nodes={treeNodes}
            selectedIds={selectedMetroIds}
            onSelectionChange={handleSelectionChange}
            placeholder="Select metro areas"
            maxSelections={maxSelections}
            disabled={disabled}
          />

          {/* Validation Error */}
          {validationError && (
            <p className="text-xs text-red-600" role="alert">
              {validationError}
            </p>
          )}
        </div>
      )}
    </div>
  );
}
