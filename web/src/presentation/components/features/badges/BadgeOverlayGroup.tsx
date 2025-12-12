'use client';

import Image from 'next/image';
import { BadgeDto, BadgePosition, getBadgePositionStyles } from '@/infrastructure/api/types/badges.types';

interface BadgeOverlayGroupProps {
  badges: BadgeDto[];
  size?: number;
  maxBadges?: number;
  className?: string;
}

/**
 * Phase 6A.25: Badge Overlay Group Component
 * Displays badges overlaid on event cards in their designated positions
 * Badges are positioned absolutely within a relative parent container
 */
export function BadgeOverlayGroup({
  badges,
  size = 60,
  maxBadges = 2,
  className = ''
}: BadgeOverlayGroupProps) {
  if (!badges || badges.length === 0) {
    return null;
  }

  // Filter to only active badges and limit to maxBadges
  const activeBadges = badges
    .filter(badge => badge.isActive)
    .slice(0, maxBadges);

  if (activeBadges.length === 0) {
    return null;
  }

  // Group badges by position to avoid overlaps
  const badgesByPosition = activeBadges.reduce((acc, badge) => {
    const position = badge.position;
    if (!acc[position]) {
      acc[position] = [];
    }
    acc[position].push(badge);
    return acc;
  }, {} as Record<BadgePosition, BadgeDto[]>);

  return (
    <div className={`absolute inset-0 pointer-events-none ${className}`}>
      {Object.entries(badgesByPosition).map(([positionStr, positionBadges]) => {
        const position = parseInt(positionStr) as BadgePosition;
        const positionStyles = getBadgePositionStyles(position);

        return positionBadges.map((badge, index) => (
          <div
            key={badge.id}
            className="absolute z-10"
            style={{
              ...positionStyles,
              // Offset multiple badges at same position
              transform: index > 0
                ? `translate(${index * 10}px, ${index * 10}px)`
                : undefined,
            }}
          >
            <Image
              src={badge.imageUrl}
              alt={badge.name}
              width={size}
              height={size}
              className="object-contain drop-shadow-md"
              unoptimized // Allow external URLs from Azure Blob Storage
            />
          </div>
        ));
      })}
    </div>
  );
}
