import { apiClient } from '../client/api-client';
import type { NotificationDto } from '../types/notifications.types';

/**
 * NotificationsRepository
 * Handles all notification-related API calls
 * Phase 6A.6: Notification System
 */
export class NotificationsRepository {
  private readonly basePath = '/notifications';

  /**
   * Get unread notifications for the current user
   */
  async getUnreadNotifications(): Promise<NotificationDto[]> {
    const response = await apiClient.get<NotificationDto[]>(
      `${this.basePath}/unread`
    );
    return response;
  }

  /**
   * Mark a notification as read
   */
  async markAsRead(notificationId: string): Promise<void> {
    await apiClient.post(`${this.basePath}/${notificationId}/read`);
  }

  /**
   * Mark all notifications as read
   */
  async markAllAsRead(): Promise<void> {
    await apiClient.post(`${this.basePath}/read-all`);
  }
}

// Export singleton instance
export const notificationsRepository = new NotificationsRepository();
