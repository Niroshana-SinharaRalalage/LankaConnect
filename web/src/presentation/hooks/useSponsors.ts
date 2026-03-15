'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import type {
  EventSponsorsResponse,
  SponsorSummaryDto,
  CreateMoneySponsorRequest,
  CreateItemSponsorRequest,
} from '@/infrastructure/api/types/events.types';

export const sponsorKeys = {
  all: ['sponsors'] as const,
  byEvent: (eventId: string) => [...sponsorKeys.all, 'event', eventId] as const,
  summary: (eventId: string) => [...sponsorKeys.all, 'summary', eventId] as const,
};

export function useEventSponsors(eventId: string | undefined, enabled = true) {
  return useQuery<EventSponsorsResponse>({
    queryKey: sponsorKeys.byEvent(eventId || ''),
    queryFn: () => eventsRepository.getEventSponsors(eventId!),
    enabled: !!eventId && enabled,
    staleTime: 2 * 60 * 1000,
    refetchOnWindowFocus: true,
  });
}

export function useSponsorSummary(eventId: string | undefined, enabled = true) {
  return useQuery<SponsorSummaryDto>({
    queryKey: sponsorKeys.summary(eventId || ''),
    queryFn: () => eventsRepository.getSponsorSummary(eventId!),
    enabled: !!eventId && enabled,
    staleTime: 2 * 60 * 1000,
  });
}

export function useCreateMoneySponsor() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: { eventId: string; request: CreateMoneySponsorRequest }) =>
      eventsRepository.createMoneySponsor(data.eventId, data.request),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: sponsorKeys.byEvent(variables.eventId) });
      queryClient.invalidateQueries({ queryKey: sponsorKeys.summary(variables.eventId) });
    },
  });
}

export function useCreateItemSponsor() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: { eventId: string; request: CreateItemSponsorRequest }) =>
      eventsRepository.createItemSponsor(data.eventId, data.request),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: sponsorKeys.byEvent(variables.eventId) });
      queryClient.invalidateQueries({ queryKey: sponsorKeys.summary(variables.eventId) });
    },
  });
}
