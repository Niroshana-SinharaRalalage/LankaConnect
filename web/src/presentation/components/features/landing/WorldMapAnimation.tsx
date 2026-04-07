'use client';

/**
 * WorldMapAnimation — Professional animated world map.
 *
 * Renders accurate Natural Earth 110m country outlines via d3-geo,
 * with curved bezier arc connections between Sri Lanka cities and
 * US diaspora hubs. CSS transform drives smooth pan/zoom; framer-motion
 * animates arc drawing and node pulses.
 *
 * Animation: World → Sri Lanka zoom → city network →
 *   Colombo→New York arc → USA zoom → hub network → World
 */

import React, { useEffect, useRef, useState, useMemo } from 'react';
import { motion } from 'framer-motion';
import { geoMercator, geoPath, geoGraticule } from 'd3-geo';
import { feature } from 'topojson-client';

// ─── Canvas ───────────────────────────────────────────────────────────────────
const W = 960;
const H = 500;

// ─── Projection ───────────────────────────────────────────────────────────────
const PROJ = geoMercator()
  .scale(W / (2 * Math.PI))
  .translate([W / 2, H / 2 + 30]);

const PATH_GEN = geoPath(PROJ);

function px(lon: number, lat: number): [number, number] {
  return (PROJ([lon, lat]) ?? [0, 0]) as [number, number];
}

// ─── Locations ────────────────────────────────────────────────────────────────
export const SRI_LANKA_CITIES = [
  { name: 'Colombo',      lat: 6.9271,  lon: 79.8612 },  // Western Province
  { name: 'Kandy',        lat: 7.2906,  lon: 80.6337 },  // Central Province
  { name: 'Galle',        lat: 6.0535,  lon: 80.2170 },  // Southern Province
  { name: 'Jaffna',       lat: 9.6615,  lon: 80.0255 },  // Northern Province
  { name: 'Trincomalee',  lat: 8.5874,  lon: 81.2152 },  // Eastern Province
  { name: 'Kurunegala',   lat: 7.4675,  lon: 80.3647 },  // North Western Province
  { name: 'Anuradhapura', lat: 8.3114,  lon: 80.4037 },  // North Central Province
  { name: 'Badulla',      lat: 6.9934,  lon: 81.0550 },  // Uva Province
  { name: 'Ratnapura',    lat: 6.6828,  lon: 80.3992 },  // Sabaragamuwa Province
];

export const US_HUBS = [
  { name: 'California',     lat: 36.7783, lon: -119.4179 },  // 0
  { name: 'New York',       lat: 40.7128, lon: -74.0060  },  // 1
  { name: 'New Jersey',     lat: 40.0583, lon: -74.4057  },  // 2
  { name: 'Texas',          lat: 31.9686, lon: -99.9018  },  // 3
  { name: 'Virginia',       lat: 37.4316, lon: -78.6569  },  // 4
  { name: 'Illinois',       lat: 40.6331, lon: -89.3985  },  // 5
  { name: 'Washington',     lat: 47.7511, lon: -120.7401 },  // 6
  { name: 'Ohio',           lat: 40.4173, lon: -82.9071  },  // 7
  { name: 'Massachusetts',  lat: 42.4072, lon: -71.3824  },  // 8
  { name: 'Florida',        lat: 27.6648, lon: -81.5158  },  // 9
  { name: 'Georgia',        lat: 32.1656, lon: -83.6431  },  // 10
  { name: 'Maryland',       lat: 39.0458, lon: -76.6413  },  // 11
  { name: 'Pennsylvania',   lat: 41.2033, lon: -77.1945  },  // 12
  { name: 'Michigan',       lat: 44.3148, lon: -85.6024  },  // 13
  { name: 'North Carolina', lat: 35.7596, lon: -79.0193  },  // 14
  { name: 'Arizona',        lat: 34.0489, lon: -111.0937 },  // 15
  { name: 'Colorado',       lat: 39.5501, lon: -105.7821 },  // 16
  { name: 'Connecticut',    lat: 41.6032, lon: -73.0877  },  // 17
  { name: 'Minnesota',      lat: 46.7296, lon: -94.6859  },  // 18
  { name: 'Oregon',         lat: 43.8041, lon: -120.5542 },  // 19
  { name: 'Nevada',         lat: 38.8026, lon: -116.4194 },  // 20
  { name: 'Indiana',        lat: 40.2672, lon: -86.1349  },  // 21
  { name: 'Missouri',       lat: 37.9643, lon: -91.8318  },  // 22
  { name: 'Tennessee',      lat: 35.5175, lon: -86.5804  },  // 23
  { name: 'Wisconsin',      lat: 43.7844, lon: -88.7879  },  // 24
  { name: 'South Carolina', lat: 33.8361, lon: -81.1637  },  // 25
  { name: 'Louisiana',      lat: 31.1695, lon: -91.8678  },  // 26
  { name: 'Kentucky',       lat: 37.6681, lon: -84.6701  },  // 27
  { name: 'Alabama',        lat: 32.3182, lon: -86.9023  },  // 28
  { name: 'Oklahoma',       lat: 35.4676, lon: -97.5164  },  // 29
  { name: 'Iowa',           lat: 41.8780, lon: -93.0977  },  // 30
  { name: 'Kansas',         lat: 38.5266, lon: -96.7265  },  // 31
  { name: 'New Mexico',     lat: 34.5199, lon: -105.8701 },  // 32
  { name: 'Utah',           lat: 39.3210, lon: -111.0937 },  // 33
];

