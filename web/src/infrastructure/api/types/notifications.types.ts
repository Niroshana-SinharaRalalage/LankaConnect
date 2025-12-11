/**
 * Notification API Types
 * Phase 6A.6: Notification System
 */

export interface NotificationDto {
  id: string;
  title: string;
  message: string;
  type: NotificationType;
  isRead: boolean;
  readAt: string | null;
  createdAt: string;
  relatedEntityId: string | null;
  relatedEntityType: string | null;
}

export type NotificationType =
  | 'RoleUpgradeApproved'
  | 'RoleUpgradeRejected'
  | 'FreeTrialExpiring'
  | 'FreeTrialExpired'
  | 'SubscriptionPaymentSucceeded'
  | 'SubscriptionPaymentFailed'
  | 'System'
  | 'Event';

export interface NotificationTypeConfig {
  icon: string;
  color: string;
  bgColor: string;
}

export const notificationTypeConfig: Record<NotificationType, NotificationTypeConfig> = {
  RoleUpgradeApproved: {
    icon: '✓',
    color: 'text-green-600',
    bgColor: 'bg-green-50',
  },
  RoleUpgradeRejected: {
    icon: '✗',
    color: 'text-red-600',
    bgColor: 'bg-red-50',
  },
  FreeTrialExpiring: {
    icon: '⏰',
    color: 'text-orange-600',
    bgColor: 'bg-orange-50',
  },
  FreeTrialExpired: {
    icon: '⏱',
    color: 'text-red-600',
    bgColor: 'bg-red-50',
  },
  SubscriptionPaymentSucceeded: {
    icon: '✓',
    color: 'text-green-600',
    bgColor: 'bg-green-50',
  },
  SubscriptionPaymentFailed: {
    icon: '!',
    color: 'text-red-600',
    bgColor: 'bg-red-50',
  },
  System: {
    icon: 'ℹ',
    color: 'text-blue-600',
    bgColor: 'bg-blue-50',
  },
  Event: {
    icon: '📅',
    color: 'text-purple-600',
    bgColor: 'bg-purple-50',
  },
};
