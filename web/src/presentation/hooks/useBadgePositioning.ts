/**
 * Phase 6A.32: Custom hook for badge positioning logic
 * Provides utilities for converting between pixel coordinates and percentage ratios
 * Handles validation and constraints for badge positioning
 */

import { useCallback } from 'react';
import {
  BadgeLocationConfigDto,
  BADGE_LOCATION_CONSTRAINTS,
} from '../../infrastructure/api/types/badges.types';

/**
 * Coordinate conversion utilities for badge positioning
 */
export interface BadgePositioningUtils {
  /** Convert pixels to ratio (0-1) */
  pxToRatio: (px: number, containerSize: number) => number;
  /** Convert ratio to pixels */
  ratioToPx: (ratio: number, containerSize: number) => number;
  /** Convert react-rnd position/size to BadgeLocationConfigDto */
  toLocationConfig: (
    position: { x: number; y: number },
    size: { width: number; height: number },
    rotation: number,
    containerSize: { width: number; height: number }
  ) => BadgeLocationConfigDto;
  /** Convert BadgeLocationConfigDto to react-rnd position/size */
  fromLocationConfig: (
    config: BadgeLocationConfigDto,
    containerSize: { width: number; height: number }
  ) => {
    position: { x: number; y: number };
    size: { width: number; height: number };
    rotation: number;
  };
  /** Validate and clamp location config values to valid ranges */
  validateConfig: (config: BadgeLocationConfigDto) => BadgeLocationConfigDto;
}

/**
 * Custom hook that provides badge positioning utilities
 * All functions are memoized for performance
 */
export function useBadgePositioning(): BadgePositioningUtils {
  /**
   * Convert pixels to ratio (0-1)
   * Clamps result to valid range
   */
  const pxToRatio = useCallback((px: number, containerSize: number): number => {
    if (containerSize <= 0) return 0;
    const ratio = px / containerSize;
    return Math.max(0, Math.min(1, ratio));
  }, []);

  /**
   * Convert ratio (0-1) to pixels
   */
  const ratioToPx = useCallback((ratio: number, containerSize: number): number => {
    return ratio * containerSize;
  }, []);

  /**
   * Convert react-rnd position and size to BadgeLocationConfigDto
   * Automatically validates and clamps values to constraints
   */
  const toLocationConfig = useCallback(
    (
      position: { x: number; y: number },
      size: { width: number; height: number },
      rotation: number,
      containerSize: { width: number; height: number }
    ): BadgeLocationConfigDto => {
      const config: BadgeLocationConfigDto = {
        positionX: pxToRatio(position.x, containerSize.width),
        positionY: pxToRatio(position.y, containerSize.height),
        sizeWidth: pxToRatio(size.width, containerSize.width),
        sizeHeight: pxToRatio(size.height, containerSize.height),
        rotation: Math.max(
          BADGE_LOCATION_CONSTRAINTS.rotation.min,
          Math.min(BADGE_LOCATION_CONSTRAINTS.rotation.max, rotation)
        ),
      };

      return validateConfig(config);
    },
    [pxToRatio]
  );

  /**
   * Convert BadgeLocationConfigDto to react-rnd position and size
   */
  const fromLocationConfig = useCallback(
    (
      config: BadgeLocationConfigDto,
      containerSize: { width: number; height: number }
    ) => {
      return {
        position: {
          x: ratioToPx(config.positionX, containerSize.width),
          y: ratioToPx(config.positionY, containerSize.height),
        },
        size: {
          width: ratioToPx(config.sizeWidth, containerSize.width),
          height: ratioToPx(config.sizeHeight, containerSize.height),
        },
        rotation: config.rotation,
      };
    },
    [ratioToPx]
  );

  /**
   * Validate and clamp all values in location config to valid ranges
   * Ensures config always meets backend validation constraints
   */
  const validateConfig = useCallback(
    (config: BadgeLocationConfigDto): BadgeLocationConfigDto => {
      return {
        positionX: clamp(
          config.positionX,
          BADGE_LOCATION_CONSTRAINTS.positionX.min,
          BADGE_LOCATION_CONSTRAINTS.positionX.max
        ),
        positionY: clamp(
          config.positionY,
          BADGE_LOCATION_CONSTRAINTS.positionY.min,
          BADGE_LOCATION_CONSTRAINTS.positionY.max
        ),
        sizeWidth: clamp(
          config.sizeWidth,
          BADGE_LOCATION_CONSTRAINTS.sizeWidth.min,
          BADGE_LOCATION_CONSTRAINTS.sizeWidth.max
        ),
        sizeHeight: clamp(
          config.sizeHeight,
          BADGE_LOCATION_CONSTRAINTS.sizeHeight.min,
          BADGE_LOCATION_CONSTRAINTS.sizeHeight.max
        ),
        rotation: clamp(
          config.rotation,
          BADGE_LOCATION_CONSTRAINTS.rotation.min,
          BADGE_LOCATION_CONSTRAINTS.rotation.max
        ),
      };
    },
    []
  );

  return {
    pxToRatio,
    ratioToPx,
    toLocationConfig,
    fromLocationConfig,
    validateConfig,
  };
}

/**
 * Helper function to clamp a value between min and max
 */
function clamp(value: number, min: number, max: number): number {
  return Math.max(min, Math.min(max, value));
}

/**
 * Round number to specified decimal places
 * Used for clean percentage display (e.g., 0.333 instead of 0.33333333)
 */
export function roundToDecimal(value: number, decimals: number = 3): number {
  const multiplier = Math.pow(10, decimals);
  return Math.round(value * multiplier) / multiplier;
}
