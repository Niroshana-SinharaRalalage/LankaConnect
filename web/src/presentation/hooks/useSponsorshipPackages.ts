'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import type {
  SponsorshipPackageDto,
  CreateSponsorshipPackageRequest,
  UpdateSponsorshipPackageRequest,
  SetSponsorshipPackageImageResult,
} from '@/infrastructure/api/types/events.types';

/**
 * Phase 6A.156 — React Query hooks for organizer-defined sponsorship packages.
 * Mirrors `useAddOns.ts` shape so reviewers familiar with the add-on flow can
 * navigate this hook unchanged. All hooks here are organizer-only — public
 * buyer-facing hooks (read active list, purchase, cart) land in 6A.157.
 */
export const sponsorshipPackageKeys = {
  all: ['sponsorshipPackages'] as const,
  list: (eventId: string) => [...sponsorshipPackageKeys.all, 'list', eventId] as const,
};

/**
 * Lists all sponsorship packages for an event (organizer view — includes inactive).
 */
export function useSponsorshipPackages(eventId: string | undefined, enabled = true) {
  return useQuery<SponsorshipPackageDto[]>({
    queryKey: sponsorshipPackageKeys.list(eventId || ''),
    queryFn: () => eventsRepository.getSponsorshipPackages(eventId!),
    enabled: !!eventId && enabled,
    staleTime: 2 * 60 * 1000,
    refetchOnWindowFocus: true,
  });
}

export function useCreateSponsorshipPackage() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: { eventId: string; request: CreateSponsorshipPackageRequest }) =>
      eventsRepository.createSponsorshipPackage(data.eventId, data.request),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: sponsorshipPackageKeys.list(variables.eventId) });
    },
  });
}

export function useUpdateSponsorshipPackage() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: {
      eventId: string;
      packageId: string;
      request: UpdateSponsorshipPackageRequest;
    }) =>
      eventsRepository.updateSponsorshipPackage(
        data.eventId,
        data.packageId,
        data.request,
      ),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: sponsorshipPackageKeys.list(variables.eventId) });
    },
  });
}

export function useDeleteSponsorshipPackage() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: { eventId: string; packageId: string }) =>
      eventsRepository.deleteSponsorshipPackage(data.eventId, data.packageId),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: sponsorshipPackageKeys.list(variables.eventId) });
    },
  });
}

export function useUploadSponsorshipPackageImage() {
  const queryClient = useQueryClient();
  return useMutation<
    SetSponsorshipPackageImageResult,
    Error,
    { eventId: string; packageId: string; file: File }
  >({
    mutationFn: (data) =>
      eventsRepository.uploadSponsorshipPackageImage(
        data.eventId,
        data.packageId,
        data.file,
      ),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: sponsorshipPackageKeys.list(variables.eventId) });
    },
  });
}

export function useDeleteSponsorshipPackageImage() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: { eventId: string; packageId: string }) =>
      eventsRepository.deleteSponsorshipPackageImage(data.eventId, data.packageId),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: sponsorshipPackageKeys.list(variables.eventId) });
    },
  });
}
