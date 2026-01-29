'use client';

import { use } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { ArrowLeft, Calendar, MapPin, Users, DollarSign, Clock, AlertCircle } from 'lucide-react';
import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Badge } from '@/presentation/components/ui/Badge';
import { useEventById, useRsvpToEvent, useUserRsvpForEvent, useUserRegistrationDetails, useUpdateRegistrationDetails } from '@/presentation/hooks/useEvents';
import { SignUpManagementSection } from '@/presentation/components/features/events/SignUpManagementSection';
import { EventRegistrationForm } from '@/presentation/components/features/events/EventRegistrationForm';
import { MediaGallery } from '@/presentation/components/features/events/MediaGallery';
import { EditRegistrationModal, type EditRegistrationData } from '@/presentation/components/features/events/EditRegistrationModal';
import { TicketSection } from '@/presentation/components/features/events/TicketSection';
import { RegistrationBadge } from '@/presentation/components/features/events/RegistrationBadge';
import { CheckoutCountdownTimer } from '@/presentation/components/features/events/CheckoutCountdownTimer';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from '@/presentation/components/ui/Dialog';
import { ConfirmDialog } from '@/presentation/components/ui/ConfirmDialog';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { EventCategory, EventStatus, RegistrationStatus, PaymentStatus, AgeCategory, Gender, type AnonymousRegistrationRequest, type RsvpRequest } from '@/infrastructure/api/types/events.types';
import { paymentsRepository } from '@/infrastructure/api/repositories/payments.repository';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import { useState, useEffect } from 'react';

/**
 * Phase 6A.46: Get badge color based on event lifecycle label
 * LankaConnect theme colors: Orange #FF7900, Rose #8B1538, Emerald #047857
 */
function getStatusBadgeColor(label: string): string {
  switch (label) {
    case 'New':
      return '#10B981'; // Emerald-500 - Fresh, exciting new events
    case 'Upcoming':
      return '#FF7900'; // LankaConnect Orange - Events starting soon
    case 'Published':
    case 'Active':
      return '#6366F1'; // Indigo-500 - Currently active events
    case 'Cancelled':
      return '#EF4444'; // Red-500 - Cancelled events
    case 'Completed':
      return '#6B7280'; // Gray-500 - Past events
    case 'Inactive':
      return '#9CA3AF'; // Gray-400 - Old inactive events
    case 'Draft':
      return '#F59E0B'; // Amber-500 - Draft events
    case 'Postponed':
      return '#F97316'; // Orange-500 - Postponed events
    case 'UnderReview':
      return '#8B5CF6'; // Violet-500 - Under admin review
    default:
      return '#8B1538'; // LankaConnect Rose - Default fallback
  }
}

/**
 * Event Detail Page
 * Displays full event details with RSVP, Stripe payment, waitlist, and sign-up management
 */
