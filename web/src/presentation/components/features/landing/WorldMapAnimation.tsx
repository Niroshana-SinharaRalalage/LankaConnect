'use client';

/**
 * WorldMapAnimation
 *
 * Animated SVG world map that sequences:
 * 1. World view fades in
 * 2. Zoom/pan to Sri Lanka → city dots pulse in
 * 3. Connection lines draw between Sri Lankan cities
 * 4. Beam shoots from Sri Lanka to USA
 * 5. Zoom/pan to USA → diaspora hub dots pulse in
 * 6. Connection lines draw between US hubs
 * 7. Zoom out to world view (both countries connected)
 * 8. Pause → loop
 *
 * Supports 6 visual themes.
 */

import React, { useEffect, useRef, useState } from 'react';
import {
  ComposableMap,
  Geographies,
  Geography,
  Marker,
  Line,
  ZoomableGroup,
} from 'react-simple-maps';
import { motion, AnimatePresence } from 'framer-motion';

// ─── GeoJSON for world map ────────────────────────────────────────────────────
const GEO_URL = 'https://cdn.jsdelivr.net/npm/world-atlas@2/countries-110m.json';

// ─── Location data ────────────────────────────────────────────────────────────
const SRI_LANKA_CITIES = [
  { name: 'Colombo',      coords: [79.8612, 6.9271]  as [number,number] },
  { name: 'Kandy',        coords: [80.6337, 7.2906]  as [number,number] },
  { name: 'Galle',        coords: [80.2170, 6.0535]  as [number,number] },
  { name: 'Jaffna',       coords: [80.0255, 9.6615]  as [number,number] },
  { name: 'Trincomalee',  coords: [81.2152, 8.5874]  as [number,number] },
];

const US_HUBS = [
  { name: 'California',    coords: [-119.4179, 36.7783] as [number,number] },
  { name: 'New York',      coords: [-74.0060,  40.7128] as [number,number] },
  { name: 'New Jersey',    coords: [-74.4057,  40.0583] as [number,number] },
  { name: 'Texas',         coords: [-99.9018,  31.9686] as [number,number] },
  { name: 'Virginia',      coords: [-78.6569,  37.4316] as [number,number] },
  { name: 'Illinois',      coords: [-89.3985,  40.6331] as [number,number] },
  { name: 'Washington',    coords: [-120.7401, 47.7511] as [number,number] },
  { name: 'Ohio',          coords: [-82.9071,  40.4173] as [number,number] },
  { name: 'Massachusetts', coords: [-71.3824,  42.4072] as [number,number] },
  { name: 'Florida',       coords: [-81.5158,  27.6648] as [number,number] },
  { name: 'Georgia',       coords: [-83.6431,  32.1656] as [number,number] },
  { name: 'Maryland',      coords: [-76.6413,  39.0458] as [number,number] },
  { name: 'Pennsylvania',  coords: [-77.1945,  41.2033] as [number,number] },
];

// Connection lines between SL cities
const SL_LINES: [number, number][][] = [
  [SRI_LANKA_CITIES[0].coords, SRI_LANKA_CITIES[1].coords],
  [SRI_LANKA_CITIES[0].coords, SRI_LANKA_CITIES[2].coords],
  [SRI_LANKA_CITIES[1].coords, SRI_LANKA_CITIES[3].coords],
  [SRI_LANKA_CITIES[1].coords, SRI_LANKA_CITIES[4].coords],
];

// Connection lines between US hubs
const US_LINES: [number, number][][] = [
  [US_HUBS[1].coords, US_HUBS[2].coords],
  [US_HUBS[1].coords, US_HUBS[4].coords],
  [US_HUBS[1].coords, US_HUBS[7].coords],
  [US_HUBS[0].coords, US_HUBS[6].coords],
  [US_HUBS[3].coords, US_HUBS[9].coords],
  [US_HUBS[4].coords, US_HUBS[11].coords],
  [US_HUBS[7].coords, US_HUBS[5].coords],
  [US_HUBS[8].coords, US_HUBS[12].coords],
  [US_HUBS[9].coords, US_HUBS[10].coords],
];

// The transcontinental beam: Colombo → New York
const BEAM_LINE: [number, number][] = [SRI_LANKA_CITIES[0].coords, US_HUBS[1].coords];

