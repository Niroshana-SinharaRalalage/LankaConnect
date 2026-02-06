/**
 * Phase 6A.90: Support Tickets React Query Hooks
 * Hooks for admin support ticket management
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { adminSupportRepository } from '@/infrastructure/api/repositories/admin-support.repository';
import type {
  GetSupportTicketsRequest,
  SupportTicketStatus,
} from '@/infrastructure/api/types/admin-support.types';

// Query key factory for cache management
export const supportTicketKeys = {
  all: ['admin-support-tickets'] as const,
  lists: () => [...supportTicketKeys.all, 'list'] as const,
  list: (filters: GetSupportTicketsRequest) => [...supportTicketKeys.lists(), filters] as const,
  details: () => [...supportTicketKeys.all, 'detail'] as const,
  detail: (id: string) => [...supportTicketKeys.details(), id] as const,
  statistics: () => [...supportTicketKeys.all, 'statistics'] as const,
};

/**
 * Hook to fetch paginated support tickets
 */
export function useSupportTickets(filters: GetSupportTicketsRequest = {}) {
  return useQuery({
    queryKey: supportTicketKeys.list(filters),
    queryFn: () => adminSupportRepository.getTickets(filters),
    staleTime: 30 * 1000, // 30 seconds
  });
}

/**
 * Hook to fetch single support ticket details
 */
export function useSupportTicketDetails(ticketId: string | null) {
  return useQuery({
    queryKey: supportTicketKeys.detail(ticketId || ''),
    queryFn: () => adminSupportRepository.getTicketById(ticketId!),
    enabled: !!ticketId,
    staleTime: 30 * 1000,
  });
}

/**
 * Hook to fetch support ticket statistics
 */
export function useSupportTicketStatistics() {
  return useQuery({
    queryKey: supportTicketKeys.statistics(),
    queryFn: () => adminSupportRepository.getStatistics(),
    staleTime: 60 * 1000, // 1 minute
  });
}

/**
 * Hook to reply to a support ticket
 */
export function useReplyToTicket() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ ticketId, content }: { ticketId: string; content: string }) =>
      adminSupportRepository.replyToTicket(ticketId, { content }),
    onSuccess: (_, { ticketId }) => {
      // Invalidate ticket details and list
      queryClient.invalidateQueries({ queryKey: supportTicketKeys.detail(ticketId) });
      queryClient.invalidateQueries({ queryKey: supportTicketKeys.lists() });
    },
  });
}

/**
 * Hook to update support ticket status
 */
export function useUpdateTicketStatus() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ ticketId, status }: { ticketId: string; status: SupportTicketStatus }) =>
      adminSupportRepository.updateStatus(ticketId, status),
    onSuccess: (_, { ticketId }) => {
      // Invalidate ticket details, list, and statistics
      queryClient.invalidateQueries({ queryKey: supportTicketKeys.detail(ticketId) });
      queryClient.invalidateQueries({ queryKey: supportTicketKeys.lists() });
      queryClient.invalidateQueries({ queryKey: supportTicketKeys.statistics() });
    },
  });
}

/**
 * Hook to assign a support ticket to an admin
 */
export function useAssignTicket() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ ticketId, assignToUserId }: { ticketId: string; assignToUserId: string }) =>
      adminSupportRepository.assignTicket(ticketId, assignToUserId),
    onSuccess: (_, { ticketId }) => {
      // Invalidate ticket details, list, and statistics
      queryClient.invalidateQueries({ queryKey: supportTicketKeys.detail(ticketId) });
      queryClient.invalidateQueries({ queryKey: supportTicketKeys.lists() });
      queryClient.invalidateQueries({ queryKey: supportTicketKeys.statistics() });
    },
  });
}

/**
 * Hook to add internal note to a support ticket
 */
export function useAddTicketNote() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ ticketId, content }: { ticketId: string; content: string }) =>
      adminSupportRepository.addNote(ticketId, { content }),
    onSuccess: (_, { ticketId }) => {
      // Invalidate ticket details
      queryClient.invalidateQueries({ queryKey: supportTicketKeys.detail(ticketId) });
    },
  });
}
