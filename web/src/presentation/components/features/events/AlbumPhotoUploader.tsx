'use client';

/**
 * AlbumPhotoUploader Component
 *
 * Multi-file drag-and-drop photo upload widget for event photo albums.
 * Supports batch selection (up to 10 files), sequential upload with per-file
 * progress indicators, and proper error handling with backend error messages.
 */

import { useState, useCallback, useRef, useEffect } from 'react';
import { useDropzone, type FileRejection } from 'react-dropzone';
import { Upload, Loader2, AlertCircle, CheckCircle2, X, ImageIcon } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { useUploadAlbumPhoto } from '@/presentation/hooks/usePhotoAlbum';
import { useQueryClient } from '@tanstack/react-query';
import { albumKeys } from '@/presentation/hooks/usePhotoAlbum';
import { cn } from '@/presentation/lib/utils';

export interface AlbumPhotoUploaderProps {
  eventId: string;
  onUploadComplete?: () => void;
}

const MAX_FILE_SIZE = 10 * 1024 * 1024; // 10 MB
const MAX_FILES = 10;

const ACCEPTED_TYPES = {
  'image/jpeg': ['.jpg', '.jpeg'],
  'image/png': ['.png'],
  'image/gif': ['.gif'],
  'image/webp': ['.webp'],
};

type FileStatus = 'pending' | 'uploading' | 'done' | 'failed';

interface QueuedFile {
  id: string;
  file: File;
  preview: string;
  status: FileStatus;
  error?: string;
}

function getErrorMessage(error: unknown): string {
  if (error && typeof error === 'object') {
    const err = error as { response?: { data?: { detail?: string } }; message?: string };
    if (err.response?.data?.detail) return err.response.data.detail;
    if (err.message) return err.message;
  }
  return 'Upload failed. Please try again.';
}

