'use client';

/**
 * WorldMapAnimation — Pure SVG, no external map library.
 *
 * Uses a simplified equirectangular world map outline embedded as SVG paths
 * and a custom Mercator projection to convert lat/lon → SVG coordinates.
 *
 * Animation loop:
 *   World view → zoom Sri Lanka → city dots → SL connections →
 *   beam to USA → zoom USA → US hub dots → US connections →
 *   zoom out → pause → repeat
 *
 * Supports 6 visual themes (exported as THEMES array).
 */

import React, { useEffect, useRef, useState, useMemo } from 'react';
import { motion } from 'framer-motion';

// ─── SVG canvas size ──────────────────────────────────────────────────────────
const W = 960;
const H = 500;

// ─── Mercator projection helpers ──────────────────────────────────────────────
function mercatorX(lon: number): number {
  return ((lon + 180) / 360) * W;
}
function mercatorY(lat: number): number {
  const rad = (lat * Math.PI) / 180;
  const sinLat = Math.sin(rad);
  const y = (H / 2) - (W / (2 * Math.PI)) * Math.log((1 + sinLat) / (1 - sinLat));
  return y;
}
function latlonToXY(lat: number, lon: number): [number, number] {
  return [mercatorX(lon), mercatorY(lat)];
}

// ─── Location data ─────────────────────────────────────────────────────────────
export const SRI_LANKA_CITIES = [
  { name: 'Colombo',     lat: 6.9271,  lon: 79.8612 },
  { name: 'Kandy',       lat: 7.2906,  lon: 80.6337 },
  { name: 'Galle',       lat: 6.0535,  lon: 80.2170 },
  { name: 'Jaffna',      lat: 9.6615,  lon: 80.0255 },
  { name: 'Trincomalee', lat: 8.5874,  lon: 81.2152 },
];

export const US_HUBS = [
  { name: 'California',    lat: 36.7783, lon: -119.4179 },
  { name: 'New York',      lat: 40.7128, lon: -74.0060  },
  { name: 'New Jersey',    lat: 40.0583, lon: -74.4057  },
  { name: 'Texas',         lat: 31.9686, lon: -99.9018  },
  { name: 'Virginia',      lat: 37.4316, lon: -78.6569  },
  { name: 'Illinois',      lat: 40.6331, lon: -89.3985  },
  { name: 'Washington',    lat: 47.7511, lon: -120.7401 },
  { name: 'Ohio',          lat: 40.4173, lon: -82.9071  },
  { name: 'Massachusetts', lat: 42.4072, lon: -71.3824  },
  { name: 'Florida',       lat: 27.6648, lon: -81.5158  },
  { name: 'Georgia',       lat: 32.1656, lon: -83.6431  },
  { name: 'Maryland',      lat: 39.0458, lon: -76.6413  },
  { name: 'Pennsylvania',  lat: 41.2033, lon: -77.1945  },
];

const SL_LINE_PAIRS = [
  [0, 1], [0, 2], [1, 3], [1, 4],
];
const US_LINE_PAIRS = [
  [1, 2], [1, 4], [1, 7], [0, 6], [3, 9], [4, 11], [7, 5], [8, 12], [9, 10],
];

// Beam: Colombo → New York
const BEAM_FROM: [number, number] = [SRI_LANKA_CITIES[0].lat, SRI_LANKA_CITIES[0].lon];
const BEAM_TO: [number, number]   = [US_HUBS[1].lat, US_HUBS[1].lon];

// ─── Theme definitions ─────────────────────────────────────────────────────────
export type ThemeKey =
  | 'sunrise-brand'
  | 'brand-dark'
  | 'satellite-navy'
  | 'deep-space'
  | 'emerald-night'
  | 'ceylon-gold';

