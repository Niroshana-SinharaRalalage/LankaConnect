'use client';

import { useState } from 'react';
import { MapPin, Check, ChevronDown, ChevronRight } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useProfileStore } from '@/presentation/store/useProfileStore';
import {
  getMetroById,
  getMetrosGroupedByState,
  US_STATES,
  isStateLevelArea,
  getStateLevelAreas,
} from '@/domain/constants/metroAreas.constants';
import { PROFILE_CONSTRAINTS } from '@/domain/constants/profile.constants';

/**
 * PreferredMetroAreasSection Component
 * Phase 5B: User Preferred Metro Areas
 *
 * Allows users to select 0-20 metro areas for location-based filtering
 * UI Pattern: State-grouped dropdown with expandable metros
 * - State-level selection (state-wide areas)
 * - City-level selection within expanded states
 * - Expand/collapse with [+] and [▼] indicators
 * - Pre-checked metros based on user's saved selections
 */
export function PreferredMetroAreasSection() {
  const { user } = useAuthStore();
  const { profile, updatePreferredMetroAreas, sectionStates, error, isLoading } = useProfileStore();

  const [isEditing, setIsEditing] = useState(false);
  const [selectedMetroAreas, setSelectedMetroAreas] = useState<string[]>([]);
  const [validationError, setValidationError] = useState<string>('');
  const [expandedStates, setExpandedStates] = useState<Set<string>>(new Set());

  const sectionState = sectionStates.preferredMetroAreas;
  const isSuccess = sectionState === 'success';
  const isError = sectionState === 'error';
  const isSaving = isLoading || sectionState === 'saving';

  const currentMetroAreas = profile?.preferredMetroAreas || [];
  const { max } = PROFILE_CONSTRAINTS.preferredMetroAreas;

  // Return null if user not authenticated
  if (!user) {
    return null;
  }

  const handleEdit = () => {
    setIsEditing(true);
    setSelectedMetroAreas([...currentMetroAreas]);
    setValidationError('');
    // Reset expanded states when entering edit mode
    setExpandedStates(new Set());
  };

  const handleCancel = () => {
    setIsEditing(false);
    setSelectedMetroAreas([]);
    setValidationError('');
    setExpandedStates(new Set());
  };

  const toggleStateExpansion = (stateCode: string) => {
    const newExpanded = new Set(expandedStates);
    if (newExpanded.has(stateCode)) {
      newExpanded.delete(stateCode);
    } else {
      newExpanded.add(stateCode);
    }
    setExpandedStates(newExpanded);
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

    try {
      await updatePreferredMetroAreas(user.userId, {
        metroAreaIds: selectedMetroAreas,
      });

      // Exit edit mode on success (store will set state to 'success')
      setIsEditing(false);
      setValidationError('');
      setExpandedStates(new Set());
    } catch (err) {
      // Error handled by store, stay in edit mode for retry
      console.error('Failed to save preferred metro areas:', err);
    }
  };

  // Get metro areas grouped by state using helper function
  const metrosByState = getMetrosGroupedByState();

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
          // ===== EDIT MODE: STATE-GROUPED DROPDOWN WITH EXPANDABLE METROS =====
          <div className="space-y-3">
            {/* State-Level Areas First (Statewide selections) */}
            <div>
              <h4 className="text-xs font-semibold uppercase tracking-wider mb-3" style={{ color: '#8B1538' }}>
                State-Wide Selections
              </h4>
              <div className="space-y-2">
                {getStateLevelAreas().map((metro) => {
                  const isSelected = selectedMetroAreas.includes(metro.id);
                  return (
                    <label
                      key={metro.id}
                      className={`flex items-center gap-3 p-2 rounded-md border cursor-pointer transition-colors ${
                        isSaving ? 'opacity-50 cursor-not-allowed' : ''
                      }`}
                      style={{
                        background: isSelected ? '#FFE8CC' : 'white',
                        borderColor: isSelected ? '#FF7900' : '#e2e8f0',
                      }}
                    >
                      <input
                        type="checkbox"
                        checked={isSelected}
                        onChange={() => handleToggleMetroArea(metro.id)}
                        disabled={isSaving}
                        className="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary"
                        aria-label={`Select all of ${metro.name}`}
                      />
                      <span className="flex-1 text-sm font-medium">All {metro.name}</span>
                    </label>
                  );
                })}
              </div>
            </div>

            {/* Divider */}
            <div className="h-px bg-gray-200 my-4" />

            {/* City-Level Areas (Grouped by State with Expand/Collapse) */}
            <div>
              <h4 className="text-xs font-semibold uppercase tracking-wider mb-3" style={{ color: '#8B1538' }}>
                City Metro Areas
              </h4>
              <div className="space-y-2">
                {US_STATES.map((state) => {
                  const metrosForState = metrosByState.get(state.code) || [];
                  // Filter to only city-level metros (not state-level)
                  const cityMetros = metrosForState.filter((m) => !isStateLevelArea(m.id));

                  if (cityMetros.length === 0) return null;

                  const isExpanded = expandedStates.has(state.code);
                  const selectedCountInState = selectedMetroAreas.filter(
                    (id) => metrosForState.map((m) => m.id).includes(id) && !isStateLevelArea(id)
                  ).length;

                  return (
                    <div key={state.code} className="border rounded-md overflow-hidden" style={{ borderColor: '#e2e8f0' }}>
                      {/* State Collapse/Expand Header */}
                      <button
                        onClick={() => toggleStateExpansion(state.code)}
                        disabled={isSaving}
                        className={`w-full flex items-center gap-2 p-3 text-left transition-colors ${
                          isSaving ? 'opacity-50 cursor-not-allowed' : 'hover:bg-gray-50'
                        }`}
                        aria-expanded={isExpanded}
                        aria-controls={`metros-${state.code}`}
                      >
                        {isExpanded ? (
                          <ChevronDown className="h-4 w-4" style={{ color: '#FF7900' }} />
                        ) : (
                          <ChevronRight className="h-4 w-4" style={{ color: '#FF7900' }} />
                        )}
                        <span className="flex-1 font-medium text-sm">{state.name}</span>
                        {selectedCountInState > 0 && (
                          <span
                            className="text-xs font-semibold px-2 py-0.5 rounded-full"
                            style={{ background: '#FFE8CC', color: '#8B1538' }}
                          >
                            {selectedCountInState} selected
                          </span>
                        )}
                      </button>

                      {/* Expandable Metro Area List */}
                      {isExpanded && (
                        <div
                          id={`metros-${state.code}`}
                          className="space-y-2 p-3 bg-gray-50 border-t"
                          style={{ borderColor: '#e2e8f0' }}
                        >
                          {cityMetros.map((metro) => {
                            const isSelected = selectedMetroAreas.includes(metro.id);
                            return (
                              <label
                                key={metro.id}
                                className={`flex items-start gap-3 p-2 rounded-md border cursor-pointer transition-colors ${
                                  isSaving ? 'opacity-50 cursor-not-allowed' : ''
                                }`}
                                style={{
                                  background: isSelected ? '#FFE8CC' : 'white',
                                  borderColor: isSelected ? '#FF7900' : '#e2e8f0',
                                }}
                              >
                                <input
                                  type="checkbox"
                                  checked={isSelected}
                                  onChange={() => handleToggleMetroArea(metro.id)}
                                  disabled={isSaving}
                                  className="mt-0.5 h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary"
                                  aria-label={`${metro.name}, ${metro.state}`}
                                />
                                <div className="flex-1 min-w-0">
                                  <div className="text-sm font-medium">{metro.name}</div>
                                  <div className="text-xs text-muted-foreground">
                                    {metro.cities.slice(0, 2).join(', ')}
                                    {metro.cities.length > 2 && `, +${metro.cities.length - 2} more`}
                                  </div>
                                </div>
                              </label>
                            );
                          })}
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>
            </div>

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
