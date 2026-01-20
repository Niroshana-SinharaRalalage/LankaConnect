'use client';

import * as React from 'react';
import { Calendar, Mail, MapPin, ExternalLink } from 'lucide-react';
import { NewsletterDto } from '@/infrastructure/api/types/newsletters.types';
import { isNewsletterActive } from '@/lib/enum-utils';
import { NewsletterStatusBadge } from './NewsletterStatusBadge';

export interface NewsletterCardProps {
  newsletter: NewsletterDto;
  onClick?: () => void;
  actionButtons?: React.ReactNode;
}

/**
 * NewsletterCard Component
 * Displays individual newsletter with status, metadata, and action buttons
 * Phase 6A.74: Newsletter Feature
 * Phase 6A.74 Part 10: Fixed HTML tag stripping for description preview
 */
export function NewsletterCard({ newsletter, onClick, actionButtons }: NewsletterCardProps) {
  const formatDate = (dateString: string): string => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: '2-digit',
    });
  };

  // Strip HTML tags from description for preview
  const getPlainTextExcerpt = (html: string, maxLength: number = 150): string => {
    const text = html.replace(/<[^>]*>/g, ' ').replace(/\s+/g, ' ').trim();
    return text.length > maxLength ? text.substring(0, maxLength) + '...' : text;
  };

  /**
   * Phase 6A.74 Part 13+: Build recipient breakdown string showing all 4 sources
   * Format: "(X from newsletter groups, Y from event groups, Z subscribers, W event registrations)"
   * Only includes sources with count > 0
   */
  const buildRecipientBreakdown = (): string | null => {
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
  };

  return (
    <div
      onClick={onClick}
      className={`bg-white rounded-lg shadow-sm border border-gray-200 p-4 transition-all hover:shadow-md ${
        onClick ? 'cursor-pointer' : ''
      }`}
    >
      {/* Header */}
      <div className="flex items-start justify-between mb-2">
        <h3 className="text-lg font-semibold text-[#8B1538] flex-1 line-clamp-2 pr-4">
          {newsletter.title}
        </h3>
        <NewsletterStatusBadge status={newsletter.status} />
      </div>

      {/* Description - Phase 6A.74 Part 10: Strip HTML tags for preview */}
      <p className="text-sm text-gray-600 line-clamp-2 mb-3">
        {getPlainTextExcerpt(newsletter.description)}
      </p>

      {/* Metadata */}
      <div className="space-y-2 mb-3">
        {/* Created Date */}
        <div className="flex items-center text-xs text-gray-600">
          <Calendar className="w-3 h-3 mr-2 text-[#FF7900]" />
          <span>Created {formatDate(newsletter.createdAt)}</span>
        </div>

        {/* Sent Date */}
        {newsletter.sentAt && (
          <div className="flex items-center text-xs text-gray-600">
            <Mail className="w-3 h-3 mr-2 text-[#10B981]" />
            <span>Sent {formatDate(newsletter.sentAt)}</span>
          </div>
        )}

        {/* Phase 6A.74 Part 13+: Recipient Count with detailed breakdown and send stats */}
        {newsletter.totalRecipientCount != null && newsletter.totalRecipientCount > 0 && (
          <div className="flex items-center flex-wrap text-xs text-gray-600">
            <Mail className="w-3 h-3 mr-2 text-[#6366F1]" />
            <span>
              {newsletter.totalRecipientCount.toLocaleString()} recipient{newsletter.totalRecipientCount === 1 ? '' : 's'}
              {buildRecipientBreakdown() && (
                <span className="text-gray-500"> {buildRecipientBreakdown()}</span>
              )}
            </span>
            {/* Success/Failed counts */}
            {newsletter.successfulSends != null && (
              <span className="text-green-600 ml-2">
                ✓ {newsletter.successfulSends} sent
              </span>
            )}
            {newsletter.failedSends != null && newsletter.failedSends > 0 && (
              <span className="text-red-600 ml-2">
                ✗ {newsletter.failedSends} failed
              </span>
            )}
          </div>
        )}

        {/* Expires Date (Active only) */}
        {isNewsletterActive(newsletter.status) && newsletter.expiresAt && (
          <div className="flex items-center text-xs text-gray-600">
            <Calendar className="w-3 h-3 mr-2 text-[#F59E0B]" />
            <span>Expires {formatDate(newsletter.expiresAt)}</span>
          </div>
        )}

        {/* Linked Event */}
        {newsletter.eventTitle && (
          <div className="flex items-center text-xs text-gray-600">
            <ExternalLink className="w-3 h-3 mr-2 text-[#6366F1]" />
            <span>Event: {newsletter.eventTitle}</span>
          </div>
        )}
      </div>

      {/* Footer */}
      <div className="flex items-center justify-between pt-3 border-t border-gray-200">
        {/* Recipient Info */}
        <div className="flex gap-2 flex-wrap">
          {newsletter.emailGroups && newsletter.emailGroups.length > 0 && (
            <span className="px-2 py-1 bg-gray-100 text-gray-700 rounded text-xs font-medium">
              {newsletter.emailGroups.length} Email {newsletter.emailGroups.length === 1 ? 'Group' : 'Groups'}
            </span>
          )}
          {newsletter.targetAllLocations && (
            <span className="px-2 py-1 bg-purple-100 text-purple-700 rounded text-xs font-medium">
              All Locations
            </span>
          )}
          {newsletter.metroAreas && newsletter.metroAreas.length > 0 && (
            <span className="px-2 py-1 bg-indigo-100 text-indigo-700 rounded text-xs font-medium">
              <MapPin className="w-3 h-3 inline mr-1" />
              {newsletter.metroAreas.length} {newsletter.metroAreas.length === 1 ? 'Metro' : 'Metros'}
            </span>
          )}
        </div>

        {/* Action Buttons */}
        {actionButtons && (
          <div className="flex gap-2 flex-wrap">
            {actionButtons}
          </div>
        )}
      </div>
    </div>
  );
}
