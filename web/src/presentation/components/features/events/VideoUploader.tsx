'use client';

/**
 * VideoUploader Component
 * Phase 6A.12: Event Media Upload System
 *
 * Professional video uploader for event galleries with thumbnail support
 * Integrates with Azure Blob Storage via useVideoUpload hook
 *
 * Features:
 * - Video and thumbnail file selection
 * - Video preview gallery
 * - Upload progress tracking
 * - File validation (size, type, formats)
 * - Delete functionality
 * - Responsive design
 * - Accessibility support
 * - Max 3 videos per event
 */

import React, { useCallback, useState } from 'react';
import { Upload, X, Loader2, AlertCircle, Video as VideoIcon, Play } from 'lucide-react';

import { useVideoUpload } from '@/presentation/hooks/useVideoUpload';
import { Button } from '@/presentation/components/ui/Button';
import { cn } from '@/presentation/lib/utils';
import type { EventVideoDto } from '@/infrastructure/api/types/events.types';

export interface VideoUploaderProps {
  /** Event ID for video uploads */
  eventId: string;

  /** Existing videos in the event gallery */
  existingVideos?: Array<{
    id: string;
    videoUrl: string;
    thumbnailUrl: string;
    displayOrder: number;
  }>;

  /** Maximum number of videos allowed */
  maxVideos?: number;

  /** Callback when videos change */
  onVideosChange?: (videoUrls: string[]) => void;

  /** Callback when upload completes */
  onUploadComplete?: () => void;

  /** Disable uploads */
  disabled?: boolean;

  /** Additional CSS classes */
  className?: string;
}

/**
 * VideoUploader Component
 *
 * @example
 * ```tsx
 * <VideoUploader
 *   eventId="event-123"
 *   existingVideos={event.videos}
 *   maxVideos={3}
 *   onUploadComplete={() => console.log('Upload complete!')}
 * />
 * ```
 */
