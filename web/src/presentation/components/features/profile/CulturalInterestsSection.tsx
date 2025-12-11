'use client';

import React, { useState } from 'react';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useProfileStore } from '@/presentation/store/useProfileStore';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { CULTURAL_INTERESTS, PROFILE_CONSTRAINTS } from '@/domain/constants/profile.constants';
import { Check, Heart, X } from 'lucide-react';

/**
 * CulturalInterestsSection Component
 *
 * Manages user's cultural interests (0-10 from predefined list)
 * - View mode: Display selected interests as badges
 * - Edit mode: Multi-select checkboxes
 * - Integrates with useProfileStore for state and API calls
 * - Shows loading/success/error states
 *
 * Follows ProfilePhotoSection pattern for consistency
 */
export function CulturalInterestsSection() {
  const { user, isAuthenticated } = useAuthStore();
  const { profile, error, sectionStates, updateCulturalInterests } = useProfileStore();

  const [isEditing, setIsEditing] = useState(false);
  const [selectedInterests, setSelectedInterests] = useState<string[]>([]);
  const [validationError, setValidationError] = useState<string>('');

  // Don't render if not authenticated
  if (!isAuthenticated || !user) {
    return null;
  }

  const currentInterests = profile?.culturalInterests || [];
  const isLoading = sectionStates.culturalInterests === 'saving';
  const isSuccess = sectionStates.culturalInterests === 'success';
  const isError = sectionStates.culturalInterests === 'error';

  /**
   * Start editing mode
   * Pre-fill with current interests
   */
  const handleEdit = () => {
    setSelectedInterests([...currentInterests]);
    setValidationError('');
    setIsEditing(true);
  };

  /**
   * Cancel editing
   */
  const handleCancel = () => {
    setIsEditing(false);
    setValidationError('');
  };

  /**
   * Toggle interest selection
   */
  const handleToggleInterest = (code: string) => {
    setSelectedInterests((prev) => {
      if (prev.includes(code)) {
        // Remove
        return prev.filter((c) => c !== code);
      } else {
        // Add (check max limit)
        if (prev.length >= PROFILE_CONSTRAINTS.culturalInterests.max) {
          setValidationError(
            `You can select up to ${PROFILE_CONSTRAINTS.culturalInterests.max} interests`
          );
          return prev;
        }
        setValidationError('');
        return [...prev, code];
      }
    });
  };

  /**
   * Handle form submission
   */
  const handleSave = async () => {
    // Validate (0-10 allowed)
    if (selectedInterests.length > PROFILE_CONSTRAINTS.culturalInterests.max) {
      setValidationError(
        `Maximum ${PROFILE_CONSTRAINTS.culturalInterests.max} interests allowed`
      );
      return;
    }

    try {
      await updateCulturalInterests(user.userId, {
        InterestCodes: selectedInterests, // PascalCase to match backend (UsersController.cs:731)
      });
      // Exit edit mode on success (store sets state to 'success')
      setIsEditing(false);
    } catch (error) {
      // Error state is handled by the store
      // Keep editing mode open so user can retry
    }
  };

  return (
    <section role="region" aria-labelledby="cultural-interests-heading">
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Heart className="h-5 w-5" style={{ color: '#FF7900' }} />
              <CardTitle id="cultural-interests-heading" style={{ color: '#8B1538' }}>Cultural Interests</CardTitle>
            </div>
            {!isEditing && (
              <Button
                onClick={handleEdit}
                variant="outline"
                size="sm"
                disabled={isLoading}
                style={{ borderColor: '#FF7900', color: '#8B1538' }}
              >
                Edit
              </Button>
            )}
          </div>
          <CardDescription>
            Share your cultural interests to connect with others (select 0-{PROFILE_CONSTRAINTS.culturalInterests.max})
          </CardDescription>
        </CardHeader>
        <CardContent>
          {!isEditing ? (
            // View Mode
            <div className="space-y-2">
              {currentInterests.length > 0 ? (
                <div className="flex flex-wrap gap-2">
                  {currentInterests.map((code) => {
                    const interest = CULTURAL_INTERESTS.find((i) => i.code === code);
                    return interest ? (
                      <span
                        key={code}
                        className="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium"
                        style={{ background: '#FFE8CC', color: '#8B1538' }}
                      >
                        {interest.name}
                      </span>
                    ) : null;
                  })}
                </div>
              ) : (
                <p className="text-sm text-muted-foreground italic">
                  No interests selected - Click Edit to add your cultural interests
                </p>
              )}

              {/* Success message */}
              {isSuccess && (
                <div className="flex items-center gap-2 text-sm" style={{ color: '#006400' }}>
                  <Check className="h-4 w-4" />
                  <span>Cultural interests saved successfully!</span>
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
            // Edit Mode
            <div className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                {CULTURAL_INTERESTS.map((interest) => {
                  const isSelected = selectedInterests.includes(interest.code);
                  return (
                    <label
                      key={interest.code}
                      className={`flex items-start gap-3 p-3 rounded-lg border cursor-pointer transition-colors ${
                        isLoading ? 'opacity-50 cursor-not-allowed' : ''
                      }`}
                      style={{
                        background: isSelected ? '#FFE8CC' : 'white',
                        borderColor: isSelected ? '#FF7900' : '#e2e8f0'
                      }}
                    >
                      <input
                        type="checkbox"
                        checked={isSelected}
                        onChange={() => handleToggleInterest(interest.code)}
                        disabled={isLoading}
                        className="mt-1 h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary"
                        aria-label={interest.name}
                      />
                      <span className="text-sm flex-1">{interest.name}</span>
                    </label>
                  );
                })}
              </div>

              {/* Selected count */}
              <p className="text-sm text-muted-foreground">
                {selectedInterests.length} of {PROFILE_CONSTRAINTS.culturalInterests.max} selected
              </p>

              {/* Validation error */}
              {validationError && (
                <p className="text-sm text-destructive" role="alert">
                  {validationError}
                </p>
              )}

              {/* API error */}
              {isError && error && (
                <p className="text-sm text-destructive" role="alert">
                  {error}
                </p>
              )}

              {/* Action Buttons */}
              <div className="flex gap-3 pt-2">
                <Button
                  onClick={handleSave}
                  disabled={isLoading}
                  className="flex-1 text-white"
                  style={{ background: '#FF7900' }}
                >
                  {isLoading ? 'Saving...' : 'Save Interests'}
                </Button>
                <Button
                  onClick={handleCancel}
                  variant="outline"
                  disabled={isLoading}
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
    </section>
  );
}
