'use client';

/**
 * /animation-preview
 *
 * Side-by-side preview of all 6 WorldMapAnimation themes.
 * Pick your favourite, then we apply it to the main landing page.
 */

import React, { useState } from 'react';
import { WorldMapAnimation, THEMES, ThemeKey } from '@/presentation/components/features/landing/WorldMapAnimation';

export default function AnimationPreviewPage() {
  const [fullscreen, setFullscreen] = useState<ThemeKey | null>(null);

  const fullTheme = fullscreen ? THEMES.find(t => t.key === fullscreen) : null;

  return (
    <div className="min-h-screen bg-gray-950 text-white p-6">
      {/* Header */}
      <div className="max-w-7xl mx-auto mb-8">
        <h1 className="text-3xl font-bold text-white mb-2">
          🗺️ Animation Theme Preview
        </h1>
        <p className="text-gray-400 text-sm">
          All 6 themes are running live. Click any card to view it fullscreen. Tell us which one you want for the landing page.
        </p>
        <div className="mt-3 inline-flex items-center gap-2 px-3 py-1.5 bg-gray-800 rounded-lg text-xs text-gray-300">
          <span className="w-2 h-2 rounded-full bg-green-400 animate-pulse inline-block" />
          Animation loops continuously: World → Sri Lanka (5 cities) → USA (13 hubs) → World
        </div>
      </div>

      {/* 2×3 Grid */}
      <div className="max-w-7xl mx-auto grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">
        {THEMES.map((theme, idx) => (
          <div
            key={theme.key}
            className="rounded-2xl overflow-hidden border border-gray-700 hover:border-gray-400 transition-all cursor-pointer group shadow-xl hover:shadow-2xl hover:scale-[1.02]"
            onClick={() => setFullscreen(theme.key)}
          >
            {/* Map preview — fixed height */}
            <div className="relative h-56">
              <WorldMapAnimation theme={theme} className="absolute inset-0" />
              {/* Fullscreen hint */}
              <div className="absolute top-3 right-3 opacity-0 group-hover:opacity-100 transition-opacity">
                <span className="bg-black/60 text-white text-xs px-2 py-1 rounded-md backdrop-blur-sm">
                  Click for fullscreen
                </span>
              </div>
              {/* Theme number badge */}
              <div className="absolute top-3 left-3">
                <span
                  className="text-xs font-bold px-2.5 py-1 rounded-full"
                  style={{ background: 'rgba(0,0,0,0.5)', color: theme.textColor, border: `1px solid ${theme.nodeFill}` }}
                >
                  #{idx + 1}
                </span>
              </div>
            </div>

            {/* Theme info */}
            <div className="p-4" style={{ background: 'rgba(0,0,0,0.7)' }}>
              <div className="flex items-center justify-between mb-1">
                <h3 className="text-sm font-semibold text-white">{theme.name}</h3>
                <div className="flex gap-1.5">
                  <span className="w-3 h-3 rounded-full border border-gray-600" style={{ background: theme.nodeFill }} title="Node color" />
                  <span className="w-3 h-3 rounded-full border border-gray-600" style={{ background: theme.lineStroke }} title="Line color" />
                  <span className="w-3 h-3 rounded-full border border-gray-600" style={{ background: theme.beamStroke }} title="Beam color" />
                </div>
              </div>
              <p className="text-xs text-gray-400">{theme.tagline}</p>
            </div>
          </div>
        ))}
      </div>

      {/* Legend */}
      <div className="max-w-7xl mx-auto mt-8 p-4 bg-gray-900 rounded-xl border border-gray-800 text-xs text-gray-400">
        <div className="flex flex-wrap gap-6">
          <div className="flex items-center gap-2">
            <span className="w-2 h-2 rounded-full bg-white inline-block" />
            Sri Lanka: Colombo, Kandy, Galle, Jaffna, Trincomalee
          </div>
          <div className="flex items-center gap-2">
            <span className="w-2 h-2 rounded-full bg-white inline-block" />
            USA: CA, NY, NJ, TX, VA, IL, WA, OH, MA, FL, GA, MD, PA
          </div>
          <div className="flex items-center gap-2">
            <span className="w-6 h-0.5 bg-gray-400 inline-block" />
            Beam: Colombo → New York
          </div>
        </div>
      </div>

      {/* Fullscreen Modal */}
      {fullscreen && fullTheme && (
        <div
          className="fixed inset-0 z-50 flex flex-col"
          style={{ background: fullTheme.bg }}
          onClick={() => setFullscreen(null)}
        >
          <WorldMapAnimation theme={fullTheme} className="absolute inset-0" />

          {/* Overlay info */}
          <div className="relative z-10 p-6 flex items-start justify-between">
            <div>
              <div
                className="inline-flex items-center gap-2 px-4 py-2 rounded-full backdrop-blur-sm border mb-3"
                style={{ borderColor: fullTheme.nodeFill, background: 'rgba(0,0,0,0.4)', color: fullTheme.textColor }}
              >
                <span className="text-sm font-semibold">Theme #{THEMES.findIndex(t => t.key === fullscreen) + 1}: {fullTheme.name}</span>
              </div>
              <p className="text-xs opacity-60" style={{ color: fullTheme.textColor }}>{fullTheme.tagline}</p>
            </div>
            <button
              className="px-4 py-2 rounded-lg text-sm font-medium backdrop-blur-sm border"
              style={{ borderColor: fullTheme.nodeFill, background: 'rgba(0,0,0,0.5)', color: fullTheme.textColor }}
              onClick={() => setFullscreen(null)}
            >
              ✕ Close
            </button>
          </div>

          {/* Sample overlay text — shows how content would look on top */}
          <div className="relative z-10 flex-1 flex flex-col items-center justify-center text-center px-4 pointer-events-none">
            <p
              className="text-xs font-medium tracking-widest uppercase mb-4 opacity-70"
              style={{ color: fullTheme.nodeFill }}
            >
              Connecting Sri Lankans Worldwide
            </p>
            <h1
              className="text-5xl md:text-7xl font-bold mb-6 drop-shadow-lg"
              style={{ color: fullTheme.textColor }}
            >
              LankaConnect
            </h1>
            <p
              className="text-xl md:text-2xl font-light opacity-80 mb-2"
              style={{ color: fullTheme.textColor }}
            >
              One Country, One Community
            </p>
            <p
              className="text-sm opacity-50 max-w-md"
              style={{ color: fullTheme.textColor }}
            >
              Join the largest Sri Lankan community platform.
            </p>
          </div>

          <div className="relative z-10 p-4 text-center">
            <p className="text-xs opacity-40" style={{ color: fullTheme.textColor }}>
              Click anywhere to close
            </p>
          </div>
        </div>
      )}
    </div>
  );
}
