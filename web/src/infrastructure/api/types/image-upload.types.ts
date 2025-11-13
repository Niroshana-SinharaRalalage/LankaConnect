/**
 * Image Upload Type Definitions
 * Client-side types for image upload functionality
 * Phase 6A.9: Azure Blob Image Upload System
 */

// ==================== Validation ====================

/**
 * Image upload validation constraints
 */
export const IMAGE_UPLOAD_CONSTRAINTS = {
  MAX_FILE_SIZE: 10 * 1024 * 1024, // 10 MB (matches backend)
  ALLOWED_MIME_TYPES: ['image/jpeg', 'image/png', 'image/gif', 'image/webp'],
  ALLOWED_EXTENSIONS: ['.jpg', '.jpeg', '.png', '.gif', '.webp'],
  MAX_IMAGES_PER_EVENT: 10, // Matches backend MAX_IMAGES constant
} as const;

/**
 * Image file validation result
 */
export interface ImageValidationResult {
  isValid: boolean;
  errors: string[];
}

/**
 * Image file metadata
 */
export interface ImageFileMetadata {
  file: File;
  preview: string; // Object URL for preview
  size: number;
  type: string;
  name: string;
}

// ==================== Upload State ====================

/**
 * Upload status for tracking progress
 */
export enum ImageUploadStatus {
  Idle = 'idle',
  Uploading = 'uploading',
  Success = 'success',
  Error = 'error',
}

/**
 * Single image upload state
 */
export interface ImageUploadState {
  id: string; // Temporary ID for UI tracking
  file: File;
  preview: string;
  status: ImageUploadStatus;
  progress: number; // 0-100
  error?: string;
  uploadedImageId?: string; // Backend image ID after successful upload
  uploadedImageUrl?: string; // Azure Blob URL after successful upload
}

/**
 * Multiple images upload state (for managing gallery)
 */
export interface ImagesUploadState {
  images: ImageUploadState[];
  isUploading: boolean;
  hasErrors: boolean;
  completedCount: number;
  totalCount: number;
}

// ==================== Hook Return Types ====================

/**
 * Return type for useImageUpload hook
 */
export interface UseImageUploadReturn {
  uploadImage: (file: File, eventId: string) => Promise<void>;
  uploadImages: (files: File[], eventId: string) => Promise<void>;
  deleteImage: (eventId: string, imageId: string) => Promise<void>;
  validateImage: (file: File) => ImageValidationResult;
  validateImages: (files: File[]) => ImageValidationResult;
  isUploading: boolean;
  uploadProgress: number;
  error: string | null;
  reset: () => void;
}

/**
 * Options for useImageUpload hook
 */
export interface UseImageUploadOptions {
  onSuccess?: (imageUrl: string) => void;
  onError?: (error: string) => void;
  maxFileSize?: number;
  allowedTypes?: string[];
}

// ==================== Component Props ====================

/**
 * Props for ImageUploader component
 */
export interface ImageUploaderProps {
  eventId: string;
  existingImages?: Array<{
    id: string;
    imageUrl: string;
    displayOrder: number;
  }>;
  maxImages?: number;
  onImagesChange?: (imageUrls: string[]) => void;
  onUploadComplete?: () => void;
  disabled?: boolean;
  className?: string;
}

/**
 * Props for ImagePreview component
 */
export interface ImagePreviewProps {
  image: ImageUploadState;
  onDelete: () => void;
  onRetry?: () => void;
  disabled?: boolean;
}

/**
 * Props for ImageDropzone component
 */
export interface ImageDropzoneProps {
  onDrop: (files: File[]) => void;
  maxFiles?: number;
  disabled?: boolean;
  className?: string;
}
