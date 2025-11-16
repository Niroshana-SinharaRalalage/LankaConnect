/**
 * NotificationsList Component
 * Reusable notifications list for dashboard integration
 *
 * Features:
 * - Display notifications with type-based styling
 * - Loading, empty, and error states
 * - Time formatting (e.g., "5 minutes ago")
 * - Keyboard accessible
 * - Unread indicator
 * - Click handling
 */

import * as React from 'react';
import { cn } from '@/presentation/lib/utils';
import type {
  NotificationDto,
  NotificationType,
} from '@/infrastructure/api/types/notifications.types';
import { notificationTypeConfig } from '@/infrastructure/api/types/notifications.types';

export interface NotificationsListProps {
  notifications: NotificationDto[];
  onNotificationClick: (notificationId: string) => void;
  isLoading?: boolean;
  error?: Error | null;
  className?: string;
}

/**
 * Format timestamp to relative time string
 */
function formatTimeAgo(dateString: string): string {
  const date = new Date(dateString);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffMins = Math.floor(diffMs / 60000);
  const diffHours = Math.floor(diffMins / 60);
  const diffDays = Math.floor(diffHours / 24);

  if (diffMins < 1) return 'Just now';
  if (diffMins < 60) return `${diffMins} ${diffMins === 1 ? 'minute' : 'minutes'} ago`;
  if (diffHours < 24) return `${diffHours} ${diffHours === 1 ? 'hour' : 'hours'} ago`;
  if (diffDays < 7) return `${diffDays} ${diffDays === 1 ? 'day' : 'days'} ago`;
  return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

/**
 * NotificationsList Component
 */
export function NotificationsList({
  notifications,
  onNotificationClick,
  isLoading = false,
  error = null,
  className = '',
}: NotificationsListProps) {
  // Loading State
  if (isLoading) {
    return (
      <div className={cn('flex items-center justify-center py-12', className)}>
        <div className="text-center">
          <svg
            className="animate-spin h-12 w-12 mx-auto mb-4 text-[#FF7900]"
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
          >
            <circle
              className="opacity-25"
              cx="12"
              cy="12"
              r="10"
              stroke="currentColor"
              strokeWidth="4"
            />
            <path
              className="opacity-75"
              fill="currentColor"
              d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
            />
          </svg>
          <p className="text-gray-600">Loading notifications...</p>
        </div>
      </div>
    );
  }

  // Error State
  if (error) {
    return (
      <div className={cn('flex items-center justify-center py-12', className)}>
        <div className="text-center">
          <svg
            className="w-12 h-12 mx-auto mb-4 text-red-500"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
            xmlns="http://www.w3.org/2000/svg"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
            />
          </svg>
          <p className="text-red-600 font-medium mb-2">Failed to load notifications</p>
          <p className="text-gray-600 text-sm">{error.message}</p>
        </div>
      </div>
    );
  }

  // Empty State
  if (notifications.length === 0) {
    return (
      <div className={cn('flex items-center justify-center py-12', className)}>
        <div className="text-center">
          <svg
            className="w-16 h-16 mx-auto mb-4 text-gray-400"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
            xmlns="http://www.w3.org/2000/svg"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"
            />
          </svg>
          <p className="text-gray-600 font-medium mb-2">No notifications</p>
          <p className="text-gray-500 text-sm">You're all caught up!</p>
        </div>
      </div>
    );
  }

  // Notifications List
  return (
    <div className={cn('space-y-3', className)}>
      {notifications.map((notification) => {
        const config = notificationTypeConfig[notification.type as NotificationType];

        return (
          <div
            key={notification.id}
            role="button"
            tabIndex={0}
            className={cn(
              'p-4 rounded-lg border transition-all cursor-pointer',
              'hover:shadow-md hover:border-[#FF7900]',
              'bg-white border-gray-200',
              'focus:outline-none focus:ring-2 focus:ring-[#FF7900] focus:ring-offset-2'
            )}
            onClick={() => onNotificationClick(notification.id)}
            onKeyDown={(e) => {
              if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                onNotificationClick(notification.id);
              }
            }}
          >
            <div className="flex gap-4">
              {/* Icon */}
              <div
                className={cn(
                  'flex-shrink-0 w-12 h-12 rounded-full',
                  'flex items-center justify-center text-xl',
                  config.bgColor,
                  config.color
                )}
              >
                {config.icon}
              </div>

              {/* Content */}
              <div className="flex-1 min-w-0">
                <div className="flex items-start justify-between gap-4 mb-2">
                  <h3 className="font-semibold text-[#8B1538] text-base">
                    {notification.title}
                  </h3>
                  {!notification.isRead && (
                    <span className="flex-shrink-0 w-2 h-2 rounded-full bg-[#FF7900]" aria-label="Unread" />
                  )}
                </div>
                <p className="text-gray-700 text-sm mb-2 leading-relaxed">
                  {notification.message}
                </p>
                <div className="flex items-center gap-2">
                  <span
                    className={cn(
                      'inline-block px-2 py-0.5 rounded text-xs font-medium',
                      config.bgColor,
                      config.color
                    )}
                  >
                    {notification.type.replace(/([A-Z])/g, ' $1').trim()}
                  </span>
                  <span className="text-gray-500 text-xs">
                    {formatTimeAgo(notification.createdAt)}
                  </span>
                </div>
              </div>
            </div>
          </div>
        );
      })}
    </div>
  );
}
