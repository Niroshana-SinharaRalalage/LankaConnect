'use client';

/**
 * LankaConnect Umbrella Landing Page
 *
 * Cinematic full-page animated world map background with the brand
 * identity centred and the sub-brand entry points (LankaEvents, LankaSeyla)
 * side by side beneath it.
 *
 * Intentionally no top-left logo/branding — the hero IS the identity.
 * No "My Dashboard" in nav — that belongs in /lanka-events.
 */

import React, { useState, useEffect, useRef } from 'react';
import Link from 'next/link';
import Image from 'next/image';
import { useRouter } from 'next/navigation';
import { User, ChevronDown, LogOut, UserCircle } from 'lucide-react';
import { WorldMapAnimation, THEMES } from '@/presentation/components/features/landing/WorldMapAnimation';
import { EntryCard } from '@/presentation/components/features/landing/EntryCard';
import { useAuthStore, useHasHydrated } from '@/presentation/store/useAuthStore';
import Footer from '@/presentation/components/layout/Footer';
// ─── Theme ────────────────────────────────────────────────────────────────────
const DEFAULT_THEME_KEY = 'satellite-navy';

// ─── Sub-brand identities ─────────────────────────────────────────────────────
const LANKA_EVENTS_COLOR = '#FF7900';

// Sampled from the LankaSeyla wordmark artwork (dominant gold; #E3B565 highlights).
const LANKA_SEYLA_COLOR = '#D7A959';
const LANKA_SEYLA_URL = 'https://lankaseyla.lankaconnect.app/';

