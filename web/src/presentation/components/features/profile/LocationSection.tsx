'use client';

import React, { useState } from 'react';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useProfileStore } from '@/presentation/store/useProfileStore';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { PROFILE_CONSTRAINTS } from '@/domain/constants/profile.constants';
import { Check, MapPin } from 'lucide-react';

/**
 * LocationSection Component
 *
 * Manages user location information (City, State, ZipCode, Country)
 * - View mode: Display current location or "Not set"
 * - Edit mode: Form with validation for all 4 fields
 * - Integrates with useProfileStore for state and API calls
 * - Shows loading/success/error states
 *
 * Follows ProfilePhotoSection pattern for consistency
 */
export function LocationSection() {
  const { user, isAuthenticated } = useAuthStore();
  const { profile, error, sectionStates, updateLocation } = useProfileStore();

  const [isEditing, setIsEditing] = useState(false);
  const [formData, setFormData] = useState({
    city: '',
    state: '',
    zipCode: '',
    country: '',
  });
  const [validationErrors, setValidationErrors] = useState<Record<string, string>>({});

  // Don't render if not authenticated
  if (!isAuthenticated || !user) {
    return null;
  }

  const currentLocation = profile?.location;
  const isLoading = sectionStates.location === 'saving';
  const isSuccess = sectionStates.location === 'success';
  const isError = sectionStates.location === 'error';

  /**
   * Start editing mode
   * Pre-fill form with current location or empty strings
   */
  const handleEdit = () => {
    setFormData({
      city: currentLocation?.city || '',
      state: currentLocation?.state || '',
      zipCode: currentLocation?.zipCode || '',
      country: currentLocation?.country || '',
    });
    setValidationErrors({});
    setIsEditing(true);
  };

  /**
   * Cancel editing
   * Reset form and return to view mode
   */
  const handleCancel = () => {
    setIsEditing(false);
    setValidationErrors({});
  };

  /**
   * Validate form data
   * Returns true if valid, false otherwise
   */
  const validateForm = (): boolean => {
    const errors: Record<string, string> = {};

    // City validation
    if (!formData.city.trim()) {
      errors.city = 'City is required';
    } else if (formData.city.length > PROFILE_CONSTRAINTS.location.cityMaxLength) {
      errors.city = `City cannot exceed ${PROFILE_CONSTRAINTS.location.cityMaxLength} characters`;
    }

    // State validation
    if (!formData.state.trim()) {
      errors.state = 'State is required';
    } else if (formData.state.length > PROFILE_CONSTRAINTS.location.stateMaxLength) {
      errors.state = `State cannot exceed ${PROFILE_CONSTRAINTS.location.stateMaxLength} characters`;
    }

    // Zip code validation
    if (!formData.zipCode.trim()) {
      errors.zipCode = 'Zip code is required';
    } else if (formData.zipCode.length > PROFILE_CONSTRAINTS.location.zipCodeMaxLength) {
      errors.zipCode = `Zip code cannot exceed ${PROFILE_CONSTRAINTS.location.zipCodeMaxLength} characters`;
    }

    // Country validation
    if (!formData.country.trim()) {
      errors.country = 'Country is required';
    } else if (formData.country.length > PROFILE_CONSTRAINTS.location.countryMaxLength) {
      errors.country = `Country cannot exceed ${PROFILE_CONSTRAINTS.location.countryMaxLength} characters`;
    }

    setValidationErrors(errors);
    return Object.keys(errors).length === 0;
  };

  /**
   * Handle form submission
   * Validate and call API
   */
  const handleSave = async () => {
    if (!validateForm()) {
      return;
    }

    try {
      await updateLocation(user.userId, {
        city: formData.city.trim(),
        state: formData.state.trim(),
        zipCode: formData.zipCode.trim(),
        country: formData.country.trim(),
      });
      // Exit edit mode on success (store sets state to 'success')
      setIsEditing(false);
    } catch (error) {
      // Error state is handled by the store
      // Keep editing mode open so user can retry
    }
  };

  /**
   * Handle input change
   * Update form data and clear validation error for that field
   */
  const handleInputChange = (field: keyof typeof formData, value: string) => {
    setFormData((prev) => ({ ...prev, [field]: value }));

    // Clear validation error for this field
    if (validationErrors[field]) {
      setValidationErrors((prev) => {
        const newErrors = { ...prev };
        delete newErrors[field];
        return newErrors;
      });
    }
  };

  return (
    <section role="region" aria-labelledby="location-heading">
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <MapPin className="h-5 w-5" style={{ color: '#FF7900' }} />
              <CardTitle id="location-heading" style={{ color: '#8B1538' }}>Location</CardTitle>
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
            Help others in the community find you by sharing your location
          </CardDescription>
        </CardHeader>
        <CardContent>
          {!isEditing ? (
            // View Mode
            <div className="space-y-2">
              {currentLocation ? (
                <div className="text-sm">
                  <p className="font-medium text-foreground">
                    {currentLocation.city}, {currentLocation.state} {currentLocation.zipCode}
                  </p>
                  <p className="text-muted-foreground">{currentLocation.country}</p>
                </div>
              ) : (
                <p className="text-sm text-muted-foreground italic">
                  Not set - Click Edit to add your location
                </p>
              )}

              {/* Success message */}
              {isSuccess && (
                <div className="flex items-center gap-2 text-sm" style={{ color: '#006400' }}>
                  <Check className="h-4 w-4" />
                  <span>Location saved successfully!</span>
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
              {/* City */}
              <div>
                <label htmlFor="city" className="block text-sm font-medium mb-1">
                  City *
                </label>
                <Input
                  id="city"
                  type="text"
                  value={formData.city}
                  onChange={(e) => handleInputChange('city', e.target.value)}
                  placeholder="e.g., Toronto"
                  disabled={isLoading}
                  error={!!validationErrors.city}
                  aria-label="City"
                  aria-invalid={!!validationErrors.city}
                  aria-describedby={validationErrors.city ? 'city-error' : undefined}
                  maxLength={PROFILE_CONSTRAINTS.location.cityMaxLength}
                />
                {validationErrors.city && (
                  <p id="city-error" className="text-sm text-destructive mt-1" role="alert">
                    {validationErrors.city}
                  </p>
                )}
              </div>

              {/* State/Province */}
              <div>
                <label htmlFor="state" className="block text-sm font-medium mb-1">
                  State/Province *
                </label>
                <Input
                  id="state"
                  type="text"
                  value={formData.state}
                  onChange={(e) => handleInputChange('state', e.target.value)}
                  placeholder="e.g., Ontario"
                  disabled={isLoading}
                  error={!!validationErrors.state}
                  aria-label="State/Province"
                  aria-invalid={!!validationErrors.state}
                  aria-describedby={validationErrors.state ? 'state-error' : undefined}
                  maxLength={PROFILE_CONSTRAINTS.location.stateMaxLength}
                />
                {validationErrors.state && (
                  <p id="state-error" className="text-sm text-destructive mt-1" role="alert">
                    {validationErrors.state}
                  </p>
                )}
              </div>

              {/* Zip/Postal Code */}
              <div>
                <label htmlFor="zipCode" className="block text-sm font-medium mb-1">
                  Zip/Postal Code *
                </label>
                <Input
                  id="zipCode"
                  type="text"
                  value={formData.zipCode}
                  onChange={(e) => handleInputChange('zipCode', e.target.value)}
                  placeholder="e.g., M5H 2N2"
                  disabled={isLoading}
                  error={!!validationErrors.zipCode}
                  aria-label="Zip/Postal Code"
                  aria-invalid={!!validationErrors.zipCode}
                  aria-describedby={validationErrors.zipCode ? 'zipCode-error' : undefined}
                  maxLength={PROFILE_CONSTRAINTS.location.zipCodeMaxLength}
                />
                {validationErrors.zipCode && (
                  <p id="zipCode-error" className="text-sm text-destructive mt-1" role="alert">
                    {validationErrors.zipCode}
                  </p>
                )}
              </div>

              {/* Country */}
              <div>
                <label htmlFor="country" className="block text-sm font-medium mb-1">
                  Country *
                </label>
                <Input
                  id="country"
                  type="text"
                  value={formData.country}
                  onChange={(e) => handleInputChange('country', e.target.value)}
                  placeholder="e.g., Canada"
                  disabled={isLoading}
                  error={!!validationErrors.country}
                  aria-label="Country"
                  aria-invalid={!!validationErrors.country}
                  aria-describedby={validationErrors.country ? 'country-error' : undefined}
                  maxLength={PROFILE_CONSTRAINTS.location.countryMaxLength}
                />
                {validationErrors.country && (
                  <p id="country-error" className="text-sm text-destructive mt-1" role="alert">
                    {validationErrors.country}
                  </p>
                )}
              </div>

              {/* Error message */}
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
                  {isLoading ? 'Saving...' : 'Save Location'}
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