// ─── Theme definitions ────────────────────────────────────────────────────────
export type ThemeKey = 'sunrise-brand' | 'brand-dark' | 'satellite-navy' | 'deep-space' | 'emerald-night' | 'ceylon-gold';

export interface MapTheme {
  key: ThemeKey;
  name: string;
  bg: string;               // CSS background (gradient or solid)
  landFill: string;
  landStroke: string;
  oceanFill: string;
  nodeFill: string;
  nodeGlow: string;         // drop-shadow color
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
    landFill: '#fed7aa',
    landStroke: '#fb923c',
    oceanFill: '#e0f2fe',
    nodeFill: '#FF7900',
    nodeGlow: 'rgba(255,121,0,0.6)',
    lineStroke: '#FF7900',
    lineGlow: 'rgba(255,121,0,0.4)',
    beamStroke: '#059669',
    beamGlow: 'rgba(5,150,105,0.5)',
    textColor: '#1a1a1a',
    tagline: 'Warm & Brand-Aligned',
  },
  {
    key: 'brand-dark',
    name: 'Brand Dark',
    bg: 'linear-gradient(135deg, #1a0a00 0%, #2d1206 50%, #0a1a0f 100%)',
    landFill: '#2d1f0a',
    landStroke: '#FF7900',
    oceanFill: '#0f1f1a',
    nodeFill: '#FF7900',
    nodeGlow: 'rgba(255,121,0,0.8)',
    lineStroke: '#FF7900',
    lineGlow: 'rgba(255,121,0,0.5)',
    beamStroke: '#10b981',
    beamGlow: 'rgba(16,185,129,0.7)',
    textColor: '#ffffff',
    tagline: 'Dark & Brand-Aligned',
  },
  {
    key: 'satellite-navy',
    name: 'Satellite Navy',
    bg: 'linear-gradient(135deg, #020818 0%, #0a0f2e 50%, #030d1a 100%)',
    landFill: '#1a2744',
    landStroke: '#1e40af',
    oceanFill: '#030d1a',
    nodeFill: '#00e5ff',
    nodeGlow: 'rgba(0,229,255,0.9)',
    lineStroke: '#00e5ff',
    lineGlow: 'rgba(0,229,255,0.5)',
    beamStroke: '#00ffcc',
    beamGlow: 'rgba(0,255,204,0.8)',
    textColor: '#e0f7ff',
    tagline: 'Satellite / Globe Style',
  },
  {
    key: 'deep-space',
    name: 'Deep Space',
    bg: 'linear-gradient(135deg, #050510 0%, #0d0b1e 50%, #050510 100%)',
    landFill: '#1a1530',
    landStroke: '#4c1d95',
    oceanFill: '#050510',
    nodeFill: '#fbbf24',
    nodeGlow: 'rgba(251,191,36,0.9)',
    lineStroke: '#a78bfa',
    lineGlow: 'rgba(167,139,250,0.5)',
    beamStroke: '#fbbf24',
    beamGlow: 'rgba(251,191,36,0.8)',
    textColor: '#f3f0ff',
    tagline: 'Deep Space Premium',
  },
  {
    key: 'emerald-night',
    name: 'Emerald Night',
    bg: 'linear-gradient(135deg, #020c06 0%, #0a1a0f 50%, #051208 100%)',
    landFill: '#14311a',
    landStroke: '#15803d',
    oceanFill: '#020c06',
    nodeFill: '#00ff88',
    nodeGlow: 'rgba(0,255,136,0.9)',
    lineStroke: '#00ff88',
    lineGlow: 'rgba(0,255,136,0.4)',
    beamStroke: '#34d399',
    beamGlow: 'rgba(52,211,153,0.8)',
    textColor: '#d1fae5',
    tagline: 'Sri Lanka Forest / Emerald',
  },
  {
    key: 'ceylon-gold',
    name: 'Ceylon Gold',
    bg: 'linear-gradient(135deg, #1a0505 0%, #2d0a0a 50%, #1a0505 100%)',
    landFill: '#3b0d0d',
    landStroke: '#7f1d1d',
    oceanFill: '#0d0505',
    nodeFill: '#fbbf24',
    nodeGlow: 'rgba(251,191,36,0.9)',
    lineStroke: '#f59e0b',
    lineGlow: 'rgba(245,158,11,0.5)',
    beamStroke: '#fde68a',
    beamGlow: 'rgba(253,230,138,0.8)',
    textColor: '#fef3c7',
    tagline: 'Sri Lanka Flag Inspired',
  },
];