export interface MapTheme {
  key: ThemeKey;
  name: string;
  bg: string;
  landFill: string;
  landStroke: string;
  oceanFill: string;
  nodeFill: string;
  nodeGlow: string;
  lineStroke: string;
  lineGlow: string;
  beamStroke: string;
  beamGlow: string;
  textColor: string;
  tagline: string;
}

export const THEMES: MapTheme[] = [
  {
    key: 'sunrise-brand',
    name: 'Sunrise Brand',
    bg: 'linear-gradient(135deg, #fff7ed 0%, #fef3c7 50%, #ecfdf5 100%)',
    landFill: '#fed7aa', landStroke: '#fb923c',
    oceanFill: '#e0f2fe',
    nodeFill: '#FF7900', nodeGlow: 'rgba(255,121,0,0.6)',
    lineStroke: '#FF7900', lineGlow: 'rgba(255,121,0,0.4)',
    beamStroke: '#059669', beamGlow: 'rgba(5,150,105,0.5)',
    textColor: '#1a1a1a',
    tagline: 'Warm & Brand-Aligned',
  },
  {
    key: 'brand-dark',
    name: 'Brand Dark',
    bg: 'linear-gradient(135deg, #1a0a00 0%, #2d1206 50%, #0a1a0f 100%)',
    landFill: '#2d1f0a', landStroke: '#FF7900',
    oceanFill: '#0f1f1a',
    nodeFill: '#FF7900', nodeGlow: 'rgba(255,121,0,0.8)',
    lineStroke: '#FF7900', lineGlow: 'rgba(255,121,0,0.5)',
    beamStroke: '#10b981', beamGlow: 'rgba(16,185,129,0.7)',
    textColor: '#ffffff',
    tagline: 'Dark & Brand-Aligned',
  },
  {
    key: 'satellite-navy',
    name: 'Satellite Navy',
    bg: 'linear-gradient(135deg, #020818 0%, #0a0f2e 50%, #030d1a 100%)',
    landFill: '#1a2744', landStroke: '#1e40af',
    oceanFill: '#030d1a',
    nodeFill: '#00e5ff', nodeGlow: 'rgba(0,229,255,0.9)',
    lineStroke: '#00e5ff', lineGlow: 'rgba(0,229,255,0.5)',
    beamStroke: '#00ffcc', beamGlow: 'rgba(0,255,204,0.8)',
    textColor: '#e0f7ff',
    tagline: 'Satellite / Globe Style',
  },
  {
    key: 'deep-space',
    name: 'Deep Space',
    bg: 'linear-gradient(135deg, #050510 0%, #0d0b1e 50%, #050510 100%)',
    landFill: '#1a1530', landStroke: '#4c1d95',
    oceanFill: '#050510',
    nodeFill: '#fbbf24', nodeGlow: 'rgba(251,191,36,0.9)',
    lineStroke: '#a78bfa', lineGlow: 'rgba(167,139,250,0.5)',
    beamStroke: '#fbbf24', beamGlow: 'rgba(251,191,36,0.8)',
    textColor: '#f3f0ff',
    tagline: 'Deep Space Premium',
  },
  {
    key: 'emerald-night',
    name: 'Emerald Night',
    bg: 'linear-gradient(135deg, #020c06 0%, #0a1a0f 50%, #051208 100%)',
    landFill: '#14311a', landStroke: '#15803d',
    oceanFill: '#020c06',
    nodeFill: '#00ff88', nodeGlow: 'rgba(0,255,136,0.9)',
    lineStroke: '#00ff88', lineGlow: 'rgba(0,255,136,0.4)',
    beamStroke: '#34d399', beamGlow: 'rgba(52,211,153,0.8)',
    textColor: '#d1fae5',
    tagline: 'Sri Lanka Forest / Emerald',
  },
  {
    key: 'ceylon-gold',
    name: 'Ceylon Gold',
    bg: 'linear-gradient(135deg, #1a0505 0%, #2d0a0a 50%, #1a0505 100%)',
    landFill: '#3b0d0d', landStroke: '#7f1d1d',
    oceanFill: '#0d0505',
    nodeFill: '#fbbf24', nodeGlow: 'rgba(251,191,36,0.9)',
    lineStroke: '#f59e0b', lineGlow: 'rgba(245,158,11,0.5)',
    beamStroke: '#fde68a', beamGlow: 'rgba(253,230,138,0.8)',
    textColor: '#fef3c7',
    tagline: 'Sri Lanka Flag Inspired',
  },
];

