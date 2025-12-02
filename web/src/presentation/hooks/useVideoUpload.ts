/**
 * Video Upload React Query Hook
 * Phase 6A.12: Event Media Upload System
 *
 * Provides React Query hooks for video upload/delete operations
 * Integrates with Azure Blob Storage via events repository
 *
 * Features:
 * - Video and thumbnail file validation (size, type, formats)
 * - Optimistic UI updates
 * - Progress tracking
 * - Error handling with rollback
 * - Automatic cache invalidation
 *
 * @requires @tanstack/react-query
 * @requires eventsRepository from infrastructure/api/repositories/events.repository
 */

import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useState, useCallback } from 'react';

import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import { eventKeys } from './useEvents';
import type { EventVideoDto } from '@/infrastructure/api/types/events.types';

/**
 * Video upload validation constraints
 * Matches backend validation rules
 */
const VALIDATION_CONSTRAINTS = {
  MAX_VIDEO_SIZE: 100 * 1024 * 1024, // 100 MB
  MAX_THUMBNAIL_SIZE: 5 * 1024 * 1024, // 5 MB
  ALLOWED_VIDEO_MIME_TYPES: ['video/mp4', 'video/webm', 'video/ogg', 'video/quicktime'],
  ALLOWED_VIDEO_EXTENSIONS: ['.mp4', '.webm', '.ogg', '.mov'],
  ALLOWED_THUMBNAIL_TYPES: ['image/jpeg', 'image/png', 'image/webp'],
  ALLOWED_THUMBNAIL_EXTENSIONS: ['.jpg', '.jpeg', '.png', '.webp'],
  MAX_VIDEOS_PER_EVENT: 3,
};

/**
 * Video file validation result
 */
interface ValidationResult {
  isValid: boolean;
  errors: string[];
}

/**
 * Video upload options
 */
interface UseVideoUploadOptions {
  onSuccess?: (videoDto: EventVideoDto) => void;
  onError?: (error: string) => void;
}

/**
 * Validates video file before upload
 * Checks file size, MIME type, and extension
 *
 * @param file - Video file to validate
 * @returns Validation result with errors if any
 */
function validateVideoFile(file: File): ValidationResult {
  const errors: string[] = [];

  // Check file size
  if (file.size > VALIDATION_CONSTRAINTS.MAX_VIDEO_SIZE) {
    const sizeMB = VALIDATION_CONSTRAINTS.MAX_VIDEO_SIZE / (1024 * 1024);
    errors.push(`Video file size exceeds maximum allowed size of ${sizeMB} MB`);
  }

  // Check MIME type
  if (!VALIDATION_CONSTRAINTS.ALLOWED_VIDEO_MIME_TYPES.includes(file.type)) {
    errors.push(
      `Invalid video file type. Allowed types: ${VALIDATION_CONSTRAINTS.ALLOWED_VIDEO_MIME_TYPES.join(', ')}`
    );
  }

  // Check file extension
  const extension = file.name.substring(file.name.lastIndexOf('.')).toLowerCase();
  if (!VALIDATION_CONSTRAINTS.ALLOWED_VIDEO_EXTENSIONS.includes(extension)) {
    errors.push(
      `Invalid video file extension. Allowed extensions: ${VALIDATION_CONSTRAINTS.ALLOWED_VIDEO_EXTENSIONS.join(', ')}`
    );
  }

  return {
    isValid: errors.length === 0,
    errors,
  };
}

/**
 * Validates thumbnail image file
 *
 * @param file - Thumbnail file to validate
 * @returns Validation result with errors if any
 */
function validateThumbnailFile(file: File): ValidationResult {
  const errors: string[] = [];

  // Check file size
  if (file.size > VALIDATION_CONSTRAINTS.MAX_THUMBNAIL_SIZE) {
    const sizeMB = VALIDATION_CONSTRAINTS.MAX_THUMBNAIL_SIZE / (1024 * 1024);
    errors.push(`Thumbnail file size exceeds maximum allowed size of ${sizeMB} MB`);
  }

  // Check MIME type
  if (!VALIDATION_CONSTRAINTS.ALLOWED_THUMBNAIL_TYPES.includes(file.type)) {
    errors.push(
      `Invalid thumbnail file type. Allowed types: ${VALIDATION_CONSTRAINTS.ALLOWED_THUMBNAIL_TYPES.join(', ')}`
    );
  }

  // Check file extension
  const extension = file.name.substring(file.name.lastIndexOf('.')).toLowerCase();
  if (!VALIDATION_CONSTRAINTS.ALLOWED_THUMBNAIL_EXTENSIONS.includes(extension)) {
    errors.push(
      `Invalid thumbnail file extension. Allowed extensions: ${VALIDATION_CONSTRAINTS.ALLOWED_THUMBNAIL_EXTENSIONS.join(', ')}`
    );
  }

  return {
    isValid: errors.length === 0,
    errors,
  };
}

