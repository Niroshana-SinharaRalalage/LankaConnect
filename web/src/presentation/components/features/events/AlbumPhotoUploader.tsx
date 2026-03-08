'use client';

/**
 * AlbumPhotoUploader Component
 *
 * Drag-and-drop photo upload widget for event photo albums.
 * Validates file type and size, provides optional caption input,
 * and shows upload progress. Uses the useUploadAlbumPhoto mutation hook.
 */

import { useState, useCallback } from 'react';
import { useDropzone, type FileRejection } from 'react-dropzone';
import { Upload, Loader2, AlertCircle, X } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { Textarea } from '@/presentation/components/ui/Textarea';
import { Label } from '@/presentation/components/ui/Label';
import { useUploadAlbumPhoto } from '@/presentation/hooks/usePhotoAlbum';
import { cn } from '@/presentation/lib/utils';

export interface AlbumPhotoUploaderProps {
  /** Event ID to upload photos to */
  eventId: string;
  /** Callback after a successful upload */
  onUploadComplete?: () => void;
}

/** Maximum file size in bytes (10 MB) */
const MAX_FILE_SIZE = 10 * 1024 * 1024;

/** Accepted MIME types and extensions */
const ACCEPTED_TYPES = {
  'image/jpeg': ['.jpg', '.jpeg'],
  'image/png': ['.png'],
  'image/gif': ['.gif'],
  'image/webp': ['.webp'],
};

/** Maximum caption length */
const MAX_CAPTION_LENGTH = 500;

export function AlbumPhotoUploader({ eventId, onUploadComplete }: AlbumPhotoUploaderProps) {
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [preview, setPreview] = useState<string | null>(null);
  const [caption, setCaption] = useState('');
  const [validationError, setValidationError] = useState<string | null>(null);

  const uploadMutation = useUploadAlbumPhoto();

  const clearSelection = useCallback(() => {
    setSelectedFile(null);
    setPreview(null);
    setCaption('');
    setValidationError(null);
  }, []);

  const onDrop = useCallback(
    (acceptedFiles: File[], rejectedFiles: FileRejection[]) => {
      setValidationError(null);

      if (rejectedFiles.length > 0) {
        const errors = rejectedFiles.flatMap((f) => f.errors.map((e) => e.message));
        const uniqueErrors = [...new Set(errors)];
        setValidationError(uniqueErrors.join('. '));
        return;
      }

      const file = acceptedFiles[0];
      if (!file) return;

      setSelectedFile(file);

      // Generate preview URL
      const objectUrl = URL.createObjectURL(file);
      setPreview(objectUrl);
    },
    []
  );

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    accept: ACCEPTED_TYPES,
    maxSize: MAX_FILE_SIZE,
    maxFiles: 1,
    onDrop,
    disabled: uploadMutation.isPending,
  });

  const handleUpload = async () => {
    if (!selectedFile) return;

    try {
      await uploadMutation.mutateAsync({
        eventId,
        file: selectedFile,
        caption: caption.trim() || undefined,
      });

      clearSelection();
      onUploadComplete?.();
    } catch (error) {
      // Error is available via uploadMutation.error
      console.error('Photo upload failed:', error);
    }
  };

  return (
    <div className="space-y-4">
      {/* Dropzone Area */}
      {!selectedFile && (
        <div
          {...getRootProps()}
          className={cn(
            'border-2 border-dashed rounded-lg p-8 text-center transition-colors cursor-pointer',
            isDragActive
              ? 'border-blue-500 bg-blue-50 dark:bg-blue-950/20'
              : 'border-gray-300 dark:border-gray-700 hover:border-gray-400 dark:hover:border-gray-600',
            uploadMutation.isPending && 'opacity-50 cursor-not-allowed',
            'focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2'
          )}
        >
          <input {...getInputProps()} aria-label="Upload photo" />

          <div className="flex flex-col items-center justify-center space-y-3">
            <Upload className="w-10 h-10 text-gray-400" />
            <div className="space-y-1">
              <p className="text-sm font-medium text-gray-700 dark:text-gray-300">
                {isDragActive
                  ? 'Drop your photo here'
                  : 'Drag & drop a photo here, or click to select'}
              </p>
              <p className="text-xs text-gray-500 dark:text-gray-400">
                JPG, PNG, GIF, or WebP (max 10 MB)
              </p>
            </div>
          </div>
        </div>
      )}

      {/* Validation Error */}
      {validationError && (
        <div className="flex items-start gap-2 p-3 bg-red-50 dark:bg-red-950/20 border border-red-200 dark:border-red-800 rounded-lg">
          <AlertCircle className="w-4 h-4 text-red-600 dark:text-red-400 flex-shrink-0 mt-0.5" />
          <p className="text-sm text-red-600 dark:text-red-400">{validationError}</p>
        </div>
      )}

      {/* Upload Error from mutation */}
      {uploadMutation.isError && (
        <div className="flex items-start gap-2 p-3 bg-red-50 dark:bg-red-950/20 border border-red-200 dark:border-red-800 rounded-lg">
          <AlertCircle className="w-4 h-4 text-red-600 dark:text-red-400 flex-shrink-0 mt-0.5" />
          <p className="text-sm text-red-600 dark:text-red-400">
            Upload failed. Please try again.
          </p>
        </div>
      )}

      {/* Selected File Preview */}
      {selectedFile && preview && (
        <div className="space-y-4">
          <div className="relative">
            {/* Preview Image */}
            <div className="relative aspect-video w-full max-w-sm mx-auto rounded-lg overflow-hidden bg-neutral-100 dark:bg-neutral-800">
              <img
                src={preview}
                alt="Upload preview"
                className="w-full h-full object-contain"
              />

              {/* Uploading Overlay */}
              {uploadMutation.isPending && (
                <div className="absolute inset-0 bg-black/50 flex items-center justify-center">
                  <div className="flex flex-col items-center gap-2">
                    <Loader2 className="w-8 h-8 text-white animate-spin" />
                    <p className="text-white text-sm font-medium">Uploading...</p>
                  </div>
                </div>
              )}
            </div>

            {/* Clear Selection Button */}
            {!uploadMutation.isPending && (
              <button
                type="button"
                onClick={clearSelection}
                className="absolute top-2 right-2 p-1.5 bg-black/60 text-white rounded-full hover:bg-black/80 transition-colors"
                aria-label="Remove selected photo"
              >
                <X className="w-4 h-4" />
              </button>
            )}
          </div>

          {/* File Info */}
          <p className="text-xs text-gray-500 dark:text-gray-400 text-center">
            {selectedFile.name} ({(selectedFile.size / (1024 * 1024)).toFixed(1)} MB)
          </p>

          {/* Caption Input */}
          <div>
            <Label htmlFor="photo-caption">
              Caption (optional)
            </Label>
            <Textarea
              id="photo-caption"
              value={caption}
              onChange={(e) => setCaption(e.target.value.slice(0, MAX_CAPTION_LENGTH))}
              placeholder="Add a caption for this photo..."
              rows={2}
              maxLength={MAX_CAPTION_LENGTH}
              disabled={uploadMutation.isPending}
            />
            <p className="text-xs text-gray-500 dark:text-gray-400 mt-1 text-right">
              {caption.length}/{MAX_CAPTION_LENGTH}
            </p>
          </div>

          {/* Upload Button */}
          <Button
            onClick={handleUpload}
            loading={uploadMutation.isPending}
            disabled={uploadMutation.isPending}
            className="w-full"
          >
            <Upload className="w-4 h-4 mr-2" />
            Upload Photo
          </Button>
        </div>
      )}
    </div>
  );
}

export default AlbumPhotoUploader;
