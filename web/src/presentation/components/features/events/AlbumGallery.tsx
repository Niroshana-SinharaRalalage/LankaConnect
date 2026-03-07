'use client';

/**
 * AlbumGallery Component
 *
 * Main photo gallery for event albums with responsive grid layout,
 * infinite scroll pagination, lightbox viewing, and skeleton loading states.
 */

import { useState, useRef, useCallback, useMemo } from 'react';
import {
  ChevronLeft,
  ChevronRight,
  X,
  ImageIcon,
  Loader2,
  Info,
} from 'lucide-react';
import { Dialog, DialogContent } from '@/presentation/components/ui/Dialog';
import { useAlbumPhotos } from '@/presentation/hooks/usePhotoAlbum';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { AlbumPhotoCard } from '@/presentation/components/features/events/AlbumPhotoCard';
import { AlbumPhotoUploader } from '@/presentation/components/features/events/AlbumPhotoUploader';
import type { PhotoAlbumDto, AlbumPhotoDto } from '@/infrastructure/api/types/events.types';

export interface AlbumGalleryProps {
  /** Event ID */
  eventId: string;
  /** Album metadata */
  album: PhotoAlbumDto;
  /** Whether the current user is an event organizer */
  isOrganizer: boolean;
  /** Whether the current user can upload photos */
  canUpload: boolean;
}

/** Number of skeleton placeholders to show while loading */
const SKELETON_COUNT = 8;

