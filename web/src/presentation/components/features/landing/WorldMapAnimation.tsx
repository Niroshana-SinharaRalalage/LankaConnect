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
  // Western Province
  { name: 'Colombo',      lat: 6.9271,  lon: 79.8612 },  // 0
  { name: 'Gampaha',      lat: 7.0873,  lon: 80.0144 },  // 1
  { name: 'Kalutara',     lat: 6.5854,  lon: 79.9607 },  // 2
  // Central Province
  { name: 'Kandy',        lat: 7.2906,  lon: 80.6337 },  // 3
  { name: 'Matale',       lat: 7.4675,  lon: 80.6234 },  // 4
  { name: 'Nuwara Eliya', lat: 6.9497,  lon: 80.7891 },  // 5
  // Southern Province
  { name: 'Galle',        lat: 6.0535,  lon: 80.2170 },  // 6
  { name: 'Matara',       lat: 5.9549,  lon: 80.5550 },  // 7
  { name: 'Hambantota',   lat: 6.1241,  lon: 81.1185 },  // 8
  // Northern Province
  { name: 'Jaffna',       lat: 9.6615,  lon: 80.0255 },  // 9
  { name: 'Kilinochchi',  lat: 9.3803,  lon: 80.4036 },  // 10
  { name: 'Mannar',       lat: 8.9810,  lon: 79.9044 },  // 11
  { name: 'Vavuniya',     lat: 8.7514,  lon: 80.4971 },  // 12
  { name: 'Mullaitivu',   lat: 9.2671,  lon: 80.8128 },  // 13
  // Eastern Province
  { name: 'Trincomalee',  lat: 8.5874,  lon: 81.2152 },  // 14
  { name: 'Batticaloa',   lat: 7.7102,  lon: 81.6924 },  // 15
  { name: 'Ampara',       lat: 7.2992,  lon: 81.6724 },  // 16
  // North Western Province
  { name: 'Kurunegala',   lat: 7.4675,  lon: 80.3647 },  // 17
  { name: 'Puttalam',     lat: 8.0362,  lon: 79.8283 },  // 18
  // North Central Province
  { name: 'Anuradhapura', lat: 8.3114,  lon: 80.4037 },  // 19
  { name: 'Polonnaruwa',  lat: 7.9403,  lon: 81.0188 },  // 20
  // Uva Province
  { name: 'Badulla',      lat: 6.9934,  lon: 81.0550 },  // 21
  { name: 'Monaragala',   lat: 6.8728,  lon: 81.3507 },  // 22
  // Sabaragamuwa Province
  { name: 'Ratnapura',    lat: 6.6828,  lon: 80.3992 },  // 23
  { name: 'Kegalle',      lat: 7.2513,  lon: 80.3464 },  // 24
];

