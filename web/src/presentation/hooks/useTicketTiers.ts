/**
 * Phase 8: Ticket Tier React Query Hooks
 *
 * Provides React Query hooks for Ticket Tier CRUD operations.
 * Handles caching, mutations, and automatic cache invalidation.
 */

import {
  useQuery,
  useMutation,
  useQueryClient,
  type UseQueryOptions,
} from '@tanstack/react-query';

import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import type {
  TicketTierDto,
  CreateTicketTierRequest,
  UpdateTicketTierRequest,
} from '@/infrastructure/api/types/events.types';
import { TicketingMode } from '@/infrastructure/api/types/events.types';
import { ApiError } from '@/infrastructure/api/client/api-errors';
import { eventKeys } from './useEvents';

/**
 * Query keys for ticket tiers
 */
export const ticketTierKeys = {
  all: ['ticketTiers'] as const,
  list: (eventId: string) => [...ticketTierKeys.all, 'list', eventId] as const,
};

/**
 * Fetch ticket tiers for an event.
 * Public — no auth required.
 */
export function useTicketTiers(
  eventId: string | undefined,
  options?: Omit<UseQueryOptions<TicketTierDto[], ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery<TicketTierDto[], ApiError>({
    queryKey: ticketTierKeys.list(eventId ?? ''),
    queryFn: () => eventsRepository.getTicketTiers(eventId!),
    enabled: !!eventId,
    ...options,
  });
}

/**
 * Set the ticketing mode for an event.
 */
export function useSetTicketingMode() {
  const queryClient = useQueryClient();

  return useMutation<void, ApiError, { eventId: string; mode: TicketingMode }>({
    mutationFn: ({ eventId, mode }) => eventsRepository.setTicketingMode(eventId, mode),
    onSuccess: (_, { eventId }) => {
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(eventId) });
      queryClient.invalidateQueries({ queryKey: ticketTierKeys.list(eventId) });
    },
  });
}

/**
 * Add a ticket tier to an event. Returns the new tier ID.
 */
export function useAddTicketTier() {
  const queryClient = useQueryClient();

  return useMutation<string, ApiError, { eventId: string; request: CreateTicketTierRequest }>({
    mutationFn: ({ eventId, request }) => eventsRepository.addTicketTier(eventId, request),
    onSuccess: (_, { eventId }) => {
      queryClient.invalidateQueries({ queryKey: ticketTierKeys.list(eventId) });
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(eventId) });
    },
  });
}

/**
 * Update an existing ticket tier.
 */
export function useUpdateTicketTier() {
  const queryClient = useQueryClient();

  return useMutation<void, ApiError, { eventId: string; tierId: string; request: UpdateTicketTierRequest }>({
    mutationFn: ({ eventId, tierId, request }) =>
      eventsRepository.updateTicketTier(eventId, tierId, request),
    onSuccess: (_, { eventId }) => {
      queryClient.invalidateQueries({ queryKey: ticketTierKeys.list(eventId) });
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(eventId) });
    },
  });
}

/**
 * Remove a ticket tier from an event.
 */
export function useRemoveTicketTier() {
  const queryClient = useQueryClient();

  return useMutation<void, ApiError, { eventId: string; tierId: string }>({
    mutationFn: ({ eventId, tierId }) => eventsRepository.removeTicketTier(eventId, tierId),
    onSuccess: (_, { eventId }) => {
      queryClient.invalidateQueries({ queryKey: ticketTierKeys.list(eventId) });
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(eventId) });
    },
  });
}