// ─── Animation phases ──────────────────────────────────────────────────────────
type Phase = 'world' | 'zoom-sl' | 'sl-cities' | 'sl-lines' | 'beam' | 'zoom-us' | 'us-hubs' | 'us-lines' | 'zoom-out' | 'pause';

const PHASE_DURATIONS: Record<Phase, number> = {
  'world': 2000, 'zoom-sl': 2000, 'sl-cities': 2500, 'sl-lines': 2000,
  'beam': 2500, 'zoom-us': 2000, 'us-hubs': 2500, 'us-lines': 2000,
  'zoom-out': 2000, 'pause': 1500,
};
const PHASE_ORDER: Phase[] = [
  'world', 'zoom-sl', 'sl-cities', 'sl-lines', 'beam',
  'zoom-us', 'us-hubs', 'us-lines', 'zoom-out', 'pause',
];

interface ViewBox { x: number; y: number; w: number; h: number; }

function getViewBox(phase: Phase): ViewBox {
  switch (phase) {
    case 'zoom-sl': case 'sl-cities': case 'sl-lines': {
      const [cx, cy] = latlonToXY(7.9, 80.7);
      const z = 10;
      return { x: cx - W / (2 * z), y: cy - H / (2 * z), w: W / z, h: H / z };
    }
    case 'beam': {
      const [x1, y1] = latlonToXY(BEAM_FROM[0], BEAM_FROM[1]);
      const [x2, y2] = latlonToXY(BEAM_TO[0], BEAM_TO[1]);
      const mx = (x1 + x2) / 2;
      const my = (y1 + y2) / 2;
      const spanX = Math.abs(x2 - x1) * 1.2;
      const spanY = Math.abs(y2 - y1) * 1.5;
      const span = Math.max(spanX, spanY * (W / H), W * 0.5);
      return { x: mx - span / 2, y: my - (span / (W / H)) / 2, w: span, h: span / (W / H) };
    }
    case 'zoom-us': case 'us-hubs': case 'us-lines': {
      const [cx, cy] = latlonToXY(37, -95);
      const z = 3.5;
      return { x: cx - W / (2 * z), y: cy - H / (2 * z), w: W / z, h: H / z };
    }
    default:
      return { x: 0, y: 0, w: W, h: H };
  }
}

