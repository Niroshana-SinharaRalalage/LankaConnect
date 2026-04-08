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

      {/* ── Top nav — always show Sign In / Join Free ─────────────────── */}
      <nav className="relative z-20 flex items-center justify-end px-6 py-4 md:px-10">
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

        {/* ── Sub-brand buttons — 3 rows × 2 columns, 150px column gap ─── */}
        <div className="w-full max-w-[820px] grid grid-cols-2 gap-y-3 mb-12" style={{ columnGap: '150px' }}>
          {SUB_BRANDS.map((brand) => {
            const inner = (
              <div
                className={`
                  relative flex items-center gap-3 px-4 py-3.5 rounded-xl border
                  transition-all duration-200 text-left w-full
                  ${brand.live
                    ? 'hover:scale-[1.02] hover:brightness-110 cursor-pointer active:scale-[0.99]'
                    : 'opacity-55 cursor-not-allowed'}
                `}
                style={{
                  background: brand.live
                    ? `linear-gradient(135deg, ${brand.color}28 0%, ${brand.color}10 100%)`
                    : 'rgba(255,255,255,0.03)',
                  borderColor: brand.live ? `${brand.color}55` : 'rgba(255,255,255,0.07)',
                  boxShadow: brand.live ? `0 2px 16px ${brand.color}20, inset 0 1px 0 rgba(255,255,255,0.06)` : 'none',
                  minHeight: '120px',
                }}
              >
                {/* Icon */}
                <div
                  className="flex items-center justify-center w-10 h-10 rounded-lg flex-shrink-0"
                  style={{
                    background: brand.live ? `${brand.color}20` : 'rgba(255,255,255,0.05)',
                    color: brand.live ? brand.color : 'rgba(255,255,255,0.2)',
                  }}
                >
                  {brand.icon}
                </div>

                {/* Text */}
                <div className="flex-1 min-w-0">
                  <div className={`font-bold text-sm leading-tight ${brand.live ? 'text-white' : 'text-white/35'}`}>
                    {brand.name}
                  </div>
                  <div className={`text-[11px] mt-0.5 leading-tight ${brand.live ? 'text-white/55' : 'text-white/20'}`}>
                    {brand.description}
                  </div>
                </div>

                {/* Right side */}
                <div className="flex flex-col items-end gap-1 flex-shrink-0">
                  {brand.live ? (
                    <>
                      <div className="flex items-center gap-1">
                        <span className="w-1.5 h-1.5 rounded-full bg-green-400 animate-pulse" />
                        <span className="text-green-400 text-[9px] font-bold uppercase">Live</span>
                      </div>
                      <ChevronRight className="h-4 w-4" style={{ color: brand.color }} />
                    </>
                  ) : (
                    <div className="flex items-center gap-1 px-1.5 py-0.5 rounded-full" style={{ background: 'rgba(255,255,255,0.08)' }}>
                      <Lock className="h-2.5 w-2.5 text-white/30" />
                      <span className="text-white/30 text-[9px] font-medium">Soon</span>
                    </div>
                  )}
                </div>
              </div>
            );

            if (!brand.live) return <div key={brand.key}>{inner}</div>;
            return <Link key={brand.key} href={brand.href} className="block">{inner}</Link>;
          })}
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
