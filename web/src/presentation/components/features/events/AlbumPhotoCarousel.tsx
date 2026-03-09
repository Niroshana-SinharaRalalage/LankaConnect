'use client';

/**
 * AlbumPhotoCarousel Component
 *
 * Displays a horizontal scrollable row of album photos (4 on desktop, 2 on mobile).
 * Used on the event details page to preview published album photos.
 * Clicking a photo navigates to the full gallery page.
 */

import { useState, useRef, useCallback } from 'react';
import { ChevronLeft, ChevronRight, ImageIcon } from 'lucide-react';
import { useAlbumPhotos } from '@/presentation/hooks/usePhotoAlbum';
import type { AlbumPhotoDto } from '@/infrastructure/api/types/events.types';

export interface AlbumPhotoCarouselProps {
  /** Event ID */
  eventId: string;
  /** Album ID */
  albumId: string;
  /** Click handler for photo — typically navigates to gallery */
  onPhotoClick?: (photo: AlbumPhotoDto) => void;
}

export function AlbumPhotoCarousel({ eventId, albumId, onPhotoClick }: AlbumPhotoCarouselProps) {
  const { data, isLoading } = useAlbumPhotos(eventId, albumId, 12);
  const scrollRef = useRef<HTMLDivElement>(null);
  const [canScrollLeft, setCanScrollLeft] = useState(false);
  const [canScrollRight, setCanScrollRight] = useState(true);

  const photos = data?.pages.flatMap((page) => page.photos) ?? [];

  const updateScrollButtons = useCallback(() => {
    const el = scrollRef.current;
    if (!el) return;
    setCanScrollLeft(el.scrollLeft > 0);
    setCanScrollRight(el.scrollLeft + el.clientWidth < el.scrollWidth - 4);
  }, []);

  const scroll = useCallback(
    (direction: 'left' | 'right') => {
      const el = scrollRef.current;
      if (!el) return;
      const scrollAmount = el.clientWidth * 0.8;
      el.scrollBy({
        left: direction === 'left' ? -scrollAmount : scrollAmount,
        behavior: 'smooth',
      });
      // Update buttons after scroll animation
      setTimeout(updateScrollButtons, 350);
    },
    [updateScrollButtons],
  );

  if (isLoading) {
    return (
      <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
        {Array.from({ length: 4 }).map((_, i) => (
          <div
            key={`skeleton-${i}`}
            className="aspect-square rounded-lg bg-neutral-200 dark:bg-neutral-700 animate-pulse"
          />
        ))}
      </div>
    );
  }

  if (photos.length === 0) {
    return (
      <div className="flex items-center justify-center py-8 text-gray-400">
        <ImageIcon className="w-6 h-6 mr-2" />
        <span className="text-sm">No photos yet</span>
      </div>
    );
  }

  return (
    <div className="relative group">
      {/* Left Arrow */}
      {canScrollLeft && (
        <button
          type="button"
          onClick={() => scroll('left')}
          className="absolute left-0 top-1/2 -translate-y-1/2 z-10 p-1.5 rounded-full bg-white/90 shadow-md border border-gray-200 hover:bg-white transition-opacity opacity-0 group-hover:opacity-100"
          aria-label="Scroll left"
        >
          <ChevronLeft className="w-5 h-5 text-gray-700" />
        </button>
      )}

      {/* Right Arrow */}
      {canScrollRight && photos.length > 4 && (
        <button
          type="button"
          onClick={() => scroll('right')}
          className="absolute right-0 top-1/2 -translate-y-1/2 z-10 p-1.5 rounded-full bg-white/90 shadow-md border border-gray-200 hover:bg-white transition-opacity opacity-0 group-hover:opacity-100"
          aria-label="Scroll right"
        >
          <ChevronRight className="w-5 h-5 text-gray-700" />
        </button>
      )}

      {/* Scrollable Photo Row */}
      <div
        ref={scrollRef}
        onScroll={updateScrollButtons}
        className="flex gap-3 overflow-x-auto scrollbar-hide snap-x snap-mandatory"
        style={{ scrollbarWidth: 'none', msOverflowStyle: 'none' }}
      >
        {photos.map((photo) => (
          <button
            key={photo.id}
            type="button"
            onClick={() => onPhotoClick?.(photo)}
            className="flex-shrink-0 w-[calc(50%-6px)] md:w-[calc(25%-9px)] aspect-square rounded-lg overflow-hidden group/photo"
          >
            <img
              src={photo.thumbnailUrl}
              alt={photo.caption || `Photo by ${photo.uploaderName}`}
              className="w-full h-full object-cover transition-transform duration-200 group-hover/photo:scale-105"
              loading="lazy"
            />
          </button>
        ))}
      </div>
    </div>
  );
}

export default AlbumPhotoCarousel;
