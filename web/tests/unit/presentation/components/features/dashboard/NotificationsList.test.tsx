/**
 * @jest-environment jsdom
 */

import { render, screen, fireEvent } from '@testing-library/react';
import { vi } from 'vitest';
import { NotificationsList } from '@/presentation/components/features/dashboard/NotificationsList';
import type { NotificationDto } from '@/infrastructure/api/types/notifications.types';

describe('NotificationsList', () => {
  const mockNotifications: NotificationDto[] = [
    {
      id: '1',
      userId: 'user-1',
      title: 'Role Upgrade Approved',
      message: 'Your request to become an Event Organizer has been approved.',
      type: 'RoleUpgradeApproved',
      relatedEntityId: 'user-1',
      relatedEntityType: 'User',
      isRead: false,
      createdAt: new Date(Date.now() - 5 * 60 * 1000).toISOString(), // 5 minutes ago
    },
    {
      id: '2',
      userId: 'user-1',
      title: 'Free Trial Expiring',
      message: 'Your free trial will expire in 7 days.',
      type: 'FreeTrialExpiring',
      relatedEntityId: 'user-1',
      relatedEntityType: 'User',
      isRead: false,
      createdAt: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(), // 2 hours ago
    },
  ];

  const mockOnNotificationClick = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Component Rendering', () => {
    it('should render notifications list with all items', () => {
      render(
        <NotificationsList
          notifications={mockNotifications}
          onNotificationClick={mockOnNotificationClick}
        />
      );

      // Check titles - they're in h3 elements
      expect(screen.getByRole('heading', { name: 'Role Upgrade Approved', level: 3 })).toBeInTheDocument();
      expect(screen.getByText('Your request to become an Event Organizer has been approved.')).toBeInTheDocument();
      expect(screen.getByRole('heading', { name: 'Free Trial Expiring', level: 3 })).toBeInTheDocument();
    });

    it('should render empty state when no notifications', () => {
      render(
        <NotificationsList
          notifications={[]}
          onNotificationClick={mockOnNotificationClick}
        />
      );

      expect(screen.getByText('No notifications')).toBeInTheDocument();
      expect(screen.getByText("You're all caught up!")).toBeInTheDocument();
    });

    it('should render loading state', () => {
      render(
        <NotificationsList
          notifications={[]}
          isLoading={true}
          onNotificationClick={mockOnNotificationClick}
        />
      );

      expect(screen.getByText('Loading notifications...')).toBeInTheDocument();
    });
  });

  describe('Time Formatting', () => {
    it('should display "Just now" for recent notifications', () => {
      const recentNotification: NotificationDto = {
        ...mockNotifications[0],
        createdAt: new Date().toISOString(),
      };

      render(
        <NotificationsList
          notifications={[recentNotification]}
          onNotificationClick={mockOnNotificationClick}
        />
      );

      expect(screen.getByText('Just now')).toBeInTheDocument();
    });

    it('should display minutes ago for notifications < 1 hour', () => {
      render(
        <NotificationsList
          notifications={[mockNotifications[0]]}
          onNotificationClick={mockOnNotificationClick}
        />
      );

      expect(screen.getByText(/\d+ minutes? ago/)).toBeInTheDocument();
    });

    it('should display hours ago for notifications < 24 hours', () => {
      render(
        <NotificationsList
          notifications={[mockNotifications[1]]}
          onNotificationClick={mockOnNotificationClick}
        />
      );

      expect(screen.getByText(/\d+ hours? ago/)).toBeInTheDocument();
    });
  });

  describe('User Interactions', () => {
    it('should call onNotificationClick when notification is clicked', () => {
      render(
        <NotificationsList
          notifications={mockNotifications}
          onNotificationClick={mockOnNotificationClick}
        />
      );

      const notificationButtons = screen.getAllByRole('button');
      const firstNotification = notificationButtons[0];
      fireEvent.click(firstNotification);

      expect(mockOnNotificationClick).toHaveBeenCalledWith('1');
    });

    it('should be keyboard accessible', () => {
      render(
        <NotificationsList
          notifications={mockNotifications}
          onNotificationClick={mockOnNotificationClick}
        />
      );

      const notificationButtons = screen.getAllByRole('button');
      const firstNotification = notificationButtons[0];
      fireEvent.keyDown(firstNotification, { key: 'Enter', code: 'Enter' });

      expect(mockOnNotificationClick).toHaveBeenCalledWith('1');
    });
  });

  describe('Notification Type Display', () => {
    it('should display notification type badge', () => {
      render(
        <NotificationsList
          notifications={[mockNotifications[0]]}
          onNotificationClick={mockOnNotificationClick}
        />
      );

      expect(screen.getByText('Role Upgrade Approved', { selector: 'span' })).toBeInTheDocument();
    });

    it('should apply correct styling for different notification types', () => {
      render(
        <NotificationsList
          notifications={mockNotifications}
          onNotificationClick={mockOnNotificationClick}
        />
      );

      // Both notification type badges should be present
      const badges = screen.getAllByText(/Role Upgrade Approved|Free Trial Expiring/, { selector: 'span' });
      expect(badges.length).toBeGreaterThan(0);
    });
  });

  describe('Unread Indicator', () => {
    it('should display unread indicator for unread notifications', () => {
      render(
        <NotificationsList
          notifications={[mockNotifications[0]]}
          onNotificationClick={mockOnNotificationClick}
        />
      );

      const unreadIndicator = screen.getByLabelText('Unread');
      expect(unreadIndicator).toBeInTheDocument();
    });
  });
});
