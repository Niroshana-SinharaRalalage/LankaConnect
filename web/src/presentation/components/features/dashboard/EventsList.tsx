'use client';

import * as React from 'react';
import { Calendar, MapPin, Users, Eye, Edit, Upload, Ban, Trash2 } from 'lucide-react';
import { EventDto, EventStatus, EventCategory } from '@/infrastructure/api/types/events.types';
import { RegistrationBadge } from '../events/RegistrationBadge';
import { useEventCategories, useEventStatuses } from '@/infrastructure/api/hooks/useReferenceData';
import { getNameFromIntValue } from '@/infrastructure/api/utils/enum-mappers';
import { Button } from '@/presentation/components/ui/Button';

export interface EventsListProps {
  events: EventDto[];
  isLoading?: boolean;
  emptyMessage?: string;
  onEventClick?: (eventId: string) => void;
  onCancelClick?: (eventId: string) => Promise<void>;
  /** Phase 6A.46: Set of event IDs for which user is registered (for bulk registration badge display) */
  registeredEventIds?: Set<string>;
  /** Phase 6A.59: Management mode shows action buttons for event organizers */
  showManagementActions?: boolean;
  onEditEvent?: (eventId: string) => void;
  onPublishEvent?: (eventId: string) => Promise<void>;
  onCancelEvent?: (eventId: string) => Promise<void>;
  onDeleteEvent?: (eventId: string) => Promise<void>;
}

/**
 * EventsList Component
 * Displays a list of events with status, category, and registration information
 * Phase: Epic 1 Dashboard Enhancements
 */
