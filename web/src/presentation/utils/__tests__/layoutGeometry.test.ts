import { describe, it, expect } from 'vitest';
import {
  parseRectGeom,
  parseCurveGeom,
  parseRoundTableGeom,
  parseRectTableGeom,
  decorationStyle,
} from '../layoutGeometry';

describe('layoutGeometry parsers', () => {
  describe('parseRectGeom', () => {
    it('returns the rect when fully specified', () => {
      expect(
        parseRectGeom('{"x":10,"y":20,"width":300,"height":200,"rotation":90}'),
      ).toEqual({ x: 10, y: 20, width: 300, height: 200, rotation: 90 });
    });

    it('accepts missing rotation (optional)', () => {
      expect(parseRectGeom('{"x":1,"y":2,"width":3,"height":4}')).toEqual({
        x: 1, y: 2, width: 3, height: 4,
      });
    });

    it.each(['', '{}', 'not-json', null, undefined, '{"x":1}'])(
      'returns null for invalid input %p',
      (raw) => {
        expect(parseRectGeom(raw as string | null | undefined)).toBeNull();
      },
    );

    it('returns null when a required field is non-numeric', () => {
      expect(parseRectGeom('{"x":"ten","y":2,"width":3,"height":4}')).toBeNull();
    });

    it('rejects JSON arrays', () => {
      expect(parseRectGeom('[1,2,3]')).toBeNull();
    });
  });

  describe('parseCurveGeom', () => {
    it('returns the curve when fully specified', () => {
      const raw = '{"centerX":600,"centerY":100,"radius":380,"startAngleDeg":20,"sweepAngleDeg":140,"rowCount":4}';
      expect(parseCurveGeom(raw)).toEqual({
        centerX: 600, centerY: 100, radius: 380,
        startAngleDeg: 20, sweepAngleDeg: 140, rowCount: 4,
      });
    });

    it('returns null when missing a required angle', () => {
      expect(
        parseCurveGeom('{"centerX":0,"centerY":0,"radius":1,"startAngleDeg":0}'),
      ).toBeNull();
    });
  });

  describe('parseRoundTableGeom', () => {
    it('returns the round-table geometry', () => {
      expect(parseRoundTableGeom('{"centerX":140,"centerY":170,"radius":55}')).toEqual({
        centerX: 140, centerY: 170, radius: 55,
      });
    });

    it('returns null on missing radius', () => {
      expect(parseRoundTableGeom('{"centerX":1,"centerY":2}')).toBeNull();
    });
  });

  describe('parseRectTableGeom', () => {
    it('returns the rect-table geometry', () => {
      const raw = '{"centerX":600,"centerY":120,"width":500,"height":60,"rotation":0}';
      expect(parseRectTableGeom(raw)).toEqual({
        centerX: 600, centerY: 120, width: 500, height: 60, rotation: 0,
      });
    });

    it('returns null on missing width', () => {
      expect(parseRectTableGeom('{"centerX":1,"centerY":2,"height":3}')).toBeNull();
    });
  });

  describe('decorationStyle', () => {
    it('returns a stage palette for Stage with "STAGE" label', () => {
      const s = decorationStyle('Stage');
      expect(s.label).toBe('STAGE');
      expect(s.fill).toBe('#1f2937');
    });

    it('returns a dance-floor palette with "Dance Floor" label', () => {
      expect(decorationStyle('DanceFloor').label).toBe('Dance Floor');
    });

    it('returns null label for Aisle / Wall / Text (no in-shape caption)', () => {
      expect(decorationStyle('Aisle').label).toBeNull();
      expect(decorationStyle('Wall').label).toBeNull();
      expect(decorationStyle('Text').label).toBeNull();
    });

    it('falls back to a neutral palette for unknown kinds', () => {
      const s = decorationStyle('NotARealKind');
      expect(s.label).toBeNull();
      expect(s.fill).toBe('#f3f4f6');
    });
  });
});
