/**
 * WhatsApp React Query Hooks
 *
 * Provides React Query hooks for WhatsApp API integration
 * Implements caching, optimistic updates, and proper error handling
 *
 * Phase 7A.4: WhatsApp Integration Frontend
 *
 * @requires @tanstack/react-query
 * @requires whatsAppRepository from infrastructure/repositories/whatsapp.repository
 * @requires WhatsApp types from infrastructure/api/types/whatsapp.types
 */

import {
  useQuery,
  useMutation,
  useQueryClient,
  UseQueryOptions,
} from '@tanstack/react-query';
import toast from 'react-hot-toast';

import { whatsAppRepository } from '@/infrastructure/api/repositories/whatsapp.repository';
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
} from '@/infrastructure/api/types/whatsapp.types';

import { ApiError } from '@/infrastructure/api/client/api-errors';

/**
 * Query Keys for WhatsApp
 * Centralized query key management for cache invalidation
 */
export const whatsAppKeys = {
  all: ['whatsapp'] as const,
  preferences: () => [...whatsAppKeys.all, 'preferences'] as const,
  // Admin keys
  admin: () => [...whatsAppKeys.all, 'admin'] as const,
  metrics: (from: string, to: string) => [...whatsAppKeys.admin(), 'metrics', from, to] as const,
  templates: () => [...whatsAppKeys.admin(), 'templates'] as const,
  messages: (filters?: GetWhatsAppMessagesFilters) => [...whatsAppKeys.admin(), 'messages', filters] as const,
};

// ==================== USER HOOKS ====================

/**
 * useWhatsAppPreferences Hook
 *
 * Fetches current user's WhatsApp preferences
 * Returns null if user has not set up WhatsApp
 */
export function useWhatsAppPreferences(
  options?: Omit<UseQueryOptions<WhatsAppPreferencesDto | null, ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: whatsAppKeys.preferences(),
    queryFn: () => whatsAppRepository.getPreferences(),
    staleTime: 2 * 60 * 1000, // 2 minutes
    refetchOnWindowFocus: true,
    retry: 1,
    ...options,
  });
}

/**
 * useEnableWhatsApp Hook
 *
 * Mutation hook for enabling WhatsApp with phone number
 * Invalidates preferences cache on success
 */
export function useEnableWhatsApp() {
  const queryClient = useQueryClient();

  return useMutation<EnableWhatsAppResponse, ApiError, EnableWhatsAppRequest>({
    mutationFn: (data) => whatsAppRepository.enable(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: whatsAppKeys.preferences() });
      toast.success('WhatsApp enabled! Please verify your phone number.', { duration: 4000 });
    },
    onError: (error) => {
      toast.error(error?.message || 'Failed to enable WhatsApp. Please try again.', { duration: 5000 });
    },
  });
}

/**
 * useDisableWhatsApp Hook
 *
 * Mutation hook for disabling WhatsApp notifications
 */
export function useDisableWhatsApp() {
  const queryClient = useQueryClient();

  return useMutation<void, ApiError, void>({
    mutationFn: () => whatsAppRepository.disable(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: whatsAppKeys.preferences() });
      toast.success('WhatsApp notifications disabled.', { duration: 3000 });
    },
    onError: (error) => {
      toast.error(error?.message || 'Failed to disable WhatsApp. Please try again.', { duration: 5000 });
    },
  });
}

/**
 * useRequestVerification Hook
 *
 * Mutation hook for requesting phone verification code via SMS
 */
export function useRequestVerification() {
  return useMutation<void, ApiError, void>({
    mutationFn: () => whatsAppRepository.requestVerification(),
    onSuccess: () => {
      toast.success('Verification code sent via SMS. Check your phone!', { duration: 5000 });
    },
    onError: (error) => {
      toast.error(error?.message || 'Failed to send verification code. Please try again.', { duration: 5000 });
    },
  });
}

/**
 * useVerifyPhone Hook
 *
 * Mutation hook for verifying phone with 6-digit code
 */
export function useVerifyPhone() {
  const queryClient = useQueryClient();

  return useMutation<VerifyWhatsAppPhoneResponse, ApiError, VerifyPhoneRequest>({
    mutationFn: (data) => whatsAppRepository.verifyPhone(data),
    onSuccess: (response) => {
      queryClient.invalidateQueries({ queryKey: whatsAppKeys.preferences() });
      if (response.phoneVerified) {
        toast.success('Phone number verified! WhatsApp notifications are now active.', { duration: 5000 });
      } else {
        toast.error(response.errorMessage || 'Verification failed. Please try again.', { duration: 5000 });
      }
    },
    onError: (error) => {
      toast.error(error?.message || 'Verification failed. Please try again.', { duration: 5000 });
    },
  });
}

/**
 * useUpdateWhatsAppPreferences Hook
 *
 * Mutation hook for updating WhatsApp notification preferences
 */
export function useUpdateWhatsAppPreferences() {
  const queryClient = useQueryClient();

  return useMutation<void, ApiError, UpdateWhatsAppPreferencesRequest>({
    mutationFn: (data) => whatsAppRepository.updatePreferences(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: whatsAppKeys.preferences() });
      toast.success('WhatsApp preferences updated.', { duration: 3000 });
    },
    onError: (error) => {
      toast.error(error?.message || 'Failed to update preferences. Please try again.', { duration: 5000 });
    },
  });
}

// ==================== ADMIN HOOKS ====================

/**
 * useWhatsAppMetrics Hook (admin)
 *
 * Fetches WhatsApp delivery metrics for a date range
 */
export function useWhatsAppMetrics(
  from: string,
  to: string,
  options?: Omit<UseQueryOptions<WhatsAppMetricsDto, ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: whatsAppKeys.metrics(from, to),
    queryFn: () => whatsAppRepository.getMetrics(from, to),
    staleTime: 1 * 60 * 1000, // 1 minute
    enabled: !!from && !!to,
    ...options,
  });
}

/**
 * useWhatsAppTemplates Hook (admin)
 *
 * Fetches all WhatsApp templates with approval status
 */
export function useWhatsAppTemplates(
  options?: Omit<UseQueryOptions<WhatsAppTemplateDto[], ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: whatsAppKeys.templates(),
    queryFn: () => whatsAppRepository.getTemplates(),
    staleTime: 5 * 60 * 1000, // 5 minutes
    ...options,
  });
}

/**
 * useWhatsAppMessages Hook (admin)
 *
 * Fetches WhatsApp message history with optional filters
 */
export function useWhatsAppMessages(
  filters?: GetWhatsAppMessagesFilters,
  options?: Omit<UseQueryOptions<WhatsAppMessageDto[], ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: whatsAppKeys.messages(filters),
    queryFn: () => whatsAppRepository.getMessages(filters),
    staleTime: 1 * 60 * 1000, // 1 minute
    ...options,
  });
}

/**
 * useSendTestWhatsApp Hook (admin)
 *
 * Mutation hook for sending a test WhatsApp message
 */
export function useSendTestWhatsApp() {
  return useMutation<SendTestWhatsAppResponse, ApiError, SendTestWhatsAppRequest>({
    mutationFn: (data) => whatsAppRepository.sendTestMessage(data),
    onSuccess: (response) => {
      if (response.success) {
        toast.success('Test message sent successfully!', { duration: 4000 });
      } else {
        toast.error(response.errorMessage || 'Test message failed.', { duration: 5000 });
      }
    },
    onError: (error) => {
      toast.error(error?.message || 'Failed to send test message.', { duration: 5000 });
    },
  });
}