/**
 * Validates both video and thumbnail files
 *
 * @param videoFile - Video file to validate
 * @param thumbnailFile - Thumbnail file to validate
 * @returns Combined validation result
 */
function validateVideoUpload(videoFile: File, thumbnailFile: File): ValidationResult {
  const allErrors: string[] = [];

  // Validate video
  const videoValidation = validateVideoFile(videoFile);
  if (!videoValidation.isValid) {
    allErrors.push(...videoValidation.errors);
  }

  // Validate thumbnail
  const thumbnailValidation = validateThumbnailFile(thumbnailFile);
  if (!thumbnailValidation.isValid) {
    allErrors.push(...thumbnailValidation.errors);
  }

  return {
    isValid: allErrors.length === 0,
    errors: allErrors,
  };
}

/**
 * useUploadEventVideo Hook
 *
 * Mutation hook for uploading a video (with thumbnail) to an event
 *
 * Features:
 * - File validation before upload
 * - Optimistic video addition to gallery
 * - Automatic cache invalidation
 * - Error rollback
 *
 * @param options - Success/error callbacks
 *
 * @example
 * ```tsx
 * const uploadVideo = useUploadEventVideo({
 *   onSuccess: (videoDto) => console.log('Uploaded:', videoDto.videoUrl),
 *   onError: (error) => console.error('Upload failed:', error)
 * });
 *
 * await uploadVideo.mutateAsync({
 *   eventId: '123',
 *   videoFile,
 *   thumbnailFile
 * });
 * ```
 */
export function useUploadEventVideo(options?: UseVideoUploadOptions) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      eventId,
      videoFile,
      thumbnailFile,
    }: {
      eventId: string;
      videoFile: File;
      thumbnailFile: File;
    }) => {
      // Validate before upload
      const validation = validateVideoUpload(videoFile, thumbnailFile);
      if (!validation.isValid) {
        throw new Error(validation.errors.join('; '));
      }

      // Upload to backend (Azure Blob Storage via API)
      const videoDto = await eventsRepository.uploadEventVideo(eventId, videoFile, thumbnailFile);
      return videoDto;
    },
    onMutate: async ({ eventId, videoFile, thumbnailFile }) => {
      // Cancel outgoing queries
      await queryClient.cancelQueries({ queryKey: eventKeys.detail(eventId) });

      // Snapshot for rollback
      const previousEvent = queryClient.getQueryData(eventKeys.detail(eventId));

      // Create preview URLs
      const videoPreviewUrl = URL.createObjectURL(videoFile);
      const thumbnailPreviewUrl = URL.createObjectURL(thumbnailFile);

      // Optimistically add video with temporary preview URLs
      queryClient.setQueryData(eventKeys.detail(eventId), (old: any) => {
        if (!old) return old;

        const tempVideo: EventVideoDto = {
          id: 'temp-' + Date.now(),
          videoUrl: videoPreviewUrl,
          thumbnailUrl: thumbnailPreviewUrl,
          duration: null,
          format: videoFile.type,
          fileSizeBytes: videoFile.size,
          displayOrder: (old.videos?.length || 0) + 1,
          uploadedAt: new Date().toISOString(),
        };

        return {
          ...old,
          videos: [...(old.videos || []), tempVideo],
        };
      });

      return { previousEvent, videoPreviewUrl, thumbnailPreviewUrl };
    },
    onError: (error, { eventId }, context) => {
      // Rollback on error
      if (context?.previousEvent) {
        queryClient.setQueryData(eventKeys.detail(eventId), context.previousEvent);
      }

      // Revoke preview URLs
      if (context?.videoPreviewUrl) {
        URL.revokeObjectURL(context.videoPreviewUrl);
      }
      if (context?.thumbnailPreviewUrl) {
        URL.revokeObjectURL(context.thumbnailPreviewUrl);
      }

      // Call error callback
      if (options?.onError) {
        options.onError(error instanceof Error ? error.message : 'Upload failed');
      }
    },
    onSuccess: (videoDto, { eventId }, context) => {
      // Revoke preview URLs (replaced with real URLs from backend)
      if (context?.videoPreviewUrl) {
        URL.revokeObjectURL(context.videoPreviewUrl);
      }
      if (context?.thumbnailPreviewUrl) {
        URL.revokeObjectURL(context.thumbnailPreviewUrl);
      }

      // Invalidate event detail to refetch with real video data
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(eventId) });

      // Call success callback
      if (options?.onSuccess) {
        options.onSuccess(videoDto);
      }
    },
  });
}

