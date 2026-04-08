'use client';

/**
 * /lanka-events-logos
 * Logo concept preview for LankaEvents.
 * 4 distinct SVG designs — pick one to adopt as the official logo.
 */

import React, { useState } from 'react';
import Link from 'next/link';

// ─── Logo A: Calendar Lanka ───────────────────────────────────────────────────
// Modern calendar with the Sri Lanka island silhouette as the focal date mark.
function LogoA({ size = 80 }: { size?: number }) {
  return (
    <svg width={size} height={size} viewBox="0 0 80 80" fill="none" xmlns="http://www.w3.org/2000/svg">
      <defs>
        <linearGradient id="la-bg" x1="0" y1="0" x2="80" y2="80" gradientUnits="userSpaceOnUse">
          <stop offset="0%" stopColor="#FF9A3C" />
          <stop offset="100%" stopColor="#FF7900" />
        </linearGradient>
        <linearGradient id="la-header" x1="0" y1="0" x2="80" y2="20" gradientUnits="userSpaceOnUse">
          <stop offset="0%" stopColor="#8B1A1A" />
          <stop offset="100%" stopColor="#5c0d0d" />
        </linearGradient>
        <filter id="la-shadow">
          <feDropShadow dx="0" dy="3" stdDeviation="4" floodColor="#FF7900" floodOpacity="0.4" />
        </filter>
      </defs>
      {/* Calendar body */}
      <rect x="6" y="14" width="68" height="58" rx="10" fill="url(#la-bg)" filter="url(#la-shadow)" />
      {/* Header bar */}
      <rect x="6" y="14" width="68" height="20" rx="10" fill="url(#la-header)" />
      <rect x="6" y="24" width="68" height="10" fill="url(#la-header)" />
      {/* Calendar ring pegs */}
      <rect x="22" y="8" width="7" height="14" rx="3.5" fill="#fff" opacity="0.9" />
      <rect x="51" y="8" width="7" height="14" rx="3.5" fill="#fff" opacity="0.9" />
      {/* Header text dots (decorative) */}
      <circle cx="27" cy="20" r="1.5" fill="rgba(255,255,255,0.5)" />
      <circle cx="40" cy="20" r="1.5" fill="rgba(255,255,255,0.5)" />
      <circle cx="53" cy="20" r="1.5" fill="rgba(255,255,255,0.5)" />
      {/* Sri Lanka island silhouette — simplified teardrop/mango shape */}
      <g transform="translate(40,49)">
        <path
          d="M0,-16 C5,-14 10,-8 10,0 C10,6 7,12 3,15 C1,16 -1,16 -3,15 C-7,12 -10,6 -10,0 C-10,-8 -5,-14 0,-16 Z"
          fill="white"
          opacity="0.95"
        />
        {/* Internal detail lines — rivers/roads suggestion */}
        <path d="M0,-10 C1,-4 2,2 1,10" stroke="#FF7900" strokeWidth="1.2" strokeLinecap="round" opacity="0.6" />
        <path d="M-4,-4 C-1,-2 2,0 4,0" stroke="#FF7900" strokeWidth="0.9" strokeLinecap="round" opacity="0.5" />
      </g>
      {/* Bottom grid dots */}
      {[0,1,2,3,4,5].map(i => (
        <circle key={i} cx={15 + i * 11} cy={74} r="1.5" fill="rgba(255,255,255,0.4)" />
      ))}
    </svg>
  );
}

