/**
 * Phase 6A.89: Email Template Management Hooks
 * React Query hooks for admin email template management
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { emailTemplateManagementRepository } from '@/infrastructure/api/repositories/email-template-management.repository';
import type {
  GetEmailTemplatesRequest,
  UpdateEmailTemplateRequest,
  ToggleActiveRequest,
  TemplatePreviewRequest,
} from '@/infrastructure/api/types/email-template-management.types';

// Query key factory for cache management
export const emailTemplateKeys = {
  all: ['emailTemplates'] as const,
  lists: () => [...emailTemplateKeys.all, 'list'] as const,
  list: (filters: GetEmailTemplatesRequest) => [...emailTemplateKeys.lists(), filters] as const,
  details: () => [...emailTemplateKeys.all, 'detail'] as const,
  detail: (id: string) => [...emailTemplateKeys.details(), id] as const,
  preview: (id: string) => [...emailTemplateKeys.all, 'preview', id] as const,
};

/**
 * Hook to fetch paginated email templates with filters
 */
export function useEmailTemplates(filters?: GetEmailTemplatesRequest) {
  return useQuery({
    queryKey: emailTemplateKeys.list(filters || {}),
    queryFn: () => emailTemplateManagementRepository.getTemplates(filters),
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchOnWindowFocus: false,
  });
}

/**
 * Hook to fetch a single email template by ID
 */
export function useEmailTemplateById(id: string | null) {
  return useQuery({
    queryKey: emailTemplateKeys.detail(id || ''),
    queryFn: () => emailTemplateManagementRepository.getTemplateById(id!),
    enabled: !!id,
    staleTime: 2 * 60 * 1000, // 2 minutes
  });
}

/**
 * Hook to update an email template
 */
export function useUpdateEmailTemplate() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateEmailTemplateRequest }) =>
      emailTemplateManagementRepository.updateTemplate(id, request),
    onSuccess: (updatedTemplate) => {
      // Update the specific template in cache
      queryClient.setQueryData(
        emailTemplateKeys.detail(updatedTemplate.id),
        updatedTemplate
      );
      // Invalidate lists to refresh
      queryClient.invalidateQueries({ queryKey: emailTemplateKeys.lists() });
    },
  });
}

/**
 * Hook to toggle email template active status
 */
export function useToggleEmailTemplateActive() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      emailTemplateManagementRepository.toggleActive(id, { isActive }),
    onSuccess: (updatedTemplate) => {
      // Update the specific template in cache
      queryClient.setQueryData(
        emailTemplateKeys.detail(updatedTemplate.id),
        updatedTemplate
      );
      // Invalidate lists to refresh
      queryClient.invalidateQueries({ queryKey: emailTemplateKeys.lists() });
    },
  });
}

/**
 * Hook to preview an email template
 */
export function usePreviewEmailTemplate() {
  return useMutation({
    mutationFn: ({ id, parameters }: { id: string; parameters?: Record<string, string> }) =>
      emailTemplateManagementRepository.previewTemplate(id, { parameters }),
  });
}
