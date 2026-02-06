/**
 * Badge API Types
 * Phase 6A.25: Badge Management System
 * Phase 6A.27: Added expiry date and role-based access
 * Phase 6A.28: Changed to duration-based expiration model
 * Phase 6A.31a: Added per-location badge positioning with percentage-based coords
 * Phase 6A.32: Interactive badge positioning UI
 */

/**
 * Badge position on event images (DEPRECATED - Use BadgeLocationConfigDto instead)
 * @deprecated Use BadgeLocationConfigDto for percentage-based positioning
 */
export enum BadgePosition {
  TopLeft = 0,
  TopRight = 1,
  BottomLeft = 2,
  BottomRight = 3,
}

/**
 * Phase 6A.31a: Badge location configuration for a specific display location
 * Stores position, size, and rotation as percentage/degree values for responsive scaling
 */
export interface BadgeLocationConfigDto {
  /** Horizontal position as ratio (0.0 = left edge, 1.0 = right edge) */
  positionX: number;
  /** Vertical position as ratio (0.0 = top edge, 1.0 = bottom edge) */
  positionY: number;
  /** Badge width as ratio of container width (0.05-1.0 = 5%-100%) */
  sizeWidth: number;
  /** Badge height as ratio of container height (0.05-1.0 = 5%-100%) */
  sizeHeight: number;
  /** Badge rotation in degrees (0-360) */
  rotation: number;
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
 * Phase 6A.31a: Added per-location configuration (listingConfig, featuredConfig, detailConfig)
 */
export interface BadgeDto {
  id: string;
  name: string;
  imageUrl: string;
  /** @deprecated Use listingConfig, featuredConfig, detailConfig instead */
  position: BadgePosition;
  /** Phase 6A.31a: Badge configuration for Events Listing page (192×144px containers) */
  listingConfig: BadgeLocationConfigDto;
  /** Phase 6A.31a: Badge configuration for Home Featured Banner (160×120px containers) */
  featuredConfig: BadgeLocationConfigDto;
  /** Phase 6A.31a: Badge configuration for Event Detail Hero (384×288px containers) */
  detailConfig: BadgeLocationConfigDto;
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
 * Phase 6A.31a: Added per-location configuration updates
 */
export interface UpdateBadgeDto {
  name?: string;
  /** @deprecated Use listingConfig, featuredConfig, detailConfig instead */
  position?: BadgePosition;
  /** Phase 6A.31a: Update badge configuration for Events Listing page */
  listingConfig?: BadgeLocationConfigDto;
  /** Phase 6A.31a: Update badge configuration for Home Featured Banner */
  featuredConfig?: BadgeLocationConfigDto;
  /** Phase 6A.31a: Update badge configuration for Event Detail Hero */
  detailConfig?: BadgeLocationConfigDto;
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
 * @deprecated Use getBadgeLocationStyles with BadgeLocationConfigDto instead
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

/**
 * Phase 6A.32: Display location types for badge positioning
 */
export type BadgeDisplayLocation = 'listing' | 'featured' | 'detail';

/**
 * Phase 6A.32: Container sizes for different display locations
 * These are the standard container dimensions for each location
 */
export const BADGE_CONTAINER_SIZES = {
  listing: { width: 192, height: 144 },   // Events Listing page
  featured: { width: 160, height: 120 },  // Home Featured Banner
  detail: { width: 384, height: 288 },    // Event Detail Hero
} as const;

/**
 * Phase 6A.32: Validation constraints for badge location config
 */
export const BADGE_LOCATION_CONSTRAINTS = {
  positionX: { min: 0, max: 1 },        // 0% to 100%
  positionY: { min: 0, max: 1 },        // 0% to 100%
  sizeWidth: { min: 0.05, max: 1 },     // 5% to 100%
  sizeHeight: { min: 0.05, max: 1 },    // 5% to 100%
  rotation: { min: 0, max: 360 },       // 0° to 360°
} as const;

/**
 * Phase 6A.32: Get CSS styles for badge based on location config
 */
export function getBadgeLocationStyles(
  config: BadgeLocationConfigDto,
  containerSize: { width: number; height: number }
): React.CSSProperties {
  return {
    position: 'absolute',
    left: `${config.positionX * 100}%`,
    top: `${config.positionY * 100}%`,
    width: `${config.sizeWidth * 100}%`,
    height: `${config.sizeHeight * 100}%`,
    transform: `rotate(${config.rotation}deg)`,
    transformOrigin: 'center',
  };
}
