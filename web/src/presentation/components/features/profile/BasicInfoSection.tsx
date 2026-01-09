'use client';

import React, { useState } from 'react';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useProfileStore } from '@/presentation/store/useProfileStore';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { PROFILE_CONSTRAINTS } from '@/domain/constants/profile.constants';
import { Check, User, CheckCircle2, AlertCircle } from 'lucide-react';

/**
 * BasicInfoSection Component
 * Phase 6A.70: Profile Basic Info with Email/Phone Verification
 *
 * Manages user basic information (First Name, Last Name, Email, Phone, Bio)
 * - View mode: Display current info with verification status
 * - Edit mode: Form with validation for all fields
 * - Email changes trigger verification flow (Phase 6A.53)
 * - Phone changes validated but no SMS verification (future Phase 6A.71+)
 * - Integrates with useProfileStore for state and API calls
 * - Shows loading/success/error states
 *
 * Follows LocationSection pattern for consistency
 */
export function BasicInfoSection() {
  const { user, isAuthenticated } = useAuthStore();
  const { profile, error, sectionStates, updateBasicInfo } = useProfileStore();

  const [isEditing, setIsEditing] = useState(false);
  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    email: '',
    phoneNumber: '',
    bio: '',
  });
  const [validationErrors, setValidationErrors] = useState<Record<string, string>>({});

  // Don't render if not authenticated
  if (!isAuthenticated || !user) {
    return null;
  }

  const isLoading = sectionStates.basicInfo === 'saving';
  const isSuccess = sectionStates.basicInfo === 'success';
  const isError = sectionStates.basicInfo === 'error';

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
      bio: profile?.bio || '',
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
      // Basic email regex validation
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
      // Basic phone validation: E.164 format or Sri Lankan local format
      const phoneRegex = /^(\+?[1-9]\d{1,14}|0\d{9})$/;
      if (!phoneRegex.test(formData.phoneNumber.trim())) {
        errors.phoneNumber = 'Invalid phone format. Use +94771234567 or 0771234567';
      }
    }

    // Bio validation (optional)
    if (formData.bio.trim() && formData.bio.length > PROFILE_CONSTRAINTS.basicInfo.bioMaxLength) {
      errors.bio = `Bio cannot exceed ${PROFILE_CONSTRAINTS.basicInfo.bioMaxLength} characters`;
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
      await updateBasicInfo(user.userId, {
        firstName: formData.firstName.trim(),
        lastName: formData.lastName.trim(),
        phoneNumber: formData.phoneNumber.trim() || null,
        bio: formData.bio.trim() || null,
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
            Manage your personal information and contact details
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

              {/* Bio */}
              {profile?.bio && (
                <div>
                  <p className="text-sm text-muted-foreground">Bio</p>
                  <p className="text-sm text-foreground whitespace-pre-wrap">{profile.bio}</p>
                </div>
              )}

              {/* Success message */}
              {isSuccess && (
                <div className="flex items-center gap-2 text-sm" style={{ color: '#006400' }}>
                  <Check className="h-4 w-4" />
                  <span>Basic information saved successfully!</span>
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

              {/* Email (Read-only in Phase 6A.70 - separate update flow needed) */}
              <div>
                <label htmlFor="email" className="block text-sm font-medium mb-1">
                  Email *
                </label>
                <Input
                  id="email"
                  type="email"
                  value={formData.email}
                  disabled={true}
                  aria-label="Email"
                  className="bg-gray-50 cursor-not-allowed"
                />
                <p className="text-xs text-muted-foreground mt-1">
                  Email changes require verification. Contact support to change your email.
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

              {/* Bio */}
              <div>
                <label htmlFor="bio" className="block text-sm font-medium mb-1">
                  Bio
                </label>
                <textarea
                  id="bio"
                  value={formData.bio}
                  onChange={(e) => handleInputChange('bio', e.target.value)}
                  placeholder="Tell us about yourself..."
                  disabled={isLoading}
                  aria-label="Bio"
                  aria-invalid={!!validationErrors.bio}
                  aria-describedby={validationErrors.bio ? 'bio-error' : undefined}
                  maxLength={PROFILE_CONSTRAINTS.basicInfo.bioMaxLength}
                  rows={4}
                  className={`w-full rounded-md border px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-offset-2 ${
                    validationErrors.bio
                      ? 'border-destructive focus:ring-destructive'
                      : 'border-input focus:ring-ring'
                  } ${isLoading ? 'opacity-50 cursor-not-allowed' : ''}`}
                />
                <div className="flex justify-between items-center mt-1">
                  <div>
                    {validationErrors.bio && (
                      <p id="bio-error" className="text-sm text-destructive" role="alert">
                        {validationErrors.bio}
                      </p>
                    )}
                  </div>
                  <p className="text-xs text-muted-foreground">
                    {formData.bio.length}/{PROFILE_CONSTRAINTS.basicInfo.bioMaxLength}
                  </p>
                </div>
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
