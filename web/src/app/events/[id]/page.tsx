'use client';

import React, { use } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { ArrowLeft, Calendar, MapPin, Users, DollarSign, Clock, AlertCircle, List, ClipboardList, CheckCircle, Trash2, Heart, Camera, Download, Loader2, Wallet, Award, ShoppingBag, HandHeart, ChevronDown, ChevronUp } from 'lucide-react';
import { LankaEventsHeader } from '@/presentation/components/layout/LankaEventsHeader';
import Footer from '@/presentation/components/layout/Footer';
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Badge } from '@/presentation/components/ui/Badge';
import { useEventById, useRsvpToEvent, useUserRsvpForEvent, useUserRegistrationDetails, useUpdateRegistrationDetails } from '@/presentation/hooks/useEvents';
import { useEventForms, useDeleteFormResponse, useUserFormResponses } from '@/presentation/hooks/useEventForms';
import { SignUpManagementSection, volunteerSectionLabels } from '@/presentation/components/features/events/SignUpManagementSection';
// Phase 6A.146: public form responses section
import { PublicFormResponsesSection } from '@/presentation/components/features/events/PublicFormResponsesSection';
import { RsvpFormSection } from '@/presentation/components/features/events/RsvpFormSection';
import { ExternalRegistrationCta } from '@/presentation/components/features/events/ExternalRegistrationCta';
import { MediaGallery } from '@/presentation/components/features/events/MediaGallery';
import { RefundRequestStatusBanner } from '@/presentation/components/features/events/RefundRequestStatusBanner';
import { RequestRefundDialog } from '@/presentation/components/features/events/RequestRefundDialog';
// Phase 6A.145 Commit 5 — top-of-page preview strips for add-ons + sponsors.
import { AddOnsPreviewStrip } from '@/presentation/components/features/events/AddOnsPreviewStrip';
import { SponsorsPreviewStrip } from '@/presentation/components/features/events/SponsorsPreviewStrip';
import { EditRegistrationModal, type EditRegistrationData } from '@/presentation/components/features/events/EditRegistrationModal';
import { AddAttendeesModal } from '@/presentation/components/features/events/AddAttendeesModal';
import { AddHeadCountModal } from '@/presentation/components/features/events/AddHeadCountModal';
import { RegistrationBreakdownCard } from '@/presentation/components/features/events/RegistrationBreakdownCard';
import { TicketSection } from '@/presentation/components/features/events/TicketSection';
import { RegistrationBadge } from '@/presentation/components/features/events/RegistrationBadge';
import { CheckoutCountdownTimer } from '@/presentation/components/features/events/CheckoutCountdownTimer';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from '@/presentation/components/ui/Dialog';
import { ConfirmDialog } from '@/presentation/components/ui/ConfirmDialog';
import { useAuthStore } from '@/presentation/store/useAuthStore';
// Phase 6A.144: Auth-encouragement gate for paid events
import { AuthEncouragementModal } from '@/presentation/components/features/auth/AuthEncouragementModal';
import { AuthEncouragementPrompt } from '@/presentation/components/features/auth/AuthEncouragementPrompt';
import { shouldShowAuthNudge, guestAckStorageKey } from '@/presentation/components/features/auth/authNudgePolicy';
import { EventCategory, EventStatus, RegistrationStatus, PaymentStatus, AgeCategory, Gender, EventFormStatus, SignUpKind, RegistrationMode, EventPaymentMode, type AnonymousRegistrationRequest, type RsvpRequest } from '@/infrastructure/api/types/events.types';
import { paymentsRepository } from '@/infrastructure/api/repositories/payments.repository';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import { useState, useEffect } from 'react';
// Phase 6A.97: Import timezone-aware date formatter
import { formatEventDate, formatEventTime, getTimezoneAbbreviation } from '@/presentation/lib/utils/date-formatter';
import { sanitizeHtml } from '@/lib/html-utils';
// Donation Feature: Import DonationSection for standalone donations
import { DonationSection } from '@/presentation/components/features/events/DonationSection';
// Financial Features: Collections, Sponsors, Add-Ons
import { CollectionSection } from '@/presentation/components/features/events/CollectionSection';
import { SponsorSection } from '@/presentation/components/features/events/SponsorSection';
import { AddOnSelector } from '@/presentation/components/features/events/AddOnSelector';
// Collapsible sections for Registration, Ticket, and Organizer
import { CollapsibleSection } from '@/presentation/components/ui/CollapsibleSection';
// Donation Feature: Import donation hooks
import { usePublicDonationSummary, useMyDonations } from '@/presentation/hooks/useDonations';
// Collection Feature: Import collection hooks
import { usePublicCollectionSummary, useMyCollections } from '@/presentation/hooks/useCollections';
// Sponsor Feature: Import sponsor hooks
import { useMySponsors } from '@/presentation/hooks/useSponsors';
// Add-On Feature: Import add-on hooks
import { useMyAddOnPurchasesMine } from '@/presentation/hooks/useAddOns';
// Multi-Album: Import album hooks and carousel
import { useEventAlbums, useDownloadAlbumZip } from '@/presentation/hooks/usePhotoAlbum';
import { AlbumPhotoCarousel } from '@/presentation/components/features/events/AlbumPhotoCarousel';
import { AlbumStatus } from '@/infrastructure/api/types/events.types';
// Phase 7A.4: WhatsApp share button
import { WhatsAppShareButton } from '@/presentation/components/features/whatsapp/WhatsAppShareButton';
// Phase 8YB.1: Hero image component shared by [id]/page.tsx (contained) and [id]/v2/page.tsx (fullWidth)
import { EventHeroImage, type EventHeroVariant } from '@/presentation/components/features/events/EventHeroImage';
// Phase 8YB.3: Mode-C "No registration required" hint (banner + quick-nav pill)
import { RegistrationStatusHint } from '@/presentation/components/features/events/RegistrationStatusHint';
// Phase 8YB.4: Quick-nav pill row (extracted) + signup-lists/forms presence probe
import { EventQuickNav, type EventQuickNavPill } from '@/presentation/components/features/events/EventQuickNav';
import { useHasSignUps } from '@/presentation/hooks/useHasSignUps';

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
 * Event Detail Page (default export)
 *
 * Phase 8YB.1 → 8YB.2: After A/B comparison on staging the user picked the full-bleed
 * hero (Option E), so the default route now renders `heroVariant="fullWidth"`. The
 * legacy constrained-column variant (Option C) lives at `/events/{id}/v2` for any
 * future tweaks the user wants to iterate on without touching the primary surface.
 */
export default function EventDetailPage({ params }: { params: Promise<{ id: string }> }) {
  return <EventDetailPageInternal params={params} heroVariant="fullWidth" />;
}

/**
 * Event Detail Page — internal implementation.
 * Displays full event details with RSVP, Stripe payment, waitlist, and sign-up management.
 *
 * @param heroVariant
 *   - "fullWidth" → hero rendered above the constrained column, spanning the full
 *                   viewport (Option E). Default — used by `/events/{id}`.
 *   - "contained" → hero rendered inside the existing max-w-7xl Card column (Option C).
 *                   Used by `/events/{id}/v2` (kept as a sandbox for layout iteration).
 */