// ─── Simplified world land outlines (SVG path strings in equirectangular space)
// These are simplified representative outlines — accurate enough for animation purposes.
const WORLD_PATHS = `
M 213 165 L 218 168 L 215 172 L 209 173 L 205 169 L 207 164 Z
M 220 155 L 235 152 L 248 158 L 252 168 L 247 177 L 238 181 L 228 179 L 219 172 L 215 163 L 218 155 Z
M 195 172 L 203 170 L 205 177 L 200 182 L 193 179 Z
M 245 182 L 260 179 L 270 185 L 268 194 L 259 198 L 248 194 L 243 187 Z
M 115 108 L 185 90 L 245 95 L 280 115 L 295 140 L 290 165 L 270 180 L 240 185 L 200 182 L 165 175 L 140 158 L 120 138 L 110 118 Z
M 290 148 L 360 138 L 430 145 L 480 160 L 500 185 L 490 210 L 460 225 L 420 228 L 375 220 L 340 205 L 308 188 L 292 168 Z
M 480 130 L 550 122 L 610 130 L 645 148 L 650 172 L 635 192 L 605 202 L 568 198 L 535 185 L 505 165 L 490 145 Z
M 540 195 L 580 188 L 615 198 L 620 215 L 605 228 L 575 228 L 550 215 Z
M 390 205 L 420 200 L 445 210 L 445 228 L 430 240 L 408 240 L 392 228 Z
M 335 185 L 360 180 L 375 190 L 372 205 L 358 212 L 340 208 L 330 196 Z
M 110 195 L 160 185 L 200 192 L 220 210 L 218 232 L 198 248 L 165 252 L 135 245 L 115 228 L 108 210 Z
M 165 255 L 192 250 L 210 262 L 208 278 L 192 288 L 170 285 L 158 272 Z
M 175 292 L 200 286 L 218 298 L 215 315 L 198 325 L 178 320 L 168 305 Z
M 192 328 L 215 320 L 228 335 L 225 352 L 210 362 L 192 358 L 182 342 Z
M 372 248 L 408 242 L 432 255 L 428 278 L 408 292 L 378 288 L 360 272 L 365 255 Z
M 605 78 L 645 70 L 680 78 L 695 98 L 685 118 L 660 125 L 630 118 L 612 100 Z
M 640 128 L 672 122 L 698 132 L 700 152 L 685 165 L 658 162 L 640 148 Z
M 680 110 L 720 102 L 758 112 L 768 132 L 755 150 L 728 155 L 698 145 L 680 128 Z
M 718 58 L 760 48 L 800 55 L 820 72 L 812 92 L 785 100 L 752 96 L 722 82 Z
M 460 82 L 510 72 L 552 80 L 565 98 L 555 118 L 525 126 L 488 120 L 462 102 Z
M 52 138 L 98 128 L 138 135 L 155 152 L 148 172 L 120 180 L 88 175 L 58 160 L 45 145 Z
M 58 182 L 102 175 L 132 185 L 138 202 L 122 215 L 92 218 L 65 208 L 52 193 Z
M 698 162 L 735 155 L 760 165 L 758 185 L 740 198 L 715 195 L 700 180 Z
M 268 142 L 290 138 L 302 148 L 298 162 L 282 168 L 265 162 L 260 150 Z
M 215 105 L 238 100 L 255 108 L 252 122 L 235 128 L 215 122 L 208 112 Z
M 135 232 L 162 228 L 178 240 L 175 258 L 158 265 L 138 260 L 128 245 Z
M 395 125 L 425 118 L 450 128 L 448 145 L 430 152 L 405 148 L 392 135 Z
M 448 105 L 478 98 L 500 108 L 498 125 L 480 132 L 452 128 L 442 115 Z
M 88 80 L 128 72 L 162 80 L 172 98 L 162 115 L 135 120 L 105 115 L 85 98 Z
M 98 50 L 135 42 L 168 50 L 178 68 L 165 82 L 138 85 L 108 80 L 88 62 Z
M 28 115 L 68 108 L 98 118 L 98 135 L 75 145 L 45 142 L 22 130 Z
M 755 192 L 790 185 L 815 198 L 812 218 L 792 228 L 762 222 L 748 208 Z
M 790 220 L 820 215 L 842 228 L 838 248 L 818 258 L 792 252 L 778 238 Z
M 720 205 L 755 198 L 775 212 L 770 232 L 750 242 L 725 235 L 712 220 Z
M 305 92 L 335 85 L 358 95 L 355 112 L 335 120 L 308 115 L 298 100 Z
M 352 72 L 382 65 L 405 75 L 402 92 L 382 100 L 355 95 L 345 82 Z
M 182 65 L 212 58 L 235 68 L 232 85 L 212 92 L 185 88 L 172 75 Z
M 268 48 L 298 42 L 320 52 L 318 68 L 298 75 L 270 70 L 258 58 Z
M 558 152 L 585 145 L 608 158 L 605 178 L 582 185 L 558 178 L 548 162 Z
M 445 155 L 475 148 L 498 162 L 495 182 L 472 190 L 448 182 L 438 168 Z
M 622 215 L 650 208 L 672 222 L 668 242 L 645 250 L 620 242 L 610 228 Z
M 568 228 L 598 222 L 620 235 L 615 255 L 592 262 L 568 255 L 558 240 Z
`;