export function EventsList({
  events,
  isLoading = false,
  emptyMessage = 'No events to display',
  onEventClick,
  onCancelClick,
  registeredEventIds,
  showManagementActions = false,
  onEditEvent,
  onPublishEvent,
  onCancelEvent,
  onDeleteEvent,
}: EventsListProps) {
  // Phase 6A.47: Fetch reference data for labels
  const { data: categories } = useEventCategories();
  const { data: statuses } = useEventStatuses();

  const [cancellingEventId, setCancellingEventId] = React.useState<string | null>(null);
  // Phase 6A.59: Loading states for management actions
  const [publishingEventId, setPublishingEventId] = React.useState<string | null>(null);
  const [cancellingMgmtEventId, setCancellingMgmtEventId] = React.useState<string | null>(null);
  const [deletingEventId, setDeletingEventId] = React.useState<string | null>(null);

  const formatDate = (dateString: string): string => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: '2-digit',
    });
  };

  /**
   * Phase 6A.46: Get badge color based on event lifecycle label
   * LankaConnect theme colors: Orange #FF7900, Rose #8B1538, Emerald #047857
   */
  const getStatusBadgeColor = (label: string): string => {
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
  };

  // Phase 6A.47: Use reference data for status and category labels
  const getStatusLabel = (status: EventStatus): string => {
    return getNameFromIntValue(statuses, status) || `Unknown (${status})`;
  };

  const getCategoryLabel = (category: EventCategory): string => {
    return getNameFromIntValue(categories, category) || 'Other';
  };

  const handleCancelClick = async (eventId: string, e: React.MouseEvent) => {
    e.stopPropagation(); // Prevent triggering onEventClick

    if (!onCancelClick || cancellingEventId) return;

    setCancellingEventId(eventId);
    try {
      await onCancelClick(eventId);
    } finally {
      setCancellingEventId(null);
    }
  };

  // Phase 6A.59: Management action handlers
  const handlePublishClick = async (eventId: string, e: React.MouseEvent) => {
    e.stopPropagation();
    if (!onPublishEvent || publishingEventId) return;

    setPublishingEventId(eventId);
    try {
      await onPublishEvent(eventId);
    } finally {
      setPublishingEventId(null);
    }
  };

  const handleCancelEventClick = async (eventId: string, e: React.MouseEvent) => {
    e.stopPropagation();
    if (!onCancelEvent || cancellingMgmtEventId) return;

    setCancellingMgmtEventId(eventId);
    try {
      await onCancelEvent(eventId);
    } finally {
      setCancellingMgmtEventId(null);
    }
  };

  const handleDeleteClick = async (eventId: string, e: React.MouseEvent) => {
    e.stopPropagation();
    if (!onDeleteEvent || deletingEventId) return;

    setDeletingEventId(eventId);
    try {
      await onDeleteEvent(eventId);
    } finally {
      setDeletingEventId(null);
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="text-center">
          <div
            className="inline-block w-8 h-8 border-4 border-gray-300 border-t-[#FF7900] rounded-full animate-spin"
            role="status"
          ></div>
          <p className="mt-2 text-gray-600">Loading events...</p>
        </div>
      </div>
    );
  }

  if (events.length === 0) {
    return (
      <div className="text-center py-12">
        <Calendar className="w-16 h-16 mx-auto mb-4 text-gray-400" />
        <p className="text-gray-600 text-lg">{emptyMessage}</p>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {events.map((event) => (
        <div
          key={event.id}
          onClick={() => onEventClick?.(event.id)}
          className={`bg-white rounded-lg shadow-sm border border-gray-200 p-4 transition-all hover:shadow-md ${
            onEventClick ? 'cursor-pointer' : ''
          }`}
        >
          <div className="flex items-start gap-4">
            {/* Phase 6A.67: Event Image Thumbnail (128x128px - increased from 96px for better visibility) */}
            <div className="relative w-32 h-32 flex-shrink-0 rounded-lg overflow-hidden bg-gradient-to-br from-orange-500 to-rose-500">
              {event.images && event.images.length > 0 ? (
                <img
                  src={(event.images.find(img => img.isPrimary) || event.images[0]).imageUrl}
                  alt={event.title}
                  className="w-full h-full object-cover"
                  loading="lazy"
                />
              ) : (
                <div className="w-full h-full flex items-center justify-center text-4xl text-white">
                  🎉
                </div>
              )}
            </div>

            {/* Event Details */}
            <div className="flex-1 min-w-0">
              {/* Event Header */}
              <div className="flex items-start justify-between mb-2">
                <h3 className="text-lg font-semibold text-[#8B1538] flex-1 line-clamp-2">{event.title}</h3>
                <div className="flex gap-2 ml-4">
                  {/* Phase 6A.46: Display Label (computed lifecycle label from backend) */}
                  <span
                    className="px-2 py-1 rounded-full text-xs font-semibold text-white whitespace-nowrap"
                    style={{ backgroundColor: getStatusBadgeColor(event.displayLabel) }}
                  >
                    {event.displayLabel}
                  </span>

                  {/* Phase 6A.46: Registration Badge */}
                  <RegistrationBadge
                    isRegistered={registeredEventIds?.has(event.id) ?? false}
                    compact={false}
                  />
                </div>
              </div>

              {/* Event Info */}
              <div className="space-y-2">
            {/* Date */}
            <div className="flex items-center text-sm text-gray-600">
              <Calendar className="w-4 h-4 mr-2 text-[#FF7900]" />
              <span>
                {formatDate(event.startDate)}
                {event.endDate && event.endDate !== event.startDate && (
                  <> - {formatDate(event.endDate)}</>
                )}
              </span>
            </div>

            {/* Location */}
            {event.city && event.state && (
              <div className="flex items-center text-sm text-gray-600">
                <MapPin className="w-4 h-4 mr-2 text-[#FF7900]" />
                <span>
                  {event.city}, {event.state}
                </span>
              </div>
            )}

            {/* Capacity */}
            <div className="flex items-center text-sm text-gray-600">
              <Users className="w-4 h-4 mr-2 text-[#FF7900]" />
              <span>
                {event.currentRegistrations} / {event.capacity} registered
              </span>
            </div>
          </div>

          {/* Footer */}
          <div className="flex items-center justify-between mt-3 pt-3 border-t border-gray-200">
            <div className="flex gap-2">
              {/* Category Badge */}
              <span className="px-2 py-1 bg-gray-100 text-gray-700 rounded text-xs font-medium">
                {getCategoryLabel(event.category)}
              </span>

              {/* Free Badge */}
              {event.isFree && (
                <span className="px-2 py-1 bg-green-100 text-green-700 rounded text-xs font-medium">
                  Free
                </span>
              )}

              {/* Phase 6A.59: Cancelled Badge - Show prominently on all cancelled events */}
              {(event.status as any) === 'Cancelled' && (
                <span className="px-2 py-1 bg-red-100 text-red-700 rounded text-xs font-bold">
                  CANCELLED
                </span>
              )}

              {/* Price - Session 23: Dual pricing, Session 33: Group pricing support */}
              {!event.isFree && (
                <span className="px-2 py-1 bg-[#FFE8CC] text-[#8B1538] rounded text-xs font-medium">
                  {event.hasGroupPricing && event.groupPricingTiers && event.groupPricingTiers.length > 0 ? (
                    // Session 33: Display price range for group pricing (no "/person")
                    (() => {
                      const prices = event.groupPricingTiers.map(t => t.pricePerPerson);
                      const minPrice = Math.min(...prices);
                      const maxPrice = Math.max(...prices);
                      return minPrice === maxPrice
                        ? `$${minPrice}`
                        : `$${minPrice}-$${maxPrice}`;
                    })()
                  ) : event.hasDualPricing ? (
                    `Adult: $${event.adultPriceAmount} | Child: $${event.childPriceAmount}`
                  ) : event.ticketPriceAmount != null ? (
                    `$${event.ticketPriceAmount}`
                  ) : (
                    'Paid Event'
                  )}
                </span>
              )}
            </div>

            {/* Phase 6A.59: Management Actions for Event Organizers */}
            {showManagementActions ? (
              <div className="flex flex-wrap gap-2">
                {/* View/Manage Event - Always visible */}
                <Button
                  onClick={(e) => {
                    e.stopPropagation();
                    onEventClick?.(event.id);
                  }}
                  size="sm"
                  variant="outline"
                  className="text-xs"
                >
                  <Eye className="w-3 h-3 mr-1" />
                  Manage
                </Button>

                {/* Edit Event - Always visible */}
                {onEditEvent && (
                  <Button
                    onClick={(e) => {
                      e.stopPropagation();
                      onEditEvent(event.id);
                    }}
                    size="sm"
                    variant="outline"
                    className="text-xs"
                  >
                    <Edit className="w-3 h-3 mr-1" />
                    Edit
                  </Button>
                )}

                {/* Publish Event - Only for unpublished events (Draft, Archived) */}
                {/* Phase 6A.59: API returns status as string, not enum number */}
                {((event.status as any) === 'Draft' || (event.status as any) === 'Archived') && onPublishEvent && (
                  <Button
                    onClick={(e) => handlePublishClick(event.id, e)}
                    disabled={publishingEventId === event.id}
                    size="sm"
                    className="text-xs bg-[#FF7900] hover:bg-[#E66D00] text-white"
                  >
                    <Upload className="w-3 h-3 mr-1" />
                    {publishingEventId === event.id ? 'Publishing...' : 'Publish'}
                  </Button>
                )}

                {/* Cancel Event - For Draft, Archived, or Published events */}
                {/* Phase 6A.59: API returns status as string, not enum number */}
                {((event.status as any) === 'Draft' ||
                  (event.status as any) === 'Archived' ||
                  (event.status as any) === 'Published' ||
                  (event.status as any) === 'Active') && onCancelEvent && (
                  <Button
                    onClick={(e) => handleCancelEventClick(event.id, e)}
                    disabled={cancellingMgmtEventId === event.id}
                    size="sm"
                    className="text-xs bg-[#F59E0B] hover:bg-[#D97706] text-white"
                  >
                    <Ban className="w-3 h-3 mr-1" />
                    {cancellingMgmtEventId === event.id ? 'Cancelling...' : 'Cancel'}
                  </Button>
                )}

                {/* Delete Event - For Draft, Archived, or Cancelled events with 0 registrations */}
                {/* Phase 6A.59: API returns status as string, not enum number */}
                {((event.status as any) === 'Draft' ||
                  (event.status as any) === 'Archived' ||
                  (event.status as any) === 'Cancelled') &&
                  event.currentRegistrations === 0 &&
                  onDeleteEvent && (
                    <Button
                      onClick={(e) => handleDeleteClick(event.id, e)}
                      disabled={deletingEventId === event.id}
                      size="sm"
                      variant="destructive"
                      className="text-xs"
                    >
                      <Trash2 className="w-3 h-3 mr-1" />
                      {deletingEventId === event.id ? 'Deleting...' : 'Delete'}
                    </Button>
                  )}
              </div>
            ) : (
              /* Cancel Registration Button - Session 30 */
              onCancelClick && (
                <button
                  onClick={(e) => handleCancelClick(event.id, e)}
                  disabled={cancellingEventId === event.id}
                  className="px-3 py-1 text-sm font-medium text-red-700 bg-red-50 border border-red-200 rounded hover:bg-red-100 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                  aria-label="Cancel registration"
                >
                  {cancellingEventId === event.id ? 'Cancelling...' : 'Cancel Registration'}
                </button>
              )
            )}
          </div>
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}
