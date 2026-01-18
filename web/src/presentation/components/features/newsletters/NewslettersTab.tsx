'use client';

import * as React from 'react';
import { useRouter } from 'next/navigation';
import { Mail, Plus, Search } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { ConfirmDialog } from '@/presentation/components/ui/ConfirmDialog';
import { NewsletterList } from './NewsletterList';
import { NewsletterStatus } from '@/infrastructure/api/types/newsletters.types';
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
 * Phase 6A.74 Part 10 Issue #2: Replaced confirm() with ConfirmDialog
 * Phase 6A.74 UI Fix Issue #5: Added client-side filtering (search + status)
 */
export function NewslettersTab() {
  const router = useRouter();

  // Phase 6A.74 Part 10 Issue #2: Dialog state for delete confirmation
  const [deleteDialogOpen, setDeleteDialogOpen] = React.useState(false);
  const [newsletterToDelete, setNewsletterToDelete] = React.useState<string | null>(null);

  // Phase 6A.74 UI Fix Issue #5: Filter state
  const [searchTerm, setSearchTerm] = React.useState('');
  const [statusFilter, setStatusFilter] = React.useState<'all' | NewsletterStatus>('all');

  // Fetch user's newsletters
  const { data: newsletters = [], isLoading } = useMyNewsletters();

  // Mutations
  const publishMutation = usePublishNewsletter();
  const sendMutation = useSendNewsletter();
  const deleteMutation = useDeleteNewsletter();

  // Phase 6A.74 UI Fix Issue #5: Client-side filtering
  const filteredNewsletters = React.useMemo(() => {
    return newsletters.filter(newsletter => {
      // Search filter (title and description)
      if (searchTerm) {
        const searchLower = searchTerm.toLowerCase();
        const matchesSearch =
          newsletter.title.toLowerCase().includes(searchLower) ||
          newsletter.description.toLowerCase().includes(searchLower);
        if (!matchesSearch) return false;
      }

      // Status filter
      if (statusFilter !== 'all') {
        if (newsletter.status !== statusFilter) return false;
      }

      return true;
    });
  }, [newsletters, searchTerm, statusFilter]);

  // Handlers
  // Phase 6A.74 Part 10 Issue #2 Fix: Correct route to dashboard create page
  const handleCreateClick = () => {
    router.push('/dashboard/my-newsletters/create');
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

  const handleDeleteClick = async (newsletterId: string) => {
    setNewsletterToDelete(newsletterId);
    setDeleteDialogOpen(true);
  };

  const handleDeleteConfirm = async () => {
    if (!newsletterToDelete) return;

    try {
      await deleteMutation.mutateAsync(newsletterToDelete);
      // Success notification handled by React Query
    } catch (error) {
      console.error('Failed to delete newsletter:', error);
      // Error notification handled by React Query
    } finally {
      setNewsletterToDelete(null);
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

      {/* Phase 6A.74 UI Fix Issue #5: Filter Controls */}
      <div className="flex flex-col sm:flex-row gap-4">
        {/* Search */}
        <div className="sm:flex-1 sm:max-w-md">
          <div className="relative">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-5 h-5" />
            <input
              type="text"
              placeholder="Search newsletters..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-orange-500 focus:border-transparent"
              aria-label="Search newsletters"
            />
          </div>
        </div>

        {/* Status Filter - Phase 6A.74 Part 11 Issue #4 Fix */}
        <select
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value === 'all' ? 'all' : e.target.value as NewsletterStatus)}
          className="px-4 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-orange-500 focus:border-transparent"
          aria-label="Filter by status"
        >
          <option value="all">All Status</option>
          <option value={NewsletterStatus.Draft}>Draft</option>
          <option value={NewsletterStatus.Active}>Active</option>
          <option value={NewsletterStatus.Sent}>Sent</option>
          <option value={NewsletterStatus.Inactive}>Inactive</option>
        </select>
      </div>

      {/* Results count */}
      {(searchTerm || statusFilter !== 'all') && (
        <div className="text-sm text-gray-600">
          Showing {filteredNewsletters.length} of {newsletters.length} newsletter{newsletters.length !== 1 ? 's' : ''}
        </div>
      )}

      {/* Newsletter List with filtered data */}
      <NewsletterList
        newsletters={filteredNewsletters}
        isLoading={isLoading}
        emptyMessage={
          searchTerm || statusFilter !== 'all'
            ? 'No newsletters match your filters. Try adjusting your search or filter criteria.'
            : 'No newsletters yet. Create your first newsletter to get started!'
        }
        onNewsletterClick={handleNewsletterClick}
        onEditNewsletter={handleEditClick}
        onPublishNewsletter={handlePublish}
        onSendNewsletter={handleSend}
        onDeleteNewsletter={handleDeleteClick}
      />

      {/* Phase 6A.74 Part 10 Issue #2: Delete Confirmation Dialog */}
      <ConfirmDialog
        open={deleteDialogOpen}
        onOpenChange={setDeleteDialogOpen}
        title="Delete Newsletter"
        description="Are you sure you want to delete this newsletter? This action cannot be undone."
        confirmLabel="Delete"
        onConfirm={handleDeleteConfirm}
        variant="danger"
        isLoading={deleteMutation.isPending}
      />
    </div>
  );
}
