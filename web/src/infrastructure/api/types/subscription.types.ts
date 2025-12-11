/**
 * Subscription Types
 * Phase 6A.1: Subscription management for Event Organizer accounts
 */

export enum SubscriptionStatus {
  None = 'None',
  Trialing = 'Trialing',
  Active = 'Active',
  PastDue = 'PastDue',
  Canceled = 'Canceled',
  Expired = 'Expired',
}

export interface SubscriptionInfo {
  status: SubscriptionStatus;
  freeTrialStartedAt?: string;
  freeTrialEndsAt?: string;
  subscriptionActivatedAt?: string;
  subscriptionCanceledAt?: string;
  stripeCustomerId?: string;
  stripeSubscriptionId?: string;
}