export const US_HUBS = [
  { name: 'Alabama',        lat: 32.3182, lon: -86.9023  },  // 0
  { name: 'Alaska',         lat: 64.2008, lon: -153.4937 },  // 1
  { name: 'Arizona',        lat: 34.0489, lon: -111.0937 },  // 2
  { name: 'Arkansas',       lat: 34.7465, lon: -92.2896  },  // 3
  { name: 'California',     lat: 36.7783, lon: -119.4179 },  // 4
  { name: 'Colorado',       lat: 39.5501, lon: -105.7821 },  // 5
  { name: 'Connecticut',    lat: 41.6032, lon: -73.0877  },  // 6
  { name: 'Delaware',       lat: 38.9108, lon: -75.5277  },  // 7
  { name: 'Florida',        lat: 27.6648, lon: -81.5158  },  // 8
  { name: 'Georgia',        lat: 32.1656, lon: -83.6431  },  // 9
  { name: 'Hawaii',         lat: 19.8968, lon: -155.5828 },  // 10
  { name: 'Idaho',          lat: 44.0682, lon: -114.7420 },  // 11
  { name: 'Illinois',       lat: 40.6331, lon: -89.3985  },  // 12
  { name: 'Indiana',        lat: 40.2672, lon: -86.1349  },  // 13
  { name: 'Iowa',           lat: 41.8780, lon: -93.0977  },  // 14
  { name: 'Kansas',         lat: 38.5266, lon: -96.7265  },  // 15
  { name: 'Kentucky',       lat: 37.6681, lon: -84.6701  },  // 16
  { name: 'Louisiana',      lat: 31.1695, lon: -91.8678  },  // 17
  { name: 'Maine',          lat: 45.2538, lon: -69.4455  },  // 18
  { name: 'Maryland',       lat: 39.0458, lon: -76.6413  },  // 19
  { name: 'Massachusetts',  lat: 42.4072, lon: -71.3824  },  // 20
  { name: 'Michigan',       lat: 44.3148, lon: -85.6024  },  // 21
  { name: 'Minnesota',      lat: 46.7296, lon: -94.6859  },  // 22
  { name: 'Mississippi',    lat: 32.3547, lon: -89.3985  },  // 23
  { name: 'Missouri',       lat: 37.9643, lon: -91.8318  },  // 24
  { name: 'Montana',        lat: 46.8797, lon: -110.3626 },  // 25
  { name: 'Nebraska',       lat: 41.4925, lon: -99.9018  },  // 26
  { name: 'Nevada',         lat: 38.8026, lon: -116.4194 },  // 27
  { name: 'New Hampshire',  lat: 43.1939, lon: -71.5724  },  // 28
  { name: 'New Jersey',     lat: 40.0583, lon: -74.4057  },  // 29
  { name: 'New Mexico',     lat: 34.5199, lon: -105.8701 },  // 30
  { name: 'New York',       lat: 42.1657, lon: -74.9481  },  // 31
  { name: 'North Carolina', lat: 35.7596, lon: -79.0193  },  // 32
  { name: 'North Dakota',   lat: 47.5515, lon: -101.0020 },  // 33
  { name: 'Ohio',           lat: 40.4173, lon: -82.9071  },  // 34
  { name: 'Oklahoma',       lat: 35.4676, lon: -97.5164  },  // 35
  { name: 'Oregon',         lat: 43.8041, lon: -120.5542 },  // 36
  { name: 'Pennsylvania',   lat: 41.2033, lon: -77.1945  },  // 37
  { name: 'Rhode Island',   lat: 41.6809, lon: -71.5118  },  // 38
  { name: 'South Carolina', lat: 33.8361, lon: -81.1637  },  // 39
  { name: 'South Dakota',   lat: 43.9695, lon: -99.9018  },  // 40
  { name: 'Tennessee',      lat: 35.5175, lon: -86.5804  },  // 41
  { name: 'Texas',          lat: 31.9686, lon: -99.9018  },  // 42
  { name: 'Utah',           lat: 39.3210, lon: -111.0937 },  // 43
  { name: 'Vermont',        lat: 44.5588, lon: -72.5778  },  // 44
  { name: 'Virginia',       lat: 37.4316, lon: -78.6569  },  // 45
  { name: 'Washington',     lat: 47.7511, lon: -120.7401 },  // 46
  { name: 'West Virginia',  lat: 38.4912, lon: -80.9545  },  // 47
  { name: 'Wisconsin',      lat: 43.7844, lon: -88.7879  },  // 48
  { name: 'Wyoming',        lat: 43.0760, lon: -107.2903 },  // 49
];

