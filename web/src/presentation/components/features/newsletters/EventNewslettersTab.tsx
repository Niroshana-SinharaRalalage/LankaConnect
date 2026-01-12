'use client';

import * as React from 'react';
import { useState } from 'react';
import { Mail, Plus, Send } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { NewsletterList } from './NewsletterList';
import { NewsletterForm } from './NewsletterForm';
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
 * Phase 6A.74: Newsletter Feature - Part 4D Event Management Integration
 */
export function EventNewslettersTab({ eventId, eventTitle }: EventNewslettersTabProps) {
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);

  // Fetch newsletters for this event
  const { data: newsletters = [], isLoading } = useNewslettersByEvent(eventId);

  // Mutations
  const publishMutation = usePublishNewsletter();
  const sendMutation = useSendNewsletter();
  const deleteMutation = useDeleteNewsletter();

  // Handlers
  const handleSendNewsletterClick = () => {
    setEditingId(null);
    setIsFormOpen(true);
  };

  const handleEditClick = (newsletterId: string) => {
    setEditingId(newsletterId);
    setIsFormOpen(true);
  };

  const handleFormSuccess = () => {
    setIsFormOpen(false);
    setEditingId(null);
  };

  const handleFormCancel = () => {
    setIsFormOpen(false);
    setEditingId(null);
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
          onClick={handleSendNewsletterClick}
          className="bg-[#FF7900] hover:bg-[#E66D00] text-white"
        >
          <Send className="w-4 h-4 mr-2" />
          Send Newsletter
        </Button>
      </div>

      {/* Description */}
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
        <p className="text-sm text-blue-800">
          <strong>Event-Specific Newsletters:</strong> Create newsletters linked to this event.
          Recipients will include registered attendees and any selected email groups.
        </p>
      </div>

      {/* Newsletter Form Modal */}
      {isFormOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50">
          <div className="bg-white rounded-lg shadow-xl max-w-4xl w-full max-h-[90vh] overflow-y-auto m-4">
            <div className="p-6">
              <h3 className="text-xl font-bold text-[#8B1538] mb-4">
                {editingId ? 'Edit Newsletter' : 'Create Event Newsletter'}
              </h3>
              <NewsletterForm
                newsletterId={editingId || undefined}
                initialEventId={eventId}
                onSuccess={handleFormSuccess}
                onCancel={handleFormCancel}
              />
            </div>
          </div>
        </div>
      )}

      {/* Newsletter List */}
      <NewsletterList
        newsletters={newsletters}
        isLoading={isLoading}
        emptyMessage="No newsletters for this event yet. Click 'Send Newsletter' to create one!"
        onEditNewsletter={handleEditClick}
        onPublishNewsletter={handlePublish}
        onSendNewsletter={handleSend}
        onDeleteNewsletter={handleDelete}
      />
    </div>
  );
}
