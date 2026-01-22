'use client';

import { useRouter } from 'next/navigation';
import { Mail, Send, FileText, ExternalLink, Bell } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { NewsletterStatusBadge } from './NewsletterStatusBadge';
import { useNewslettersByEvent } from '@/presentation/hooks/useNewsletters';
import {
  useSendEventNotification,
  useEventNotificationHistory,
  useEventById,
  useSendEventReminder,
  useEventReminderHistory,
} from '@/presentation/hooks/useEvents';
import { EventStatus } from '@/infrastructure/api/types/events.types';
import { NewsletterDto } from '@/infrastructure/api/types/newsletters.types';
import toast from 'react-hot-toast';

/**
 * Phase 6A.74 Part 13+: Build recipient breakdown string showing all 4 sources
 * Format: "(X from newsletter groups, Y from event groups, Z subscribers, W event registrations)"
 * Only includes sources with count > 0
 */
function buildNewsletterRecipientBreakdown(newsletter: NewsletterDto): string | null {
  const parts: string[] = [];

  // Newsletter email groups
  if (newsletter.newsletterEmailGroupCount && newsletter.newsletterEmailGroupCount > 0) {
    parts.push(`${newsletter.newsletterEmailGroupCount} from newsletter groups`);
  }

  // Event email groups (if newsletter linked to event)
  if (newsletter.eventEmailGroupCount && newsletter.eventEmailGroupCount > 0) {
    parts.push(`${newsletter.eventEmailGroupCount} from event groups`);
  }

  // Newsletter subscribers
  if (newsletter.subscriberCount && newsletter.subscriberCount > 0) {
    parts.push(`${newsletter.subscriberCount} subscribers`);
  }

  // Event registrations (if newsletter linked to event)
  if (newsletter.eventRegistrationCount && newsletter.eventRegistrationCount > 0) {
    parts.push(`${newsletter.eventRegistrationCount} from event registrations`);
  }

  return parts.length > 0 ? `(${parts.join(', ')})` : null;
}

export interface EventNewslettersTabProps {
  eventId: string;
  eventTitle?: string;
}

/**
 * EventNewslettersTab Component
 * Event management tab for event-specific newsletters
 * Phase 6A.74 Part 6: Updated to use route-based navigation
 * Phase 6A.61: Added event notification email functionality
 * Phase 6A.76: Added manual event reminder functionality
 */
