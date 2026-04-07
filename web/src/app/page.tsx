'use client';

/**
 * LankaConnect Umbrella Landing Page
 *
 * Cinematic full-page animated world map background with the brand
 * identity centred and a single prominent LankaEvents entry point.
 *
 * Intentionally no top-left logo/branding — the hero IS the identity.
 * No "My Dashboard" in nav — that belongs in /lanka-events.
 */

import React, { useState, useEffect } from 'react';
import Link from 'next/link';
import Image from 'next/image';
import { Calendar, ChevronRight, Sparkles } from 'lucide-react';
import { WorldMapAnimation, THEMES } from '@/presentation/components/features/landing/WorldMapAnimation';
import { useAuthStore } from '@/presentation/store/useAuthStore';

// ─── Theme ────────────────────────────────────────────────────────────────────
const DEFAULT_THEME_KEY = 'satellite-navy';

// ─── Component ────────────────────────────────────────────────────────────────
export default function LankaConnectHome() {
  const { user } = useAuthStore();
  const [mounted, setMounted] = useState(false);
  useEffect(() => { setMounted(true); }, []);

  const theme = THEMES.find(t => t.key === DEFAULT_THEME_KEY) ?? THEMES[2];

  return (
    <div className="relative min-h-screen w-full overflow-hidden">

      {/* ── Full-page animated world map ──────────────────────────────── */}
      <div className="absolute inset-0 z-0">
        {mounted && <WorldMapAnimation theme={theme} className="w-full h-full" />}
      </div>

      {/* ── Subtle dark overlay — just enough for text legibility ─────── */}
      <div className="absolute inset-0 z-10 bg-black/20" />

      {/* ── Top nav — sign-in / join only, no dashboard ───────────────── */}
      <nav className="relative z-20 flex items-center justify-end px-6 py-4 md:px-10">
        {!user ? (
          <div className="flex items-center gap-3">
            <Link
              href="/login"
              className="px-4 py-2 rounded-lg text-sm font-medium text-white/80 hover:text-white transition-colors"
            >
              Sign In
            </Link>
            <Link
              href="/register"
              className="px-4 py-2 rounded-lg text-sm font-semibold text-white border transition-all hover:bg-white/10 backdrop-blur-sm"
              style={{ borderColor: theme.nodeFill, boxShadow: `0 0 12px ${theme.nodeFill}40` }}
            >
              Join Free
            </Link>
          </div>
        ) : (
          <div className="flex items-center gap-3">
            <Link
              href="/lanka-events"
              className="px-4 py-2 rounded-lg text-sm font-medium text-white/80 hover:text-white transition-colors"
            >
              Go to LankaEvents
            </Link>
          </div>
        )}
      </nav>

      {/* ── Hero ──────────────────────────────────────────────────────── */}
      <div className="relative z-20 flex flex-col items-center justify-center text-center px-6 pt-2 pb-8 md:pt-4 md:pb-12">

        {/* Brand identity — logo + name side by side, equal heights */}
        <div className="flex items-center gap-4 mb-5" style={{ filter: 'drop-shadow(0 4px 24px rgba(0,0,0,0.6))' }}>
          {/* Logo — 84px, defines the column height */}
          <Image
            src="/images/lankaconnect-logo.png"
            alt="LankaConnect"
            width={84}
            height={84}
            className="object-contain flex-shrink-0"
            priority
          />
          {/* Text column — same height as logo, title at top / subtitle at bottom */}
          <div className="flex flex-col justify-between items-start" style={{ height: '84px' }}>
            {/*
              "LankaConnect" (12 chars) and subtitle (24 chars, uppercase, spaced)
              are tuned to the same rendered width ≈ 185px via complementary
              font-size + letter-spacing values.
            */}
            <span
              className="font-semibold text-white leading-none"
              style={{
                fontSize: '27px',
                letterSpacing: '-0.01em',
                textShadow: '0 2px 16px rgba(0,0,0,0.55)',
              }}
            >
              LankaConnect
            </span>
            <span
              className="font-medium text-white/65 uppercase"
              style={{
                fontSize: '10.5px',
                letterSpacing: '0.215em',
              }}
            >
              Sri Lankan Community Hub
            </span>
          </div>
        </div>

        {/* Live indicator pill */}
        <div
          className="inline-flex items-center gap-2 px-4 py-1.5 rounded-full text-xs font-semibold mb-6 backdrop-blur-sm border"
          style={{
            borderColor: `${theme.nodeFill}55`,
            background: `${theme.nodeFill}1a`,
            color: theme.nodeFill,
          }}
        >
          <span className="w-1.5 h-1.5 rounded-full animate-pulse" style={{ background: theme.nodeFill }} />
          Connecting Sri Lankans Worldwide
        </div>

        {/* Main copy */}
        <h2
          className="text-3xl md:text-4xl font-bold text-white mb-4 drop-shadow-md leading-tight"
          style={{ textShadow: '0 2px 16px rgba(0,0,0,0.6)' }}
        >
          One Country,{' '}
          <span style={{ color: theme.nodeFill }}>One Community</span>
        </h2>
        <p
          className="text-base md:text-lg font-medium text-white/90 max-w-lg mb-10 leading-relaxed"
          style={{ textShadow: '0 1px 8px rgba(0,0,0,0.7)' }}
        >
          Join the largest Sri Lankan community platform. Discover events,
          connect with businesses, engage in discussions, and celebrate our
          rich culture together.
        </p>

        {/* ── LankaEvents — the ONLY live sub-brand, prominently featured ── */}
        <Link href="/lanka-events" className="w-full max-w-2xl group block">
          <div
            className="relative rounded-2xl p-7 md:p-8 border backdrop-blur-xl transition-all duration-300 hover:scale-[1.02] hover:shadow-2xl cursor-pointer text-left"
            style={{
              background: 'rgba(255,121,0,0.13)',
              borderColor: 'rgba(255,121,0,0.45)',
              boxShadow: '0 0 50px rgba(255,121,0,0.18), inset 0 1px 0 rgba(255,255,255,0.08)',
            }}
          >
            {/* Top row: badge + live indicator */}
            <div className="flex items-center gap-3 mb-4">
              <div
                className="flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-bold"
                style={{ background: 'rgba(255,121,0,0.25)', color: '#FF7900' }}
              >
                <Calendar className="h-3 w-3" />
                Event Planner
              </div>
              <div className="flex items-center gap-1.5">
                <span className="w-2 h-2 rounded-full bg-green-400 animate-pulse" />
                <span className="text-green-400 text-xs font-semibold">Live Now</span>
              </div>
            </div>

            {/* Main content */}
            <div className="flex flex-col sm:flex-row items-start sm:items-center gap-6">
              <div className="flex-1">
                <h3 className="text-3xl md:text-4xl font-extrabold text-white mb-2 tracking-tight">
                  LankaEvents
                </h3>
                <p className="text-white/70 text-sm md:text-base leading-relaxed mb-5">
                  Sri Lanka&apos;s premier event planning platform — discover, plan &amp; celebrate
                  Sri Lankan cultural events, festivals, and gatherings worldwide.
                </p>
                <div
                  className="inline-flex items-center gap-2 px-6 py-3 rounded-xl text-sm font-bold transition-all group-hover:gap-3"
                  style={{ background: '#FF7900', color: 'white', boxShadow: '0 4px 20px rgba(255,121,0,0.5)' }}
                >
                  <Sparkles className="h-4 w-4" />
                  Explore LankaEvents
                  <ChevronRight className="h-4 w-4 transition-transform group-hover:translate-x-1" />
                </div>
              </div>

              {/* Stats */}
              <div className="flex sm:flex-col gap-6 sm:gap-4 shrink-0">
                <div className="text-center">
                  <div className="text-2xl font-bold text-white">31+</div>
                  <div className="text-xs text-white/50">Members</div>
                </div>
                <div className="text-center">
                  <div className="text-2xl font-bold text-white">38+</div>
                  <div className="text-xs text-white/50">Events</div>
                </div>
              </div>
            </div>
          </div>
        </Link>

      </div>

      {/* ── Footer ───────────────────────────────────────────────────────── */}
      <div className="relative z-20 text-center pb-4 px-6">
        <p className="text-xs text-white/20">
          © {new Date().getFullYear()} LankaConnect LLC · lankaconnect.app
          {' '}·{' '}
          <Link href="/animation-preview" className="underline hover:text-white/50 transition-colors">
            Preview themes
          </Link>
        </p>
      </div>
    </div>
  );
}
