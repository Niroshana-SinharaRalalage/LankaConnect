'use client';

import React, { useState } from 'react';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useProfileStore } from '@/presentation/store/useProfileStore';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { PROFILE_CONSTRAINTS } from '@/domain/constants/profile.constants';
import { Check, User, CheckCircle2, AlertCircle, Mail } from 'lucide-react';
import { apiClient } from '@/infrastructure/api/client/api-client';

/**
 * BasicInfoSection Component
 * Phase 6A.70 + Enhancements: Profile Basic Info with Location & Email Verification
 *
 * Manages user basic information and location in single form:
 * - First Name, Last Name (required)
 * - Email (editable with verification flow)
 * - Phone Number (optional)
 * - City, State, Zip Code, Country (location fields)
 *
 * Features:
 * - View/Edit mode toggle
 * - Email verification status with resend button
 * - Email editing triggers verification flow
 * - Combined save for basic info + location
 * - Client-side validation with error messages
 * - Section-specific loading/success/error states
 */
export function BasicInfoSection() {
  const { user, isAuthenticated } = useAuthStore();
  const { profile, error, sectionStates, updateBasicInfo, updateLocation, updateEmail } = useProfileStore();

  const [isEditing, setIsEditing] = useState(false);
  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    email: '',
    phoneNumber: '',
    // Location fields (merged from LocationSection)
    city: '',
    state: '',
    zipCode: '',
    country: '',
  });
  const [validationErrors, setValidationErrors] = useState<Record<string, string>>({});
  const [resendingVerification, setResendingVerification] = useState(false);

  // Don't render if not authenticated
  if (!isAuthenticated || !user) {
    return null;
  }

  const isLoading = sectionStates.basicInfo === 'saving' || sectionStates.location === 'saving';
  const isSuccess = sectionStates.basicInfo === 'success';
  const isError = sectionStates.basicInfo === 'error' || sectionStates.location === 'error';

  /**
   * Start editing mode
   * Pre-fill form with current profile data
   */
  const handleEdit = () => {
    setFormData({
      firstName: profile?.firstName || '',
      lastName: profile?.lastName || '',
      email: profile?.email || '',
      phoneNumber: profile?.phoneNumber || '',
      city: profile?.location?.city || '',
      state: profile?.location?.state || '',
      zipCode: profile?.location?.zipCode || '',
      country: profile?.location?.country || '',
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
   * Resend email verification
   */
  const handleResendVerification = async () => {
    if (!user?.userId || resendingVerification) return;

    setResendingVerification(true);
    try {
      await apiClient.post('/communications/send-email-verification', { userId: user.userId });
      alert('Verification email sent! Please check your inbox and spam folder.');
    } catch (error: any) {
      const message = error?.response?.data?.detail || error?.message || 'Failed to resend verification email';
      alert(message);
    } finally {
      setResendingVerification(false);
    }
  };

  /**
   * Validate form data
   * Returns true if valid, false otherwise
   */
  const validateForm = (): boolean => {
    const errors: Record<string, string> = {};

    // First name validation (required)
    if (!formData.firstName.trim()) {
      errors.firstName = 'First name is required';
    } else if (formData.firstName.trim().length < PROFILE_CONSTRAINTS.basicInfo.firstNameMinLength) {
      errors.firstName = 'First name is too short';
    } else if (formData.firstName.length > PROFILE_CONSTRAINTS.basicInfo.firstNameMaxLength) {
      errors.firstName = `First name cannot exceed ${PROFILE_CONSTRAINTS.basicInfo.firstNameMaxLength} characters`;
    }

    // Last name validation (required)
    if (!formData.lastName.trim()) {
      errors.lastName = 'Last name is required';
    } else if (formData.lastName.trim().length < PROFILE_CONSTRAINTS.basicInfo.lastNameMinLength) {
      errors.lastName = 'Last name is too short';
    } else if (formData.lastName.length > PROFILE_CONSTRAINTS.basicInfo.lastNameMaxLength) {
      errors.lastName = `Last name cannot exceed ${PROFILE_CONSTRAINTS.basicInfo.lastNameMaxLength} characters`;
    }

    // Email validation (required)
    if (!formData.email.trim()) {
      errors.email = 'Email is required';
    } else {
      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      if (!emailRegex.test(formData.email.trim())) {
        errors.email = 'Invalid email format';
      }
    }

    // Phone number validation (optional)
    if (formData.phoneNumber.trim()) {
      if (formData.phoneNumber.length > PROFILE_CONSTRAINTS.basicInfo.phoneNumberMaxLength) {
        errors.phoneNumber = `Phone number cannot exceed ${PROFILE_CONSTRAINTS.basicInfo.phoneNumberMaxLength} characters`;
      }
      const phoneRegex = /^(\+?[1-9]\d{1,14}|0\d{9})$/;
      if (!phoneRegex.test(formData.phoneNumber.trim())) {
        errors.phoneNumber = 'Invalid phone format. Use +94771234567 or 0771234567';
      }
    }

    // Location validation (all fields required if any is filled)
    const locationFilled = formData.city.trim() || formData.state.trim() ||
                           formData.zipCode.trim() || formData.country.trim();

    if (locationFilled) {
      if (!formData.city.trim()) errors.city = 'City is required';
      if (!formData.state.trim()) errors.state = 'State is required';
      if (!formData.zipCode.trim()) errors.zipCode = 'Zip code is required';
      if (!formData.country.trim()) errors.country = 'Country is required';

      if (formData.city.length > PROFILE_CONSTRAINTS.location.cityMaxLength) {
        errors.city = `City cannot exceed ${PROFILE_CONSTRAINTS.location.cityMaxLength} characters`;
      }
      if (formData.state.length > PROFILE_CONSTRAINTS.location.stateMaxLength) {
        errors.state = `State cannot exceed ${PROFILE_CONSTRAINTS.location.stateMaxLength} characters`;
      }
      if (formData.zipCode.length > PROFILE_CONSTRAINTS.location.zipCodeMaxLength) {
        errors.zipCode = `Zip code cannot exceed ${PROFILE_CONSTRAINTS.location.zipCodeMaxLength} characters`;
      }
      if (formData.country.length > PROFILE_CONSTRAINTS.location.countryMaxLength) {
        errors.country = `Country cannot exceed ${PROFILE_CONSTRAINTS.location.countryMaxLength} characters`;
      }
    }

    setValidationErrors(errors);
    return Object.keys(errors).length === 0;
  };

  /**
   * Handle form submission
   * Saves basic info and location (two API calls)
   */
  const handleSave = async () => {
    if (!validateForm()) {
      return;
    }

    try {
      const emailChanged = formData.email.trim() !== profile?.email;

      // Step 1: Update basic info (name, phone)
      await updateBasicInfo(user.userId, {
        firstName: formData.firstName.trim(),
        lastName: formData.lastName.trim(),
        phoneNumber: formData.phoneNumber.trim() || null,
        bio: null, // Bio removed per user request
      });

      // Step 2: Update location (city, state, zip, country)
      const locationFilled = formData.city.trim() || formData.state.trim() ||
                             formData.zipCode.trim() || formData.country.trim();

      if (locationFilled) {
        await updateLocation(user.userId, {
          city: formData.city.trim() || null,
          state: formData.state.trim() || null,
          zipCode: formData.zipCode.trim() || null,
          country: formData.country.trim() || null,
        });
      } else {
        // Clear location if all fields empty
        await updateLocation(user.userId, {
          city: null,
          state: null,
          zipCode: null,
          country: null,
        });
      }

      // Step 3: Update email if changed (triggers verification)
      if (emailChanged) {
        try {
          const result = await updateEmail(user.userId, formData.email.trim());
          alert(result.message + '\n\nYou may need to log out and log back in to see the updated email.');
        } catch (emailError: any) {
          // Show email-specific error but don't block other updates
          const emailErrorMsg = emailError?.response?.data?.detail || emailError?.message || 'Failed to update email';
          alert(`Profile saved, but email update failed: ${emailErrorMsg}`);
        }
      }

      // Success - exit edit mode
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
    <section role="region" aria-labelledby="basic-info-heading">
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <User className="h-5 w-5" style={{ color: '#FF7900' }} />
              <CardTitle id="basic-info-heading" style={{ color: '#8B1538' }}>
                Basic Information
              </CardTitle>
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
            Manage your personal information and location details
          </CardDescription>
        </CardHeader>
        <CardContent>
          {!isEditing ? (
            // View Mode
            <div className="space-y-3">
              {/* Name */}
              <div>
                <p className="text-sm text-muted-foreground">Name</p>
                <p className="font-medium text-foreground">
                  {profile?.firstName} {profile?.lastName}
                </p>
              </div>

              {/* Email with verification status */}
              <div>
                <p className="text-sm text-muted-foreground">Email</p>
                <div className="space-y-2">
                  <div className="flex items-center gap-2">
                    <p className="font-medium text-foreground">{profile?.email}</p>
                    {user.isEmailVerified ? (
                      <div className="flex items-center gap-1 text-green-600">
                        <CheckCircle2 className="h-4 w-4" />
                        <span className="text-xs">Verified</span>
                      </div>
                    ) : (
                      <div className="flex items-center gap-1 text-amber-600">
                        <AlertCircle className="h-4 w-4" />
                        <span className="text-xs">Not verified</span>
                      </div>
                    )}
                  </div>
                  {!user.isEmailVerified && (
                    <div className="text-xs text-muted-foreground space-y-1">
                      <p>Please check your email inbox (and spam folder) for the verification link.</p>
                      <button
                        onClick={handleResendVerification}
                        disabled={resendingVerification}
                        className="text-blue-600 hover:underline disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-1"
                      >
                        <Mail className="h-3 w-3" />
                        {resendingVerification ? 'Sending...' : 'Resend verification email'}
                      </button>
                    </div>
                  )}
                </div>
              </div>

              {/* Phone */}
              <div>
                <p className="text-sm text-muted-foreground">Phone</p>
                <p className="font-medium text-foreground">
                  {profile?.phoneNumber || (
                    <span className="text-sm text-muted-foreground italic">Not set</span>
                  )}
                </p>
              </div>

              {/* Location */}
              {profile?.location && (
                <div>
                  <p className="text-sm text-muted-foreground">Location</p>
                  <p className="font-medium text-foreground">
                    {profile.location.city}, {profile.location.state} {profile.location.zipCode}
                  </p>
                  <p className="text-sm text-muted-foreground">{profile.location.country}</p>
                </div>
              )}

              {/* Success message */}
              {isSuccess && (
                <div className="flex items-center gap-2 text-sm" style={{ color: '#006400' }}>
                  <Check className="h-4 w-4" />
                  <span>Profile updated successfully!</span>
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
              {/* First Name */}
              <div>
                <label htmlFor="firstName" className="block text-sm font-medium mb-1">
                  First Name *
                </label>
                <Input
                  id="firstName"
                  type="text"
                  value={formData.firstName}
                  onChange={(e) => handleInputChange('firstName', e.target.value)}
                  placeholder="e.g., John"
                  disabled={isLoading}
                  error={!!validationErrors.firstName}
                  aria-label="First Name"
                  aria-invalid={!!validationErrors.firstName}
                  aria-describedby={validationErrors.firstName ? 'firstName-error' : undefined}
                  maxLength={PROFILE_CONSTRAINTS.basicInfo.firstNameMaxLength}
                />
                {validationErrors.firstName && (
                  <p id="firstName-error" className="text-sm text-destructive mt-1" role="alert">
                    {validationErrors.firstName}
                  </p>
                )}
              </div>

              {/* Last Name */}
              <div>
                <label htmlFor="lastName" className="block text-sm font-medium mb-1">
                  Last Name *
                </label>
                <Input
                  id="lastName"
                  type="text"
                  value={formData.lastName}
                  onChange={(e) => handleInputChange('lastName', e.target.value)}
                  placeholder="e.g., Doe"
                  disabled={isLoading}
                  error={!!validationErrors.lastName}
                  aria-label="Last Name"
                  aria-invalid={!!validationErrors.lastName}
                  aria-describedby={validationErrors.lastName ? 'lastName-error' : undefined}
                  maxLength={PROFILE_CONSTRAINTS.basicInfo.lastNameMaxLength}
                />
                {validationErrors.lastName && (
                  <p id="lastName-error" className="text-sm text-destructive mt-1" role="alert">
                    {validationErrors.lastName}
                  </p>
                )}
              </div>

              {/* Email (now editable) */}
              <div>
                <label htmlFor="email" className="block text-sm font-medium mb-1">
                  Email *
                </label>
                <Input
                  id="email"
                  type="email"
                  value={formData.email}
                  onChange={(e) => handleInputChange('email', e.target.value)}
                  placeholder="e.g., john@example.com"
                  disabled={isLoading}
                  error={!!validationErrors.email}
                  aria-label="Email"
                  aria-invalid={!!validationErrors.email}
                  aria-describedby={validationErrors.email ? 'email-error' : undefined}
                />
                {validationErrors.email && (
                  <p id="email-error" className="text-sm text-destructive mt-1" role="alert">
                    {validationErrors.email}
                  </p>
                )}
                <p className="text-xs text-muted-foreground mt-1">
                  Changing your email will require verification. You may need to log out and log back in.
                </p>
              </div>

              {/* Phone Number */}
              <div>
                <label htmlFor="phoneNumber" className="block text-sm font-medium mb-1">
                  Phone Number
                </label>
                <Input
                  id="phoneNumber"
                  type="tel"
                  value={formData.phoneNumber}
                  onChange={(e) => handleInputChange('phoneNumber', e.target.value)}
                  placeholder="e.g., +94771234567 or 0771234567"
                  disabled={isLoading}
                  error={!!validationErrors.phoneNumber}
                  aria-label="Phone Number"
                  aria-invalid={!!validationErrors.phoneNumber}
                  aria-describedby={validationErrors.phoneNumber ? 'phoneNumber-error' : undefined}
                  maxLength={PROFILE_CONSTRAINTS.basicInfo.phoneNumberMaxLength}
                />
                {validationErrors.phoneNumber && (
                  <p id="phoneNumber-error" className="text-sm text-destructive mt-1" role="alert">
                    {validationErrors.phoneNumber}
                  </p>
                )}
                <p className="text-xs text-muted-foreground mt-1">
                  Format: +94771234567 (international) or 0771234567 (Sri Lankan)
                </p>
              </div>

              {/* Divider */}
              <div className="border-t pt-4">
                <h4 className="text-sm font-medium mb-3" style={{ color: '#8B1538' }}>
                  Location (Optional)
                </h4>
              </div>

              {/* City */}
              <div>
                <label htmlFor="city" className="block text-sm font-medium mb-1">
                  City
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
                  State/Province
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
                  Zip/Postal Code
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
                  Country
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
                  {isLoading ? 'Saving...' : 'Save Changes'}
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