const SL_LINE_PAIRS = [
  [0, 1], [0, 2], [0, 5], [0, 8],   // Colombo → Central, Southern, NW, Sabara
  [1, 5], [1, 6], [1, 7],            // Kandy → NW, NC, Uva
  [2, 8], [2, 7],                    // Galle → Sabara, Uva
  [3, 4], [3, 6],                    // Jaffna → Eastern, NC
  [4, 7], [6, 4], [6, 3],            // Trinco ↔ NC ↔ Uva
  [5, 6], [7, 8],                    // NW → NC; Uva → Sabara
];
const US_LINE_PAIRS = [
  // East Coast corridor
  [8, 17], [8, 1], [1, 2], [2, 12], [12, 11], [11, 4], [4, 14], [14, 25], [25, 10], [10, 9],
  // Southeast cluster
  [9, 28], [28, 26], [28, 23], [23, 27], [27, 7], [10, 23],
  // Great Lakes / Midwest
  [7, 21], [21, 5], [5, 24], [24, 13], [13, 7], [5, 22], [22, 30], [30, 18], [18, 24],
  // South-Central
  [3, 26], [3, 29], [29, 22], [3, 32], [3, 31], [31, 16],
  // Mountain / West
  [32, 15], [15, 20], [20, 0], [33, 20], [33, 16], [16, 15],
  // Pacific Northwest
  [6, 19], [19, 0], [6, 0],
  // Diaspora cross-country
  [1, 5], [0, 5], [0, 3], [1, 11], [1, 4],
];

// Natural Earth ISO 3166-1 numeric IDs
const SL_ID = '144';
const US_ID = '840';

// ─── Curved arc helper ────────────────────────────────────────────────────────
function makeArc(x1: number, y1: number, x2: number, y2: number): string {
  const mx = (x1 + x2) / 2;
  const my = (y1 + y2) / 2;
  const dx = x2 - x1;
  const dy = y2 - y1;
  const len = Math.sqrt(dx * dx + dy * dy) || 1;
  const curvature = Math.min(len * 0.35, 130);
  // Perpendicular offset: (-dy, dx) direction, curves toward "north" for east-west arcs
  const cx = mx + (-dy / len) * curvature;
  const cy = my + ( dx / len) * curvature;
  return `M ${x1} ${y1} Q ${cx} ${cy} ${x2} ${y2}`;
}

// ─── Themes ───────────────────────────────────────────────────────────────────
export type ThemeKey =
  | 'sunrise-brand' | 'brand-dark' | 'satellite-navy'
  | 'deep-space'    | 'emerald-night' | 'ceylon-gold';

export interface MapTheme {
  key: ThemeKey;
  name: string;
  tagline: string;
  bg: string;
  landFill: string;
  landStroke: string;
  highlightFill: string;
  highlightStroke: string;
  oceanFill: string;
  gridColor: string;
  nodeFill: string;
  nodeGlow: string;
  lineStroke: string;
  beamStroke: string;
  textColor: string;
  isDark: boolean;
}

