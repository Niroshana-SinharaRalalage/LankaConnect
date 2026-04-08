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
import { ArrowUpRight, User } from 'lucide-react';
import { WorldMapAnimation, THEMES } from '@/presentation/components/features/landing/WorldMapAnimation';
import { useAuthStore, useHasHydrated } from '@/presentation/store/useAuthStore';
import Footer from '@/presentation/components/layout/Footer';
// ─── Theme ────────────────────────────────────────────────────────────────────
const DEFAULT_THEME_KEY = 'satellite-navy';

// ─── LankaEvents brand ────────────────────────────────────────────────────────
const LANKA_EVENTS_COLOR = '#FF7900';

// ─── Component ────────────────────────────────────────────────────────────────
export default function LankaConnectHome() {
  const [mounted, setMounted] = useState(false);
  useEffect(() => { setMounted(true); }, []);

  const { isAuthenticated, user } = useAuthStore();
  const hasHydrated = useHasHydrated();

  const theme = THEMES.find(t => t.key === DEFAULT_THEME_KEY) ?? THEMES[2];

  return (
    <div className="relative min-h-screen w-full overflow-hidden">

      {/* ── Full-page animated world map ──────────────────────────────── */}
      <div className="absolute inset-0 z-0">
        {mounted && <WorldMapAnimation theme={theme} className="w-full h-full" />}
      </div>

      {/* ── Subtle dark overlay — just enough for text legibility ─────── */}
      <div className="absolute inset-0 z-10 bg-black/20" />

      {/* ── Top nav — auth-aware ──────────────────────────────────────── */}
      <nav className="relative z-20 flex items-center justify-end px-6 py-4 md:px-10">
        {/* Placeholder while Zustand rehydrates — prevents flash of wrong state */}
        {!hasHydrated && <div className="h-9 w-40" />}

        {hasHydrated && isAuthenticated && user && (
          <div className="flex items-center gap-3">
            {/* User avatar */}
            <div
              className="flex items-center gap-2 px-3 py-1.5 rounded-full backdrop-blur-sm border"
              style={{ borderColor: 'rgba(255,255,255,0.15)', background: 'rgba(255,255,255,0.08)' }}
            >
              <div
                className="w-6 h-6 rounded-full flex items-center justify-center text-xs font-bold flex-shrink-0"
                style={{ background: theme.nodeFill, color: '#000' }}
              >
                {user.fullName?.[0]?.toUpperCase() ?? <User className="w-3 h-3" />}
              </div>
              <span className="text-white/90 text-sm font-medium hidden sm:inline">
                {user.fullName?.split(' ')[0]}
              </span>
            </div>
            <Link
              href="/lanka-events/dashboard"
              className="px-4 py-2 rounded-lg text-sm font-semibold text-white border transition-all hover:bg-white/10 backdrop-blur-sm"
              style={{ borderColor: theme.nodeFill, boxShadow: `0 0 12px ${theme.nodeFill}40` }}
            >
              Dashboard
            </Link>
          </div>
        )}

        {hasHydrated && !isAuthenticated && (
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
        )}
      </nav>

      {/* ── Hero ──────────────────────────────────────────────────────── */}
      <div className="relative z-20 flex flex-col items-center justify-center text-center px-4 pt-2 pb-6 md:pt-4 md:pb-10">

        {/* Brand identity */}
        <div
          className="flex items-center gap-3 mb-4"
          style={{ filter: 'drop-shadow(0 4px 24px rgba(0,0,0,0.55))' }}
        >
          <Image
            src="/images/lankaconnect-logo.png"
            alt="LankaConnect"
            width={84}
            height={84}
            className="object-contain flex-shrink-0"
            priority
          />
          <div className="flex flex-col items-start gap-1.5">
            <span
              className="font-bold text-white leading-none"
              style={{ fontSize: '40px', letterSpacing: '0em', textShadow: '0 2px 14px rgba(0,0,0,0.55)' }}
            >
              LankaConnect
            </span>
            <span
              className="font-medium text-white/60 uppercase leading-none"
              style={{ fontSize: '13.5px', letterSpacing: '0.255em' }}
            >
              Sri Lankan Community Hub
            </span>
          </div>
        </div>

        {/* Connecting pill */}
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

        {/* ── LankaEvents — single centred entry card ──────────────────── */}
        <div className="w-full flex justify-center px-4 mb-10">
          <Link href="/lanka-events" className="block w-full" style={{ maxWidth: '520px' }}>
            <div
              className="relative flex items-start gap-5 px-6 py-5 rounded-2xl cursor-pointer select-none"
              style={{
                background: `linear-gradient(175deg, ${LANKA_EVENTS_COLOR}30 0%, ${LANKA_EVENTS_COLOR}14 60%, ${LANKA_EVENTS_COLOR}06 100%)`,
                border: `1px solid ${LANKA_EVENTS_COLOR}55`,
                boxShadow: [
                  `inset 0 1px 0 rgba(255,255,255,0.18)`,
                  `inset 0 -1px 0 ${LANKA_EVENTS_COLOR}60`,
                  `0 6px 0 ${LANKA_EVENTS_COLOR}55`,
                  `0 10px 32px ${LANKA_EVENTS_COLOR}22`,
                  `0 2px 4px rgba(0,0,0,0.45)`,
                ].join(', '),
                minHeight: '120px',
                transition: 'transform 0.10s ease, box-shadow 0.10s ease',
              }}
              onMouseEnter={e => {
                const el = e.currentTarget as HTMLDivElement;
                el.style.transform = 'translateY(-3px)';
                el.style.boxShadow = [
                  `inset 0 1px 0 rgba(255,255,255,0.22)`,
                  `inset 0 -1px 0 ${LANKA_EVENTS_COLOR}70`,
                  `0 9px 0 ${LANKA_EVENTS_COLOR}60`,
                  `0 16px 48px ${LANKA_EVENTS_COLOR}35`,
                  `0 4px 8px rgba(0,0,0,0.5)`,
                ].join(', ');
              }}
              onMouseLeave={e => {
                const el = e.currentTarget as HTMLDivElement;
                el.style.transform = '';
                el.style.boxShadow = [
                  `inset 0 1px 0 rgba(255,255,255,0.18)`,
                  `inset 0 -1px 0 ${LANKA_EVENTS_COLOR}60`,
                  `0 6px 0 ${LANKA_EVENTS_COLOR}55`,
                  `0 10px 32px ${LANKA_EVENTS_COLOR}22`,
                  `0 2px 4px rgba(0,0,0,0.45)`,
                ].join(', ');
              }}
              onMouseDown={e => {
                const el = e.currentTarget as HTMLDivElement;
                el.style.transform = 'translateY(5px)';
                el.style.boxShadow = [
                  `inset 0 1px 0 rgba(255,255,255,0.10)`,
                  `inset 0 -1px 0 ${LANKA_EVENTS_COLOR}40`,
                  `0 1px 0 ${LANKA_EVENTS_COLOR}55`,
                  `0 3px 12px ${LANKA_EVENTS_COLOR}18`,
                  `0 1px 2px rgba(0,0,0,0.5)`,
                ].join(', ');
              }}
              onMouseUp={e => {
                const el = e.currentTarget as HTMLDivElement;
                el.style.transform = 'translateY(-3px)';
                el.style.boxShadow = [
                  `inset 0 1px 0 rgba(255,255,255,0.22)`,
                  `inset 0 -1px 0 ${LANKA_EVENTS_COLOR}70`,
                  `0 9px 0 ${LANKA_EVENTS_COLOR}60`,
                  `0 16px 48px ${LANKA_EVENTS_COLOR}35`,
                  `0 4px 8px rgba(0,0,0,0.5)`,
                ].join(', ');
              }}
            >
              {/* Logo image */}
              <div
                className="flex items-center justify-center w-20 h-20 rounded-2xl flex-shrink-0 overflow-hidden"
                style={{
                  background: `${LANKA_EVENTS_COLOR}18`,
                  boxShadow: `inset 0 1px 0 rgba(255,255,255,0.15), 0 2px 12px ${LANKA_EVENTS_COLOR}35`,
                }}
              >
                <Image
                  src="/images/lanka-events-logo.png"
                  alt="LankaEvents"
                  width={80}
                  height={80}
                  className="object-contain w-full h-full"
                  onError={(e) => {
                    // fallback: hide broken image, show nothing (parent bg shows)
                    (e.currentTarget as HTMLImageElement).style.display = 'none';
                  }}
                />
              </div>

              {/* Text */}
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2 mb-1.5">
                  <span className="font-bold text-xl text-white leading-tight">LankaEvents</span>
                  <span
                    className="text-[11px] font-semibold px-2.5 py-0.5 rounded-full leading-none"
                    style={{
                      background: `${LANKA_EVENTS_COLOR}25`,
                      color: LANKA_EVENTS_COLOR,
                      border: `1px solid ${LANKA_EVENTS_COLOR}40`,
                    }}
                  >
                    Event Planner
                  </span>
                </div>
                <div className="text-sm text-white/65 leading-relaxed">
                  Discover, create &amp; manage Sri Lankan events worldwide. Browse upcoming concerts, cultural shows, food fairs &amp; more.
                </div>
              </div>

              {/* Right side */}
              <div className="flex flex-col items-end gap-2 flex-shrink-0 pt-1">
                <div className="flex items-center gap-1">
                  <span className="w-1.5 h-1.5 rounded-full bg-green-400 animate-pulse" />
                  <span className="text-green-400 text-[10px] font-bold uppercase tracking-wide">Live</span>
                </div>
                <ArrowUpRight className="h-5 w-5" style={{ color: LANKA_EVENTS_COLOR }} />
              </div>
            </div>
          </Link>
        </div>

        {/* Main copy — below the buttons */}
        <div className="max-w-lg">
          <h2
            className="text-2xl md:text-3xl font-bold text-white mb-3 drop-shadow-md leading-tight"
            style={{ textShadow: '0 2px 16px rgba(0,0,0,0.6)' }}
          >
            One Country,{' '}
            <span style={{ color: theme.nodeFill }}>One Community</span>
          </h2>
          <p
            className="text-sm md:text-base font-medium text-white/80 leading-relaxed"
            style={{ textShadow: '0 1px 8px rgba(0,0,0,0.7)' }}
          >
            Join the largest Sri Lankan community platform. Discover events,
            connect with businesses, engage in discussions, and celebrate our
            rich culture together.
          </p>
        </div>

      </div>

      {/* ── Footer — shared component (same as /lanka-events) ────────── */}
      <div className="relative z-20">
        <Footer />
      </div>
    </div>
  );
}
