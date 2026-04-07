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
import { Calendar, ChevronRight, MessageSquare, ShoppingBag, Home, ShoppingCart, BookOpen, Lock } from 'lucide-react';
import { WorldMapAnimation, THEMES } from '@/presentation/components/features/landing/WorldMapAnimation';
import { useAuthStore } from '@/presentation/store/useAuthStore';

// ─── Theme ────────────────────────────────────────────────────────────────────
const DEFAULT_THEME_KEY = 'satellite-navy';

// ─── Sub-brand definitions ────────────────────────────────────────────────────
interface SubBrand {
  key: string;
  name: string;
  tagline: string;
  href: string;
  icon: React.ReactNode;
  live: boolean;
  color: string;
  description: string;
}

const SUB_BRANDS: SubBrand[] = [
  {
    key: 'lanka-events',
    name: 'LankaEvents',
    tagline: 'Event Planner',
    description: 'Discover & plan Sri Lankan events worldwide',
    href: '/lanka-events',
    icon: <Calendar className="h-5 w-5" />,
    live: true,
    color: '#FF7900',
  },
  {
    key: 'lanka-forums',
    name: 'LankaForums',
    tagline: 'Community Forum',
    description: 'Connect & discuss with the community',
    href: '#',
    icon: <MessageSquare className="h-5 w-5" />,
    live: false,
    color: '#6366f1',
  },
  {
    key: 'lanka-seyla',
    name: 'LankaSeyla',
    tagline: 'Fashion & Clothing',
    description: 'Sri Lankan clothing & fashion',
    href: '#',
    icon: <ShoppingBag className="h-5 w-5" />,
    live: false,
    color: '#ec4899',
  },
  {
    key: 'lanka-nivasa',
    name: 'LankaNivasa',
    tagline: 'Home & Living',
    description: 'Home goods & furnishings',
    href: '#',
    icon: <Home className="h-5 w-5" />,
    live: false,
    color: '#10b981',
  },
  {
    key: 'lanka-mart',
    name: 'LankaMart',
    tagline: 'Grocery Store',
    description: 'Sri Lankan grocery & essentials',
    href: '#',
    icon: <ShoppingCart className="h-5 w-5" />,
    live: false,
    color: '#f59e0b',
  },
  {
    key: 'lanka-learn',
    name: 'LankaLearn',
    tagline: 'Education',
    description: 'Online learning platform',
    href: '#',
    icon: <BookOpen className="h-5 w-5" />,
    live: false,
    color: '#3b82f6',
  },
];

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

        {/* Brand identity — logo + compact text lockup */}
        <div
          className="flex items-center gap-3 mb-5"
          style={{ filter: 'drop-shadow(0 4px 24px rgba(0,0,0,0.55))' }}
        >
          {/* Logo — 84px as confirmed in DevTools */}
          <Image
            src="/images/lankaconnect-logo.png"
            alt="LankaConnect"
            width={84}
            height={84}
            className="object-contain flex-shrink-0"
            priority
          />
          {/* Text lockup — exact values confirmed via DevTools inspection */}
          <div className="flex flex-col items-start gap-1.5">
            <span
              className="font-bold text-white leading-none"
              style={{
                fontSize: '40px',
                letterSpacing: '0em',
                textShadow: '0 2px 14px rgba(0,0,0,0.55)',
              }}
            >
              LankaConnect
            </span>
            <span
              className="font-medium text-white/60 uppercase leading-none"
              style={{
                fontSize: '13.5px',
                letterSpacing: '0.255em',
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

        {/* ── Sub-brand grid — 3 rows × 2 columns ─────────────────────── */}
        <div className="w-full max-w-xl grid grid-cols-2 gap-3">
          {SUB_BRANDS.map((brand) => {
            const card = (
              <div
                className={`
                  relative flex flex-col gap-2 p-4 rounded-xl border
                  transition-all duration-200
                  ${brand.live
                    ? 'hover:scale-[1.03] hover:shadow-xl cursor-pointer'
                    : 'opacity-60 cursor-not-allowed'}
                `}
                style={{
                  background: brand.live
                    ? `rgba(${brand.color === '#FF7900' ? '255,121,0' : '20,10,5'},${brand.live ? '0.18' : '0.1'})`
                    : 'rgba(255,255,255,0.04)',
                  borderColor: brand.live ? `${brand.color}50` : 'rgba(255,255,255,0.08)',
                  boxShadow: brand.live ? `0 0 20px ${brand.color}15` : 'none',
                }}
              >
                {/* Coming soon badge */}
                {!brand.live && (
                  <div className="absolute top-2 right-2 flex items-center gap-1 px-1.5 py-0.5 rounded-full bg-white/10 text-white/40 text-[9px]">
                    <Lock className="h-2 w-2" />
                    Soon
                  </div>
                )}

                {/* Live badge */}
                {brand.live && (
                  <div className="absolute top-2 right-2 flex items-center gap-1">
                    <span className="w-1.5 h-1.5 rounded-full bg-green-400 animate-pulse" />
                    <span className="text-green-400 text-[9px] font-semibold">Live</span>
                  </div>
                )}

                {/* Icon */}
                <div
                  className="flex items-center justify-center w-9 h-9 rounded-lg"
                  style={{
                    background: brand.live ? `${brand.color}22` : 'rgba(255,255,255,0.06)',
                    color: brand.live ? brand.color : 'rgba(255,255,255,0.25)',
                  }}
                >
                  {brand.icon}
                </div>

                {/* Name + tagline */}
                <div>
                  <div className={`font-bold text-sm leading-none mb-0.5 ${brand.live ? 'text-white' : 'text-white/40'}`}>
                    {brand.name}
                  </div>
                  <div className={`text-[10px] font-medium ${brand.live ? 'text-white/50' : 'text-white/25'}`}>
                    {brand.tagline}
                  </div>
                </div>

                {/* Description */}
                <p className={`text-[11px] leading-snug ${brand.live ? 'text-white/60' : 'text-white/20'}`}>
                  {brand.description}
                </p>

                {/* CTA for live brand */}
                {brand.live && (
                  <div
                    className="flex items-center gap-1 text-[11px] font-semibold mt-auto"
                    style={{ color: brand.color }}
                  >
                    Explore <ChevronRight className="h-3 w-3" />
                  </div>
                )}
              </div>
            );

            if (!brand.live) return <div key={brand.key}>{card}</div>;
            return <Link key={brand.key} href={brand.href}>{card}</Link>;
          })}
        </div>

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
