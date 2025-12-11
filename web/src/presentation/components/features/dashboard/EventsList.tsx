'use client';

import * as React from 'react';
import { Calendar, MapPin, Users } from 'lucide-react';
import { EventDto, EventStatus, EventCategory } from '@/infrastructure/api/types/events.types';

export interface EventsListProps {
  events: EventDto[];
  isLoading?: boolean;
  emptyMessage?: string;
  onEventClick?: (eventId: string) => void;
  onCancelClick?: (eventId: string) => Promise<void>;
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
}: EventsListProps) {
  const [cancellingEventId, setCancellingEventId] = React.useState<string | null>(null);
  const formatDate = (dateString: string): string => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: '2-digit',
    });
  };

  const getStatusColor = (status: EventStatus): string => {
    switch (status) {
      case EventStatus.Published:
        return '#10B981'; // Green
      case EventStatus.Active:
        return '#FF7900'; // Saffron
      case EventStatus.Draft:
        return '#6B7280'; // Gray
      case EventStatus.Postponed:
        return '#F59E0B'; // Amber
      case EventStatus.Cancelled:
        return '#EF4444'; // Red
      case EventStatus.Completed:
        return '#3B82F6'; // Blue
      case EventStatus.UnderReview:
        return '#8B5CF6'; // Purple
      default:
        return '#6B7280';
    }
  };

  const getStatusLabel = (status: EventStatus): string => {
    // Debug logging to identify the issue
    console.log('Event status value:', status, 'Type:', typeof status);

    switch (status) {
      case EventStatus.Published:
        return 'Published';
      case EventStatus.Active:
        return 'Active';
      case EventStatus.Draft:
        return 'Draft';
      case EventStatus.Postponed:
        return 'Postponed';
      case EventStatus.Cancelled:
        return 'Cancelled';
      case EventStatus.Completed:
        return 'Completed';
      case EventStatus.UnderReview:
        return 'Under Review';
      case EventStatus.Archived:
        return 'Archived';
      default:
        // Additional debug info
        console.warn(`Unknown status value: ${status}, Type: ${typeof status}, EventStatus.Draft=${EventStatus.Draft}`);
        return `Unknown (${status})`;
    }
  };

  const getCategoryLabel = (category: EventCategory): string => {
    switch (category) {
      case EventCategory.Religious:
        return 'Religious';
      case EventCategory.Cultural:
        return 'Cultural';
      case EventCategory.Community:
        return 'Community';
      case EventCategory.Educational:
        return 'Educational';
      case EventCategory.Social:
        return 'Social';
      case EventCategory.Business:
        return 'Business';
      case EventCategory.Charity:
        return 'Charity';
      case EventCategory.Entertainment:
        return 'Entertainment';
      default:
        return 'Other';
    }
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
          {/* Event Header */}
          <div className="flex items-start justify-between mb-2">
            <h3 className="text-lg font-semibold text-[#8B1538] flex-1">{event.title}</h3>
            <div className="flex gap-2 ml-4">
              {/* Status Badge */}
              <span
                className="px-2 py-1 rounded-full text-xs font-semibold text-white whitespace-nowrap"
                style={{ backgroundColor: getStatusColor(event.status) }}
              >
                {getStatusLabel(event.status)}
              </span>
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

              {/* Price - Session 23: Dual pricing support */}
              {!event.isFree && (
                <span className="px-2 py-1 bg-[#FFE8CC] text-[#8B1538] rounded text-xs font-medium">
                  {event.hasDualPricing ? (
                    `Adult: $${event.adultPriceAmount} | Child: $${event.childPriceAmount}`
                  ) : (
                    `$${event.ticketPriceAmount}`
                  )}
                </span>
              )}
            </div>

            {/* Cancel Registration Button - Session 30 */}
            {onCancelClick && (
              <button
                onClick={(e) => handleCancelClick(event.id, e)}
                disabled={cancellingEventId === event.id}
                className="px-3 py-1 text-sm font-medium text-red-700 bg-red-50 border border-red-200 rounded hover:bg-red-100 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                aria-label="Cancel registration"
              >
                {cancellingEventId === event.id ? 'Cancelling...' : 'Cancel Registration'}
              </button>
            )}
          </div>
        </div>
      ))}
    </div>
  );
}
