'use client';

import * as React from 'react';
import { useState } from 'react';
import { Mail, Plus } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { NewsletterList } from './NewsletterList';
import { NewsletterForm } from './NewsletterForm';
import {
  useMyNewsletters,
  usePublishNewsletter,
  useSendNewsletter,
  useDeleteNewsletter,
} from '@/presentation/hooks/useNewsletters';

/**
 * NewslettersTab Component
 * Dashboard tab for newsletter management
 * Phase 6A.74: Newsletter Feature - Part 4C Dashboard Integration
 */
export function NewslettersTab() {
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);

  // Fetch user's newsletters
  const { data: newsletters = [], isLoading } = useMyNewsletters();

  // Mutations
  const publishMutation = usePublishNewsletter();
  const sendMutation = useSendNewsletter();
  const deleteMutation = useDeleteNewsletter();

  // Handlers
  const handleCreateClick = () => {
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
      // Success notification handled by React Query
    } catch (error) {
      console.error('Failed to publish newsletter:', error);
      // Error notification handled by React Query
    }
  };

  const handleSend = async (newsletterId: string) => {
    try {
      await sendMutation.mutateAsync(newsletterId);
      // Success notification handled by React Query
    } catch (error) {
      console.error('Failed to send newsletter:', error);
      // Error notification handled by React Query
    }
  };

  const handleDelete = async (newsletterId: string) => {
    if (!confirm('Are you sure you want to delete this newsletter? This action cannot be undone.')) {
      return;
    }

    try {
      await deleteMutation.mutateAsync(newsletterId);
      // Success notification handled by React Query
    } catch (error) {
      console.error('Failed to delete newsletter:', error);
      // Error notification handled by React Query
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Mail className="w-6 h-6 text-[#FF7900]" />
          <h2 className="text-2xl font-bold text-[#8B1538]">Newsletters</h2>
        </div>
        <Button
          onClick={handleCreateClick}
          className="bg-[#FF7900] hover:bg-[#E66D00] text-white"
        >
          <Plus className="w-4 h-4 mr-2" />
          Create Newsletter
        </Button>
      </div>

      {/* Description */}
      <p className="text-gray-600">
        Create and manage newsletters to communicate with your email groups and subscribers.
      </p>

      {/* Newsletter Form Modal */}
      {isFormOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50">
          <div className="bg-white rounded-lg shadow-xl max-w-4xl w-full max-h-[90vh] overflow-y-auto m-4">
            <div className="p-6">
              <h3 className="text-xl font-bold text-[#8B1538] mb-4">
                {editingId ? 'Edit Newsletter' : 'Create Newsletter'}
              </h3>
              <NewsletterForm
                newsletterId={editingId || undefined}
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
        emptyMessage="No newsletters yet. Create your first newsletter to get started!"
        onEditNewsletter={handleEditClick}
        onPublishNewsletter={handlePublish}
        onSendNewsletter={handleSend}
        onDeleteNewsletter={handleDelete}
      />
    </div>
  );
}
