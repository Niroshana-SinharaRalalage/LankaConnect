'use client';

/**
 * LankaEventsHeader
 *
 * Dedicated header for the /lanka-events route.
 * Left: LankaConnect lion icon (links to /) + LankaEvents brand block
 * Nav: Events Home | Events | Newsletters | My Dashboard (auth) | Create Event (auth)
 * Auth: notification bell + user avatar dropdown (same pattern as Header.tsx)
 *
 * NOTE: Shared with Header.tsx auth section — extract to UserMenuDropdown when refactoring.
 * Do NOT modify Header.tsx — this file is intentionally separate to avoid breaking other pages.
 */

import * as React from 'react';
import Link from 'next/link';
import Image from 'next/image';
import { useRouter } from 'next/navigation';
import { useQueryClient } from '@tanstack/react-query';
import { Logo } from '@/presentation/components/atoms/Logo';
import { Button } from '@/presentation/components/ui/Button';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { NotificationBell } from '@/presentation/components/features/notifications/NotificationBell';
import { NotificationDropdown } from '@/presentation/components/features/notifications/NotificationDropdown';
import { useUnreadNotifications } from '@/presentation/hooks/useNotifications';
import { authRepository } from '@/infrastructure/api/repositories/auth.repository';
import { User, LogOut, ChevronDown, Menu, X, Loader2, Plus } from 'lucide-react';
import { UserRole } from '@/infrastructure/api/types/auth.types';
import { UpgradeModal } from '@/presentation/components/features/role-upgrade/UpgradeModal';
import { isEventOrganizer, isAdmin } from '@/infrastructure/api/utils/role-helpers';

/**
 * Helper to get user initials from fullName.
 * Shared with Header.tsx — extract to utility when refactoring.
 */
function getUserInitials(fullName: string): string {
  const names = fullName.trim().split(' ');
  if (names.length === 1) return names[0].substring(0, 2).toUpperCase();
  return (names[0][0] + names[names.length - 1][0]).toUpperCase();
}