export const THEMES: MapTheme[] = [
  {
    key: 'sunrise-brand', name: 'Sunrise Brand', tagline: 'Warm & Brand-Aligned',
    bg: 'linear-gradient(180deg, #fff7ed 0%, #fef3c7 60%, #ecfdf5 100%)',
    landFill: '#fde68a', landStroke: '#f59e0b',
    highlightFill: '#fb923c', highlightStroke: '#c2410c',
    oceanFill: '#dbeafe', gridColor: 'rgba(251,191,36,0.15)',
    nodeFill: '#FF7900', nodeGlow: 'rgba(255,121,0,0.8)',
    lineStroke: '#FF7900', beamStroke: '#059669',
    textColor: '#1a1a1a', isDark: false,
  },
  {
    key: 'brand-dark', name: 'Brand Dark', tagline: 'Dark & Brand-Aligned',
    bg: 'linear-gradient(180deg, #1a0a00 0%, #2d1206 60%, #0a1a0f 100%)',
    landFill: '#3d2007', landStroke: '#7c2d12',
    highlightFill: '#9a3412', highlightStroke: '#FF7900',
    oceanFill: '#0f1a10', gridColor: 'rgba(255,121,0,0.07)',
    nodeFill: '#FF7900', nodeGlow: 'rgba(255,121,0,0.9)',
    lineStroke: '#FF7900', beamStroke: '#10b981',
    textColor: '#fff7ed', isDark: true,
  },
  {
    key: 'satellite-navy', name: 'Satellite Navy', tagline: 'Satellite / Globe Style',
    bg: 'linear-gradient(180deg, #020818 0%, #0a0f2e 60%, #030d1a 100%)',
    landFill: '#1a2744', landStroke: '#1d4ed8',
    highlightFill: '#1e3a8a', highlightStroke: '#60a5fa',
    oceanFill: '#020d1a', gridColor: 'rgba(0,229,255,0.06)',
    nodeFill: '#00e5ff', nodeGlow: 'rgba(0,229,255,0.9)',
    lineStroke: '#00e5ff', beamStroke: '#00ffcc',
    textColor: '#e0f7ff', isDark: true,
  },
  {
    key: 'deep-space', name: 'Deep Space', tagline: 'Deep Space Premium',
    bg: 'linear-gradient(180deg, #050510 0%, #0d0b1e 60%, #050510 100%)',
    landFill: '#1e1b4b', landStroke: '#4c1d95',
    highlightFill: '#2e1065', highlightStroke: '#a855f7',
    oceanFill: '#050510', gridColor: 'rgba(167,139,250,0.06)',
    nodeFill: '#fbbf24', nodeGlow: 'rgba(251,191,36,0.9)',
    lineStroke: '#a78bfa', beamStroke: '#fbbf24',
    textColor: '#f3f0ff', isDark: true,
  },
  {
    key: 'emerald-night', name: 'Emerald Night', tagline: 'Sri Lanka Forest / Emerald',
    bg: 'linear-gradient(180deg, #020c06 0%, #0a1a0f 60%, #051208 100%)',
    landFill: '#14311a', landStroke: '#166534',
    highlightFill: '#15803d', highlightStroke: '#4ade80',
    oceanFill: '#020c06', gridColor: 'rgba(0,255,136,0.05)',
    nodeFill: '#00ff88', nodeGlow: 'rgba(0,255,136,0.9)',
    lineStroke: '#00ff88', beamStroke: '#34d399',
    textColor: '#d1fae5', isDark: true,
  },
  {
    key: 'ceylon-gold', name: 'Ceylon Gold', tagline: 'Sri Lanka Flag Inspired',
    bg: 'linear-gradient(180deg, #1a0505 0%, #2d0a0a 60%, #1a0505 100%)',
    landFill: '#3b0d0d', landStroke: '#7f1d1d',
    highlightFill: '#991b1b', highlightStroke: '#fbbf24',
    oceanFill: '#0d0505', gridColor: 'rgba(251,191,36,0.06)',
    nodeFill: '#fbbf24', nodeGlow: 'rgba(251,191,36,0.9)',
    lineStroke: '#f59e0b', beamStroke: '#fde68a',
    textColor: '#fef3c7', isDark: true,
  },
];

// ─── Animation phases ─────────────────────────────────────────────────────────
type Phase =
  | 'world' | 'zoom-sl' | 'sl-cities' | 'sl-lines'
  | 'beam'  | 'zoom-us' | 'us-hubs'   | 'us-lines'
  | 'zoom-out' | 'pause';

