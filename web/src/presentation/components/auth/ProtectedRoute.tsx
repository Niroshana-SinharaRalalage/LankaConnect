'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useAuthStore, useHasHydrated } from '@/presentation/store/useAuthStore';

interface ProtectedRouteProps {
  children: React.ReactNode;
}

/**
 * ProtectedRoute Component
 * Wrapper for routes that require authentication
 * Redirects to login if not authenticated
 * Handles Zustand hydration to prevent redirect on page refresh
 */
export function ProtectedRoute({ children }: ProtectedRouteProps) {
  const router = useRouter();
  const { isAuthenticated, isLoading } = useAuthStore();
  const hasHydrated = useHasHydrated();

  useEffect(() => {
    // Only redirect after hydration is complete
    if (hasHydrated && !isLoading && !isAuthenticated) {
      console.log('🔍 [PROTECTED ROUTE] Redirecting to login - hasHydrated:', hasHydrated, 'isLoading:', isLoading, 'isAuthenticated:', isAuthenticated);
      router.push('/login');
    }
  }, [isAuthenticated, isLoading, hasHydrated, router]);

  // Show loading state while hydrating or checking authentication
  if (!hasHydrated || isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center" style={{ background: '#f7fafc' }}>
        <div className="text-center">
          <div
            className="animate-spin rounded-full h-12 w-12 border-b-2 mx-auto mb-4"
            style={{ borderColor: '#FF7900' }}
          ></div>
          <p style={{ color: '#8B1538', fontSize: '0.9rem' }}>Loading...</p>
        </div>
      </div>
    );
  }

  // Don't render children if not authenticated
  if (!isAuthenticated) {
    return null;
  }

  return <>{children}</>;
}