export function EventNewslettersTab({ eventId, eventTitle }: EventNewslettersTabProps) {
  const router = useRouter();

  // Phase 6A.76: Custom reminder type (automatic reminders handled by system)

  // Fetch event details to check status
  const { data: event } = useEventById(eventId);

  // Fetch newsletters for this event
  const { data: newsletters = [], isLoading } = useNewslettersByEvent(eventId);

  // Phase 6A.61: Notification functionality
  const sendEmailMutation = useSendEventNotification();
  const { data: history, isLoading: historyLoading, error: historyError } = useEventNotificationHistory(eventId);

  // Phase 6A.76: Reminder functionality
  const sendReminderMutation = useSendEventReminder();
  const { data: reminderHistory, isLoading: reminderHistoryLoading } = useEventReminderHistory(eventId);

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

  // Phase 6A.76: Send manual event reminder (always custom type - automatic reminders handled by system)
  const handleSendReminder = async () => {
    try {
      const result = await sendReminderMutation.mutateAsync({ eventId, reminderType: 'custom' });
      if (result.recipientCount === 0) {
        toast.success(
          'No registrations found for this event. Reminder not sent.',
          { duration: 4000 }
        );
      } else {
        toast.success(
          `Reminder queued for ${result.recipientCount} registered attendee${result.recipientCount === 1 ? '' : 's'}!`,
          { duration: 4000 }
        );
      }
    } catch (error: any) {
      toast.error(error?.message || 'Failed to send reminder');
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

        {/* Email Send History - Phase 6A.76: Updated to match newsletter format */}
        <div className="border-t pt-4">
          <h4 className="text-md font-semibold mb-3 text-[#8B1538]">Email Send History</h4>
          {historyLoading && <div className="text-sm text-gray-500">Loading history...</div>}
          {historyError && <div className="text-sm text-red-500">Failed to load history</div>}
          {history && history.length === 0 && (
            <div className="text-sm text-gray-500">No emails sent yet</div>
          )}
          {history && history.length > 0 && (
            <div className="space-y-2 max-h-[240px] overflow-y-auto pr-2">
              {history.map((record) => (
                <div key={record.id} className="border-b pb-2 last:border-b-0">
                  <p className="text-sm text-gray-700 flex items-center flex-wrap gap-1">
                    <Mail className="w-3 h-3 text-[#6366F1]" />
                    <span className="font-medium">{record.recipientCount}</span>
                    <span>recipient{record.recipientCount === 1 ? '' : 's'}</span>
                    <span className="text-gray-500">(from email groups, subscribers & registrations)</span>
                    <span className="text-green-600">✓ {record.successfulSends} sent</span>
                    {record.failedSends > 0 && (
                      <span className="text-red-600">✗ {record.failedSends} failed</span>
                    )}
                  </p>
                  <p className="text-xs text-gray-500 mt-1">
                    Sent on {new Date(record.sentAt).toLocaleString()} by {record.sentByUserName}
                  </p>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      {/* Phase 6A.76: Event Reminders Section */}
      <div className="border rounded-lg p-6 bg-white shadow-sm">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-3">
            <Bell className="w-6 h-6 text-[#FF7900]" />
            <div>
              <h3 className="text-lg font-bold text-[#8B1538]">Event Reminders</h3>
              <p className="text-sm text-gray-600">Send reminder emails to registered attendees</p>
            </div>
          </div>
        </div>

        {canSendNotification ? (
          <>
            <div className="bg-blue-50 border border-blue-200 rounded-lg p-3 mb-4">
              <p className="text-sm text-blue-800">
                Send a custom reminder email to all registered attendees for this event.
                Automatic reminders are sent 7 days, 2 days, and 1 day before the event by the system.
              </p>
            </div>

            <div className="flex items-center gap-4">
              <Button
                onClick={handleSendReminder}
                disabled={sendReminderMutation.isPending}
                className="flex items-center gap-2 text-white"
                style={{ background: '#FF7900' }}
              >
                <Bell className="h-4 w-4" />
                {sendReminderMutation.isPending ? 'Sending...' : 'Send Custom Reminder'}
              </Button>
            </div>

            {/* Phase 6A.76: Reminder History */}
            <div className="border-t pt-4 mt-4">
              <h4 className="text-md font-semibold mb-3 text-[#8B1538]">Reminder Send History</h4>
              {reminderHistoryLoading && <div className="text-sm text-gray-500">Loading history...</div>}
              {!reminderHistoryLoading && (!reminderHistory || reminderHistory.length === 0) && (
                <div className="text-sm text-gray-500">No reminders sent yet</div>
              )}
              {!reminderHistoryLoading && reminderHistory && reminderHistory.length > 0 && (
                <div className="space-y-2 max-h-[180px] overflow-y-auto pr-2">
                  {reminderHistory.map((record, index) => (
                    <div key={`${record.reminderType}-${record.sentDate}-${index}`} className="border-b pb-2 last:border-b-0">
                      <p className="text-sm text-gray-700 flex items-center flex-wrap gap-1">
                        <Mail className="w-3 h-3 text-[#6366F1]" />
                        <span className="font-medium">{record.recipientCount}</span>
                        <span>recipient{record.recipientCount === 1 ? '' : 's'}</span>
                        <span className="text-gray-500">({record.reminderTypeLabel})</span>
                        <span className="text-green-600">✓ sent</span>
                      </p>
                      <p className="text-xs text-gray-500 mt-1">
                        Sent on {new Date(record.sentDate).toLocaleDateString()}
                      </p>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </>
        ) : (
          <div className="bg-gray-50 border border-gray-200 rounded-lg p-3">
            <p className="text-sm text-gray-600">
              Event reminders are only available for Active and Published events.
            </p>
          </div>
        )}
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
                      {/* Phase 6A.74 Part 13+: Display recipient counts with detailed breakdown and send stats */}
                      {newsletter.totalRecipientCount != null && newsletter.totalRecipientCount > 0 && (
                        <p className="text-xs text-gray-600 mt-1 flex items-center flex-wrap gap-1">
                          <Mail className="w-3 h-3 text-[#6366F1]" />
                          <span>
                            {newsletter.totalRecipientCount.toLocaleString()} recipient{newsletter.totalRecipientCount === 1 ? '' : 's'}
                            {buildNewsletterRecipientBreakdown(newsletter) && (
                              <span className="text-gray-500"> {buildNewsletterRecipientBreakdown(newsletter)}</span>
                            )}
                          </span>
                          {/* Success/Failed counts */}
                          {newsletter.successfulSends != null && (
                            <span className="text-green-600 ml-1">
                              ✓ {newsletter.successfulSends} sent
                            </span>
                          )}
                          {newsletter.failedSends != null && newsletter.failedSends > 0 && (
                            <span className="text-red-600 ml-1">
                              ✗ {newsletter.failedSends} failed
                            </span>
                          )}
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
