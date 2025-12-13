'use client';

import * as React from 'react';
import Image from 'next/image';
import {
  BadgePosition,
  getPositionDisplayName,
  getBadgePositionStyles,
  type BadgeDto,
} from '@/infrastructure/api/types/badges.types';

interface BadgePreviewSectionProps {
  /** Badge data to preview (can use URL.createObjectURL for new file uploads) */
  badge: Partial<BadgeDto> & { imageUrl: string };
  /** Override position for preview (for position selector in dialogs) */
  position?: BadgePosition;
  /** Called when user changes the preview position */
  onPositionChange?: (position: BadgePosition) => void;
  /** Size of the badge in the preview */
  badgeSize?: number;
}

/**
 * Phase 6A.29: Badge Preview Section Component
 * Shows how a badge will appear on an event card
 *
 * Features:
 * - Mock event card with sample image
 * - Position selector to preview all 4 positions
 * - Real-time preview updates
 * - Matches actual event card appearance
 */
export function BadgePreviewSection({
  badge,
  position,
  onPositionChange,
  badgeSize = 50,
}: BadgePreviewSectionProps) {
  const [previewPosition, setPreviewPosition] = React.useState<BadgePosition>(
    position ?? badge.position ?? BadgePosition.TopRight
  );

  // Sync with external position changes
  React.useEffect(() => {
    if (position !== undefined) {
      setPreviewPosition(position);
    }
  }, [position]);

  const handlePositionChange = (newPosition: BadgePosition) => {
    setPreviewPosition(newPosition);
    onPositionChange?.(newPosition);
  };

  const positionStyles = getBadgePositionStyles(previewPosition);

  return (
    <div className="space-y-3">
      <h4 className="text-sm font-medium text-gray-700">Preview on Event Card</h4>

      {/* Position selector buttons */}
      <div className="flex flex-wrap gap-2">
        {Object.entries(BadgePosition)
          .filter(([, value]) => typeof value === 'number')
          .map(([key, value]) => (
            <button
              key={value}
              type="button"
              onClick={() => handlePositionChange(value as BadgePosition)}
              className={`px-3 py-1.5 text-xs rounded-md transition-colors ${
                previewPosition === value
                  ? 'bg-[#FF7900] text-white'
                  : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
              }`}
            >
              {getPositionDisplayName(value as BadgePosition)}
            </button>
          ))}
      </div>

      {/* Mock Event Card Preview */}
      <div className="relative w-full max-w-[280px] aspect-[4/3] rounded-lg overflow-hidden shadow-md border border-gray-200">
        {/* Sample event background image */}
        <Image
          src="/images/sri-lankan-background.jpg"
          alt="Sample Event"
          fill
          className="object-cover"
          unoptimized
        />

        {/* Badge overlay */}
        <div
          className="absolute z-10 p-2"
          style={positionStyles}
        >
          <Image
            src={badge.imageUrl}
            alt={badge.name || 'Badge Preview'}
            width={badgeSize}
            height={badgeSize}
            className="object-contain drop-shadow-lg"
            unoptimized
          />
        </div>

        {/* Mock event card overlay (like actual event cards) */}
        <div className="absolute bottom-0 left-0 right-0 bg-gradient-to-t from-black/80 via-black/40 to-transparent p-3">
          <h5 className="text-white font-semibold text-sm truncate">
            Sample Cultural Event
          </h5>
          <p className="text-white/80 text-xs mt-0.5">
            Dec 25, 2025 • Cleveland, OH
          </p>
        </div>
      </div>

      <p className="text-xs text-gray-400">
        This preview shows how your badge will appear on event cards. The actual position may vary slightly based on card size.
      </p>
    </div>
  );
}