export function EventDetailPageInternal({
  params,
  heroVariant = 'fullWidth',
}: {
  params: Promise<{ id: string }>;
  heroVariant?: EventHeroVariant;
}) {
  const { id } = use(params);
  const router = useRouter();
  const searchParams = useSearchParams();
  const { user, _hasHydrated, isAuthenticated } = useAuthStore();

  // Session 33: Track where user came from for back navigation
  const fromPage = searchParams.get('from');
  const donationStatus = searchParams.get('donation'); // 'success' | 'cancelled' | null
  const collectionStatus = searchParams.get('collection'); // 'success' | 'cancelled' | null
  const sponsorStatus = searchParams.get('sponsor'); // 'success' | 'cancelled' | null
  const addOnStatus = searchParams.get('addon'); // 'success' | 'cancelled' | null
  const [isProcessing, setIsProcessing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isJoiningWaitlist, setIsJoiningWaitlist] = useState(false);
  const [isPublishing, setIsPublishing] = useState(false);
  const [showCancelConfirm, setShowCancelConfirm] = useState(false);
  const [cancelError, setCancelError] = useState<string | null>(null);
  const [isCancelling, setIsCancelling] = useState(false);
  const [showEditModal, setShowEditModal] = useState(false);
  const [isUpdatingRegistration, setIsUpdatingRegistration] = useState(false);
  // Add-Only Attendees: State for showing AddAttendeesModal
  const [showAddAttendeesModal, setShowAddAttendeesModal] = useState(false);
  // Phase 6A.28: User choice for deleting signup commitments
  const [deleteSignUpCommitments, setDeleteSignUpCommitments] = useState(false);
  // Cancellation enhancement: User choice for deleting form responses
  const [deleteFormResponses, setDeleteFormResponses] = useState(false);
  // Cancellation enhancement: User choice for refunding add-on purchases
  const [refundAddOnPurchases, setRefundAddOnPurchases] = useState(false);
  // Phase 6A.137F: User choice for refunding collections and sponsors
  const [refundCollections, setRefundCollections] = useState(false);
  const [refundSponsors, setRefundSponsors] = useState(false);
  // Phase 6A.80: Success dialog for anonymous registration
  const [showSuccessDialog, setShowSuccessDialog] = useState(false);
  const [successEmail, setSuccessEmail] = useState<string>('');
  // Phase 6A.144: Auth-encouragement gate state. The nudge fires for anonymous
  // users on PAID events only. `guestModeAcknowledged` is hydrated from
  // sessionStorage so a refresh doesn't re-prompt the user mid-flow, but a new
  // session re-asks (per architect — we don't want to train dismissal).
  const [showAuthNudge, setShowAuthNudge] = useState(false);
  const [guestModeAcknowledged, setGuestModeAcknowledged] = useState(false);
  // Phase 6A.146 (2026-05-15 UAT correction): inline Show/Hide responses per
  // form card. Set of formIds whose public-response panel is currently expanded.
  const [expandedResponseFormIds, setExpandedResponseFormIds] = useState<Set<string>>(new Set());
  const toggleResponsesExpanded = (formId: string) => {
    setExpandedResponseFormIds((prev) => {
      const next = new Set(prev);
      if (next.has(formId)) next.delete(formId);
      else next.add(formId);
      return next;
    });
  };
  // GitHub Issue #31: Replace native confirm()/alert() with styled dialogs
  const [showWithdrawRefundDialog, setShowWithdrawRefundDialog] = useState(false);
  const [showCancelPendingDialog, setShowCancelPendingDialog] = useState(false);
  const [withdrawRefundError, setWithdrawRefundError] = useState<string | null>(null);
  const [cancelPendingError, setCancelPendingError] = useState<string | null>(null);
  const [paymentLinkError, setPaymentLinkError] = useState<string | null>(null);
  // Phase 6A.91 Fix: Track when user wants to re-register after abandoned checkout
  // Phase 6A.137F: retryAfterAbandoned removed — abandoned/incomplete states now show form directly
  // Phase 6A.93 Fix: Track when user wants to re-register while refund is in progress
  const [retryAfterRefund, setRetryAfterRefund] = useState(false);
  // Phase 6A.109: Track form response deletion
  const [deletingFormId, setDeletingFormId] = useState<string | null>(null);
  const [showFormDeleteConfirm, setShowFormDeleteConfirm] = useState(false);

  // Phase 6A.148 — Refund approval workflow state
  const [myRefundRequest, setMyRefundRequest] =
    useState<import('@/infrastructure/api/types/refund-request.types').AttendeeRefundRequestDto | null>(null);
  const [showRequestRefundDialog, setShowRequestRefundDialog] = useState(false);
  const [isWithdrawingV2, setIsWithdrawingV2] = useState(false);

  // Phase 6A.113: Tab navigation removed — signup lists and forms are now separate
  // CollapsibleSections with id anchors. Hash-based scrolling handled by the
  // existing useEffect at line ~318 (scrolls to any element by id from URL hash).

  // Fetch event details
  const { data: event, isLoading, error: fetchError } = useEventById(id);

  // Phase 6A.133: Use backend-computed organizer flag instead of client-side ID comparison
  const isOrganizer = event?.isCurrentUserOrganizer === true;

  // Donation Feature: Public summary (when organizer enabled ShowDonationSummary)
  const { data: publicDonationSummary } = usePublicDonationSummary(
    event?.donationConfig?.isEnabled && event?.donationConfig?.showDonationSummary ? id : undefined
  );

  // Donation Feature: My donations (logged-in user's own donations for this event)
  const { data: myDonations } = useMyDonations(
    isAuthenticated && event?.donationConfig?.isEnabled ? id : undefined
  );

  // Collection Feature: Public summary (goal progress, contributor count)
  const { data: publicCollectionSummary } = usePublicCollectionSummary(
    event?.collectionConfig?.isEnabled ? id : undefined
  );

  // Collection Feature: My collections (logged-in user's own contributions)
  const { data: myCollections } = useMyCollections(
    isAuthenticated && event?.collectionConfig?.isEnabled ? id : undefined
  );

  // Sponsor Feature: My sponsors (logged-in user's own sponsorships for this event)
  const { data: mySponsors } = useMySponsors(
    isAuthenticated && event?.sponsorConfig?.isEnabled ? id : undefined
  );

  // Add-On Feature: My add-on purchases (logged-in user's own purchases for this event)
  const { data: myAddOnPurchases } = useMyAddOnPurchasesMine(
    isAuthenticated && event?.addOnConfig?.isEnabled ? id : undefined
  );

  // Multi-Album: Fetch published albums for event details carousel
  const { data: eventAlbums } = useEventAlbums(id);
  const publishedAlbumsWithPhotos = (eventAlbums ?? []).filter(
    (a) => a.status === AlbumStatus.Published && a.photoCount > 0,
  );
  const [activeCarouselAlbumId, setActiveCarouselAlbumId] = useState<string | null>(null);
  const activeCarouselAlbum =
    publishedAlbumsWithPhotos.find((a) => a.id === activeCarouselAlbumId) ??
    publishedAlbumsWithPhotos[0] ?? null;
  const downloadZip = useDownloadAlbumZip();

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

  // Phase 6A.X: Handle legacy/inconsistent data where status is Confirmed but payment is Pending
  // This can happen with old registrations created before Preliminary/Abandoned flow was implemented
  // or if the backend failed to update status when checkout expired
  // Check if checkout session has expired
  const checkoutExpired = registrationDetails?.checkoutSessionExpiresAt
    ? new Date(registrationDetails.checkoutSessionExpiresAt) < new Date()
    : true; // Assume expired if no expiry date

  const isPaymentIncomplete = !!userRsvp &&
    !isLoadingRegistration &&
    registrationDetails?.status === 'Confirmed' &&
    registrationDetails?.paymentStatus === 'Pending';

  // Phase 6A.91: Check if this was a paid registration (for button text)
  const isPaidRegistration = registrationDetails?.paymentStatus === 'Completed';

  // Phase 6A.148: Load this user's most recent refund request for the event. The
  // backend endpoint is feature-flag-gated and returns null when the flag is OFF,
  // so this is safe to call always — no behavior change when flag is OFF.
  React.useEffect(() => {
    if (!isPaidRegistration || !id) {
      setMyRefundRequest(null);
      return;
    }
    let cancelled = false;
    void (async () => {
      try {
        const data = await eventsRepository.getMyRefundRequest(id);
        if (!cancelled) setMyRefundRequest(data);
      } catch (err) {
        // 404 means flag is off — silently ignore. Other errors logged.
        if (!cancelled) {
          const msg = err instanceof Error ? err.message : String(err);
          if (!/not found|404/i.test(msg)) {
            console.warn('[6A.148] getMyRefundRequest failed:', err);
          }
          setMyRefundRequest(null);
        }
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [id, isPaidRegistration, registrationDetails?.status]);

  const hasActiveRefundRequest =
    myRefundRequest !== null &&
    (myRefundRequest.status === 'Pending' ||
      myRefundRequest.status === 'Approved' ||
      myRefundRequest.status === 'Processing');

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
    isPaymentIncomplete,  // Phase 6A.X: Legacy data with Confirmed + Pending
    isPaidRegistration,  // Phase 6A.91: For button text
    checkoutExpired,  // Phase 6A.X: Whether checkout session has expired
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

  // Phase 7.3: Fetch custom forms for this event
  const { data: eventForms, isLoading: isLoadingForms } = useEventForms(id);

  // Phase 7D.1 Phase G + Phase 8YB.4: page-scope sign-up probes used to gate the
  // quick-nav pills + their sibling sections. Each probe uses a kind-specific
  // query key (see `signUpKeys.list` in useEventSignUps) so they cannot collide
  // with each other or with the kind-Items query mounted inside
  // SignUpManagementSection. The probe-level isFetched gate prevents the worse
  // "pill flashes in then disappears" failure mode on slower networks.
  const { hasSignUps: hasVolunteerLists } = useHasSignUps(id, SignUpKind.Volunteers);
  const { hasSignUps: hasItemSignUpLists } = useHasSignUps(id, SignUpKind.Items);

  // Filter to show only Active forms to attendees
  // Note: Backend sends enum as string ('Active'), frontend enum is numeric (1)
  // Check both to handle serialization difference (same pattern as RegistrationBadge.tsx)
  const activeForms = eventForms?.filter(form =>
    form.status === ('Active' as any) || form.status === EventFormStatus.Active
  ) || [];

  // Phase 7E (UX): Mode-aware labels for the registration nav button + section heading.
  // - Mode A (DetailedAttendees) → "Register" (per-attendee form)
  // - Mode B (HeadCount*)        → "RSVP"     (lightweight head-count form)
  // - Mode C (NoRegistration)    → button hidden + section heading "About this event"
  // Defensive read tolerates stale React Query cached payloads from before 7E shipped.
  const registrationMode = event?.registrationMode ?? RegistrationMode.DetailedAttendees;
  const isModeC = registrationMode === RegistrationMode.NoRegistration;
  const isModeB =
    registrationMode === RegistrationMode.HeadCountOnly ||
    registrationMode === RegistrationMode.HeadCountByAge ||
    registrationMode === RegistrationMode.HeadCountByGender ||
    registrationMode === RegistrationMode.HeadCountByAgeAndGender;
  // Phase 8X.11 — ExternalPaid uses the standard "Register" CTA label (per product
  // owner: "We still display the registration button on top of the event details page.
  // When we click it, user will navigate the registration section and external payment
  // details will be shown there"). The vendor-aware "Buy on {Vendor}" copy lives inside
  // the ExternalRegistrationCta card in the section, where it's contextually relevant.
  const isExternalPaid = event?.paymentMode === EventPaymentMode.ExternalPaid;
  const registrationCtaLabel = isModeB ? 'RSVP' : 'Register';
  const registrationSectionTitle = isExternalPaid
    ? 'Register for this Event'
    : isModeC
      ? 'About this event'
      : isModeB
        ? 'RSVP for this Event'
        : 'Register for this Event';

  // Phase 6A.128: Use React Query's useQueries for user form responses (single source of truth)
  // Replaces manual useEffect + useState — cache invalidation from mutations propagates automatically
  const formIds = activeForms.map(f => f.id);
  const { userFormResponses, isFetchingResponses } = useUserFormResponses(
    id,
    formIds,
    isAuthenticated,
  );

  // RSVP mutation
  const rsvpMutation = useRsvpToEvent();

  // Phase 6A.14: Update registration mutation
  const updateRegistrationMutation = useUpdateRegistrationDetails();

  // Phase 6A.109: Delete form response mutation
  const deleteFormResponseMutation = useDeleteFormResponse();

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

  // Phase 6A.144: Hydrate the per-event guest acknowledgement flag from
  // sessionStorage. Wrapped in try/catch because private/Safari modes can throw.
  useEffect(() => {
    if (!event?.id) return;
    try {
      if (typeof window === 'undefined') return;
      const ack = window.sessionStorage.getItem(guestAckStorageKey(event.id));
      if (ack === '1') {
        setGuestModeAcknowledged(true);
      }
    } catch (err) {
      console.warn('[EventDetail 6A.144] sessionStorage read failed for guest-ack', err);
    }
  }, [event?.id]);

  // Phase 6A.144: When the user returns from sign-in/sign-up via the auth
  // encouragement modal, the deep link carries `?intent=register`. Once the
  // event has loaded and the user is authenticated, scroll to the RSVP section
  // and strip the param so back-button / re-render doesn't re-fire the scroll.
  useEffect(() => {
    if (!event?.id || !_hasHydrated) return;
    if (!isAuthenticated) return;
    const intent = searchParams.get('intent');
    if (intent !== 'register') return;
    const id = window.requestAnimationFrame(() => {
      try {
        document.getElementById('rsvp-section')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
        // Strip ?intent=register from the URL — mirrors the existing
        // history.replaceState patterns for ?registered=true in LoginForm.
        if (typeof window !== 'undefined' && window.history?.replaceState) {
          const url = new URL(window.location.href);
          url.searchParams.delete('intent');
          window.history.replaceState({}, '', url.pathname + (url.search ? url.search : '') + url.hash);
        }
      } catch (err) {
        console.warn('[EventDetail 6A.144] intent=register scroll/replaceState failed', err);
      }
    });
    return () => window.cancelAnimationFrame(id);
  }, [event?.id, _hasHydrated, isAuthenticated, searchParams]);

  // Category labels
  // Phase 6A.X: Support BOTH numeric and string category keys for API compatibility
  const categoryLabels: Record<string, string> = {
    // Numeric keys
    [EventCategory.Religious]: 'Religious',
    [EventCategory.Cultural]: 'Cultural',
    [EventCategory.Community]: 'Community',
    [EventCategory.Educational]: 'Educational',
    [EventCategory.Social]: 'Social',
    [EventCategory.Business]: 'Business',
    [EventCategory.Charity]: 'Charity',
    [EventCategory.Entertainment]: 'Entertainment',
    [EventCategory.Workshop]: 'Workshop',
    [EventCategory.Festival]: 'Festival',
    [EventCategory.Ceremony]: 'Ceremony',
    [EventCategory.Celebration]: 'Celebration',
    // String name keys (for JsonStringEnumConverter)
    'Religious': 'Religious',
    'Cultural': 'Cultural',
    'Community': 'Community',
    'Educational': 'Educational',
    'Social': 'Social',
    'Business': 'Business',
    'Charity': 'Charity',
    'Entertainment': 'Entertainment',
    'Workshop': 'Workshop',
    'Festival': 'Festival',
    'Ceremony': 'Ceremony',
    'Celebration': 'Celebration',
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
        // Donation Feature: Pass through donation fields for combined checkout
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
          // Donation Feature: Include donation fields for combined checkout
          donationAmount: (data as any).donationAmount ?? undefined,
          donorName: (data as any).donorName ?? undefined,
          donorPhone: (data as any).donorPhone ?? undefined,
          donorNotes: (data as any).donorNotes ?? undefined,
          // Phase 6A.137D: Include add-on selections for bundled checkout
          addOnSelections: (data as any).addOnSelections ?? undefined,
          // Phase 6A.137E: Include collection/sponsor for bundled checkout
          collectionAmount: (data as any).collectionAmount ?? undefined,
          collectionNotes: (data as any).collectionNotes ?? undefined,
          sponsorAmount: (data as any).sponsorAmount ?? undefined,
          sponsorOrganization: (data as any).sponsorOrganization ?? undefined,
          sponsorNotes: (data as any).sponsorNotes ?? undefined,
          // Phase 7E (bug fix): thread Mode-B head-count payload through. The
          // HeadCountRsvpForm sends these for B1-B4 events; without them the hook
          // silently dropped the fields and the backend returned "Lead attendee
          // name is required for HeadCountOnly events".
          leadAttendeeName: (data as any).leadAttendeeName ?? undefined,
          headCount: (data as any).headCount ?? undefined,
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
    if (!event || event.isCurrentUserOrganizer !== true) {
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

  // Phase 6A.106-110 Fix: Handle form response deletion for BOTH anonymous and logged-in users
  const handleDeleteFormResponse = async () => {
    if (!deletingFormId) return;

    setShowFormDeleteConfirm(false);

    try {
      // Check if user is logged in and has a response in userFormResponses
      const userResponse = userFormResponses[deletingFormId];
      const storageKey = `form_response_token_${id}_${deletingFormId}`;
      const accessToken = localStorage.getItem(storageKey);

      if (userResponse) {
        // Logged-in user - delete using userId (no token needed)
        await deleteFormResponseMutation.mutateAsync({
          eventId: id,
          formId: deletingFormId,
          responseId: userResponse.id,
          // No accessToken - backend will use userId from JWT
        });
        // React Query cache invalidation in useDeleteFormResponse onSuccess
        // automatically updates userFormResponses via useUserFormResponses hook
      } else if (accessToken) {
        // Anonymous user - delete using access token
        // Fetch the response first to get the responseId
        const response = await eventsRepository.getMyFormResponse(id, deletingFormId, accessToken);

        if (!response) {
          // Phase 6A.128b Fix: Response already gone from DB - clean up stale localStorage token
          // instead of showing an error. This handles the case where response was deleted
          // externally (e.g., via RSVP cancellation) but localStorage token persisted.
          localStorage.removeItem(storageKey);
          setDeletingFormId(null);
          return;
        }

        // Now delete the response
        await deleteFormResponseMutation.mutateAsync({
          eventId: id,
          formId: deletingFormId,
          responseId: response.id,
          accessToken,
        });
      } else {
        // Phase 6A.128b: If neither API response nor localStorage token exists,
        // the user genuinely has no response. Nothing to delete.
        setDeletingFormId(null);
        return;
      }

      setDeletingFormId(null);
      // Success toast is handled by the mutation's onSuccess callback
    } catch (err: any) {
      setError(err.message || 'Failed to delete response');
      setDeletingFormId(null);
    }
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
        <LankaEventsHeader />
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
        <LankaEventsHeader />
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

  // Phase 6A.97: Use timezone-aware date formatting for consistent display.
  // Phase 8YA.3: TBD events surface "Date TBD" / "Time TBD" placeholders so the
  // page renders cleanly without throwing on null inputs.
  const formattedStartDate = event.startDate
    ? formatEventDate(event.startDate, event.timeZoneId)
    : 'Date TBD';
  const formattedStartTime = event.startDate
    ? formatEventTime(event.startDate, event.timeZoneId)
    : 'Time TBD';
  const formattedEndTime = event.endDate
    ? formatEventTime(event.endDate, event.timeZoneId)
    : 'Time TBD';
  const timezoneAbbreviation = event.startDate
    ? getTimezoneAbbreviation(event.timeZoneId, event.startDate)
    : '';

  const isFull = event.currentRegistrations >= event.capacity;
  const spotsLeft = event.capacity - event.currentRegistrations;
  // TBD events haven't started by definition — they have no scheduled time yet.
  const hasStarted = event.startDate ? new Date(event.startDate) <= new Date() : false;
  // GitHub Issue #37: Check if event is cancelled to hide registration section
  // Note: Backend may return status as string "Cancelled" or enum number 4
  const isCancelled = (event.status as unknown) === 'Cancelled' || event.status === EventStatus.Cancelled;

  return (
    <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
      <LankaEventsHeader />

      {/* Phase 8YB.1 — Option E: full-bleed hero rendered above the constrained column.
          Only active on the /v2 test route (heroVariant="fullWidth"). The default route
          renders the hero inside the Card below (Option C). */}
      {heroVariant === 'fullWidth' && (
        <EventHeroImage
          images={event.images}
          title={event.title}
          categoryLabel={categoryLabels[event.category] ?? ''}
          variant="fullWidth"
        />
      )}

      {/* Back Button and Organizer Actions */}
      <div className="max-w-screen-2xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
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
          {event && user && event.isCurrentUserOrganizer === true && (
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
      <div className="max-w-screen-2xl mx-auto px-4 sm:px-6 lg:px-8 pb-12">
        <Card className="overflow-hidden">
          {/* Phase 8YB.1 — Option C: contained hero (responsive aspect-ratio + object-contain).
              Only renders on the default route. The /v2 route uses fullWidth above instead. */}
          {heroVariant === 'contained' && (
            <EventHeroImage
              images={event.images}
              title={event.title}
              categoryLabel={categoryLabels[event.category] ?? ''}
              variant="contained"
            />
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

                {/* Registration Badge - Issue #2: Use registration status directly */}
                <RegistrationBadge registrationStatus={registrationDetails?.status as any} compact={false} />

                {/* Phase 7A.4: WhatsApp Share */}
                <WhatsAppShareButton
                  eventTitle={event.title}
                  eventUrl={typeof window !== 'undefined' ? window.location.href : ''}
                  eventDate={event.startDate ? formatEventDate(event.startDate, event.timeZoneId) : undefined}
                  eventLocation={event.city || undefined}
                />
              </div>

              {/* Quick Navigation Bar — anchor links to sections below.
                  Phase 8YB.3: Mode-C events have no Register anchor (no section to jump
                  to), so we lead the row with a non-clickable "No registration required"
                  status pill instead of leaving the gap silent.
                  Phase 8YB.4: signup-lists / signup-forms pills are now gated on
                  presence probes (mirrors the volunteers pattern) so the row no longer
                  advertises sections the event hasn't configured. The descriptor array
                  is rendered by EventQuickNav for unit-testable visibility logic. */}
              <div className="flex flex-wrap gap-2 mb-4">
                <RegistrationStatusHint
                  registrationMode={registrationMode}
                  variant="pill"
                  isCancelled={isCancelled}
                />
                <EventQuickNav
                  pills={[
                    { id: 'registration', label: registrationCtaLabel, icon: <Users className="h-3.5 w-3.5" />, show: !isModeC },
                    { id: 'donations', label: 'Donate', icon: <Heart className="h-3.5 w-3.5" />, show: event?.donationConfig?.isEnabled === true },
                    { id: 'collections', label: 'Contribute', icon: <Wallet className="h-3.5 w-3.5" />, show: event?.collectionConfig?.isEnabled === true },
                    { id: 'sponsors', label: 'Sponsor', icon: <Award className="h-3.5 w-3.5" />, show: event?.sponsorConfig?.isEnabled === true },
                    { id: 'add-ons', label: 'Add-Ons', icon: <ShoppingBag className="h-3.5 w-3.5" />, show: event?.addOnConfig?.isEnabled === true && event?.addOnConfig?.availableStandalone === true },
                    { id: 'signup-lists', label: 'Signup Lists', icon: <List className="h-3.5 w-3.5" />, show: hasItemSignUpLists },
                    { id: 'volunteers', label: 'Volunteer', icon: <HandHeart className="h-3.5 w-3.5" />, show: hasVolunteerLists },
                    { id: 'signup-forms', label: 'Signup Forms', icon: <ClipboardList className="h-3.5 w-3.5" />, show: !isLoadingForms && activeForms.length > 0 },
                    { id: 'albums', label: 'Albums', icon: <Camera className="h-3.5 w-3.5" />, show: publishedAlbumsWithPhotos.length > 0 && (isUserRegistered || isOrganizer) },
                  ] satisfies EventQuickNavPill[]}
                />
              </div>

              {/* Phase 8YB.3: Mode-C above-the-fold "No registration required" banner.
                  Renders nothing for other modes / cancelled events — the component
                  itself gates visibility, so this is safe to always include. */}
              <div className="mb-4">
                <RegistrationStatusHint
                  registrationMode={registrationMode}
                  variant="banner"
                  isCancelled={isCancelled}
                />
              </div>

              <div
                className="prose prose-lg max-w-none text-neutral-600 leading-relaxed prose-a:text-orange-600 prose-a:underline hover:prose-a:text-orange-700"
                dangerouslySetInnerHTML={{
                  __html: sanitizeHtml(event.description)
                }}
              />
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
                      {event.locationName || `${event.city}, ${event.state}`}
                    </p>
                    {event.address && (
                      <p className="text-sm text-neutral-600">
                        {event.address}
                        {event.locationName ? `, ${event.city}, ${event.state}` : ''}
                      </p>
                    )}
                    {/* Phase 7C.1: Secondary location (parking lot or secondary venue) */}
                    {event.hasSecondaryLocation && event.secondaryLocationType && (
                      <div className="mt-2 pt-2 border-t border-neutral-200">
                        <p className="text-sm font-medium text-neutral-500">
                          {event.secondaryLocationType === 'ParkingLot' ? 'Parking Lot Address:' : 'Secondary Venue:'}
                        </p>
                        {event.secondaryLocationName && (
                          <p className="text-sm font-semibold text-neutral-900">{event.secondaryLocationName}</p>
                        )}
                        <p className="text-sm text-neutral-600">
                          {event.secondaryAddress && `${event.secondaryAddress}, `}
                          {event.secondaryCity}{event.secondaryState ? `, ${event.secondaryState}` : ''}
                        </p>
                      </div>
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
                  ) : event.hasTicketTiers && event.ticketTiers && event.ticketTiers.length > 0 ? (
                    // Phase 8: Multi-tier ticketing display
                    <div className="space-y-1">
                      <p className="text-sm font-medium text-neutral-600 mb-1">Ticket Tiers</p>
                      {event.ticketTiers.filter(t => t.isActive).map((tier) => (
                        <div key={tier.id} className="flex justify-between items-center">
                          <span className="text-base font-semibold" style={{ color: '#8B1538' }}>
                            {tier.name}: {tier.isFree ? 'Free' : `$${tier.adultPriceAmount.toFixed(2)}`}
                            {tier.childPriceAmount != null && !tier.isFree && (
                              <span className="text-sm text-neutral-500 ml-1">
                                (Child: ${tier.childPriceAmount.toFixed(2)})
                              </span>
                            )}
                          </span>
                          <span className={`text-xs px-2 py-0.5 rounded-full ${
                            tier.availableQuantity === 0
                              ? 'bg-red-100 text-red-700'
                              : tier.availableQuantity <= 10
                              ? 'bg-orange-100 text-orange-700'
                              : 'bg-green-100 text-green-700'
                          }`}>
                            {tier.availableQuantity === 0 ? 'Sold Out' : `${tier.availableQuantity} left`}
                          </span>
                        </div>
                      ))}
                    </div>
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

            {/* Phase 6A.145 Commit 5 — top-of-page preview strips. Render add-ons
                first (always visible if any active add-ons), then sponsors-with-images.
                Both replace where the MediaGallery used to live so the prominent
                slot goes to the items the operator most cares about. MediaGallery
                moves to its own collapsible "Event Media" section below the card. */}
            <AddOnsPreviewStrip eventId={event.id} addOnConfig={event.addOnConfig} />
            <SponsorsPreviewStrip eventId={event.id} sponsorConfig={event.sponsorConfig} />

          </CardContent>
        </Card>

        {/* Phase 6A.145 Commit 5 — Event Media section, default-collapsed. The
            photos+videos previously lived inside the main event-details card; the
            operator wanted that space reserved for add-ons/sponsors instead. */}
        {((event.images && event.images.length > 0) || (event.videos && event.videos.length > 0)) && (
          <div id="event-media" className="mt-8">
            <CollapsibleSection
              title="Event Media"
              description={`${event.images?.length ?? 0} photo${event.images?.length === 1 ? '' : 's'}${
                event.videos?.length ? ` and ${event.videos.length} video${event.videos.length === 1 ? '' : 's'}` : ''
              }`}
              icon={<Camera className="h-5 w-5 text-neutral-500" />}
              defaultOpen={false}
            >
              <MediaGallery images={event.images} videos={event.videos} />
            </CollapsibleSection>
          </div>
        )}

        {/* Registration Section — outside Event Details card */}
        <div id="registration" className="mt-8">
          <CollapsibleSection
            title={isCancelled
                ? 'Event Cancelled'
                : isUserRegistered
                ? "You're Registered!"
                : registrationDetails?.status === 'Cancelled'
                ? 'Registration Cancelled'
                : registrationSectionTitle}
              description={isCancelled
                ? 'This event has been cancelled. Registration is not available.'
                : isModeC
                ? "This is a drop-in event — no registration needed. Donations, sponsorships, and other contributions are still welcome."
                : isUserRegistered
                ? 'Click to view your registration details'
                : registrationDetails?.status === 'Cancelled'
                ? hasStarted
                  ? 'Your registration was cancelled. This event has already started, so new registrations are not allowed.'
                  : 'Your registration for this event has been cancelled. You can register again if you wish.'
                : hasStarted
                ? 'This event has already started. Registration is no longer available.'
                : isFull
                ? 'This event is currently full. Join the waitlist to be notified when spots become available.'
                : 'Reserve your spot now!'}
              borderColor="#FF7900"
              defaultOpen={false}
              badge={isUserRegistered ? <CheckCircle className="h-5 w-5 text-green-600" /> : undefined}
            >
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
                ) : isExternalPaid && !isUserRegistered ? (
                  // Phase 8X.12 — single registration-section gate for ExternalPaid.
                  // Replaces the prior 5 RsvpFormSection mount sites that were ungated for
                  // ExternalPaid (only the 1149 site was — see UAT defect D2). On-platform
                  // states (refund-in-progress / expired-checkout / incomplete-payment) are
                  // structurally impossible for ExternalPaid (no on-platform registrations),
                  // so this branch is the complete handler for non-cancelled ExternalPaid views.
                  // Decision #1 = B: already-registered users (impossible for ExternalPaid but
                  // defensive) fall through to the isUserRegistered branch below for context.
                  <ExternalRegistrationCta event={event} />
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
                    ) : isExternalPaid ? (
                      // Phase 8X.7+8: ExternalPaid events render the outbound CTA + optional
                      // vendor + instructions instead of an internal registration form. No
                      // capacity / "full" check applies — capacity is informational only for
                      // ExternalPaid since registrations happen off-platform.
                      <ExternalRegistrationCta event={event} />
                    ) : !isFull ? (
                      // Phase 7E.6: dispatcher routes to per-attendee form (Mode A), head-count
                      // form (Mode B), or "no registration required" notice (Mode C).
                      <RsvpFormSection
                        event={event}
                        spotsLeft={spotsLeft}
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

                            {/* Phase 7F-E.2: cross-surface RegistrationBreakdown card.
                                Shows per-tier rows with N/A placeholders for un-captured
                                axes (B1: both N/A; B2: gender N/A; B3: age N/A; B4: both
                                captured). Renders for both Mode A AND Mode B per architect
                                "in addition to" rule — Mode A still shows the per-attendee
                                list below. */}
                            {registrationDetails.breakdown && (
                              <div className="mb-3">
                                <RegistrationBreakdownCard
                                  breakdown={registrationDetails.breakdown}
                                  leadAttendeeName={registrationDetails.leadAttendeeName}
                                />
                              </div>
                            )}

                            {/* Attendees Section - Show if we have attendees array with items.
                                Phase 7F-E.4b fix: under Mode B the attendees list is empty AND
                                the breakdown card above already shows "Number of attendees" —
                                so the legacy fallback line is suppressed when breakdown is set
                                to avoid the duplicated count. */}
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
                            ) : !registrationDetails.breakdown ? (
                              <div className="text-sm text-green-800 dark:text-green-200">
                                <p>Number of attendees: {registrationDetails.quantity || userRsvp?.currentRegistrations || 1}</p>
                              </div>
                            ) : null}
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

                    {/* Phase 6A.148 — Refund approval workflow integration.
                        Banner shown when a refund request exists (active or terminal).
                        Withdrawn requests render nothing (component returns null). */}
                    {myRefundRequest && (
                      <RefundRequestStatusBanner
                        refundRequest={myRefundRequest}
                        isWithdrawing={isWithdrawingV2}
                        onWithdraw={async () => {
                          if (!id) return;
                          setIsWithdrawingV2(true);
                          try {
                            await eventsRepository.withdrawMyRefundRequest(id);
                            const refreshed = await eventsRepository.getMyRefundRequest(id);
                            setMyRefundRequest(refreshed);
                            // Registration query will refresh on next refetch tick; the
                            // local banner state already reflects the change.
                          } catch (err) {
                            console.error('[6A.148] withdraw failed:', err);
                          } finally {
                            setIsWithdrawingV2(false);
                          }
                        }}
                      />
                    )}

                    {/* Phase 6A.148 — "Request Refund" button alongside legacy Cancel.
                        Only shown when: paid registration, no active refund request,
                        backend feature flag enabled (myRefundRequest can still be null
                        with flag on if no request exists; the click handler will surface
                        backend 404 if the flag is off). */}
                    {isPaidRegistration && !hasActiveRefundRequest && !hasStarted && (
                      <div className="mb-3">
                        <Button
                          variant="outline"
                          className="w-full"
                          style={{ borderColor: '#2563EB', color: '#2563EB' }}
                          onClick={() => setShowRequestRefundDialog(true)}
                        >
                          Request Refund
                        </Button>
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

                          {/* Cancellation enhancement: User choice for form response deletion */}
                          {activeForms.length > 0 && (
                            <div className="p-3 bg-yellow-50 border border-yellow-200 rounded-lg">
                              <label className="flex items-start gap-3 cursor-pointer">
                                <input
                                  type="checkbox"
                                  checked={deleteFormResponses}
                                  onChange={(e) => setDeleteFormResponses(e.target.checked)}
                                  className="mt-1 h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                                />
                                <div className="flex-1">
                                  <p className="text-sm font-medium text-gray-900">
                                    Also delete my form submissions
                                  </p>
                                  <p className="text-xs text-gray-600 mt-1">
                                    {deleteFormResponses
                                      ? "Your sign-up form responses will be permanently deleted."
                                      : "Your form submissions will be kept even after cancellation (default)."}
                                  </p>
                                </div>
                              </label>
                            </div>
                          )}

                          {/* Cancellation enhancement: User choice for add-on purchase refund */}
                          {(() => {
                            // Phase 6A.137F-Fix4: Scope to current registration only — excludes orphaned purchases from previous registrations
                            const completedAddOnPurchases = myAddOnPurchases?.filter((p: any) => p.status === 'Completed' && p.registrationId === registrationDetails?.id) || [];
                            if (completedAddOnPurchases.length === 0) return null;
                            const totalAddOnAmount = completedAddOnPurchases.reduce((sum: number, p: any) => sum + (p.totalAmount ?? 0), 0);
                            return (
                              <div className="p-3 bg-yellow-50 border border-yellow-200 rounded-lg">
                                <label className="flex items-start gap-3 cursor-pointer">
                                  <input
                                    type="checkbox"
                                    checked={refundAddOnPurchases}
                                    onChange={(e) => setRefundAddOnPurchases(e.target.checked)}
                                    className="mt-1 h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                                  />
                                  <div className="flex-1">
                                    <p className="text-sm font-medium text-gray-900">
                                      Also refund my add-on purchases (${totalAddOnAmount.toFixed(2)})
                                    </p>
                                    <p className="text-xs text-gray-600 mt-1">
                                      {refundAddOnPurchases
                                        ? `${completedAddOnPurchases.length} add-on purchase(s) totaling $${totalAddOnAmount.toFixed(2)} will be refunded to your original payment method.`
                                        : "Your add-on purchases will not be refunded (default)."}
                                    </p>
                                  </div>
                                </label>
                              </div>
                            );
                          })()}

                          {/* Phase 6A.137F: Collection refund checkbox */}
                          {(() => {
                            const completedCollections = myCollections?.filter((c: any) => c.status === 'Completed') || [];
                            if (completedCollections.length === 0) return null;
                            const totalCollectionAmount = completedCollections.reduce((sum: number, c: any) => sum + (c.amount ?? 0), 0);
                            return (
                              <div className="p-3 bg-yellow-50 border border-yellow-200 rounded-lg">
                                <label className="flex items-start gap-3 cursor-pointer">
                                  <input
                                    type="checkbox"
                                    checked={refundCollections}
                                    onChange={(e) => setRefundCollections(e.target.checked)}
                                    className="mt-1 h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                                  />
                                  <div className="flex-1">
                                    <p className="text-sm font-medium text-gray-900">
                                      Also refund my collection contribution (${totalCollectionAmount.toFixed(2)})
                                    </p>
                                    <p className="text-xs text-gray-600 mt-1">
                                      {refundCollections
                                        ? `$${totalCollectionAmount.toFixed(2)} will be refunded to your original payment method.`
                                        : "Your collection contribution will not be refunded (default)."}
                                    </p>
                                  </div>
                                </label>
                              </div>
                            );
                          })()}

                          {/* Phase 6A.137F: Sponsor refund checkbox */}
                          {(() => {
                            const completedSponsors = mySponsors?.filter((s: any) => s.sponsorType === 'Money' && s.status === 'Completed') || [];
                            if (completedSponsors.length === 0) return null;
                            const totalSponsorAmount = completedSponsors.reduce((sum: number, s: any) => sum + (s.amount ?? 0), 0);
                            return (
                              <div className="p-3 bg-yellow-50 border border-yellow-200 rounded-lg">
                                <label className="flex items-start gap-3 cursor-pointer">
                                  <input
                                    type="checkbox"
                                    checked={refundSponsors}
                                    onChange={(e) => setRefundSponsors(e.target.checked)}
                                    className="mt-1 h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                                  />
                                  <div className="flex-1">
                                    <p className="text-sm font-medium text-gray-900">
                                      Also refund my sponsorship (${totalSponsorAmount.toFixed(2)})
                                    </p>
                                    <p className="text-xs text-gray-600 mt-1">
                                      {refundSponsors
                                        ? `$${totalSponsorAmount.toFixed(2)} will be refunded to your original payment method.`
                                        : "Your sponsorship will not be refunded (default)."}
                                    </p>
                                  </div>
                                </label>
                              </div>
                            );
                          })()}

                          {/* Non-refundable financial items disclaimer (donations only) */}
                          {(() => {
                            const completedDonations = myDonations?.filter((d: any) => d.status === 'Completed') || [];
                            const totalNonRefundable = completedDonations.reduce((sum: number, d: any) => sum + (d.amount ?? 0), 0);

                            if (totalNonRefundable <= 0) return null;

                            return (
                              <div className="p-3 bg-amber-50 border border-amber-200 rounded-lg">
                                <div className="flex items-start gap-2">
                                  <svg className="h-4 w-4 text-amber-600 flex-shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.082 16.5c-.77.833.192 2.5 1.732 2.5z" />
                                  </svg>
                                  <div className="flex-1">
                                    <p className="text-xs font-medium text-amber-800">
                                      Non-refundable: Donations (${totalNonRefundable.toFixed(2)})
                                    </p>
                                    <p className="text-xs text-amber-600 mt-1">
                                      Donations are voluntary and will not be refunded.
                                    </p>
                                  </div>
                                </div>
                              </div>
                            );
                          })()}

                          {/* Phase 6A.93: Notification about two emails for paid registrations */}
                          {isPaidRegistration && (
                            <div className="p-3 bg-blue-50 border border-blue-200 rounded-lg">
                              <div className="flex items-start gap-2">
                                <svg
                                  className="h-4 w-4 text-blue-600 flex-shrink-0 mt-0.5"
                                  fill="none"
                                  stroke="currentColor"
                                  viewBox="0 0 24 24"
                                >
                                  <path
                                    strokeLinecap="round"
                                    strokeLinejoin="round"
                                    strokeWidth={2}
                                    d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
                                  />
                                </svg>
                                <p className="text-xs text-blue-800">
                                  You will receive <strong>two emails</strong>: one confirming your cancellation, and another with refund details.
                                </p>
                              </div>
                            </div>
                          )}

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
                                setDeleteFormResponses(false);
                                setRefundAddOnPurchases(false);
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
                                  console.log('[CancelRsvp] User confirmed cancellation, DeleteSignUpCommitments:', deleteSignUpCommitments, 'DeleteFormResponses:', deleteFormResponses, 'RefundAddOnPurchases:', refundAddOnPurchases, 'RefundCollections:', refundCollections, 'RefundSponsors:', refundSponsors);
                                  setIsCancelling(true);
                                  setCancelError(null);
                                  const cancelResult = await eventsRepository.cancelRsvp(id, {
                                    deleteSignUpCommitments,
                                    deleteFormResponses,
                                    refundAddOnPurchases,
                                    refundCollections,
                                    refundSponsors,
                                  });
                                  console.log('[CancelRsvp] Successfully cancelled registration', cancelResult);

                                  // Show warnings for partial failures before reloading
                                  if (cancelResult?.warnings && cancelResult.warnings.length > 0) {
                                    const warningMsg = cancelResult.warnings.join('\n');
                                    console.warn('[CancelRsvp] Partial failures:', warningMsg);
                                    alert(`Registration cancelled, but some actions had issues:\n\n${warningMsg}`);
                                  }

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
                  // Phase 6A.93: Refund requested state - show status with register again option
                  retryAfterRefund ? (
                    // User clicked "Register Again" - show the registration form
                    <div className="space-y-4">
                      <div className="p-3 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg mb-4">
                        <p className="text-sm text-blue-800 dark:text-blue-200">
                          Your previous registration has a refund in progress. Complete the form below to create a new registration.
                        </p>
                      </div>
                      {!isFull ? (
                        // Phase 7E.6: mode-aware dispatch
                        <RsvpFormSection
                          event={event}
                          spotsLeft={spotsLeft}
                          isProcessing={isProcessing}
                          onSubmit={handleRegistration}
                          error={error}
                        />
                      ) : (
                        <div className="p-4 bg-orange-50 border border-orange-200 rounded-lg">
                          <p className="text-sm text-orange-800">
                            This event is currently full.
                          </p>
                        </div>
                      )}
                    </div>
                  ) : (
                    // Show "Refund in Progress" status with options
                    <div className="space-y-4">
                      <div className="p-4 bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg">
                        <div className="flex items-center gap-2 mb-3">
                          <AlertCircle className="h-5 w-5 text-yellow-600 dark:text-yellow-400" />
                          <h3 className="text-lg font-semibold text-yellow-900 dark:text-yellow-100">
                            Refund in Progress
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

                        {/* Action Buttons */}
                        <div className="space-y-3">
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
                            {hasStarted && (
                              <div className="absolute bottom-full left-1/2 transform -translate-x-1/2 mb-2 px-3 py-2 bg-gray-900 text-white text-xs rounded-lg opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap z-10">
                                Withdrawal is not available after the event has started
                              </div>
                            )}
                          </div>

                          {/* Phase 6A.93: Register Again Button */}
                          {!hasStarted && !isFull && (
                            <Button
                              className="w-full"
                              style={{
                                backgroundColor: '#FF7900',
                                color: '#FFFFFF'
                              }}
                              onClick={() => {
                                console.log('[RefundRequested] User clicked Register Again - showing registration form');
                                setRetryAfterRefund(true);
                              }}
                            >
                              Register Again
                            </Button>
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
                  )
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
                  // Phase 6A.137F Fix: Skip "Checkout Session Expired" banner — show registration form directly.
                  // Users were stuck in a loop: expired → banner → "Register Again" → expired again.
                  // Now the form is shown immediately so users can re-register without an extra click.
                  <div className="space-y-4">
                    <div className="p-3 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg mb-4">
                      <p className="text-sm text-blue-800 dark:text-blue-200">
                        Your previous checkout session expired. Complete the form below to get a new payment link.
                      </p>
                    </div>
                    {/* Phase 7E.6: mode-aware dispatch */}
                    <RsvpFormSection
                      event={event}
                      spotsLeft={spotsLeft}
                      isProcessing={isProcessing}
                      onSubmit={handleRegistration}
                      error={error}
                    />
                  </div>
                ) : isPaymentIncomplete ? (
                  // Phase 6A.137F Fix: Show registration form directly for incomplete payments too.
                  // Same rationale as isAbandoned — skip the banner, let users re-register immediately.
                  <div className="space-y-4">
                    <div className="p-3 bg-orange-50 dark:bg-orange-900/20 border border-orange-200 dark:border-orange-800 rounded-lg mb-4">
                      <p className="text-sm text-orange-800 dark:text-orange-200">
                        Your previous registration had incomplete payment. Complete the form below to get a new payment link.
                      </p>
                    </div>
                    {/* Phase 7E.6: mode-aware dispatch */}
                    <RsvpFormSection
                      event={event}
                      spotsLeft={spotsLeft}
                      isProcessing={isProcessing}
                      onSubmit={handleRegistration}
                      error={error}
                    />
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
                  // Phase 7E.6: mode-aware dispatch
                  // Phase 6A.144: For anonymous users on PAID events, show the
                  // soft auth-encouragement prompt in place of the form. The
                  // form re-mounts once the user chooses "Continue as Guest"
                  // (sets sessionStorage flag) or signs in. Recovery flows
                  // (isAbandoned / isPaymentIncomplete / refund-retry /
                  // cancellation re-register) are intentionally NOT gated —
                  // the user is mid-flow and re-prompting would disrupt UX.
                  <div id="rsvp-section">
                    {shouldShowAuthNudge({
                      isAuthenticated,
                      isFree: !!event.isFree,
                      guestAcknowledged: guestModeAcknowledged,
                    }) ? (
                      <AuthEncouragementPrompt
                        eventTitle={event.title}
                        onClick={() => setShowAuthNudge(true)}
                      />
                    ) : (
                      <RsvpFormSection
                        event={event}
                        spotsLeft={spotsLeft}
                        isProcessing={isProcessing}
                        onSubmit={handleRegistration}
                        error={error}
                      />
                    )}
                  </div>
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
            </CollapsibleSection>
        </div>

        {/* Phase 6A.24: Ticket Section for Paid Events */}
        {/* Shows QR code, download PDF, and resend email buttons for registered paid events */}
        {/* Wait for auth hydration before rendering to ensure token is available for API calls */}
        {/* Phase 8X.7+8: TicketSection only renders for OnPlatformPaid (paid + Stripe-issued
            tickets). ExternalPaid events have no internal Registration row and no internal
            ticket — ticketing happens off-platform. */}
        {_hasHydrated && isUserRegistered && event && !event.isFree && !isExternalPaid && (
          <div className="mt-8">
            <TicketSection eventId={id} isPaidEvent={!event.isFree} />
          </div>
        )}

        {/* Donation Feature: Success/Cancelled Banner */}
        {donationStatus === 'success' && (
          <div className="mt-8 p-4 bg-emerald-50 border border-emerald-200 rounded-lg">
            <div className="flex items-center gap-3">
              <Heart className="h-6 w-6 text-emerald-600" />
              <div>
                <h3 className="font-semibold text-emerald-800">Thank you for your generous donation!</h3>
                <p className="text-sm text-emerald-700 mt-0.5">
                  Your payment has been processed successfully. You will receive a confirmation email shortly.
                </p>
              </div>
            </div>
          </div>
        )}
        {donationStatus === 'cancelled' && (
          <div className="mt-8 p-4 bg-amber-50 border border-amber-200 rounded-lg">
            <div className="flex items-center gap-3">
              <Heart className="h-6 w-6 text-amber-600" />
              <div>
                <h3 className="font-semibold text-amber-800">Donation cancelled</h3>
                <p className="text-sm text-amber-700 mt-0.5">
                  Your donation was not processed. You can try again below if you&apos;d like to support this event.
                </p>
              </div>
            </div>
          </div>
        )}

        {/* Collection Feature: Success/Cancelled Banner */}
        {collectionStatus === 'success' && (
          <div className="mt-8 p-4 bg-emerald-50 border border-emerald-200 rounded-lg">
            <div className="flex items-center gap-3">
              <Wallet className="h-6 w-6 text-emerald-600" />
              <div>
                <h3 className="font-semibold text-emerald-800">Thank you for your contribution!</h3>
                <p className="text-sm text-emerald-700 mt-0.5">
                  Your payment has been processed successfully. You will receive a confirmation email shortly.
                </p>
              </div>
            </div>
          </div>
        )}
        {collectionStatus === 'cancelled' && (
          <div className="mt-8 p-4 bg-amber-50 border border-amber-200 rounded-lg">
            <div className="flex items-center gap-3">
              <Wallet className="h-6 w-6 text-amber-600" />
              <div>
                <h3 className="font-semibold text-amber-800">Contribution cancelled</h3>
                <p className="text-sm text-amber-700 mt-0.5">
                  Your contribution was not processed. You can try again below.
                </p>
              </div>
            </div>
          </div>
        )}

        {/* Sponsor Feature: Success/Cancelled Banner */}
        {sponsorStatus === 'success' && (
          <div className="mt-8 p-4 bg-emerald-50 border border-emerald-200 rounded-lg">
            <div className="flex items-center gap-3">
              <Award className="h-6 w-6 text-emerald-600" />
              <div>
                <h3 className="font-semibold text-emerald-800">Thank you for your sponsorship!</h3>
                <p className="text-sm text-emerald-700 mt-0.5">
                  Your sponsorship has been processed successfully. You will receive a confirmation email shortly.
                </p>
              </div>
            </div>
          </div>
        )}
        {sponsorStatus === 'cancelled' && (
          <div className="mt-8 p-4 bg-amber-50 border border-amber-200 rounded-lg">
            <div className="flex items-center gap-3">
              <Award className="h-6 w-6 text-amber-600" />
              <div>
                <h3 className="font-semibold text-amber-800">Sponsorship cancelled</h3>
                <p className="text-sm text-amber-700 mt-0.5">
                  Your sponsorship was not processed. You can try again below.
                </p>
              </div>
            </div>
          </div>
        )}

        {/* Add-On Feature: Success/Cancelled Banner */}
        {addOnStatus === 'success' && (
          <div className="mt-8 p-4 bg-emerald-50 border border-emerald-200 rounded-lg">
            <div className="flex items-center gap-3">
              <ShoppingBag className="h-6 w-6 text-emerald-600" />
              <div>
                <h3 className="font-semibold text-emerald-800">Purchase successful!</h3>
                <p className="text-sm text-emerald-700 mt-0.5">
                  Your add-on purchase has been processed. You will receive a confirmation email shortly.
                </p>
              </div>
            </div>
          </div>
        )}
        {addOnStatus === 'cancelled' && (
          <div className="mt-8 p-4 bg-amber-50 border border-amber-200 rounded-lg">
            <div className="flex items-center gap-3">
              <ShoppingBag className="h-6 w-6 text-amber-600" />
              <div>
                <h3 className="font-semibold text-amber-800">Purchase cancelled</h3>
                <p className="text-sm text-amber-700 mt-0.5">
                  Your add-on purchase was not processed. You can try again below.
                </p>
              </div>
            </div>
          </div>
        )}

        {/* Donation Feature: Combined Section (summary + donate form + your donations) */}
        {event?.donationConfig?.isEnabled === true && (
          <div id="donations" className="mt-8">
            <DonationSection
              eventId={id}
              donationConfig={event.donationConfig}
              publicSummary={publicDonationSummary}
              myDonations={myDonations}
            />
          </div>
        )}

        {/* Collection Feature: Public contribution form with goal progress */}
        {event?.collectionConfig?.isEnabled === true && (
          <div id="collections" className="mt-8">
            <CollectionSection
              eventId={id}
              collectionConfig={event.collectionConfig}
              publicSummary={publicCollectionSummary}
              myCollections={myCollections}
            />
          </div>
        )}

        {/* Sponsor Feature: Public sponsor form (money via Stripe + item submissions) */}
        {event?.sponsorConfig?.isEnabled === true && (
          <div id="sponsors" className="mt-8">
            <SponsorSection
              eventId={id}
              sponsorConfig={event.sponsorConfig}
              mySponsors={mySponsors}
            />
          </div>
        )}

        {/* Add-On Feature: Purchasable items with stock levels */}
        {event?.addOnConfig?.isEnabled === true && event?.addOnConfig?.availableStandalone === true && (
          <div id="add-ons" className="mt-8">
            <AddOnSelector
              eventId={id}
              addOnConfig={event.addOnConfig}
              myAddOnPurchases={myAddOnPurchases}
            />
          </div>
        )}

        {/* After Event Albums — shows published albums with photo carousel */}
        {publishedAlbumsWithPhotos.length > 0 && (isUserRegistered || isOrganizer) && (
          <div id="albums" className="mt-8 scroll-mt-20">
            <CollapsibleSection
              title="After Event Albums"
              icon={<Camera className="h-5 w-5 text-purple-600" />}
              defaultOpen={false}
            >
              <div className="space-y-4">
                {/* Album Tabs (if multiple) */}
                {publishedAlbumsWithPhotos.length > 1 && (
                  <div className="flex items-center gap-2 overflow-x-auto pb-2 border-b">
                    {publishedAlbumsWithPhotos.map((album) => (
                      <button
                        key={album.id}
                        type="button"
                        onClick={() => setActiveCarouselAlbumId(album.id)}
                        className={`px-3 py-1.5 text-sm font-medium rounded-t-lg whitespace-nowrap transition-colors ${
                          activeCarouselAlbum?.id === album.id
                            ? 'bg-white border border-b-0 border-gray-200 text-gray-900'
                            : 'text-gray-500 hover:text-gray-700 hover:bg-gray-50'
                        }`}
                      >
                        {album.name}
                        <span className="ml-1.5 text-xs text-gray-400">({album.photoCount})</span>
                      </button>
                    ))}
                  </div>
                )}

                {/* Active Album Carousel */}
                {activeCarouselAlbum && (
                  <>
                    {publishedAlbumsWithPhotos.length === 1 && (
                      <div className="flex items-center justify-between">
                        <h3 className="text-sm font-medium text-gray-700">
                          {activeCarouselAlbum.name}
                          <span className="ml-2 text-xs text-gray-400 font-normal">
                            {activeCarouselAlbum.photoCount} {activeCarouselAlbum.photoCount === 1 ? 'item' : 'items'}
                          </span>
                        </h3>
                      </div>
                    )}

                    <AlbumPhotoCarousel
                      eventId={id}
                      albumId={activeCarouselAlbum.id}
                      onPhotoClick={() => router.push(`/events/${id}/photos?album=${activeCarouselAlbum.id}`)}
                    />

                    {/* Actions */}
                    <div className="flex items-center gap-3 pt-2">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => router.push(`/events/${id}/photos?album=${activeCarouselAlbum.id}`)}
                      >
                        View All Photos
                      </Button>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() =>
                          downloadZip.mutateAsync({
                            eventId: id,
                            albumId: activeCarouselAlbum.id,
                            albumName: activeCarouselAlbum.name,
                          })
                        }
                        disabled={downloadZip.isPending}
                      >
                        {downloadZip.isPending ? (
                          <Loader2 className="h-4 w-4 animate-spin" />
                        ) : (
                          <>
                            <Download className="h-4 w-4 mr-1" />
                            Download ZIP
                          </>
                        )}
                      </Button>
                    </div>
                  </>
                )}
              </div>
            </CollapsibleSection>
          </div>
        )}

        {/* Event Organizer Contacts - Collapsible */}
        {event && event.publishOrganizerContact && event.organizerContacts && event.organizerContacts.length > 0 && (
          <div className="mt-6">
            <CollapsibleSection
              title="Event Organizer Contacts"
              icon={<Users className="h-5 w-5 text-blue-600" />}
              defaultOpen={false}
            >
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="bg-gray-50 border-b border-gray-200">
                      <th className="text-left px-4 py-2 font-medium text-gray-600">Name</th>
                      <th className="text-left px-4 py-2 font-medium text-gray-600">Email</th>
                      <th className="text-left px-4 py-2 font-medium text-gray-600">Phone</th>
                    </tr>
                  </thead>
                  <tbody>
                    {event.organizerContacts.map((contact, idx) => (
                      <tr key={contact.id || idx} className="border-b border-gray-100">
                        <td className="px-4 py-2">
                          <span className="font-medium text-gray-900">{contact.contactName}</span>
                          {contact.isPrimary && (
                            <span className="ml-2 inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-blue-100 text-blue-800">
                              Primary
                            </span>
                          )}
                        </td>
                        <td className="px-4 py-2 text-gray-600">
                          {contact.contactEmail ? (
                            <a href={`mailto:${contact.contactEmail}`} className="text-blue-600 hover:underline">{contact.contactEmail}</a>
                          ) : '—'}
                        </td>
                        <td className="px-4 py-2 text-gray-600">
                          {contact.contactPhone ? (
                            <a href={`tel:${contact.contactPhone}`} className="text-blue-600 hover:underline">{contact.contactPhone}</a>
                          ) : '—'}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </CollapsibleSection>
          </div>
        )}

        {/* Signup Lists Section — CollapsibleSection (replaces old TabPanel) */}
        {/* Backward compat: hidden anchor for email links using #sign-ups */}
        <div id="sign-ups" className="sr-only" aria-hidden="true" />
        {/* Phase 8YB.4: only render when at least one Items-kind signup list exists,
            mirroring the volunteers section pattern below. Otherwise the page used
            to ship an empty CollapsibleSection card for events that never created
            any lists. */}
        {hasItemSignUpLists && (
          <div id="signup-lists" className="mt-8">
            <CollapsibleSection
              title="Signup Lists"
              icon={<List className="h-5 w-5 text-indigo-600" />}
              defaultOpen={false}
            >
              <SignUpManagementSection
                eventId={id}
                userId={user?.userId}
                isOrganizer={false}
                kind={SignUpKind.Items}
              />
            </CollapsibleSection>
          </div>
        )}

        {/* Phase 7D.1 Phase G: Volunteer Roles — dedicated section, separate from Signup Lists */}
        {hasVolunteerLists && (
          <div id="volunteers" className="mt-8">
            <CollapsibleSection
              title="Volunteer Roles"
              icon={<HandHeart className="h-5 w-5 text-rose-600" />}
              defaultOpen={false}
            >
              <SignUpManagementSection
                eventId={id}
                userId={user?.userId}
                isOrganizer={false}
                kind={SignUpKind.Volunteers}
                labels={volunteerSectionLabels}
              />
            </CollapsibleSection>
          </div>
        )}

        {/* Signup Forms Section — CollapsibleSection.
            Phase 8YB.4: only render when at least one Active form exists; otherwise
            the page used to ship an empty "No signup forms available for this event
            yet." card on every event without forms. The probe is `!isLoadingForms &&
            activeForms.length > 0` (matches the quick-nav pill gate) so the section
            stays hidden during the in-flight fetch — avoids flash-then-disappear. */}
        {!isLoadingForms && activeForms.length > 0 && (
        <div id="signup-forms" className="mt-8">
          <CollapsibleSection
            title="Signup Forms"
            description="Fill out forms to provide additional information for this event"
            icon={<ClipboardList className="h-5 w-5 text-violet-600" />}
            defaultOpen={false}
            summary={
              !isLoadingForms && activeForms.length > 0 ? (
                <span className="inline-flex items-center gap-2">
                  <span className="font-medium text-neutral-700">
                    {activeForms.length} form{activeForms.length !== 1 ? 's' : ''} available
                  </span>
                  {(() => {
                    const respondedCount = activeForms.filter((f) => {
                      const userResponse = userFormResponses[f.id];
                      const hasUserResponse = userResponse !== null && userResponse !== undefined;
                      const storageKey = `form_response_token_${id}_${f.id}`;
                      const hasStoredToken = typeof window !== 'undefined' && !!localStorage.getItem(storageKey);
                      return isAuthenticated ? hasUserResponse : (hasStoredToken || hasUserResponse);
                    }).length;
                    const pending = activeForms.length - respondedCount;
                    if (pending > 0) {
                      return (
                        <span className="text-orange-700">
                          • {pending} need{pending === 1 ? 's' : ''} your response
                        </span>
                      );
                    }
                    return <span className="text-green-700">• All responses submitted</span>;
                  })()}
                </span>
              ) : undefined
            }
          >
            {!isLoadingForms && activeForms.length > 0 ? (
              <div className="space-y-4">
                {activeForms.map((form) => {
                  // Phase 6A.128b Fix: For authenticated users, API is the single source of truth.
                  const storageKey = `form_response_token_${id}_${form.id}`;
                  const hasStoredToken = typeof window !== 'undefined' && !!localStorage.getItem(storageKey);
                  const userResponse = userFormResponses[form.id];
                  const hasUserResponse = userResponse !== null && userResponse !== undefined;
                  const hasResponded = isAuthenticated ? hasUserResponse : (hasStoredToken || hasUserResponse);

                  const isFormFull = form.maxResponses != null && form.maxResponses > 0 && form.responseCount >= form.maxResponses;
                  const isDeadlinePassed = form.responseDeadline != null && new Date(form.responseDeadline) < new Date();

                  return (
                    <Card key={form.id} className="border border-gray-200">
                      <CardContent className="pt-6">
                        <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4">
                          <div className="flex-1">
                            <h3 className="text-lg font-semibold text-gray-900 mb-2">
                              {form.title}
                            </h3>
                            {form.description && (
                              <p className="text-sm text-gray-600 mb-3">
                                {form.description}
                              </p>
                            )}
                            <div className="flex flex-wrap gap-3 text-sm text-gray-500">
                              {hasResponded && (
                                <span className="flex items-center gap-1 text-green-600 font-medium">
                                  <CheckCircle className="h-4 w-4" />
                                  You already responded
                                </span>
                              )}
                              {form.responseCount > 0 && (
                                <span className="flex items-center gap-1">
                                  <Users className="h-4 w-4" />
                                  {form.responseCount} response{form.responseCount !== 1 ? 's' : ''}
                                </span>
                              )}
                              {form.responseDeadline && (
                                <span className="flex items-center gap-1">
                                  <Clock className="h-4 w-4" />
                                  Due: {new Date(form.responseDeadline).toLocaleDateString()}
                                </span>
                              )}
                              {form.maxResponses != null && form.maxResponses > 0 && (
                                <span className="flex items-center gap-1 text-orange-600">
                                  <AlertCircle className="h-4 w-4" />
                                  {form.maxResponses - form.responseCount} spot{(form.maxResponses - form.responseCount) !== 1 ? 's' : ''} left
                                </span>
                              )}
                            </div>
                          </div>
                          <div className="flex-shrink-0 flex gap-2">
                            <Button
                              onClick={() => router.push(`/events/${id}/forms/${form.id}`)}
                              disabled={isFormFull || isDeadlinePassed}
                              variant={hasResponded ? 'outline' : 'default'}
                              className="w-full sm:w-auto"
                            >
                              {isFormFull
                                ? 'Form Full'
                                : isDeadlinePassed
                                ? 'Deadline Passed'
                                : hasResponded
                                ? 'Edit Your Response'
                                : 'Fill Out Form'}
                            </Button>
                            {/* Phase 6A.106-110 Fix: Delete button for BOTH anonymous and logged-in users */}
                            {hasResponded && !isFormFull && !isDeadlinePassed && (
                              <Button
                                variant="ghost"
                                onClick={() => {
                                  setDeletingFormId(form.id);
                                  setShowFormDeleteConfirm(true);
                                }}
                                className="text-red-600 hover:text-red-700 hover:bg-red-50 w-full sm:w-auto"
                              >
                                <Trash2 className="h-4 w-4" />
                                <span className="sr-only">Delete response</span>
                              </Button>
                            )}
                          </div>
                        </div>

                        {/* Phase 6A.146 (2026-05-15 UAT correction): inline
                            Show/Hide responses toggle. Visible only when the
                            organizer has enabled public visibility for this
                            form AND it has at least one response. Status gate
                            (Active/Closed only) is enforced inside the embedded
                            section component, so we can safely render the
                            button for Active forms here (forms in #signup-forms
                            are already filtered to Active by activeForms). */}
                        {form.allowAttendeesToViewResponses && form.responseCount > 0 && (
                          <>
                            <div className="mt-4 flex justify-start">
                              <Button
                                variant="outline"
                                size="sm"
                                onClick={() => toggleResponsesExpanded(form.id)}
                                aria-expanded={expandedResponseFormIds.has(form.id)}
                                aria-controls={`public-responses-${form.id}`}
                              >
                                {expandedResponseFormIds.has(form.id) ? (
                                  <>
                                    <ChevronUp className="h-4 w-4 mr-1" />
                                    Hide responses
                                  </>
                                ) : (
                                  <>
                                    <ChevronDown className="h-4 w-4 mr-1" />
                                    Show responses ({form.responseCount})
                                  </>
                                )}
                              </Button>
                            </div>
                            {expandedResponseFormIds.has(form.id) && (
                              <div id={`public-responses-${form.id}`}>
                                <PublicFormResponsesSection
                                  eventId={id}
                                  form={form}
                                  embedded
                                />
                              </div>
                            )}
                          </>
                        )}
                      </CardContent>
                    </Card>
                  );
                })}
              </div>
            ) : (
              <div className="text-center py-12 text-gray-500">
                <p>No signup forms available for this event yet.</p>
              </div>
            )}
          </CollapsibleSection>
        </div>
        )}

        {/* Phase 6A.146 — Public form responses are rendered INLINE inside
            each form card within the #signup-forms section above. The earlier
            separate #public-form-responses section duplicated the form title
            and was removed on 2026-05-15 after UAT feedback. */}
      </div>

      <Footer />

      {/* Phase 6A.148 — Request Refund dialog (attendee path).
          For MVP, the only refundable line offered is the ticket payment itself —
          the attendee's add-ons / collections / sponsorships are visible in the
          attendee dashboard but are out of scope for this dialog. The organizer
          can still approve a partial amount in their approval dialog. */}
      {id && isPaidRegistration && registrationDetails && (
        <RequestRefundDialog
          open={showRequestRefundDialog}
          eventId={id}
          availableLines={(() => {
            // MVP: a single Ticket line valued at the registration's total. The
            // backend RegistrationPayment.Id is the ReferenceId, but the FE doesn't
            // currently surface it. We pass the registration ID as a stand-in; the
            // backend validates the actual payment lookup at request-creation time.
            // (Per-line add-on/collection/sponsor selection is a Phase B enhancement.)
            const total = registrationDetails.totalPriceAmount;
            const currency = (registrationDetails.totalPriceCurrency ?? 'USD') as
              import('@/infrastructure/api/types/refund-request.types').RefundCurrency;
            if (!total || total <= 0) return [];
            return [
              {
                type: 'Ticket' as const,
                referenceId: registrationDetails.id ?? id,
                requestedAmount: total,
                currency,
              },
            ];
          })()}
          onClose={() => setShowRequestRefundDialog(false)}
          onSubmitted={async () => {
            if (!id) return;
            const refreshed = await eventsRepository.getMyRefundRequest(id);
            setMyRefundRequest(refreshed);
          }}
        />
      )}

      {/* Phase 6A.144: Auth Encouragement Modal — fires only for anonymous
          users on PAID events (gated by shouldShowAuthNudge in the render
          path above). "Continue as Guest" sets a per-event sessionStorage
          flag so the prompt doesn't re-appear on subsequent clicks within
          the same session. */}
      {event && (
        <AuthEncouragementModal
          open={showAuthNudge}
          onOpenChange={setShowAuthNudge}
          context="event-paid"
          redirectTo={`/events/${event.id}?intent=register`}
          onContinueAsGuest={() => {
            try {
              if (typeof window !== 'undefined') {
                window.sessionStorage.setItem(guestAckStorageKey(event.id), '1');
              }
            } catch (err) {
              console.warn('[EventDetail 6A.144] sessionStorage write failed for guest-ack', err);
            }
            setGuestModeAcknowledged(true);
            setShowAuthNudge(false);
          }}
        />
      )}

      {/* Phase 6A.14: Edit Registration Modal */}
      {/* Issue #51: Pass maxAttendeesPerRegistration to EditRegistrationModal */}
      {/* Add-Only Attendees: Pass onAddAttendeesClick for paid registrations */}
      <EditRegistrationModal
        open={showEditModal}
        onOpenChange={setShowEditModal}
        registration={registrationDetails || null}
        eventId={id}
        eventTitle={event?.title}
        isFreeEvent={event?.isFree ?? true}
        spotsLeft={spotsLeft}
        maxAttendeesPerRegistration={event?.maxAttendeesPerRegistration}
        onSave={handleEditRegistration}
        isSubmitting={isUpdatingRegistration}
        onAddAttendeesClick={() => setShowAddAttendeesModal(true)}
      />

      {/* Add-Only Attendees: dispatches by registration mode (Phase 7F-D adds Mode-B path).
          Mode A → existing per-attendee modal; Mode B (1/2/3/4) → head-count delta modal. */}
      {registrationDetails && isModeB && (
        <AddHeadCountModal
          open={showAddAttendeesModal}
          onOpenChange={setShowAddAttendeesModal}
          registrationId={registrationDetails.id}
          mode={registrationMode}
          maxAttendeesPerRegistration={event?.maxAttendeesPerRegistration ?? 10}
          currentAttendeeCount={registrationDetails.attendees?.length || registrationDetails.quantity}
          onSuccess={() => {
            window.location.reload();
          }}
        />
      )}
      {registrationDetails && !isModeB && (
        <AddAttendeesModal
          open={showAddAttendeesModal}
          onOpenChange={setShowAddAttendeesModal}
          registrationId={registrationDetails.id}
          eventId={id}
          eventTitle={event?.title || ''}
          currentAttendeeCount={registrationDetails.attendees?.length || registrationDetails.quantity}
          maxAttendeesPerRegistration={event?.maxAttendeesPerRegistration ?? 10}
          onSuccess={() => {
            // Refresh registration details after successful addition
            window.location.reload();
          }}
        />
      )}

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
            await eventsRepository.cancelRsvp(id, {});
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

      {/* Phase 6A.109: Form response deletion confirmation */}
      <ConfirmDialog
        open={showFormDeleteConfirm}
        onOpenChange={setShowFormDeleteConfirm}
        title="Cancel Form Response"
        description="Are you sure you want to cancel your form response? This action cannot be undone. You will receive a confirmation email."
        confirmLabel="Yes, Cancel Response"
        cancelLabel="Keep Response"
        variant="danger"
        onConfirm={handleDeleteFormResponse}
      />
    </div>
  );
}
