import { describe, it, expect } from 'vitest';
import {
  parseRectGeom,
  parseCurveGeom,
  parseRoundTableGeom,
  parseRectTableGeom,
  decorationStyle,
  computeRectZoneSeatPositions,
  computeRoundTableSeatPositions,
  computeRectTableSeatPositions,
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

  describe('computeRectZoneSeatPositions', () => {
    const zone = { x: 0, y: 0, width: 1000, height: 400 };

    it('returns an empty array for no seats', () => {
      expect(computeRectZoneSeatPositions([], zone)).toEqual([]);
    });

    it('places a 2×3 grid inside the zone with symmetric spacing', () => {
      const seats = [
        { id: 'a1', row: 'A', number: 1 }, { id: 'a2', row: 'A', number: 2 }, { id: 'a3', row: 'A', number: 3 },
        { id: 'b1', row: 'B', number: 1 }, { id: 'b2', row: 'B', number: 2 }, { id: 'b3', row: 'B', number: 3 },
      ];
      const pts = computeRectZoneSeatPositions(seats, zone);
      expect(pts).toHaveLength(6);
      // Row A should render at a smaller y than row B (rows top-to-bottom).
      const a1 = pts.find((p) => p.seatId === 'a1')!;
      const b1 = pts.find((p) => p.seatId === 'b1')!;
      expect(a1.y).toBeLessThan(b1.y);
      // Seats in a row should share the same y.
      expect(pts.filter((p) => ['a1', 'a2', 'a3'].includes(p.seatId)).every((p) => p.y === a1.y)).toBe(true);
      // Seat radius is positive.
      expect(a1.r).toBeGreaterThan(0);
    });

    it('respects custom starting row labels (sorts alphabetically)', () => {
      const seats = [
        { id: 'k1', row: 'K', number: 1 },
        { id: 'c1', row: 'C', number: 1 },
      ];
      const pts = computeRectZoneSeatPositions(seats, zone);
      const kPt = pts.find((p) => p.seatId === 'k1')!;
      const cPt = pts.find((p) => p.seatId === 'c1')!;
      expect(cPt.y).toBeLessThan(kPt.y);
    });

    it('shifts points by the zone offset', () => {
      const shifted = computeRectZoneSeatPositions(
        [{ id: 's1', row: 'A', number: 1 }],
        { x: 200, y: 140, width: 400, height: 200 },
      );
      expect(shifted[0].x).toBeGreaterThan(200);
      expect(shifted[0].y).toBeGreaterThan(140);
    });
  });

  describe('computeRoundTableSeatPositions', () => {
    const table = { centerX: 100, centerY: 100, radius: 50 };

    it('returns an empty array for no seats', () => {
      expect(computeRoundTableSeatPositions([], table)).toEqual([]);
    });

    it('distributes 8 seats evenly around the circumference when no angleDeg', () => {
      const seats = Array.from({ length: 8 }, (_, i) => ({ id: `s${i}`, number: i + 1 }));
      const pts = computeRoundTableSeatPositions(seats, table);
      expect(pts).toHaveLength(8);
      // First seat sits at angle 0° (east / 3 o'clock).
      expect(pts[0].x).toBeGreaterThan(100);
      expect(Math.round(pts[0].y)).toBe(100);
      // All points sit on a ring outside the table radius.
      for (const p of pts) {
        const dx = p.x - 100;
        const dy = p.y - 100;
        const dist = Math.sqrt(dx * dx + dy * dy);
        expect(dist).toBeGreaterThan(table.radius);
      }
    });

    it('honors per-seat angleDeg when provided', () => {
      const seats = [
        { id: 'top', number: 1, angleDeg: 270 },
        { id: 'right', number: 2, angleDeg: 0 },
      ];
      const pts = computeRoundTableSeatPositions(seats, table);
      const top = pts.find((p) => p.seatId === 'top')!;
      const right = pts.find((p) => p.seatId === 'right')!;
      // 270° is "north" in SVG/canvas conventions — smaller y than center.
      expect(top.y).toBeLessThan(100);
      // 0° is "east" — larger x than center.
      expect(right.x).toBeGreaterThan(100);
    });
  });

  describe('computeRectTableSeatPositions', () => {
    const table = { centerX: 100, centerY: 50, width: 80, height: 40 };

    it('returns an empty array for no seats', () => {
      expect(computeRectTableSeatPositions([], table)).toEqual([]);
    });

    it('distributes 8 seats two-per-side around a square-proportion table', () => {
      const seats = Array.from({ length: 8 }, (_, i) => ({ id: `s${i}`, number: i + 1 }));
      const pts = computeRectTableSeatPositions(seats, table);
      expect(pts).toHaveLength(8);

      const halfH = table.height / 2;
      const halfW = table.width / 2;
      // Top-side seats sit above the table; bottom-side below.
      const aboveTable = pts.filter((p) => p.y < table.centerY - halfH);
      const belowTable = pts.filter((p) => p.y > table.centerY + halfH);
      const leftOfTable = pts.filter((p) => p.x < table.centerX - halfW);
      const rightOfTable = pts.filter((p) => p.x > table.centerX + halfW);
      expect(aboveTable.length).toBe(2);
      expect(belowTable.length).toBe(2);
      expect(leftOfTable.length).toBe(2);
      expect(rightOfTable.length).toBe(2);
    });

    it('puts the extra seats on the long sides first (remainder > 0)', () => {
      // 5 seats: 2 on top, 1 on right, 1 on bottom, 1 on left.
      const seats = Array.from({ length: 5 }, (_, i) => ({ id: `s${i}`, number: i + 1 }));
      const pts = computeRectTableSeatPositions(seats, table);
      expect(pts).toHaveLength(5);
      const halfH = table.height / 2;
      expect(pts.filter((p) => p.y < table.centerY - halfH).length).toBe(2);
    });
  });
});
