'use client';

/**
 * AlbumPhotoCard Component
 *
 * Individual media card displayed in the album gallery grid.
 * Shows thumbnail with hover overlay containing uploader info, caption, and expiry badge.
 * For videos: shows play icon overlay and duration badge.
 * Supports click-to-view lightbox, delete actions, and selection mode for bulk operations.
 */

import { useMemo } from 'react';
import { Trash2, Clock, Check, Play } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import type { AlbumPhotoDto } from '@/infrastructure/api/types/events.types';

export interface AlbumPhotoCardProps {
  /** The photo/video data to display */
  photo: AlbumPhotoDto;
  /** Whether the current user is an event organizer */
  isOrganizer: boolean;
  /** The current user's ID (for permission checks) */
  currentUserId?: string;
  /** Callback when the item is clicked for viewing */
  onView: (photo: AlbumPhotoDto) => void;
  /** Callback when delete is requested (only shown to uploader or organizer) */
  onDelete?: (photoId: string) => void;
  /** Whether selection mode is active */
  isSelectionMode?: boolean;
  /** Whether this item is currently selected */
  isSelected?: boolean;
  /** Callback when selection checkbox is toggled */
  onToggleSelect?: (photoId: string) => void;
}

/**
 * Computes the expiry display info from an expiry date string.
 * Returns label text and a color class based on remaining time.
 */
function getExpiryInfo(expiresAt: string): { label: string; colorClass: string } {
  const now = new Date();
  const expiry = new Date(expiresAt);
  const diffMs = expiry.getTime() - now.getTime();

  if (diffMs <= 0) {
    return { label: 'Expired', colorClass: 'bg-red-600 text-white' };
  }

  const diffHours = diffMs / (1000 * 60 * 60);
  const diffDays = Math.floor(diffHours / 24);

  if (diffHours < 24) {
    const hours = Math.max(1, Math.floor(diffHours));
    return {
      label: `Expires in ${hours}h`,
      colorClass: 'bg-red-500 text-white',
    };
  }

  if (diffDays <= 3) {
    return {
      label: `Expires in ${diffDays}d`,
      colorClass: 'bg-yellow-500 text-white',
    };
  }

  return {
    label: `Expires in ${diffDays}d`,
    colorClass: 'bg-green-600 text-white',
  };
}

/**
 * Format seconds into mm:ss display
 */
function formatDuration(seconds: number): string {
  const mins = Math.floor(seconds / 60);
  const secs = seconds % 60;
  return `${mins}:${secs.toString().padStart(2, '0')}`;
}

export function AlbumPhotoCard({
  photo,
  isOrganizer,
  currentUserId,
  onView,
  onDelete,
  isSelectionMode = false,
  isSelected = false,
  onToggleSelect,
}: AlbumPhotoCardProps) {
  const expiryInfo = useMemo(() => getExpiryInfo(photo.expiresAt), [photo.expiresAt]);
  const isVideo = photo.mediaType === 'Video';

  const canDelete = isOrganizer || (currentUserId && currentUserId === photo.uploaderId);

  const handleClick = () => {
    if (isSelectionMode && onToggleSelect) {
      onToggleSelect(photo.id);
    } else {
      onView(photo);
    }
  };

  const handleDelete = (e: React.MouseEvent) => {
    e.stopPropagation();
    if (onDelete) {
      onDelete(photo.id);
    }
  };

  return (
    <button
      type="button"
      onClick={handleClick}
      className={`relative aspect-square rounded-lg overflow-hidden bg-neutral-100 dark:bg-neutral-800 group focus:outline-none focus:ring-2 focus:ring-offset-2 transition-all ${
        isSelectionMode && isSelected
          ? 'ring-2 ring-blue-500 ring-offset-2'
          : 'focus:ring-primary'
      }`}
      aria-label={
        isSelectionMode
          ? `${isSelected ? 'Deselect' : 'Select'} ${isVideo ? 'video' : 'photo'} by ${photo.uploaderName}`
          : `View ${isVideo ? 'video' : 'photo'} by ${photo.uploaderName}${photo.caption ? `: ${photo.caption}` : ''}`
      }
    >
      {/* Gallery Image — use medium (800px) for photos, thumbnail for videos */}
      <img
        src={isVideo ? photo.thumbnailUrl : photo.mediumUrl}
        alt={photo.caption || `${isVideo ? 'Video' : 'Photo'} by ${photo.uploaderName}`}
        className="w-full h-full object-cover transition-transform duration-200 group-hover:scale-105"
        loading="lazy"
      />

      {/* Video Play Icon Overlay */}
      {isVideo && (
        <div className="absolute inset-0 flex items-center justify-center pointer-events-none">
          <div className="w-12 h-12 rounded-full bg-black/60 flex items-center justify-center">
            <Play className="w-6 h-6 text-white ml-0.5" fill="white" />
          </div>
        </div>
      )}

      {/* Video Duration Badge */}
      {isVideo && photo.durationSeconds != null && photo.durationSeconds > 0 && (
        <div className="absolute bottom-2 right-2 z-10">
          <span className="text-xs font-medium text-white bg-black/70 px-1.5 py-0.5 rounded">
            {formatDuration(photo.durationSeconds)}
          </span>
        </div>
      )}

      {/* Selection Checkbox (visible in selection mode) */}
      {isSelectionMode && (
        <div className="absolute top-2 left-2 z-20">
          <div
            className={`w-6 h-6 rounded-full border-2 flex items-center justify-center transition-colors ${
              isSelected
                ? 'bg-blue-600 border-blue-600 text-white'
                : 'bg-white/80 border-gray-400'
            }`}
          >
            {isSelected && <Check className="w-4 h-4" />}
          </div>
        </div>
      )}

      {/* Expiry Badge (always visible) */}
      <div className="absolute top-2 right-2 z-10">
        <span
          className={`inline-flex items-center gap-1 text-xs font-medium px-2 py-0.5 rounded-full ${expiryInfo.colorClass}`}
        >
          <Clock className="w-3 h-3" />
          {expiryInfo.label}
        </span>
      </div>

      {/* Hover Overlay */}
      <div className="absolute inset-0 bg-black/0 group-hover:bg-black/50 transition-colors duration-200 flex flex-col justify-end">
        <div className="p-3 translate-y-full group-hover:translate-y-0 transition-transform duration-200">
          {/* Uploader Name */}
          <p className="text-white text-sm font-medium truncate">
            {photo.uploaderName}
          </p>

          {/* Caption */}
          {photo.caption && (
            <p className="text-white/80 text-xs mt-0.5 line-clamp-2">
              {photo.caption}
            </p>
          )}
        </div>
      </div>

      {/* Delete Button (visible on hover to authorized users, hidden in selection mode) */}
      {!isSelectionMode && canDelete && onDelete && (
        <div className="absolute top-2 left-2 opacity-0 group-hover:opacity-100 transition-opacity duration-200 z-10">
          <Button
            variant="destructive"
            size="sm"
            onClick={handleDelete}
            className="h-8 w-8 p-0"
            aria-label={`Delete ${isVideo ? 'video' : 'photo'}`}
          >
            <Trash2 className="w-4 h-4" />
          </Button>
        </div>
      )}
    </button>
  );
}

export default AlbumPhotoCard;