export function AlbumPhotoUploader({ eventId, onUploadComplete }: AlbumPhotoUploaderProps) {
  const [queue, setQueue] = useState<QueuedFile[]>([]);
  const [isUploading, setIsUploading] = useState(false);
  const [validationError, setValidationError] = useState<string | null>(null);
  const objectUrlsRef = useRef<Set<string>>(new Set());
  const uploadMutation = useUploadAlbumPhoto();
  const queryClient = useQueryClient();

  // Revoke all object URLs on unmount
  useEffect(() => {
    return () => {
      objectUrlsRef.current.forEach((url) => URL.revokeObjectURL(url));
    };
  }, []);

  // Warn before navigating away during upload
  useEffect(() => {
    if (!isUploading) return;
    const handler = (e: BeforeUnloadEvent) => {
      e.preventDefault();
    };
    window.addEventListener('beforeunload', handler);
    return () => window.removeEventListener('beforeunload', handler);
  }, [isUploading]);

  const removeFile = useCallback((id: string) => {
    setQueue((prev) => {
      const item = prev.find((f) => f.id === id);
      if (item) {
        URL.revokeObjectURL(item.preview);
        objectUrlsRef.current.delete(item.preview);
      }
      return prev.filter((f) => f.id !== id);
    });
  }, []);

  const clearQueue = useCallback(() => {
    setQueue((prev) => {
      prev.forEach((f) => {
        URL.revokeObjectURL(f.preview);
        objectUrlsRef.current.delete(f.preview);
      });
      return [];
    });
    setValidationError(null);
  }, []);

  const onDrop = useCallback(
    (acceptedFiles: File[], rejectedFiles: FileRejection[]) => {
      setValidationError(null);

      if (rejectedFiles.length > 0) {
        const errors = rejectedFiles.flatMap((f) => f.errors.map((e) => e.message));
        const uniqueErrors = [...new Set(errors)];
        setValidationError(uniqueErrors.join('. '));
      }

      if (acceptedFiles.length === 0) return;

      const pendingCount = queue.filter((f) => f.status === 'pending').length;
      const available = MAX_FILES - pendingCount;
      const filesToAdd = acceptedFiles.slice(0, available);

      if (acceptedFiles.length > available) {
        setValidationError(`Only ${MAX_FILES} files can be uploaded at once. ${acceptedFiles.length - available} file(s) were not added.`);
      }

      const newItems: QueuedFile[] = filesToAdd.map((file) => {
        const preview = URL.createObjectURL(file);
        objectUrlsRef.current.add(preview);
        return {
          id: `${file.name}-${Date.now()}-${Math.random()}`,
          file,
          preview,
          status: 'pending' as FileStatus,
        };
      });

      setQueue((prev) => [...prev, ...newItems]);
    },
    [queue]
  );

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    accept: ACCEPTED_TYPES,
    maxSize: MAX_FILE_SIZE,
    onDrop,
    disabled: isUploading,
    multiple: true,
  });

  const handleUploadAll = useCallback(async () => {
    const pendingFiles = queue.filter((f) => f.status === 'pending');
    if (pendingFiles.length === 0) return;

    setIsUploading(true);

    for (const item of pendingFiles) {
      // Mark as uploading
      setQueue((prev) =>
        prev.map((f) => (f.id === item.id ? { ...f, status: 'uploading' as FileStatus } : f))
      );

      try {
        await uploadMutation.mutateAsync({
          eventId,
          file: item.file,
        });

        // Mark as done
        setQueue((prev) =>
          prev.map((f) => (f.id === item.id ? { ...f, status: 'done' as FileStatus } : f))
        );
      } catch (error) {
        const errorMsg = getErrorMessage(error);
        setQueue((prev) =>
          prev.map((f) =>
            f.id === item.id ? { ...f, status: 'failed' as FileStatus, error: errorMsg } : f
          )
        );
      }
    }

    // Batch invalidation after all uploads complete
    queryClient.invalidateQueries({ queryKey: albumKeys.photos(eventId) });
    queryClient.invalidateQueries({ queryKey: albumKeys.detail(eventId) });

    setIsUploading(false);
    onUploadComplete?.();

    // Auto-clear completed files after a short delay
    setTimeout(() => {
      setQueue((prev) => {
        const remaining = prev.filter((f) => f.status === 'failed');
        // Revoke URLs of completed files
        prev
          .filter((f) => f.status === 'done')
          .forEach((f) => {
            URL.revokeObjectURL(f.preview);
            objectUrlsRef.current.delete(f.preview);
          });
        return remaining;
      });
    }, 2000);
  }, [queue, eventId, uploadMutation, queryClient, onUploadComplete]);

  const pendingCount = queue.filter((f) => f.status === 'pending').length;
  const doneCount = queue.filter((f) => f.status === 'done').length;
  const failedCount = queue.filter((f) => f.status === 'failed').length;

  return (
    <div className="space-y-4">
      {/* Dropzone Area */}
      <div
        {...getRootProps()}
        className={cn(
          'border-2 border-dashed rounded-lg p-6 text-center transition-colors cursor-pointer',
          isDragActive
            ? 'border-blue-500 bg-blue-50 dark:bg-blue-950/20'
            : 'border-gray-300 dark:border-gray-700 hover:border-gray-400 dark:hover:border-gray-600',
          isUploading && 'opacity-50 cursor-not-allowed',
          'focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2'
        )}
      >
        <input {...getInputProps()} aria-label="Upload photos" />
        <div className="flex flex-col items-center justify-center space-y-2">
          <Upload className="w-8 h-8 text-gray-400" />
          <div className="space-y-1">
            <p className="text-sm font-medium text-gray-700 dark:text-gray-300">
              {isDragActive
                ? 'Drop your photos here'
                : 'Drag & drop photos here, or click to select'}
            </p>
            <p className="text-xs text-gray-500 dark:text-gray-400">
              JPG, PNG, GIF, or WebP (max 10 MB each, up to {MAX_FILES} at a time)
            </p>
          </div>
        </div>
      </div>

      {/* Validation Error */}
      {validationError && (
        <div className="flex items-start gap-2 p-3 bg-red-50 dark:bg-red-950/20 border border-red-200 dark:border-red-800 rounded-lg">
          <AlertCircle className="w-4 h-4 text-red-600 dark:text-red-400 flex-shrink-0 mt-0.5" />
          <p className="text-sm text-red-600 dark:text-red-400">{validationError}</p>
        </div>
      )}

      {/* File Queue */}
      {queue.length > 0 && (
        <div className="space-y-3">
          {/* Queue Header */}
          <div className="flex items-center justify-between">
            <p className="text-sm font-medium text-gray-700 dark:text-gray-300">
              {isUploading
                ? `Uploading... (${doneCount}/${queue.length})`
                : `${pendingCount} file${pendingCount !== 1 ? 's' : ''} selected`}
            </p>
            {!isUploading && (
              <button
                type="button"
                onClick={clearQueue}
                className="text-xs text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200"
              >
                Clear all
              </button>
            )}
          </div>

          {/* File List */}
          <div className="space-y-2">
            {queue.map((item) => (
              <div
                key={item.id}
                className={cn(
                  'flex items-center gap-3 p-2 rounded-lg border',
                  item.status === 'done' && 'bg-green-50 border-green-200 dark:bg-green-950/20 dark:border-green-800',
                  item.status === 'failed' && 'bg-red-50 border-red-200 dark:bg-red-950/20 dark:border-red-800',
                  item.status === 'uploading' && 'bg-blue-50 border-blue-200 dark:bg-blue-950/20 dark:border-blue-800',
                  item.status === 'pending' && 'bg-white border-gray-200 dark:bg-neutral-900 dark:border-gray-700',
                )}
              >
                {/* Thumbnail */}
                <div className="w-10 h-10 rounded overflow-hidden bg-neutral-100 dark:bg-neutral-800 flex-shrink-0">
                  <img
                    src={item.preview}
                    alt={item.file.name}
                    className="w-full h-full object-cover"
                  />
                </div>

                {/* File Info */}
                <div className="flex-1 min-w-0">
                  <p className="text-sm text-gray-700 dark:text-gray-300 truncate">
                    {item.file.name}
                  </p>
                  <p className="text-xs text-gray-500 dark:text-gray-400">
                    {(item.file.size / (1024 * 1024)).toFixed(1)} MB
                    {item.error && (
                      <span className="text-red-600 dark:text-red-400 ml-2">{item.error}</span>
                    )}
                  </p>
                </div>

                {/* Status Icon */}
                <div className="flex-shrink-0">
                  {item.status === 'uploading' && (
                    <Loader2 className="w-5 h-5 text-blue-500 animate-spin" />
                  )}
                  {item.status === 'done' && (
                    <CheckCircle2 className="w-5 h-5 text-green-500" />
                  )}
                  {item.status === 'failed' && (
                    <AlertCircle className="w-5 h-5 text-red-500" />
                  )}
                  {item.status === 'pending' && !isUploading && (
                    <button
                      type="button"
                      onClick={() => removeFile(item.id)}
                      className="p-1 text-gray-400 hover:text-gray-600 dark:hover:text-gray-200"
                      aria-label={`Remove ${item.file.name}`}
                    >
                      <X className="w-4 h-4" />
                    </button>
                  )}
                  {item.status === 'pending' && isUploading && (
                    <ImageIcon className="w-5 h-5 text-gray-400" />
                  )}
                </div>
              </div>
            ))}
          </div>

          {/* Upload Button */}
          {pendingCount > 0 && (
            <Button
              onClick={handleUploadAll}
              disabled={isUploading}
              className="w-full"
            >
              {isUploading ? (
                <>
                  <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                  Uploading {doneCount + 1} of {queue.length}...
                </>
              ) : (
                <>
                  <Upload className="w-4 h-4 mr-2" />
                  Upload {pendingCount} Photo{pendingCount !== 1 ? 's' : ''}
                </>
              )}
            </Button>
          )}

          {/* Summary after completion */}
          {!isUploading && doneCount > 0 && pendingCount === 0 && (
            <p className="text-sm text-green-600 dark:text-green-400 text-center">
              {doneCount} photo{doneCount !== 1 ? 's' : ''} uploaded successfully
              {failedCount > 0 && ` (${failedCount} failed)`}
            </p>
          )}
        </div>
      )}
    </div>
  );
}

export default AlbumPhotoUploader;
