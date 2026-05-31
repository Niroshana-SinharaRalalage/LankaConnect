'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import type {
  SponsorshipPackageDto,
  SponsorshipPackagePublicDto,
  CreateSponsorshipPackageRequest,
  CreatePackageSponsorRequest,
  CreatePackageSponsorResult,
  UpdateSponsorshipPackageRequest,
  SetSponsorshipPackageImageResult,
} from '@/infrastructure/api/types/events.types';

/**
 * Phase 6A.156 — React Query hooks for organizer-defined sponsorship packages.
 * Mirrors `useAddOns.ts` shape so reviewers familiar with the add-on flow can
 * navigate this hook unchanged. Phase 6A.157 adds public/buyer hooks at the
 * bottom of the file.
 */
export const sponsorshipPackageKeys = {
  all: ['sponsorshipPackages'] as const,
  list: (eventId: string) => [...sponsorshipPackageKeys.all, 'list', eventId] as const,
  // Phase 6A.157 — separate cache key for the public (active-only) list so it
  // doesn't collide with the organizer list (which includes inactive rows).
  publicList: (eventId: string) => [...sponsorshipPackageKeys.all, 'publicList', eventId] as const,
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

// ────────────────────────────────────────────────────────────────────────────
// Phase 6A.157 — public/buyer-facing hooks
// ────────────────────────────────────────────────────────────────────────────

/**
 * Phase 6A.157 — anonymous list of buyable packages for an event. Returns
 * `[]` for events that haven't opted into packages (server-side filtered).
 * Distinct cache key from `useSponsorshipPackages` (organizer view) so the
 * two surfaces don't accidentally share invalidations.
 *
 * Safe to call on the public event detail page — the underlying endpoint is
 * `[AllowAnonymous]` and does NOT trigger the api-client's auth-redirect
 * chain (per CLAUDE.md memory `feedback_401_does_not_prove_feature_reachable`
 * — the route avoids the 401 path entirely, not just suppresses it).
 */
export function usePublicSponsorshipPackages(eventId: string | undefined, enabled = true) {
  return useQuery<SponsorshipPackagePublicDto[]>({
    queryKey: sponsorshipPackageKeys.publicList(eventId || ''),
    queryFn: () => eventsRepository.getActiveSponsorshipPackages(eventId!),
    enabled: !!eventId && enabled,
    // 60 s freshness — buyer-list changes (stock decrement, manual deactivate)
    // surface on next event-page refresh without thrashing React Query cache.
    staleTime: 60 * 1000,
    refetchOnWindowFocus: true,
  });
}

/**
 * Phase 6A.157 — purchase mutation. On success the caller redirects to
 * `result.checkoutUrl` (Stripe Checkout for paid, SuccessUrl directly for
 * free $0 packages). Invalidates the public list so a successful purchase
 * that decremented stock shows updated remainingStock on next list fetch.
 */
export function usePurchasePackageSponsor() {
  const queryClient = useQueryClient();
  return useMutation<
    CreatePackageSponsorResult,
    Error,
    { eventId: string; packageId: string; request: CreatePackageSponsorRequest }
  >({
    mutationFn: (data) =>
      eventsRepository.purchaseSponsorshipPackage(data.eventId, data.packageId, data.request),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({
        queryKey: sponsorshipPackageKeys.publicList(variables.eventId),
      });
    },
  });
}
