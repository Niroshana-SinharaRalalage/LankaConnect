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
 * Phase 6A.30: Multi-Location Preview Enhancement
 * Shows how a badge will appear across all three event display locations
 *
 * Features:
 * - Three side-by-side preview cards (Events Listing, Home Featured, Event Detail)
 * - Position selector to preview all 4 positions
 * - Real-time preview updates across all locations
 * - Dynamic badge sizing based on container dimensions
 * - Responsive layout (stacks on mobile)
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
      <h4 className="text-sm font-medium text-gray-700">Preview Across Event Locations</h4>

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

      {/* Three preview cards in responsive grid - Phase 6A.30 */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {/* Preview 1: Events Listing (192px height, 50px badge) */}
        <div className="space-y-2">
          <label className="text-xs font-medium text-gray-600">Events Listing</label>
          <div className="relative w-full aspect-[4/3] rounded-lg overflow-hidden shadow-md border border-gray-200">
            <Image
              src="/images/sri-lankan-background.jpg"
              alt="Events Listing Preview"
              fill
              className="object-cover"
              unoptimized
            />
            <div className="absolute z-10 p-2" style={positionStyles}>
              <Image
                src={badge.imageUrl}
                alt={badge.name || 'Badge Preview'}
                width={50}
                height={50}
                className="object-contain drop-shadow-lg"
                unoptimized
              />
            </div>
            <div className="absolute bottom-0 left-0 right-0 bg-gradient-to-t from-black/80 via-black/40 to-transparent p-2">
              <h5 className="text-white font-semibold text-xs truncate">
                Sample Cultural Event
              </h5>
              <p className="text-white/80 text-[10px] mt-0.5">
                Dec 25 • Cleveland, OH
              </p>
            </div>
          </div>
          <p className="text-[10px] text-gray-400 text-center">192×144px • 50px badge</p>
        </div>

        {/* Preview 2: Home Featured Banner (160px height, 42px badge) */}
        <div className="space-y-2">
          <label className="text-xs font-medium text-gray-600">Home Featured</label>
          <div className="relative w-full aspect-[4/3] rounded-lg overflow-hidden shadow-md border border-gray-200">
            <Image
              src="/images/sri-lankan-background.jpg"
              alt="Home Featured Preview"
              fill
              className="object-cover"
              unoptimized
            />
            <div className="absolute z-10 p-2" style={positionStyles}>
              <Image
                src={badge.imageUrl}
                alt={badge.name || 'Badge Preview'}
                width={42}
                height={42}
                className="object-contain drop-shadow-lg"
                unoptimized
              />
            </div>
            <div className="absolute bottom-0 left-0 right-0 bg-gradient-to-t from-black/80 via-black/40 to-transparent p-2">
              <h5 className="text-white font-semibold text-xs truncate">
                Sample Cultural Event
              </h5>
              <p className="text-white/80 text-[10px] mt-0.5">
                Dec 25 • Cleveland, OH
              </p>
            </div>
          </div>
          <p className="text-[10px] text-gray-400 text-center">160×120px • 42px badge</p>
        </div>

        {/* Preview 3: Event Detail Hero (384px height, 80px badge) */}
        <div className="space-y-2">
          <label className="text-xs font-medium text-gray-600">Event Detail Hero</label>
          <div className="relative w-full aspect-[4/3] rounded-lg overflow-hidden shadow-md border border-gray-200">
            <Image
              src="/images/sri-lankan-background.jpg"
              alt="Event Detail Preview"
              fill
              className="object-cover"
              unoptimized
            />
            <div className="absolute z-10 p-2" style={positionStyles}>
              <Image
                src={badge.imageUrl}
                alt={badge.name || 'Badge Preview'}
                width={80}
                height={80}
                className="object-contain drop-shadow-lg"
                unoptimized
              />
            </div>
            <div className="absolute bottom-0 left-0 right-0 bg-gradient-to-t from-black/80 via-black/40 to-transparent p-2">
              <h5 className="text-white font-semibold text-xs truncate">
                Sample Cultural Event
              </h5>
              <p className="text-white/80 text-[10px] mt-0.5">
                Dec 25 • Cleveland, OH
              </p>
            </div>
          </div>
          <p className="text-[10px] text-gray-400 text-center">384×288px • 80px badge</p>
        </div>
      </div>

      <p className="text-xs text-gray-400">
        Badge automatically scales to fit each location. Position remains the same across all locations.
      </p>
    </div>
  );
}
