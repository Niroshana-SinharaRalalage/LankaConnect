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

export interface EventNewslettersTabProps {
  eventId: string;
  eventTitle?: string;
}

/**
 * EventNewslettersTab Component
 * Event management tab for event-specific newsletters
 * Phase 6A.74 Part 6: Updated to use route-based navigation
 */
export function EventNewslettersTab({ eventId, eventTitle }: EventNewslettersTabProps) {
  const router = useRouter();

  // Fetch newsletters for this event
  const { data: newsletters = [], isLoading } = useNewslettersByEvent(eventId);

  // Mutations
  const publishMutation = usePublishNewsletter();
  const sendMutation = useSendNewsletter();
  const deleteMutation = useDeleteNewsletter();

  // Handlers
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

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
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
  );
}
