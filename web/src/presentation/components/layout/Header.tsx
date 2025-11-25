'use client';

import * as React from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { OfficialLogo } from '@/presentation/components/atoms/OfficialLogo';
import { Button } from '@/presentation/components/ui/Button';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { NotificationBell } from '@/presentation/components/features/notifications/NotificationBell';
import { NotificationDropdown } from '@/presentation/components/features/notifications/NotificationDropdown';
import { useUnreadNotifications } from '@/presentation/hooks/useNotifications';
import { User, LogOut, ChevronDown, Search } from 'lucide-react';

export interface HeaderProps {
  className?: string;
}

/**
 * Header Component
 * Reusable navigation header for LankaConnect application
 * Features: Sticky navigation, authentication state, responsive design
 * Styling: Sri Lankan flag colors (Maroon #8B1538, Saffron #FF7900)
 * Phase 6A.8: Added user dropdown menu with Profile and Logout options
 */
export function Header({ className = '' }: HeaderProps) {
  const { user, isAuthenticated, clearAuth } = useAuthStore();
  const router = useRouter();
  const [notificationDropdownOpen, setNotificationDropdownOpen] = React.useState(false);
  const [userMenuOpen, setUserMenuOpen] = React.useState(false);
  const [searchOpen, setSearchOpen] = React.useState(false);
  const userMenuRef = React.useRef<HTMLDivElement>(null);
  const searchRef = React.useRef<HTMLDivElement>(null);

  // Fetch unread notifications only when authenticated
  const { data: unreadNotifications = [] } = useUnreadNotifications({
    enabled: isAuthenticated,
  });

  // Close dropdowns when clicking outside
  React.useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (userMenuRef.current && !userMenuRef.current.contains(event.target as Node)) {
        setUserMenuOpen(false);
      }
      if (searchRef.current && !searchRef.current.contains(event.target as Node)) {
        setSearchOpen(false);
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  /**
   * Helper to get user initials from fullName
   * Returns first letter of first name and last name, or first two letters if single name
   */
  const getUserInitials = (fullName: string): string => {
    const names = fullName.trim().split(' ');
    if (names.length === 1) {
      return names[0].substring(0, 2).toUpperCase();
    }
    return (names[0][0] + names[names.length - 1][0]).toUpperCase();
  };

  return (
    <header
      className={`sticky top-0 z-50 bg-white shadow-[0_2px_10px_rgba(0,0,0,0.08)] ${className}`}
    >
      <nav className="container mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between py-4">
          {/* Official LankaConnect Logo with Subtitle */}
          <OfficialLogo size="md" />

          {/* Navigation Links - Responsive */}
          <nav className="hidden lg:flex items-center gap-6">
            <Link
              href="/events"
              className="text-[#333] hover:text-[#FF7900] font-medium transition-colors"
            >
              Events
            </Link>
            <Link
              href="/forums"
              className="text-[#333] hover:text-[#FF7900] font-medium transition-colors"
            >
              Forums
            </Link>
            <Link
              href="/business"
              className="text-[#333] hover:text-[#FF7900] font-medium transition-colors"
            >
              Business
            </Link>
            <Link
              href="/marketplace"
              className="text-[#333] hover:text-[#FF7900] font-medium transition-colors"
            >
              Marketplace
            </Link>
            {isAuthenticated && (
              <Link
                href="/dashboard"
                className="text-[#333] hover:text-[#FF7900] font-medium transition-colors"
              >
                Dashboard
              </Link>
            )}

            {/* Search */}
            <div className="relative" ref={searchRef}>
              <button
                onClick={() => setSearchOpen(!searchOpen)}
                className="p-2 text-[#333] hover:text-[#FF7900] transition-colors"
                aria-label="Search"
              >
                <Search className="w-5 h-5" />
              </button>

              {searchOpen && (
                <div className="absolute right-0 mt-2 w-80 bg-white rounded-lg shadow-lg border border-gray-200 p-4 z-50">
                  <input
                    type="text"
                    placeholder="Search events, forums, businesses..."
                    className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-[#FF7900] focus:border-transparent"
                    autoFocus
                  />
                </div>
              )}
            </div>
          </nav>

          {/* Auth Section */}
          <div className="flex items-center gap-4">
            {isAuthenticated && user ? (
              // Authenticated: Show notification bell and user avatar with dropdown
              <div className="flex items-center gap-3">
                {/* Notification Bell */}
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

                {/* User Menu Dropdown */}
                <div className="relative" ref={userMenuRef}>
                  <div
                    className="flex items-center gap-2 cursor-pointer hover:opacity-90 transition-opacity"
                    onClick={() => setUserMenuOpen(!userMenuOpen)}
                    role="button"
                    tabIndex={0}
                    onKeyDown={(e) => {
                      if (e.key === 'Enter' || e.key === ' ') {
                        setUserMenuOpen(!userMenuOpen);
                      }
                    }}
                    title={user.fullName}
                  >
                    {/* User Avatar */}
                    <div
                      className="w-10 h-10 rounded-full flex items-center justify-center text-white font-bold"
                      style={{
                        background: 'linear-gradient(135deg, #FF7900, #8B1538)',
                      }}
                    >
                      {getUserInitials(user.fullName)}
                    </div>

                    {/* User Name */}
                    <span className="text-sm font-medium text-[#333] hidden lg:inline">
                      {user.fullName}
                    </span>

                    {/* Dropdown Indicator Arrow */}
                    <ChevronDown
                      className={`w-4 h-4 text-[#666] transition-transform duration-200 hidden lg:block ${
                        userMenuOpen ? 'transform rotate-180' : ''
                      }`}
                    />
                  </div>

                  {/* Dropdown Menu */}
                  {userMenuOpen && (
                    <div
                      className="absolute right-0 mt-2 w-48 rounded-lg shadow-lg overflow-hidden z-50"
                      style={{
                        background: 'white',
                        border: '1px solid #e2e8f0'
                      }}
                    >
                      {/* User Info Section */}
                      <div className="px-4 py-3 border-b border-gray-200" style={{ background: '#f7fafc' }}>
                        <p className="text-sm font-medium text-[#333]">{user.fullName}</p>
                        <p className="text-xs text-gray-500">{user.email}</p>
                      </div>

                      {/* Profile Button */}
                      <button
                        onClick={() => {
                          setUserMenuOpen(false);
                          router.push('/profile');
                        }}
                        className="w-full flex items-center gap-3 px-4 py-3 hover:bg-gray-50 transition-colors text-left"
                      >
                        <User className="w-4 h-4" style={{ color: '#FF7900' }} />
                        <span style={{ color: '#2d3748' }}>Profile</span>
                      </button>

                      <div style={{ borderTop: '1px solid #e2e8f0' }}></div>

                      {/* Logout Button */}
                      <button
                        onClick={async () => {
                          setUserMenuOpen(false);
                          try {
                            // Call logout endpoint if needed
                            const { authRepository } = await import('@/infrastructure/api/repositories/auth.repository');
                            await authRepository.logout();
                          } catch (error) {
                            // Silently handle logout errors
                          } finally {
                            clearAuth();
                            router.push('/login');
                          }
                        }}
                        className="w-full flex items-center gap-3 px-4 py-3 hover:bg-gray-50 transition-colors text-left"
                      >
                        <LogOut className="w-4 h-4" style={{ color: '#8B1538' }} />
                        <span style={{ color: '#2d3748' }}>Logout</span>
                      </button>
                    </div>
                  )}
                </div>
              </div>
            ) : (
              // Not authenticated: Show Login and Sign Up buttons
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
      </nav>
    </header>
  );
}
