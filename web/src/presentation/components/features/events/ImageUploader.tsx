'use client';

/**
 * ImageUploader Component
 * Phase 6A.9: Azure Blob Image Upload System
 *
 * Professional drag-and-drop image uploader for event galleries
 * Integrates with Azure Blob Storage via useImageUpload hook
 *
 * Features:
 * - Drag-and-drop support (react-dropzone)
 * - Multiple file uploads
 * - Image preview gallery
 * - Upload progress tracking
 * - File validation (size, type)
 * - Delete functionality
 * - Responsive design
 * - Accessibility support
 */

import React, { useCallback, useState } from 'react';
import { useDropzone } from 'react-dropzone';
import Image from 'next/image';
import { Upload, X, Loader2, AlertCircle, Image as ImageIcon } from 'lucide-react';

import { useImageUpload } from '@/presentation/hooks/useImageUpload';
import { Button } from '@/presentation/components/ui/Button';
import { cn } from '@/presentation/lib/utils';

export interface ImageUploaderProps {
  /** Event ID for image uploads */
  eventId: string;

  /** Existing images in the event gallery */
  existingImages?: Array<{
    id: string;
    imageUrl: string;
    displayOrder: number;
  }>;

  /** Maximum number of images allowed */
  maxImages?: number;

  /** Callback when images change */
  onImagesChange?: (imageUrls: string[]) => void;

  /** Callback when upload completes */
  onUploadComplete?: () => void;

  /** Disable uploads */
  disabled?: boolean;

  /** Additional CSS classes */
  className?: string;
}

/**
 * ImageUploader Component
 *
 * @example
 * ```tsx
 * <ImageUploader
 *   eventId="event-123"
 *   existingImages={event.images}
 *   maxImages={10}
 *   onUploadComplete={() => console.log('Upload complete!')}
 * />
 * ```
 */
