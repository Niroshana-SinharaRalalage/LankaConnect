'use client';

/**
 * LankaConnect Umbrella Landing Page
 *
 * The root landing page for lankaconnect.app.
 * Shows the world map animation as full-page background, the brand
 * identity, and entry points to all LankaConnect sub-brands.
 *
 * Currently live:  LankaEvents
 * Coming soon:     LankaSeyla, LankaNivasa, LankaMart, LankaForums, LankaLearn
 */

import React, { useState, useEffect } from 'react';
import Link from 'next/link';
import {
  Calendar,
  ShoppingBag,
  Home,
  ShoppingCart,
  MessageSquare,
  BookOpen,
  Lock,
  ChevronRight,
  Globe,
} from 'lucide-react';
import { WorldMapAnimation, THEMES } from '@/presentation/components/features/landing/WorldMapAnimation';
import { useAuthStore } from '@/presentation/store/useAuthStore';

// ─── Default theme — change this to user's chosen key after preview ───────────
const DEFAULT_THEME_KEY = 'satellite-navy';

// ─── Sub-brand definitions ────────────────────────────────────────────────────
interface SubBrand {
  key: string;
  name: string;
  tagline: string;
  href: string;
  icon: React.ReactNode;
  live: boolean;
  color: string;         // accent color for the card border/icon
  bgColor: string;       // card background tint
}

