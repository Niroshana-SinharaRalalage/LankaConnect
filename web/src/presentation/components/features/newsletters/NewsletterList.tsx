'use client';

import * as React from 'react';
import { Mail, Edit, Upload, Send, Trash2 } from 'lucide-react';
import { NewsletterDto } from '@/infrastructure/api/types/newsletters.types';
import { NewsletterCard } from './NewsletterCard';
import { Button } from '@/presentation/components/ui/Button';
import {
  isNewsletterDraft,
  isNewsletterActive,
  isNewsletterInactive,
  isNewsletterSent,
} from '@/lib/enum-utils';

export interface NewsletterListProps {
  newsletters: NewsletterDto[];
  isLoading?: boolean;
  emptyMessage?: string;
  onNewsletterClick?: (newsletterId: string) => void;
  onEditNewsletter?: (newsletterId: string) => void;
  onPublishNewsletter?: (newsletterId: string) => Promise<void>;
  onSendNewsletter?: (newsletterId: string) => Promise<void>;
  onDeleteNewsletter?: (newsletterId: string) => Promise<void>;
}

/**
 * NewsletterList Component
 * Displays list of newsletters with status-based action buttons
 * Follows EventsList pattern but simplified for newsletter management
 * Phase 6A.74: Newsletter Feature
 * Phase 6A.74 Part 10: Fixed status comparison for string/enum handling
 * Phase 6A.86: Added visual feedback for newsletters being sent in background
 */
export function NewsletterList({
  newsletters,
  isLoading = false,
  emptyMessage = 'No newsletters to display',
  onNewsletterClick,
  onEditNewsletter,
  onPublishNewsletter,
  onSendNewsletter,
  onDeleteNewsletter,
}: NewsletterListProps) {
  // Loading states for each action
  const [publishingId, setPublishingId] = React.useState<string | null>(null);
  const [sendingId, setSendingId] = React.useState<string | null>(null);
  const [deletingId, setDeletingId] = React.useState<string | null>(null);

  // Phase 6A.86: Track recently sent newsletters for visual feedback
  // Keep newsletters in "sending" state for 10 seconds to show banner
  const [recentlySent, setRecentlySent] = React.useState<Set<string>>(new Set());

  // Action handlers with loading states
  const handlePublish = async (newsletterId: string, e: React.MouseEvent) => {
    e.stopPropagation();
    if (!onPublishNewsletter || publishingId) return;

    setPublishingId(newsletterId);
    try {
      await onPublishNewsletter(newsletterId);
    } finally {
      setPublishingId(null);
    }
  };

  const handleSend = async (newsletterId: string, e: React.MouseEvent) => {
    e.stopPropagation();
    if (!onSendNewsletter || sendingId) return;

    setSendingId(newsletterId);
    try {
      await onSendNewsletter(newsletterId);

      // Phase 6A.86: Mark as recently sent for visual feedback
      setRecentlySent((prev) => new Set([...prev, newsletterId]));

      // Remove from recently sent after 10 seconds
      setTimeout(() => {
        setRecentlySent((prev) => {
          const next = new Set(prev);
          next.delete(newsletterId);
          return next;
        });
      }, 10000); // 10 seconds
    } finally {
      setSendingId(null);
    }
  };

  const handleDelete = async (newsletterId: string, e: React.MouseEvent) => {
    e.stopPropagation();
    if (!onDeleteNewsletter || deletingId) return;

    setDeletingId(newsletterId);
    try {
      await onDeleteNewsletter(newsletterId);
    } finally {
      setDeletingId(null);
    }
  };

  // Loading state (EventsList pattern lines 153-165)
  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="text-center">
          <div
            className="inline-block w-8 h-8 border-4 border-gray-300 border-t-[#FF7900] rounded-full animate-spin"
            role="status"
          ></div>
          <p className="mt-2 text-gray-600">Loading newsletters...</p>
        </div>
      </div>
    );
  }

  // Empty state (EventsList pattern lines 167-174)
  if (newsletters.length === 0) {
    return (
      <div className="text-center py-12">
        <Mail className="w-16 h-16 mx-auto mb-4 text-gray-400" />
        <p className="text-gray-600 text-lg">{emptyMessage}</p>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {newsletters.map((newsletter) => {
        // Phase 6A.74 Part 10: Use helper functions for status comparison
        const isDraft = isNewsletterDraft(newsletter.status);
        const isActive = isNewsletterActive(newsletter.status);
        const isInactive = isNewsletterInactive(newsletter.status);
        const isSent = isNewsletterSent(newsletter.status);

        // Determine action buttons based on status
        const actionButtons = (
          <>
            {/* Draft: Edit, Publish, Delete */}
            {isDraft && (
              <>
                {onEditNewsletter && (
                  <Button
                    onClick={(e) => {
                      e.stopPropagation();
                      onEditNewsletter(newsletter.id);
                    }}
                    size="sm"
                    variant="outline"
                    className="text-xs"
                  >
                    <Edit className="w-3 h-3 mr-1" />
                    Edit
                  </Button>
                )}
                {onPublishNewsletter && (
                  <Button
                    onClick={(e) => handlePublish(newsletter.id, e)}
                    disabled={publishingId === newsletter.id}
                    size="sm"
                    className="text-xs bg-[#FF7900] hover:bg-[#E66D00] text-white"
                  >
                    <Upload className="w-3 h-3 mr-1" />
                    {publishingId === newsletter.id ? 'Publishing...' : 'Publish'}
                  </Button>
                )}
                {onDeleteNewsletter && (
                  <Button
                    onClick={(e) => handleDelete(newsletter.id, e)}
                    disabled={deletingId === newsletter.id}
                    size="sm"
                    variant="destructive"
                    className="text-xs"
                  >
                    <Trash2 className="w-3 h-3 mr-1" />
                    {deletingId === newsletter.id ? 'Deleting...' : 'Delete'}
                  </Button>
                )}
              </>
            )}

            {/* Active: Edit, Send Email (unlimited sends - Phase 6A.74 Part 14) */}
            {isActive && (
              <>
                {onEditNewsletter && (
                  <Button
                    onClick={(e) => {
                      e.stopPropagation();
                      onEditNewsletter(newsletter.id);
                    }}
                    size="sm"
                    variant="outline"
                    className="text-xs"
                  >
                    <Edit className="w-3 h-3 mr-1" />
                    Edit
                  </Button>
                )}
                {onSendNewsletter && (
                  <Button
                    onClick={(e) => handleSend(newsletter.id, e)}
                    disabled={sendingId === newsletter.id}
                    size="sm"
                    className="text-xs bg-[#10B981] hover:bg-[#059669] text-white"
                  >
                    <Send className="w-3 h-3 mr-1" />
                    {sendingId === newsletter.id ? 'Sending...' : 'Send Email'}
                  </Button>
                )}
              </>
            )}

            {/* Sent: No actions (just display sent date) */}
            {isSent && (
              <span className="text-xs text-gray-500 italic">
                Email sent
              </span>
            )}

            {/* Inactive: Show info (reactivate button not implemented yet) */}
            {isInactive && (
              <span className="text-xs text-gray-500 italic">
                Inactive newsletter
              </span>
            )}
          </>
        );

        return (
          <NewsletterCard
            key={newsletter.id}
            newsletter={newsletter}
            onClick={onNewsletterClick ? () => onNewsletterClick(newsletter.id) : undefined}
            actionButtons={actionButtons}
            isSending={recentlySent.has(newsletter.id)} // Phase 6A.86: Show sending banner
          />
        );
      })}
    </div>
  );
}
