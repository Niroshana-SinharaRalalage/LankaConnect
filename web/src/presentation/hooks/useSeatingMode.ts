/**
 * Seating Redesign Slice 1: React Query hook for setting an event's SeatingMode.
 *
 * Backend endpoint: PUT /api/events/{id}/seating-mode
 * Domain rule enforced server-side: AssignedSeating requires TicketingMode.Tiered.
 */

import { useMutation, useQueryClient } from '@tanstack/react-query';

import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import { SeatingMode } from '@/infrastructure/api/types/events.types';
import { ApiError } from '@/infrastructure/api/client/api-errors';
import { eventKeys } from './useEvents';

/**
 * Mutation hook to change SeatingMode on an existing event.
 * Invalidates the event detail cache on success so consumers re-fetch.
 */
export function useSetSeatingMode() {
  const queryClient = useQueryClient();

  return useMutation<void, ApiError, { eventId: string; mode: SeatingMode }>({
    mutationFn: ({ eventId, mode }) => eventsRepository.setSeatingMode(eventId, mode),
    onSuccess: (_, { eventId }) => {
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(eventId) });
    },
  });
}
