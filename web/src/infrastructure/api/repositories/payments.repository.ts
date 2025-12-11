import { apiClient } from '../client/api-client';
import type {
  CreateCheckoutSessionRequest,
  CreateCheckoutSessionResponse,
  CreatePortalSessionRequest,
  CreatePortalSessionResponse,
  StripeConfigResponse,
} from '../types/payments.types';

/**
 * PaymentsRepository
 * Handles all Stripe payment-related API calls
 * Repository pattern for payment operations
 * Phase 6A.4: Stripe Payment Integration
 */
export class PaymentsRepository {
  private readonly basePath = '/payments';

  /**
   * Get Stripe publishable key for client-side initialization
   */
  async getStripeConfig(): Promise<StripeConfigResponse> {
    const response = await apiClient.get<StripeConfigResponse>(
      `${this.basePath}/config`
    );
    return response;
  }

  /**
   * Create a Stripe Checkout session for subscription upgrade
   */
  async createCheckoutSession(
    request: CreateCheckoutSessionRequest
  ): Promise<CreateCheckoutSessionResponse> {
    const response = await apiClient.post<CreateCheckoutSessionResponse>(
      `${this.basePath}/create-checkout-session`,
      request
    );
    return response;
  }

  /**
   * Create a Stripe Customer Portal session for subscription management
   */
  async createPortalSession(
    request: CreatePortalSessionRequest
  ): Promise<CreatePortalSessionResponse> {
    const response = await apiClient.post<CreatePortalSessionResponse>(
      `${this.basePath}/create-portal-session`,
      request
    );
    return response;
  }
}

// Export singleton instance
export const paymentsRepository = new PaymentsRepository();