export default function EventDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const router = useRouter();
  const searchParams = useSearchParams();
  const { user, _hasHydrated } = useAuthStore();

  // Session 33: Track where user came from for back navigation
  const fromPage = searchParams.get('from');
  const [isProcessing, setIsProcessing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isJoiningWaitlist, setIsJoiningWaitlist] = useState(false);
  const [isPublishing, setIsPublishing] = useState(false);
  const [showCancelConfirm, setShowCancelConfirm] = useState(false);
  const [cancelError, setCancelError] = useState<string | null>(null);
  const [isCancelling, setIsCancelling] = useState(false);
  const [showEditModal, setShowEditModal] = useState(false);
  const [isUpdatingRegistration, setIsUpdatingRegistration] = useState(false);
  // Phase 6A.28: User choice for deleting signup commitments
  const [deleteSignUpCommitments, setDeleteSignUpCommitments] = useState(false);
  // Phase 6A.80: Success dialog for anonymous registration
  const [showSuccessDialog, setShowSuccessDialog] = useState(false);
  const [successEmail, setSuccessEmail] = useState<string>('');
  // GitHub Issue #31: Replace native confirm()/alert() with styled dialogs
  const [showWithdrawRefundDialog, setShowWithdrawRefundDialog] = useState(false);
  const [showCancelPendingDialog, setShowCancelPendingDialog] = useState(false);
  const [withdrawRefundError, setWithdrawRefundError] = useState<string | null>(null);
  const [cancelPendingError, setCancelPendingError] = useState<string | null>(null);
  const [paymentLinkError, setPaymentLinkError] = useState<string | null>(null);

  // Fetch event details
  const { data: event, isLoading, error: fetchError } = useEventById(id);

  // Phase 6A.56 FIX: Remove _hasHydrated dependency - causes registration status "flipping"
  // The auth store now correctly restores isAuthenticated during hydration
  // React Query hooks can execute immediately if user exists (token is already in API client)
  const { data: userRsvp, isLoading: isLoadingRsvp } = useUserRsvpForEvent(
    user?.userId ? id : undefined
  );

  // Fetch full registration details with attendee information
  // Fetch details whenever userRsvp exists (even if cancelled status)
  // Phase 6A.79 Part 3 Fix: Pass !!userRsvp directly to enable fetching when RSVP exists
  // This was causing a catch-22: isUserRegistered depends on registrationDetails,
  // but registrationDetails wouldn't fetch until isUserRegistered was true
  const { data: registrationDetails, isLoading: isLoadingRegistration } = useUserRegistrationDetails(
    user?.userId ? id : undefined,
    !!userRsvp  // ✅ Correct: Enable whenever userRsvp exists, not when isUserRegistered is true
  );

  // Fix: Check registration status - user is only "registered" if status is Confirmed AND payment is completed/not required
  // CRITICAL BUG FIX: Prevent showing "You're Registered" for pending payments
  // RACE CONDITION FIX: Wait for registrationDetails to load before checking status
  // Phase 6A.79 Part 3 Fix 2: API returns STRING values, not numeric enums - compare to strings
  const isUserRegistered = !!userRsvp &&
    !isLoadingRegistration &&
    registrationDetails?.status === 'Confirmed' &&  // Compare to string, not numeric enum
    (registrationDetails?.paymentStatus === 'Completed' ||
     registrationDetails?.paymentStatus === 'NotRequired');

  // Phase 6A.81: Payment pending state - registration created but payment not yet completed
  // Preliminary status = waiting for Stripe payment (checkout session active, expires in 24h)
  // RACE CONDITION FIX: Wait for registrationDetails to load before checking status
  // Phase 6A.79 Part 3 Fix 2: API returns STRING values, not numeric enums - compare to strings
  const isPaymentPending = !!userRsvp &&
    !isLoadingRegistration &&
    registrationDetails?.status === 'Preliminary';  // Phase 6A.81: New Preliminary status for unpaid registrations

  // Phase 6A.81: Abandoned state - checkout session expired (user didn't complete payment within 24h)
  // Allows user to retry registration with same email
  const isAbandoned = !!userRsvp &&
    !isLoadingRegistration &&
    registrationDetails?.status === 'Abandoned';

  // Phase 6A.91: RefundRequested state - user requested refund, awaiting Stripe confirmation
  // User can withdraw request to restore Confirmed status before event starts
  const isRefundRequested = !!userRsvp &&
    !isLoadingRegistration &&
    registrationDetails?.status === 'RefundRequested';

  // Phase 6A.91: Check if this was a paid registration (for button text)
  const isPaidRegistration = registrationDetails?.paymentStatus === 'Completed';

  // Phase 6A.79 Part 3: Enhanced logging to debug registration status display
  console.log('[EventDetail] 🔍 Registration state debug:', {
    eventId: id,
    userId: user?.userId,
    hasUserRsvp: !!userRsvp,
    userRsvpData: userRsvp,
    isLoadingRegistration,
    registrationDetailsData: registrationDetails,
    registrationStatus: registrationDetails?.status,
    registrationStatusName: registrationDetails?.status ?? 'undefined',  // Already a string from API
    paymentStatus: registrationDetails?.paymentStatus,
    paymentStatusName: registrationDetails?.paymentStatus ?? 'undefined',  // Already a string from API
    isUserRegistered,
    isPaymentPending,
    isAbandoned,  // Phase 6A.81: New state
    isRefundRequested,  // Phase 6A.91: New state
    isPaidRegistration,  // Phase 6A.91: For button text
    // Show what values are being compared (Phase 6A.79 Part 3: Compare to strings)
    statusCheck: {
      isConfirmed: registrationDetails?.status === 'Confirmed',
      isPreliminary: registrationDetails?.status === 'Preliminary',  // Phase 6A.81: New state
      isAbandoned: registrationDetails?.status === 'Abandoned',  // Phase 6A.81: New state
      isRefundRequested: registrationDetails?.status === 'RefundRequested',  // Phase 6A.91: New state
      isPending: registrationDetails?.status === 'Pending',  // Deprecated
      paymentCompleted: registrationDetails?.paymentStatus === 'Completed',
      paymentNotRequired: registrationDetails?.paymentStatus === 'NotRequired',
      paymentPending: registrationDetails?.paymentStatus === 'Pending',
    }
  });

  // RSVP mutation
  const rsvpMutation = useRsvpToEvent();

  // Phase 6A.14: Update registration mutation
  const updateRegistrationMutation = useUpdateRegistrationDetails();

  // Phase 6A.74 Part 12 Issue #4 Fix: Handle hash navigation for anchor links
  // Newsletter emails contain links like /events/{id}#sign-ups that should scroll to the section
  useEffect(() => {
    // Only run after component has mounted, data is loaded, AND auth is hydrated
    if (!event || isLoading || !_hasHydrated) return;

    // Check if URL contains a hash
    const hash = window.location.hash;
    if (!hash) return;

    console.log('[EventDetail] Attempting to scroll to hash:', hash);

    // Longer delay to ensure DOM is fully rendered (including conditional sections)
    const timeoutId = setTimeout(() => {
      const elementId = hash.substring(1); // Remove # from hash
      const element = document.getElementById(elementId);

      if (element) {
        console.log('[EventDetail] Found element, scrolling to:', elementId);
        element.scrollIntoView({
          behavior: 'smooth',
          block: 'start'
        });
      } else {
        console.warn('[EventDetail] Element not found with id:', elementId);
      }
    }, 500); // Increased from 300ms to 500ms

    return () => clearTimeout(timeoutId);
  }, [event, isLoading, _hasHydrated]);

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

        // Phase 6A.25 Fix: Free event - no page reload needed
        // The useRsvpToEvent mutation's onSuccess handler invalidates all relevant caches:
        // - eventKeys.detail(eventId) - updates registration count
        // - ['user-rsvps'] - updates isUserRegistered status
        // - ['user-registration', eventId] - updates registration details
        // React Query will automatically refetch and update the UI
        setIsProcessing(false);
      } else {
        // Anonymous registration
        // Phase 6A.44: Build redirect URLs for anonymous payment flow
        const baseUrl = typeof window !== 'undefined' ? window.location.origin : '';
        const successUrl = `${baseUrl}/events/payment/success?eventId=${id}`;
        const cancelUrl = `${baseUrl}/events/payment/cancel?eventId=${id}`;

        // Phase 6A.44: Anonymous registration returns checkout URL for paid events
        const response = await eventsRepository.registerAnonymous(id, {
          ...data,
          successUrl,
          cancelUrl,
        });

        // If checkout URL is returned, redirect to Stripe for payment
        if (response.checkoutUrl) {
          // Paid event - redirect to Stripe Checkout
          window.location.href = response.checkoutUrl;
          return; // Don't set isProcessing false - user is being redirected
        }

        // Free event - show success message and reload
        // Phase 6A.80: Show success dialog before reload for better UX
        setSuccessEmail(data.email);
        setShowSuccessDialog(true);
        // Dialog close handler will trigger reload
      }

      setIsProcessing(false);
    } catch (err: any) {
      console.error('Registration failed:', err);

      // Check if it's an authentication error
      if (err?.response?.status === 401 || err?.message?.includes('Token refresh failed')) {
        setError('Your session has expired. Please log out and log back in to continue.');
        // Optionally redirect to login after a delay
        setTimeout(() => {
          router.push('/login?redirect=' + encodeURIComponent(`/events/${id}`));
        }, 3000);
      } else {
        setError(err instanceof Error ? err.message : 'Failed to register. Please try again.');
      }

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
      // Session 30: Removed alert popup for better UX
      // The UI will update automatically after page reload
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

  // Phase 6A.14: Handle Edit Registration
  const handleEditRegistration = async (data: EditRegistrationData) => {
    try {
      setIsUpdatingRegistration(true);
      await updateRegistrationMutation.mutateAsync({
        eventId: id,
        attendees: data.attendees,
        email: data.email,
        phoneNumber: data.phoneNumber,
        address: data.address,
      });
      setIsUpdatingRegistration(false);
      // Modal will close itself on success
    } catch (err) {
      setIsUpdatingRegistration(false);
      throw err; // Re-throw to let the modal handle the error display
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
              <Button onClick={() => router.push(fromPage === 'dashboard' ? '/dashboard' : '/events')}>
                {fromPage === 'dashboard' ? 'Back to Dashboard' : 'Back to Events'}
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
  const hasStarted = new Date(event.startDate) <= new Date();
  // GitHub Issue #37: Check if event is cancelled to hide registration section
  // Note: Backend may return status as string "Cancelled" or enum number 4
  const isCancelled = (event.status as unknown) === 'Cancelled' || event.status === EventStatus.Cancelled;

  return (
    <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
      <Header />

      {/* Back Button and Organizer Actions */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
        <div className="flex items-center justify-between gap-4">
          <Button
            variant="outline"
            onClick={() => router.push(fromPage === 'dashboard' ? '/dashboard' : '/events')}
            className="flex items-center gap-2"
          >
            <ArrowLeft className="h-4 w-4" />
            {fromPage === 'dashboard' ? 'Back to Dashboard' : 'Back to Events'}
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

              {/* Phase 6A.46: Display Label and Registration Badge */}
              <div className="flex flex-wrap items-center gap-3 mb-4">
                {/* Display Label (computed lifecycle label from backend) */}
                <Badge
                  variant="default"
                  className="text-white text-sm font-semibold"
                  style={{ backgroundColor: getStatusBadgeColor(event.displayLabel) }}
                >
                  {event.displayLabel}
                </Badge>

                {/* Registration Badge */}
                <RegistrationBadge isRegistered={isUserRegistered} compact={false} />
              </div>

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

              {/* Pricing - Session 23: Dual pricing, Session 33: Group pricing support */}
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
                  ) : event.hasGroupPricing && event.groupPricingTiers && event.groupPricingTiers.length > 0 ? (
                    // Session 33: Group tiered pricing display - show individual tiers
                    <div className="space-y-1">
                      <p className="text-sm font-medium text-neutral-600 mb-1">Group Tiered Pricing</p>
                      {event.groupPricingTiers.map((tier, index) => (
                        <p key={index} className="text-base font-semibold" style={{ color: '#8B1538' }}>
                          {tier.maxAttendees
                            ? (tier.minAttendees === tier.maxAttendees
                                ? `${tier.minAttendees} ${tier.minAttendees === 1 ? 'person' : 'persons'}`
                                : `${tier.minAttendees}-${tier.maxAttendees} persons`)
                            : `${tier.minAttendees}+ persons`}
                          : ${tier.pricePerPerson.toFixed(2)}
                        </p>
                      ))}
                    </div>
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
                  ) : event.ticketPriceAmount != null ? (
                    <>
                      <p className="text-base font-semibold" style={{ color: '#8B1538' }}>
                        ${event.ticketPriceAmount.toFixed(2)} per person
                      </p>
                      <p className="text-sm text-neutral-600">
                        {event.ticketPriceCurrency === 1 ? 'USD' : 'LKR'}
                      </p>
                    </>
                  ) : (
                    <p className="text-base font-semibold" style={{ color: '#8B1538' }}>
                      Paid Event
                    </p>
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
                  {/* GitHub Issue #37: Show "Event Cancelled" for cancelled events */}
                  {isCancelled
                    ? 'Event Cancelled'
                    : isUserRegistered
                    ? 'Your Registration'
                    : registrationDetails?.status === 'Cancelled'
                    ? 'Registration Cancelled'
                    : 'Register for this Event'}
                </CardTitle>
                <CardDescription>
                  {/* GitHub Issue #37: Show cancelled event message first */}
                  {isCancelled
                    ? 'This event has been cancelled. Registration is not available.'
                    : isUserRegistered
                    ? 'You are already registered for this event!'
                    : registrationDetails?.status === 'Cancelled'
                    ? hasStarted
                      ? 'Your registration was cancelled. This event has already started, so new registrations are not allowed.'
                      : 'Your registration for this event has been cancelled. You can register again if you wish.'
                    : hasStarted
                    ? 'This event has already started. Registration is no longer available.'
                    : isFull
                    ? 'This event is currently full. Join the waitlist to be notified when spots become available.'
                    : 'Reserve your spot now!'}
                </CardDescription>
              </CardHeader>
              <CardContent>
                {/* GitHub Issue #37: Show cancelled event info box FIRST */}
                {isCancelled ? (
                  <div className="p-4 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg">
                    <div className="flex items-center gap-2 mb-3">
                      <svg
                        className="h-5 w-5 text-red-600 dark:text-red-400"
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                      >
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={2}
                          d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
                        />
                      </svg>
                      <h3 className="text-lg font-semibold text-red-900 dark:text-red-100">
                        Event Cancelled
                      </h3>
                    </div>
                    <p className="text-sm text-red-800 dark:text-red-200">
                      This event has been cancelled by the organizer. Registration is no longer available.
                    </p>
                    <p className="text-sm text-red-700 dark:text-red-300 mt-2">
                      If you were registered for this event, you should have received a notification about the cancellation.
                    </p>
                  </div>
                ) : registrationDetails?.status === 'Cancelled' ? (
                  // Show cancelled status with option to re-register
                  <div className="space-y-6">
                    <div className="p-4 bg-gray-50 dark:bg-gray-900/20 border border-gray-200 dark:border-gray-800 rounded-lg">
                      <div className="flex items-center gap-2 mb-3">
                        <svg
                          className="h-5 w-5 text-gray-600 dark:text-gray-400"
                          fill="none"
                          stroke="currentColor"
                          viewBox="0 0 24 24"
                        >
                          <path
                            strokeLinecap="round"
                            strokeLinejoin="round"
                            strokeWidth={2}
                            d="M6 18L18 6M6 6l12 12"
                          />
                        </svg>
                        <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
                          Registration Cancelled
                        </h3>
                      </div>
                      <p className="text-sm text-gray-800 dark:text-gray-200 mb-3">
                        Your registration for this event was cancelled{registrationDetails.updatedAt ? ` on ${new Date(registrationDetails.updatedAt).toLocaleDateString()}` : ''}.
                      </p>
                      <p className="text-sm text-gray-700 dark:text-gray-300">
                        You can register again using the form below.
                      </p>
                    </div>

                    {/* Show registration form for re-registration */}
                    {hasStarted ? (
                      <div className="p-4 bg-gray-50 border border-gray-200 rounded-lg">
                        <p className="text-sm text-gray-800">
                          This event has already started. Registration is no longer available.
                        </p>
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
                        hasGroupPricing={event.hasGroupPricing}
                        groupPricingTiers={event.groupPricingTiers}
                        isProcessing={isProcessing}
                        onSubmit={handleRegistration}
                        error={error}
                      />
                    ) : (
                      <div className="p-4 bg-orange-50 border border-orange-200 rounded-lg">
                        <p className="text-sm text-orange-800">
                          This event is currently full. The waitlist feature is coming soon.
                        </p>
                      </div>
                    )}
                  </div>
                ) : isUserRegistered ? (
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
                        <h4 className="text-sm font-medium text-green-900 dark:text-green-100 mb-3">
                          Registration Details:
                        </h4>
                        {isLoadingRegistration ? (
                          <p className="text-sm text-green-800 dark:text-green-200">Loading registration details...</p>
                        ) : registrationDetails ? (
                          <div className="space-y-4">
                            {/* Contact Information Section */}
                            {(registrationDetails.contactEmail || registrationDetails.contactPhone || registrationDetails.contactAddress) && (
                              <div className="bg-green-100 dark:bg-green-900/30 rounded p-3">
                                <p className="text-xs font-semibold text-green-900 dark:text-green-200 mb-2">
                                  CONTACT INFORMATION
                                </p>
                                <div className="space-y-1 text-xs text-green-800 dark:text-green-300">
                                  {registrationDetails.contactEmail && (
                                    <p>
                                      <span className="font-medium">Email:</span> {registrationDetails.contactEmail}
                                    </p>
                                  )}
                                  {registrationDetails.contactPhone && (
                                    <p>
                                      <span className="font-medium">Phone:</span> {registrationDetails.contactPhone}
                                    </p>
                                  )}
                                  {registrationDetails.contactAddress && (
                                    <p>
                                      <span className="font-medium">Address:</span> {registrationDetails.contactAddress}
                                    </p>
                                  )}
                                </div>
                              </div>
                            )}

                            {/* Attendees Section - Show if we have attendees array with items */}
                            {registrationDetails.attendees && registrationDetails.attendees.length > 0 ? (
                              <div>
                                <p className="text-sm font-semibold text-green-900 dark:text-green-100 mb-2">
                                  Attendees ({registrationDetails.attendees.length}):
                                </p>
                                <div className="space-y-2">
                                  {registrationDetails.attendees.map((attendee, index) => (
                                    <div key={index} className="bg-green-100 dark:bg-green-900/30 rounded p-2.5 text-xs">
                                      <div className="flex justify-between items-start">
                                        <div>
                                          <p className="font-medium text-green-900 dark:text-green-100">
                                            {index + 1}. {attendee.name}
                                          </p>
                                          <p className="text-green-700 dark:text-green-300 mt-0.5">
                                            {attendee.ageCategory === AgeCategory.Adult || (attendee.ageCategory as unknown) === 'Adult' ? 'Adult' : 'Child'}
                                            {attendee.gender !== null && attendee.gender !== undefined && ` • ${attendee.gender === Gender.Male || (attendee.gender as unknown) === 'Male' ? 'Male' : attendee.gender === Gender.Female || (attendee.gender as unknown) === 'Female' ? 'Female' : 'Other'}`}
                                          </p>
                                        </div>
                                      </div>
                                    </div>
                                  ))}
                                </div>
                              </div>
                            ) : (
                              <div className="text-sm text-green-800 dark:text-green-200">
                                <p>Number of attendees: {registrationDetails.quantity || userRsvp?.currentRegistrations || 1}</p>
                              </div>
                            )}
                          </div>
                        ) : (
                          <div className="text-sm text-green-800 dark:text-green-200">
                            <p>Number of attendees: {userRsvp?.currentRegistrations || 1}</p>
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
                        onClick={() => setShowEditModal(true)}
                      >
                        Edit Registration
                      </Button>

                      {!showCancelConfirm ? (
                        <div className="flex-1 relative group">
                          <Button
                            variant="outline"
                            className="w-full"
                            style={{
                              borderColor: hasStarted ? '#9CA3AF' : '#EF4444',
                              color: hasStarted ? '#9CA3AF' : '#EF4444',
                              cursor: hasStarted ? 'not-allowed' : 'pointer',
                              opacity: hasStarted ? 0.6 : 1
                            }}
                            disabled={hasStarted}
                            onClick={() => {
                              console.log('[CancelRsvp] User clicked Cancel Registration button');
                              setShowCancelConfirm(true);
                            }}
                          >
                            {isPaidRegistration ? 'Cancel Registration and Refund' : 'Cancel Registration'}
                          </Button>
                          {/* Phase 6A.91: Tooltip explaining why button is disabled */}
                          {hasStarted && (
                            <div className="absolute bottom-full left-1/2 transform -translate-x-1/2 mb-2 px-3 py-2 bg-gray-900 text-white text-xs rounded-lg opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap z-10">
                              Cancellation is not available after the event has started
                            </div>
                          )}
                        </div>
                      ) : (
                        <div className="flex-1 space-y-3">
                          {/* Phase 6A.28: User choice for signup commitments */}
                          <div className="p-3 bg-yellow-50 border border-yellow-200 rounded-lg">
                            <label className="flex items-start gap-3 cursor-pointer">
                              <input
                                type="checkbox"
                                checked={deleteSignUpCommitments}
                                onChange={(e) => setDeleteSignUpCommitments(e.target.checked)}
                                className="mt-1 h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                              />
                              <div className="flex-1">
                                <p className="text-sm font-medium text-gray-900">
                                  Also delete my sign-up commitments
                                </p>
                                <p className="text-xs text-gray-600 mt-1">
                                  {deleteSignUpCommitments
                                    ? "Your sign-up items will be removed and available for others to claim."
                                    : "Your sign-up commitments will be kept even after cancellation (default)."}
                                </p>
                              </div>
                            </label>
                          </div>

                          {/* Action buttons */}
                          <div className="flex gap-2">
                            <Button
                              variant="outline"
                              className="flex-1"
                              onClick={() => {
                                console.log('[CancelRsvp] User cancelled the cancellation');
                                setShowCancelConfirm(false);
                                setCancelError(null);
                                setIsCancelling(false);
                                setDeleteSignUpCommitments(false);
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
                                  console.log('[CancelRsvp] User confirmed cancellation, DeleteSignUpCommitments:', deleteSignUpCommitments);
                                  setIsCancelling(true);
                                  setCancelError(null);
                                  await eventsRepository.cancelRsvp(id, deleteSignUpCommitments);
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
                        </div>
                      )}
                    </div>
                  </div>
                ) : isRefundRequested ? (
                  // Phase 6A.91: Refund requested state - show status and withdrawal option
                  <div className="space-y-4">
                    <div className="p-4 bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg">
                      <div className="flex items-center gap-2 mb-3">
                        <AlertCircle className="h-5 w-5 text-yellow-600 dark:text-yellow-400" />
                        <h3 className="text-lg font-semibold text-yellow-900 dark:text-yellow-100">
                          Refund Requested
                        </h3>
                      </div>
                      <p className="text-sm text-yellow-800 dark:text-yellow-200 mb-3">
                        Your refund request is being processed. The refund will be credited to your original payment method.
                      </p>
                      <p className="text-xs text-yellow-700 dark:text-yellow-300 mb-4">
                        Note: Refunds typically appear on your statement within 5-10 business days.
                      </p>

                      {/* Registration Details (if available) */}
                      {registrationDetails && (
                        <div className="mb-4 p-3 bg-yellow-100 dark:bg-yellow-900/30 rounded">
                          <p className="text-xs font-semibold text-yellow-900 dark:text-yellow-200 mb-2">
                            REFUND DETAILS
                          </p>
                          <div className="space-y-1 text-xs text-yellow-800 dark:text-yellow-300">
                            {registrationDetails.totalPriceAmount && (
                              <p>
                                <span className="font-medium">Refund Amount:</span> {registrationDetails.totalPriceCurrency} {registrationDetails.totalPriceAmount.toFixed(2)}
                              </p>
                            )}
                            {registrationDetails.contactEmail && (
                              <p>
                                <span className="font-medium">Email:</span> {registrationDetails.contactEmail}
                              </p>
                            )}
                          </div>
                        </div>
                      )}

                      {/* Withdraw Refund Request Button */}
                      <div className="flex justify-center relative group">
                        <Button
                          variant="outline"
                          className="w-full"
                          style={{
                            borderColor: hasStarted ? '#9CA3AF' : '#10B981',
                            color: hasStarted ? '#9CA3AF' : '#10B981',
                            cursor: hasStarted ? 'not-allowed' : 'pointer',
                            opacity: hasStarted ? 0.6 : 1
                          }}
                          disabled={hasStarted}
                          onClick={() => {
                            setWithdrawRefundError(null);
                            setShowWithdrawRefundDialog(true);
                          }}
                        >
                          Withdraw Refund Request
                        </Button>
                        {/* Phase 6A.91: Tooltip explaining why button is disabled */}
                        {hasStarted && (
                          <div className="absolute bottom-full left-1/2 transform -translate-x-1/2 mb-2 px-3 py-2 bg-gray-900 text-white text-xs rounded-lg opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap z-10">
                            Withdrawal is not available after the event has started
                          </div>
                        )}
                      </div>
                      {/* GitHub Issue #31: Styled error display instead of alert() */}
                      {withdrawRefundError && (
                        <div className="mt-3 p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded text-sm text-red-600 dark:text-red-400">
                          Failed to withdraw refund request: {withdrawRefundError}
                        </div>
                      )}
                    </div>
                  </div>
                ) : isPaymentPending ? (
                  // CRITICAL FIX: Show payment pending state for users who started registration but haven't completed payment
                  <div className="space-y-4">
                    <div className="p-4 bg-orange-50 dark:bg-orange-900/20 border border-orange-200 dark:border-orange-800 rounded-lg">
                      <div className="flex items-center gap-2 mb-3">
                        <AlertCircle className="h-5 w-5 text-orange-600 dark:text-orange-400" />
                        <h3 className="text-lg font-semibold text-orange-900 dark:text-orange-100">
                          Payment Pending
                        </h3>
                      </div>
                      <p className="text-sm text-orange-800 dark:text-orange-200 mb-3">
                        Your registration is pending payment. Please complete your payment to confirm your registration.
                      </p>

                      {/* Phase 6A.81 Part 3: Countdown Timer for Checkout Session Expiration */}
                      {registrationDetails?.checkoutSessionExpiresAt && (
                        <div className="mb-3 flex justify-center">
                          <CheckoutCountdownTimer
                            expiresAt={registrationDetails.checkoutSessionExpiresAt}
                            onExpired={() => {
                              console.log('[CheckoutCountdown] Session expired - refreshing registration status');
                              // Refresh will update status to Abandoned via backend logic
                              window.location.reload();
                            }}
                          />
                        </div>
                      )}

                      {/* Payment Instructions */}
                      <div className="mt-3 pt-3 border-t border-orange-200 dark:border-orange-700">
                        <p className="text-sm text-orange-800 dark:text-orange-200 mb-3">
                          Click the button below to complete your payment and secure your spot at this event.
                        </p>

                        {/* Registration Details (if available) */}
                        {registrationDetails && (
                          <div className="mb-3 p-3 bg-orange-100 dark:bg-orange-900/30 rounded">
                            <p className="text-xs font-semibold text-orange-900 dark:text-orange-200 mb-2">
                              REGISTRATION DETAILS
                            </p>
                            <div className="space-y-1 text-xs text-orange-800 dark:text-orange-300">
                              {registrationDetails.contactEmail && (
                                <p>
                                  <span className="font-medium">Email:</span> {registrationDetails.contactEmail}
                                </p>
                              )}
                              {registrationDetails.quantity && (
                                <p>
                                  <span className="font-medium">Attendees:</span> {registrationDetails.quantity}
                                </p>
                              )}
                              {registrationDetails.totalPriceAmount && (
                                <p>
                                  <span className="font-medium">Amount:</span> {registrationDetails.totalPriceCurrency} {registrationDetails.totalPriceAmount.toFixed(2)}
                                </p>
                              )}
                            </div>
                          </div>
                        )}

                        <Button
                          className="w-full"
                          style={{
                            backgroundColor: '#FF7900',
                            color: '#FFFFFF'
                          }}
                          onClick={() => {
                            // Phase 6A.81 Part 3: Use stripeCheckoutUrl from registrationDetails
                            const checkoutUrl = registrationDetails?.stripeCheckoutUrl;

                            if (checkoutUrl) {
                              console.log('[PaymentPending] Redirecting to Stripe checkout:', checkoutUrl);
                              window.location.href = checkoutUrl;
                            } else {
                              console.error('[PaymentPending] No checkout URL available in registration details');
                              setPaymentLinkError('Payment link not available. Please refresh the page or contact support.');
                            }
                          }}
                          disabled={!registrationDetails?.stripeCheckoutUrl}
                        >
                          Complete Payment
                        </Button>

                        {registrationDetails?.checkoutSessionExpiresAt && (
                          <p className="text-xs text-orange-600 dark:text-orange-400 mt-3 text-center">
                            Complete payment before countdown expires to secure your spot.
                          </p>
                        )}

                        {/* GitHub Issue #31: Styled error display instead of alert() */}
                        {paymentLinkError && (
                          <div className="mt-3 p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded text-sm text-red-600 dark:text-red-400">
                            {paymentLinkError}
                          </div>
                        )}
                      </div>
                    </div>

                    {/* Cancel Registration Option */}
                    <div className="flex justify-center relative group">
                      <Button
                        variant="outline"
                        size="sm"
                        style={{
                          borderColor: hasStarted ? '#9CA3AF' : '#EF4444',
                          color: hasStarted ? '#9CA3AF' : '#EF4444',
                          cursor: hasStarted ? 'not-allowed' : 'pointer',
                          opacity: hasStarted ? 0.6 : 1
                        }}
                        disabled={hasStarted}
                        onClick={() => {
                          setCancelPendingError(null);
                          setShowCancelPendingDialog(true);
                        }}
                      >
                        Cancel Registration
                      </Button>
                      {/* Phase 6A.91: Tooltip explaining why button is disabled */}
                      {hasStarted && (
                        <div className="absolute bottom-full left-1/2 transform -translate-x-1/2 mb-2 px-3 py-2 bg-gray-900 text-white text-xs rounded-lg opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap z-10">
                          Cancellation is not available after the event has started
                        </div>
                      )}
                    </div>
                    {/* GitHub Issue #31: Styled error display instead of alert() */}
                    {cancelPendingError && (
                      <div className="mt-3 p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded text-sm text-red-600 dark:text-red-400">
                        Failed to cancel registration: {cancelPendingError}
                      </div>
                    )}
                  </div>
                ) : isAbandoned ? (
                  // Phase 6A.81: Abandoned state - checkout session expired, user can retry
                  <div className="space-y-4">
                    <div className="p-4 bg-gray-50 dark:bg-gray-900/20 border border-gray-200 dark:border-gray-700 rounded-lg">
                      <div className="flex items-center gap-2 mb-3">
                        <AlertCircle className="h-5 w-5 text-gray-600 dark:text-gray-400" />
                        <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
                          Checkout Session Expired
                        </h3>
                      </div>
                      <p className="text-sm text-gray-800 dark:text-gray-200 mb-3">
                        Your previous checkout session expired after 24 hours without payment completion.
                      </p>
                      <p className="text-sm text-gray-800 dark:text-gray-200 mb-3">
                        You can register again to get a new checkout link and complete your registration.
                      </p>

                      <Button
                        className="w-full mt-3"
                        onClick={() => {
                          // Clear the abandoned registration and allow re-registration
                          window.location.reload();
                        }}
                      >
                        Register Again
                      </Button>
                    </div>
                  </div>
                ) : hasStarted ? (
                  <div className="p-4 bg-gray-50 border border-gray-200 rounded-lg">
                    <div className="flex items-center gap-2 mb-2">
                      <AlertCircle className="h-5 w-5 text-gray-600" />
                      <h3 className="font-semibold text-gray-900">Event Has Started</h3>
                    </div>
                    <p className="text-sm text-gray-800">
                      This event has already started. Registration is no longer available.
                    </p>
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
                    hasGroupPricing={event.hasGroupPricing}
                    groupPricingTiers={event.groupPricingTiers}
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

        {/* Phase 6A.24: Ticket Section for Paid Events */}
        {/* Shows QR code, download PDF, and resend email buttons for registered paid events */}
        {/* Wait for auth hydration before rendering to ensure token is available for API calls */}
        {_hasHydrated && isUserRegistered && event && !event.isFree && (
          <div className="mt-8">
            <TicketSection eventId={id} isPaidEvent={!event.isFree} />
          </div>
        )}

        {/* Phase 6A.X: Event Organizer Contact - Compact Version */}
        {event && event.publishOrganizerContact && event.organizerContactName && (
          <div className="mt-6">
            <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
              <div className="flex items-center gap-2 mb-3">
                <Users className="h-4 w-4 text-blue-600" />
                <h3 className="text-sm font-semibold text-blue-900">Event Organizer Contact</h3>
              </div>
              <div className="space-y-2 text-sm">
                <div className="flex items-center gap-2">
                  <span className="font-medium text-gray-700">Name:</span>
                  <span className="text-gray-900">{event.organizerContactName}</span>
                </div>
                {event.organizerContactEmail && (
                  <div className="flex items-center gap-2">
                    <span className="font-medium text-gray-700">Email:</span>
                    <a
                      href={`mailto:${event.organizerContactEmail}`}
                      className="text-blue-600 hover:underline"
                    >
                      {event.organizerContactEmail}
                    </a>
                  </div>
                )}
                {event.organizerContactPhone && (
                  <div className="flex items-center gap-2">
                    <span className="font-medium text-gray-700">Phone:</span>
                    <a
                      href={`tel:${event.organizerContactPhone}`}
                      className="text-blue-600 hover:underline"
                    >
                      {event.organizerContactPhone}
                    </a>
                  </div>
                )}
              </div>
            </div>
          </div>
        )}

        {/* Sign-Up Management Section */}
        {/* Phase 6A.76: Removed _hasHydrated condition to allow non-members to view sign-up lists */}
        {/* The component handles userId being undefined for anonymous users */}
        {/* Issue #4 Fix: Add id="sign-ups" anchor for newsletter link navigation */}
        <div id="sign-ups" className="mt-8">
          <SignUpManagementSection
            eventId={id}
            userId={user?.userId}
            isOrganizer={false}
          />
        </div>
      </div>

      <Footer />

      {/* Phase 6A.14: Edit Registration Modal */}
      <EditRegistrationModal
        open={showEditModal}
        onOpenChange={setShowEditModal}
        registration={registrationDetails || null}
        eventId={id}
        isFreeEvent={event?.isFree ?? true}
        spotsLeft={spotsLeft}
        onSave={handleEditRegistration}
        isSubmitting={isUpdatingRegistration}
      />

      {/* Phase 6A.80: Success Dialog for Anonymous Registration */}
      <Dialog open={showSuccessDialog} onOpenChange={setShowSuccessDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Registration Successful!</DialogTitle>
            <DialogDescription>
              Your registration for {event?.title} has been confirmed.
            </DialogDescription>
          </DialogHeader>
          <div className="py-4">
            <div className="flex items-start gap-3 p-4 bg-green-50 border border-green-200 rounded-lg">
              <svg
                className="h-5 w-5 text-green-600 flex-shrink-0 mt-0.5"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
              <div className="flex-1">
                <p className="text-sm font-medium text-green-900 mb-2">
                  Check your email
                </p>
                <p className="text-sm text-green-800">
                  A confirmation email will be sent to <strong>{successEmail}</strong> within 2-6 minutes.
                </p>
                <p className="text-xs text-green-700 mt-2">
                  Please check your inbox and spam folder if you don't see it right away.
                </p>
              </div>
            </div>
          </div>
          <DialogFooter>
            <Button
              onClick={() => {
                setShowSuccessDialog(false);
                window.location.reload();
              }}
              style={{
                backgroundColor: '#FF7900',
                color: '#FFFFFF'
              }}
            >
              Got it!
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* GitHub Issue #31: Styled confirmation dialogs to replace browser confirm() */}
      <ConfirmDialog
        open={showWithdrawRefundDialog}
        onOpenChange={setShowWithdrawRefundDialog}
        title="Withdraw Refund Request"
        description="Are you sure you want to withdraw your refund request? Your registration will be restored and you will keep your spot at the event."
        confirmLabel="Yes, Withdraw Request"
        cancelLabel="No, Keep Refund Request"
        variant="info"
        onConfirm={async () => {
          try {
            console.log('[WithdrawRefund] Withdrawing refund request');
            await eventsRepository.withdrawRefundRequest(id);
            console.log('[WithdrawRefund] Successfully withdrew refund request - reloading page');
            window.location.reload();
          } catch (error: any) {
            console.error('[WithdrawRefund] Failed to withdraw refund request:', error);
            const errorMessage = error?.response?.data?.detail || error?.response?.data?.message || error?.message || 'Unknown error';
            setWithdrawRefundError(errorMessage);
            setShowWithdrawRefundDialog(false);
          }
        }}
      />

      <ConfirmDialog
        open={showCancelPendingDialog}
        onOpenChange={setShowCancelPendingDialog}
        title="Cancel Registration"
        description="Are you sure you want to cancel this registration? You will need to register again if you change your mind."
        confirmLabel="Yes, Cancel Registration"
        cancelLabel="No, Keep Registration"
        variant="danger"
        onConfirm={async () => {
          try {
            console.log('[PaymentPending] Cancelling pending registration');
            await eventsRepository.cancelRsvp(id, false);
            console.log('[PaymentPending] Successfully cancelled - reloading page');
            window.location.reload();
          } catch (error: any) {
            console.error('[PaymentPending] Failed to cancel registration:', error);
            const errorMessage = error?.response?.data?.detail || error?.response?.data?.message || error?.message || 'Unknown error';
            setCancelPendingError(errorMessage);
            setShowCancelPendingDialog(false);
          }
        }}
      />
    </div>
  );
}
