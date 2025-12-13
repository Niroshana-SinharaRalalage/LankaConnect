/**
 * Badge API Types
 * Phase 6A.25: Badge Management System
 * Phase 6A.27: Added expiry date and role-based access
 */

/**
 * Badge position on event images
 */
export enum BadgePosition {
  TopLeft = 0,
  TopRight = 1,
  BottomLeft = 2,
  BottomRight = 3,
}

/**
 * Badge DTO matching backend BadgeDto
 * Phase 6A.27: Added expiresAt, isExpired, createdByUserId, creatorName
 */
export interface BadgeDto {
  id: string;
  name: string;
  imageUrl: string;
  position: BadgePosition;
  isActive: boolean;
  isSystem: boolean;
  displayOrder: number;
  createdAt: string;
  /** Phase 6A.27: Optional expiry date (ISO string, null = never expires) */
  expiresAt: string | null;
  /** Phase 6A.27: Whether the badge has expired */
  isExpired: boolean;
  /** Phase 6A.27: ID of the user who created this badge (null for system badges) */
  createdByUserId: string | null;
  /** Phase 6A.27: Display name of the creator (for Admin view) */
  creatorName: string | null;
}

/**
 * Create badge request DTO
 * Phase 6A.27: Added expiresAt parameter
 */
export interface CreateBadgeDto {
  name: string;
  position: BadgePosition;
  /** Phase 6A.27: Optional expiry date (ISO string) */
  expiresAt?: string;
}

/**
 * Update badge request DTO
 * Phase 6A.27: Added expiresAt and clearExpiry parameters
 */
export interface UpdateBadgeDto {
  name?: string;
  position?: BadgePosition;
  isActive?: boolean;
  displayOrder?: number;
  /** Phase 6A.27: New expiry date (ISO string, or null to not change) */
  expiresAt?: string | null;
  /** Phase 6A.27: Set to true to explicitly clear/remove the expiry date */
  clearExpiry?: boolean;
}

/**
 * Event badge assignment DTO
 */
export interface EventBadgeDto {
  id: string;
  eventId: string;
  badgeId: string;
  badge: BadgeDto;
  assignedAt: string;
  assignedByUserId: string;
}

/**
 * Helper function to get position display name
 */
export function getPositionDisplayName(position: BadgePosition): string {
  switch (position) {
    case BadgePosition.TopLeft:
      return 'Top Left';
    case BadgePosition.TopRight:
      return 'Top Right';
    case BadgePosition.BottomLeft:
      return 'Bottom Left';
    case BadgePosition.BottomRight:
      return 'Bottom Right';
    default:
      return 'Unknown';
  }
}

/**
 * Helper function to get CSS positioning for badge overlay
 */
export function getBadgePositionStyles(position: BadgePosition): {
  top?: string;
  bottom?: string;
  left?: string;
  right?: string;
} {
  switch (position) {
    case BadgePosition.TopLeft:
      return { top: '0', left: '0' };
    case BadgePosition.TopRight:
      return { top: '0', right: '0' };
    case BadgePosition.BottomLeft:
      return { bottom: '0', left: '0' };
    case BadgePosition.BottomRight:
      return { bottom: '0', right: '0' };
    default:
      return { top: '0', right: '0' };
  }
}
