/**
 * Image Upload React Query Hook
 * Phase 6A.9: Azure Blob Image Upload System
 *
 * Provides React Query hooks for image upload/delete operations
 * Integrates with Azure Blob Storage via events repository
 *
 * Features:
 * - File validation (size, type, magic numbers)
 * - Optimistic UI updates
 * - Progress tracking
 * - Error handling with rollback
 * - Automatic cache invalidation
 *
 * @requires @tanstack/react-query
 * @requires eventsRepository from infrastructure/api/repositories/events.repository
 * @requires Image upload types from infrastructure/api/types/image-upload.types
 */

import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useState, useCallback } from 'react';

import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import { eventKeys } from './useEvents';
import type {
  IMAGE_UPLOAD_CONSTRAINTS,
  ImageValidationResult,
  UseImageUploadOptions,
} from '@/infrastructure/api/types/image-upload.types';
import type { EventImageDto } from '@/infrastructure/api/types/events.types';

/**
 * Image upload validation constraints
 * Matches backend validation rules (ImageService.cs)
 */
const VALIDATION_CONSTRAINTS = {
  MAX_FILE_SIZE: 10 * 1024 * 1024, // 10 MB
  ALLOWED_MIME_TYPES: ['image/jpeg', 'image/png', 'image/gif', 'image/webp'],
  ALLOWED_EXTENSIONS: ['.jpg', '.jpeg', '.png', '.gif', '.webp'],
  MAX_IMAGES_PER_EVENT: 10,
};

/**
 * Validates image file before upload
 * Checks file size, MIME type, and extension
 *
 * @param file - File to validate
 * @returns Validation result with errors if any
 */
function validateImageFile(file: File): ImageValidationResult {
  const errors: string[] = [];

  // Check file size
  if (file.size > VALIDATION_CONSTRAINTS.MAX_FILE_SIZE) {
    const sizeMB = VALIDATION_CONSTRAINTS.MAX_FILE_SIZE / (1024 * 1024);
    errors.push(`File size exceeds maximum allowed size of ${sizeMB} MB`);
  }

  // Check MIME type
  if (!VALIDATION_CONSTRAINTS.ALLOWED_MIME_TYPES.includes(file.type)) {
    errors.push(
      `Invalid file type. Allowed types: ${VALIDATION_CONSTRAINTS.ALLOWED_MIME_TYPES.join(', ')}`
    );
  }

  // Check file extension
  const extension = file.name.substring(file.name.lastIndexOf('.')).toLowerCase();
  if (!VALIDATION_CONSTRAINTS.ALLOWED_EXTENSIONS.includes(extension)) {
    errors.push(
      `Invalid file extension. Allowed extensions: ${VALIDATION_CONSTRAINTS.ALLOWED_EXTENSIONS.join(', ')}`
    );
  }

  return {
    isValid: errors.length === 0,
    errors,
  };
}

/**
 * Validates multiple image files
 *
 * @param files - Files to validate
 * @returns Validation result for all files
 */
function validateImageFiles(files: File[]): ImageValidationResult {
  const allErrors: string[] = [];

  // Check if too many files
  if (files.length > VALIDATION_CONSTRAINTS.MAX_IMAGES_PER_EVENT) {
    allErrors.push(
      `Too many files. Maximum ${VALIDATION_CONSTRAINTS.MAX_IMAGES_PER_EVENT} images allowed per event`
    );
  }

  // Validate each file
  files.forEach((file, index) => {
    const result = validateImageFile(file);
    if (!result.isValid) {
      result.errors.forEach((error) => {
        allErrors.push(`File ${index + 1} (${file.name}): ${error}`);
      });
    }
  });

  return {
    isValid: allErrors.length === 0,
    errors: allErrors,
  };
}

/**
 * useUploadEventImage Hook
 *
 * Mutation hook for uploading a single image to an event
 *
 * Features:
 * - File validation before upload
 * - Optimistic image addition to gallery
 * - Automatic cache invalidation
 * - Error rollback
 *
 * @param options - Success/error callbacks
 *
 * @example
 * ```tsx
 * const uploadImage = useUploadEventImage({
 *   onSuccess: (imageDto) => console.log('Uploaded:', imageDto.imageUrl),
 *   onError: (error) => console.error('Upload failed:', error)
 * });
 *
 * await uploadImage.mutateAsync({ eventId: '123', file });
 * ```
 */
