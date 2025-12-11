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
import { Upload, X, Loader2, AlertCircle, Image as ImageIcon, GripVertical, Star } from 'lucide-react';
import {
  DndContext,
  closestCenter,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  DragEndEvent,
} from '@dnd-kit/core';
import {
  arrayMove,
  SortableContext,
  sortableKeyboardCoordinates,
  useSortable,
  rectSortingStrategy,
} from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';

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
    isPrimary: boolean;
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
/**
 * SortableImageItem Component
 * Individual draggable image item with drag handle and delete button
 */
interface SortableImageItemProps {
  image: {
    id: string;
    imageUrl: string;
    displayOrder: number;
    isPrimary: boolean;
  };
  onDelete: (imageId: string) => void;
  onSetPrimary: (imageId: string) => void;
  disabled: boolean;
  isUploading: boolean;
  isSettingPrimary: boolean;
}

const SortableImageItem: React.FC<SortableImageItemProps> = ({
  image,
  onDelete,
  onSetPrimary,
  disabled,
  isUploading,
  isSettingPrimary,
}) => {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id: image.id });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
  };

  return (
    <div
      ref={setNodeRef}
      style={style}
      className={cn(
        'relative aspect-square group rounded-lg overflow-hidden border-2 transition-colors',
        isDragging
          ? 'border-blue-500 shadow-lg z-50'
          : image.isPrimary
          ? 'border-yellow-400 dark:border-yellow-500 shadow-md'
          : 'border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600'
      )}
    >
      {/* Image Preview */}
      <Image
        src={image.imageUrl}
        alt={`Event image ${image.displayOrder}`}
        fill
        className="object-cover"
        sizes="(max-width: 640px) 50vw, (max-width: 768px) 33vw, 25vw"
      />

      {/* Drag Handle */}
      <div
        {...attributes}
        {...listeners}
        className="absolute top-2 right-2 p-1.5 bg-black/60 text-white rounded cursor-move hover:bg-black/80 transition-colors"
        aria-label="Drag to reorder"
      >
        <GripVertical className="w-4 h-4" />
      </div>

      {/* Primary Star Badge */}
      {image.isPrimary && (
        <div className="absolute top-2 left-2 bg-yellow-500 text-white text-xs font-medium px-2 py-1 rounded flex items-center gap-1 shadow-md">
          <Star className="w-3 h-3 fill-white" />
          Main
        </div>
      )}

      {/* Display Order Badge (only if not primary) */}
      {!image.isPrimary && (
        <div className="absolute top-2 left-2 bg-black/60 text-white text-xs font-medium px-2 py-1 rounded">
          #{image.displayOrder}
        </div>
      )}

      {/* Action Buttons Overlay */}
      <div className="absolute inset-0 bg-black/0 group-hover:bg-black/40 transition-colors flex flex-col items-center justify-center gap-2">
        {!image.isPrimary && (
          <Button
            variant="default"
            size="sm"
            onClick={() => onSetPrimary(image.id)}
            disabled={disabled || isUploading || isSettingPrimary}
            className="opacity-0 group-hover:opacity-100 transition-opacity bg-yellow-500 hover:bg-yellow-600 text-white"
            aria-label="Set as main image"
          >
            <Star className="w-4 h-4 mr-1" />
            Set as Main
          </Button>
        )}
        <Button
          variant="destructive"
          size="sm"
          onClick={() => onDelete(image.id)}
          disabled={disabled || isUploading}
          className="opacity-0 group-hover:opacity-100 transition-opacity"
          aria-label={`Delete image ${image.displayOrder}`}
        >
          <X className="w-4 h-4 mr-1" />
          Delete
        </Button>
      </div>
    </div>
  );
};

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
  const [localImages, setLocalImages] = useState(existingImages);

  // Sync localImages with existingImages when they change
  React.useEffect(() => {
    setLocalImages(existingImages);
  }, [existingImages]);

  const {
    uploadImage,
    deleteImage,
    reorderImages,
    setPrimaryImage,
    validateImages,
    isUploading,
    isReordering,
    isSettingPrimary,
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

  // Setup drag-and-drop sensors
  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: {
        distance: 8, // Prevent accidental drags
      },
    }),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    })
  );

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

  // Handle drag end
  const handleDragEnd = useCallback(
    async (event: DragEndEvent) => {
      const { active, over } = event;

      if (!over || active.id === over.id) {
        return;
      }

      const oldIndex = localImages.findIndex(img => img.id === active.id);
      const newIndex = localImages.findIndex(img => img.id === over.id);

      if (oldIndex === -1 || newIndex === -1) {
        return;
      }

      // Reorder locally for instant feedback
      const newOrder = arrayMove(localImages, oldIndex, newIndex);
      setLocalImages(newOrder);

      // Build newOrders map with 1-indexed display orders
      const newOrders: Record<string, number> = {};
      newOrder.forEach((img, index) => {
        newOrders[img.id] = index + 1;
      });

      // Send to backend
      try {
        await reorderImages(eventId, newOrders);
      } catch (err) {
        console.error('Reorder failed:', err);
        // Rollback is handled by the mutation hook
        setLocalImages(existingImages);
      }
    },
    [localImages, existingImages, eventId, reorderImages]
  );

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

  // Handle set primary image
  const handleSetPrimaryImage = async (imageId: string) => {
    if (disabled || isUploading || isSettingPrimary) return;

    try {
      await setPrimaryImage(eventId, imageId);
    } catch (err) {
      console.error('Set primary failed:', err);
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

      {/* Image Gallery Grid with Drag-and-Drop */}
      {localImages.length > 0 && (
        <div>
          <div className="flex items-center justify-between mb-3">
            <h3 className="text-sm font-medium text-gray-700 dark:text-gray-300">
              Event Images ({localImages.length}/{maxImages})
            </h3>
            {isReordering && (
              <div className="flex items-center gap-2 text-xs text-blue-600 dark:text-blue-400">
                <Loader2 className="w-3 h-3 animate-spin" />
                Saving order...
              </div>
            )}
          </div>

          <DndContext
            sensors={sensors}
            collisionDetection={closestCenter}
            onDragEnd={handleDragEnd}
          >
            <SortableContext
              items={localImages.map(img => img.id)}
              strategy={rectSortingStrategy}
            >
              <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-4">
                {localImages
                  .sort((a, b) => a.displayOrder - b.displayOrder)
                  .map((image) => (
                    <SortableImageItem
                      key={image.id}
                      image={image}
                      onDelete={handleDeleteImage}
                      onSetPrimary={handleSetPrimaryImage}
                      disabled={disabled}
                      isUploading={isUploading}
                      isSettingPrimary={isSettingPrimary}
                    />
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
            </SortableContext>
          </DndContext>

          {/* Drag Instructions */}
          {localImages.length > 1 && (
            <p className="text-xs text-gray-500 dark:text-gray-400 mt-3 text-center">
              💡 Drag the grip icon to reorder images
            </p>
          )}
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