// ─── Logo B: Event Pin ────────────────────────────────────────────────────────
// Location pin with a star/spark inside — events happen at a place.
function LogoB({ size = 80 }: { size?: number }) {
  return (
    <svg width={size} height={size} viewBox="0 0 80 80" fill="none" xmlns="http://www.w3.org/2000/svg">
      <defs>
        <radialGradient id="lb-pin" cx="50%" cy="35%" r="60%">
          <stop offset="0%" stopColor="#FF9A3C" />
          <stop offset="60%" stopColor="#FF7900" />
          <stop offset="100%" stopColor="#cc5500" />
        </radialGradient>
        <radialGradient id="lb-inner" cx="50%" cy="40%" r="50%">
          <stop offset="0%" stopColor="#fff" stopOpacity="0.95" />
          <stop offset="100%" stopColor="#ffe4c4" stopOpacity="0.85" />
        </radialGradient>
        <filter id="lb-glow">
          <feGaussianBlur stdDeviation="3" result="blur" />
          <feMerge><feMergeNode in="blur" /><feMergeNode in="SourceGraphic" /></feMerge>
        </filter>
        <linearGradient id="lb-maroon" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor="#9B1C1C" />
          <stop offset="100%" stopColor="#5c0d0d" />
        </linearGradient>
      </defs>
      {/* Pin drop shadow */}
      <ellipse cx="40" cy="76" rx="10" ry="3" fill="rgba(0,0,0,0.3)" />
      {/* Pin body */}
      <path
        d="M40 8 C24 8 12 20 12 33 C12 50 40 72 40 72 C40 72 68 50 68 33 C68 20 56 8 40 8 Z"
        fill="url(#lb-pin)"
        filter="url(#lb-glow)"
      />
      {/* Pin inner circle */}
      <circle cx="40" cy="33" r="16" fill="url(#lb-maroon)" />
      <circle cx="40" cy="33" r="14" fill="url(#lb-inner)" />
      {/* Star burst inside */}
      <g transform="translate(40,33)">
        {[0,45,90,135,180,225,270,315].map((angle, i) => {
          const rad = (angle * Math.PI) / 180;
          const r1 = i % 2 === 0 ? 9 : 5;
          const x = Math.cos(rad - Math.PI / 2) * r1;
          const y = Math.sin(rad - Math.PI / 2) * r1;
          return <circle key={i} cx={x} cy={y} r="1.2" fill="#FF7900" opacity="0.8" />;
        })}
        {/* Central calendar icon */}
        <rect x="-5" y="-6" width="10" height="9" rx="2" fill="#FF7900" />
        <rect x="-5" y="-6" width="10" height="3" rx="1" fill="#8B1A1A" />
        <rect x="-2.5" y="-8" width="2" height="4" rx="1" fill="#8B1A1A" />
        <rect x="2" y="-8" width="2" height="4" rx="1" fill="#8B1A1A" />
        <circle cx="0" cy="1.5" r="1.5" fill="white" />
      </g>
    </svg>
  );
}

// ─── Logo C: Lotus Calendar ───────────────────────────────────────────────────
// Lotus flower (Sri Lanka national flower) whose petals form a calendar circle.
function LogoC({ size = 80 }: { size?: number }) {
  return (
    <svg width={size} height={size} viewBox="0 0 80 80" fill="none" xmlns="http://www.w3.org/2000/svg">
      <defs>
        <linearGradient id="lc-petal" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor="#FF9A3C" />
          <stop offset="100%" stopColor="#FF7900" />
        </linearGradient>
        <linearGradient id="lc-inner-petal" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor="#cc5500" />
          <stop offset="100%" stopColor="#8B1A1A" />
        </linearGradient>
        <radialGradient id="lc-center" cx="50%" cy="50%" r="50%">
          <stop offset="0%" stopColor="#fff" />
          <stop offset="50%" stopColor="#ffe4c4" />
          <stop offset="100%" stopColor="#FF7900" />
        </radialGradient>
        <filter id="lc-glow">
          <feGaussianBlur stdDeviation="2.5" result="blur" />
          <feMerge><feMergeNode in="blur" /><feMergeNode in="SourceGraphic" /></feMerge>
        </filter>
      </defs>
      {/* Outer petals — 8 petals */}
      {Array.from({ length: 8 }, (_, i) => {
        const angle = (i * 45 * Math.PI) / 180;
        const cx = 40 + Math.cos(angle) * 26;
        const cy = 40 + Math.sin(angle) * 26;
        return (
          <ellipse
            key={i}
            cx={cx} cy={cy}
            rx="8" ry="12"
            fill="url(#lc-petal)"
            opacity="0.85"
            transform={`rotate(${i * 45 + 90}, ${cx}, ${cy})`}
          />
        );
      })}
      {/* Inner petals — 8 petals offset */}
      {Array.from({ length: 8 }, (_, i) => {
        const angle = ((i * 45 + 22.5) * Math.PI) / 180;
        const cx = 40 + Math.cos(angle) * 17;
        const cy = 40 + Math.sin(angle) * 17;
        return (
          <ellipse
            key={i}
            cx={cx} cy={cy}
            rx="5.5" ry="9"
            fill="url(#lc-inner-petal)"
            opacity="0.9"
            transform={`rotate(${i * 45 + 112.5}, ${cx}, ${cy})`}
          />
        );
      })}
      {/* Center disc */}
      <circle cx="40" cy="40" r="13" fill="url(#lc-center)" filter="url(#lc-glow)" />
      {/* Mini calendar in center */}
      <rect x="32" y="34" width="16" height="13" rx="3" fill="#FF7900" />
      <rect x="32" y="34" width="16" height="5" rx="2" fill="#8B1A1A" />
      <rect x="36" y="31" width="2.5" height="5" rx="1.2" fill="#8B1A1A" />
      <rect x="41.5" y="31" width="2.5" height="5" rx="1.2" fill="#8B1A1A" />
      {/* Calendar date dots */}
      <circle cx="36" cy="41.5" r="1.5" fill="white" opacity="0.9" />
      <circle cx="40" cy="41.5" r="1.5" fill="white" opacity="0.9" />
      <circle cx="44" cy="41.5" r="1.5" fill="white" opacity="0.9" />
      <circle cx="36" cy="44.5" r="1" fill="white" opacity="0.5" />
      <circle cx="40" cy="44.5" r="1" fill="white" opacity="0.5" />
    </svg>
  );
}

