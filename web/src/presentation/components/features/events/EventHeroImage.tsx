'use client';

import React from 'react';
import { Badge } from '@/presentation/components/ui/Badge';
import { cn } from '@/presentation/lib/utils';
import type { EventImageDto } from '@/infrastructure/api/types/events.types';

export type EventHeroVariant = 'contained' | 'fullWidth';

export interface EventHeroImageProps {
  images: readonly EventImageDto[] | undefined;
  title: string;
  categoryLabel: string;
  variant: EventHeroVariant;
}

/**
 * Hero image renderer for the public event details page.
 *
 * Two layout variants:
 *   - "contained"  → Option C. Lives inside the existing max-w-7xl Card column.
 *                    Responsive aspect ratio (16:9 mobile, 3:1 desktop), object-contain
 *                    so the full image is always visible (no cropping).
 *   - "fullWidth"  → Option E. Designed to render outside the max-w-7xl wrapper so the
 *                    hero spans the full viewport width. Same aspect ratio + contain
 *                    behaviour; just stretches edge-to-edge on desktop.
 *
 * Both variants letterbox onto the LankaConnect orange→rose gradient when the uploaded
 * image's aspect ratio differs from the hero. This matches the existing dashboard
 * thumbnail precedent (Phase 6A.67) and the badge / lightbox letterbox pattern.
 *
 * Returns null when no images are available so the parent can render its own placeholder.
 */
export const EventHeroImage: React.FC<EventHeroImageProps> = ({
  images,
  title,
  categoryLabel,
  variant,
}) => {
  if (!images || images.length === 0) {
    return null;
  }

  const primary = images.find((img) => img.isPrimary) ?? images[0];

  return (
    <div
      className={cn(
        'relative bg-gradient-to-br from-orange-500 to-rose-500',
        'aspect-[16/9] md:aspect-[3/1]',
        variant === 'fullWidth' && 'w-full',
      )}
      data-hero-variant={variant}
    >
      <img
        src={primary.imageUrl}
        alt={title}
        className="w-full h-full object-contain"
        loading="eager"
      />

      {categoryLabel && (
        <div className="absolute top-4 right-4 md:top-6 md:right-6">
          <Badge
            variant="default"
            className="text-white shadow-lg text-base px-4 py-2"
            style={{ background: '#8B1538' }}
          >
            {categoryLabel}
          </Badge>
        </div>
      )}
    </div>
  );
};

export default EventHeroImage;
