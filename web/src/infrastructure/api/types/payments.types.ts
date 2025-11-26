/**
 * Stripe Payment Types
 * Phase 6A.4: Stripe payment integration for subscription management
 */

/**
 * Request to create a Stripe Checkout session
 */
export interface CreateCheckoutSessionRequest {
  priceId: string;
  successUrl: string;
  cancelUrl: string;
}

/**
 * Response from creating a Stripe Checkout session
 */
export interface CreateCheckoutSessionResponse {
  sessionId: string;
  sessionUrl: string;
}

/**
 * Request to create a Stripe Customer Portal session
 */
export interface CreatePortalSessionRequest {
  returnUrl: string;
}

/**
 * Response from creating a Stripe Customer Portal session
 */
export interface CreatePortalSessionResponse {
  sessionUrl: string;
}

/**
 * Stripe configuration response
 */
export interface StripeConfigResponse {
  publishableKey: string;
}

/**
 * Pricing tier enum
 */
export enum PricingTier {
  General = 'General',
  EventOrganizer = 'EventOrganizer',
}

/**
 * Billing interval enum
 */
export enum BillingInterval {
  Monthly = 'monthly',
  Annual = 'annual',
}
