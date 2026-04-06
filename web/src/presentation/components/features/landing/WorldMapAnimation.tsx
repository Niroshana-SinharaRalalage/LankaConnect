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

const SL_LINE_PAIRS = [[0, 1], [0, 2], [0, 3], [1, 4], [3, 4]];
const US_LINE_PAIRS = [[0, 6], [1, 2], [1, 4], [1, 8], [3, 9], [4, 12], [5, 7], [9, 10]];

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

  // Load world TopoJSON from /public
  useEffect(() => {
    fetch('/world-110m.json')
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
        console.warn('[WorldMapAnimation] Failed to load world-110m.json:', err);
      });
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
      className={`w-full h-full overflow-hidden ${className}`}
      style={{ background: theme.bg }}
    >
      <svg
        viewBox={`0 0 ${W} ${H}`}
        className="w-full h-full"
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
        </defs>

        {/* Ocean */}
        <rect x={0} y={0} width={W} height={H} fill={theme.oceanFill} />

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

          {/* SL city arcs */}
          {showSLLines && slArcs.map((d, i) => (
            <motion.path
              key={`sl-arc-${i}`}
              d={d}
              fill="none"
              stroke={theme.lineStroke}
              strokeWidth={strokeW * 0.9}
              strokeLinecap="round"
              filter={`url(#${filterId('gl')})`}
              initial={{ pathLength: 0, opacity: 0 }}
              animate={{ pathLength: 1, opacity: 0.9 }}
              transition={{ duration: 1.0, delay: i * 0.22 }}
            />
          ))}

          {/* Transcontinental beam */}
          {showBeam && (
            <motion.path
              key="beam"
              d={beamPath}
              fill="none"
              stroke={theme.beamStroke}
              strokeWidth={strokeW * 1.6}
              strokeLinecap="round"
              strokeDasharray={`${4 / z} ${2.5 / z}`}
              filter={`url(#${filterId('gb')})`}
              initial={{ pathLength: 0, opacity: 0 }}
              animate={{ pathLength: 1, opacity: 0.95 }}
              transition={{ duration: 2.4, ease: 'easeInOut' }}
            />
          )}

          {/* US hub arcs */}
          {showUSLines && usArcs.map((d, i) => (
            <motion.path
              key={`us-arc-${i}`}
              d={d}
              fill="none"
              stroke={theme.lineStroke}
              strokeWidth={strokeW * 0.9}
              strokeLinecap="round"
              filter={`url(#${filterId('gl')})`}
              initial={{ pathLength: 0, opacity: 0 }}
              animate={{ pathLength: 1, opacity: 0.9 }}
              transition={{ duration: 0.85, delay: i * 0.2 }}
            />
          ))}

          {/* Sri Lanka city nodes */}
          {slXY.map((xy, i) => (
            <g key={`sl-node-${i}`}>
              {showSLCities && (
                <>
                  {/* Outer pulse ring */}
                  <motion.circle
                    cx={xy[0]} cy={xy[1]}
                    r={nodeR * 5}
                    fill="none"
                    stroke={theme.nodeFill}
                    strokeWidth={0.3 / z}
                    initial={{ scale: 0.6, opacity: 0.8 }}
                    animate={{ scale: 2.2, opacity: 0 }}
                    transition={{ duration: 2.2, delay: i * 0.3, repeat: Infinity }}
                  />
                  {/* Inner ring */}
                  <motion.circle
                    cx={xy[0]} cy={xy[1]}
                    r={nodeR * 3}
                    fill="none"
                    stroke={theme.nodeFill}
                    strokeWidth={0.2 / z}
                    opacity={0.5}
                    initial={{ scale: 0.5, opacity: 0 }}
                    animate={{ scale: 1.8, opacity: 0 }}
                    transition={{ duration: 2.2, delay: i * 0.3 + 0.6, repeat: Infinity }}
                  />
                </>
              )}
              {/* Core dot */}
              <motion.circle
                cx={xy[0]} cy={xy[1]}
                r={nodeR * 1.8}
                fill={`url(#${filterId('ng')})`}
                filter={`url(#${filterId('gn')})`}
                initial={{ scale: 0, opacity: 0 }}
                animate={showSLCities ? { scale: 1, opacity: 1 } : { scale: 0, opacity: 0 }}
                transition={{ duration: 0.55, delay: i * 0.15 }}
              />
            </g>
          ))}

          {/* US hub nodes */}
          {usXY.map((xy, i) => (
            <g key={`us-node-${i}`}>
              {showUSHubs && (
                <motion.circle
                  cx={xy[0]} cy={xy[1]}
                  r={nodeR * 4}
                  fill="none"
                  stroke={theme.nodeFill}
                  strokeWidth={0.25 / z}
                  initial={{ scale: 0.6, opacity: 0.8 }}
                  animate={{ scale: 2.2, opacity: 0 }}
                  transition={{ duration: 2, delay: i * 0.18, repeat: Infinity }}
                />
              )}
              <motion.circle
                cx={xy[0]} cy={xy[1]}
                r={nodeR * 1.5}
                fill={`url(#${filterId('ng')})`}
                filter={`url(#${filterId('gn')})`}
                initial={{ scale: 0, opacity: 0 }}
                animate={showUSHubs ? { scale: 1, opacity: 1 } : { scale: 0, opacity: 0 }}
                transition={{ duration: 0.45, delay: i * 0.12 }}
              />
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
