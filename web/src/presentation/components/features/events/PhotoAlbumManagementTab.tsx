'use client';

/**
 * PhotoAlbumManagementTab Component
 *
 * Inline tab content for the Photo Album tab on the Event Manage page.
 * Simplified UI: stats card + gallery with upload area.
 * Album auto-publishes on first photo upload (no manual Publish button needed).
 *
 * States handled:
 * 1. Loading — spinner while album data loads
 * 2. No album — empty state with "Create Album" CTA
 * 3. Album exists — stats card, close action, gallery with upload
 */

import { useState } from 'react';
import {
  Camera,
  Image,
  Loader2,
  AlertCircle,
  CheckCircle2,
} from 'lucide-react';
import { Card, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Badge } from '@/presentation/components/ui/Badge';
import {
  usePhotoAlbum,
  useCreateAlbum,
  useCloseAlbum,
} from '@/presentation/hooks/usePhotoAlbum';
import { AlbumGallery } from './AlbumGallery';
import {
  AlbumStatus,
} from '@/infrastructure/api/types/events.types';

export interface PhotoAlbumManagementTabProps {
  eventId: string;
}

function getStatusBadge(status: AlbumStatus): { label: string; className: string } {
  switch (status) {
    case AlbumStatus.Draft:
      return { label: 'Draft', className: 'bg-yellow-100 text-yellow-800' };
    case AlbumStatus.Published:
      return { label: 'Published', className: 'bg-green-100 text-green-800' };
    case AlbumStatus.Closed:
      return { label: 'Closed', className: 'bg-gray-100 text-gray-800' };
    default:
      return { label: String(status), className: 'bg-gray-100 text-gray-800' };
  }
}

export function PhotoAlbumManagementTab({ eventId }: PhotoAlbumManagementTabProps) {
  const { data: album, isLoading, error } = usePhotoAlbum(eventId);
  const createAlbum = useCreateAlbum();
  const closeAlbum = useCloseAlbum();

  const [inlineMessage, setInlineMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  const showMessage = (type: 'success' | 'error', text: string) => {
    setInlineMessage({ type, text });
    setTimeout(() => setInlineMessage(null), 5000);
  };

  const handleCreateAlbum = async () => {
    try {
      await createAlbum.mutateAsync({ eventId });
      showMessage('success', 'Photo album created successfully!');
    } catch {
      showMessage('error', 'Failed to create album. Please try again.');
    }
  };

  const handleClose = async () => {
    try {
      await closeAlbum.mutateAsync({ eventId });
      showMessage('success', 'Album closed. No more uploads will be accepted.');
    } catch {
      showMessage('error', 'Failed to close album. Please try again.');
    }
  };

  // Loading state
  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center py-16">
        <Loader2 className="h-8 w-8 animate-spin text-gray-400 mb-3" />
        <p className="text-sm text-gray-500">Loading photo album...</p>
      </div>
    );
  }

  // Error state
  if (error && !album) {
    return (
      <div className="flex flex-col items-center justify-center py-16">
        <AlertCircle className="h-10 w-10 text-red-300 mb-3" />
        <p className="text-sm text-red-600">Failed to load album data. Please refresh the page.</p>
      </div>
    );
  }

  // Inline message banner
  const messageBanner = inlineMessage && (
    <div
      className={`flex items-center gap-2 p-3 rounded-lg mb-4 ${
        inlineMessage.type === 'success'
          ? 'bg-green-50 border border-green-200 text-green-700'
          : 'bg-red-50 border border-red-200 text-red-700'
      }`}
    >
      {inlineMessage.type === 'success' ? (
        <CheckCircle2 className="h-4 w-4 flex-shrink-0" />
      ) : (
        <AlertCircle className="h-4 w-4 flex-shrink-0" />
      )}
      <p className="text-sm">{inlineMessage.text}</p>
    </div>
  );

  // No album yet — show create CTA
  if (!album) {
    return (
      <div className="space-y-4">
        {messageBanner}
        <div className="text-center py-12">
          <Camera className="h-16 w-16 text-purple-300 mx-auto mb-4" />
          <h3 className="text-xl font-semibold text-gray-900 mb-2">No Photo Album Yet</h3>
          <p className="text-gray-600 mb-6 max-w-md mx-auto">
            Create a photo album to share photos from your event.
            Photos are automatically deleted after 7 days.
          </p>
          <Button
            onClick={handleCreateAlbum}
            disabled={createAlbum.isPending}
            className="text-white"
            style={{ background: '#8B1538' }}
          >
            {createAlbum.isPending ? (
              <>
                <Loader2 className="h-4 w-4 animate-spin mr-2" />
                Creating...
              </>
            ) : (
              'Create Photo Album'
            )}
          </Button>
        </div>
      </div>
    );
  }

  // Album exists — render stats card + gallery
  const statusBadge = getStatusBadge(album.status);

  return (
    <div className="space-y-6">
      {messageBanner}

      {/* Album Stats & Actions Card */}
      <Card>
        <CardContent className="pt-6">
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
            {/* Left: Status + Stats */}
            <div className="flex items-center gap-4 flex-wrap">
              <Badge className={`text-sm px-3 py-1 font-semibold ${statusBadge.className}`}>
                {statusBadge.label}
              </Badge>
              <div className="flex items-center gap-4 text-sm text-gray-600">
                <span className="flex items-center gap-1">
                  <Image className="h-4 w-4" />
                  {album.photoCount} {album.photoCount === 1 ? 'photo' : 'photos'}
                </span>
                <span>{album.retentionDays}-day retention</span>
              </div>
              {album.coverPhotoUrl && (
                <img
                  src={album.coverPhotoUrl}
                  alt="Cover"
                  className="h-10 w-10 object-cover rounded border hidden sm:block"
                />
              )}
            </div>

            {/* Right: Close Album action (Published only) */}
            <div className="flex items-center gap-2 flex-wrap">
              {album.status === AlbumStatus.Published && (
                <Button
                  onClick={handleClose}
                  disabled={closeAlbum.isPending}
                  variant="outline"
                  className="text-red-600 border-red-300 hover:bg-red-50"
                  size="sm"
                >
                  {closeAlbum.isPending ? (
                    <>
                      <Loader2 className="h-3 w-3 animate-spin mr-1" />
                      Closing...
                    </>
                  ) : (
                    'Close Album'
                  )}
                </Button>
              )}
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Gallery with upload area — shown directly (no sub-tabs) */}
      <AlbumGallery
        eventId={eventId}
        album={album}
        isOrganizer={true}
        canUpload={album.status !== AlbumStatus.Closed}
      />
    </div>
  );
}