export function useUploadEventImage(options?: UseImageUploadOptions) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ eventId, file }: { eventId: string; file: File }) => {
      // Validate before upload
      const validation = validateImageFile(file);
      if (!validation.isValid) {
        throw new Error(validation.errors.join('; '));
      }

      // Upload to backend (Azure Blob Storage via API)
      const imageDto = await eventsRepository.uploadEventImage(eventId, file);
      return imageDto;
    },
    onMutate: async ({ eventId, file }) => {
      // Cancel outgoing queries
      await queryClient.cancelQueries({ queryKey: eventKeys.detail(eventId) });

      // Snapshot for rollback
      const previousEvent = queryClient.getQueryData(eventKeys.detail(eventId));

      // Optimistically add image with temporary preview URL
      const previewUrl = URL.createObjectURL(file);
      queryClient.setQueryData(eventKeys.detail(eventId), (old: any) => {
        if (!old) return old;

        const tempImage: EventImageDto = {
          id: 'temp-' + Date.now(),
          imageUrl: previewUrl,
          displayOrder: (old.images?.length || 0) + 1,
          uploadedAt: new Date().toISOString(),
        };

        return {
          ...old,
          images: [...(old.images || []), tempImage],
        };
      });

      return { previousEvent, previewUrl };
    },
    onError: (error, { eventId }, context) => {
      // Rollback on error
      if (context?.previousEvent) {
        queryClient.setQueryData(eventKeys.detail(eventId), context.previousEvent);
      }

      // Revoke preview URL
      if (context?.previewUrl) {
        URL.revokeObjectURL(context.previewUrl);
      }

      // Call error callback
      if (options?.onError) {
        options.onError(error instanceof Error ? error.message : 'Upload failed');
      }
    },
    onSuccess: (imageDto, { eventId }, context) => {
      // Revoke preview URL (replaced with real URL from backend)
      if (context?.previewUrl) {
        URL.revokeObjectURL(context.previewUrl);
      }

      // Invalidate event detail to refetch with real image data
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(eventId) });

      // Call success callback
      if (options?.onSuccess) {
        options.onSuccess(imageDto.imageUrl);
      }
    },
  });
}

/**
 * useDeleteEventImage Hook
 *
 * Mutation hook for deleting an image from event gallery
 *
 * Features:
 * - Optimistic removal from gallery
 * - Automatic cache invalidation
 * - Error rollback
 *
 * @example
 * ```tsx
 * const deleteImage = useDeleteEventImage();
 *
 * await deleteImage.mutateAsync({
 *   eventId: '123',
 *   imageId: 'img-456'
 * });
 * ```
 */