export const ImageUploader: React.FC<ImageUploaderProps> = ({
  eventId,
  existingImages = [],
  maxImages = 10,
  onImagesChange,
  onUploadComplete,
  disabled = false,
  className,
}) => {
  const [uploadingFiles, setUploadingFiles] = useState<Set<string>>(new Set());

  const {
    uploadImage,
    deleteImage,
    validateImages,
    isUploading,
    error,
    reset,
  } = useImageUpload({
    onSuccess: (imageUrl) => {
      onImagesChange?.([...existingImages.map(img => img.imageUrl), imageUrl]);
      onUploadComplete?.();
    },
    onError: (errorMsg) => {
      console.error('Image upload error:', errorMsg);
    },
  });

  // Check if max images reached
  const imagesRemaining = maxImages - existingImages.length;
  const canUploadMore = imagesRemaining > 0 && !disabled;

  // Handle file drop
  const onDrop = useCallback(
    async (acceptedFiles: File[]) => {
      if (!canUploadMore) return;

      // Validate files
      const validation = validateImages(acceptedFiles.slice(0, imagesRemaining));
      if (!validation.isValid) {
        alert(validation.errors.join('\n'));
        return;
      }

      // Upload files sequentially
      for (const file of acceptedFiles.slice(0, imagesRemaining)) {
        const fileId = `${file.name}-${Date.now()}`;
        setUploadingFiles(prev => new Set(prev).add(fileId));

        try {
          await uploadImage(file, eventId);
        } catch (err) {
          console.error('Upload failed for', file.name, err);
        } finally {
          setUploadingFiles(prev => {
            const newSet = new Set(prev);
            newSet.delete(fileId);
            return newSet;
          });
        }
      }
    },
    [eventId, canUploadMore, imagesRemaining, uploadImage, validateImages]
  );

  // Setup react-dropzone
  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: {
      'image/jpeg': ['.jpg', '.jpeg'],
      'image/png': ['.png'],
      'image/gif': ['.gif'],
      'image/webp': ['.webp'],
    },
    maxSize: 10 * 1024 * 1024, // 10 MB
    maxFiles: imagesRemaining,
    disabled: !canUploadMore || isUploading,
    multiple: true,
  });

  // Handle delete image
  const handleDeleteImage = async (imageId: string) => {
    if (disabled || isUploading) return;

    const confirmed = window.confirm('Are you sure you want to delete this image?');
    if (!confirmed) return;

    try {
      await deleteImage(eventId, imageId);
      const remainingImages = existingImages.filter(img => img.id !== imageId);
      onImagesChange?.(remainingImages.map(img => img.imageUrl));
    } catch (err) {
      console.error('Delete failed:', err);
    }
  };

  return (
    <div className={cn('space-y-4', className)}>
      {/* Upload Dropzone */}
      {canUploadMore && (
        <div
          {...getRootProps()}
          className={cn(
            'border-2 border-dashed rounded-lg p-8 text-center transition-colors cursor-pointer',
            isDragActive
              ? 'border-blue-500 bg-blue-50 dark:bg-blue-950/20'
              : 'border-gray-300 dark:border-gray-700 hover:border-gray-400 dark:hover:border-gray-600',
            (disabled || isUploading) && 'opacity-50 cursor-not-allowed',
            'focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2'
          )}
        >
          <input {...getInputProps()} aria-label="Upload images" />

          <div className="flex flex-col items-center justify-center space-y-3">
            {isUploading ? (
              <>
                <Loader2 className="w-12 h-12 text-blue-500 animate-spin" />
                <p className="text-sm font-medium text-gray-700 dark:text-gray-300">
                  Uploading images...
                </p>
              </>
            ) : (
              <>
                <Upload className="w-12 h-12 text-gray-400" />
                <div className="space-y-1">
                  <p className="text-sm font-medium text-gray-700 dark:text-gray-300">
                    {isDragActive
                      ? 'Drop images here'
                      : 'Drag & drop images here, or click to select'}
                  </p>
                  <p className="text-xs text-gray-500 dark:text-gray-400">
                    Supports JPG, PNG, GIF, WebP (max 10MB each)
                  </p>
                  <p className="text-xs text-gray-500 dark:text-gray-400">
                    {imagesRemaining} {imagesRemaining === 1 ? 'image' : 'images'} remaining
                    (max {maxImages})
                  </p>
                </div>
              </>
            )}
          </div>
        </div>
      )}

      {/* Error Display */}
      {error && (
        <div className="flex items-start gap-2 p-4 bg-red-50 dark:bg-red-950/20 border border-red-200 dark:border-red-800 rounded-lg">
          <AlertCircle className="w-5 h-5 text-red-600 dark:text-red-400 flex-shrink-0 mt-0.5" />
          <div className="flex-1">
            <p className="text-sm font-medium text-red-800 dark:text-red-200">
              Upload Error
            </p>
            <p className="text-sm text-red-600 dark:text-red-400 mt-1">{error}</p>
          </div>
          <Button
            variant="ghost"
            size="sm"
            onClick={reset}
            className="text-red-600 hover:text-red-700 dark:text-red-400 dark:hover:text-red-300"
          >
            Dismiss
          </Button>
        </div>
      )}

      {/* Max Images Reached Message */}
      {!canUploadMore && !disabled && (
        <div className="flex items-center gap-2 p-4 bg-blue-50 dark:bg-blue-950/20 border border-blue-200 dark:border-blue-800 rounded-lg">
          <ImageIcon className="w-5 h-5 text-blue-600 dark:text-blue-400 flex-shrink-0" />
          <p className="text-sm text-blue-800 dark:text-blue-200">
            Maximum of {maxImages} images reached. Delete an image to upload more.
          </p>
        </div>
      )}

      {/* Image Gallery Grid */}
      {existingImages.length > 0 && (
        <div>
          <h3 className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-3">
            Event Images ({existingImages.length}/{maxImages})
          </h3>

          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-4">
            {existingImages
              .sort((a, b) => a.displayOrder - b.displayOrder)
              .map((image) => (
                <div
                  key={image.id}
                  className="relative aspect-square group rounded-lg overflow-hidden border-2 border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600 transition-colors"
                >
                  {/* Image Preview */}
                  <Image
                    src={image.imageUrl}
                    alt={`Event image ${image.displayOrder}`}
                    fill
                    className="object-cover"
                    sizes="(max-width: 640px) 50vw, (max-width: 768px) 33vw, 25vw"
                  />

                  {/* Delete Button Overlay */}
                  <div className="absolute inset-0 bg-black/0 group-hover:bg-black/40 transition-colors flex items-center justify-center">
                    <Button
                      variant="destructive"
                      size="sm"
                      onClick={() => handleDeleteImage(image.id)}
                      disabled={disabled || isUploading}
                      className="opacity-0 group-hover:opacity-100 transition-opacity"
                      aria-label={`Delete image ${image.displayOrder}`}
                    >
                      <X className="w-4 h-4 mr-1" />
                      Delete
                    </Button>
                  </div>

                  {/* Display Order Badge */}
                  <div className="absolute top-2 left-2 bg-black/60 text-white text-xs font-medium px-2 py-1 rounded">
                    #{image.displayOrder}
                  </div>
                </div>
              ))}

            {/* Loading Placeholders */}
            {Array.from(uploadingFiles).map((fileId) => (
              <div
                key={fileId}
                className="relative aspect-square rounded-lg overflow-hidden border-2 border-blue-300 dark:border-blue-700 bg-blue-50 dark:bg-blue-950/20 flex items-center justify-center"
              >
                <Loader2 className="w-8 h-8 text-blue-500 animate-spin" />
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Empty State */}
      {existingImages.length === 0 && !isUploading && (
        <div className="text-center py-8 text-gray-500 dark:text-gray-400">
          <ImageIcon className="w-16 h-16 mx-auto mb-3 opacity-50" />
          <p className="text-sm">No images uploaded yet</p>
          <p className="text-xs mt-1">Upload your first event image above</p>
        </div>
      )}
    </div>
  );
};

export default ImageUploader;
