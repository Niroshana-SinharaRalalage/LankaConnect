'use client';

import * as React from 'react';
import { useRouter } from 'next/navigation';
import { Mail, Plus } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { NewsletterList } from './NewsletterList';
import {
  useMyNewsletters,
  usePublishNewsletter,
  useSendNewsletter,
  useDeleteNewsletter,
} from '@/presentation/hooks/useNewsletters';

/**
 * NewslettersTab Component
 * Dashboard tab for newsletter management
 * Phase 6A.74 Part 6: Updated to use route-based navigation instead of modal
 */
export function NewslettersTab() {
  const router = useRouter();

  // Fetch user's newsletters
  const { data: newsletters = [], isLoading } = useMyNewsletters();

  // Mutations
  const publishMutation = usePublishNewsletter();
  const sendMutation = useSendNewsletter();
  const deleteMutation = useDeleteNewsletter();

  // Handlers
  const handleCreateClick = () => {
    router.push('/newsletters/create');
  };

  const handleNewsletterClick = (newsletterId: string) => {
    router.push(`/dashboard/my-newsletters/${newsletterId}`);
  };

  const handleEditClick = (newsletterId: string) => {
    router.push(`/dashboard/my-newsletters/${newsletterId}/edit`);
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

      {/* Newsletter List */}
      <NewsletterList
        newsletters={newsletters}
        isLoading={isLoading}
        emptyMessage="No newsletters yet. Create your first newsletter to get started!"
        onNewsletterClick={handleNewsletterClick}
        onEditNewsletter={handleEditClick}
        onPublishNewsletter={handlePublish}
        onSendNewsletter={handleSend}
        onDeleteNewsletter={handleDelete}
      />
    </div>
  );
}
