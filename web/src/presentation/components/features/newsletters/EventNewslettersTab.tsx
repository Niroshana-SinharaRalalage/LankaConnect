'use client';

import { useRouter } from 'next/navigation';
import { Mail, Send, FileText, ExternalLink } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { NewsletterStatusBadge } from './NewsletterStatusBadge';
import { useNewslettersByEvent } from '@/presentation/hooks/useNewsletters';
import {
  useSendEventNotification,
  useEventNotificationHistory,
  useEventById,
} from '@/presentation/hooks/useEvents';
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

  // Handlers
  // Phase 6A.61: Send event notification email
  // Phase 6A.61+ Issue #4: Show warning if 0 recipients found
  const handleSendEmail = async () => {
    try {
      await sendEmailMutation.mutateAsync(eventId);
      // Phase 6A.61+ Issue #4: Check recipient count and show appropriate message
      // Note: recipientCount is 0 initially because the actual count is updated by background job
      // However, we can still give a general success message and the history will update
      toast.success(
        'Email notification queued! Check the history below for delivery status.',
        { duration: 4000 }
      );
    } catch (error: any) {
      toast.error(error?.message || 'Failed to send email');
    }
  };

  const handleSendReminderClick = () => {
    // Navigate to newsletter creation with event pre-linked
    router.push(`/dashboard/my-newsletters/create?eventId=${eventId}`);
  };

  // Phase 6A.61+ Issue #8: Pass navigation source for back button
  const handleNewsletterClick = (newsletterId: string) => {
    router.push(`/dashboard/my-newsletters/${newsletterId}?from=event&eventId=${eventId}`);
  };

  // Phase 6A.61: Check if event is Active or Published (can send notification)
  // Handle status as enum value, string, or lowercase string (API inconsistency fix)
  const canSendNotification = event && (
    (event.status as any) === EventStatus.Active ||
    (event.status as any) === EventStatus.Published ||
    (event.status as any) === 'Active' ||
    (event.status as any) === 'Published' ||
    String(event.status).toLowerCase() === 'active' ||
    String(event.status).toLowerCase() === 'published'
  );

  return (
    <div className="space-y-8">
      {/* Phase 6A.61: Event Email Notifications Section */}
      {/* Phase 6A.61+ Issues #1, #2, #3: Updated text labels */}
      <div className="border rounded-lg p-6 bg-white shadow-sm">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-3">
            <Mail className="w-6 h-6 text-[#FF7900]" />
            <div>
              <h3 className="text-lg font-bold text-[#8B1538]">Event Email Notifications</h3>
              <p className="text-sm text-gray-600">Send event details to recipients</p>
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
              {sendEmailMutation.isPending ? 'Sending...' : 'Send an Email'}
            </Button>
          )}
        </div>

        {/* Description - Phase 6A.61+ Issue #3: Updated text */}
        {canSendNotification && (
          <div className="bg-blue-50 border border-blue-200 rounded-lg p-3 mb-4">
            <p className="text-sm text-blue-800">
              Sends a pre-formatted email with event details to email groups selected in the event,
              all registered attendees and eligible newsletter subscribers. The email is sent in the background.
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
            <div className="space-y-3 max-h-[240px] overflow-y-auto pr-2">
              {history.map((record) => (
                <div key={record.id} className="border-b pb-3 last:border-b-0">
                  <div className="flex justify-between items-center">
                    <div>
                      <p className="text-sm font-medium text-gray-900">
                        Sent on {new Date(record.sentAt).toLocaleString()}
                      </p>
                      <p className="text-xs text-gray-600 mt-1">
                        By: {record.sentByUserName}
                      </p>
                    </div>
                    {/* Consolidated stats on single line */}
                    <p className="text-sm text-gray-700">
                      <span className="font-medium">{record.recipientCount}</span> recipients
                      <span className="text-green-600 ml-3">✓ {record.successfulSends} sent</span>
                      {record.failedSends > 0 && (
                        <span className="text-red-600 ml-3">✗ {record.failedSends} failed</span>
                      )}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      {/* Phase 6A.61+ Issue #5: Event Newsletters Section with Box Container */}
      <div className="border rounded-lg p-6 bg-white shadow-sm">
        {/* Header - Phase 6A.61+ Issue #6: Updated button text */}
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-3">
            <FileText className="w-6 h-6 text-[#FF7900]" />
            <div>
              <h3 className="text-lg font-bold text-[#8B1538]">Event Newsletters</h3>
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
            Create Reminder/Update Newsletter
          </Button>
        </div>

        {/* Description - Phase 6A.61+ Issue #7: Updated text */}
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-3 mb-4">
          <p className="text-sm text-blue-800">
            <strong>Event-Specific Newsletters:</strong> Create newsletters linked to this event.
            Recipients will include event registered attendees, any selected email groups in the event,
            any selected email groups in the newsletter, and eligible newsletter subscribers.
          </p>
        </div>

        {/* Phase 6A.61+ Issue #8: Newsletter List with clickable items */}
        <div className="border-t pt-4">
          <h4 className="text-md font-semibold mb-3 text-[#8B1538]">Event Newsletters</h4>
          {isLoading && <div className="text-sm text-gray-500">Loading newsletters...</div>}
          {!isLoading && newsletters.length === 0 && (
            <div className="text-sm text-gray-500">
              No newsletters for this event yet. Click &apos;Create Reminder/Update Newsletter&apos; to create one!
            </div>
          )}
          {!isLoading && newsletters.length > 0 && (
            <div className="space-y-3 max-h-[360px] overflow-y-auto pr-2">
              {newsletters.map((newsletter) => (
                <div
                  key={newsletter.id}
                  onClick={() => handleNewsletterClick(newsletter.id)}
                  className="border rounded-lg p-4 hover:bg-gray-50 cursor-pointer transition-colors"
                >
                  <div className="flex justify-between items-start">
                    <div className="flex-1">
                      <div className="flex items-center gap-2">
                        <h5 className="font-medium text-gray-900">{newsletter.title}</h5>
                        <NewsletterStatusBadge status={newsletter.status} />
                      </div>
                      <p className="text-sm text-gray-600 mt-1 line-clamp-2">
                        {newsletter.description?.replace(/<[^>]*>/g, '').substring(0, 100)}
                        {newsletter.description && newsletter.description.length > 100 ? '...' : ''}
                      </p>
                      <p className="text-xs text-gray-500 mt-2">
                        Created: {new Date(newsletter.createdAt).toLocaleDateString()}
                        {newsletter.sentAt && ` • Sent: ${new Date(newsletter.sentAt).toLocaleDateString()}`}
                      </p>
                      {/* Phase 6A.74 Part 13 Issue #2: Display recipient counts in Event Communications tab */}
                      {newsletter.totalRecipientCount && newsletter.totalRecipientCount > 0 && (
                        <p className="text-xs text-gray-600 mt-1 flex items-center gap-1">
                          <Mail className="w-3 h-3 text-[#6366F1]" />
                          <span>
                            Sent to {newsletter.totalRecipientCount.toLocaleString()} recipient{newsletter.totalRecipientCount === 1 ? '' : 's'}
                            {newsletter.emailGroupRecipientCount != null && newsletter.subscriberRecipientCount != null && (
                              <span className="text-gray-500">
                                {' '}({newsletter.emailGroupRecipientCount} from groups, {newsletter.subscriberRecipientCount} subscribers)
                              </span>
                            )}
                          </span>
                        </p>
                      )}
                    </div>
                    <ExternalLink className="w-4 h-4 text-gray-400 flex-shrink-0 ml-2" />
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
