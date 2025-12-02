'use client';

import { use } from 'react';
import { useRouter } from 'next/navigation';
import { ArrowLeft, Calendar, MapPin, Users, DollarSign, Clock, AlertCircle } from 'lucide-react';
import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Badge } from '@/presentation/components/ui/Badge';
import { useEventById, useRsvpToEvent } from '@/presentation/hooks/useEvents';
import { SignUpManagementSection } from '@/presentation/components/features/events/SignUpManagementSection';
import { EventRegistrationForm } from '@/presentation/components/features/events/EventRegistrationForm';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { EventCategory, EventStatus, type AnonymousRegistrationRequest } from '@/infrastructure/api/types/events.types';
import { paymentsRepository } from '@/infrastructure/api/repositories/payments.repository';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import { useState } from 'react';

/**
 * Event Detail Page
 * Displays full event details with RSVP, Stripe payment, waitlist, and sign-up management
 */
export default function EventDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const router = useRouter();
  const { user } = useAuthStore();
  const [isProcessing, setIsProcessing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isJoiningWaitlist, setIsJoiningWaitlist] = useState(false);
  const [isPublishing, setIsPublishing] = useState(false);

  // Fetch event details
  const { data: event, isLoading, error: fetchError } = useEventById(id);

  // RSVP mutation
  const rsvpMutation = useRsvpToEvent();

  // Category labels
  const categoryLabels: Record<EventCategory, string> = {
    [EventCategory.Religious]: 'Religious',
    [EventCategory.Cultural]: 'Cultural',
    [EventCategory.Community]: 'Community',
    [EventCategory.Educational]: 'Educational',
    [EventCategory.Social]: 'Social',
    [EventCategory.Business]: 'Business',
    [EventCategory.Charity]: 'Charity',
    [EventCategory.Entertainment]: 'Entertainment',
  };

  // Handle Registration (both anonymous and authenticated)
  const handleRegistration = async (data: AnonymousRegistrationRequest | { userId: string; quantity: number }) => {
    if (!event) return;

    try {
      setIsProcessing(true);
      setError(null);

      // Check if this is anonymous or authenticated registration
      if ('userId' in data) {
        // Authenticated user registration
        // For paid events, redirect to Stripe Checkout
        if (!event.isFree && event.ticketPriceAmount) {
          // TODO: Integrate with Stripe checkout session for paid events
          // For now, handle as free RSVP
          await rsvpMutation.mutateAsync({
            eventId: id,
            userId: data.userId,
            quantity: data.quantity,
          });
        } else {
          // Free event - direct RSVP
          await rsvpMutation.mutateAsync({
            eventId: id,
            userId: data.userId,
            quantity: data.quantity,
          });
        }
      } else {
        // Anonymous registration
        await eventsRepository.registerAnonymous(id, data);

        // Show success message
        alert('Registration successful! We\'ve sent a confirmation email to ' + data.email);

        // Reload to show updated registration count
        window.location.reload();
      }

      setIsProcessing(false);
    } catch (err) {
      console.error('Registration failed:', err);
      setError(err instanceof Error ? err.message : 'Failed to register. Please try again.');
      setIsProcessing(false);
    }
  };

  // Handle Waitlist
  const handleJoinWaitlist = async () => {
    if (!user?.userId) {
      router.push('/login?redirect=' + encodeURIComponent(`/events/${id}`));
      return;
    }

    try {
      setIsJoiningWaitlist(true);
      setError(null);
      await eventsRepository.addToWaitingList(id);
      setIsJoiningWaitlist(false);
      // Show success message or update UI
      alert('Successfully joined waitlist! You will be notified when a spot becomes available.');
    } catch (err) {
      console.error('Failed to join waitlist:', err);
      setError(err instanceof Error ? err.message : 'Failed to join waitlist. Please try again.');
      setIsJoiningWaitlist(false);
    }
  };

  // Handle Publish Event
  const handlePublishEvent = async () => {
    if (!event || event.organizerId !== user?.userId) {
      return;
    }

    try {
      setIsPublishing(true);
      setError(null);
      await eventsRepository.publishEvent(id);
      setIsPublishing(false);
      // Reload page to show updated status
      window.location.reload();
    } catch (err) {
      console.error('Failed to publish event:', err);
      setError(err instanceof Error ? err.message : 'Failed to publish event. Please try again.');
      setIsPublishing(false);
    }
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
        <Header />
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
          <Card className="animate-pulse">
            <CardContent className="p-12">
              <div className="h-8 bg-neutral-200 rounded w-3/4 mb-4"></div>
              <div className="h-4 bg-neutral-200 rounded w-1/2 mb-8"></div>
              <div className="h-64 bg-neutral-200 rounded mb-8"></div>
              <div className="h-32 bg-neutral-200 rounded"></div>
            </CardContent>
          </Card>
        </div>
        <Footer />
      </div>
    );
  }

  if (fetchError || !event) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
        <Header />
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
          <Card>
            <CardContent className="p-12 text-center">
              <AlertCircle className="h-16 w-16 mx-auto mb-4 text-destructive" />
              <h3 className="text-xl font-semibold text-neutral-900 mb-2">
                Event Not Found
              </h3>
              <p className="text-neutral-500 mb-6">
                The event you're looking for doesn't exist or has been removed.
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

  const startDate = new Date(event.startDate);
  const endDate = new Date(event.endDate);
  const formattedStartDate = startDate.toLocaleDateString('en-US', {
    month: 'long',
    day: 'numeric',
    year: 'numeric',
  });
  const formattedStartTime = startDate.toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit',
  });
  const formattedEndTime = endDate.toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit',
  });

  const isFull = event.currentRegistrations >= event.capacity;
  const spotsLeft = event.capacity - event.currentRegistrations;

  return (
    <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
      <Header />

      {/* Back Button and Organizer Actions */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
        <div className="flex items-center justify-between gap-4">
          <Button
            variant="outline"
            onClick={() => router.push('/events')}
            className="flex items-center gap-2"
          >
            <ArrowLeft className="h-4 w-4" />
            Back to Events
          </Button>

          {/* Organizer-only actions */}
          {event && user && event.organizerId === user.userId && (
            <div className="flex items-center gap-3">
              {/* Publish button - only show for Draft events */}
              {event.status === EventStatus.Draft && (
                <Button
                  onClick={handlePublishEvent}
                  disabled={isPublishing}
                  className="flex items-center gap-2"
                  style={{ background: '#10B981' }}
                >
                  {isPublishing ? 'Publishing...' : 'Publish Event'}
                </Button>
              )}
            </div>
          )}
        </div>
      </div>

      {/* Event Hero Section */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 pb-12">
        <Card className="overflow-hidden">
          {/* Event Image */}
          {event.images && event.images.length > 0 && (
            <div className="relative h-96 bg-gradient-to-br from-orange-500 to-rose-500">
              <img
                src={event.images[0].imageUrl}
                alt={event.title}
                className="w-full h-full object-cover"
              />
              <div className="absolute top-4 right-4">
                <Badge
                  variant="default"
                  className="text-white shadow-lg text-base px-4 py-2"
                  style={{ background: '#8B1538' }}
                >
                  {categoryLabels[event.category]}
                </Badge>
              </div>
            </div>
          )}

          <CardContent className="p-8">
            {/* Title and Description */}
            <div className="mb-8">
              <h1 className="text-4xl font-bold text-neutral-900 mb-4">
                {event.title}
              </h1>
              <p className="text-lg text-neutral-600 leading-relaxed whitespace-pre-wrap">
                {event.description}
              </p>
            </div>

            {/* Event Details Grid */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-8">
              {/* Date & Time */}
              <div className="flex items-start gap-3">
                <div className="p-3 rounded-lg" style={{ background: '#FFF4ED' }}>
                  <Calendar className="h-6 w-6" style={{ color: '#FF7900' }} />
                </div>
                <div>
                  <p className="text-sm font-medium text-neutral-500">Date & Time</p>
                  <p className="text-base font-semibold text-neutral-900">
                    {formattedStartDate}
                  </p>
                  <p className="text-sm text-neutral-600">
                    {formattedStartTime} - {formattedEndTime}
                  </p>
                </div>
              </div>

              {/* Location */}
              {event.city && event.state && (
                <div className="flex items-start gap-3">
                  <div className="p-3 rounded-lg" style={{ background: '#FFF4ED' }}>
                    <MapPin className="h-6 w-6" style={{ color: '#FF7900' }} />
                  </div>
                  <div>
                    <p className="text-sm font-medium text-neutral-500">Location</p>
                    <p className="text-base font-semibold text-neutral-900">
                      {event.city}, {event.state}
                    </p>
                    {event.address && (
                      <p className="text-sm text-neutral-600">{event.address}</p>
                    )}
                  </div>
                </div>
              )}

              {/* Capacity */}
              <div className="flex items-start gap-3">
                <div className="p-3 rounded-lg" style={{ background: '#FFF4ED' }}>
                  <Users className="h-6 w-6" style={{ color: '#FF7900' }} />
                </div>
                <div>
                  <p className="text-sm font-medium text-neutral-500">Capacity</p>
                  <p className="text-base font-semibold text-neutral-900">
                    {event.currentRegistrations} / {event.capacity} registered
                  </p>
                  {isFull ? (
                    <Badge className="mt-1 bg-red-600 text-white">Event Full</Badge>
                  ) : (
                    <p className="text-sm text-neutral-600">
                      {spotsLeft} {spotsLeft === 1 ? 'spot' : 'spots'} remaining
                    </p>
                  )}
                </div>
              </div>

              {/* Pricing */}
              <div className="flex items-start gap-3">
                <div className="p-3 rounded-lg" style={{ background: '#FFF4ED' }}>
                  <DollarSign className="h-6 w-6" style={{ color: '#FF7900' }} />
                </div>
                <div>
                  <p className="text-sm font-medium text-neutral-500">Pricing</p>
                  <p className="text-base font-semibold" style={{ color: '#8B1538' }}>
                    {event.isFree ? 'Free Event' : `$${event.ticketPriceAmount?.toFixed(2)} per person`}
                  </p>
                  {!event.isFree && (
                    <p className="text-sm text-neutral-600">
                      {event.ticketPriceCurrency === 1 ? 'USD' : 'LKR'}
                    </p>
                  )}
                </div>
              </div>
            </div>

            {/* Registration Section */}
            <Card className="border-2" style={{ borderColor: '#FF7900' }}>
              <CardHeader>
                <CardTitle>Register for this Event</CardTitle>
                <CardDescription>
                  {isFull
                    ? 'This event is currently full. Join the waitlist to be notified when spots become available.'
                    : 'Reserve your spot now!'}
                </CardDescription>
              </CardHeader>
              <CardContent>
                {!isFull ? (
                  <EventRegistrationForm
                    eventId={id}
                    spotsLeft={spotsLeft}
                    isFree={event.isFree}
                    ticketPrice={event.ticketPriceAmount ?? undefined}
                    isProcessing={isProcessing}
                    onSubmit={handleRegistration}
                    error={error}
                  />
                ) : (
                  <>
                    {/* Waitlist Section */}
                    <div className="space-y-4">
                      <Button
                        onClick={handleJoinWaitlist}
                        disabled={isJoiningWaitlist}
                        className="w-full text-lg py-6"
                        variant="outline"
                        style={{ borderColor: '#FF7900', color: '#FF7900' }}
                      >
                        {isJoiningWaitlist ? (
                          <>
                            <Clock className="h-5 w-5 mr-2 animate-spin" />
                            Joining...
                          </>
                        ) : (
                          'Join Waitlist'
                        )}
                      </Button>

                      {!user?.userId && (
                        <p className="text-sm text-center text-neutral-500">
                          You'll be redirected to login before joining the waitlist
                        </p>
                      )}
                    </div>

                    {error && (
                      <div className="mt-4 p-4 bg-red-50 border border-red-200 rounded-lg">
                        <p className="text-sm text-red-600">{error}</p>
                      </div>
                    )}
                  </>
                )}
              </CardContent>
            </Card>
          </CardContent>
        </Card>

        {/* Sign-Up Management Section */}
        <div className="mt-8">
          <SignUpManagementSection
            eventId={id}
            userId={user?.userId}
            isOrganizer={event.organizerId === user?.userId}
          />
        </div>
      </div>

      <Footer />
    </div>
  );
}