// ─── Animation phases ─────────────────────────────────────────────────────────
type Phase =
  | 'world'        // full world view
  | 'zoom-sl'      // zooming to Sri Lanka
  | 'sl-cities'    // city dots appearing
  | 'sl-lines'     // lines connecting SL cities
  | 'beam'         // beam SL → USA
  | 'zoom-us'      // zooming to USA
  | 'us-hubs'      // US hub dots appearing
  | 'us-lines'     // lines connecting US hubs
  | 'zoom-out'     // zoom out to world
  | 'pause';       // brief pause before loop

const PHASE_DURATIONS: Record<Phase, number> = {
  world:     2000,
  'zoom-sl': 2000,
  'sl-cities': 2500,
  'sl-lines':  2000,
  beam:      2500,
  'zoom-us': 2000,
  'us-hubs': 2500,
  'us-lines': 2000,
  'zoom-out': 2000,
  pause:     1500,
};

const PHASE_ORDER: Phase[] = [
  'world', 'zoom-sl', 'sl-cities', 'sl-lines',
  'beam', 'zoom-us', 'us-hubs', 'us-lines',
  'zoom-out', 'pause',
];

// Zoom configs per phase
interface ZoomConfig { center: [number, number]; zoom: number; }

function getZoom(phase: Phase): ZoomConfig {
  switch (phase) {
    case 'zoom-sl':
    case 'sl-cities':
    case 'sl-lines':
      return { center: [80.7, 7.9], zoom: 14 };
    case 'beam':
      return { center: [30, 25], zoom: 1.2 };
    case 'zoom-us':
    case 'us-hubs':
    case 'us-lines':
      return { center: [-95, 37], zoom: 3.5 };
    case 'zoom-out':
    case 'pause':
      return { center: [0, 20], zoom: 1 };
    default:
      return { center: [0, 20], zoom: 1 };
  }
}

// ─── Animated connection line ─────────────────────────────────────────────────
interface AnimatedLineProps {
  from: [number, number];
  to: [number, number];
  stroke: string;
  glow: string;
  visible: boolean;
  delay?: number;
  strokeWidth?: number;
}

function AnimatedLine({ from, to, stroke, glow, visible, delay = 0, strokeWidth = 1 }: AnimatedLineProps) {
  return (
    <Line
      from={from}
      to={to}
      stroke={stroke}
      strokeWidth={strokeWidth}
      strokeLinecap="round"
      style={{
        filter: `drop-shadow(0 0 4px ${glow})`,
        opacity: visible ? 1 : 0,
        transition: `opacity 0.6s ease ${delay}ms`,
        strokeDasharray: '1000',
        strokeDashoffset: visible ? '0' : '1000',
        transitionProperty: 'opacity, stroke-dashoffset',
      }}
    />
  );
}

// ─── Pulsing node ─────────────────────────────────────────────────────────────
interface PulseNodeProps {
  coords: [number, number];
  fill: string;
  glow: string;
  visible: boolean;
  delay?: number;
  radius?: number;
}

function PulseNode({ coords, fill, glow, visible, delay = 0, radius = 2 }: PulseNodeProps) {
  return (
    <Marker coordinates={coords}>
      <motion.circle
        r={radius}
        fill={fill}
        initial={{ scale: 0, opacity: 0 }}
        animate={visible ? { scale: [0, 1.5, 1], opacity: 1 } : { scale: 0, opacity: 0 }}
        transition={{ duration: 0.6, delay: delay / 1000, ease: 'easeOut' }}
        style={{ filter: `drop-shadow(0 0 6px ${glow})` }}
      />
      {visible && (
        <motion.circle
          r={radius * 2}
          fill="transparent"
          stroke={fill}
          strokeWidth={0.5}
          initial={{ scale: 0.5, opacity: 0.8 }}
          animate={{ scale: 3, opacity: 0 }}
          transition={{ duration: 1.5, delay: delay / 1000, repeat: Infinity, ease: 'easeOut' }}
        />
      )}
    </Marker>
  );
}

// ─── Main component ───────────────────────────────────────────────────────────
interface WorldMapAnimationProps {
  theme: MapTheme;
  className?: string;
}

