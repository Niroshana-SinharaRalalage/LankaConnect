'use client';

import * as React from 'react';
import Link from 'next/link';
import { cn } from '@/presentation/lib/utils';
import type {
  NotificationDto,
  NotificationType,
} from '@/infrastructure/api/types/notifications.types';
import { notificationTypeConfig } from '@/infrastructure/api/types/notifications.types';
import {
  useMarkNotificationAsRead,
  useMarkAllNotificationsAsRead,
} from '@/presentation/hooks/useNotifications';

export interface NotificationDropdownProps {
  notifications: NotificationDto[];
  isOpen: boolean;
  onClose: () => void;
  className?: string;
}

/**
 * NotificationDropdown Component
 * Phase 6A.6: Notification System
 *
 * Dropdown showing list of unread notifications
 *
 * Features:
 * - List of unread notifications with icons
 * - Click to mark individual notification as read
 * - "Mark all as read" button
 * - Link to full notifications inbox page
 * - Auto-close on outside click
 * - Accessible with keyboard navigation
 * - Sri Lankan flag colors for styling
 *
 * @example
 * ```tsx
 * <NotificationDropdown
 *   notifications={unreadNotifications}
 *   isOpen={dropdownOpen}
 *   onClose={() => setDropdownOpen(false)}
 * />
 * ```
 */
export function NotificationDropdown({
  notifications,
  isOpen,
  onClose,
  className = '',
}: NotificationDropdownProps) {
  const dropdownRef = React.useRef<HTMLDivElement>(null);
  const markAsRead = useMarkNotificationAsRead();
  const markAllAsRead = useMarkAllNotificationsAsRead();

  // Close dropdown on outside click
  React.useEffect(() => {
    if (!isOpen) return;

    const handleClickOutside = (event: MouseEvent) => {
      if (
        dropdownRef.current &&
        !dropdownRef.current.contains(event.target as Node)
      ) {
        onClose();
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [isOpen, onClose]);

  // Close on Escape key
  React.useEffect(() => {
    if (!isOpen) return;

    const handleEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        onClose();
      }
    };

    document.addEventListener('keydown', handleEscape);
    return () => document.removeEventListener('keydown', handleEscape);
  }, [isOpen, onClose]);

  const handleNotificationClick = async (notificationId: string) => {
    try {
      await markAsRead.mutateAsync(notificationId);
    } catch (error) {
      console.error('Failed to mark notification as read:', error);
    }
  };

  const handleMarkAllAsRead = async () => {
    try {
      await markAllAsRead.mutateAsync();
      onClose();
    } catch (error) {
      console.error('Failed to mark all notifications as read:', error);
    }
  };

  const formatTimeAgo = (dateString: string): string => {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMins / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;
    return date.toLocaleDateString();
  };

  if (!isOpen) return null;

  return (
    <div
      ref={dropdownRef}
      className={cn(
        'absolute right-0 mt-2 w-80 sm:w-96',
        'bg-white rounded-lg shadow-lg',
        'border border-gray-200',
        'z-50',
        'animate-[dropdown-fade-in_0.2s_ease-out]',
        className
      )}
      role="menu"
      aria-label="Notifications menu"
    >
      {/* Header */}
      <div className="flex items-center justify-between px-4 py-3 border-b border-gray-200">
        <h3 className="text-lg font-semibold text-[#8B1538]">Notifications</h3>
        {notifications.length > 0 && (
          <button
            type="button"
            className={cn(
              'text-sm text-[#FF7900] font-medium',
              'hover:text-[#E66D00] transition-colors',
              'focus:outline-none focus:underline'
            )}
            onClick={handleMarkAllAsRead}
            disabled={markAllAsRead.isPending}
          >
            {markAllAsRead.isPending ? 'Marking...' : 'Mark all as read'}
          </button>
        )}
      </div>

      {/* Notifications List */}
      <div className="max-h-96 overflow-y-auto">
        {notifications.length === 0 ? (
          <div className="px-4 py-8 text-center text-gray-500">
            <svg
              className="w-12 h-12 mx-auto mb-3 text-gray-400"
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
            <p className="font-medium">No new notifications</p>
            <p className="text-sm mt-1">You're all caught up!</p>
          </div>
        ) : (
          <ul className="divide-y divide-gray-100">
            {notifications.map((notification) => {
              const config = notificationTypeConfig[notification.type as NotificationType];
              return (
                <li key={notification.id}>
                  <button
                    type="button"
                    className={cn(
                      'w-full px-4 py-3 text-left',
                      'hover:bg-gray-50 transition-colors',
                      'focus:outline-none focus:bg-gray-50'
                    )}
                    onClick={() => handleNotificationClick(notification.id)}
                    disabled={markAsRead.isPending}
                  >
                    <div className="flex gap-3">
                      {/* Icon */}
                      <div
                        className={cn(
                          'flex-shrink-0 w-10 h-10 rounded-full',
                          'flex items-center justify-center text-lg',
                          config.bgColor,
                          config.color
                        )}
                      >
                        {config.icon}
                      </div>

                      {/* Content */}
                      <div className="flex-1 min-w-0">
                        <p className="text-sm font-semibold text-[#333] truncate">
                          {notification.title}
                        </p>
                        <p className="text-sm text-gray-600 mt-1 line-clamp-2">
                          {notification.message}
                        </p>
                        <p className="text-xs text-gray-500 mt-1">
                          {formatTimeAgo(notification.createdAt)}
                        </p>
                      </div>

                      {/* Unread indicator */}
                      <div className="flex-shrink-0">
                        <span
                          className="inline-block w-2 h-2 rounded-full bg-[#FF7900]"
                          aria-label="Unread"
                        />
                      </div>
                    </div>
                  </button>
                </li>
              );
            })}
          </ul>
        )}
      </div>

      {/* Footer */}
      {notifications.length > 0 && (
        <div className="px-4 py-3 border-t border-gray-200">
          <Link
            href="/notifications"
            className={cn(
              'block w-full text-center text-sm font-medium',
              'text-[#8B1538] hover:text-[#70112D]',
              'py-2 rounded-md hover:bg-gray-50',
              'transition-colors'
            )}
            onClick={onClose}
          >
            View all notifications
          </Link>
        </div>
      )}
    </div>
  );
}

/**
 * Add this animation to your global CSS or tailwind.config.ts:
 *
 * @keyframes dropdown-fade-in {
 *   from {
 *     opacity: 0;
 *     transform: translateY(-10px);
 *   }
 *   to {
 *     opacity: 1;
 *     transform: translateY(0);
 *   }
 * }
 */
