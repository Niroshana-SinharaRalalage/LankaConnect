'use client';

import * as React from 'react';
import Link from 'next/link';
import { Button } from '@/presentation/components/ui/Button';
import { Logo } from '@/presentation/components/atoms/Logo';
import { Home, ArrowLeft, Search } from 'lucide-react';

/**
 * 404 Not Found Page Component
 * Custom error page with Sri Lankan flag colors and branding
 * Features: Flag header, logo, error message, navigation buttons
 */
export default function NotFound() {
  const [isAuthenticated, setIsAuthenticated] = React.useState(false);

  React.useEffect(() => {
    // Check if user is authenticated by checking for auth token
    if (typeof window !== 'undefined') {
      const token = localStorage.getItem('auth_token');
      setIsAuthenticated(!!token);
    }
  }, []);

  return (
    <div className="min-h-screen bg-white">
      {/* Sri Lankan Flag Header Stripe */}
      <div
        className="h-[8px]"
        style={{
          background: 'linear-gradient(90deg, #FF7900 0%, #FF7900 33%, #8B1538 33%, #8B1538 66%, #006400 66%, #006400 100%)'
        }}
      />

      {/* Main Content Container */}
      <div className="container mx-auto px-4 sm:px-6 lg:px-8 min-h-[calc(100vh-8px)] flex items-center justify-center">
        <div className="max-w-2xl w-full text-center">
          {/* Logo and Brand */}
          <div className="flex justify-center mb-8">
            <div className="flex flex-col items-center">
              <Logo size="xl" showText={false} />
              <span className="mt-4 text-3xl font-bold text-[#8B1538]">LankaConnect</span>
            </div>
          </div>

          {/* 404 Error Display with Sri Lankan Flag Gradient */}
          <div
            className="inline-block px-8 py-6 rounded-2xl mb-8"
            style={{
              background: 'linear-gradient(135deg, #FF7900 0%, #8B1538 50%, #006400 100%)'
            }}
          >
            <h1 className="text-8xl font-bold text-white mb-2">404</h1>
            <p className="text-xl text-white/90">Page Not Found</p>
          </div>

          {/* Error Message */}
          <div className="mb-8">
            <h2 className="text-2xl font-semibold text-[#8B1538] mb-3">
              Oops! This page seems to have wandered off
            </h2>
            <p className="text-lg text-[#718096] leading-relaxed max-w-xl mx-auto">
              The page you're looking for doesn't exist or has been moved.
              Don't worry, you can navigate back to safety using the buttons below.
            </p>
          </div>

          {/* Decorative Flag Accent */}
          <div className="flex justify-center gap-3 mb-8">
            <div className="w-12 h-1 bg-[#FF7900] rounded-full"></div>
            <div className="w-12 h-1 bg-[#8B1538] rounded-full"></div>
            <div className="w-12 h-1 bg-[#006400] rounded-full"></div>
          </div>

          {/* Navigation Buttons */}
          <div className="flex flex-col sm:flex-row gap-4 justify-center items-center mb-8">
            <Link href="/">
              <Button
                size="lg"
                className="bg-[#FF7900] hover:bg-[#E66D00] text-white font-semibold transition-all shadow-md hover:shadow-lg min-w-[180px]"
              >
                <Home className="mr-2 h-5 w-5" />
                Go Home
              </Button>
            </Link>

            {isAuthenticated && (
              <Link href="/dashboard">
                <Button
                  size="lg"
                  variant="outline"
                  className="border-2 border-[#8B1538] text-[#8B1538] hover:bg-[#8B1538] hover:text-white font-semibold transition-all min-w-[180px]"
                >
                  <ArrowLeft className="mr-2 h-5 w-5" />
                  Go to Dashboard
                </Button>
              </Link>
            )}

            <Link href="/">
              <Button
                size="lg"
                variant="outline"
                className="border-2 border-[#006400] text-[#006400] hover:bg-[#006400] hover:text-white font-semibold transition-all min-w-[180px]"
              >
                <Search className="mr-2 h-5 w-5" />
                Search Events
              </Button>
            </Link>
          </div>

          {/* Helpful Links */}
          <div className="pt-8 border-t border-[#e2e8f0]">
            <p className="text-sm text-[#718096] mb-4">
              Looking for something specific? Try these popular pages:
            </p>
            <div className="flex flex-wrap justify-center gap-4 text-sm">
              <Link
                href="/"
                className="text-[#FF7900] hover:text-[#E66D00] font-medium transition-colors"
              >
                Community Feed
              </Link>
              <span className="text-[#cbd5e0]">•</span>
              <Link
                href="/events"
                className="text-[#FF7900] hover:text-[#E66D00] font-medium transition-colors"
              >
                Upcoming Events
              </Link>
              <span className="text-[#cbd5e0]">•</span>
              <Link
                href="/forums"
                className="text-[#FF7900] hover:text-[#E66D00] font-medium transition-colors"
              >
                Discussion Forums
              </Link>
              <span className="text-[#cbd5e0]">•</span>
              <Link
                href="/businesses"
                className="text-[#FF7900] hover:text-[#E66D00] font-medium transition-colors"
              >
                Local Businesses
              </Link>
            </div>
          </div>

          {/* Footer Note */}
          <div className="mt-12 text-xs text-[#a0aec0]">
            <p>Need help? Contact us at support@lankaconnect.com</p>
          </div>
        </div>
      </div>
    </div>
  );
}
