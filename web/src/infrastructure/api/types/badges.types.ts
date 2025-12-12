/**
 * Badge API Types
 * Phase 6A.25: Badge Management System
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
}

/**
 * Create badge request DTO
 */
export interface CreateBadgeDto {
  name: string;
  position: BadgePosition;
}

/**
 * Update badge request DTO
 */
export interface UpdateBadgeDto {
  name?: string;
  position?: BadgePosition;
  isActive?: boolean;
  displayOrder?: number;
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
