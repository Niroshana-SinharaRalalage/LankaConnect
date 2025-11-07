'use client';

import React, { useRef, useState, useCallback } from 'react';
import Image from 'next/image';
import { Upload, X, Loader2 } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { cn } from '@/presentation/lib/utils';

export interface PhotoUploadWidgetProps {
  currentPhotoUrl?: string | null;
  onUpload: (file: File) => Promise<void> | void;
  onDelete: () => Promise<void> | void;
  maxSizeMB?: number;
  acceptedFormats?: string[];
  isLoading?: boolean;
  error?: string;
}

const DEFAULT_ACCEPTED_FORMATS = ['image/jpeg', 'image/jpg', 'image/png', 'image/webp'];
const DEFAULT_MAX_SIZE_MB = 5;

/**
 * PhotoUploadWidget Component
 *
 * Reusable photo upload widget with:
 * - Drag and drop support
 * - Click to upload
 * - File validation (type and size)
 * - Preview current photo
 * - Delete functionality
 * - Loading states
 * - Error handling
 *
 * Follows UI/UX best practices with accessibility support
 */
export const PhotoUploadWidget: React.FC<PhotoUploadWidgetProps> = ({
  currentPhotoUrl,
  onUpload,
  onDelete,
  maxSizeMB = DEFAULT_MAX_SIZE_MB,
  acceptedFormats = DEFAULT_ACCEPTED_FORMATS,
  isLoading = false,
  error,
}) => {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [isDragOver, setIsDragOver] = useState(false);
  const [validationError, setValidationError] = useState<string>('');

  // Format accepted formats for display
  const getFormatsDisplay = () => {
    return acceptedFormats
      .map((format) => format.replace('image/', '').toUpperCase())
      .join(', ');
  };

  // Validate file
  const validateFile = (file: File): boolean => {
    setValidationError('');

    // Check file type
    if (!acceptedFormats.includes(file.type)) {
      setValidationError(`Only ${getFormatsDisplay()} images are allowed`);
      return false;
    }

    // Check file size
    const fileSizeMB = file.size / (1024 * 1024);
    if (fileSizeMB > maxSizeMB) {
      setValidationError(`File size exceeds ${maxSizeMB}MB`);
      return false;
    }

    return true;
  };

  // Handle file selection
  const handleFileSelect = useCallback(
    async (file: File | null) => {
      if (!file) return;

      if (validateFile(file)) {
        await onUpload(file);
      }
    },
    [onUpload, maxSizeMB, acceptedFormats]
  );

  // Handle file input change
  const handleFileInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      handleFileSelect(file);
    }
    // Reset input value to allow selecting the same file again
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  // Handle drag over
  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragOver(true);
  };

  // Handle drag leave
  const handleDragLeave = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragOver(false);
  };

  // Handle drop
  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragOver(false);

    const file = e.dataTransfer.files?.[0];
    if (file) {
      handleFileSelect(file);
    }
  };

  // Handle click on upload area
  const handleUploadAreaClick = () => {
    fileInputRef.current?.click();
  };

  // Handle delete
  const handleDelete = async () => {
    if (!isLoading) {
      await onDelete();
    }
  };

  const displayError = error || validationError;

  return (
    <div className="space-y-4">
      {/* Current Photo Preview */}
      {currentPhotoUrl && (
        <div className="relative flex flex-col items-center space-y-4">
          <div className="relative h-32 w-32 overflow-hidden rounded-full border-4 border-border">
            <Image
              src={currentPhotoUrl}
              alt="Current profile photo"
              fill
              className="object-cover"
              sizes="128px"
            />
          </div>
          <Button
            type="button"
            variant="destructive"
            size="sm"
            onClick={handleDelete}
            disabled={isLoading}
            aria-label="Delete photo"
          >
            <X className="mr-2 h-4 w-4" />
            Delete Photo
          </Button>
        </div>
      )}

      {/* Upload Area */}
      {!currentPhotoUrl && (
        <div
          className={cn(
            'relative flex flex-col items-center justify-center rounded-lg border-2 border-dashed p-8 transition-colors',
            isDragOver && 'border-primary bg-primary/5',
            !isDragOver && 'border-border hover:border-primary/50',
            isLoading && 'pointer-events-none opacity-50'
          )}
          onDragOver={handleDragOver}
          onDragLeave={handleDragLeave}
          onDrop={handleDrop}
          onClick={handleUploadAreaClick}
          role="button"
          tabIndex={0}
          onKeyDown={(e) => {
            if (e.key === 'Enter' || e.key === ' ') {
              handleUploadAreaClick();
            }
          }}
        >
          <input
            ref={fileInputRef}
            type="file"
            accept={acceptedFormats.join(',')}
            onChange={handleFileInputChange}
            className="hidden"
            disabled={isLoading}
            aria-label="Upload profile photo"
          />

          {isLoading ? (
            <div className="flex flex-col items-center space-y-2" role="status" aria-live="polite">
              <Loader2 className="h-12 w-12 animate-spin text-primary" />
              <p className="text-sm text-muted-foreground">Uploading...</p>
            </div>
          ) : (
            <>
              <Upload className="mb-4 h-12 w-12 text-muted-foreground" />
              <p className="mb-2 text-sm font-medium text-foreground">Upload Photo</p>
              <p className="mb-1 text-xs text-muted-foreground">
                Drag and drop or click to select
              </p>
              <p className="text-xs text-muted-foreground">
                {getFormatsDisplay()} (Max {maxSizeMB}MB)
              </p>
            </>
          )}
        </div>
      )}

      {/* Error Message */}
      {displayError && (
        <div className="rounded-md bg-destructive/10 p-3">
          <p className="text-sm text-destructive">{displayError}</p>
        </div>
      )}
    </div>
  );
};