/**
 * useDeleteEventVideo Hook
 *
 * Mutation hook for deleting a video from event gallery
 *
 * Features:
 * - Optimistic removal from gallery
 * - Automatic cache invalidation
 * - Error rollback
 *
 * @example
 * ```tsx
 * const deleteVideo = useDeleteEventVideo();
 *
 * await deleteVideo.mutateAsync({
 *   eventId: '123',
 *   videoId: 'vid-456'
 * });
 * ```
 */
export function useDeleteEventVideo() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ eventId, videoId }: { eventId: string; videoId: string }) =>
      eventsRepository.deleteEventVideo(eventId, videoId),
    onMutate: async ({ eventId, videoId }) => {
      // Cancel queries
      await queryClient.cancelQueries({ queryKey: eventKeys.detail(eventId) });

      // Snapshot for rollback
      const previousEvent = queryClient.getQueryData(eventKeys.detail(eventId));

      // Optimistically remove video from gallery
      queryClient.setQueryData(eventKeys.detail(eventId), (old: any) => {
        if (!old) return old;

        return {
          ...old,
          videos: (old.videos || []).filter((vid: EventVideoDto) => vid.id !== videoId),
        };
      });

      return { previousEvent };
    },
    onError: (error, { eventId }, context) => {
      // Rollback on error
      if (context?.previousEvent) {
        queryClient.setQueryData(eventKeys.detail(eventId), context.previousEvent);
      }
    },
    onSuccess: (_data, { eventId }) => {
      // Invalidate event detail to ensure consistency
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(eventId) });
    },
  });
}

/**
 * useVideoUpload Hook (Convenience wrapper)
 *
 * Combines upload and delete mutations with validation utilities
 * Provides state management for upload progress and errors
 *
 * @param options - Configuration options
 *
 * @example
 * ```tsx
 * const {
 *   uploadVideo,
 *   deleteVideo,
 *   validateVideo,
 *   isUploading,
 *   error
 * } = useVideoUpload({
 *   onSuccess: (videoDto) => console.log('Video uploaded:', videoDto.videoUrl)
 * });
 *
 * // Validate before showing file picker
 * const validation = validateVideo(videoFile, thumbnailFile);
 * if (!validation.isValid) {
 *   alert(validation.errors.join('\n'));
 *   return;
 * }
 *
 * // Upload
 * await uploadVideo(videoFile, thumbnailFile, eventId);
 * ```
 */
export function useVideoUpload(options?: UseVideoUploadOptions) {
  const [error, setError] = useState<string | null>(null);
  const [uploadProgress, setUploadProgress] = useState<number>(0);

  const uploadMutation = useUploadEventVideo({
    ...options,
    onError: (errorMsg) => {
      setError(errorMsg);
      options?.onError?.(errorMsg);
    },
    onSuccess: (videoDto) => {
      setError(null);
      setUploadProgress(100);
      options?.onSuccess?.(videoDto);
    },
  });

  const deleteMutation = useDeleteEventVideo();

  const uploadVideo = useCallback(
    async (videoFile: File, thumbnailFile: File, eventId: string) => {
      setError(null);
      setUploadProgress(0);

      try {
        // Simulate progress (actual progress tracking would require backend support)
        const progressInterval = setInterval(() => {
          setUploadProgress((prev) => Math.min(prev + 5, 90));
        }, 500);

        await uploadMutation.mutateAsync({ eventId, videoFile, thumbnailFile });

        clearInterval(progressInterval);
        setUploadProgress(100);
      } catch (err) {
        setUploadProgress(0);
        throw err;
      }
    },
    [uploadMutation]
  );

  const deleteVideo = useCallback(
    async (eventId: string, videoId: string) => {
      setError(null);
      await deleteMutation.mutateAsync({ eventId, videoId });
    },
    [deleteMutation]
  );

  const reset = useCallback(() => {
    setError(null);
    setUploadProgress(0);
  }, []);

  return {
    uploadVideo,
    deleteVideo,
    validateVideo: validateVideoUpload,
    validateVideoFile,
    validateThumbnailFile,
    isUploading: uploadMutation.isPending || deleteMutation.isPending,
    uploadProgress,
    error,
    reset,
  };
}

/**
 * Export all hooks and utilities
 */
export default {
  useUploadEventVideo,
  useDeleteEventVideo,
  useVideoUpload,
  validateVideoFile,
  validateThumbnailFile,
  validateVideoUpload,
  VALIDATION_CONSTRAINTS,
};
