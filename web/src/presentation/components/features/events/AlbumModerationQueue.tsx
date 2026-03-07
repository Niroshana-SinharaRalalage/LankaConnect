'use client';

/**
 * AlbumModerationQueue Component
 *
 * Displays pending photos awaiting organizer approval/rejection.
 * Used in the album management page when moderation mode is PreApproval.
 */

import { useState } from 'react';
import { Check, X, Loader2, ShieldCheck, ImageIcon } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardContent } from '@/presentation/components/ui/Card';
import { Badge } from '@/presentation/components/ui/Badge';
import { Button } from '@/presentation/components/ui/Button';
import { usePendingPhotos, useApprovePhoto, useRejectPhoto } from '@/presentation/hooks/usePhotoAlbum';

export interface AlbumModerationQueueProps {
  /** Event ID */
  eventId: string;
}

export function AlbumModerationQueue({ eventId }: AlbumModerationQueueProps) {
  const { data: pendingPhotos, isLoading, isError } = usePendingPhotos(eventId);
  const approveMutation = useApprovePhoto();
  const rejectMutation = useRejectPhoto();

  // Track which photos are currently being processed
  const [processingIds, setProcessingIds] = useState<Set<string>>(new Set());

  const handleApprove = async (photoId: string) => {
    setProcessingIds((prev) => new Set(prev).add(photoId));
    try {
      await approveMutation.mutateAsync({ eventId, photoId });
    } catch (error) {
      console.error('Failed to approve photo:', error);
    } finally {
      setProcessingIds((prev) => {
        const next = new Set(prev);
        next.delete(photoId);
        return next;
      });
    }
  };

  const handleReject = async (photoId: string) => {
    setProcessingIds((prev) => new Set(prev).add(photoId));
    try {
      await rejectMutation.mutateAsync({ eventId, photoId });
    } catch (error) {
      console.error('Failed to reject photo:', error);
    } finally {
      setProcessingIds((prev) => {
        const next = new Set(prev);
        next.delete(photoId);
        return next;
      });
    }
  };

  const pendingCount = pendingPhotos?.length ?? 0;

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <ShieldCheck className="w-5 h-5 text-gray-500" />
            <CardTitle className="text-lg">Moderation Queue</CardTitle>
          </div>
          {pendingCount > 0 && (
            <Badge className="bg-orange-100 text-orange-800">
              {pendingCount} pending
            </Badge>
          )}
        </div>
      </CardHeader>
      <CardContent>
        {/* Loading State */}
        {isLoading && (
          <div className="flex justify-center py-8">
            <Loader2 className="w-6 h-6 text-gray-400 animate-spin" />
          </div>
        )}

        {/* Error State */}
        {isError && (
          <p className="text-sm text-red-600 dark:text-red-400 text-center py-4">
            Failed to load pending photos.
          </p>
        )}

        {/* Empty State */}
        {!isLoading && !isError && pendingCount === 0 && (
          <div className="text-center py-8">
            <ImageIcon className="w-12 h-12 mx-auto mb-3 text-gray-300 dark:text-gray-600" />
            <p className="text-sm text-gray-500 dark:text-gray-400">
              No photos pending approval
            </p>
          </div>
        )}

        {/* Pending Photos Grid */}
        {!isLoading && !isError && pendingPhotos && pendingPhotos.length > 0 && (
          <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
            {pendingPhotos.map((photo) => {
              const isProcessing = processingIds.has(photo.id);

              return (
                <div
                  key={photo.id}
                  className="relative rounded-lg overflow-hidden border border-gray-200 dark:border-gray-700 bg-neutral-100 dark:bg-neutral-800"
                >
                  {/* Thumbnail */}
                  <div className="aspect-square">
                    <img
                      src={photo.thumbnailUrl}
                      alt={photo.caption || `Photo by ${photo.uploaderName}`}
                      className="w-full h-full object-cover"
                      loading="lazy"
                    />
                  </div>

                  {/* Photo Info */}
                  <div className="p-2 space-y-1">
                    <p className="text-xs font-medium text-gray-700 dark:text-gray-300 truncate">
                      {photo.uploaderName}
                    </p>
                    {photo.caption && (
                      <p className="text-xs text-gray-500 dark:text-gray-400 line-clamp-1">
                        {photo.caption}
                      </p>
                    )}
                  </div>

                  {/* Action Buttons */}
                  <div className="flex border-t border-gray-200 dark:border-gray-700">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleApprove(photo.id)}
                      disabled={isProcessing}
                      className="flex-1 rounded-none text-green-600 hover:bg-green-50 dark:hover:bg-green-950/20 hover:text-green-700"
                      aria-label={`Approve photo by ${photo.uploaderName}`}
                    >
                      {isProcessing ? (
                        <Loader2 className="w-4 h-4 animate-spin" />
                      ) : (
                        <>
                          <Check className="w-4 h-4 mr-1" />
                          Approve
                        </>
                      )}
                    </Button>
                    <div className="w-px bg-gray-200 dark:bg-gray-700" />
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleReject(photo.id)}
                      disabled={isProcessing}
                      className="flex-1 rounded-none text-red-600 hover:bg-red-50 dark:hover:bg-red-950/20 hover:text-red-700"
                      aria-label={`Reject photo by ${photo.uploaderName}`}
                    >
                      {isProcessing ? (
                        <Loader2 className="w-4 h-4 animate-spin" />
                      ) : (
                        <>
                          <X className="w-4 h-4 mr-1" />
                          Reject
                        </>
                      )}
                    </Button>
                  </div>

                  {/* Processing Overlay */}
                  {isProcessing && (
                    <div className="absolute inset-0 bg-white/50 dark:bg-black/50 flex items-center justify-center">
                      <Loader2 className="w-6 h-6 text-gray-500 animate-spin" />
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        )}
      </CardContent>
    </Card>
  );
}

export default AlbumModerationQueue;
