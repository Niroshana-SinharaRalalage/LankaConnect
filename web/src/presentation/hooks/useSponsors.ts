'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import type {
  EventSponsorsResponse,
  SponsorSummaryDto,
  SponsorDto,
  CreateMoneySponsorRequest,
  CreateItemSponsorRequest,
  CreateOffPlatformSponsorRequest,
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
 * Phase 6A.145 — upload (or replace) a sponsor's image. Any sponsor may attach an
 * image regardless of amount. Invalidates sponsor list + summary + the user's
 * own-sponsors query so the new thumbnail surfaces everywhere.
 */
export function useUploadSponsorImage() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: { eventId: string; sponsorId: string; file: File }) =>
      eventsRepository.uploadSponsorImage(data.eventId, data.sponsorId, data.file),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: sponsorKeys.byEvent(variables.eventId) });
      queryClient.invalidateQueries({ queryKey: sponsorKeys.summary(variables.eventId) });
      queryClient.invalidateQueries({ queryKey: sponsorKeys.mine(variables.eventId) });
    },
  });
}

/**
 * Phase 6A.145 — clear a sponsor's image (organizer-only). Idempotent.
 */
export function useDeleteSponsorImage() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: { eventId: string; sponsorId: string }) =>
      eventsRepository.deleteSponsorImage(data.eventId, data.sponsorId),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: sponsorKeys.byEvent(variables.eventId) });
      queryClient.invalidateQueries({ queryKey: sponsorKeys.summary(variables.eventId) });
      queryClient.invalidateQueries({ queryKey: sponsorKeys.mine(variables.eventId) });
    },
  });
}

/**
 * Phase 6A.145 — organizer records an off-platform sponsorship (cash money or in-kind
 * item collected outside the platform). Bypasses Stripe entirely; the resulting Sponsor
 * is immediately Completed (money) or RecordedItem (item). Optional image upload.
 */
export function useCreateOffPlatformSponsor() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: { eventId: string; request: CreateOffPlatformSponsorRequest }) =>
      eventsRepository.createOffPlatformSponsor(data.eventId, data.request),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: sponsorKeys.byEvent(variables.eventId) });
      queryClient.invalidateQueries({ queryKey: sponsorKeys.summary(variables.eventId) });
    },
  });
}
