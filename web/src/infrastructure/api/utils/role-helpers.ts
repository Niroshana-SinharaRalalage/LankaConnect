import { UserRole } from '../types/auth.types';
import { SubscriptionStatus } from '../types/subscription.types';

/**
 * Role Helper Functions
 * Phase 6A.1: Utility functions for role-based logic
 */

/**
 * Display names for user roles
 */
export function getRoleDisplayName(role: UserRole): string {
  switch (role) {
    case UserRole.GeneralUser:
      return 'General User';
    case UserRole.EventOrganizer:
      return 'Event Organizer';
    case UserRole.Admin:
      return 'Administrator';
    case UserRole.AdminManager:
      return 'Admin Manager';
    default:
      return role;
  }
}

/**
 * Check if user can manage other users
 */
export function canManageUsers(role: UserRole): boolean {
  return role === UserRole.Admin || role === UserRole.AdminManager;
}

/**
 * Check if user can create events (requires Event Organizer role + active subscription)
 */
export function canCreateEvents(role: UserRole, subscriptionStatus?: SubscriptionStatus): boolean {
  // General Users cannot create events
  if (role === UserRole.GeneralUser) {
    return false;
  }

  // Admins can always create events
  if (isAdmin(role)) {
    return true;
  }

  // Event Organizers must have active subscription (trialing or paid)
  if (role === UserRole.EventOrganizer) {
    if (!subscriptionStatus) {
      return false;
    }
    return canCreateEventsWithSubscription(subscriptionStatus);
  }

  return false;
}

/**
 * Check if user can moderate content
 */
export function canModerateContent(role: UserRole): boolean {
  return role === UserRole.Admin || role === UserRole.AdminManager;
}

/**
 * Check if role is Event Organizer
 */
export function isEventOrganizer(role: UserRole): boolean {
  return role === UserRole.EventOrganizer;
}

/**
 * Check if role is Admin (Admin or AdminManager)
 */
export function isAdmin(role: UserRole): boolean {
  return role === UserRole.Admin || role === UserRole.AdminManager;
}

/**
 * Check if role requires subscription
 */
export function requiresSubscription(role: UserRole): boolean {
  return role === UserRole.EventOrganizer;
}

/**
 * Get subscription status display name
 */
export function getSubscriptionStatusDisplay(status: SubscriptionStatus): string {
  switch (status) {
    case SubscriptionStatus.None:
      return 'No Subscription';
    case SubscriptionStatus.Trialing:
      return 'Free Trial';
    case SubscriptionStatus.Active:
      return 'Active';
    case SubscriptionStatus.PastDue:
      return 'Past Due';
    case SubscriptionStatus.Canceled:
      return 'Canceled';
    case SubscriptionStatus.Expired:
      return 'Expired';
    default:
      return status;
  }
}

/**
 * Check if subscription allows creating events
 */
export function canCreateEventsWithSubscription(status: SubscriptionStatus): boolean {
  return status === SubscriptionStatus.Trialing || status === SubscriptionStatus.Active;
}

/**
 * Check if subscription requires payment
 */
export function requiresPayment(status: SubscriptionStatus): boolean {
  return status === SubscriptionStatus.PastDue || status === SubscriptionStatus.Expired;
}

/**
 * Check if subscription is active
 */
export function isSubscriptionActive(status: SubscriptionStatus): boolean {
  return status === SubscriptionStatus.Trialing || status === SubscriptionStatus.Active;
}

/**
 * Calculate days remaining in free trial
 */
export function getFreeTrialDaysRemaining(freeTrialEndsAt?: string): number | null {
  if (!freeTrialEndsAt) {
    return null;
  }

  const endDate = new Date(freeTrialEndsAt);
  const now = new Date();
  const daysRemaining = Math.ceil((endDate.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));

  return daysRemaining > 0 ? daysRemaining : 0;
}
