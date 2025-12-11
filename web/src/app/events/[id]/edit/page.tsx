'use client';

import { use, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { ArrowLeft, AlertCircle } from 'lucide-react';
import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { Card, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { EventEditForm } from '@/presentation/components/features/events/EventEditForm';
import { useEventById } from '@/presentation/hooks/useEvents';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { UserRole } from '@/infrastructure/api/types/auth.types';

/**
 * Event Edit Page
 * Allows event organizers to edit their events
 *
 * Features:
 * - Authentication required (redirects to login if not authenticated)
 * - Authorization check (only event organizer can edit)
 * - Pre-filled form with existing event data
 * - Validates all fields before submission
 * - Redirects to event detail page after successful update
 */
export default function EditEventPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const router = useRouter();
  const { user, isAuthenticated } = useAuthStore();
  const { data: event, isLoading, error: fetchError } = useEventById(id);

  // Redirect to login if not authenticated
  useEffect(() => {
    if (!isAuthenticated || !user?.userId) {
      router.push('/login?redirect=' + encodeURIComponent(`/events/${id}/edit`));
      return;
    }

    // Check if user has permission to edit events
    const canEdit = user.role === UserRole.EventOrganizer ||
                     user.role === UserRole.Admin ||
                     user.role === UserRole.AdminManager;

    if (!canEdit) {
      router.push('/dashboard');
    }
  }, [isAuthenticated, user, router, id]);

  // Check if user is the event organizer
  useEffect(() => {
    if (event && user && event.organizerId !== user.userId) {
      // Only event organizer can edit
      const isAdmin = user.role === UserRole.Admin || user.role === UserRole.AdminManager;
      if (!isAdmin) {
        router.push(`/events/${id}`);
      }
    }
  }, [event, user, router, id]);

  // Loading state
  if (isLoading || !isAuthenticated || !user?.userId) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
        <Header />
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
          <Card className="animate-pulse">
            <CardContent className="p-12">
              <div className="h-8 bg-neutral-200 rounded w-3/4 mb-4"></div>
              <div className="h-4 bg-neutral-200 rounded w-1/2 mb-8"></div>
              <div className="h-64 bg-neutral-200 rounded mb-8"></div>
            </CardContent>
          </Card>
        </div>
        <Footer />
      </div>
    );
  }

  // Error state
  if (fetchError || !event) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
        <Header />
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
          <Card>
            <CardContent className="p-12 text-center">
              <AlertCircle className="h-16 w-16 mx-auto mb-4 text-destructive" />
              <h3 className="text-xl font-semibold text-neutral-900 mb-2">
                Event Not Found
              </h3>
              <p className="text-neutral-500 mb-6">
                The event you're trying to edit doesn't exist or has been removed.
              </p>
              <Button onClick={() => router.push('/events')}>
                Back to Events
              </Button>
            </CardContent>
          </Card>
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
              Edit Event
            </h1>
            <p className="text-lg text-white/90 max-w-2xl mx-auto">
              Update your event details and keep your attendees informed
            </p>
          </div>
        </div>
      </div>

      {/* Back Button */}
      <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
        <Button
          variant="outline"
          onClick={() => router.push(`/events/${id}/manage`)}
          className="flex items-center gap-2"
        >
          <ArrowLeft className="h-4 w-4" />
          Back to Manage Event
        </Button>
      </div>

      {/* Event Edit Form */}
      <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 pb-12">
        <EventEditForm event={event} />
      </div>

      <Footer />
    </div>
  );
}