// ─── Component ────────────────────────────────────────────────────────────────
export default function LankaConnectHome() {
  const [mounted, setMounted] = useState(false);
  useEffect(() => { setMounted(true); }, []);

  const { isAuthenticated, user, clearAuth } = useAuthStore();
  const hasHydrated = useHasHydrated();
  const router = useRouter();
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const userMenuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (userMenuRef.current && !userMenuRef.current.contains(e.target as Node)) {
        setUserMenuOpen(false);
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleLogout = () => {
    clearAuth();
    setUserMenuOpen(false);
    router.push('/');
  };

  const theme = THEMES.find(t => t.key === DEFAULT_THEME_KEY) ?? THEMES[2];

  return (
    <div className="w-full">

      {/* ══════════════════════════════════════════════════════════════════
          HERO — exactly 100vh. Nav floats top-right. Content fills height
          with brand at top, card in middle, tagline at bottom.
      ══════════════════════════════════════════════════════════════════ */}
      <div className="relative w-full overflow-hidden" style={{ height: '100vh' }}>

        {/* Animation — constrained strictly to this box */}
        <div className="absolute inset-0 z-0">
          {mounted && <WorldMapAnimation theme={theme} className="w-full h-full" />}
        </div>
        <div className="absolute inset-0 z-10 bg-black/20" />

        {/* ── Nav — floats top-right, does NOT consume vertical flow ───── */}
        <nav className="absolute top-0 right-0 z-30 flex items-center px-6 py-4 md:px-10">
          {!hasHydrated && <div className="h-9 w-32" />}

          {hasHydrated && isAuthenticated && user && (
            /* User chip with dropdown — no Dashboard button on landing page */
            <div className="relative" ref={userMenuRef}>
              <button
                onClick={() => setUserMenuOpen(v => !v)}
                className="flex items-center gap-2 px-3 py-1.5 rounded-full backdrop-blur-sm border transition-colors"
                style={{ borderColor: 'rgba(255,255,255,0.20)', background: 'rgba(255,255,255,0.10)' }}
              >
                {/* Avatar — photo if available, else gradient initials */}
                {user.profilePhotoUrl ? (
                  <Image
                    src={user.profilePhotoUrl}
                    alt={user.fullName}
                    width={28}
                    height={28}
                    className="w-7 h-7 rounded-full object-cover flex-shrink-0"
                  />
                ) : (
                  <div
                    className="w-7 h-7 rounded-full flex items-center justify-center text-xs font-bold flex-shrink-0 text-white"
                    style={{ background: 'linear-gradient(135deg, #FF7900, #8B1538)' }}
                  >
                    {user.fullName ? (user.fullName.trim().split(' ').map(n => n[0]).slice(0, 2).join('').toUpperCase()) : <User className="w-3 h-3" />}
                  </div>
                )}
                <span className="text-white/90 text-sm font-medium hidden sm:inline max-w-[140px] truncate">
                  {user.fullName}
                </span>
                <ChevronDown className="w-3.5 h-3.5 text-white/60 flex-shrink-0" />
              </button>

              {/* Dropdown */}
              {userMenuOpen && (
                <div
                  className="absolute right-0 mt-2 w-48 rounded-xl shadow-xl border overflow-hidden z-50"
                  style={{ background: 'rgba(10,15,40,0.95)', borderColor: 'rgba(255,255,255,0.12)', backdropFilter: 'blur(12px)' }}
                >
                  <div className="px-4 py-3 border-b" style={{ borderColor: 'rgba(255,255,255,0.08)' }}>
                    <p className="text-white text-sm font-semibold truncate">{user.fullName}</p>
                    <p className="text-white/50 text-xs truncate">{user.email}</p>
                  </div>
                  <Link
                    href="/profile"
                    onClick={() => setUserMenuOpen(false)}
                    className="flex items-center gap-2.5 px-4 py-2.5 text-white/80 hover:text-white hover:bg-white/10 transition-colors text-sm"
                  >
                    <UserCircle className="w-4 h-4" />
                    My Profile
                  </Link>
                  <button
                    onClick={handleLogout}
                    className="w-full flex items-center gap-2.5 px-4 py-2.5 text-red-400 hover:text-red-300 hover:bg-white/10 transition-colors text-sm"
                  >
                    <LogOut className="w-4 h-4" />
                    Sign Out
                  </button>
                </div>
              )}
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

        {/* ── Content — fills full 100vh, flex column, space-between ───── */}
        <div className="relative z-20 flex flex-col items-center justify-between text-center px-4 h-full pt-[55px] pb-[25px]">

          {/* ── TOP: Brand identity + pill ─────────────────────────────── */}
          <div className="flex flex-col items-center mt-5">
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

            <div
              className="inline-flex items-center gap-2 px-4 py-1.5 rounded-full text-xs font-semibold backdrop-blur-sm border"
              style={{
                borderColor: `${theme.nodeFill}55`,
                background: `${theme.nodeFill}1a`,
                color: theme.nodeFill,
              }}
            >
              <span className="w-1.5 h-1.5 rounded-full animate-pulse" style={{ background: theme.nodeFill }} />
              Connecting Sri Lankans Worldwide
            </div>
          </div>

          {/* ── MIDDLE: sub-brand entry cards ───────────────────────────
              Stacked below md, side-by-side above it. `items-stretch` keeps the
              two cards the same height when one tagline wraps.             */}
          <div className="w-full flex flex-col md:flex-row items-stretch justify-center gap-4 md:gap-5 px-4">
            <EntryCard
              brandColor={LANKA_EVENTS_COLOR}
              logoSrc="/lanka-events.png"
              logoAlt="LankaEvents"
              name="LankaEvents"
              badge="Event Planner"
              tagline="Plan Your Event with Ease"
              href="/lanka-events"
              live
            />
            <EntryCard
              brandColor={LANKA_SEYLA_COLOR}
              logoSrc="/lanka-seyla.png"
              logoAlt="LankaSeyla"
              name="LankaSeyla"
              badge="Clothing Store"
              tagline="Tradition Woven with Elegance"
              href={LANKA_SEYLA_URL}
              external
            />
          </div>

          {/* ── BOTTOM: One Country tagline ─────────────────────────────── */}
          <div className="max-w-lg mb-6">
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

        </div>{/* ── end content ── */}
      </div>{/* ══ end 100vh hero ══ */}

      {/* Footer — satellite-navy gradient continues from hero */}
      <div style={{ background: 'linear-gradient(180deg, #020818 0%, #0a0f2e 50%, #030d1a 100%)' }}>
        <Footer transparent={true} />
      </div>

    </div>
  );
}
