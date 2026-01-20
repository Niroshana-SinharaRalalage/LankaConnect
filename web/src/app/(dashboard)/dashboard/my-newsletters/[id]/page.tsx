'use client';

import * as React from 'react';
import { use } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { ArrowLeft, Edit, Upload, Send, Trash2, Calendar, Mail, MapPin, ExternalLink, XCircle } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { ConfirmDialog } from '@/presentation/components/ui/ConfirmDialog';
import { NewsletterStatusBadge } from '@/presentation/components/features/newsletters/NewsletterStatusBadge';
import {
  useNewsletterById,
  usePublishNewsletter,
  useUnpublishNewsletter,
  useSendNewsletter,
  useDeleteNewsletter,
  useReactivateNewsletter,
} from '@/presentation/hooks/useNewsletters';
import {
  isNewsletterDraft,
  isNewsletterActive,
  isNewsletterInactive,
  isNewsletterSent,
} from '@/lib/enum-utils';

/**
 * Newsletter Details Page
 * Phase 6A.74 Part 6 - Route-based UI
 * Phase 6A.61+ Issue #8 - Dynamic back button navigation
 * Phase 6A.74 Part 10 - Fixed status comparison for string/enum handling
 *
 * Features:
 * - View full newsletter content (HTML rendered safely)
 * - Action buttons based on status
 * - Recipient information display
 * - Event linkage display
 * - Dynamic back navigation based on referrer (event page, newsletters page, or dashboard)
 */
