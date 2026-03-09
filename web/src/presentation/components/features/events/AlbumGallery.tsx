'use client';

/**
 * AlbumGallery Component
 *
 * Main photo gallery for event albums with responsive grid layout,
 * infinite scroll pagination, lightbox viewing, skeleton loading states,
 * selection mode for bulk delete, and ConfirmDialog for delete confirmation.
 */

import { useState, useRef, useCallback, useMemo } from 'react';
import {
  ChevronLeft,
  ChevronRight,
  X,
  ImageIcon,
  Loader2,
  Info,
  CheckSquare,
  Trash2,
} from 'lucide-react';
import { Dialog, DialogContent } from '@/presentation/components/ui/Dialog';
import { ConfirmDialog } from '@/presentation/components/ui/ConfirmDialog';
import { Button } from '@/presentation/components/ui/Button';
import { useAlbumPhotos, useDeleteAlbumPhoto, useBulkDeleteAlbumPhotos } from '@/presentation/hooks/usePhotoAlbum';
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
  const deletePhotoMutation = useDeleteAlbumPhoto();
  const bulkDeleteMutation = useBulkDeleteAlbumPhotos();

  // Single delete confirmation state (replaces native confirm())
  const [deletePhotoId, setDeletePhotoId] = useState<string | null>(null);

  // Selection mode state for bulk delete
  const [isSelectionMode, setIsSelectionMode] = useState(false);
  const [selectedPhotoIds, setSelectedPhotoIds] = useState<Set<string>>(new Set());
  const [showBulkDeleteDialog, setShowBulkDeleteDialog] = useState(false);

  // Infinite scroll query — uses album.id for multi-album support
  const {
    data,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
    isLoading,
    isError,
  } = useAlbumPhotos(eventId, album.id);

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

  // Single photo delete — opens ConfirmDialog
  const handleDeletePhoto = useCallback((photoId: string) => {
    setDeletePhotoId(photoId);
  }, []);

  // Confirm single photo deletion
  const confirmDeletePhoto = useCallback(async () => {
    if (!deletePhotoId) return;
    await deletePhotoMutation.mutateAsync({ eventId, albumId: album.id, photoId: deletePhotoId });
    if (lightboxPhoto?.id === deletePhotoId) {
      setLightboxPhoto(null);
    }
    setDeletePhotoId(null);
  }, [deletePhotoId, eventId, album.id, lightboxPhoto, deletePhotoMutation]);

  // Selection mode handlers
  const toggleSelectionMode = useCallback(() => {
    setIsSelectionMode((prev) => {
      if (prev) setSelectedPhotoIds(new Set());
      return !prev;
    });
  }, []);

  const handleToggleSelect = useCallback((photoId: string) => {
    setSelectedPhotoIds((prev) => {
      const next = new Set(prev);
      if (next.has(photoId)) next.delete(photoId);
      else next.add(photoId);
      return next;
    });
  }, []);

  const handleSelectAll = useCallback(() => {
    if (selectedPhotoIds.size === allPhotos.length) {
      setSelectedPhotoIds(new Set());
    } else {
      setSelectedPhotoIds(new Set(allPhotos.map((p) => p.id)));
    }
  }, [allPhotos, selectedPhotoIds.size]);

  // Confirm bulk deletion
  const confirmBulkDelete = useCallback(async () => {
    if (selectedPhotoIds.size === 0) return;
    await bulkDeleteMutation.mutateAsync({
      eventId,
      albumId: album.id,
      photoIds: Array.from(selectedPhotoIds),
    });
    if (lightboxPhoto && selectedPhotoIds.has(lightboxPhoto.id)) {
      setLightboxPhoto(null);
    }
    setIsSelectionMode(false);
    setSelectedPhotoIds(new Set());
  }, [selectedPhotoIds, eventId, album.id, lightboxPhoto, bulkDeleteMutation]);

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
        <AlbumPhotoUploader eventId={eventId} albumId={album.id} />
      )}

      {/* Selection Toolbar (organizer only, when photos exist) */}
      {isOrganizer && !isLoading && !isError && allPhotos.length > 0 && (
        <div className="flex items-center justify-between bg-neutral-50 dark:bg-neutral-900 border rounded-lg px-4 py-2">
          <Button
            variant={isSelectionMode ? 'default' : 'outline'}
            size="sm"
            onClick={toggleSelectionMode}
          >
            <CheckSquare className="w-4 h-4 mr-1.5" />
            {isSelectionMode ? 'Cancel' : 'Select'}
          </Button>

          {isSelectionMode && (
            <div className="flex items-center gap-2">
              <Button variant="outline" size="sm" onClick={handleSelectAll}>
                {selectedPhotoIds.size === allPhotos.length ? 'Deselect All' : 'Select All'}
              </Button>
              <Button
                variant="destructive"
                size="sm"
                disabled={selectedPhotoIds.size === 0}
                onClick={() => setShowBulkDeleteDialog(true)}
              >
                <Trash2 className="w-4 h-4 mr-1.5" />
                Delete ({selectedPhotoIds.size})
              </Button>
            </div>
          )}
        </div>
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
              isSelectionMode={isSelectionMode}
              isSelected={selectedPhotoIds.has(photo.id)}
              onToggleSelect={handleToggleSelect}
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

      {/* Single Delete Confirmation Dialog */}
      <ConfirmDialog
        open={deletePhotoId !== null}
        onOpenChange={(open) => { if (!open) setDeletePhotoId(null); }}
        title="Delete Photo"
        description="Are you sure you want to delete this photo? This action cannot be undone."
        confirmLabel="Delete"
        variant="danger"
        onConfirm={confirmDeletePhoto}
        isLoading={deletePhotoMutation.isPending}
      />

      {/* Bulk Delete Confirmation Dialog */}
      <ConfirmDialog
        open={showBulkDeleteDialog}
        onOpenChange={setShowBulkDeleteDialog}
        title="Delete Selected Photos"
        description={`Are you sure you want to delete ${selectedPhotoIds.size} photo${selectedPhotoIds.size === 1 ? '' : 's'}? This action cannot be undone.`}
        confirmLabel={`Delete ${selectedPhotoIds.size} Photo${selectedPhotoIds.size === 1 ? '' : 's'}`}
        variant="danger"
        onConfirm={confirmBulkDelete}
        isLoading={bulkDeleteMutation.isPending}
      />
    </div>
  );
}

export default AlbumGallery;
