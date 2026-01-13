'use client';

import * as React from 'react';
import { useRouter } from 'next/navigation';
import { Mail, Send } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { NewsletterList } from './NewsletterList';
import {
  useNewslettersByEvent,
  usePublishNewsletter,
  useSendNewsletter,
  useDeleteNewsletter,
} from '@/presentation/hooks/useNewsletters';
import {
  useSendEventNotification,
  useEventNotificationHistory,
} from '@/presentation/hooks/useEvents';
import { useEventById } from '@/presentation/hooks/useEvents';
import { EventStatus } from '@/infrastructure/api/types/events.types';
import toast from 'react-hot-toast';

export interface EventNewslettersTabProps {
  eventId: string;
  eventTitle?: string;
}

/**
 * EventNewslettersTab Component
 * Event management tab for event-specific newsletters
 * Phase 6A.74 Part 6: Updated to use route-based navigation
 * Phase 6A.61: Added event notification email functionality
 */
export function EventNewslettersTab({ eventId, eventTitle }: EventNewslettersTabProps) {
  const router = useRouter();

  // Fetch event details to check status
  const { data: event } = useEventById(eventId);

  // Fetch newsletters for this event
  const { data: newsletters = [], isLoading } = useNewslettersByEvent(eventId);

  // Phase 6A.61: Notification functionality
  const sendEmailMutation = useSendEventNotification();
  const { data: history, isLoading: historyLoading, error: historyError } = useEventNotificationHistory(eventId);

  // Mutations
  const publishMutation = usePublishNewsletter();
  const sendMutation = useSendNewsletter();
  const deleteMutation = useDeleteNewsletter();

  // Handlers
  // Phase 6A.61: Send event notification email
  const handleSendEmail = async () => {
    try {
      await sendEmailMutation.mutateAsync(eventId);
      toast.success('Email is being sent in the background!');
    } catch (error: any) {
      toast.error(error?.message || 'Failed to send email');
    }
  };

  const handleSendReminderClick = () => {
    // Navigate to newsletter creation with event pre-linked
    router.push(`/dashboard/newsletters/create?eventId=${eventId}`);
  };

  const handleNewsletterClick = (newsletterId: string) => {
    router.push(`/dashboard/newsletters/${newsletterId}`);
  };

  const handleEditClick = (newsletterId: string) => {
    router.push(`/dashboard/newsletters/${newsletterId}/edit`);
  };

  const handlePublish = async (newsletterId: string) => {
    try {
      await publishMutation.mutateAsync(newsletterId);
    } catch (error) {
      console.error('Failed to publish newsletter:', error);
    }
  };

  const handleSend = async (newsletterId: string) => {
    try {
      await sendMutation.mutateAsync(newsletterId);
    } catch (error) {
      console.error('Failed to send newsletter:', error);
    }
  };

  const handleDelete = async (newsletterId: string) => {
    if (!confirm('Are you sure you want to delete this newsletter? This action cannot be undone.')) {
      return;
    }

    try {
      await deleteMutation.mutateAsync(newsletterId);
    } catch (error) {
      console.error('Failed to delete newsletter:', error);
    }
  };

  // Phase 6A.61: Check if event is Active or Published (can send notification)
  const canSendNotification = event && (event.status === EventStatus.Active || event.status === EventStatus.Published);

  return (
    <div className="space-y-8">
      {/* Phase 6A.61: Event Notification Section */}
      <div className="border rounded-lg p-6 bg-white shadow-sm">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-3">
            <Mail className="w-6 h-6 text-[#FF7900]" />
            <div>
              <h3 className="text-lg font-bold text-[#8B1538]">Quick Event Notification</h3>
              <p className="text-sm text-gray-600">Send event details to all attendees</p>
            </div>
          </div>
          {canSendNotification && (
            <Button
              onClick={handleSendEmail}
              disabled={sendEmailMutation.isPending}
              className="flex items-center gap-2 text-white"
              style={{ background: '#FF7900' }}
            >
              <Mail className="h-4 w-4" />
              {sendEmailMutation.isPending ? 'Sending...' : 'Send Email to Attendees'}
            </Button>
          )}
        </div>

        {/* Description */}
        {canSendNotification && (
          <div className="bg-blue-50 border border-blue-200 rounded-lg p-3 mb-4">
            <p className="text-sm text-blue-800">
              Sends a pre-formatted email with event details to all registered attendees and newsletter subscribers.
              The email is sent in the background.
            </p>
          </div>
        )}

        {!canSendNotification && (
          <div className="bg-gray-50 border border-gray-200 rounded-lg p-3 mb-4">
            <p className="text-sm text-gray-600">
              Event notification is only available for Active and Published events.
            </p>
          </div>
        )}

        {/* Email Send History */}
        <div className="border-t pt-4">
          <h4 className="text-md font-semibold mb-3 text-[#8B1538]">Email Send History</h4>
          {historyLoading && <div className="text-sm text-gray-500">Loading history...</div>}
          {historyError && <div className="text-sm text-red-500">Failed to load history</div>}
          {history && history.length === 0 && (
            <div className="text-sm text-gray-500">No emails sent yet</div>
          )}
          {history && history.length > 0 && (
            <div className="space-y-3">
              {history.map((record) => (
                <div key={record.id} className="border-b pb-3 last:border-b-0">
                  <div className="flex justify-between items-start">
                    <div>
                      <p className="text-sm font-medium text-gray-900">
                        Sent on {new Date(record.sentAt).toLocaleString()}
                      </p>
                      <p className="text-xs text-gray-600 mt-1">
                        By: {record.sentByUserName}
                      </p>
                    </div>
                    <div className="text-right">
                      <p className="text-sm text-gray-900">
                        <span className="font-medium">{record.recipientCount}</span> recipients
                      </p>
                      <p className="text-xs text-green-600 mt-1">
                        ✓ {record.successfulSends} sent
                      </p>
                      {record.failedSends > 0 && (
                        <p className="text-xs text-red-600">
                          ✗ {record.failedSends} failed
                        </p>
                      )}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      {/* Divider */}
      <div className="border-t border-gray-200"></div>

      {/* Event Newsletters Section */}
      <div>
        {/* Header */}
        <div className="flex items-center justify-between mb-6">
          <div className="flex items-center gap-3">
            <Mail className="w-6 h-6 text-[#FF7900]" />
            <div>
              <h3 className="text-xl font-bold text-[#8B1538]">Event Newsletters</h3>
              {eventTitle && (
                <p className="text-sm text-gray-600">For: {eventTitle}</p>
              )}
            </div>
          </div>
          <Button
            onClick={handleSendReminderClick}
            className="bg-[#FF7900] hover:bg-[#E66D00] text-white"
          >
            <Send className="w-4 h-4 mr-2" />
            Send Reminder/Update
          </Button>
        </div>

      {/* Description */}
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
        <p className="text-sm text-blue-800">
          <strong>Event-Specific Newsletters:</strong> Create newsletters linked to this event.
          Recipients will include registered attendees and any selected email groups.
        </p>
      </div>

        {/* Newsletter List */}
        <NewsletterList
          newsletters={newsletters}
          isLoading={isLoading}
          emptyMessage="No newsletters for this event yet. Click 'Send Reminder/Update' to create one!"
          onNewsletterClick={handleNewsletterClick}
          onEditNewsletter={handleEditClick}
          onPublishNewsletter={handlePublish}
          onSendNewsletter={handleSend}
          onDeleteNewsletter={handleDelete}
        />
      </div>
    </div>
  );
}
