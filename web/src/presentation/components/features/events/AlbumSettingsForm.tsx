'use client';

/**
 * AlbumSettingsForm Component
 *
 * Organizer-facing settings form for configuring photo album properties.
 * Allows changing upload permissions, moderation mode, and album description.
 */

import { useState, useEffect } from 'react';
import { Save, CheckCircle } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Label } from '@/presentation/components/ui/Label';
import { Textarea } from '@/presentation/components/ui/Textarea';
import { useUpdateAlbumSettings } from '@/presentation/hooks/usePhotoAlbum';
import {
  AlbumUploadPermission,
  AlbumModerationMode,
  type PhotoAlbumDto,
  type UpdateAlbumSettingsRequest,
} from '@/infrastructure/api/types/events.types';

export interface AlbumSettingsFormProps {
  /** Event ID */
  eventId: string;
  /** Current album data */
  album: PhotoAlbumDto;
}

/** Human-readable labels for upload permission options */
const UPLOAD_PERMISSION_LABELS: Record<AlbumUploadPermission, string> = {
  [AlbumUploadPermission.OrganizerOnly]: 'Organizer Only',
  [AlbumUploadPermission.RegisteredAttendees]: 'Registered Attendees',
  [AlbumUploadPermission.AnyAuthenticated]: 'Any Authenticated User',
};

/** Human-readable labels for moderation mode options */
const MODERATION_MODE_LABELS: Record<AlbumModerationMode, string> = {
  [AlbumModerationMode.None]: 'None (all photos visible immediately)',
  [AlbumModerationMode.PostModeration]: 'Post-Moderation (visible, can be removed)',
  [AlbumModerationMode.PreApproval]: 'Pre-Approval (photos require approval)',
};

/** Maximum description length */
const MAX_DESCRIPTION_LENGTH = 2000;

export function AlbumSettingsForm({ eventId, album }: AlbumSettingsFormProps) {
  const [uploadPermission, setUploadPermission] = useState<AlbumUploadPermission>(
    album.uploadPermission
  );
  const [moderationMode, setModerationMode] = useState<AlbumModerationMode>(
    album.moderationMode
  );
  const [description, setDescription] = useState(album.description || '');
  const [showSuccess, setShowSuccess] = useState(false);

  const updateSettingsMutation = useUpdateAlbumSettings();

  // Reset form when album data changes (e.g., after save)
  useEffect(() => {
    setUploadPermission(album.uploadPermission);
    setModerationMode(album.moderationMode);
    setDescription(album.description || '');
  }, [album]);

  // Check if form has unsaved changes
  const hasChanges =
    uploadPermission !== album.uploadPermission ||
    moderationMode !== album.moderationMode ||
    description !== (album.description || '');

  const handleSave = async () => {
    const request: UpdateAlbumSettingsRequest = {};

    if (uploadPermission !== album.uploadPermission) {
      request.uploadPermission = uploadPermission;
    }
    if (moderationMode !== album.moderationMode) {
      request.moderationMode = moderationMode;
    }
    if (description !== (album.description || '')) {
      request.description = description;
    }

    try {
      await updateSettingsMutation.mutateAsync({ eventId, request });
      setShowSuccess(true);
      setTimeout(() => setShowSuccess(false), 3000);
    } catch (error) {
      console.error('Failed to update album settings:', error);
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-lg">Album Settings</CardTitle>
      </CardHeader>
      <CardContent className="space-y-6">
        {/* Upload Permission */}
        <div>
          <Label htmlFor="upload-permission">Who can upload photos</Label>
          <select
            id="upload-permission"
            value={uploadPermission}
            onChange={(e) => setUploadPermission(e.target.value as AlbumUploadPermission)}
            disabled={updateSettingsMutation.isPending}
            className="mt-1 w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm bg-white dark:bg-neutral-800 text-sm focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary"
          >
            {Object.entries(UPLOAD_PERMISSION_LABELS).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </select>
        </div>

        {/* Moderation Mode */}
        <div>
          <Label htmlFor="moderation-mode">Moderation mode</Label>
          <select
            id="moderation-mode"
            value={moderationMode}
            onChange={(e) => setModerationMode(e.target.value as AlbumModerationMode)}
            disabled={updateSettingsMutation.isPending}
            className="mt-1 w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm bg-white dark:bg-neutral-800 text-sm focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary"
          >
            {Object.entries(MODERATION_MODE_LABELS).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </select>
        </div>

        {/* Description */}
        <div>
          <Label htmlFor="album-description">Description</Label>
          <Textarea
            id="album-description"
            value={description}
            onChange={(e) => setDescription(e.target.value.slice(0, MAX_DESCRIPTION_LENGTH))}
            placeholder="Describe this photo album..."
            rows={4}
            maxLength={MAX_DESCRIPTION_LENGTH}
            disabled={updateSettingsMutation.isPending}
            className="mt-1"
          />
          <p className="text-xs text-gray-500 dark:text-gray-400 mt-1 text-right">
            {description.length}/{MAX_DESCRIPTION_LENGTH}
          </p>
        </div>

        {/* Error */}
        {updateSettingsMutation.isError && (
          <p className="text-sm text-red-600 dark:text-red-400">
            Failed to save settings. Please try again.
          </p>
        )}

        {/* Success Message */}
        {showSuccess && (
          <div className="flex items-center gap-2 text-sm text-green-600 dark:text-green-400">
            <CheckCircle className="w-4 h-4" />
            Settings saved successfully.
          </div>
        )}

        {/* Save Button */}
        <Button
          onClick={handleSave}
          loading={updateSettingsMutation.isPending}
          disabled={!hasChanges || updateSettingsMutation.isPending}
        >
          <Save className="w-4 h-4 mr-2" />
          Save Settings
        </Button>
      </CardContent>
    </Card>
  );
}

export default AlbumSettingsForm;