// ─── Component ─────────────────────────────────────────────────────────────────
export interface WorldMapAnimationProps {
  theme: MapTheme;
  className?: string;
}

export function WorldMapAnimation({ theme, className = '' }: WorldMapAnimationProps) {
  const [phase, setPhase] = useState<Phase>('world');
  const phaseRef = useRef<Phase>('world');
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    function advance() {
      const idx = PHASE_ORDER.indexOf(phaseRef.current);
      const next = PHASE_ORDER[(idx + 1) % PHASE_ORDER.length];
      phaseRef.current = next;
      setPhase(next);
      timerRef.current = setTimeout(advance, PHASE_DURATIONS[next]);
    }
    timerRef.current = setTimeout(advance, PHASE_DURATIONS['world']);
    return () => { if (timerRef.current) clearTimeout(timerRef.current); };
  }, []);

  const vb = getViewBox(phase);
  const viewBox = `${vb.x} ${vb.y} ${vb.w} ${vb.h}`;

  const showSLCities = ['sl-cities','sl-lines','beam','zoom-out','pause'].includes(phase);
  const showSLLines  = ['sl-lines','beam','zoom-out','pause'].includes(phase);
  const showBeam     = ['beam','zoom-us','us-hubs','us-lines','zoom-out','pause'].includes(phase);
  const showUSHubs   = ['us-hubs','us-lines','zoom-out','pause'].includes(phase);
  const showUSLines  = ['us-lines','zoom-out','pause'].includes(phase);

  const slCoords = useMemo(() => SRI_LANKA_CITIES.map(c => latlonToXY(c.lat, c.lon)), []);
  const usCoords = useMemo(() => US_HUBS.map(c => latlonToXY(c.lat, c.lon)), []);
  const beamFromXY = useMemo(() => latlonToXY(BEAM_FROM[0], BEAM_FROM[1]), []);
  const beamToXY   = useMemo(() => latlonToXY(BEAM_TO[0], BEAM_TO[1]), []);

  const nodeR = ['zoom-sl','sl-cities','sl-lines'].includes(phase) ? 2.5
    : ['zoom-us','us-hubs','us-lines'].includes(phase) ? 2.5
    : 1.5;

  return (
    <div className={`w-full h-full overflow-hidden ${className}`} style={{ background: theme.bg }}>
      <svg
        viewBox={viewBox}
        style={{
          width: '100%', height: '100%',
          transition: 'viewBox 2s ease-in-out',
        }}
        preserveAspectRatio="xMidYMid slice"
      >
        <defs>
          <filter id="glow-node">
            <feGaussianBlur stdDeviation="2" result="blur" />
            <feMerge><feMergeNode in="blur" /><feMergeNode in="SourceGraphic" /></feMerge>
          </filter>
          <filter id="glow-line">
            <feGaussianBlur stdDeviation="1.5" result="blur" />
            <feMerge><feMergeNode in="blur" /><feMergeNode in="SourceGraphic" /></feMerge>
          </filter>
          <filter id="glow-beam">
            <feGaussianBlur stdDeviation="3" result="blur" />
            <feMerge><feMergeNode in="blur" /><feMergeNode in="SourceGraphic" /></feMerge>
          </filter>
        </defs>

        {/* Ocean */}
        <rect x="-500" y="-500" width="2000" height="1500" fill={theme.oceanFill} />

        {/* Land */}
        <g fill={theme.landFill} stroke={theme.landStroke} strokeWidth="0.4">
          <path d={WORLD_PATHS} />
        </g>

        {/* SL city connection lines */}
        {showSLLines && SL_LINE_PAIRS.map(([a, b], i) => (
          <motion.line
            key={`sl-line-${i}`}
            x1={slCoords[a][0]} y1={slCoords[a][1]}
            x2={slCoords[b][0]} y2={slCoords[b][1]}
            stroke={theme.lineStroke}
            strokeWidth="0.6"
            strokeLinecap="round"
            filter="url(#glow-line)"
            initial={{ pathLength: 0, opacity: 0 }}
            animate={{ pathLength: 1, opacity: 0.9 }}
            transition={{ duration: 0.8, delay: i * 0.25 }}
          />
        ))}

        {/* Transcontinental beam */}
        {showBeam && (
          <motion.line
            x1={beamFromXY[0]} y1={beamFromXY[1]}
            x2={beamToXY[0]}   y2={beamToXY[1]}
            stroke={theme.beamStroke}
            strokeWidth="1.0"
            strokeLinecap="round"
            strokeDasharray="4 3"
            filter="url(#glow-beam)"
            initial={{ pathLength: 0, opacity: 0 }}
            animate={{ pathLength: 1, opacity: 0.85 }}
            transition={{ duration: 1.5 }}
          />
        )}

        {/* US hub connection lines */}
        {showUSLines && US_LINE_PAIRS.map(([a, b], i) => (
          <motion.line
            key={`us-line-${i}`}
            x1={usCoords[a][0]} y1={usCoords[a][1]}
            x2={usCoords[b][0]} y2={usCoords[b][1]}
            stroke={theme.lineStroke}
            strokeWidth="0.6"
            strokeLinecap="round"
            filter="url(#glow-line)"
            initial={{ pathLength: 0, opacity: 0 }}
            animate={{ pathLength: 1, opacity: 0.9 }}
            transition={{ duration: 0.7, delay: i * 0.2 }}
          />
        ))}

        {/* Sri Lanka city nodes */}
        {slCoords.map((xy, i) => (
          <g key={`sl-${i}`}>
            <motion.circle
              cx={xy[0]} cy={xy[1]}
              r={nodeR}
              fill={showSLCities ? theme.nodeFill : 'transparent'}
              filter="url(#glow-node)"
              initial={{ scale: 0, opacity: 0 }}
              animate={showSLCities ? { scale: 1, opacity: 1 } : { scale: 0, opacity: 0 }}
              transition={{ duration: 0.5, delay: i * 0.2 }}
            />
            {showSLCities && (
              <motion.circle
                cx={xy[0]} cy={xy[1]}
                r={nodeR * 3}
                fill="transparent"
                stroke={theme.nodeFill}
                strokeWidth="0.4"
                initial={{ scale: 0.5, opacity: 0.7 }}
                animate={{ scale: 2, opacity: 0 }}
                transition={{ duration: 1.8, delay: i * 0.2, repeat: Infinity }}
              />
            )}
          </g>
        ))}

        {/* US hub nodes */}
        {usCoords.map((xy, i) => (
          <g key={`us-${i}`}>
            <motion.circle
              cx={xy[0]} cy={xy[1]}
              r={nodeR}
              fill={showUSHubs ? theme.nodeFill : 'transparent'}
              filter="url(#glow-node)"
              initial={{ scale: 0, opacity: 0 }}
              animate={showUSHubs ? { scale: 1, opacity: 1 } : { scale: 0, opacity: 0 }}
              transition={{ duration: 0.5, delay: i * 0.15 }}
            />
            {showUSHubs && (
              <motion.circle
                cx={xy[0]} cy={xy[1]}
                r={nodeR * 3}
                fill="transparent"
                stroke={theme.nodeFill}
                strokeWidth="0.4"
                initial={{ scale: 0.5, opacity: 0.7 }}
                animate={{ scale: 2, opacity: 0 }}
                transition={{ duration: 1.8, delay: i * 0.15, repeat: Infinity }}
              />
            )}
          </g>
        ))}
      </svg>
    </div>
  );
}