export default function NewsletterDetailsPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const router = useRouter();
  const searchParams = useSearchParams();

  // Phase 6A.61+ Issue #8: Get navigation source from query params
  const fromSource = searchParams.get('from');
  const sourceEventId = searchParams.get('eventId');

  // Phase 6A.74 Part 10 Issue #2: Dialog states for confirmation modals
  const [showUnpublishDialog, setShowUnpublishDialog] = React.useState(false);
  const [showSendDialog, setShowSendDialog] = React.useState(false);
  const [showDeleteDialog, setShowDeleteDialog] = React.useState(false);

  const { data: newsletter, isLoading } = useNewsletterById(id);
  const publishMutation = usePublishNewsletter();
  const unpublishMutation = useUnpublishNewsletter();
  const sendMutation = useSendNewsletter();
  const deleteMutation = useDeleteNewsletter();
  const reactivateMutation = useReactivateNewsletter();

  const handlePublish = async () => {
    try {
      await publishMutation.mutateAsync(id);
    } catch (error) {
      console.error('Failed to publish newsletter:', error);
    }
  };

  const handleUnpublish = async () => {
    try {
      await unpublishMutation.mutateAsync(id);
    } catch (error) {
      console.error('Failed to unpublish newsletter:', error);
    }
  };

  const handleSend = async () => {
    try {
      await sendMutation.mutateAsync(id);
    } catch (error) {
      console.error('Failed to send newsletter:', error);
    }
  };

  const handleDelete = async () => {
    try {
      await deleteMutation.mutateAsync(id);
      router.push('/dashboard?tab=newsletters'); // Dashboard navigation is correct
    } catch (error) {
      console.error('Failed to delete newsletter:', error);
    }
  };

  const handleReactivate = async () => {
    try {
      await reactivateMutation.mutateAsync(id);
    } catch (error) {
      console.error('Failed to reactivate newsletter:', error);
    }
  };

  if (isLoading) {
    return (
      <div className="container mx-auto px-4 py-8">
        <div className="text-center py-12">
          <div className="inline-block w-8 h-8 border-4 border-gray-300 border-t-[#FF7900] rounded-full animate-spin"></div>
          <p className="mt-2 text-gray-600">Loading newsletter...</p>
        </div>
      </div>
    );
  }

  if (!newsletter) {
    return (
      <div className="container mx-auto px-4 py-8">
        <div className="text-center py-12">
          <Mail className="w-16 h-16 mx-auto mb-4 text-gray-400" />
          <p className="text-gray-600 text-lg">Newsletter not found</p>
          <Button onClick={() => router.push('/dashboard?tab=newsletters')} className="mt-4">
            Back to Newsletters
          </Button>
        </div>
      </div>
    );
  }

  const formatDate = (dateString: string): string => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  // Phase 6A.61+ Issue #8: Dynamic back button navigation
  const getBackUrl = (): string => {
    if (fromSource === 'event' && sourceEventId) {
      // Came from Event Communications tab - go back to event management
      return `/events/${sourceEventId}/manage?tab=communications`;
    }
    if (fromSource === 'newsletters') {
      // Came from public /newsletters page
      return '/newsletters';
    }
    // Default: go to dashboard newsletters tab
    return '/dashboard?tab=newsletters';
  };

  const getBackLabel = (): string => {
    if (fromSource === 'event') {
      return 'Back to Event Communications';
    }
    if (fromSource === 'newsletters') {
      return 'Back to Newsletters';
    }
    return 'Back to My Newsletters';
  };

  // Phase 6A.74 Part 10: Use helper functions for status comparison
  const isDraft = isNewsletterDraft(newsletter.status);
  const isActive = isNewsletterActive(newsletter.status);
  const isInactive = isNewsletterInactive(newsletter.status);
  const isSent = isNewsletterSent(newsletter.status);

  return (
    <div className="container mx-auto px-4 py-8 max-w-5xl">
      {/* Breadcrumb Navigation - Phase 6A.61+ Issue #8: Dynamic back navigation */}
      <div className="mb-6">
        <button
          onClick={() => router.push(getBackUrl())}
          className="flex items-center gap-2 text-gray-600 hover:text-gray-900 transition-colors"
        >
          <ArrowLeft className="w-4 h-4" />
          <span>{getBackLabel()}</span>
        </button>
      </div>

      {/* Header with Title and Status */}
      <div className="mb-8">
        <div className="flex items-start justify-between mb-4">
          <div className="flex-1">
            <h1 className="text-3xl font-bold text-[#8B1538] mb-2">{newsletter.title}</h1>
            <div className="flex items-center gap-3 text-sm text-gray-600">
              <span>Created {formatDate(newsletter.createdAt)}</span>
              <NewsletterStatusBadge status={newsletter.status} />
            </div>
          </div>
        </div>

        {/* Action Buttons - Phase 6A.74 Part 10: Using helper functions */}
        <div className="flex gap-2 flex-wrap">
          {/* Draft: Edit, Publish, Delete */}
          {isDraft && (
            <>
              <Button
                onClick={() => router.push(`/dashboard/my-newsletters/${id}/edit`)}
                variant="outline"
              >
                <Edit className="w-4 h-4 mr-2" />
                Edit
              </Button>
              <Button
                onClick={handlePublish}
                disabled={publishMutation.isPending}
                className="bg-[#FF7900] hover:bg-[#E66D00] text-white"
              >
                <Upload className="w-4 h-4 mr-2" />
                {publishMutation.isPending ? 'Publishing...' : 'Publish'}
              </Button>
              <Button
                onClick={() => setShowDeleteDialog(true)}
                disabled={deleteMutation.isPending}
                variant="destructive"
              >
                <Trash2 className="w-4 h-4 mr-2" />
                {deleteMutation.isPending ? 'Deleting...' : 'Delete'}
              </Button>
            </>
          )}

          {/* Active: Edit, Send Email, Unpublish (Phase 6A.74 Part 14: unlimited sends) */}
          {isActive && (
            <>
              <Button
                onClick={() => router.push(`/dashboard/my-newsletters/${id}/edit`)}
                variant="outline"
              >
                <Edit className="w-4 h-4 mr-2" />
                Edit
              </Button>
              <Button
                onClick={() => setShowSendDialog(true)}
                disabled={sendMutation.isPending}
                className="bg-[#10B981] hover:bg-[#059669] text-white"
              >
                <Send className="w-4 h-4 mr-2" />
                {sendMutation.isPending ? 'Sending...' : 'Send Email'}
              </Button>
              {/* Phase 6A.74 Part 14: Don't show Unpublish for announcement-only newsletters */}
              {!newsletter.isAnnouncementOnly && (
                <Button
                  onClick={() => setShowUnpublishDialog(true)}
                  disabled={unpublishMutation.isPending}
                  variant="outline"
                  className="border-red-600 text-red-600 hover:bg-red-50"
                >
                  <XCircle className="w-4 h-4 mr-2" />
                  {unpublishMutation.isPending ? 'Unpublishing...' : 'Unpublish'}
                </Button>
              )}
            </>
          )}

          {/* Inactive: Reactivate (Phase 6A.74 Part 14: allow for all inactive newsletters) */}
          {isInactive && (
            <Button
              onClick={handleReactivate}
              disabled={reactivateMutation.isPending}
              className="bg-[#FF7900] hover:bg-[#E66D00] text-white"
            >
              <Upload className="w-4 h-4 mr-2" />
              {reactivateMutation.isPending ? 'Reactivating...' : 'Reactivate (Extend 1 Week)'}
            </Button>
          )}

          {/* Sent: View only */}
          {isSent && (
            <div className="text-sm text-gray-600 italic">
              Email sent on {newsletter.sentAt && formatDate(newsletter.sentAt)}
            </div>
          )}
        </div>
      </div>

      {/* Metadata Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-8">
        {/* Recipient Info Card */}
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <h2 className="text-lg font-semibold text-[#8B1538] mb-4">Recipients</h2>
          <div className="space-y-2">
            {newsletter.emailGroups && newsletter.emailGroups.length > 0 && (
              <div className="flex items-center gap-2">
                <Mail className="w-4 h-4 text-[#FF7900]" />
                <span className="text-sm">
                  {newsletter.emailGroups.length} Email {newsletter.emailGroups.length === 1 ? 'Group' : 'Groups'}
                </span>
              </div>
            )}
            {newsletter.targetAllLocations && (
              <div className="flex items-center gap-2">
                <MapPin className="w-4 h-4 text-purple-600" />
                <span className="text-sm">All Locations</span>
              </div>
            )}
            {newsletter.metroAreas && newsletter.metroAreas.length > 0 && (
              <div className="flex items-center gap-2">
                <MapPin className="w-4 h-4 text-indigo-600" />
                <span className="text-sm">
                  {newsletter.metroAreas.length} Metro {newsletter.metroAreas.length === 1 ? 'Area' : 'Areas'}
                </span>
              </div>
            )}
          </div>
        </div>

        {/* Event Linkage Card */}
        {newsletter.eventId && newsletter.eventTitle && (
          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <h2 className="text-lg font-semibold text-[#8B1538] mb-4">Linked Event</h2>
            <div className="space-y-2">
              <div className="flex items-center gap-2">
                <ExternalLink className="w-4 h-4 text-[#6366F1]" />
                <span className="text-sm font-medium">{newsletter.eventTitle}</span>
              </div>
              <a
                href={`/events/${newsletter.eventId}`}
                className="text-sm text-[#FF7900] hover:underline inline-flex items-center gap-1"
                target="_blank"
                rel="noopener noreferrer"
              >
                View Event Details
                <ExternalLink className="w-3 h-3" />
              </a>
            </div>
          </div>
        )}

        {/* Status Info Card */}
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <h2 className="text-lg font-semibold text-[#8B1538] mb-4">Status Information</h2>
          <div className="space-y-2 text-sm">
            {newsletter.publishedAt && (
              <div className="flex items-center gap-2">
                <Calendar className="w-4 h-4 text-[#6366F1]" />
                <span>Published: {formatDate(newsletter.publishedAt)}</span>
              </div>
            )}
            {newsletter.sentAt && (
              <div className="flex items-center gap-2">
                <Mail className="w-4 h-4 text-[#10B981]" />
                <span>Sent: {formatDate(newsletter.sentAt)}</span>
              </div>
            )}
            {isActive && newsletter.expiresAt && (
              <div className="flex items-center gap-2">
                <Calendar className="w-4 h-4 text-[#F59E0B]" />
                <span>Expires: {formatDate(newsletter.expiresAt)}</span>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Newsletter Content */}
      <div className="bg-white rounded-lg border border-gray-200 p-8">
        <h2 className="text-lg font-semibold text-[#8B1538] mb-6">Content</h2>
        <div
          className="prose prose-lg max-w-none"
          dangerouslySetInnerHTML={{ __html: newsletter.description }}
        />
      </div>

      {/* Phase 6A.74 Part 10 Issue #2: Confirmation Dialogs */}
      <ConfirmDialog
        open={showDeleteDialog}
        onOpenChange={setShowDeleteDialog}
        title="Delete Newsletter"
        description="Are you sure you want to delete this newsletter? This action cannot be undone."
        confirmLabel="Delete"
        onConfirm={handleDelete}
        variant="danger"
        isLoading={deleteMutation.isPending}
      />

      <ConfirmDialog
        open={showSendDialog}
        onOpenChange={setShowSendDialog}
        title="Send Newsletter"
        description="Are you sure you want to send this newsletter? This action cannot be undone. The email will be sent to all selected recipients."
        confirmLabel="Send Email"
        onConfirm={handleSend}
        variant="warning"
        isLoading={sendMutation.isPending}
      />

      <ConfirmDialog
        open={showUnpublishDialog}
        onOpenChange={setShowUnpublishDialog}
        title="Unpublish Newsletter"
        description="Are you sure you want to unpublish this newsletter? It will be reverted to Draft status and removed from public view."
        confirmLabel="Unpublish"
        onConfirm={handleUnpublish}
        variant="warning"
        isLoading={unpublishMutation.isPending}
      />
    </div>
  );
}
