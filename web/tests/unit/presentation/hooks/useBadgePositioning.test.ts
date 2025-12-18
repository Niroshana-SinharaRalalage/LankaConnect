/**
 * Phase 6A.32: Unit tests for useBadgePositioning hook
 * Tests coordinate conversion and validation logic
 */

import { renderHook } from '@testing-library/react';
import { useBadgePositioning, roundToDecimal } from '../../../../src/presentation/hooks/useBadgePositioning';
import { BadgeLocationConfigDto } from '../../../../src/infrastructure/api/types/badges.types';

describe('useBadgePositioning', () => {
  describe('pxToRatio', () => {
    it('should convert pixels to ratio correctly', () => {
      const { result } = renderHook(() => useBadgePositioning());

      expect(result.current.pxToRatio(96, 192)).toBe(0.5);
      expect(result.current.pxToRatio(0, 192)).toBe(0);
      expect(result.current.pxToRatio(192, 192)).toBe(1);
    });

    it('should clamp out-of-range values to 0-1', () => {
      const { result } = renderHook(() => useBadgePositioning());

      expect(result.current.pxToRatio(-10, 192)).toBe(0);
      expect(result.current.pxToRatio(300, 192)).toBe(1);
    });

    it('should handle zero container size safely', () => {
      const { result } = renderHook(() => useBadgePositioning());

      expect(result.current.pxToRatio(100, 0)).toBe(0);
    });
  });

  describe('ratioToPx', () => {
    it('should convert ratio to pixels correctly', () => {
      const { result } = renderHook(() => useBadgePositioning());

      expect(result.current.ratioToPx(0.5, 192)).toBe(96);
      expect(result.current.ratioToPx(0, 192)).toBe(0);
      expect(result.current.ratioToPx(1, 192)).toBe(192);
    });

    it('should handle different container sizes', () => {
      const { result } = renderHook(() => useBadgePositioning());

      expect(result.current.ratioToPx(0.5, 384)).toBe(192);
      expect(result.current.ratioToPx(0.25, 160)).toBe(40);
    });
  });

  describe('toLocationConfig', () => {
    it('should convert position and size to location config', () => {
      const { result } = renderHook(() => useBadgePositioning());

      const config = result.current.toLocationConfig(
        { x: 96, y: 72 },
        { width: 50, height: 38 },
        45,
        { width: 192, height: 144 }
      );

      expect(config.positionX).toBeCloseTo(0.5);
      expect(config.positionY).toBeCloseTo(0.5);
      expect(config.sizeWidth).toBeCloseTo(0.26, 2);
      expect(config.sizeHeight).toBeCloseTo(0.264, 2);
      expect(config.rotation).toBe(45);
    });

    it('should clamp size to minimum 5%', () => {
      const { result } = renderHook(() => useBadgePositioning());

      const config = result.current.toLocationConfig(
        { x: 0, y: 0 },
        { width: 5, height: 5 },  // Less than 5% of 192px
        0,
        { width: 192, height: 144 }
      );

      expect(config.sizeWidth).toBe(0.05);  // Clamped to minimum
      expect(config.sizeHeight).toBe(0.05);
    });

    it('should clamp position to 0-1 range', () => {
      const { result } = renderHook(() => useBadgePositioning());

      const config = result.current.toLocationConfig(
        { x: -50, y: 500 },  // Out of bounds
        { width: 50, height: 38 },
        0,
        { width: 192, height: 144 }
      );

      expect(config.positionX).toBe(0);  // Clamped to 0
      expect(config.positionY).toBe(1);  // Clamped to 1
    });

    it('should clamp rotation to 0-360 range', () => {
      const { result } = renderHook(() => useBadgePositioning());

      const config = result.current.toLocationConfig(
        { x: 0, y: 0 },
        { width: 50, height: 38 },
        400,  // Greater than 360
        { width: 192, height: 144 }
      );

      expect(config.rotation).toBe(360);  // Clamped to max
    });
  });

  describe('fromLocationConfig', () => {
    it('should convert location config to position and size', () => {
      const { result } = renderHook(() => useBadgePositioning());

      const config: BadgeLocationConfigDto = {
        positionX: 0.5,
        positionY: 0.5,
        sizeWidth: 0.26,
        sizeHeight: 0.26,
        rotation: 45,
      };

      const { position, size, rotation } = result.current.fromLocationConfig(
        config,
        { width: 192, height: 144 }
      );

      expect(position.x).toBe(96);
      expect(position.y).toBe(72);
      expect(size.width).toBeCloseTo(49.92);
      expect(size.height).toBeCloseTo(37.44);
      expect(rotation).toBe(45);
    });

    it('should handle different container sizes correctly', () => {
      const { result } = renderHook(() => useBadgePositioning());

      const config: BadgeLocationConfigDto = {
        positionX: 1.0,
        positionY: 0.0,
        sizeWidth: 0.15,
        sizeHeight: 0.15,
        rotation: 0,
      };

      // Events Listing: 192×144px
      const listing = result.current.fromLocationConfig(
        config,
        { width: 192, height: 144 }
      );
      expect(listing.size.width).toBeCloseTo(28.8);
      expect(listing.size.height).toBeCloseTo(21.6);

      // Home Featured: 160×120px
      const featured = result.current.fromLocationConfig(
        config,
        { width: 160, height: 120 }
      );
      expect(featured.size.width).toBe(24);
      expect(featured.size.height).toBe(18);

      // Event Detail: 384×288px
      const detail = result.current.fromLocationConfig(
        config,
        { width: 384, height: 288 }
      );
      expect(detail.size.width).toBeCloseTo(57.6);
      expect(detail.size.height).toBeCloseTo(43.2);
    });
  });

  describe('validateConfig', () => {
    it('should accept valid config unchanged', () => {
      const { result } = renderHook(() => useBadgePositioning());

      const validConfig: BadgeLocationConfigDto = {
        positionX: 0.5,
        positionY: 0.3,
        sizeWidth: 0.26,
        sizeHeight: 0.26,
        rotation: 45,
      };

      const validated = result.current.validateConfig(validConfig);

      expect(validated).toEqual(validConfig);
    });

    it('should clamp position values to 0-1', () => {
      const { result } = renderHook(() => useBadgePositioning());

      const invalidConfig: BadgeLocationConfigDto = {
        positionX: -0.5,
        positionY: 1.5,
        sizeWidth: 0.26,
        sizeHeight: 0.26,
        rotation: 0,
      };

      const validated = result.current.validateConfig(invalidConfig);

      expect(validated.positionX).toBe(0);
      expect(validated.positionY).toBe(1);
    });

    it('should clamp size values to 0.05-1 range', () => {
      const { result } = renderHook(() => useBadgePositioning());

      const invalidConfig: BadgeLocationConfigDto = {
        positionX: 0.5,
        positionY: 0.5,
        sizeWidth: 0.01,   // Less than minimum 5%
        sizeHeight: 1.5,   // Greater than maximum 100%
        rotation: 0,
      };

      const validated = result.current.validateConfig(invalidConfig);

      expect(validated.sizeWidth).toBe(0.05);   // Clamped to minimum
      expect(validated.sizeHeight).toBe(1);     // Clamped to maximum
    });

    it('should clamp rotation to 0-360', () => {
      const { result } = renderHook(() => useBadgePositioning());

      const invalidConfig: BadgeLocationConfigDto = {
        positionX: 0.5,
        positionY: 0.5,
        sizeWidth: 0.26,
        sizeHeight: 0.26,
        rotation: -45,  // Negative rotation
      };

      const validated = result.current.validateConfig(invalidConfig);

      expect(validated.rotation).toBe(0);  // Clamped to minimum
    });
  });

  describe('roundToDecimal', () => {
    it('should round to 3 decimal places by default', () => {
      expect(roundToDecimal(0.33333333)).toBe(0.333);
      expect(roundToDecimal(0.66666666)).toBe(0.667);
      expect(roundToDecimal(0.12345678)).toBe(0.123);
    });

    it('should round to specified decimal places', () => {
      expect(roundToDecimal(0.33333333, 2)).toBe(0.33);
      expect(roundToDecimal(0.66666666, 4)).toBe(0.6667);
      expect(roundToDecimal(0.12345678, 1)).toBe(0.1);
    });

    it('should handle whole numbers', () => {
      expect(roundToDecimal(1)).toBe(1);
      expect(roundToDecimal(0)).toBe(0);
    });
  });
});
