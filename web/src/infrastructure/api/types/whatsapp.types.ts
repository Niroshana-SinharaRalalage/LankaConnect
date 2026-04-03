/**
 * WhatsApp API Type Definitions
 * Phase 7A.4: WhatsApp Integration Frontend
 * DTOs matching backend API contracts (LankaConnect.Application.Communications.WhatsApp)
 */

// ==================== Enums ====================

/**
 * WhatsApp message status matching backend WhatsAppMessageStatus
 * Backend uses JsonStringEnumConverter → string values
 */
export enum WhatsAppMessageStatus {
  Draft = 'Draft',
  Scheduled = 'Scheduled',
  PendingValidation = 'PendingValidation',
  ValidationFailed = 'ValidationFailed',
  Sending = 'Sending',
  Sent = 'Sent',
  Delivered = 'Delivered',
  Read = 'Read',
  Failed = 'Failed',
  Expired = 'Expired',
  Cancelled = 'Cancelled',
  Rejected = 'Rejected',
}

/**
 * WhatsApp message type matching backend WhatsAppMessageType
 */
export enum WhatsAppMessageType {
  Text = 'Text',
  Template = 'Template',
  Media = 'Media',
  Interactive = 'Interactive',
  Broadcast = 'Broadcast',
  EventNotification = 'EventNotification',
  FestivalGreeting = 'FestivalGreeting',
  CommunityAnnouncement = 'CommunityAnnouncement',
  Reminder = 'Reminder',
  LocationBased = 'LocationBased',
}

/**
 * WhatsApp template category matching backend WhatsAppTemplateCategory
 */
export enum WhatsAppTemplateCategory {
  Utility = 'Utility',
  Marketing = 'Marketing',
}

/**
 * WhatsApp template status matching backend WhatsAppTemplateStatus
 */
export enum WhatsAppTemplateStatus {
  Pending = 'Pending',
  Approved = 'Approved',
  Rejected = 'Rejected',
}

// ==================== Response DTOs ====================

/**
 * WhatsApp preferences DTO matching backend WhatsAppPreferencesDto
 */
export interface WhatsAppPreferencesDto {
  userId: string;
  whatsAppEnabled: boolean;
  whatsAppPhoneNumber: string | null;
  phoneVerified: boolean;
  phoneVerifiedAt: string | null;
  isFullyVerified: boolean;
  isLocked: boolean;
  verificationLockedUntil: string | null;
  // Per-notification-type preferences
  notifyEventRegistration: boolean;
  notifyEventReminder: boolean;
  notifyEventCancellation: boolean;
  notifyEventUpdate: boolean;
  notifySignupCommitment: boolean;
  notifyRefund: boolean;
  notifyNewsletter: boolean;
  notifyNewEvent: boolean;
  notifyPayment: boolean;
  // Cultural preferences
  preferredLanguage: string;
  quietHoursStart: string | null;
  quietHoursEnd: string | null;
  respectCulturalTiming: boolean;
}

/**
 * Enable WhatsApp response matching backend EnableWhatsAppResponse
 */
export interface EnableWhatsAppResponse {
  userId: string;
  phoneNumber: string;
  isEnabled: boolean;
  phoneVerified: boolean;
}

/**
 * Verify phone response matching backend VerifyWhatsAppPhoneResponse
 */
export interface VerifyWhatsAppPhoneResponse {
  phoneVerified: boolean;
  errorMessage: string | null;
}

/**
 * WhatsApp metrics DTO matching backend WhatsAppMetricsDto (admin)
 */
export interface WhatsAppMetricsDto {
  totalSent: number;
  totalDelivered: number;
  totalRead: number;
  totalFailed: number;
  deliveryRate: number;
  readRate: number;
  byTemplate: Record<string, number>;
  from: string;
  to: string;
}

/**
 * WhatsApp template DTO matching backend WhatsAppTemplateDto (admin)
 */
export interface WhatsAppTemplateDto {
  id: string;
  templateName: string;
  displayName: string;
  category: WhatsAppTemplateCategory;
  status: WhatsAppTemplateStatus;
  parameterCount: number;
  language: string;
  isApproved: boolean;
  isUsable: boolean;
  headerText: string | null;
  bodyText: string;
  footerText: string | null;
  parameterNames: string[];
}

/**
 * WhatsApp message DTO matching backend WhatsAppMessageDto (admin)
 */
export interface WhatsAppMessageDto {
  id: string;
  toPhoneNumber: string;
  templateName: string | null;
  status: WhatsAppMessageStatus;
  messageType: WhatsAppMessageType;
  language: string;
  sentAt: string | null;
  deliveredAt: string | null;
  readAt: string | null;
  failedAt: string | null;
  errorMessage: string | null;
  retryCount: number;
  userId: string | null;
  eventId: string | null;
}

/**
 * Send test WhatsApp response matching backend SendTestWhatsAppResponse (admin)
 */
export interface SendTestWhatsAppResponse {
  success: boolean;
  messageId: string | null;
  errorMessage: string | null;
}

// ==================== Request DTOs ====================

/**
 * Enable WhatsApp request matching backend EnableWhatsAppRequest
 */
export interface EnableWhatsAppRequest {
  phoneNumber: string;
}

/**
 * Verify phone request matching backend VerifyPhoneRequest
 */
export interface VerifyPhoneRequest {
  code: string;
}

/**
 * Update preferences request matching backend UpdatePreferencesRequest
 */
export interface UpdateWhatsAppPreferencesRequest {
  eventRegistration: boolean;
  eventReminder: boolean;
  eventCancellation: boolean;
  eventUpdate: boolean;
  signupCommitment: boolean;
  refund: boolean;
  newsletter: boolean;
  newEvent: boolean;
  payment: boolean;
  preferredLanguage?: string | null;
  quietHoursStart?: string | null;
  quietHoursEnd?: string | null;
  respectCulturalTiming: boolean;
}

/**
 * Send test WhatsApp request matching backend SendTestWhatsAppRequest (admin)
 */
export interface SendTestWhatsAppRequest {
  recipientPhone: string;
  templateName: string;
  parameters?: Record<string, string> | null;
}

/**
 * Filters for WhatsApp message history query (admin)
 */
export interface GetWhatsAppMessagesFilters {
  userId?: string;
  eventId?: string;
  page?: number;
  pageSize?: number;
}
