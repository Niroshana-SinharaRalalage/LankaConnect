'use client';

import * as React from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { Logo } from '@/presentation/components/atoms/Logo';
import { Button } from '@/presentation/components/ui/Button';
import { useAuthStore } from '@/presentation/store/useAuthStore';

export interface HeaderProps {
  className?: string;
}

/**
 * Header Component
 * Reusable navigation header for LankaConnect application
 * Features: Sticky navigation, authentication state, responsive design
 * Styling: Sri Lankan flag colors (Maroon #8B1538, Saffron #FF7900)
 */
export function Header({ className = '' }: HeaderProps) {
  const { user, isAuthenticated } = useAuthStore();
  const router = useRouter();

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
          {/* Logo with LankaConnect Text - Both Clickable */}
          <Link href="/" className="flex items-center hover:opacity-90 transition-opacity">
            <Logo size="lg" showText={false} />
            <span className="ml-3 text-2xl font-bold text-[#8B1538]">LankaConnect</span>
          </Link>

          {/* Navigation Links - Hidden on mobile */}
          <ul className="hidden md:flex items-center gap-8">
            <li>
              <Link
                href="/"
                className="text-[#333] hover:text-[#FF7900] font-medium transition-colors"
              >
                Home
              </Link>
            </li>
            <li>
              <Link
                href="#events"
                className="text-[#333] hover:text-[#FF7900] font-medium transition-colors"
              >
                Events
              </Link>
            </li>
            <li>
              <Link
                href="#forums"
                className="text-[#333] hover:text-[#FF7900] font-medium transition-colors"
              >
                Forums
              </Link>
            </li>
            <li>
              <Link
                href="#business"
                className="text-[#333] hover:text-[#FF7900] font-medium transition-colors"
              >
                Business
              </Link>
            </li>
            <li>
              <Link
                href="#culture"
                className="text-[#333] hover:text-[#FF7900] font-medium transition-colors"
              >
                Culture
              </Link>
            </li>
            {isAuthenticated && (
              <li>
                <Link
                  href="/dashboard"
                  className="text-[#333] hover:text-[#FF7900] font-medium transition-colors"
                >
                  Dashboard
                </Link>
              </li>
            )}
          </ul>

          {/* Auth Section */}
          <div className="flex items-center gap-4">
            {isAuthenticated && user ? (
              // Authenticated: Show user avatar with name
              <div className="flex items-center gap-3">
                <span className="text-sm font-medium text-[#333] hidden lg:inline">
                  {user.fullName}
                </span>
                <div
                  className="w-10 h-10 rounded-full flex items-center justify-center text-white font-bold cursor-pointer hover:opacity-90 transition-opacity"
                  style={{
                    background: 'linear-gradient(135deg, #FF7900, #8B1538)',
                  }}
                  onClick={() => router.push('/profile')}
                  title={user.fullName}
                  role="button"
                  tabIndex={0}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter' || e.key === ' ') {
                      router.push('/profile');
                    }
                  }}
                >
                  {getUserInitials(user.fullName)}
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