export const VideoUploader: React.FC<VideoUploaderProps> = ({
  eventId,
  existingVideos = [],
  maxVideos = 3,
  onVideosChange,
  onUploadComplete,
  disabled = false,
  className,
}) => {
  const [selectedVideo, setSelectedVideo] = useState<File | null>(null);
  const [selectedThumbnail, setSelectedThumbnail] = useState<File | null>(null);
  const [videoPreviewUrl, setVideoPreviewUrl] = useState<string | null>(null);
  const [thumbnailPreviewUrl, setThumbnailPreviewUrl] = useState<string | null>(null);

  const {
    uploadVideo,
    deleteVideo,
    validateVideo,
    isUploading,
    uploadProgress,
    error,
    reset,
  } = useVideoUpload({
    onSuccess: (videoDto) => {
      onVideosChange?.([...existingVideos.map(v => v.videoUrl), videoDto.videoUrl]);
      onUploadComplete?.();
      handleClearSelection();
    },
    onError: (errorMsg) => {
      console.error('Video upload error:', errorMsg);
    },
  });

  // Check if max videos reached
  const videosRemaining = maxVideos - existingVideos.length;
  const canUploadMore = videosRemaining > 0 && !disabled;

  // Handle video file selection
  const handleVideoSelect = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    // Clear previous preview
    if (videoPreviewUrl) {
      URL.revokeObjectURL(videoPreviewUrl);
    }

    setSelectedVideo(file);
    setVideoPreviewUrl(URL.createObjectURL(file));
  }, [videoPreviewUrl]);

  // Handle thumbnail file selection
  const handleThumbnailSelect = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    // Clear previous preview
    if (thumbnailPreviewUrl) {
      URL.revokeObjectURL(thumbnailPreviewUrl);
    }

    setSelectedThumbnail(file);
    setThumbnailPreviewUrl(URL.createObjectURL(file));
  }, [thumbnailPreviewUrl]);

  // Handle upload
  const handleUpload = useCallback(async () => {
    if (!selectedVideo || !selectedThumbnail) return;

    // Validate files
    const validation = validateVideo(selectedVideo, selectedThumbnail);
    if (!validation.isValid) {
      alert(validation.errors.join('\n'));
      return;
    }

    try {
      await uploadVideo(selectedVideo, selectedThumbnail, eventId);
    } catch (err) {
      console.error('Upload failed:', err);
    }
  }, [selectedVideo, selectedThumbnail, validateVideo, uploadVideo, eventId]);

  // Handle delete
  const handleDelete = useCallback(async (videoId: string) => {
    if (!confirm('Are you sure you want to delete this video?')) return;

    try {
      await deleteVideo(eventId, videoId);
    } catch (err) {
      console.error('Delete failed:', err);
    }
  }, [deleteVideo, eventId]);

  // Clear selection
  const handleClearSelection = useCallback(() => {
    if (videoPreviewUrl) {
      URL.revokeObjectURL(videoPreviewUrl);
    }
    if (thumbnailPreviewUrl) {
      URL.revokeObjectURL(thumbnailPreviewUrl);
    }

    setSelectedVideo(null);
    setSelectedThumbnail(null);
    setVideoPreviewUrl(null);
    setThumbnailPreviewUrl(null);
    reset();
  }, [videoPreviewUrl, thumbnailPreviewUrl, reset]);

  return (
    <div className={cn('space-y-6', className)}>
      {/* Existing Videos Gallery */}
      {existingVideos.length > 0 && (
        <div className="space-y-3">
          <h4 className="text-sm font-semibold text-neutral-700 dark:text-neutral-300">
            Current Videos ({existingVideos.length}/{maxVideos})
          </h4>
          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-4">
            {existingVideos
              .sort((a, b) => a.displayOrder - b.displayOrder)
              .map((video) => (
                <div key={video.id} className="relative group">
                  <div className="relative aspect-video rounded-lg overflow-hidden bg-neutral-100 dark:bg-neutral-800">
                    <img
                      src={video.thumbnailUrl}
                      alt={`Video ${video.displayOrder}`}
                      className="w-full h-full object-cover"
                    />
                    <div className="absolute inset-0 bg-black/30 flex items-center justify-center">
                      <div className="bg-white/90 rounded-full p-3">
                        <Play className="h-6 w-6 text-neutral-900" fill="currentColor" />
                      </div>
                    </div>
                    <div className="absolute top-2 left-2 bg-black/70 text-white text-xs px-2 py-1 rounded">
                      {video.displayOrder}
                    </div>
                  </div>
                  <button
                    onClick={() => handleDelete(video.id)}
                    className="absolute top-2 right-2 p-1.5 bg-red-600 text-white rounded-full opacity-0 group-hover:opacity-100 transition-opacity hover:bg-red-700"
                    aria-label="Delete video"
                  >
                    <X className="h-4 w-4" />
                  </button>
                </div>
              ))}
          </div>
        </div>
      )}

      {/* Upload Section */}
      {canUploadMore && (
        <div className="space-y-4">
          <h4 className="text-sm font-semibold text-neutral-700 dark:text-neutral-300">
            Upload New Video
          </h4>

          {/* Video File Input */}
          <div className="space-y-2">
            <label htmlFor="video-input" className="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
              Video File (MP4, WebM, OGG, MOV - Max 100MB)
            </label>
            <input
              id="video-input"
              type="file"
              accept="video/mp4,video/webm,video/ogg,video/quicktime"
              onChange={handleVideoSelect}
              disabled={disabled || isUploading}
              className="block w-full text-sm text-neutral-600 dark:text-neutral-400 file:mr-4 file:py-2 file:px-4 file:rounded-lg file:border-0 file:text-sm file:font-semibold file:bg-orange-50 file:text-orange-700 hover:file:bg-orange-100 dark:file:bg-orange-900 dark:file:text-orange-200"
            />
          </div>

          {/* Video Preview */}
          {videoPreviewUrl && (
            <div className="relative aspect-video rounded-lg overflow-hidden bg-neutral-100 dark:bg-neutral-800">
              <video
                src={videoPreviewUrl}
                controls
                className="w-full h-full object-contain"
              />
            </div>
          )}

          {/* Thumbnail File Input */}
          <div className="space-y-2">
            <label htmlFor="thumbnail-input" className="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
              Thumbnail Image (JPG, PNG, WebP - Max 5MB)
            </label>
            <input
              id="thumbnail-input"
              type="file"
              accept="image/jpeg,image/png,image/webp"
              onChange={handleThumbnailSelect}
              disabled={disabled || isUploading}
              className="block w-full text-sm text-neutral-600 dark:text-neutral-400 file:mr-4 file:py-2 file:px-4 file:rounded-lg file:border-0 file:text-sm file:font-semibold file:bg-orange-50 file:text-orange-700 hover:file:bg-orange-100 dark:file:bg-orange-900 dark:file:text-orange-200"
            />
          </div>

          {/* Thumbnail Preview */}
          {thumbnailPreviewUrl && (
            <div className="relative w-full max-w-xs aspect-video rounded-lg overflow-hidden bg-neutral-100 dark:bg-neutral-800">
              <img
                src={thumbnailPreviewUrl}
                alt="Thumbnail preview"
                className="w-full h-full object-cover"
              />
            </div>
          )}

          {/* Upload Progress */}
          {isUploading && (
            <div className="space-y-2">
              <div className="flex items-center justify-between text-sm text-neutral-600 dark:text-neutral-400">
                <span>Uploading...</span>
                <span>{uploadProgress}%</span>
              </div>
              <div className="h-2 bg-neutral-200 dark:bg-neutral-700 rounded-full overflow-hidden">
                <div
                  className="h-full bg-orange-600 transition-all duration-300"
                  style={{ width: `${uploadProgress}%` }}
                />
              </div>
            </div>
          )}

          {/* Error Message */}
          {error && (
            <div className="p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg flex items-start gap-2">
              <AlertCircle className="h-5 w-5 text-red-600 dark:text-red-400 flex-shrink-0 mt-0.5" />
              <div className="flex-1">
                <p className="text-sm font-medium text-red-800 dark:text-red-200">Upload Error</p>
                <p className="text-sm text-red-600 dark:text-red-300 mt-1">{error}</p>
              </div>
              <button
                onClick={reset}
                className="text-red-600 dark:text-red-400 hover:text-red-800 dark:hover:text-red-200"
                aria-label="Dismiss error"
              >
                <X className="h-4 w-4" />
              </button>
            </div>
          )}

          {/* Action Buttons */}
          <div className="flex gap-3">
            <Button
              onClick={handleUpload}
              disabled={!selectedVideo || !selectedThumbnail || isUploading || disabled}
              className="flex-1"
              style={{ background: '#FF7900' }}
            >
              {isUploading ? (
                <>
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                  Uploading...
                </>
              ) : (
                <>
                  <Upload className="h-4 w-4 mr-2" />
                  Upload Video
                </>
              )}
            </Button>
            {(selectedVideo || selectedThumbnail) && !isUploading && (
              <Button
                onClick={handleClearSelection}
                variant="outline"
                disabled={isUploading}
              >
                Clear
              </Button>
            )}
          </div>
        </div>
      )}

      {/* Max Videos Reached */}
      {!canUploadMore && !disabled && (
        <div className="p-4 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg">
          <div className="flex items-center gap-2">
            <VideoIcon className="h-5 w-5 text-blue-600 dark:text-blue-400" />
            <p className="text-sm font-medium text-blue-800 dark:text-blue-200">
              Maximum videos reached ({maxVideos}/{maxVideos})
            </p>
          </div>
          <p className="text-sm text-blue-600 dark:text-blue-300 mt-1">
            Delete an existing video to upload a new one.
          </p>
        </div>
      )}

      {/* Empty State */}
      {existingVideos.length === 0 && !selectedVideo && canUploadMore && (
        <div className="text-center py-8 text-neutral-500 dark:text-neutral-400">
          <VideoIcon className="h-12 w-12 mx-auto mb-3 opacity-50" />
          <p className="text-sm">No videos yet. Upload your first video above.</p>
        </div>
      )}
    </div>
  );
};