// ─── Logo D: Ceylon Spark ─────────────────────────────────────────────────────
// Abstract "LE" monogram inside a dynamic spark/burst — bold, minimal, modern.
function LogoD({ size = 80 }: { size?: number }) {
  return (
    <svg width={size} height={size} viewBox="0 0 80 80" fill="none" xmlns="http://www.w3.org/2000/svg">
      <defs>
        <linearGradient id="ld-bg" x1="0" y1="0" x2="80" y2="80" gradientUnits="userSpaceOnUse">
          <stop offset="0%" stopColor="#1a0800" />
          <stop offset="100%" stopColor="#0d0400" />
        </linearGradient>
        <linearGradient id="ld-ring" x1="0" y1="0" x2="80" y2="80" gradientUnits="userSpaceOnUse">
          <stop offset="0%" stopColor="#FF9A3C" />
          <stop offset="50%" stopColor="#FF7900" />
          <stop offset="100%" stopColor="#8B1A1A" />
        </linearGradient>
        <radialGradient id="ld-glow" cx="50%" cy="50%" r="50%">
          <stop offset="0%" stopColor="#FF7900" stopOpacity="0.35" />
          <stop offset="100%" stopColor="#FF7900" stopOpacity="0" />
        </radialGradient>
        <filter id="ld-blur">
          <feGaussianBlur stdDeviation="4" result="blur" />
          <feMerge><feMergeNode in="blur" /><feMergeNode in="SourceGraphic" /></feMerge>
        </filter>
      </defs>
      {/* Dark circle base */}
      <circle cx="40" cy="40" r="38" fill="url(#ld-bg)" />
      {/* Ambient glow */}
      <circle cx="40" cy="40" r="38" fill="url(#ld-glow)" />
      {/* Outer ring */}
      <circle cx="40" cy="40" r="35" stroke="url(#ld-ring)" strokeWidth="2.5" fill="none" />
      {/* Spark rays — 12 short marks around ring */}
      {Array.from({ length: 12 }, (_, i) => {
        const angle = (i * 30 * Math.PI) / 180;
        const r1 = 29, r2 = i % 3 === 0 ? 23 : 26;
        const x1 = 40 + Math.cos(angle) * r1;
        const y1 = 40 + Math.sin(angle) * r1;
        const x2 = 40 + Math.cos(angle) * r2;
        const y2 = 40 + Math.sin(angle) * r2;
        return (
          <line key={i} x1={x1} y1={y1} x2={x2} y2={y2}
            stroke={i % 3 === 0 ? '#FF7900' : '#FF7900'} strokeWidth={i % 3 === 0 ? 2 : 1}
            strokeLinecap="round" opacity={i % 3 === 0 ? 1 : 0.45}
          />
        );
      })}
      {/* "LE" monogram */}
      <g fill="none" strokeLinecap="round" strokeLinejoin="round">
        {/* L */}
        <path d="M24 28 L24 52 L34 52" stroke="#FF7900" strokeWidth="5" />
        {/* E */}
        <path d="M40 28 L40 52" stroke="#FF7900" strokeWidth="5" />
        <path d="M40 28 L54 28" stroke="#FF7900" strokeWidth="5" />
        <path d="M40 40 L51 40" stroke="#FF7900" strokeWidth="5" />
        <path d="M40 52 L54 52" stroke="#FF7900" strokeWidth="5" />
      </g>
      {/* Highlight dot top-right */}
      <circle cx="57" cy="23" r="3" fill="#FF7900" filter="url(#ld-blur)" />
      <circle cx="57" cy="23" r="1.8" fill="white" opacity="0.9" />
    </svg>
  );
}

// ─── Preview page ─────────────────────────────────────────────────────────────
const LOGOS = [
  {
    id: 'A',
    name: 'Calendar Lanka',
    component: LogoA,
    desc: 'Calendar body with Sri Lanka island silhouette as the featured date. Warm orange body, maroon header bar. Professional and immediately recognisable as both a calendar app and Sri Lankan.',
    verdict: 'Best for: App icon / favicon — clear calendar metaphor',
  },
  {
    id: 'B',
    name: 'Event Pin',
    component: LogoB,
    desc: 'Location pin drop shape with a calendar/star detail inside. Orange gradient with maroon inner ring. Communicates "events at a place" — ideal for a community events platform.',
    verdict: 'Best for: Mobile app icon — strong shape, reads well small',
  },
  {
    id: 'C',
    name: 'Lotus Calendar',
    component: LogoC,
    desc: 'Lotus flower (Sri Lanka\'s national flower) whose petals radiate around a central calendar icon. Orange outer petals, maroon inner petals. Rich, cultural, celebratory.',
    verdict: 'Best for: Website hero / large displays — elaborate and eye-catching',
  },
  {
    id: 'D',
    name: 'Ceylon Spark',
    component: LogoD,
    desc: '"LE" monogram on a dark disc with a graduation/spark ring. Bold, modern, minimal. Works as a badge or avatar. Orange on dark — high contrast, striking.',
    verdict: 'Best for: Dark UI / nav bar — bold monogram, timeless',
  },
];