const PHASE_MS: Record<Phase, number> = {
  'world': 2500, 'zoom-sl': 2000, 'sl-cities': 2500, 'sl-lines': 2500,
  'beam': 3000, 'zoom-us': 2000, 'us-hubs': 2500, 'us-lines': 2500,
  'zoom-out': 2500, 'pause': 1500,
};
const PHASE_SEQ: Phase[] = [
  'world', 'zoom-sl', 'sl-cities', 'sl-lines',
  'beam', 'zoom-us', 'us-hubs', 'us-lines',
  'zoom-out', 'pause',
];

interface View { lon: number; lat: number; zoom: number; }

function getView(phase: Phase): View {
  switch (phase) {
    case 'zoom-sl': case 'sl-cities': case 'sl-lines':
      return { lon: 80.5, lat: 7.9, zoom: 16 };
    case 'beam':
      return { lon: 15,  lat: 28,  zoom: 1.1 };
    case 'zoom-us': case 'us-hubs': case 'us-lines':
      return { lon: -93, lat: 38,  zoom: 4.8 };
    default:
      return { lon: 15,  lat: 15,  zoom: 1 };
  }
}

// ─── Deterministic starfield ──────────────────────────────────────────────────
const STARS = Array.from({ length: 140 }, (_, i) => {
  const a = (Math.sin(i * 127.1) * 43758.5453) % 1;
  const b = (Math.sin(i * 311.7) * 43758.5453) % 1;
  const c = (Math.sin(i * 74.9)  * 43758.5453) % 1;
  const d = (Math.sin(i * 521.3) * 43758.5453) % 1;
  return {
    x: Math.abs(a) * W,
    y: Math.abs(b) * H,
    r: 0.3 + Math.abs(c) * 0.9,
    o: 0.15 + Math.abs(d) * 0.5,
  };
});

// ─── Component ────────────────────────────────────────────────────────────────
export interface WorldMapAnimationProps {
  theme: MapTheme;
  className?: string;
}

