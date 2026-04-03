import { apiClient } from '../client/api-client';
import type {
  WhatsAppPreferencesDto,
  EnableWhatsAppRequest,
  EnableWhatsAppResponse,
  VerifyPhoneRequest,
  VerifyWhatsAppPhoneResponse,
  UpdateWhatsAppPreferencesRequest,
  WhatsAppMetricsDto,
  WhatsAppTemplateDto,
  WhatsAppMessageDto,
  SendTestWhatsAppRequest,
  SendTestWhatsAppResponse,
  GetWhatsAppMessagesFilters,
} from '../types/whatsapp.types';

/**
 * WhatsAppRepository
 * Phase 7A.4: WhatsApp Integration API client
 * Handles all WhatsApp-related API calls following the repository pattern
 *
 * User endpoints from WhatsAppController.cs:
 * - GET /api/whatsapp/preferences - Get user's WhatsApp preferences
 * - POST /api/whatsapp/enable - Enable WhatsApp with phone number
 * - POST /api/whatsapp/disable - Disable WhatsApp
 * - POST /api/whatsapp/verify/request - Request phone verification code (SMS)
 * - POST /api/whatsapp/verify/confirm - Verify phone with 6-digit code
 * - PUT /api/whatsapp/preferences - Update notification preferences
 *
 * Admin endpoints from WhatsAppAdminController.cs:
 * - GET /api/whatsapp-admin/metrics - Delivery metrics
 * - GET /api/whatsapp-admin/templates - Template list
 * - POST /api/whatsapp-admin/test-message - Send test message
 * - GET /api/whatsapp-admin/messages - Message history
 */
export class WhatsAppRepository {
  private readonly basePath = '/whatsapp';
  private readonly adminBasePath = '/whatsapp-admin';

  // ==================== USER QUERIES ====================

  /**
   * Get current user's WhatsApp preferences
   * Maps to backend GetWhatsAppPreferencesQuery
   */
  async getPreferences(): Promise<WhatsAppPreferencesDto | null> {
    return await apiClient.get<WhatsAppPreferencesDto | null>(`${this.basePath}/preferences`);
  }

  // ==================== USER MUTATIONS ====================

  /**
   * Enable WhatsApp with phone number
   * Maps to backend EnableWhatsAppCommand
   *
   * @param data - Phone number in E.164 format
   */
  async enable(data: EnableWhatsAppRequest): Promise<EnableWhatsAppResponse> {
    return await apiClient.post<EnableWhatsAppResponse>(`${this.basePath}/enable`, data);
  }

  /**
   * Disable WhatsApp notifications
   * Maps to backend DisableWhatsAppCommand
   */
  async disable(): Promise<void> {
    await apiClient.post(`${this.basePath}/disable`);
  }

  /**
   * Request phone verification code via SMS
   * Maps to backend RequestPhoneVerificationCommand
   */
  async requestVerification(): Promise<void> {
    await apiClient.post(`${this.basePath}/verify/request`);
  }

  /**
   * Verify phone with 6-digit code
   * Maps to backend VerifyWhatsAppPhoneCommand
   *
   * @param data - Verification code
   */
  async verifyPhone(data: VerifyPhoneRequest): Promise<VerifyWhatsAppPhoneResponse> {
    return await apiClient.post<VerifyWhatsAppPhoneResponse>(`${this.basePath}/verify/confirm`, data);
  }

  /**
   * Update WhatsApp notification preferences
   * Maps to backend UpdateWhatsAppPreferencesCommand
   *
   * @param data - Notification toggle preferences
   */
  async updatePreferences(data: UpdateWhatsAppPreferencesRequest): Promise<void> {
    await apiClient.put(`${this.basePath}/preferences`, data);
  }

  // ==================== ADMIN QUERIES ====================

  /**
   * Get WhatsApp delivery metrics (admin)
   * Maps to backend GetWhatsAppMetricsQuery
   *
   * @param from - Start date
   * @param to - End date
   */
  async getMetrics(from: string, to: string): Promise<WhatsAppMetricsDto> {
    return await apiClient.get<WhatsAppMetricsDto>(
      `${this.adminBasePath}/metrics?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`
    );
  }

  /**
   * Get WhatsApp template list (admin)
   * Maps to backend GetWhatsAppTemplatesQuery
   */
  async getTemplates(): Promise<WhatsAppTemplateDto[]> {
    return await apiClient.get<WhatsAppTemplateDto[]>(`${this.adminBasePath}/templates`);
  }

  /**
   * Get WhatsApp message history (admin)
   * Maps to backend GetWhatsAppMessageHistoryQuery
   */
  async getMessages(filters?: GetWhatsAppMessagesFilters): Promise<WhatsAppMessageDto[]> {
    const params = new URLSearchParams();

    if (filters?.userId) {
      params.append('userId', filters.userId);
    }
    if (filters?.eventId) {
      params.append('eventId', filters.eventId);
    }
    if (filters?.page) {
      params.append('page', filters.page.toString());
    }
    if (filters?.pageSize) {
      params.append('pageSize', filters.pageSize.toString());
    }

    const queryString = params.toString();
    const url = queryString
      ? `${this.adminBasePath}/messages?${queryString}`
      : `${this.adminBasePath}/messages`;

    return await apiClient.get<WhatsAppMessageDto[]>(url);
  }

  // ==================== ADMIN MUTATIONS ====================

  /**
   * Send test WhatsApp message (admin)
   * Maps to backend SendTestWhatsAppCommand
   */
  async sendTestMessage(data: SendTestWhatsAppRequest): Promise<SendTestWhatsAppResponse> {
    return await apiClient.post<SendTestWhatsAppResponse>(`${this.adminBasePath}/test-message`, data);
  }
}

/**
 * Singleton instance for use across the application
 */
export const whatsAppRepository = new WhatsAppRepository();