export function AlbumGallery({ eventId, album, isOrganizer, canUpload }: AlbumGalleryProps) {
  const { user } = useAuthStore();
  const [lightboxPhoto, setLightboxPhoto] = useState<AlbumPhotoDto | null>(null);

  // Infinite scroll query
  const {
    data,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
    isLoading,
    isError,
  } = useAlbumPhotos(eventId);

  // Flatten all pages into a single photos array
  const allPhotos = useMemo(
    () => data?.pages.flatMap((page) => page.photos) ?? [],
    [data]
  );

  // Infinite scroll observer
  const observerRef = useRef<IntersectionObserver | null>(null);
  const loadMoreRef = useCallback(
    (node: HTMLDivElement | null) => {
      if (observerRef.current) observerRef.current.disconnect();
      observerRef.current = new IntersectionObserver((entries) => {
        if (entries[0].isIntersecting && hasNextPage && !isFetchingNextPage) {
          fetchNextPage();
        }
      });
      if (node) observerRef.current.observe(node);
    },
    [hasNextPage, isFetchingNextPage, fetchNextPage]
  );

  // Lightbox navigation
  const currentLightboxIndex = useMemo(() => {
    if (!lightboxPhoto) return -1;
    return allPhotos.findIndex((p) => p.id === lightboxPhoto.id);
  }, [lightboxPhoto, allPhotos]);

  const goToPrevious = useCallback(() => {
    if (currentLightboxIndex <= 0) return;
    setLightboxPhoto(allPhotos[currentLightboxIndex - 1]);
  }, [currentLightboxIndex, allPhotos]);

  const goToNext = useCallback(() => {
    if (currentLightboxIndex >= allPhotos.length - 1) return;
    setLightboxPhoto(allPhotos[currentLightboxIndex + 1]);
  }, [currentLightboxIndex, allPhotos]);

  const handleDeletePhoto = useCallback(
    (photoId: string) => {
      // If the deleted photo is currently in the lightbox, close it
      if (lightboxPhoto?.id === photoId) {
        setLightboxPhoto(null);
      }
      // Deletion is handled by the parent page via the useDeleteAlbumPhoto hook
      // This component just provides the UI trigger
    },
    [lightboxPhoto]
  );

  // Keyboard navigation for lightbox
  const handleLightboxKeyDown = useCallback(
    (e: React.KeyboardEvent) => {
      if (e.key === 'ArrowLeft') {
        e.preventDefault();
        goToPrevious();
      } else if (e.key === 'ArrowRight') {
        e.preventDefault();
        goToNext();
      }
    },
    [goToPrevious, goToNext]
  );

  return (
    <div className="space-y-6">
      {/* Retention Banner */}
      <div className="flex items-start gap-2 p-3 bg-blue-50 dark:bg-blue-950/20 border border-blue-200 dark:border-blue-800 rounded-lg">
        <Info className="w-4 h-4 text-blue-600 dark:text-blue-400 flex-shrink-0 mt-0.5" />
        <p className="text-sm text-blue-800 dark:text-blue-200">
          Photos are available for {album.retentionDays} days after upload.
        </p>
      </div>

      {/* Upload Section */}
      {canUpload && (
        <AlbumPhotoUploader eventId={eventId} />
      )}

      {/* Loading State */}
      {isLoading && (
        <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
          {Array.from({ length: SKELETON_COUNT }).map((_, i) => (
            <div
              key={`skeleton-${i}`}
              className="aspect-square rounded-lg bg-neutral-200 dark:bg-neutral-700 animate-pulse"
            />
          ))}
        </div>
      )}

      {/* Error State */}
      {isError && (
        <div className="text-center py-12">
          <p className="text-sm text-red-600 dark:text-red-400">
            Failed to load photos. Please try refreshing the page.
          </p>
        </div>
      )}

      {/* Photo Grid */}
      {!isLoading && !isError && allPhotos.length > 0 && (
        <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
          {allPhotos.map((photo) => (
            <AlbumPhotoCard
              key={photo.id}
              photo={photo}
              isOrganizer={isOrganizer}
              currentUserId={user?.userId}
              onView={setLightboxPhoto}
              onDelete={handleDeletePhoto}
            />
          ))}
        </div>
      )}

      {/* Empty State */}
      {!isLoading && !isError && allPhotos.length === 0 && (
        <div className="text-center py-16">
          <ImageIcon className="w-16 h-16 mx-auto mb-4 text-gray-300 dark:text-gray-600" />
          <p className="text-gray-500 dark:text-gray-400 text-sm">No photos yet</p>
          {canUpload && (
            <p className="text-gray-400 dark:text-gray-500 text-xs mt-1">
              Be the first to upload a photo!
            </p>
          )}
        </div>
      )}

      {/* Infinite Scroll Trigger */}
      {hasNextPage && (
        <div ref={loadMoreRef} className="flex justify-center py-4">
          {isFetchingNextPage && (
            <Loader2 className="w-6 h-6 text-gray-400 animate-spin" />
          )}
        </div>
      )}

      {/* Lightbox Dialog */}
      <Dialog
        open={lightboxPhoto !== null}
        onOpenChange={(open) => {
          if (!open) setLightboxPhoto(null);
        }}
      >
        <DialogContent
          className="max-w-7xl w-full h-[90vh] p-0 bg-black/95"
          onKeyDown={handleLightboxKeyDown}
        >
          <div className="relative w-full h-full flex items-center justify-center">
            {/* Close Button */}
            <button
              type="button"
              onClick={() => setLightboxPhoto(null)}
              className="absolute top-4 right-4 z-50 p-2 rounded-full bg-white/10 hover:bg-white/20 transition-colors"
              aria-label="Close lightbox"
            >
              <X className="h-6 w-6 text-white" />
            </button>

            {/* Navigation */}
            {allPhotos.length > 1 && (
              <>
                <button
                  type="button"
                  onClick={goToPrevious}
                  disabled={currentLightboxIndex <= 0}
                  className="absolute left-4 z-50 p-3 rounded-full bg-white/10 hover:bg-white/20 transition-colors disabled:opacity-30 disabled:cursor-not-allowed"
                  aria-label="Previous photo"
                >
                  <ChevronLeft className="h-6 w-6 text-white" />
                </button>
                <button
                  type="button"
                  onClick={goToNext}
                  disabled={currentLightboxIndex >= allPhotos.length - 1}
                  className="absolute right-4 z-50 p-3 rounded-full bg-white/10 hover:bg-white/20 transition-colors disabled:opacity-30 disabled:cursor-not-allowed"
                  aria-label="Next photo"
                >
                  <ChevronRight className="h-6 w-6 text-white" />
                </button>
              </>
            )}

            {/* Main Image */}
            {lightboxPhoto && (
              <div className="w-full h-full flex flex-col items-center justify-center p-12">
                <img
                  src={lightboxPhoto.originalUrl}
                  alt={lightboxPhoto.caption || `Photo by ${lightboxPhoto.uploaderName}`}
                  className="max-w-full max-h-[calc(90vh-10rem)] object-contain"
                />

                {/* Photo Info */}
                <div className="mt-4 text-center">
                  <p className="text-white text-sm font-medium">
                    {lightboxPhoto.uploaderName}
                  </p>
                  {lightboxPhoto.caption && (
                    <p className="text-white/70 text-sm mt-1 max-w-lg">
                      {lightboxPhoto.caption}
                    </p>
                  )}
                </div>
              </div>
            )}

            {/* Counter */}
            <div className="absolute bottom-4 left-1/2 -translate-x-1/2 bg-white/10 text-white text-sm px-4 py-2 rounded-full">
              {currentLightboxIndex + 1} / {allPhotos.length}
            </div>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}

export default AlbumGallery;