export function WorldMapAnimation({ theme, className = '' }: WorldMapAnimationProps) {
  const [phase, setPhase] = useState<Phase>('world');
  const phaseRef = useRef<Phase>('world');
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    function advance() {
      const currentIndex = PHASE_ORDER.indexOf(phaseRef.current);
      const nextIndex = (currentIndex + 1) % PHASE_ORDER.length;
      const next = PHASE_ORDER[nextIndex];
      phaseRef.current = next;
      setPhase(next);
      timerRef.current = setTimeout(advance, PHASE_DURATIONS[next]);
    }
    timerRef.current = setTimeout(advance, PHASE_DURATIONS['world']);
    return () => { if (timerRef.current) clearTimeout(timerRef.current); };
  }, []);

  const zoomConfig = getZoom(phase);

  const showSLCities = ['sl-cities','sl-lines','beam','zoom-out','pause'].includes(phase);
  const showSLLines  = ['sl-lines','beam','zoom-out','pause'].includes(phase);
  const showBeam     = ['beam','zoom-us','us-hubs','us-lines','zoom-out','pause'].includes(phase);
  const showUSHubs   = ['us-hubs','us-lines','zoom-out','pause'].includes(phase);
  const showUSLines  = ['us-lines','zoom-out','pause'].includes(phase);

  return (
    <div
      className={`w-full h-full ${className}`}
      style={{ background: theme.bg }}
    >
      <ComposableMap
        width={900}
        height={500}
        style={{ width: '100%', height: '100%' }}
        projection="geoMercator"
      >
        <ZoomableGroup
          center={zoomConfig.center}
          zoom={zoomConfig.zoom}
          // smooth transition via CSS on the <g> inside
        >
          {/* Ocean background */}
          <rect x="-10000" y="-10000" width="20000" height="20000" fill={theme.oceanFill} />

          {/* Land */}
          <Geographies geography={GEO_URL}>
            {({ geographies }: { geographies: Array<{ rsmKey: string; [key: string]: unknown }> }) =>
              geographies.map((geo) => (
                <Geography
                  key={geo.rsmKey}
                  geography={geo}
                  fill={theme.landFill}
                  stroke={theme.landStroke}
                  strokeWidth={0.3}
                  style={{
                    default: { outline: 'none' },
                    hover:   { outline: 'none' },
                    pressed: { outline: 'none' },
                  }}
                />
              ))
            }
          </Geographies>

          {/* Sri Lanka city connections */}
          {SL_LINES.map((line, i) => (
            <AnimatedLine
              key={`sl-line-${i}`}
              from={line[0] as [number,number]}
              to={line[1] as [number,number]}
              stroke={theme.lineStroke}
              glow={theme.lineGlow}
              visible={showSLLines}
              delay={i * 250}
              strokeWidth={0.8}
            />
          ))}

          {/* Transcontinental beam */}
          <AnimatedLine
            from={BEAM_LINE[0] as [number,number]}
            to={BEAM_LINE[1] as [number,number]}
            stroke={theme.beamStroke}
            glow={theme.beamGlow}
            visible={showBeam}
            strokeWidth={1.2}
          />

          {/* US hub connections */}
          {US_LINES.map((line, i) => (
            <AnimatedLine
              key={`us-line-${i}`}
              from={line[0] as [number,number]}
              to={line[1] as [number,number]}
              stroke={theme.lineStroke}
              glow={theme.lineGlow}
              visible={showUSLines}
              delay={i * 200}
              strokeWidth={0.8}
            />
          ))}

          {/* Sri Lanka city nodes */}
          {SRI_LANKA_CITIES.map((city, i) => (
            <PulseNode
              key={city.name}
              coords={city.coords}
              fill={theme.nodeFill}
              glow={theme.nodeGlow}
              visible={showSLCities}
              delay={i * 300}
              radius={phase === 'sl-cities' || phase === 'sl-lines' || phase === 'zoom-sl' ? 3 : 1.5}
            />
          ))}

          {/* US hub nodes */}
          {US_HUBS.map((hub, i) => (
            <PulseNode
              key={hub.name}
              coords={hub.coords}
              fill={theme.nodeFill}
              glow={theme.nodeGlow}
              visible={showUSHubs}
              delay={i * 200}
              radius={phase === 'us-hubs' || phase === 'us-lines' || phase === 'zoom-us' ? 3 : 1.5}
            />
          ))}
        </ZoomableGroup>
      </ComposableMap>
    </div>
  );
}
