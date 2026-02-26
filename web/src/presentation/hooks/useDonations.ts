'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import type {
  EventDonationsResponse,
  DonationSummaryDto,
  CreateDonationRequest,
} from '@/infrastructure/api/types/events.types';

export const donationKeys = {
  all: ['donations'] as const,
  byEvent: (eventId: string) => [...donationKeys.all, 'event', eventId] as const,
  summary: (eventId: string) => [...donationKeys.all, 'summary', eventId] as const,
};

/**
 * Fetches all donations for an event (organizer).
 */
export function useEventDonations(eventId: string | undefined, enabled = true) {
  return useQuery<EventDonationsResponse>({
    queryKey: donationKeys.byEvent(eventId || ''),
    queryFn: () => eventsRepository.getEventDonations(eventId!),
    enabled: !!eventId && enabled,
    staleTime: 2 * 60 * 1000,
    refetchOnWindowFocus: true,
  });
}

/**
 * Fetches donation summary for an event (organizer).
 */
export function useDonationSummary(eventId: string | undefined, enabled = true) {
  return useQuery<DonationSummaryDto>({
    queryKey: donationKeys.summary(eventId || ''),
    queryFn: () => eventsRepository.getDonationSummary(eventId!),
    enabled: !!eventId && enabled,
    staleTime: 2 * 60 * 1000,
  });
}

/**
 * Creates a standalone donation. Returns the Stripe checkout URL.
 */
export function useCreateDonation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: { eventId: string; request: CreateDonationRequest }) =>
      eventsRepository.createDonation(data.eventId, data.request),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: donationKeys.byEvent(variables.eventId) });
      queryClient.invalidateQueries({ queryKey: donationKeys.summary(variables.eventId) });
    },
  });
}
