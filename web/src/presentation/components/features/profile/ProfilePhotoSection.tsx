'use client';

import React from 'react';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useProfileStore } from '@/presentation/store/useProfileStore';
import { PhotoUploadWidget } from '@/presentation/components/widgets/PhotoUploadWidget';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';

/**
 * ProfilePhotoSection Component
 *
 * Container component for profile photo upload/management
 * - Integrates with useAuthStore for user ID
 * - Integrates with useProfileStore for photo state and actions
 * - Uses PhotoUploadWidget for UI
 * - Handles upload/delete operations
 * - Shows section states (idle, saving, success, error)
 *
 * Follows UI/UX best practices with proper error handling and loading states
 */
export function ProfilePhotoSection() {
  const { user, isAuthenticated } = useAuthStore();
  const { profile, error, sectionStates, uploadPhoto, deletePhoto } = useProfileStore();

  // Don't render if not authenticated
  if (!isAuthenticated || !user) {
    return null;
  }

  const currentPhotoUrl = profile?.profilePhotoUrl || null;
  const isLoading = sectionStates.photo === 'saving';
  const photoError = sectionStates.photo === 'error' ? error : null;

  /**
   * Handle photo upload
   * Calls uploadPhoto action from store with user ID and file
   */
  const handleUpload = async (file: File) => {
    await uploadPhoto(user.userId, file);
  };

  /**
   * Handle photo deletion
   * Calls deletePhoto action from store with user ID
   */
  const handleDelete = async () => {
    await deletePhoto(user.userId);
  };

  return (
    <section role="region" aria-labelledby="profile-photo-heading">
      <Card>
        <CardHeader>
          <CardTitle id="profile-photo-heading">Profile Photo</CardTitle>
          <CardDescription>
            Upload a photo to personalize your profile and help others recognize you
          </CardDescription>
        </CardHeader>
        <CardContent>
          <PhotoUploadWidget
            currentPhotoUrl={currentPhotoUrl}
            onUpload={handleUpload}
            onDelete={handleDelete}
            isLoading={isLoading}
            error={photoError || undefined}
            maxSizeMB={5}
            acceptedFormats={['image/jpeg', 'image/jpg', 'image/png', 'image/webp']}
          />
        </CardContent>
      </Card>
    </section>
  );
}