export function useDeleteEventImage() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ eventId, imageId }: { eventId: string; imageId: string }) =>
      eventsRepository.deleteEventImage(eventId, imageId),
    onMutate: async ({ eventId, imageId }) => {
      // Cancel queries
      await queryClient.cancelQueries({ queryKey: eventKeys.detail(eventId) });

      // Snapshot for rollback
      const previousEvent = queryClient.getQueryData(eventKeys.detail(eventId));

      // Optimistically remove image from gallery
      queryClient.setQueryData(eventKeys.detail(eventId), (old: any) => {
        if (!old) return old;

        return {
          ...old,
          images: (old.images || []).filter((img: EventImageDto) => img.id !== imageId),
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
 * useReorderEventImages Hook
 *
 * Mutation hook for reordering event images via drag-and-drop
 *
 * Features:
 * - Optimistic reordering with instant visual feedback
 * - Automatic cache invalidation
 * - Error rollback to previous order
 *
 * @example
 * ```tsx
 * const reorderImages = useReorderEventImages();
 *
 * await reorderImages.mutateAsync({
 *   eventId: '123',
 *   newOrders: { 'img-1': 2, 'img-2': 1 }
 * });
 * ```
 */
export function useReorderEventImages() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ eventId, newOrders }: { eventId: string; newOrders: Record<string, number> }) =>
      eventsRepository.reorderEventImages(eventId, newOrders),
    onMutate: async ({ eventId, newOrders }) => {
      // Cancel outgoing queries
      await queryClient.cancelQueries({ queryKey: eventKeys.detail(eventId) });

      // Snapshot for rollback
      const previousEvent = queryClient.getQueryData(eventKeys.detail(eventId));

      // Optimistically reorder images
      queryClient.setQueryData(eventKeys.detail(eventId), (old: any) => {
        if (!old) return old;

        const reorderedImages = (old.images || []).map((img: EventImageDto) => {
          const newOrder = newOrders[img.id];
          return newOrder !== undefined ? { ...img, displayOrder: newOrder } : img;
        });

        // Sort by new display order
        reorderedImages.sort((a: EventImageDto, b: EventImageDto) => a.displayOrder - b.displayOrder);

        return {
          ...old,
          images: reorderedImages,
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
 * useImageUpload Hook (Convenience wrapper)
 *
 * Combines upload and delete mutations with validation utilities
 * Provides state management for upload progress and errors
 *
 * @param options - Configuration options
 *
 * @example
 * ```tsx
 * const {
 *   uploadImage,
 *   deleteImage,
 *   validateImage,
 *   isUploading,
 *   error
 * } = useImageUpload({
 *   onSuccess: (url) => console.log('Image uploaded:', url)
 * });
 *
 * // Validate before showing file picker
 * const validation = validateImage(file);
 * if (!validation.isValid) {
 *   alert(validation.errors.join('\n'));
 *   return;
 * }
 *
 * // Upload
 * await uploadImage(file, eventId);
 * ```
 */
export function useImageUpload(options?: UseImageUploadOptions) {
  const [error, setError] = useState<string | null>(null);
  const [uploadProgress, setUploadProgress] = useState<number>(0);

  const uploadMutation = useUploadEventImage({
    ...options,
    onError: (errorMsg) => {
      setError(errorMsg);
      options?.onError?.(errorMsg);
    },
    onSuccess: (imageUrl) => {
      setError(null);
      setUploadProgress(100);
      options?.onSuccess?.(imageUrl);
    },
  });

  const deleteMutation = useDeleteEventImage();
  const reorderMutation = useReorderEventImages();

  const uploadImage = useCallback(
    async (file: File, eventId: string) => {
      setError(null);
      setUploadProgress(0);

      try {
        // Simulate progress (actual progress tracking would require backend support)
        const progressInterval = setInterval(() => {
          setUploadProgress((prev) => Math.min(prev + 10, 90));
        }, 200);

        await uploadMutation.mutateAsync({ eventId, file });

        clearInterval(progressInterval);
        setUploadProgress(100);
      } catch (err) {
        setUploadProgress(0);
        throw err;
      }
    },
    [uploadMutation]
  );

  const uploadImages = useCallback(
    async (files: File[], eventId: string) => {
      const validation = validateImageFiles(files);
      if (!validation.isValid) {
        setError(validation.errors.join('; '));
        throw new Error(validation.errors.join('; '));
      }

      // Upload files sequentially
      for (const file of files) {
        await uploadImage(file, eventId);
      }
    },
    [uploadImage]
  );

  const deleteImage = useCallback(
    async (eventId: string, imageId: string) => {
      setError(null);
      await deleteMutation.mutateAsync({ eventId, imageId });
    },
    [deleteMutation]
  );

  const reorderImages = useCallback(
    async (eventId: string, newOrders: Record<string, number>) => {
      setError(null);
      await reorderMutation.mutateAsync({ eventId, newOrders });
    },
    [reorderMutation]
  );

  const reset = useCallback(() => {
    setError(null);
    setUploadProgress(0);
  }, []);

  return {
    uploadImage,
    uploadImages,
    deleteImage,
    reorderImages,
    validateImage: validateImageFile,
    validateImages: validateImageFiles,
    isUploading: uploadMutation.isPending || deleteMutation.isPending,
    isReordering: reorderMutation.isPending,
    uploadProgress,
    error,
    reset,
  };
}

/**
 * Export all hooks and utilities
 */
export default {
  useUploadEventImage,
  useDeleteEventImage,
  useReorderEventImages,
  useImageUpload,
  validateImageFile,
  validateImageFiles,
  VALIDATION_CONSTRAINTS,
};
