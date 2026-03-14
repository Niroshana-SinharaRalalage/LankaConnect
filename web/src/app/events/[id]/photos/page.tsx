'use client';

/**
 * Photo Album Public Gallery Page
 *
 * Public-facing page at /events/{id}/photos that displays event photo albums.
 * Supports multi-album with tab selector and ?album={albumId} query parameter.
 */

import { use, useMemo, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { ArrowLeft, Camera, Loader2, AlertCircle, Download } from 'lucide-react';
import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { Button } from '@/presentation/components/ui/Button';
import { Badge } from '@/presentation/components/ui/Badge';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useEventAlbums, useDownloadAlbumZip } from '@/presentation/hooks/usePhotoAlbum';
import { AlbumGallery } from '@/presentation/components/features/events/AlbumGallery';
import { AlbumStatus } from '@/infrastructure/api/types/events.types';

function getAlbumStatusBadge(status: AlbumStatus): { label: string; className: string } {
  switch (status) {
    case AlbumStatus.Draft:
      return { label: 'Draft', className: 'bg-yellow-100 text-yellow-800' };
    case AlbumStatus.Published:
      return { label: 'Published', className: 'bg-green-100 text-green-800' };
    default:
      return { label: status, className: 'bg-gray-100 text-gray-800' };
  }
}

export default function PhotosPage({ params }: { params: Promise<{ id: string }> }) {
  const { id: eventId } = use(params);
  const router = useRouter();
  const searchParams = useSearchParams();
  const { user, isAuthenticated } = useAuthStore();
  const downloadZip = useDownloadAlbumZip();

  const { data: albums, isLoading, isError } = useEventAlbums(eventId);

  // Filter to published albums for public view
  const publishedAlbums = useMemo(
    () => albums?.filter((a) => a.status === AlbumStatus.Published) ?? [],
    [albums],
  );

  // Determine active album from URL param or first published album
  const requestedAlbumId = searchParams.get('album');
  const [selectedAlbumId, setSelectedAlbumId] = useState<string | null>(null);

  const activeAlbum = useMemo(() => {
    if (publishedAlbums.length === 0) return null;
    // User's tab click takes priority over URL query param
    if (selectedAlbumId) {
      const found = publishedAlbums.find((a) => a.id === selectedAlbumId);
      if (found) return found;
    }
    if (requestedAlbumId) {
      const found = publishedAlbums.find((a) => a.id === requestedAlbumId);
      if (found) return found;
    }
    return publishedAlbums[0];
  }, [publishedAlbums, requestedAlbumId, selectedAlbumId]);

  const isOrganizer = useMemo(() => {
    if (!user || !activeAlbum) return false;
    return user.userId === activeAlbum.organizerId;
  }, [user, activeAlbum]);

  const canUpload = isOrganizer && isAuthenticated;

  const statusBadge = activeAlbum ? getAlbumStatusBadge(activeAlbum.status) : null;

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
                Loading photo albums...
              </p>
            </div>
          )}

          {/* Error State / No Albums */}
          {!isLoading && (isError || publishedAlbums.length === 0) && (
            <div className="flex flex-col items-center justify-center py-24">
              <AlertCircle className="w-12 h-12 text-gray-300 dark:text-gray-600 mb-4" />
              <p className="text-gray-500 dark:text-gray-400 text-sm">
                No photo albums are available for this event.
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
          {!isLoading && activeAlbum && (
            <div className="space-y-6">
              {/* Album Tabs (if multiple) */}
              {publishedAlbums.length > 1 && (
                <div className="flex items-center gap-2 overflow-x-auto pb-2 border-b">
                  {publishedAlbums.map((album) => (
                    <button
                      key={album.id}
                      type="button"
                      onClick={() => setSelectedAlbumId(album.id)}
                      className={`px-4 py-2 text-sm font-medium rounded-t-lg whitespace-nowrap transition-colors ${
                        activeAlbum.id === album.id
                          ? 'bg-white border border-b-0 border-gray-200 text-gray-900'
                          : 'text-gray-500 hover:text-gray-700 hover:bg-gray-50'
                      }`}
                    >
                      {album.name}
                      <span className="ml-2 text-xs text-gray-400">({album.photoCount})</span>
                    </button>
                  ))}
                </div>
              )}

              {/* Album Header */}
              <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
                <div>
                  <div className="flex items-center gap-3">
                    <Camera className="w-6 h-6 text-gray-500" />
                    <h1 className="text-2xl font-bold text-neutral-900 dark:text-neutral-100">
                      {activeAlbum.name}
                    </h1>
                    {statusBadge && (
                      <Badge className={statusBadge.className}>
                        {statusBadge.label}
                      </Badge>
                    )}
                  </div>
                  <p className="text-sm text-gray-500 dark:text-gray-400 mt-1 ml-9">
                    {activeAlbum.eventTitle}
                  </p>
                  {activeAlbum.description && (
                    <p className="text-sm text-gray-600 dark:text-gray-300 mt-2 ml-9">
                      {activeAlbum.description}
                    </p>
                  )}
                </div>

                <div className="flex items-center gap-3">
                  <span className="text-sm text-gray-500 dark:text-gray-400">
                    {activeAlbum.photoCount} {activeAlbum.photoCount === 1 ? 'photo' : 'photos'}
                  </span>
                  {activeAlbum.photoCount > 0 && (
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() =>
                        downloadZip.mutateAsync({
                          eventId,
                          albumId: activeAlbum.id,
                          albumName: activeAlbum.name,
                        })
                      }
                      disabled={downloadZip.isPending}
                    >
                      {downloadZip.isPending ? (
                        <Loader2 className="h-4 w-4 animate-spin" />
                      ) : (
                        <>
                          <Download className="h-4 w-4 mr-1" />
                          Download ZIP
                        </>
                      )}
                    </Button>
                  )}
                </div>
              </div>

              {/* Gallery */}
              <AlbumGallery
                eventId={eventId}
                album={activeAlbum}
                isOrganizer={isOrganizer}
                canUpload={canUpload}
              />
            </div>
          )}
        </div>
      </main>
      <Footer />
    </>
  );
}