export function WorldMapAnimation({ theme, className = '' }: WorldMapAnimationProps) {
  const [phase, setPhase] = useState<Phase>('world');
  const phaseRef = useRef<Phase>('world');
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const [countryPaths, setCountryPaths] = useState<Array<{ id: string; d: string }>>([]);
  const [graticuleD,   setGraticuleD  ] = useState('');
  const [usBorderPaths, setUsBorderPaths] = useState<string[]>([]);
  const [lkProvincePaths, setLkProvincePaths] = useState<string[]>([]);

  // Load world TopoJSON from /public
  useEffect(() => {
    fetch('/world-50m.json')
      .then(r => {
        if (!r.ok) throw new Error(`HTTP ${r.status}`);
        return r.json();
      })
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      .then((topo: any) => {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const countries = feature(topo, topo.objects.countries) as any;
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const paths: Array<{ id: string; d: string }> = countries.features.map((f: any) => ({
          id: String(f.id ?? ''),
          d: PATH_GEN(f) ?? '',
        })).filter((p: { id: string; d: string }) => p.d.length > 0);
        setCountryPaths(paths);

        const grat = geoGraticule().step([15, 15]);
        setGraticuleD(PATH_GEN(grat()) ?? '');
      })
      .catch(err => {
        console.warn('[WorldMapAnimation] Failed to load world-50m.json:', err);
      });
  }, []);

  // Load US state borders
  useEffect(() => {
    fetch('/us-states.json')
      .then(r => { if (!r.ok) throw new Error(`HTTP ${r.status}`); return r.json(); })
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      .then((geojson: any) => {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const paths = geojson.features.map((f: any) => PATH_GEN(f) ?? '').filter((d: string) => d.length > 0);
        setUsBorderPaths(paths);
      })
      .catch(err => console.warn('[WorldMapAnimation] US states load failed:', err));
  }, []);

  // Load Sri Lanka province borders
  useEffect(() => {
    fetch('/lk-provinces.json')
      .then(r => { if (!r.ok) throw new Error(`HTTP ${r.status}`); return r.json(); })
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      .then((geojson: any) => {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const paths = geojson.features.map((f: any) => PATH_GEN(f) ?? '').filter((d: string) => d.length > 0);
        setLkProvincePaths(paths);
      })
      .catch(err => console.warn('[WorldMapAnimation] SL provinces load failed:', err));
  }, []);

  // Phase timer loop
  useEffect(() => {
    const advance = () => {
      const idx = PHASE_SEQ.indexOf(phaseRef.current);
      const next = PHASE_SEQ[(idx + 1) % PHASE_SEQ.length];
      phaseRef.current = next;
      setPhase(next);
      timerRef.current = setTimeout(advance, PHASE_MS[next]);
    };
    timerRef.current = setTimeout(advance, PHASE_MS.world);
    return () => { if (timerRef.current) clearTimeout(timerRef.current); };
  }, []);

  // Pre-compute SVG coordinates
  const slXY     = useMemo(() => SRI_LANKA_CITIES.map(c => px(c.lon, c.lat)), []);
  const usXY     = useMemo(() => US_HUBS.map(c => px(c.lon, c.lat)),          []);
  const beamFrom = useMemo(() => px(SRI_LANKA_CITIES[0].lon, SRI_LANKA_CITIES[0].lat), []);
  const beamTo   = useMemo(() => px(US_HUBS[1].lon, US_HUBS[1].lat),                   []);

  // Arc paths
  const slArcs   = useMemo(() => SL_LINE_PAIRS.map(([a, b]) => makeArc(slXY[a][0], slXY[a][1], slXY[b][0], slXY[b][1])), [slXY]);
  const usArcs   = useMemo(() => US_LINE_PAIRS.map(([a, b]) => makeArc(usXY[a][0], usXY[a][1], usXY[b][0], usXY[b][1])), [usXY]);
  const beamPath = useMemo(() => makeArc(beamFrom[0], beamFrom[1], beamTo[0], beamTo[1]),                                  [beamFrom, beamTo]);

  // CSS pan/zoom transform — pivot on the target centre, scale around it
  const view = getView(phase);
  const [vpx, vpy] = useMemo(() => px(view.lon, view.lat), [view.lon, view.lat]);
  const tx = (W / 2 - view.zoom * vpx).toFixed(3);
  const ty = (H / 2 - view.zoom * vpy).toFixed(3);
  const groupTransform = `translate(${tx}px, ${ty}px) scale(${view.zoom})`;

  // Visibility flags
  const showSLCities = ['sl-cities','sl-lines','beam','zoom-out','pause'].includes(phase);
  const showSLLines  = ['sl-lines', 'beam','zoom-out','pause'].includes(phase);
  const showBeam     = ['beam','zoom-us','us-hubs','us-lines','zoom-out','pause'].includes(phase);
  const showUSHubs   = ['us-hubs','us-lines','zoom-out','pause'].includes(phase);
  const showUSLines  = ['us-lines','zoom-out','pause'].includes(phase);
  const isSLPhase    = ['zoom-sl','sl-cities','sl-lines'].includes(phase);
  const isUSPhase    = ['zoom-us','us-hubs','us-lines'].includes(phase);

  // Scale-invariant sizes (visual size stays constant across zoom levels)
  const z        = view.zoom;
  const nodeR    = 1.5 / z;
  const strokeW  = 0.7 / z;

  const filterId = (type: string) => `${type}-${theme.key}`;

  return (
    <div
      className={`relative w-full h-full overflow-hidden ${className}`}
      style={{ background: theme.bg }}
    >
      {/* Sri Lanka phase: matches /lanka-events banner — orange → crimson/pink → forest green */}
      <div
        className="absolute inset-0 pointer-events-none"
        style={{
          background: 'linear-gradient(135deg, #e85d04 0%, #c1121f 35%, #9b2226 55%, #1a6b3c 100%)',
          opacity: ['zoom-sl','sl-cities','sl-lines'].includes(phase) ? 0.80 : 0,
          transition: 'opacity 1.8s ease-in-out',
        }}
      />
      {/* USA phase: deep blue gradient */}
      <div
        className="absolute inset-0 pointer-events-none"
        style={{
          background: 'linear-gradient(135deg, #0c2461 0%, #1e3799 50%, #0a3d62 100%)',
          opacity: ['zoom-us','us-hubs','us-lines'].includes(phase) ? 0.82 : 0,
          transition: 'opacity 1.8s ease-in-out',
        }}
      />
      <svg
        viewBox={`0 0 ${W} ${H}`}
        className="absolute inset-0 w-full h-full"
        preserveAspectRatio="xMidYMid slice"
        style={{ display: 'block' }}
      >
        <defs>
          {/* Glow: node */}
          <filter id={filterId('gn')} x="-100%" y="-100%" width="300%" height="300%">
            <feGaussianBlur in="SourceGraphic" stdDeviation="2.5" result="blur" />
            <feMerge>
              <feMergeNode in="blur" />
              <feMergeNode in="SourceGraphic" />
            </feMerge>
          </filter>
          {/* Glow: line */}
          <filter id={filterId('gl')} x="-40%" y="-40%" width="180%" height="180%">
            <feGaussianBlur in="SourceGraphic" stdDeviation="1.5" result="blur" />
            <feMerge>
              <feMergeNode in="blur" />
              <feMergeNode in="SourceGraphic" />
            </feMerge>
          </filter>
          {/* Glow: beam */}
          <filter id={filterId('gb')} x="-30%" y="-80%" width="160%" height="260%">
            <feGaussianBlur in="SourceGraphic" stdDeviation="3" result="blur" />
            <feMerge>
              <feMergeNode in="blur" />
              <feMergeNode in="SourceGraphic" />
            </feMerge>
          </filter>
          {/* Node radial gradient */}
          <radialGradient id={filterId('ng')} cx="50%" cy="40%" r="50%">
            <stop offset="0%"   stopColor="white"         stopOpacity="0.95" />
            <stop offset="40%"  stopColor={theme.nodeFill} stopOpacity="1"   />
            <stop offset="100%" stopColor={theme.nodeFill} stopOpacity="0.2" />
          </radialGradient>
          {/* Vignette */}
          <radialGradient id="vg" cx="50%" cy="50%" r="70%">
            <stop offset="55%" stopColor="black" stopOpacity="0" />
            <stop offset="100%" stopColor="black" stopOpacity="0.65" />
          </radialGradient>
          {/* Beam gradient for animated trail */}
          <linearGradient id={filterId('bg-grad')} x1="0%" y1="0%" x2="100%" y2="0%">
            <stop offset="0%"   stopColor={theme.beamStroke} stopOpacity="0" />
            <stop offset="40%"  stopColor={theme.beamStroke} stopOpacity="0.6" />
            <stop offset="100%" stopColor={theme.beamStroke} stopOpacity="1" />
          </linearGradient>
          {/* Strong glow for beam */}
          <filter id={filterId('gb2')} x="-50%" y="-200%" width="200%" height="500%">
            <feGaussianBlur in="SourceGraphic" stdDeviation="5" result="blur" />
            <feMerge>
              <feMergeNode in="blur" />
              <feMergeNode in="blur" />
              <feMergeNode in="SourceGraphic" />
            </feMerge>
          </filter>
        </defs>

        {/* Ocean — semi-transparent so phase gradient overlays show through */}
        <rect
          x={0} y={0} width={W} height={H}
          fill={theme.oceanFill}
          style={{
            fillOpacity: isSLPhase || isUSPhase ? 0.18 : 0.88,
            transition: 'fill-opacity 1.8s ease-in-out',
          }}
        />

        {/* Stars (dark themes only) */}
        {theme.isDark && STARS.map((s, i) => (
          <circle key={i} cx={s.x} cy={s.y} r={s.r} fill="white" opacity={s.o} />
        ))}

        {/* Graticule grid */}
        {graticuleD && (
          <path d={graticuleD} stroke={theme.gridColor} strokeWidth="0.4" fill="none" />
        )}

        {/* ── Pan/zoom group ──────────────────────────────────────────────── */}
        <g
          style={{
            transform: groupTransform,
            transformOrigin: '0 0',
            transition: 'transform 2s cubic-bezier(0.4, 0, 0.2, 1)',
          }}
        >
          {/* Country fills */}
          {countryPaths.map(({ id, d }) => {
            const hlSL = isSLPhase && id === SL_ID;
            const hlUS = isUSPhase && id === US_ID;
            const hl   = hlSL || hlUS;
            return (
              <path
                key={id}
                d={d}
                fill={hl ? theme.highlightFill : theme.landFill}
                stroke={hl ? theme.highlightStroke : theme.landStroke}
                strokeWidth={hl ? 0.7 / z : 0.25 / z}
                strokeLinejoin="round"
              />
            );
          })}

          {/* US state borders — shown during US phases */}
          {isUSPhase && usBorderPaths.map((d, i) => (
            <path
              key={`us-state-${i}`}
              d={d}
              fill="none"
              stroke="rgba(255,255,255,0.18)"
              strokeWidth={0.4 / z}
              strokeLinejoin="round"
            />
          ))}

          {/* SL province borders — shown during SL phases */}
          {isSLPhase && lkProvincePaths.map((d, i) => (
            <path
              key={`lk-prov-${i}`}
              d={d}
              fill="none"
              stroke="rgba(255,255,255,0.25)"
              strokeWidth={0.8 / z}
              strokeLinejoin="round"
            />
          ))}

          {/* SL city arcs — glow layer + sharp line layer */}
          {showSLLines && slArcs.map((d, i) => (
            <g key={`sl-arc-${i}`}>
              {/* Wide blurred glow */}
              <motion.path
                d={d} fill="none"
                stroke={theme.lineStroke}
                strokeWidth={strokeW * 5}
                strokeLinecap="round"
                filter={`url(#${filterId('gl')})`}
                opacity={0.22}
                initial={{ pathLength: 0 }}
                animate={{ pathLength: 1 }}
                transition={{ duration: 1.0, delay: i * 0.22 }}
              />
              {/* Sharp line */}
              <motion.path
                d={d} fill="none"
                stroke={theme.lineStroke}
                strokeWidth={strokeW * 0.9}
                strokeLinecap="round"
                initial={{ pathLength: 0, opacity: 0 }}
                animate={{ pathLength: 1, opacity: 0.9 }}
                transition={{ duration: 1.0, delay: i * 0.22 }}
              />
            </g>
          ))}

          {/* Transcontinental beam — wide glow + core */}
          {showBeam && (
            <g>
              {/* Wide glow layer */}
              <motion.path
                key="beam-glow"
                d={beamPath} fill="none"
                stroke={theme.beamStroke}
                strokeWidth={strokeW * 8}
                strokeLinecap="round"
                filter={`url(#${filterId('gb2')})`}
                opacity={0.3}
                initial={{ pathLength: 0 }}
                animate={{ pathLength: 1 }}
                transition={{ duration: 2.4, ease: 'easeInOut' }}
              />
              {/* Medium glow */}
              <motion.path
                key="beam-mid"
                d={beamPath} fill="none"
                stroke={theme.beamStroke}
                strokeWidth={strokeW * 3}
                strokeLinecap="round"
                filter={`url(#${filterId('gb')})`}
                opacity={0.55}
                initial={{ pathLength: 0 }}
                animate={{ pathLength: 1 }}
                transition={{ duration: 2.4, ease: 'easeInOut' }}
              />
              {/* Core line */}
              <motion.path
                key="beam-core"
                d={beamPath} fill="none"
                stroke={theme.beamStroke}
                strokeWidth={strokeW * 1.4}
                strokeLinecap="round"
                strokeDasharray={`${3 / z} ${2 / z}`}
                initial={{ pathLength: 0, opacity: 0 }}
                animate={{ pathLength: 1, opacity: 0.95 }}
                transition={{ duration: 2.4, ease: 'easeInOut' }}
              />
            </g>
          )}

          {/* US hub arcs — glow layer + sharp line layer */}
          {showUSLines && usArcs.map((d, i) => (
            <g key={`us-arc-${i}`}>
              {/* Wide blurred glow */}
              <motion.path
                d={d} fill="none"
                stroke={theme.lineStroke}
                strokeWidth={strokeW * 5}
                strokeLinecap="round"
                filter={`url(#${filterId('gl')})`}
                opacity={0.22}
                initial={{ pathLength: 0 }}
                animate={{ pathLength: 1 }}
                transition={{ duration: 0.85, delay: i * 0.2 }}
              />
              {/* Sharp line */}
              <motion.path
                d={d} fill="none"
                stroke={theme.lineStroke}
                strokeWidth={strokeW * 0.9}
                strokeLinecap="round"
                initial={{ pathLength: 0, opacity: 0 }}
                animate={{ pathLength: 1, opacity: 0.9 }}
                transition={{ duration: 0.85, delay: i * 0.2 }}
              />
            </g>
          ))}

          {/* Sri Lanka city nodes — 3D glowing sphere */}
          {slXY.map((xy, i) => (
            <g key={`sl-node-${i}`}>
              {showSLCities && (
                <>
                  {/* Pulse ring 1 */}
                  <motion.circle cx={xy[0]} cy={xy[1]} r={nodeR * 7}
                    fill="none" stroke={theme.nodeFill} strokeWidth={0.2 / z}
                    initial={{ scale: 0.4, opacity: 0.7 }}
                    animate={{ scale: 2.8, opacity: 0 }}
                    transition={{ duration: 2.8, delay: i * 0.25, repeat: Infinity }}
                  />
                  {/* Pulse ring 2 — offset timing */}
                  <motion.circle cx={xy[0]} cy={xy[1]} r={nodeR * 5}
                    fill="none" stroke={theme.nodeFill} strokeWidth={0.15 / z}
                    initial={{ scale: 0.5, opacity: 0.5 }}
                    animate={{ scale: 2.4, opacity: 0 }}
                    transition={{ duration: 2.8, delay: i * 0.25 + 0.9, repeat: Infinity }}
                  />
                  {/* Halo ring — static soft glow */}
                  <circle cx={xy[0]} cy={xy[1]} r={nodeR * 3.5}
                    fill={theme.nodeGlow} opacity={0.15}
                    filter={`url(#${filterId('gn')})`}
                  />
                </>
              )}
              {/* Core sphere with radial gradient */}
              <motion.circle
                cx={xy[0]} cy={xy[1]} r={nodeR * 2}
                fill={`url(#${filterId('ng')})`}
                filter={`url(#${filterId('gn')})`}
                initial={{ scale: 0, opacity: 0 }}
                animate={showSLCities ? { scale: 1, opacity: 1 } : { scale: 0, opacity: 0 }}
                transition={{ duration: 0.55, delay: i * 0.15 }}
              />
              {/* White hot center */}
              {showSLCities && (
                <circle cx={xy[0]} cy={xy[1]} r={nodeR * 0.6} fill="white" opacity={0.9} />
              )}
            </g>
          ))}

          {/* US hub nodes — 3D glowing sphere */}
          {usXY.map((xy, i) => (
            <g key={`us-node-${i}`}>
              {showUSHubs && (
                <>
                  {/* Pulse ring 1 */}
                  <motion.circle cx={xy[0]} cy={xy[1]} r={nodeR * 7}
                    fill="none" stroke={theme.nodeFill} strokeWidth={0.2 / z}
                    initial={{ scale: 0.4, opacity: 0.7 }}
                    animate={{ scale: 2.8, opacity: 0 }}
                    transition={{ duration: 2.8, delay: i * 0.12, repeat: Infinity }}
                  />
                  {/* Pulse ring 2 — offset timing */}
                  <motion.circle cx={xy[0]} cy={xy[1]} r={nodeR * 5}
                    fill="none" stroke={theme.nodeFill} strokeWidth={0.15 / z}
                    initial={{ scale: 0.5, opacity: 0.5 }}
                    animate={{ scale: 2.4, opacity: 0 }}
                    transition={{ duration: 2.8, delay: i * 0.12 + 0.7, repeat: Infinity }}
                  />
                  {/* Halo ring — static soft glow */}
                  <circle cx={xy[0]} cy={xy[1]} r={nodeR * 3.5}
                    fill={theme.nodeGlow} opacity={0.15}
                    filter={`url(#${filterId('gn')})`}
                  />
                </>
              )}
              {/* Core sphere with radial gradient */}
              <motion.circle
                cx={xy[0]} cy={xy[1]} r={nodeR * 2}
                fill={`url(#${filterId('ng')})`}
                filter={`url(#${filterId('gn')})`}
                initial={{ scale: 0, opacity: 0 }}
                animate={showUSHubs ? { scale: 1, opacity: 1 } : { scale: 0, opacity: 0 }}
                transition={{ duration: 0.55, delay: i * 0.12 }}
              />
              {/* White hot center */}
              {showUSHubs && (
                <circle cx={xy[0]} cy={xy[1]} r={nodeR * 0.6} fill="white" opacity={0.9} />
              )}
            </g>
          ))}
        </g>
        {/* ── end pan/zoom group ───────────────────────────────────────────── */}

        {/* Vignette */}
        <rect x={0} y={0} width={W} height={H} fill="url(#vg)" />
      </svg>
    </div>
  );
}
