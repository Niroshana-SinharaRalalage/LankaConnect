/**
 * Badge API Types
 * Phase 6A.25: Badge Management System
 * Phase 6A.27: Added expiry date and role-based access
 * Phase 6A.28: Changed to duration-based expiration model
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
 * Phase 6A.28: Duration preset options for dropdown
 */
export type DurationPreset = 7 | 14 | 30 | 90 | null | 'custom';

/**
 * Phase 6A.28: Duration preset options for UI
 */
export const DURATION_PRESETS = [
  { value: 7, label: '7 days' },
  { value: 14, label: '14 days (2 weeks)' },
  { value: 30, label: '30 days (1 month)' },
  { value: 90, label: '90 days (3 months)' },
  { value: null, label: 'Never expire' },
  { value: 'custom' as const, label: 'Custom days...' },
] as const;

/**
 * Badge DTO matching backend BadgeDto
 * Phase 6A.27: Added expiresAt, isExpired, createdByUserId, creatorName
 * Phase 6A.28: Changed expiresAt/isExpired to defaultDurationDays
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
  /** Phase 6A.28: Default duration in days for badge assignments (null = never expires) */
  defaultDurationDays: number | null;
  /** Phase 6A.27: ID of the user who created this badge (null for system badges) */
  createdByUserId: string | null;
  /** Phase 6A.27: Display name of the creator (for Admin view) */
  creatorName: string | null;
}

/**
 * Create badge request DTO
 * Phase 6A.28: Changed expiresAt to defaultDurationDays
 */
export interface CreateBadgeDto {
  name: string;
  position: BadgePosition;
  /** Phase 6A.28: Default duration in days for badge assignments (null = never expires) */
  defaultDurationDays?: number | null;
}

/**
 * Update badge request DTO
 * Phase 6A.28: Changed expiresAt/clearExpiry to defaultDurationDays/clearDuration
 */
export interface UpdateBadgeDto {
  name?: string;
  position?: BadgePosition;
  isActive?: boolean;
  displayOrder?: number;
  /** Phase 6A.28: Default duration in days (null = no change, set value = update duration) */
  defaultDurationDays?: number | null;
  /** Phase 6A.28: Set to true to explicitly clear/remove the default duration (making badge never expire) */
  clearDuration?: boolean;
}

/**
 * Event badge assignment DTO
 * Phase 6A.28: Added duration and expiration fields
 */
export interface EventBadgeDto {
  id: string;
  eventId: string;
  badgeId: string;
  badge: BadgeDto;
  assignedAt: string;
  assignedByUserId: string;
  /** Phase 6A.28: Duration in days for this specific assignment (may differ from badge default) */
  durationDays: number | null;
  /** Phase 6A.28: Calculated expiration date: assignedAt + durationDays */
  expiresAt: string | null;
  /** Phase 6A.28: Whether this assignment has expired */
  isExpired: boolean;
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
