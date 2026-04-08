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
import { Calendar, ChevronRight, MessageSquare, ShoppingBag, Home, ShoppingCart, BookOpen, Lock, ArrowUpRight, Facebook, Twitter, Instagram, Youtube, Mail, User } from 'lucide-react';
import { WorldMapAnimation, THEMES } from '@/presentation/components/features/landing/WorldMapAnimation';
import { useAuthStore, useHasHydrated } from '@/presentation/store/useAuthStore';
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
    description: 'Discover, create & manage Sri Lankan events worldwide. Browse upcoming concerts, cultural shows, food fairs & more.',
    href: '/lanka-events',
    icon: <Calendar className="h-7 w-7" />,
    live: true,
    color: '#FF7900',
  },
  {
    key: 'lanka-forums',
    name: 'LankaForums',
    tagline: 'Community Forum',
    description: 'Discuss news, culture & life with Sri Lankans across the globe. Share stories, ask questions, build connections.',
    href: '#',
    icon: <MessageSquare className="h-7 w-7" />,
    live: false,
    color: '#6366f1',
  },
  {
    key: 'lanka-seyla',
    name: 'LankaSeyla',
    tagline: 'Fashion & Clothing',
    description: 'Authentic Sri Lankan clothing, batik, traditional wear & modern fashion delivered worldwide.',
    href: '#',
    icon: <ShoppingBag className="h-7 w-7" />,
    live: false,
    color: '#ec4899',
  },
  {
    key: 'lanka-nivasa',
    name: 'LankaNivasa',
    tagline: 'Home & Living',
    description: 'Sri Lankan home decor, furniture & lifestyle goods. Bring the warmth of home wherever you are.',
    href: '#',
    icon: <Home className="h-7 w-7" />,
    live: false,
    color: '#10b981',
  },
  {
    key: 'lanka-mart',
    name: 'LankaMart',
    tagline: 'Grocery & Essentials',
    description: 'Sri Lankan spices, rice, lentils, snacks & pantry essentials. Fresh and authentic, shipped to your door.',
    href: '#',
    icon: <ShoppingCart className="h-7 w-7" />,
    live: false,
    color: '#f59e0b',
  },
  {
    key: 'lanka-learn',
    name: 'LankaLearn',
    tagline: 'Education Platform',
    description: 'Online courses in Sinhala, Tamil, Sri Lankan history, culture, cooking & professional skills.',
    href: '#',
    icon: <BookOpen className="h-7 w-7" />,
    live: false,
    color: '#3b82f6',
  },
];

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

        {/* ── Sub-brand grid — 1 col mobile, 2 col desktop ────────────── */}
        <div className="w-full px-3 md:px-10 grid grid-cols-1 md:grid-cols-2 gap-3 md:gap-y-4 mb-10" style={{ maxWidth: '1200px', columnGap: '140px' }}>
          {SUB_BRANDS.map((brand) => {
            const inner = (
              <div
                className={`
                  relative flex items-start gap-4 px-5 py-4 rounded-2xl
                  text-left w-full select-none
                  ${brand.live ? 'cursor-pointer' : 'opacity-50 cursor-not-allowed'}
                `}
                style={{
                  /* Layered 3-D effect: top-edge highlight + bottom-edge depth shadow + ambient glow */
                  background: brand.live
                    ? `linear-gradient(175deg, ${brand.color}30 0%, ${brand.color}14 60%, ${brand.color}06 100%)`
                    : 'rgba(255,255,255,0.03)',
                  border: brand.live
                    ? `1px solid ${brand.color}55`
                    : '1px solid rgba(255,255,255,0.07)',
                  boxShadow: brand.live
                    ? [
                        `inset 0 1px 0 rgba(255,255,255,0.18)`,           /* top-edge shine */
                        `inset 0 -1px 0 ${brand.color}60`,                /* bottom-edge inner lip */
                        `0 6px 0 ${brand.color}55`,                       /* 3-D lift shadow */
                        `0 10px 32px ${brand.color}22`,                   /* ambient glow */
                        `0 2px 4px rgba(0,0,0,0.45)`,                     /* ground shadow */
                      ].join(', ')
                    : 'none',
                  minHeight: '110px',
                  transition: 'transform 0.10s ease, box-shadow 0.10s ease',
                }}
                onMouseEnter={e => {
                  if (!brand.live) return;
                  const el = e.currentTarget as HTMLDivElement;
                  el.style.transform = 'translateY(-2px)';
                  el.style.boxShadow = [
                    `inset 0 1px 0 rgba(255,255,255,0.22)`,
                    `inset 0 -1px 0 ${brand.color}70`,
                    `0 8px 0 ${brand.color}60`,
                    `0 14px 40px ${brand.color}30`,
                    `0 4px 6px rgba(0,0,0,0.5)`,
                  ].join(', ');
                }}
                onMouseLeave={e => {
                  if (!brand.live) return;
                  const el = e.currentTarget as HTMLDivElement;
                  el.style.transform = '';
                  el.style.boxShadow = [
                    `inset 0 1px 0 rgba(255,255,255,0.18)`,
                    `inset 0 -1px 0 ${brand.color}60`,
                    `0 6px 0 ${brand.color}55`,
                    `0 10px 32px ${brand.color}22`,
                    `0 2px 4px rgba(0,0,0,0.45)`,
                  ].join(', ');
                }}
                onMouseDown={e => {
                  if (!brand.live) return;
                  const el = e.currentTarget as HTMLDivElement;
                  el.style.transform = 'translateY(5px)';
                  el.style.boxShadow = [
                    `inset 0 1px 0 rgba(255,255,255,0.10)`,
                    `inset 0 -1px 0 ${brand.color}40`,
                    `0 1px 0 ${brand.color}55`,
                    `0 3px 12px ${brand.color}18`,
                    `0 1px 2px rgba(0,0,0,0.5)`,
                  ].join(', ');
                }}
                onMouseUp={e => {
                  if (!brand.live) return;
                  const el = e.currentTarget as HTMLDivElement;
                  el.style.transform = 'translateY(-2px)';
                  el.style.boxShadow = [
                    `inset 0 1px 0 rgba(255,255,255,0.22)`,
                    `inset 0 -1px 0 ${brand.color}70`,
                    `0 8px 0 ${brand.color}60`,
                    `0 14px 40px ${brand.color}30`,
                    `0 4px 6px rgba(0,0,0,0.5)`,
                  ].join(', ');
                }}
              >
                {/* Icon */}
                <div
                  className="flex items-center justify-center w-14 h-14 rounded-xl flex-shrink-0 mt-0.5"
                  style={{
                    background: brand.live ? `${brand.color}22` : 'rgba(255,255,255,0.05)',
                    color: brand.live ? brand.color : 'rgba(255,255,255,0.2)',
                    boxShadow: brand.live ? `inset 0 1px 0 rgba(255,255,255,0.15), 0 2px 8px ${brand.color}30` : 'none',
                  }}
                >
                  {brand.icon}
                </div>

                {/* Text */}
                <div className="flex-1 min-w-0 pr-2">
                  <div className="flex items-center gap-2 mb-1">
                    <span className={`font-bold text-base leading-tight ${brand.live ? 'text-white' : 'text-white/35'}`}>
                      {brand.name}
                    </span>
                    <span
                      className="text-[10px] font-semibold px-2 py-0.5 rounded-full leading-none flex-shrink-0"
                      style={{
                        background: brand.live ? `${brand.color}25` : 'rgba(255,255,255,0.06)',
                        color: brand.live ? brand.color : 'rgba(255,255,255,0.25)',
                        border: `1px solid ${brand.live ? `${brand.color}40` : 'rgba(255,255,255,0.08)'}`,
                      }}
                    >
                      {brand.tagline}
                    </span>
                  </div>
                  <div className={`text-xs leading-relaxed ${brand.live ? 'text-white/60' : 'text-white/22'}`}>
                    {brand.description}
                  </div>
                </div>

                {/* Right side */}
                <div className="flex flex-col items-end gap-2 flex-shrink-0 pt-0.5">
                  {brand.live ? (
                    <>
                      <div className="flex items-center gap-1">
                        <span className="w-1.5 h-1.5 rounded-full bg-green-400 animate-pulse" />
                        <span className="text-green-400 text-[10px] font-bold uppercase tracking-wide">Live</span>
                      </div>
                      <ArrowUpRight className="h-5 w-5" style={{ color: brand.color }} />
                    </>
                  ) : (
                    <div className="flex items-center gap-1 px-2 py-1 rounded-full" style={{ background: 'rgba(255,255,255,0.07)' }}>
                      <Lock className="h-3 w-3 text-white/30" />
                      <span className="text-white/30 text-[10px] font-medium">Coming Soon</span>
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
      <footer className="relative z-20 mt-8">
        {/* Gradient background with cross pattern */}
        <div
          className="relative px-8 md:px-16 pt-12 pb-8"
          style={{
            background: 'linear-gradient(135deg, #c45000 0%, #8B0000 30%, #6b0020 55%, #1a4a20 100%)',
          }}
        >
          {/* Cross/plus pattern overlay */}
          <div
            className="absolute inset-0 pointer-events-none"
            style={{
              backgroundImage: `radial-gradient(circle, rgba(255,255,255,0.12) 1px, transparent 1px)`,
              backgroundSize: '32px 32px',
              maskImage: 'linear-gradient(to bottom, rgba(0,0,0,0.4) 0%, rgba(0,0,0,0.15) 100%)',
            }}
          />

          {/* Link columns */}
          <div className="relative grid grid-cols-2 md:grid-cols-3 gap-8 mb-10 max-w-4xl">
            {/* Community */}
            <div>
              <h4 className="text-white font-bold text-sm mb-4 tracking-wide">Community</h4>
              <ul className="space-y-2.5">
                {[
                  { label: 'Events', href: '/lanka-events' },
                  { label: 'Forums', href: '#' },
                  { label: 'Dashboard', href: '/lanka-events/dashboard' },
                ].map(l => (
                  <li key={l.label}>
                    <Link href={l.href} className="text-white/65 hover:text-white text-sm transition-colors">
                      {l.label}
                    </Link>
                  </li>
                ))}
              </ul>
            </div>

            {/* Marketplace */}
            <div>
              <h4 className="text-white font-bold text-sm mb-4 tracking-wide">Marketplace</h4>
              <ul className="space-y-2.5">
                {[
                  { label: 'Browse Listings', href: '#' },
                  { label: 'Businesses', href: '#' },
                ].map(l => (
                  <li key={l.label}>
                    <Link href={l.href} className="text-white/65 hover:text-white text-sm transition-colors">
                      {l.label}
                    </Link>
                  </li>
                ))}
              </ul>
            </div>

            {/* About */}
            <div>
              <h4 className="text-white font-bold text-sm mb-4 tracking-wide">About</h4>
              <ul className="space-y-2.5">
                {[
                  { label: 'About Us', href: '#' },
                  { label: 'Contact Us', href: '#' },
                ].map(l => (
                  <li key={l.label}>
                    <Link href={l.href} className="text-white/65 hover:text-white text-sm transition-colors">
                      {l.label}
                    </Link>
                  </li>
                ))}
              </ul>
            </div>
          </div>

          {/* Divider */}
          <div className="relative border-t border-white/15 mb-6" />

          {/* Bottom bar */}
          <div className="relative flex flex-col sm:flex-row items-center justify-between gap-4">
            {/* Brand */}
            <div className="flex items-center gap-3">
              <Image
                src="/images/lankaconnect-logo.png"
                alt="LankaConnect"
                width={38}
                height={38}
                className="object-contain"
              />
              <div>
                <div className="text-white font-bold text-sm leading-tight">LankaConnect</div>
                <div className="text-white/50 text-xs">© {new Date().getFullYear()} All rights reserved</div>
              </div>
            </div>

            {/* Social icons */}
            <div className="flex items-center gap-5">
              {[
                { icon: <Facebook className="w-4 h-4" />, href: '#', label: 'Facebook' },
                { icon: <Twitter className="w-4 h-4" />, href: '#', label: 'Twitter' },
                { icon: <Instagram className="w-4 h-4" />, href: '#', label: 'Instagram' },
                { icon: <Youtube className="w-4 h-4" />, href: '#', label: 'YouTube' },
                { icon: <Mail className="w-4 h-4" />, href: 'mailto:hello@lankaconnect.app', label: 'Email' },
              ].map(s => (
                <Link
                  key={s.label}
                  href={s.href}
                  aria-label={s.label}
                  className="text-white/50 hover:text-white transition-colors"
                >
                  {s.icon}
                </Link>
              ))}
            </div>
          </div>
        </div>
      </footer>
    </div>
  );
}