const SL_LINE_PAIRS = [
  // Western cluster
  [0,1],[0,2],[1,2],[0,24],[1,17],
  // Central cluster
  [3,4],[3,5],[4,17],[3,24],[5,21],
  // Western → Central
  [0,3],[1,4],[2,6],
  // Southern cluster
  [6,7],[7,8],[6,2],[8,22],
  // Northern cluster
  [9,10],[10,11],[10,12],[9,12],[12,13],[11,18],
  // Northern → Central spine
  [9,19],[12,19],[13,14],
  // Eastern cluster
  [14,15],[15,16],[14,20],[16,8],
  // North Central
  [19,20],[20,21],[19,17],[20,15],
  // Uva cluster
  [21,22],[22,16],[21,5],
  // Sabaragamuwa
  [23,24],[23,2],[24,3],[23,6],
  // Cross-island connections
  [0,19],[3,20],[14,9],
];
const US_LINE_PAIRS = [
  // New England
  [18,28],[28,44],[44,20],[20,6],[6,38],[38,29],[28,31],
  // Mid-Atlantic
  [31,29],[29,37],[37,19],[19,45],[45,47],[7,29],[7,19],
  // Southeast
  [32,39],[39,9],[9,0],[0,23],[23,17],[17,3],[3,41],[41,16],[16,47],
  [9,8],[8,17],[39,32],[32,45],[0,41],
  // Great Lakes
  [34,13],[13,12],[12,48],[48,21],[21,34],[34,37],[13,16],
  // Midwest spine
  [14,22],[22,33],[33,25],[25,40],[40,26],[26,15],[15,35],[35,24],
  [14,12],[22,48],[24,12],[14,26],
  // South Central
  [42,35],[42,17],[42,30],[42,15],[35,3],[30,5],
  // Mountain West
  [30,2],[2,43],[43,27],[27,4],[5,43],[5,25],[25,11],[11,36],
  // Pacific
  [36,4],[36,46],[46,11],[4,27],[46,4],
  // Northern tier
  [25,33],[33,22],[46,25],[49,25],[49,5],[49,43],
  // Hawaii & Alaska (connect to West Coast)
  [10,4],[1,46],
  // Cross-country diaspora links
  [31,12],[31,19],[31,45],[4,12],[4,42],[31,42],
  // Remaining isolated states
  [44,31],[38,20],[40,14],[47,34],[7,32],
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
  'world': 3000, 'zoom-sl': 2000, 'sl-cities': 5000, 'sl-lines': 6000,
  'beam': 3500, 'zoom-us': 2000, 'us-hubs': 6000, 'us-lines': 8000,
  'zoom-out': 2500, 'pause': 2000,
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
  const beamTo   = useMemo(() => px(US_HUBS[31].lon, US_HUBS[31].lat),                  []);

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

  // Visibility flags — nodes/lines only shown during their own zoomed phase to avoid colour bleed on world view
  const showSLCities = ['sl-cities','sl-lines','beam'].includes(phase);
  const showSLLines  = ['sl-lines','beam'].includes(phase);
  const showBeam     = ['beam','zoom-us','us-hubs','us-lines','zoom-out','pause'].includes(phase);
  const showUSHubs   = ['us-hubs','us-lines'].includes(phase);
  const showUSLines  = ['us-lines'].includes(phase);
  const isSLPhase    = ['zoom-sl','sl-cities','sl-lines'].includes(phase);
  const isUSPhase    = ['zoom-us','us-hubs','us-lines'].includes(phase);

  // Scale-invariant sizes (visual size stays constant across zoom levels)
  const z        = view.zoom;
  const nodeR    = 1.5 / z;
  const strokeW  = 0.7 / z;

  // Phase-specific node colors: SL flag gold, US flag white, default theme color
  const phaseNodeFill = isSLPhase ? '#FFD700' : isUSPhase ? '#ffffff' : theme.nodeFill;
  const phaseLineStroke = isSLPhase ? '#FFD700' : isUSPhase ? '#ffffff' : theme.lineStroke;

  // US flag cycling colors (red, white, blue) for nodes and arcs
  const US_FLAG_COLORS = ['#BF0A30', '#e8e8e8', '#002868'] as const;
  const usNodeFill  = (i: number) => isUSPhase ? US_FLAG_COLORS[i % 3] : phaseNodeFill;
  const usArcColor  = (i: number) => isUSPhase ? US_FLAG_COLORS[i % 3] : phaseLineStroke;

  const filterId = (type: string) => `${type}-${theme.key}`;

  return (
    <div
      className={`relative w-full h-full overflow-hidden ${className}`}
      style={{ background: theme.bg }}
    >
      {/* Sri Lanka phase: Sri Lanka flag colors — dark maroon + saffron gold */}
      <div
        className="absolute inset-0 pointer-events-none"
        style={{
          background: 'linear-gradient(160deg, #4a0000 0%, #7b0000 30%, #8B0000 55%, #4a3000 80%, #2d1a00 100%)',
          opacity: ['zoom-sl','sl-cities','sl-lines'].includes(phase) ? 0.82 : 0,
          transition: 'opacity 1.8s ease-in-out',
        }}
      />
      {/* USA phase: US flag colors — strong navy + deep red diagonal */}
      <div
        className="absolute inset-0 pointer-events-none"
        style={{
          background: 'linear-gradient(160deg, #001040 0%, #002868 22%, #001040 42%, #6b0010 58%, #BF0A30 75%, #8B0015 88%, #001040 100%)',
          opacity: ['zoom-us','us-hubs','us-lines'].includes(phase) ? 0.88 : 0,
          transition: 'opacity 1.8s ease-in-out',
        }}
      />
      {/* USA phase: subtle red stripe accent overlay */}
      <div
        className="absolute inset-0 pointer-events-none"
        style={{
          background: 'repeating-linear-gradient(0deg, transparent 0px, transparent 28px, rgba(191,10,48,0.06) 28px, rgba(191,10,48,0.06) 32px)',
          opacity: ['zoom-us','us-hubs','us-lines'].includes(phase) ? 1 : 0,
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
          {/* Phase-specific node gradient */}
          <radialGradient id={filterId('ng-phase')} cx="50%" cy="40%" r="50%">
            <stop offset="0%"   stopColor="white"          stopOpacity="0.95" />
            <stop offset="40%"  stopColor={phaseNodeFill}   stopOpacity="1"   />
            <stop offset="100%" stopColor={phaseNodeFill}   stopOpacity="0.2" />
          </radialGradient>
          {/* US flag node gradients — red / white / blue */}
          <radialGradient id="ng-us-red" cx="50%" cy="40%" r="50%">
            <stop offset="0%"   stopColor="white"    stopOpacity="0.95" />
            <stop offset="40%"  stopColor="#BF0A30"  stopOpacity="1"   />
            <stop offset="100%" stopColor="#8B0015"  stopOpacity="0.25" />
          </radialGradient>
          <radialGradient id="ng-us-white" cx="50%" cy="40%" r="50%">
            <stop offset="0%"   stopColor="white"    stopOpacity="1"   />
            <stop offset="40%"  stopColor="#d0d0d0"  stopOpacity="1"   />
            <stop offset="100%" stopColor="#a0a0a0"  stopOpacity="0.25" />
          </radialGradient>
          <radialGradient id="ng-us-blue" cx="50%" cy="40%" r="50%">
            <stop offset="0%"   stopColor="white"    stopOpacity="0.95" />
            <stop offset="40%"  stopColor="#002868"  stopOpacity="1"   />
            <stop offset="100%" stopColor="#001540"  stopOpacity="0.25" />
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
                stroke={phaseLineStroke}
                strokeWidth={strokeW * 5}
                strokeLinecap="round"
                filter={`url(#${filterId('gl')})`}
                opacity={0.22}
                initial={{ pathLength: 0 }}
                animate={{ pathLength: 1 }}
                transition={{ duration: 0.75, delay: i * 0.055, ease: [0.22, 1, 0.36, 1] }}
              />
              {/* Sharp line */}
              <motion.path
                d={d} fill="none"
                stroke={phaseLineStroke}
                strokeWidth={strokeW * 0.9}
                strokeLinecap="round"
                initial={{ pathLength: 0, opacity: 0 }}
                animate={{ pathLength: 1, opacity: 0.9 }}
                transition={{ duration: 0.75, delay: i * 0.055, ease: [0.22, 1, 0.36, 1] }}
              />
            </g>
          ))}

          {/* Transcontinental beam — always mounted, opacity-controlled to prevent re-draw blast */}
          <g style={{ pointerEvents: 'none' }}>
            {/* Wide glow layer — draws once on first mount, then fades in/out via opacity */}
            <motion.path
              d={beamPath} fill="none"
              stroke={theme.beamStroke}
              strokeWidth={strokeW * 8}
              strokeLinecap="round"
              filter={`url(#${filterId('gb2')})`}
              initial={{ pathLength: 0, opacity: 0 }}
              animate={{ pathLength: 1, opacity: showBeam ? 0.3 : 0 }}
              transition={{ pathLength: { duration: 2.4, ease: 'easeInOut' }, opacity: { duration: 1.2 } }}
            />
            {/* Medium glow */}
            <motion.path
              d={beamPath} fill="none"
              stroke={theme.beamStroke}
              strokeWidth={strokeW * 3}
              strokeLinecap="round"
              filter={`url(#${filterId('gb')})`}
              initial={{ pathLength: 0, opacity: 0 }}
              animate={{ pathLength: 1, opacity: showBeam ? 0.55 : 0 }}
              transition={{ pathLength: { duration: 2.4, ease: 'easeInOut' }, opacity: { duration: 1.2 } }}
            />
            {/* Core dashed line */}
            <motion.path
              d={beamPath} fill="none"
              stroke={theme.beamStroke}
              strokeWidth={strokeW * 1.4}
              strokeLinecap="round"
              strokeDasharray={`${3 / z} ${2 / z}`}
              initial={{ pathLength: 0, opacity: 0 }}
              animate={{ pathLength: 1, opacity: showBeam ? 0.95 : 0 }}
              transition={{ pathLength: { duration: 2.4, ease: 'easeInOut' }, opacity: { duration: 1.2 } }}
            />
          </g>

          {/* US hub arcs — glow layer + sharp line layer, flag colors */}
          {showUSLines && usArcs.map((d, i) => (
            <g key={`us-arc-${i}`}>
              {/* Wide blurred glow */}
              <motion.path
                d={d} fill="none"
                stroke={usArcColor(i)}
                strokeWidth={strokeW * 5}
                strokeLinecap="round"
                filter={`url(#${filterId('gl')})`}
                opacity={0.28}
                initial={{ pathLength: 0 }}
                animate={{ pathLength: 1 }}
                transition={{ duration: 0.65, delay: i * 0.04, ease: [0.22, 1, 0.36, 1] }}
              />
              {/* Sharp line */}
              <motion.path
                d={d} fill="none"
                stroke={usArcColor(i)}
                strokeWidth={strokeW * 0.9}
                strokeLinecap="round"
                initial={{ pathLength: 0, opacity: 0 }}
                animate={{ pathLength: 1, opacity: 0.85 }}
                transition={{ duration: 0.65, delay: i * 0.04, ease: [0.22, 1, 0.36, 1] }}
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
                    fill="none" stroke={phaseNodeFill} strokeWidth={0.2 / z}
                    initial={{ scale: 0.4, opacity: 0.7 }}
                    animate={{ scale: 2.8, opacity: 0 }}
                    transition={{ duration: 2.8, delay: i * 0.25, repeat: Infinity }}
                  />
                  {/* Pulse ring 2 — offset timing */}
                  <motion.circle cx={xy[0]} cy={xy[1]} r={nodeR * 5}
                    fill="none" stroke={phaseNodeFill} strokeWidth={0.15 / z}
                    initial={{ scale: 0.5, opacity: 0.5 }}
                    animate={{ scale: 2.4, opacity: 0 }}
                    transition={{ duration: 2.8, delay: i * 0.25 + 0.9, repeat: Infinity }}
                  />
                  {/* Halo ring — static soft glow, gold during SL phase */}
                  <circle cx={xy[0]} cy={xy[1]} r={nodeR * 3.5}
                    fill={isSLPhase ? phaseNodeFill : theme.nodeGlow} opacity={0.18}
                    filter={`url(#${filterId('gn')})`}
                  />
                </>
              )}
              {/* Core sphere with radial gradient */}
              <motion.circle
                cx={xy[0]} cy={xy[1]} r={nodeR * 2}
                fill={`url(#${filterId('ng-phase')})`}
                filter={`url(#${filterId('gn')})`}
                initial={{ scale: 0, opacity: 0 }}
                animate={showSLCities ? { scale: 1, opacity: 1 } : { scale: 0, opacity: 0 }}
                transition={{ type: 'spring', stiffness: 380, damping: 22, delay: i * 0.22 }}
              />
              {/* White hot center */}
              {showSLCities && (
                <circle cx={xy[0]} cy={xy[1]} r={nodeR * 0.6} fill="white" opacity={0.9} />
              )}
            </g>
          ))}

          {/* US hub nodes — 3D glowing sphere with flag colors */}
          {usXY.map((xy, i) => {
            const US_NODE_GRADS = ['ng-us-red', 'ng-us-white', 'ng-us-blue'] as const;
            const nodeGrad = isUSPhase ? `url(#${US_NODE_GRADS[i % 3]})` : `url(#${filterId('ng-phase')})`;
            const ringColor = usNodeFill(i);
            return (
              <g key={`us-node-${i}`}>
                {showUSHubs && (
                  <>
                    {/* Pulse ring 1 */}
                    <motion.circle cx={xy[0]} cy={xy[1]} r={nodeR * 7}
                      fill="none" stroke={ringColor} strokeWidth={0.2 / z}
                      initial={{ scale: 0.4, opacity: 0.7 }}
                      animate={{ scale: 2.8, opacity: 0 }}
                      transition={{ duration: 2.8, delay: i * 0.15, repeat: Infinity }}
                    />
                    {/* Pulse ring 2 — offset timing */}
                    <motion.circle cx={xy[0]} cy={xy[1]} r={nodeR * 5}
                      fill="none" stroke={ringColor} strokeWidth={0.15 / z}
                      initial={{ scale: 0.5, opacity: 0.5 }}
                      animate={{ scale: 2.4, opacity: 0 }}
                      transition={{ duration: 2.8, delay: i * 0.15 + 0.7, repeat: Infinity }}
                    />
                    {/* Halo ring — static soft glow */}
                    <circle cx={xy[0]} cy={xy[1]} r={nodeR * 3.5}
                      fill={ringColor} opacity={0.18}
                      filter={`url(#${filterId('gn')})`}
                    />
                  </>
                )}
                {/* Core sphere with flag-color radial gradient */}
                <motion.circle
                  cx={xy[0]} cy={xy[1]} r={nodeR * 2}
                  fill={nodeGrad}
                  filter={`url(#${filterId('gn')})`}
                  initial={{ scale: 0, opacity: 0 }}
                  animate={showUSHubs ? { scale: 1, opacity: 1 } : { scale: 0, opacity: 0 }}
                  transition={{ type: 'spring', stiffness: 380, damping: 22, delay: i * 0.18 }}
                />
                {/* White hot center */}
                {showUSHubs && (
                  <circle cx={xy[0]} cy={xy[1]} r={nodeR * 0.6} fill="white" opacity={0.9} />
                )}
              </g>
            );
          })}
        </g>
        {/* ── end pan/zoom group ───────────────────────────────────────────── */}

        {/* Vignette */}
        <rect x={0} y={0} width={W} height={H} fill="url(#vg)" />
      </svg>
    </div>
  );
}
