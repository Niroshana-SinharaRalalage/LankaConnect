'use client';

import { use } from 'react';
import { useRouter } from 'next/navigation';
import { ArrowLeft, Calendar, MapPin, Users, DollarSign, Clock, AlertCircle } from 'lucide-react';
import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Badge } from '@/presentation/components/ui/Badge';
import { useEventById, useRsvpToEvent, useUserRsvpForEvent, useUserRegistrationDetails } from '@/presentation/hooks/useEvents';
import { SignUpManagementSection } from '@/presentation/components/features/events/SignUpManagementSection';
import { EventRegistrationForm } from '@/presentation/components/features/events/EventRegistrationForm';
import { MediaGallery } from '@/presentation/components/features/events/MediaGallery';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { EventCategory, EventStatus, type AnonymousRegistrationRequest, type RsvpRequest } from '@/infrastructure/api/types/events.types';
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
  const { user, isHydrated } = useAuthStore();
  const [isProcessing, setIsProcessing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isJoiningWaitlist, setIsJoiningWaitlist] = useState(false);
  const [isPublishing, setIsPublishing] = useState(false);
  const [showCancelConfirm, setShowCancelConfirm] = useState(false);
  const [cancelError, setCancelError] = useState<string | null>(null);
  const [isCancelling, setIsCancelling] = useState(false);

  // Fetch event details
  const { data: event, isLoading, error: fetchError } = useEventById(id);

  // Check if user is already registered for this event
  // Only enable after hydration to prevent race condition with token restoration
  const { data: userRsvp, isLoading: isLoadingRsvp } = useUserRsvpForEvent(
    (user?.userId && isHydrated) ? id : undefined
  );
  const isUserRegistered = !!userRsvp;

  // Fix 1: Fetch full registration details with attendee information
  // Only fetch when user is registered to avoid unnecessary 404/401 errors
  // Only enable after hydration to prevent race condition with token restoration
  const { data: registrationDetails, isLoading: isLoadingRegistration } = useUserRegistrationDetails(
    (user?.userId && isHydrated) ? id : undefined,
    isUserRegistered // Only enabled when user is actually registered
  );

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
  const handleRegistration = async (data: AnonymousRegistrationRequest | RsvpRequest) => {
    if (!event) return;

    try {
      setIsProcessing(true);
      setError(null);

      // Check if this is anonymous or authenticated registration
      if ('userId' in data) {
        // Authenticated user registration (RsvpRequest)
        // Session 23: Build redirect URLs for payment flow
        const baseUrl = typeof window !== 'undefined' ? window.location.origin : '';
        const successUrl = `${baseUrl}/events/payment/success?eventId=${id}`;
        const cancelUrl = `${baseUrl}/events/payment/cancel?eventId=${id}`;

        // Session 23: RSVP with payment support
        // Backend returns checkout URL for paid events, null for free events
        // Phase 6A.11 FIX: Always send both quantity AND attendees (backend expects both)
        const checkoutUrl = await rsvpMutation.mutateAsync({
          eventId: id,
          userId: data.userId,
          quantity: data.attendees?.length || (data as any).quantity || 1,
          attendees: data.attendees,
          email: data.email,
          phoneNumber: data.phoneNumber,
          address: data.address,
          successUrl,
          cancelUrl,
        });

        // If checkout URL is returned, redirect to Stripe for payment
        if (checkoutUrl) {
          // Paid event - redirect to Stripe Checkout
          window.location.href = checkoutUrl;
          return; // Don't set isProcessing false - user is being redirected
        }

        // Free event - show success message and reload
        alert('Registration successful!');
        window.location.reload();
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
                src={(event.images.find(img => img.isPrimary) || event.images[0]).imageUrl}
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

              {/* Pricing - Session 23: Dual pricing support */}
              <div className="flex items-start gap-3">
                <div className="p-3 rounded-lg" style={{ background: '#FFF4ED' }}>
                  <DollarSign className="h-6 w-6" style={{ color: '#FF7900' }} />
                </div>
                <div>
                  <p className="text-sm font-medium text-neutral-500">Pricing</p>
                  {event.isFree ? (
                    <p className="text-base font-semibold" style={{ color: '#8B1538' }}>
                      Free Event
                    </p>
                  ) : event.hasDualPricing ? (
                    <>
                      <p className="text-base font-semibold" style={{ color: '#8B1538' }}>
                        Adult: ${event.adultPriceAmount?.toFixed(2)}
                      </p>
                      <p className="text-base font-semibold" style={{ color: '#8B1538' }}>
                        Child (under {event.childAgeLimit}): ${event.childPriceAmount?.toFixed(2)}
                      </p>
                      <p className="text-sm text-neutral-600">
                        {event.adultPriceCurrency === 1 ? 'USD' : 'LKR'}
                      </p>
                    </>
                  ) : (
                    <>
                      <p className="text-base font-semibold" style={{ color: '#8B1538' }}>
                        ${event.ticketPriceAmount?.toFixed(2)} per person
                      </p>
                      <p className="text-sm text-neutral-600">
                        {event.ticketPriceCurrency === 1 ? 'USD' : 'LKR'}
                      </p>
                    </>
                  )}
                </div>
              </div>
            </div>

            {/* Media Gallery */}
            {((event.images && event.images.length > 0) || (event.videos && event.videos.length > 0)) && (
              <div className="mb-8">
                <MediaGallery images={event.images} videos={event.videos} />
              </div>
            )}

            {/* Registration Section */}
            <Card className="border-2" style={{ borderColor: '#FF7900' }}>
              <CardHeader>
                <CardTitle>
                  {isUserRegistered ? 'Your Registration' : 'Register for this Event'}
                </CardTitle>
                <CardDescription>
                  {isUserRegistered
                    ? 'You are already registered for this event!'
                    : isFull
                    ? 'This event is currently full. Join the waitlist to be notified when spots become available.'
                    : 'Reserve your spot now!'}
                </CardDescription>
              </CardHeader>
              <CardContent>
                {isUserRegistered ? (
                  // Show registration status when user is already registered
                  <div className="space-y-4">
                    <div className="p-4 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg">
                      <div className="flex items-center gap-2 mb-3">
                        <svg
                          className="h-5 w-5 text-green-600 dark:text-green-400"
                          fill="none"
                          stroke="currentColor"
                          viewBox="0 0 24 24"
                        >
                          <path
                            strokeLinecap="round"
                            strokeLinejoin="round"
                            strokeWidth={2}
                            d="M5 13l4 4L19 7"
                          />
                        </svg>
                        <h3 className="text-lg font-semibold text-green-900 dark:text-green-100">
                          You're Registered!
                        </h3>
                      </div>
                      <p className="text-sm text-green-800 dark:text-green-200 mb-3">
                        You have successfully registered for this event. We look forward to seeing you there!
                      </p>

                      {/* Registration Summary with Attendee Details */}
                      <div className="mt-3 pt-3 border-t border-green-200 dark:border-green-700">
                        <h4 className="text-sm font-medium text-green-900 dark:text-green-100 mb-2">
                          Registration Details:
                        </h4>
                        {isLoadingRegistration ? (
                          <p className="text-sm text-green-800 dark:text-green-200">Loading registration details...</p>
                        ) : registrationDetails && registrationDetails.attendees && registrationDetails.attendees.length > 0 ? (
                          <div className="space-y-2">
                            <p className="text-sm font-medium text-green-900 dark:text-green-100">
                              Attendees ({registrationDetails.attendees.length}):
                            </p>
                            <ul className="space-y-1">
                              {registrationDetails.attendees.map((attendee, index) => (
                                <li key={index} className="text-sm text-green-800 dark:text-green-200 pl-3">
                                  • {attendee.name} (Age: {attendee.age})
                                </li>
                              ))}
                            </ul>
                            {registrationDetails.contactEmail && (
                              <p className="text-xs text-green-700 dark:text-green-300 mt-2">
                                Contact: {registrationDetails.contactEmail}
                              </p>
                            )}
                          </div>
                        ) : (
                          <div className="text-sm text-green-800 dark:text-green-200">
                            <p>Number of attendees: {registrationDetails?.quantity || userRsvp.currentRegistrations || 1}</p>
                          </div>
                        )}
                      </div>
                    </div>

                    {/* Cancel Error Message */}
                    {cancelError && (
                      <div className="mb-4 p-4 bg-red-50 border border-red-200 rounded-lg">
                        <div className="flex items-start gap-2">
                          <AlertCircle className="h-5 w-5 text-red-600 flex-shrink-0 mt-0.5" />
                          <div className="flex-1">
                            <h4 className="text-sm font-semibold text-red-900 mb-1">
                              Failed to Cancel Registration
                            </h4>
                            <p className="text-sm text-red-700">
                              {cancelError}
                            </p>
                            <p className="text-xs text-red-600 mt-2">
                              Please try again or contact support if the problem persists.
                            </p>
                          </div>
                        </div>
                      </div>
                    )}

                    {/* Edit and Cancel buttons */}
                    <div className="flex gap-3">
                      <Button
                        variant="outline"
                        className="flex-1"
                        onClick={() => {
                          alert('Edit registration feature coming soon! You will be able to update attendee names and ages.');
                        }}
                      >
                        Edit Registration
                      </Button>

                      {!showCancelConfirm ? (
                        <Button
                          variant="outline"
                          className="flex-1"
                          style={{ borderColor: '#EF4444', color: '#EF4444' }}
                          onClick={() => {
                            console.log('[CancelRsvp] User clicked Cancel Registration button');
                            setShowCancelConfirm(true);
                          }}
                        >
                          Cancel Registration
                        </Button>
                      ) : (
                        <div className="flex flex-1 gap-2">
                          <Button
                            variant="outline"
                            className="flex-1"
                            onClick={() => {
                              console.log('[CancelRsvp] User cancelled the cancellation');
                              setShowCancelConfirm(false);
                              setCancelError(null);
                              setIsCancelling(false);
                            }}
                          >
                            Keep Registration
                          </Button>
                          <Button
                            variant="outline"
                            className="flex-1"
                            disabled={isCancelling}
                            style={{
                              borderColor: '#EF4444',
                              color: '#FFFFFF',
                              backgroundColor: '#EF4444',
                              opacity: isCancelling ? 0.6 : 1
                            }}
                            onClick={async () => {
                              try {
                                console.log('[CancelRsvp] User confirmed cancellation, attempting to cancel registration for event:', id);
                                setIsCancelling(true);
                                setCancelError(null);
                                await eventsRepository.cancelRsvp(id);
                                console.log('[CancelRsvp] Successfully cancelled registration - reloading page');
                                window.location.reload();
                              } catch (error: any) {
                                console.error('[CancelRsvp] Failed to cancel registration:', error);
                                console.error('[CancelRsvp] Error details:', {
                                  message: error?.message,
                                  response: error?.response,
                                  status: error?.response?.status,
                                  data: error?.response?.data,
                                  detail: error?.response?.data?.detail
                                });
                                const errorMessage = error?.response?.data?.detail || error?.response?.data?.message || error?.message || 'Unknown error';
                                setCancelError(errorMessage);
                                setIsCancelling(false);
                                // Don't reset showCancelConfirm so user can see the error and try again
                              }
                            }}
                          >
                            {isCancelling ? 'Cancelling...' : 'Confirm Cancel'}
                          </Button>
                        </div>
                      )}
                    </div>
                  </div>
                ) : !isFull ? (
                  <EventRegistrationForm
                    eventId={id}
                    spotsLeft={spotsLeft}
                    isFree={event.isFree}
                    ticketPrice={event.ticketPriceAmount ?? undefined}
                    hasDualPricing={event.hasDualPricing}
                    adultPrice={event.adultPriceAmount ?? undefined}
                    childPrice={event.childPriceAmount ?? undefined}
                    childAgeLimit={event.childAgeLimit ?? undefined}
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
            isOrganizer={false}
          />
        </div>
      </div>

      <Footer />
    </div>
  );
}