const SUB_BRANDS: SubBrand[] = [
  {
    key: 'lanka-events',
    name: 'LankaEvents',
    tagline: 'Plan & discover Sri Lankan events',
    href: '/lanka-events',
    icon: <Calendar className="h-7 w-7" />,
    live: true,
    color: '#FF7900',
    bgColor: 'rgba(255,121,0,0.08)',
  },
  {
    key: 'lanka-forums',
    name: 'LankaForums',
    tagline: 'Connect & discuss in the community',
    href: '#',
    icon: <MessageSquare className="h-7 w-7" />,
    live: false,
    color: '#6366f1',
    bgColor: 'rgba(99,102,241,0.06)',
  },
  {
    key: 'lanka-seyla',
    name: 'LankaSeyla',
    tagline: 'Sri Lankan clothing & fashion',
    href: '#',
    icon: <ShoppingBag className="h-7 w-7" />,
    live: false,
    color: '#ec4899',
    bgColor: 'rgba(236,72,153,0.06)',
  },
  {
    key: 'lanka-nivasa',
    name: 'LankaNivasa',
    tagline: 'Home goods & furnishings',
    href: '#',
    icon: <Home className="h-7 w-7" />,
    live: false,
    color: '#10b981',
    bgColor: 'rgba(16,185,129,0.06)',
  },
  {
    key: 'lanka-mart',
    name: 'LankaMart',
    tagline: 'Sri Lankan grocery store',
    href: '#',
    icon: <ShoppingCart className="h-7 w-7" />,
    live: false,
    color: '#f59e0b',
    bgColor: 'rgba(245,158,11,0.06)',
  },
  {
    key: 'lanka-learn',
    name: 'LankaLearn',
    tagline: 'Online education platform',
    href: '#',
    icon: <BookOpen className="h-7 w-7" />,
    live: false,
    color: '#3b82f6',
    bgColor: 'rgba(59,130,246,0.06)',
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

      {/* ── Full-page animated background ───────────────────────────────── */}
      <div className="absolute inset-0 z-0">
        {mounted && <WorldMapAnimation theme={theme} className="w-full h-full" />}
      </div>

      {/* ── Dark overlay for text readability ───────────────────────────── */}
      <div className="absolute inset-0 z-10 bg-black/30" />

      {/* ── Top nav bar ─────────────────────────────────────────────────── */}
      <nav className="relative z-20 flex items-center justify-between px-6 py-4 md:px-10">
        <div className="flex items-center gap-2">
          <Globe className="h-5 w-5" style={{ color: theme.nodeFill }} />
          <span className="text-white font-bold text-lg tracking-tight">LankaConnect</span>
        </div>
        <div className="flex items-center gap-3">
          {user ? (
            <Link
              href="/lanka-events"
              className="px-4 py-2 rounded-lg text-sm font-medium text-white border border-white/30 hover:bg-white/10 transition-colors backdrop-blur-sm"
            >
              My Dashboard
            </Link>
          ) : (
            <>
              <Link
                href="/login"
                className="px-4 py-2 rounded-lg text-sm font-medium text-white/80 hover:text-white transition-colors"
              >
                Sign In
              </Link>
              <Link
                href="/register"
                className="px-4 py-2 rounded-lg text-sm font-medium text-white border border-white/30 hover:bg-white/10 transition-colors backdrop-blur-sm"
                style={{ borderColor: theme.nodeFill }}
              >
                Join Free
              </Link>
            </>
          )}
        </div>
      </nav>

      {/* ── Hero content ────────────────────────────────────────────────── */}
      <div className="relative z-20 flex flex-col items-center justify-center text-center px-6 pt-10 pb-6 md:pt-16 md:pb-10">

        {/* Tagline pill */}
        <div
          className="inline-flex items-center gap-2 px-4 py-1.5 rounded-full text-xs font-medium mb-5 backdrop-blur-sm border"
          style={{
            borderColor: `${theme.nodeFill}60`,
            background: `${theme.nodeFill}18`,
            color: theme.nodeFill,
          }}
        >
          <span className="w-1.5 h-1.5 rounded-full animate-pulse" style={{ background: theme.nodeFill }} />
          Connecting Sri Lankans Worldwide
        </div>

        {/* Main heading */}
        <h1 className="text-5xl md:text-7xl font-bold text-white mb-3 drop-shadow-lg tracking-tight">
          LankaConnect
        </h1>
        <p className="text-xl md:text-2xl font-light text-white/80 mb-3">
          One Country, One Community
        </p>
        <p className="text-sm text-white/55 max-w-md mb-10 leading-relaxed">
          Join the largest Sri Lankan community platform. Discover events, connect
          with businesses, engage in discussions, and celebrate our rich culture together.
        </p>

        {/* ── Sub-brand cards grid ─────────────────────────────────────── */}
        <div className="w-full max-w-4xl grid grid-cols-2 sm:grid-cols-3 gap-3 md:gap-4">
          {SUB_BRANDS.map((brand) => (
            <SubBrandCard key={brand.key} brand={brand} />
          ))}
        </div>

        {/* Coming soon note */}
        <p className="mt-6 text-xs text-white/35 flex items-center gap-1.5">
          <Lock className="h-3 w-3" />
          5 more platforms coming soon — locked cards will unlock as they launch
        </p>
      </div>

      {/* ── Footer strip ────────────────────────────────────────────────── */}
      <div className="relative z-20 text-center pb-4 px-6">
        <p className="text-xs text-white/25">
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

// ─── Sub-brand card ───────────────────────────────────────────────────────────
function SubBrandCard({ brand }: { brand: SubBrand }) {
  const inner = (
    <div
      className={`
        relative flex flex-col items-center text-center p-4 md:p-5 rounded-2xl
        border backdrop-blur-md transition-all duration-200
        ${brand.live
          ? 'hover:scale-[1.03] hover:shadow-xl cursor-pointer'
          : 'opacity-50 cursor-not-allowed'}
      `}
      style={{
        background: brand.live ? `${brand.bgColor}` : 'rgba(255,255,255,0.04)',
        borderColor: brand.live ? `${brand.color}50` : 'rgba(255,255,255,0.08)',
        boxShadow: brand.live ? `0 0 20px ${brand.color}15` : 'none',
      }}
    >
      {/* Coming soon lock badge */}
      {!brand.live && (
        <div className="absolute top-2.5 right-2.5 flex items-center gap-1 px-1.5 py-0.5 rounded-full bg-white/10 text-white/40 text-[10px]">
          <Lock className="h-2.5 w-2.5" />
          Soon
        </div>
      )}

      {/* Icon */}
      <div
        className="flex items-center justify-center w-12 h-12 rounded-xl mb-3"
        style={{
          background: brand.live ? `${brand.color}20` : 'rgba(255,255,255,0.06)',
          color: brand.live ? brand.color : 'rgba(255,255,255,0.25)',
        }}
      >
        {brand.icon}
      </div>

      {/* Name */}
      <h3 className={`font-semibold text-sm mb-1 ${brand.live ? 'text-white' : 'text-white/40'}`}>
        {brand.name}
      </h3>

      {/* Tagline */}
      <p className={`text-xs leading-snug ${brand.live ? 'text-white/60' : 'text-white/25'}`}>
        {brand.tagline}
      </p>

      {/* Arrow for live brands */}
      {brand.live && (
        <div className="mt-3 flex items-center gap-1 text-xs font-medium" style={{ color: brand.color }}>
          Explore <ChevronRight className="h-3 w-3" />
        </div>
      )}
    </div>
  );

  if (!brand.live) return inner;

  return <Link href={brand.href}>{inner}</Link>;
}