export function LankaEventsHeader() {
  const { user, isAuthenticated, isHydrated, clearAuth } = useAuthStore();
  const router = useRouter();
  // NOTE: queryClient.clear() on logout is critical — do NOT remove (prevents data leakage between users)
  const queryClient = useQueryClient();
  const [notificationDropdownOpen, setNotificationDropdownOpen] = React.useState(false);
  const [userMenuOpen, setUserMenuOpen] = React.useState(false);
  const [mobileMenuOpen, setMobileMenuOpen] = React.useState(false);
  const [isLoggingOut, setIsLoggingOut] = React.useState(false);
  const [showUpgradeModal, setShowUpgradeModal] = React.useState(false);
  const userMenuRef = React.useRef<HTMLDivElement>(null);

  // CRITICAL: wait for hydration before firing auth-gated requests (prevents 401)
  const { data: unreadNotifications = [] } = useUnreadNotifications({
    enabled: isAuthenticated && isHydrated,
  });

  React.useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (userMenuRef.current && !userMenuRef.current.contains(event.target as Node)) {
        setUserMenuOpen(false);
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleCreateEvent = () => {
    if (!user) return;
    if (user.role === UserRole.GeneralUser) {
      setShowUpgradeModal(true);
    } else if (isEventOrganizer(user.role) || isAdmin(user.role)) {
      router.push('/events/create');
    }
  };

  const handleLogout = () => {
    setIsLoggingOut(true);
    setUserMenuOpen(false);
    // CRITICAL: Clear React Query cache to prevent data leakage between users
    // Shared with Header.tsx — do NOT remove
    queryClient.clear();
    clearAuth();
    authRepository.logout().catch(() => {
      // Silently ignore server logout errors — client is already logged out
    });
    router.push('/login');
  };

  return (
    <header className="sticky top-0 z-50 bg-white shadow-[0_2px_10px_rgba(0,0,0,0.08)]">
      <nav className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between py-3">

          {/* ── Brand block ──────────────────────────────────────────────── */}
          <div className="flex items-center gap-3">
            {/* LankaConnect lion icon — links back to umbrella landing */}
            <Link href="/" aria-label="LankaConnect home">
              <Logo size="md" showText={false} className="flex-shrink-0" />
            </Link>

            {/* Divider */}
            <div className="w-px h-10 bg-gray-200 hidden sm:block" />

            {/* LankaEvents brand — text only, no separate icon */}
            <Link href="/lanka-events" className="flex flex-col leading-tight group hidden sm:flex">
              <span className="font-bold text-2xl text-[#8B1538] group-hover:text-[#FF7900] transition-colors">
                LankaEvents
              </span>
              <span className="text-xs tracking-[0.035em] text-gray-600 font-medium -mt-1">
                Plan Your Event with Ease
              </span>
            </Link>
          </div>

          {/* ── Mobile hamburger ──────────────────────────────────────────── */}
          <button
            onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
            className="lg:hidden p-2 text-[#333] hover:text-[#FF7900] transition-colors"
            aria-label="Toggle menu"
          >
            {mobileMenuOpen ? <X className="w-6 h-6" /> : <Menu className="w-6 h-6" />}
          </button>

          {/* ── Desktop nav links ─────────────────────────────────────────── */}
          <nav className="hidden lg:flex items-center gap-6">
            <Link href="/events" className="text-[#333] hover:text-[#FF7900] font-medium transition-colors">
              Events
            </Link>
            <Link href="/newsletters" className="text-[#333] hover:text-[#FF7900] font-medium transition-colors">
              Newsletters
            </Link>
            {isAuthenticated && (
              <Link href="/dashboard" className="text-[#333] hover:text-[#FF7900] font-medium transition-colors">
                My Dashboard
              </Link>
            )}
            {isAuthenticated && user && (
              <button
                onClick={handleCreateEvent}
                className="flex items-center gap-2 px-4 py-2 bg-[#FF7900] hover:bg-[#E66D00] text-white font-semibold rounded-lg transition-all text-sm"
              >
                <Plus className="w-4 h-4" />
                Create Event
              </button>
            )}
          </nav>

          {/* ── Auth section ──────────────────────────────────────────────── */}
          <div className="hidden lg:flex items-center gap-4">
            {isAuthenticated && user ? (
              <div className="flex items-center gap-3">
                {/* Notification bell — stays in bar on all screen sizes */}
                <div className="relative">
                  <NotificationBell
                    unreadCount={unreadNotifications.length}
                    onClick={() => setNotificationDropdownOpen(!notificationDropdownOpen)}
                  />
                  <NotificationDropdown
                    notifications={unreadNotifications}
                    isOpen={notificationDropdownOpen}
                    onClose={() => setNotificationDropdownOpen(false)}
                  />
                </div>

                {/* User dropdown */}
                <div className="relative" ref={userMenuRef}>
                  <div
                    className="flex items-center gap-2 cursor-pointer hover:opacity-90 transition-opacity"
                    onClick={() => setUserMenuOpen(!userMenuOpen)}
                    role="button"
                    tabIndex={0}
                    onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') setUserMenuOpen(!userMenuOpen); }}
                    title={user.fullName}
                  >
                    {user.profilePhotoUrl ? (
                      <Image
                        src={user.profilePhotoUrl}
                        alt={user.fullName}
                        width={40}
                        height={40}
                        className="w-10 h-10 rounded-full object-cover"
                      />
                    ) : (
                      <div
                        className="w-10 h-10 rounded-full flex items-center justify-center text-white font-bold"
                        style={{ background: 'linear-gradient(135deg, #FF7900, #8B1538)' }}
                      >
                        {getUserInitials(user.fullName)}
                      </div>
                    )}
                    <span className="text-sm font-medium text-[#333] hidden lg:inline">{user.fullName}</span>
                    <ChevronDown className={`w-4 h-4 text-[#666] transition-transform duration-200 ${userMenuOpen ? 'rotate-180' : ''}`} />
                  </div>

                  {userMenuOpen && (
                    <div className="absolute right-0 mt-2 w-48 rounded-lg shadow-lg overflow-hidden z-50 bg-white border border-[#e2e8f0]">
                      <div className="px-4 py-3 border-b border-gray-200 bg-[#f7fafc]">
                        <p className="text-sm font-medium text-[#333]">{user.fullName}</p>
                        <p className="text-xs text-gray-500">{user.email}</p>
                      </div>
                      <button
                        onClick={() => { setUserMenuOpen(false); router.push('/profile'); }}
                        className="w-full flex items-center gap-3 px-4 py-3 hover:bg-gray-50 transition-colors text-left"
                      >
                        <User className="w-4 h-4 text-[#FF7900]" />
                        <span className="text-[#2d3748]">Profile</span>
                      </button>
                      <div className="border-t border-[#e2e8f0]" />
                      <button
                        onClick={handleLogout}
                        disabled={isLoggingOut}
                        className="w-full flex items-center gap-3 px-4 py-3 hover:bg-gray-50 transition-colors text-left disabled:opacity-50"
                      >
                        {isLoggingOut
                          ? <Loader2 className="w-4 h-4 animate-spin text-[#8B1538]" />
                          : <LogOut className="w-4 h-4 text-[#8B1538]" />
                        }
                        <span className="text-[#2d3748]">{isLoggingOut ? 'Logging out...' : 'Logout'}</span>
                      </button>
                    </div>
                  )}
                </div>
              </div>
            ) : (
              <>
                <Button
                  variant="outline"
                  size="default"
                  className="border-[#8B1538] text-[#8B1538] hover:bg-[#8B1538] hover:text-white font-semibold transition-all"
                  onClick={() => router.push('/login')}
                >
                  Login
                </Button>
                <Button
                  variant="default"
                  size="default"
                  className="bg-[#FF7900] hover:bg-[#E66D00] text-white font-semibold transition-all"
                  onClick={() => router.push('/register')}
                >
                  Sign Up
                </Button>
              </>
            )}
          </div>
        </div>

        {/* ── Mobile menu ────────────────────────────────────────────────── */}
        {mobileMenuOpen && (
          <div className="lg:hidden border-t border-gray-200 py-4">
            <div className="flex flex-col gap-3">
              {/* Notification bell for mobile authenticated users */}
              {isAuthenticated && user && (
                <div className="flex items-center gap-3 px-4 pb-2 border-b border-gray-100">
                  <div className="relative">
                    <NotificationBell
                      unreadCount={unreadNotifications.length}
                      onClick={() => { setNotificationDropdownOpen(!notificationDropdownOpen); setMobileMenuOpen(false); }}
                    />
                  </div>
                  <span className="text-sm text-gray-600">{user.fullName}</span>
                </div>
              )}

              <Link href="/events" className="text-[#333] hover:text-[#FF7900] font-medium transition-colors px-4 py-2 hover:bg-gray-50 rounded-lg" onClick={() => setMobileMenuOpen(false)}>
                Events
              </Link>
              <Link href="/newsletters" className="text-[#333] hover:text-[#FF7900] font-medium transition-colors px-4 py-2 hover:bg-gray-50 rounded-lg" onClick={() => setMobileMenuOpen(false)}>
                Newsletters
              </Link>
              {isAuthenticated && (
                <Link href="/dashboard" className="text-[#333] hover:text-[#FF7900] font-medium transition-colors px-4 py-2 hover:bg-gray-50 rounded-lg" onClick={() => setMobileMenuOpen(false)}>
                  My Dashboard
                </Link>
              )}
              {isAuthenticated && user && (
                <button
                  onClick={() => { setMobileMenuOpen(false); handleCreateEvent(); }}
                  className="flex items-center gap-2 mx-4 px-4 py-2 bg-[#FF7900] hover:bg-[#E66D00] text-white font-semibold rounded-lg transition-all text-sm justify-center"
                >
                  <Plus className="w-4 h-4" />
                  Create Event
                </button>
              )}

              {!isAuthenticated && (
                <div className="flex flex-col gap-2 px-4 pt-3 border-t border-gray-200">
                  <Button variant="outline" size="default" className="w-full border-[#8B1538] text-[#8B1538] hover:bg-[#8B1538] hover:text-white font-semibold transition-all" onClick={() => { setMobileMenuOpen(false); router.push('/login'); }}>
                    Login
                  </Button>
                  <Button variant="default" size="default" className="w-full bg-[#FF7900] hover:bg-[#E66D00] text-white font-semibold transition-all" onClick={() => { setMobileMenuOpen(false); router.push('/register'); }}>
                    Sign Up
                  </Button>
                </div>
              )}
            </div>
          </div>
        )}
      </nav>

      <UpgradeModal isOpen={showUpgradeModal} onClose={() => setShowUpgradeModal(false)} />
    </header>
  );
}