export default function LankaEventsLogosPage() {
  const [selected, setSelected] = useState<string | null>(null);
  const [previewSize, setPreviewSize] = useState(160);

  return (
    <div className="min-h-screen bg-gray-950 text-white p-6 md:p-10">
      <div className="max-w-6xl mx-auto">
        {/* Header */}
        <div className="mb-8">
          <Link href="/" className="text-xs text-gray-500 hover:text-gray-300 transition-colors mb-4 inline-block">← Back to landing</Link>
          <h1 className="text-3xl font-bold text-white mb-2">LankaEvents Logo Concepts</h1>
          <p className="text-gray-400 text-sm max-w-xl">
            4 logo concepts for LankaEvents. Each uses the LankaConnect brand palette — orange <span className="text-orange-400 font-semibold">#FF7900</span> primary + maroon <span className="text-red-800 font-semibold">#8B1A1A</span> secondary. Click a card to compare sizes.
          </p>
          <div className="mt-3 flex items-center gap-3">
            <span className="text-xs text-gray-500">Preview size:</span>
            {[80, 120, 160, 220].map(s => (
              <button key={s}
                onClick={() => setPreviewSize(s)}
                className={`px-3 py-1 rounded-lg text-xs font-medium transition-colors ${previewSize === s ? 'bg-orange-600 text-white' : 'bg-gray-800 text-gray-400 hover:bg-gray-700'}`}
              >{s}px</button>
            ))}
          </div>
        </div>

        {/* 2×2 Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {LOGOS.map(({ id, name, component: Logo, desc, verdict }) => (
            <div
              key={id}
              onClick={() => setSelected(selected === id ? null : id)}
              className={`rounded-2xl border p-6 cursor-pointer transition-all duration-200 ${
                selected === id
                  ? 'border-orange-500 bg-orange-950/30 scale-[1.01]'
                  : 'border-gray-800 bg-gray-900 hover:border-gray-600'
              }`}
            >
              {/* Logo preview */}
              <div className="flex items-center justify-center rounded-xl mb-5 py-8"
                style={{ background: 'linear-gradient(135deg, #111 0%, #1a0800 100%)' }}>
                <Logo size={previewSize} />
              </div>

              {/* Logo on light bg */}
              <div className="flex items-center justify-center rounded-xl mb-5 py-8 bg-gray-100">
                <Logo size={Math.min(previewSize, 120)} />
              </div>

              {/* Info */}
              <div className="flex items-start justify-between gap-3 mb-2">
                <div>
                  <span className="text-xs font-bold text-orange-500 tracking-widest uppercase">Option {id}</span>
                  <h3 className="text-lg font-bold text-white mt-0.5">{name}</h3>
                </div>
                {selected === id && (
                  <span className="flex-shrink-0 mt-1 px-3 py-1 rounded-full bg-orange-600 text-white text-xs font-bold">Selected</span>
                )}
              </div>
              <p className="text-sm text-gray-400 leading-relaxed mb-3">{desc}</p>
              <div className="text-xs text-orange-400/70 italic">{verdict}</div>

              {/* Small sizes strip */}
              <div className="mt-4 pt-4 border-t border-gray-800 flex items-center gap-4">
                <span className="text-xs text-gray-600">At small sizes:</span>
                {[16, 24, 32, 48].map(s => (
                  <Logo key={s} size={s} />
                ))}
              </div>
            </div>
          ))}
        </div>

        {/* Selection callout */}
        {selected && (
          <div className="mt-8 p-5 rounded-2xl border border-orange-500 bg-orange-950/20">
            <div className="flex items-center gap-3 mb-2">
              {React.createElement(LOGOS.find(l => l.id === selected)!.component, { size: 56 })}
              <div>
                <p className="text-sm font-semibold text-white">Option {selected}: {LOGOS.find(l => l.id === selected)?.name}</p>
                <p className="text-xs text-gray-400">Tell the team which option to adopt — they can then refine it in Figma/Illustrator.</p>
              </div>
            </div>
          </div>
        )}

        {/* Footer note */}
        <p className="mt-10 text-xs text-gray-600 text-center">
          These are vector SVG concepts. The chosen design can be refined with custom typography, fine-tuned proportions, and exported as SVG / PNG / ICO.
        </p>
      </div>
    </div>
  );
}
