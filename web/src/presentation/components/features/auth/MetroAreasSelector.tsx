'use client';

import React, { useMemo } from 'react';
import { TreeDropdown, TreeNode } from '@/presentation/components/ui/TreeDropdown';
import { useMetroAreas } from '@/presentation/hooks/useMetroAreas';

/**
 * Metro Areas Selector Props
 */
export interface MetroAreasSelectorProps {
  /** Selected metro area IDs */
  value: string[];
  /** Callback when selection changes */
  onChange: (ids: string[]) => void;
  /** Validation error message */
  error?: string;
  /** Whether field is required */
  required?: boolean;
  /** Minimum number of selections required */
  minSelection?: number;
  /** Maximum number of selections allowed */
  maxSelection?: number;
}

/**
 * Metro Areas Selector Component
 * Standalone component for selecting preferred metro areas during registration
 *
 * Features:
 * - Reuses TreeDropdown and useMetroAreas hook from profile page
 * - Groups metro areas by state
 * - Supports min/max selection validation
 * - Loading and error states
 * - Required field indicator
 * - Helper text for user guidance
 *
 * Usage:
 * ```tsx
 * <MetroAreasSelector
 *   value={selectedMetroIds}
 *   onChange={setSelectedMetroIds}
 *   required={true}
 *   minSelection={1}
 *   maxSelection={20}
 *   error={errors.preferredMetroAreaIds?.message}
 * />
 * ```
 */
export function MetroAreasSelector({
  value,
  onChange,
  error,
  required = true,
  minSelection = 1,
  maxSelection = 20,
}: MetroAreasSelectorProps) {
  // Fetch metro areas from API using reusable hook
  const { metroAreas, metroAreasByState, isLoading, error: apiError } = useMetroAreas();

  // Convert metro areas to tree structure for TreeDropdown
  // Reuses logic from PreferredMetroAreasSection.tsx
  const metroAreaNodes: TreeNode[] = useMemo(() => {
    if (!metroAreas || metroAreas.length === 0) {
      return [];
    }

    // Convert Map to array and sort states alphabetically
    const stateEntries = Array.from(metroAreasByState.entries()).sort(([stateA], [stateB]) =>
      stateA.localeCompare(stateB)
    );

    // Build tree structure: states as parents, metros as children
    return stateEntries.map(([state, metros]) => ({
      id: state,
      label: state,
      checked: false, // Parent nodes don't have direct checked state
      children: metros
        // Sort metros alphabetically within each state
        .sort((a, b) => a.name.localeCompare(b.name))
        .map((metro) => ({
          id: metro.id,
          label: metro.name,
          checked: value.includes(metro.id),
        })),
    }));
  }, [metroAreas, metroAreasByState, value]);

  // Determine placeholder text based on loading state
  const placeholder = isLoading
    ? 'Loading metro areas...'
    : 'Select your preferred metro areas';

  // Helper text shows selection range
  const helperText = `Select ${minSelection}-${maxSelection} metro area${
    maxSelection > 1 ? 's' : ''
  } where you want to see listings`;

  // Display error from either validation or API
  const displayError = error || apiError;

  return (
    <div className="space-y-2">
      {/* Label with required indicator */}
      <label className="text-sm font-medium block">
        Preferred Metro Areas {required && <span className="text-red-500">*</span>}
      </label>

      {/* Tree Dropdown - reuses existing component */}
      <TreeDropdown
        nodes={metroAreaNodes}
        selectedIds={value}
        onSelectionChange={onChange}
        placeholder={placeholder}
        maxSelections={maxSelection}
        disabled={isLoading}
      />

      {/* Helper text */}
      <p className="text-xs text-gray-600">{helperText}</p>

      {/* Error message */}
      {displayError && (
        <p className="text-sm text-red-600" role="alert">
          {displayError}
        </p>
      )}
    </div>
  );
}
