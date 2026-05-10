'use client';

import { use, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import {
  ArrowLeft,
  Edit,
  FileText,
  ListChecks,
  Ban,
  Trash2,
  Mail,
  Camera,
  Banknote,
  Users,
} from 'lucide-react';
import { LankaEventsHeader } from '@/presentation/components/layout/LankaEventsHeader';
import Footer from '@/presentation/components/layout/Footer';
import { Button } from '@/presentation/components/ui/Button';
import { Badge } from '@/presentation/components/ui/Badge';
import { TabPanel, Tab } from '@/presentation/components/ui/TabPanel';
import { useEventById } from '@/presentation/hooks/useEvents';
import { useEventSignUps } from '@/presentation/hooks/useEventSignUps';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { EventStatus } from '@/infrastructure/api/types/events.types';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';

// Phase 6A.45: Import new components
import { EventDetailsTab } from '@/presentation/components/features/events/EventDetailsTab';
import { SignUpListsTab } from '@/presentation/components/features/events/SignUpListsTab';
import { VolunteerListsTab } from '@/presentation/components/features/events/VolunteerListsTab';
import { UnpublishEventModal } from '@/presentation/components/features/events/UnpublishEventModal';
import { DeleteEventModal } from '@/presentation/components/features/events/DeleteEventModal';

// Phase 6A.74: Import EventNewslettersTab
import { EventNewslettersTab } from '@/presentation/components/features/newsletters/EventNewslettersTab';

// Consolidated Attendees & Finance tab (replaces 5 separate tabs)
import { AttendeesAndFinanceTab } from '@/presentation/components/features/events/AttendeesAndFinanceTab';

// Custom Forms Feature - Phase 6A: Import EventFormsTab
import { EventFormsTab } from '@/presentation/components/features/events/EventFormsTab';

// Photo Album Feature: Inline management tab
import { PhotoAlbumManagementTab } from '@/presentation/components/features/events/PhotoAlbumManagementTab';

/**
 * Event Management Page - Phase 6A.45 Refactored + Phase 6A.59 Cancel/Delete + Phase 6A.74 Communications
 * Organizer-only page with tabbed interface for managing events
 *
 * Tabs:
 * 1. Event Details - Event info, statistics, publish/edit buttons, media, badges
 * 2. Attendees - Phase 6A.45: Registration list with export functionality
 * 3. Signup Lists - Existing signup list management
 * 4. Communications - Phase 6A.74: Event-specific newsletters
 *
 * Phase 6A.59: Added Cancel and Delete event buttons
 * Phase 6A.74: Added Communications tab for event-specific newsletters
 */
export default function EventManagePage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const router = useRouter();
  const searchParams = useSearchParams();
  const tabFromUrl = searchParams.get('tab');
  const { user } = useAuthStore();
  const [isPublishing, setIsPublishing] = useState(false);
  const [isUnpublishing, setIsUnpublishing] = useState(false);
  const [isCancelling, setIsCancelling] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [showUnpublishModal, setShowUnpublishModal] = useState(false);
  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [showCancelModal, setShowCancelModal] = useState(false);
  const [cancellationReason, setCancellationReason] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  // Fetch event details
  const { data: event, isLoading, error: fetchError, refetch } = useEventById(id);

  // Fetch sign-up lists (needed for tab content)
  const { data: signUpLists } = useEventSignUps(id);

  // Status labels
  const statusLabels: Record<EventStatus, string> = {
    [EventStatus.Draft]: 'Draft',
    [EventStatus.Published]: 'Published',
    [EventStatus.Active]: 'Active',
    [EventStatus.Postponed]: 'Postponed',
    [EventStatus.Cancelled]: 'Cancelled',
    [EventStatus.Completed]: 'Completed',
    [EventStatus.Archived]: 'Archived',
    [EventStatus.UnderReview]: 'Under Review',
    // Phase 8YB.5 (D1=A): TBD events created without dates land in Planning
    // and can publish directly via the Publish button below.
    // Phase 8YB.6 (DP3=A): simplified label from "Planning (Date TBD)" — the
    // listing card already surfaces "Date TBD" in the date field below, so the
    // suffix on the manage badge was redundant visual noise.
    [EventStatus.Planning]: 'Planning',
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
      await refetch();
    } catch (err) {
      console.error('Failed to publish event:', err);
      setError(err instanceof Error ? err.message : 'Failed to publish event. Please try again.');
      setIsPublishing(false);
    }
  };

  // Handle Unpublish Event
  const handleUnpublishEvent = async () => {
    if (!event || event.isCurrentUserOrganizer !== true) {
      return;
    }

    try {
      setIsUnpublishing(true);
      setError(null);
      await eventsRepository.unpublishEvent(id);
      setShowUnpublishModal(false);
      setIsUnpublishing(false);
      await refetch();
    } catch (err) {
      console.error('Failed to unpublish event:', err);
      setError(err instanceof Error ? err.message : 'Failed to unpublish event. Please try again.');
      setIsUnpublishing(false);
    }
  };

  // Phase 6A.59: Handle Cancel Event
  const handleCancelEvent = async () => {
    if (!event || event.isCurrentUserOrganizer !== true) {
      return;
    }

    if (!cancellationReason.trim()) {
      setError('Cancellation reason is required');
      return;
    }

    if (cancellationReason.trim().length < 10) {
      setError('Cancellation reason must be at least 10 characters');
      return;
    }

    try {
      setIsCancelling(true);
      setError(null);
      await eventsRepository.cancelEvent(id, cancellationReason.trim());
      setIsCancelling(false);
      setShowCancelModal(false);
      setCancellationReason('');
      await refetch();
    } catch (err) {
      console.error('Failed to cancel event:', err);
      setError(err instanceof Error ? err.message : 'Failed to cancel event. Please try again.');
      setIsCancelling(false);
    }
  };

  // Phase 6A.59: Handle Delete Event
  const handleDeleteEvent = async () => {
    if (!event || event.isCurrentUserOrganizer !== true) {
      return;
    }

    try {
      setIsDeleting(true);
      setDeleteError(null);
      await eventsRepository.deleteEvent(id);
      // On success, close modal and redirect to dashboard
      setShowDeleteModal(false);
      router.push('/dashboard');
    } catch (err) {
      console.error('Failed to delete event:', err);
      const errorMessage = err instanceof Error ? err.message : 'Failed to delete event. Please try again.';
      setDeleteError(errorMessage);
      setIsDeleting(false);
    }
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
        <LankaEventsHeader />
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
          <div className="animate-pulse">
            <div className="h-8 bg-neutral-200 rounded w-3/4 mb-4"></div>
            <div className="h-4 bg-neutral-200 rounded w-1/2 mb-8"></div>
            <div className="h-64 bg-neutral-200 rounded"></div>
          </div>
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
          <div className="p-12 text-center bg-white rounded-lg shadow">
            <h2 className="text-2xl font-bold text-red-600 mb-4">Event Not Found</h2>
            <p className="text-neutral-600 mb-6">
              The event you're looking for doesn't exist or you don't have permission to manage it.
            </p>
            <Button onClick={() => router.push('/dashboard')}>Go to Dashboard</Button>
          </div>
        </div>
        <Footer />
      </div>
    );
  }

  // Check if user is the organizer
  const isOrganizer = event.isCurrentUserOrganizer === true;

  if (!isOrganizer) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
        <LankaEventsHeader />
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
          <div className="p-12 text-center bg-white rounded-lg shadow">
            <h2 className="text-2xl font-bold text-red-600 mb-4">Access Denied</h2>
            <p className="text-neutral-600 mb-6">You don't have permission to manage this event.</p>
            <Button onClick={() => router.push(`/events/${id}`)}>View Event</Button>
          </div>
        </div>
        <Footer />
      </div>
    );
  }

  // Check event status for buttons
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const isDraft = (event.status as any) === EventStatus.Draft ||
                   (event.status as any) === 'Draft' ||
                   String(event.status).toLowerCase() === 'draft';

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const isPublished = (event.status as any) === EventStatus.Published ||
                       (event.status as any) === 'Published' ||
                       String(event.status).toLowerCase() === 'published';

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const isCancelled = (event.status as any) === EventStatus.Cancelled ||
                       (event.status as any) === 'Cancelled' ||
                       String(event.status).toLowerCase() === 'cancelled';

  // Phase 8YB.5 (D1=A): Planning events (TBD-dates) can publish directly.
  // Mirrors the defensive triple-compare pattern used for the other status flags
  // so we tolerate string-or-number variants from any cached/stale payloads.
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const isPlanning = (event.status as any) === EventStatus.Planning ||
                     (event.status as any) === 'Planning' ||
                     String(event.status).toLowerCase() === 'planning';

  // Phase 6A.59 + Phase 8YB.5: Button visibility logic.
  // Planning events are pre-publication too — same Cancel/Delete affordances as Draft.
  const canCancel = (isDraft || isPlanning || isPublished) && !isCancelled;
  const canDelete = (isDraft || isPlanning || isCancelled) && event.currentRegistrations === 0;

  // Backward compatibility: map old tab IDs to the consolidated tab
  const resolveTab = (tab: string | null): string => {
    if (!tab) return 'details';
    const legacyMap: Record<string, string> = {
      attendees: 'attendees-finance',
      donations: 'attendees-finance',
      collections: 'attendees-finance',
      sponsors: 'attendees-finance',
      addons: 'attendees-finance',
    };
    return legacyMap[tab] ?? tab;
  };

  // Define tabs
  const tabs: Tab[] = [
    {
      id: 'details',
      label: 'Event Details',
      icon: FileText,
      content: (
        <EventDetailsTab
          event={event}
          onRefetch={refetch}
          isDraft={isDraft}
          isPublished={isPublished}
          isPublishing={isPublishing}
          isUnpublishing={isUnpublishing}
          onPublish={handlePublishEvent}
          onUnpublish={handleUnpublishEvent}
        />
      ),
    },
    {
      id: 'attendees-finance',
      label: 'Attendees & Finance',
      icon: Banknote,
      content: <AttendeesAndFinanceTab eventId={id} event={event} />,
    },
    {
      id: 'signups',
      label: 'Signup Lists',
      icon: ListChecks,
      content: <SignUpListsTab eventId={id} signUpLists={signUpLists || []} />,
    },
    {
      id: 'volunteers',
      label: 'Volunteers',
      icon: Users,
      content: <VolunteerListsTab eventId={id} signUpLists={signUpLists || []} />,
    },
    {
      id: 'forms',
      label: 'Signup Forms',
      icon: FileText,
      content: <EventFormsTab eventId={id} />,
    },
    {
      id: 'communications',
      label: 'Communications',
      icon: Mail,
      content: <EventNewslettersTab eventId={id} eventTitle={event.title} />,
    },
    // Photo Album tab — visible for Published/Active/Completed/Archived events
    ...(['Published', 'Active', 'Completed', 'Archived'].includes(String(event.status)) ||
        [EventStatus.Published, EventStatus.Active, EventStatus.Completed, EventStatus.Archived].includes(event.status as EventStatus)
      ? [{
          id: 'album',
          label: 'Photo Album',
          icon: Camera,
          content: <PhotoAlbumManagementTab eventId={id} />,
        }]
      : []),
  ];

  return (
    <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
      <LankaEventsHeader />

      {/* Page Header */}
      <div className="bg-gradient-to-r from-orange-600 via-rose-800 to-emerald-800 py-8">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-3xl font-bold text-white mb-2">Manage Event</h1>
              <p className="text-white/90">{event.title}</p>
            </div>
            <Badge
              className="text-sm px-4 py-2 font-bold"
              style={{
                backgroundColor:
                  (event.status as any) === 'Cancelled' || event.status === EventStatus.Cancelled
                    ? '#DC2626' // Red for cancelled
                    : event.status === EventStatus.Published
                    ? '#10B981' // Green for published
                    : '#6B7280', // Gray for others
                color: '#FFFFFF',
              }}
            >
              {(event.status as any) === 'Cancelled' || event.status === EventStatus.Cancelled
                ? 'CANCELLED'
                : statusLabels[event.status]}
            </Badge>
          </div>
        </div>
      </div>

      {/* Navigation Buttons - Phase 6A.X Issue #43: Added relative z-20 to fix z-index stacking conflict with sticky table headers */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6 relative z-20">
        <div className="flex items-center justify-between gap-4 flex-wrap">
          <Button
            variant="outline"
            onClick={() => router.push('/dashboard')}
            className="flex items-center gap-2 text-neutral-900 border-neutral-300"
          >
            <ArrowLeft className="h-4 w-4" />
            Back to Dashboard
          </Button>

          <div className="flex items-center gap-3 flex-wrap">
            {/* Publish Button — Phase 8YB.5 (D1=A): Planning events (TBD-dates) can
                publish directly alongside Draft events. The domain Publish() method
                accepts both Draft and Planning per Phase 8YA.1. Listing/detail pages
                render "Date TBD" + "Coming Soon" affordances; registration is still
                domain-blocked while StartDate is null (Phase 8YA.1 Q2=A). */}
            {(isDraft || isPlanning) && (
              <Button
                onClick={handlePublishEvent}
                disabled={isPublishing}
                className="flex items-center gap-2 text-white"
                style={{ background: '#10B981', color: 'white' }}
                data-testid="publish-event-button"
              >
                {isPublishing ? 'Publishing...' : 'Publish Event'}
              </Button>
            )}

            {/* Unpublish Button - Show for Published events */}
            {isPublished && (
              <Button
                onClick={() => setShowUnpublishModal(true)}
                disabled={isUnpublishing}
                className="flex items-center gap-2 text-white"
                style={{ background: '#EF4444', color: 'white' }}
              >
                {isUnpublishing ? 'Unpublishing...' : 'Unpublish Event'}
              </Button>
            )}

            {/* Phase 6A.59: Cancel Event Button - Show for Published or Draft (not Cancelled) */}
            {canCancel && (
              <Button
                onClick={() => setShowCancelModal(true)}
                disabled={isCancelling}
                className="flex items-center gap-2 text-white"
                style={{ background: '#F59E0B', color: 'white' }}
              >
                <Ban className="h-4 w-4" />
                {isCancelling ? 'Cancelling...' : 'Cancel Event'}
              </Button>
            )}

            {/* Phase 6A.59: Delete Event Button - Show ONLY for Draft/Cancelled with 0 registrations */}
            {canDelete && (
              <Button
                onClick={() => setShowDeleteModal(true)}
                disabled={isDeleting}
                variant="destructive"
                className="flex items-center gap-2"
              >
                <Trash2 className="h-4 w-4" />
                {isDeleting ? 'Deleting...' : 'Delete Event'}
              </Button>
            )}

            {/* Edit Event Button */}
            <Button
              onClick={() => router.push(`/events/${id}/edit`)}
              className="flex items-center gap-2 text-white"
              style={{ background: '#FF7900', color: 'white' }}
            >
              <Edit className="h-4 w-4" />
              Edit Event
            </Button>
          </div>
        </div>
      </div>

      {/* Error Message */}
      {error && (
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 mb-6">
          <div className="p-4 bg-red-50 border border-red-200 rounded-lg">
            <p className="text-sm text-red-600">{error}</p>
          </div>
        </div>
      )}

      {/* Phase 6A.59: Cancel Event Modal */}
      {showCancelModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-lg shadow-xl max-w-md w-full p-6">
            <div className="flex items-center gap-3 mb-4">
              <div className="p-2 bg-amber-100 rounded-full">
                <Ban className="h-6 w-6 text-amber-600" />
              </div>
              <h2 className="text-xl font-bold text-neutral-900">Cancel Event</h2>
            </div>

            <div className="mb-4">
              <p className="text-sm text-neutral-600 mb-2">
                This will cancel the event and notify all {event.currentRegistrations} registered attendees via email.
              </p>
              <p className="text-sm text-amber-600 font-medium">
                The event will be marked as Cancelled but will remain in the database.
              </p>
            </div>

            <div className="mb-4">
              <label className="block text-sm font-medium text-neutral-700 mb-2">
                Cancellation Reason <span className="text-red-500">*</span>
              </label>
              <textarea
                value={cancellationReason}
                onChange={(e) => {
                  setCancellationReason(e.target.value);
                  setError(null);
                }}
                placeholder="Please provide a reason for cancelling this event (min. 10 characters)..."
                className="w-full px-3 py-2 border border-neutral-300 rounded-md focus:outline-none focus:ring-2 focus:ring-amber-500 resize-none"
                rows={4}
                maxLength={500}
              />
              <p className="text-xs text-neutral-500 mt-1">
                {cancellationReason.length}/500 characters {cancellationReason.length < 10 && `(min. 10 required)`}
              </p>
            </div>

            <div className="flex items-center gap-3">
              <Button
                onClick={() => {
                  setShowCancelModal(false);
                  setCancellationReason('');
                  setError(null);
                }}
                variant="outline"
                className="flex-1"
                disabled={isCancelling}
              >
                Go Back
              </Button>
              <Button
                onClick={handleCancelEvent}
                disabled={isCancelling || cancellationReason.trim().length < 10}
                className="flex-1 text-white"
                style={{ background: '#F59E0B', color: 'white' }}
              >
                {isCancelling ? 'Cancelling...' : 'Confirm Cancellation'}
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Main Content - Tabbed Interface */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 pb-12">
        <TabPanel tabs={tabs} defaultTab={resolveTab(tabFromUrl)} />
      </div>

      {/* Unpublish Event Modal */}
      <UnpublishEventModal
        open={showUnpublishModal}
        onOpenChange={setShowUnpublishModal}
        onConfirm={handleUnpublishEvent}
        isUnpublishing={isUnpublishing}
        eventTitle={event?.title || ''}
      />

      {/* Delete Event Modal */}
      <DeleteEventModal
        open={showDeleteModal}
        onOpenChange={(open) => {
          setShowDeleteModal(open);
          if (!open) setDeleteError(null); // Clear error when closing
        }}
        onConfirm={handleDeleteEvent}
        isDeleting={isDeleting}
        eventTitle={event?.title || ''}
        error={deleteError}
      />

      <Footer />
    </div>
  );
}
