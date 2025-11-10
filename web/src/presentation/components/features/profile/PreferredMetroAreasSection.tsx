'use client';

import { useState, useEffect } from 'react';
import { MapPin, Check } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useProfileStore } from '@/presentation/store/useProfileStore';
import { ALL_METRO_AREAS, getMetroAreaById } from '@/domain/constants/metroAreas.constants';
import { PROFILE_CONSTRAINTS } from '@/domain/constants/profile.constants';

/**
 * PreferredMetroAreasSection Component
 * Phase 5B: User Preferred Metro Areas
 *
 * Allows users to select 0-10 metro areas for location-based filtering
 * Follows the pattern from CulturalInterestsSection
 */
export function PreferredMetroAreasSection() {
  const { user } = useAuthStore();
  const { profile, updatePreferredMetroAreas, sectionStates, error, isLoading } = useProfileStore();

  const [isEditing, setIsEditing] = useState(false);
  const [selectedMetroAreas, setSelectedMetroAreas] = useState<string[]>([]);
  const [validationError, setValidationError] = useState<string>('');

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
  };

  const handleCancel = () => {
    setIsEditing(false);
    setSelectedMetroAreas([]);
    setValidationError('');
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
    } catch (err) {
      // Error handled by store, stay in edit mode for retry
      console.error('Failed to save preferred metro areas:', err);
    }
  };

  // Group metro areas by state for better UX
  const ohioAreas = ALL_METRO_AREAS.filter((m) => m.state === 'OH' && !m.id.includes('all-'));
  const otherAreas = ALL_METRO_AREAS.filter((m) => m.state !== 'OH' && !m.id.includes('all-'));
  const stateLevelAreas = ALL_METRO_AREAS.filter((m) => m.id.includes('all-'));

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
                  const metro = getMetroAreaById(metroId);
                  return (
                    <div
                      key={metroId}
                      className="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium"
                      style={{ background: '#FFE8CC', color: '#8B1538' }}
                    >
                      {metro?.name}, {metro?.state}
                    </div>
                  );
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
                <span>Section saved successfully!</span>
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
          // ===== EDIT MODE =====
          <div className="space-y-4">
            {/* Ohio Metro Areas */}
            {ohioAreas.length > 0 && (
              <div>
                <h4 className="text-sm font-medium mb-2" style={{ color: '#8B1538' }}>
                  Ohio Metro Areas
                </h4>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                  {ohioAreas.map((metro) => {
                    const isSelected = selectedMetroAreas.includes(metro.id);
                    return (
                      <label
                        key={metro.id}
                        className={`flex items-start gap-3 p-3 rounded-lg border cursor-pointer transition-colors ${
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
                          className="mt-1 h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary"
                          aria-label={`${metro.name}, ${metro.state}`}
                        />
                        <div className="flex-1 text-sm">
                          <div className="font-medium">{metro.name}</div>
                          <div className="text-xs text-muted-foreground">
                            {metro.cities.slice(0, 3).join(', ')}
                            {metro.cities.length > 3 && `, +${metro.cities.length - 3} more`}
                          </div>
                        </div>
                      </label>
                    );
                  })}
                </div>
              </div>
            )}

            {/* Other Major US Metro Areas */}
            {otherAreas.length > 0 && (
              <div>
                <h4 className="text-sm font-medium mb-2" style={{ color: '#8B1538' }}>
                  Other US Metro Areas
                </h4>
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
                  {otherAreas.map((metro) => {
                    const isSelected = selectedMetroAreas.includes(metro.id);
                    return (
                      <label
                        key={metro.id}
                        className={`flex items-start gap-3 p-3 rounded-lg border cursor-pointer transition-colors ${
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
                          className="mt-1 h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary"
                          aria-label={`${metro.name}, ${metro.state}`}
                        />
                        <span className="flex-1 text-sm font-medium">
                          {metro.name}, {metro.state}
                        </span>
                      </label>
                    );
                  })}
                </div>
              </div>
            )}

            {/* State-Level Areas */}
            {stateLevelAreas.length > 0 && (
              <div>
                <h4 className="text-sm font-medium mb-2" style={{ color: '#8B1538' }}>
                  State-Level Areas
                </h4>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                  {stateLevelAreas.map((metro) => {
                    const isSelected = selectedMetroAreas.includes(metro.id);
                    return (
                      <label
                        key={metro.id}
                        className={`flex items-start gap-3 p-3 rounded-lg border cursor-pointer transition-colors ${
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
                          className="mt-1 h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary"
                          aria-label={`${metro.name}`}
                        />
                        <span className="flex-1 text-sm font-medium">{metro.name}</span>
                      </label>
                    );
                  })}
                </div>
              </div>
            )}

            {/* Selection counter */}
            <p className="text-sm text-muted-foreground">
              {selectedMetroAreas.length} of {max} selected
            </p>

            {/* Validation error */}
            {validationError && (
              <p className="text-sm text-destructive" role="alert">
                {validationError}
              </p>
            )}

            {/* Action Buttons */}
            <div className="flex gap-3 pt-2">
              <Button
                onClick={handleSave}
                disabled={isSaving || !!validationError}
                className="flex-1 text-white"
                style={{ background: '#FF7900' }}
              >
                {isSaving ? 'Saving...' : 'Save Section'}
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
