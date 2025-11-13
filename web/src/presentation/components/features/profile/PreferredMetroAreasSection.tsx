'use client';

import { useState, useMemo } from 'react';
import { MapPin, Check } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { TreeDropdown, type TreeNode } from '@/presentation/components/ui/TreeDropdown';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useProfileStore } from '@/presentation/store/useProfileStore';
import { useMetroAreas } from '@/presentation/hooks/useMetroAreas';
import { US_STATES } from '@/domain/constants/metroAreas.constants';
import { PROFILE_CONSTRAINTS } from '@/domain/constants/profile.constants';

/**
 * PreferredMetroAreasSection Component
 * Phase 5B: User Preferred Metro Areas
 * Phase 6A.9: Updated to use TreeDropdown with API data
 *
 * Allows users to select 0-20 metro areas for location-based filtering
 * UI Pattern: Tree dropdown with state-grouped metros
 * - State-level selection (state-wide areas)
 * - City-level selection within expanded states
 * - Expand/collapse with [+] and [▼] indicators
 * - Pre-checked metros based on user's saved selections
 */
export function PreferredMetroAreasSection() {
  const { user } = useAuthStore();
  const { profile, updatePreferredMetroAreas, sectionStates, error, isLoading } = useProfileStore();

  // Phase 6A.9: Fetch metro areas from API instead of hardcoded constants
  const {
    metroAreas,
    metroAreasByState,
    stateLevelMetros,
    isLoading: metrosLoading,
    error: metrosError,
  } = useMetroAreas();

  const [isEditing, setIsEditing] = useState(false);
  const [selectedMetroAreas, setSelectedMetroAreas] = useState<string[]>([]);
  const [validationError, setValidationError] = useState<string>('');

  const sectionState = sectionStates.preferredMetroAreas;
  const isSuccess = sectionState === 'success';
  const isError = sectionState === 'error';
  const isSaving = isLoading || sectionState === 'saving';

  const currentMetroAreas = profile?.preferredMetroAreas || [];
  const { max } = PROFILE_CONSTRAINTS.preferredMetroAreas;

  // Convert metro areas data to tree structure for TreeDropdown
  // MUST be called before any early returns (Rules of Hooks)
  const treeNodes = useMemo<TreeNode[]>(() => {
    const nodes: TreeNode[] = [];

    US_STATES.forEach((state) => {
      const metrosForState = metroAreasByState.get(state.code) || [];

      if (metrosForState.length === 0) return;

      // Separate state-level and city-level metros
      const stateMetro = metrosForState.find((m) => m.isStateLevelArea);
      const cityMetros = metrosForState.filter((m) => !m.isStateLevelArea);

      const children: TreeNode[] = [];

      // Add state-level metro first if it exists
      if (stateMetro) {
        children.push({
          id: stateMetro.id,
          label: stateMetro.name,
          checked: selectedMetroAreas.includes(stateMetro.id),
        });
      }

      // Add city-level metros
      cityMetros.forEach((metro) => {
        children.push({
          id: metro.id,
          label: metro.name,
          checked: selectedMetroAreas.includes(metro.id),
        });
      });

      nodes.push({
        id: state.code,
        label: state.name,
        checked: false, // Parent nodes don't have checkboxes in our design
        children,
      });
    });

    return nodes;
  }, [metroAreasByState, stateLevelMetros, selectedMetroAreas]);

  // Return null if user not authenticated
  if (!user) {
    return null;
  }

  // Show loading state while fetching metros
  if (metrosLoading) {
    return (
      <Card role="region" aria-label="Preferred Metro Areas">
        <CardHeader>
          <div className="flex items-center gap-2">
            <MapPin className="h-5 w-5" style={{ color: '#FF7900' }} />
            <CardTitle style={{ color: '#8B1538' }}>Preferred Metro Areas</CardTitle>
          </div>
          <CardDescription>Loading metro areas...</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex items-center justify-center p-4">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2" style={{ borderColor: '#FF7900' }}></div>
          </div>
        </CardContent>
      </Card>
    );
  }

  // Show error if metros failed to load
  if (metrosError) {
    return (
      <Card role="region" aria-label="Preferred Metro Areas">
        <CardHeader>
          <div className="flex items-center gap-2">
            <MapPin className="h-5 w-5" style={{ color: '#FF7900' }} />
            <CardTitle style={{ color: '#8B1538' }}>Preferred Metro Areas</CardTitle>
          </div>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-destructive" role="alert">
            Failed to load metro areas: {metrosError}
          </p>
        </CardContent>
      </Card>
    );
  }

  const handleEdit = () => {
    setIsEditing(true);
    setSelectedMetroAreas([...currentMetroAreas]);
    setValidationError('');
  };

  const handleCancel = () => {
    setIsEditing(false);
    setSelectedMetroAreas([]);
    setValidationError('');
  };

  const handleSelectionChange = (newSelectedIds: string[]) => {
    if (newSelectedIds.length > max) {
      setValidationError(`You cannot select more than ${max} metro areas`);
      return;
    }
    setValidationError('');
    setSelectedMetroAreas(newSelectedIds);
  };

  const handleToggleMetroArea = (metroId: string) => {
    setSelectedMetroAreas((prev) => {
      if (prev.includes(metroId)) {
        // Remove if already selected
        setValidationError(''); // Clear error when removing
        return prev.filter((id) => id !== metroId);
      } else {
        // Add (check max limit first)
        if (prev.length >= max) {
          setValidationError(`You cannot select more than ${max} metro areas`);
          return prev; // Don't add
        }
        setValidationError(''); // Clear error
        return [...prev, metroId];
      }
    });
  };

  const handleSave = async () => {
    if (!user?.userId) return;

    console.log('=== DEBUG: Attempting to save metro areas ===');
    console.log('Selected IDs:', selectedMetroAreas);
    console.log('Selected Count:', selectedMetroAreas.length);
    selectedMetroAreas.forEach(id => {
      const metro = getMetroById(id);
      console.log(`  - ${id}: ${metro ? `${metro.name}, ${metro.state}` : 'NOT FOUND IN LOCAL DATA'}`);
    });

    try {
      await updatePreferredMetroAreas(user.userId, {
        metroAreaIds: selectedMetroAreas,
      });

      console.log('✅ Save successful');
      // Exit edit mode on success (store will set state to 'success')
      setIsEditing(false);
      setValidationError('');
    } catch (err) {
      // Error handled by store, stay in edit mode for retry
      console.error('❌ Save failed:', err);
    }
  };

  // Helper to get metro by ID from API data
  const getMetroById = (id: string) => {
    return metroAreas.find(metro => metro.id === id);
  };

  // Helper to check if metro is state-level
  const isStateLevelArea = (id: string) => {
    const metro = getMetroById(id);
    return metro?.isStateLevelArea ?? false;
  };

  return (
    <Card role="region" aria-label="Preferred Metro Areas">
      <CardHeader>
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <MapPin className="h-5 w-5" style={{ color: '#FF7900' }} />
            <CardTitle style={{ color: '#8B1538' }}>Preferred Metro Areas</CardTitle>
          </div>
          {!isEditing && (
            <Button
              variant="outline"
              size="sm"
              onClick={handleEdit}
              disabled={isSaving}
              style={{ borderColor: '#FF7900', color: '#8B1538' }}
            >
              Edit
            </Button>
          )}
        </div>
        <CardDescription>
          Select up to {max} metro areas for personalized event and content filtering. You can opt out by leaving
          this empty.
        </CardDescription>
      </CardHeader>
      <CardContent>
        {!isEditing ? (
          // ===== VIEW MODE =====
          <div className="space-y-2">
            {currentMetroAreas.length > 0 ? (
              <div className="flex flex-wrap gap-2">
                {currentMetroAreas.map((metroId) => {
                  const metro = getMetroById(metroId);
                  return metro ? (
                    <div
                      key={metroId}
                      className="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium"
                      style={{ background: '#FFE8CC', color: '#8B1538' }}
                    >
                      {metro.name}
                      {!isStateLevelArea(metroId) && `, ${metro.state}`}
                    </div>
                  ) : null;
                })}
              </div>
            ) : (
              <p className="text-sm text-muted-foreground italic">
                No metro areas selected - Click Edit to add your preferred locations
              </p>
            )}

            {/* Success message */}
            {isSuccess && (
              <div className="flex items-center gap-2 text-sm" style={{ color: '#006400' }}>
                <Check className="h-4 w-4" />
                <span>Preferred metro areas saved successfully!</span>
              </div>
            )}

            {/* Error message */}
            {isError && error && (
              <p className="text-sm text-destructive" role="alert">
                {error}
              </p>
            )}
          </div>
        ) : (
          // ===== EDIT MODE: TREE DROPDOWN =====
          <div className="space-y-3">
            <p className="text-sm text-muted-foreground mb-2">
              Select metro areas by expanding states and checking your preferred locations
            </p>

            <TreeDropdown
              nodes={treeNodes}
              selectedIds={selectedMetroAreas}
              onSelectionChange={handleSelectionChange}
              placeholder={`Select up to ${max} metro areas`}
              maxSelections={max}
              disabled={isSaving}
              className="w-full"
            />

            {/* Selection counter and validation */}
            <div className="flex items-center justify-between pt-2 border-t" style={{ borderColor: '#e2e8f0' }}>
              <p className="text-sm text-muted-foreground">
                {selectedMetroAreas.length} of {max} selected
              </p>

              {validationError && (
                <p className="text-sm text-destructive" role="alert">
                  {validationError}
                </p>
              )}
            </div>

            {/* Action Buttons */}
            <div className="flex gap-3 pt-2">
              <Button
                onClick={handleSave}
                disabled={isSaving || !!validationError}
                className="flex-1 text-white"
                style={{ background: '#FF7900' }}
              >
                {isSaving ? 'Saving...' : 'Save Changes'}
              </Button>
              <Button
                onClick={handleCancel}
                variant="outline"
                disabled={isSaving}
                className="flex-1"
                style={{ borderColor: '#8B1538', color: '#8B1538' }}
              >
                Cancel
              </Button>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
