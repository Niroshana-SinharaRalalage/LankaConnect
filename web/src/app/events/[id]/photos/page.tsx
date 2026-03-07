'use client';

/**
 * Photo Album Public Gallery Page
 *
 * Public-facing page at /events/{id}/photos that displays the event's photo album.
 * Shows the gallery with upload capability for authorized users,
 * album status badges, and navigation back to the event detail page.
 */

import { use, useMemo } from 'react';
import { useRouter } from 'next/navigation';
import { ArrowLeft, Camera, Loader2, AlertCircle } from 'lucide-react';
import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { Button } from '@/presentation/components/ui/Button';
import { Badge } from '@/presentation/components/ui/Badge';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { usePhotoAlbum } from '@/presentation/hooks/usePhotoAlbum';
import { AlbumGallery } from '@/presentation/components/features/events/AlbumGallery';
import {
  AlbumStatus,
  AlbumUploadPermission,
} from '@/infrastructure/api/types/events.types';

/**
 * Returns display configuration for album status badges.
 */
function getAlbumStatusBadge(status: AlbumStatus): { label: string; className: string } {
  switch (status) {
    case AlbumStatus.Draft:
      return { label: 'Draft', className: 'bg-yellow-100 text-yellow-800' };
    case AlbumStatus.Published:
      return { label: 'Published', className: 'bg-green-100 text-green-800' };
    case AlbumStatus.Closed:
      return { label: 'Closed', className: 'bg-gray-100 text-gray-800' };
    default:
      return { label: status, className: 'bg-gray-100 text-gray-800' };
  }
}

export default function PhotosPage({ params }: { params: Promise<{ id: string }> }) {
  const { id: eventId } = use(params);
  const router = useRouter();
  const { user, isAuthenticated } = useAuthStore();

  const { data: album, isLoading, isError } = usePhotoAlbum(eventId);

  // Determine if the current user is the organizer
  const isOrganizer = useMemo(() => {
    if (!user || !album) return false;
    return user.userId === album.organizerId;
  }, [user, album]);

  // Determine if the current user can upload photos based on album settings
  const canUpload = useMemo(() => {
    if (!album || album.status === AlbumStatus.Closed) return false;
    if (!isAuthenticated) return false;

    switch (album.uploadPermission) {
      case AlbumUploadPermission.OrganizerOnly:
        return isOrganizer;
      case AlbumUploadPermission.RegisteredAttendees:
        // For now, allow authenticated users (registration check would need additional API)
        return isAuthenticated;
      case AlbumUploadPermission.AnyAuthenticated:
        return isAuthenticated;
      default:
        return false;
    }
  }, [album, isAuthenticated, isOrganizer]);

  const statusBadge = album ? getAlbumStatusBadge(album.status) : null;

  return (
    <>
      <Header />
      <main className="min-h-screen bg-gray-50 dark:bg-neutral-950">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          {/* Back Navigation */}
          <Button
            variant="ghost"
            size="sm"
            onClick={() => router.push(`/events/${eventId}`)}
            className="mb-6"
          >
            <ArrowLeft className="w-4 h-4 mr-2" />
            Back to Event
          </Button>

          {/* Loading State */}
          {isLoading && (
            <div className="flex flex-col items-center justify-center py-24">
              <Loader2 className="w-8 h-8 text-gray-400 animate-spin mb-4" />
              <p className="text-sm text-gray-500 dark:text-gray-400">
                Loading photo album...
              </p>
            </div>
          )}

          {/* Error State / No Album */}
          {!isLoading && (isError || !album) && (
            <div className="flex flex-col items-center justify-center py-24">
              <AlertCircle className="w-12 h-12 text-gray-300 dark:text-gray-600 mb-4" />
              <p className="text-gray-500 dark:text-gray-400 text-sm">
                This event does not have a photo album yet.
              </p>
              <Button
                variant="outline"
                size="sm"
                onClick={() => router.push(`/events/${eventId}`)}
                className="mt-4"
              >
                Return to Event
              </Button>
            </div>
          )}

          {/* Album Content */}
          {!isLoading && album && (
            <div className="space-y-6">
              {/* Album Header */}
              <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
                <div>
                  <div className="flex items-center gap-3">
                    <Camera className="w-6 h-6 text-gray-500" />
                    <h1 className="text-2xl font-bold text-neutral-900 dark:text-neutral-100">
                      Photo Album
                    </h1>
                    {statusBadge && (
                      <Badge className={statusBadge.className}>
                        {statusBadge.label}
                      </Badge>
                    )}
                  </div>
                  <p className="text-sm text-gray-500 dark:text-gray-400 mt-1 ml-9">
                    {album.eventTitle}
                  </p>
                  {album.description && (
                    <p className="text-sm text-gray-600 dark:text-gray-300 mt-2 ml-9">
                      {album.description}
                    </p>
                  )}
                </div>

                <div className="text-sm text-gray-500 dark:text-gray-400 sm:text-right">
                  <p>{album.photoCount} {album.photoCount === 1 ? 'photo' : 'photos'}</p>
                </div>
              </div>

              {/* Album is Draft - show message for non-organizers */}
              {album.status === AlbumStatus.Draft && !isOrganizer && (
                <div className="text-center py-12">
                  <p className="text-gray-500 dark:text-gray-400 text-sm">
                    This album is not yet published.
                  </p>
                </div>
              )}

              {/* Gallery (shown when published or organizer viewing draft) */}
              {(album.status !== AlbumStatus.Draft || isOrganizer) && (
                <AlbumGallery
                  eventId={eventId}
                  album={album}
                  isOrganizer={isOrganizer}
                  canUpload={canUpload}
                />
              )}
            </div>
          )}
        </div>
      </main>
      <Footer />
    </>
  );
}
