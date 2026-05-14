'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import { eventKeys } from '@/presentation/hooks/useEvents';
import type {
  EventSponsorsResponse,
  SponsorSummaryDto,
  SponsorDto,
  CreateMoneySponsorRequest,
  CreateItemSponsorRequest,
} from '@/infrastructure/api/types/events.types';

export const sponsorKeys = {
  all: ['sponsors'] as const,
  byEvent: (eventId: string) => [...sponsorKeys.all, 'event', eventId] as const,
  summary: (eventId: string) => [...sponsorKeys.all, 'summary', eventId] as const,
  mine: (eventId: string) => [...sponsorKeys.all, 'mine', eventId] as const,
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

/**
 * Fetches the authenticated user's own sponsorships for an event.
 */
export function useMySponsors(eventId: string | undefined, enabled = true) {
  return useQuery<SponsorDto[]>({
    queryKey: sponsorKeys.mine(eventId || ''),
    queryFn: () => eventsRepository.getMySponsors(eventId!),
    enabled: !!eventId && enabled,
    staleTime: 2 * 60 * 1000,
    refetchOnWindowFocus: true,
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

/**
 * Phase 6A.143 — upload (or replace) the sponsor banner image.
 * Banner lives on Event.SponsorConfig (JSONB VO), so invalidate the event detail query
 * to make SponsorSection pick up the new URL.
 */
export function useUploadSponsorImage() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: { eventId: string; file: File }) =>
      eventsRepository.uploadSponsorImage(data.eventId, data.file),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(variables.eventId) });
    },
  });
}

/**
 * Phase 6A.143 — clear the sponsor banner image. Idempotent.
 */
export function useDeleteSponsorImage() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: { eventId: string }) =>
      eventsRepository.deleteSponsorImage(data.eventId),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(variables.eventId) });
    },
  });
}
