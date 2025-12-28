'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { EventCreationForm } from '@/presentation/components/features/events/EventCreationForm';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { ArrowLeft } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { UserRole } from '@/infrastructure/api/types/auth.types';

/**
 * Event Creation Page
 * Allows authenticated organizers to create new events
 *
 * Features:
 * - Authentication required (redirects to login if not authenticated)
 * - Comprehensive event creation form
 * - Validates all fields before submission
 * - Redirects to event detail page after successful creation
 */
export default function CreateEventPage() {
  const router = useRouter();
  const { user, isAuthenticated, _hasHydrated } = useAuthStore();

  // Redirect to login if not authenticated, or to dashboard if unauthorized
  // IMPORTANT: Wait for hydration to complete before checking auth
  useEffect(() => {
    // Don't check auth until hydration is complete
    if (!_hasHydrated) {
      return;
    }

    if (!isAuthenticated || !user?.userId) {
      router.push('/login?redirect=' + encodeURIComponent('/events/create'));
      return;
    }

    // Check if user has permission to create events (EventOrganizer, Admin, or AdminManager)
    const canCreate = user.role === UserRole.EventOrganizer ||
                       user.role === UserRole.Admin ||
                       user.role === UserRole.AdminManager;

    if (!canCreate) {
      // Redirect unauthorized users to dashboard
      router.push('/dashboard');
    }
  }, [isAuthenticated, user, router, _hasHydrated]);

  // Don't render form until hydration is complete and authentication is confirmed
  if (!_hasHydrated || !isAuthenticated || !user?.userId) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
        <Header />
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
          <div className="text-center">
            <p className="text-neutral-500">Redirecting to login...</p>
          </div>
        </div>
        <Footer />
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
      <Header />

      {/* Page Header */}
      <div className="bg-gradient-to-r from-orange-600 via-rose-800 to-emerald-800 py-12 relative overflow-hidden">
        {/* Decorative Background Pattern */}
        <div className="absolute inset-0 opacity-10">
          <div
            className="absolute inset-0"
            style={{
              backgroundImage: `url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%23ffffff' fill-opacity='1'%3E%3Cpath d='M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")`,
            }}
          ></div>
        </div>

        {/* Decorative gradient blobs */}
        <div className="absolute inset-0 overflow-hidden">
          <div className="absolute -top-24 -left-24 w-96 h-96 bg-orange-400/20 rounded-full blur-3xl"></div>
          <div className="absolute -bottom-24 -right-24 w-96 h-96 bg-emerald-400/20 rounded-full blur-3xl"></div>
          <div className="absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2 w-96 h-96 bg-rose-400/10 rounded-full blur-3xl"></div>
        </div>

        <div className="relative max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center">
            <h1 className="text-4xl font-bold text-white mb-4">
              Create New Event
            </h1>
            <p className="text-lg text-white/90 max-w-2xl mx-auto">
              Share your event with the community and start receiving registrations
            </p>
          </div>
        </div>
      </div>

      {/* Back Button - Session 33: Navigate to dashboard instead of events */}
      <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
        <Button
          variant="outline"
          onClick={() => router.push('/dashboard')}
          className="flex items-center gap-2"
        >
          <ArrowLeft className="h-4 w-4" />
          Back to Dashboard
        </Button>
      </div>

      {/* Event Creation Form */}
      <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 pb-12">
        <EventCreationForm />
      </div>

      <Footer />
    </div>
  );
}
